using System;
using System.Collections;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// Tir automatique (D1) + tir manuel E (parité web, coexistence actée) :
    /// raycast hitscan depuis le centre caméra, pellets indépendants pour les shotguns.
    public class WeaponController : MonoBehaviour
    {
        public Camera aimCamera;
        public LayerMask hitMask = ~0;

        /// Posés chaque frame par PlayerInputBridge (E maintenu / Q maintenu).
        [NonSerialized] public bool FireHeld;
        [NonSerialized] public bool AimHeld;

        /// Branchés par SliceBootstrap : adrénaline (PV du joueur), précision (immobile), surchauffe LMG.
        public PlayerHealth health;
        public PlayerMotor motor;
        public bool isLmg;
        readonly OverheatModel overheat = new OverheatModel();

        AmmoModel ammo;
        float cooldownRemaining;
        bool wasReloading;
        bool firePrev;
        Coroutine burstRoutine;

        public WeaponData Weapon { get; private set; }

        /// Mode de tir du tir MANUEL (E/clic) — T (burst) / P (semi) ; l'auto-fire mobile (D1) reste full-auto, indépendant.
        public FireMode Mode { get; private set; }

        // Stats effectives = fiche × formules d'upgrade (niveau depuis la save) — la fiche SO n'est jamais mutée
        public float EffectiveCooldown { get; private set; }
        public float EffectiveDmgMult { get; private set; }
        public float EffectiveRecoil { get; private set; }

        public event Action<int, int> AmmoChanged;
        public event Action<bool> ReloadingChanged;
        public event Action Fired;                                       // chaque coup parti : recul, muzzle flash, kick caméra
        public event Action<HitZone, bool, EnemyHealth> HitConfirmed;    // zone, killed, cible : hitmarker/kill feed/callouts
        public event Action<Vector3> TracerRequested;                    // point d'impact ou fin de portée (l'origine vient du viewmodel)
        public event Action<int, int> LongShot;                          // argent, xp — tir confirmé à ≥ 30 m (web #)
        public event Action<FireMode> ModeChanged;                       // T (burst) / P (semi) : indicateur HUD

        public void Init(WeaponData weapon, int upgradeLevel = 0)
        {
            Weapon = weapon;
            EffectiveCooldown = UpgradeFormulas.WeaponCooldown(weapon.cooldown, upgradeLevel);
            EffectiveDmgMult = UpgradeFormulas.WeaponDamageMult(weapon.dmgMult, upgradeLevel);
            EffectiveRecoil = UpgradeFormulas.WeaponRecoil(weapon.recoil, upgradeLevel);
            ammo = new AmmoModel(UpgradeFormulas.WeaponMag(weapon.mag, upgradeLevel), weapon.reload);
            Mode = FireModeModel.Default(weapon.id, weapon.semiAuto);
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }

        public void RequestReload()
        {
            if (Weapon != null) ammo.StartReload();
        }

        void SetMode(FireMode mode)
        {
            if (Mode == mode) return;
            if (mode != FireMode.Burst && burstRoutine != null)
            {
                StopCoroutine(burstRoutine);
                burstRoutine = null;
            }
            Mode = mode;
            ModeChanged?.Invoke(Mode);
        }

        /// T : bascule Auto ↔ Burst (uniquement sur les armes qui le permettent, web #).
        public void ToggleBurst()
        {
            if (Weapon == null || !FireModeModel.CanToggleBurst(Weapon.id)) return;
            SetMode(Mode == FireMode.Burst ? FireMode.Auto : FireMode.Burst);
        }

        /// P : bascule Auto ↔ Semi (sauf armes en semi-auto forcé, web #).
        public void ToggleSemi()
        {
            if (Weapon == null || FireModeModel.ForcesSemi(Weapon.id)) return;
            SetMode(Mode == FireMode.Semi ? FireMode.Auto : FireMode.Semi);
        }

        void Update()
        {
            if (Weapon == null || aimCamera == null) return;
            float dt = Time.deltaTime;

            ammo.Tick(dt);
            if (wasReloading != ammo.IsReloading)
            {
                wasReloading = ammo.IsReloading;
                ReloadingChanged?.Invoke(ammo.IsReloading);
                AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
            }
            cooldownRemaining -= dt;

            if (isLmg) overheat.Tick(Time.time);

            Ray center = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool onTarget = false;
            float distance = 0f;
            if (Physics.Raycast(center, out RaycastHit hit, Weapon.range, hitMask))
            {
                onTarget = hit.collider.GetComponentInParent<EnemyHealth>() != null;
                distance = hit.distance;
            }

            bool ammoReady = !ammo.IsReloading && ammo.InMag > 0;
            bool auto = AutoFireDecider.ShouldFire(onTarget, distance, Weapon.range, cooldownRemaining, ammoReady);
            bool weaponCanFire = !isLmg || overheat.CanFire;
            bool fireEdge = FireHeld && !firePrev;
            firePrev = FireHeld;

            if (auto && weaponCanFire)
            {
                Fire(); // tir auto mobile (D1) : full-auto, indépendant du mode de tir manuel
            }
            else if (weaponCanFire && cooldownRemaining <= 0f && ammoReady)
            {
                switch (Mode)
                {
                    case FireMode.Semi:
                        if (fireEdge) Fire(); // un coup par appui, jamais par frame maintenue
                        break;
                    case FireMode.Burst:
                        if (fireEdge && burstRoutine == null) burstRoutine = StartCoroutine(BurstRoutine());
                        break;
                    default: // Auto : comportement manuel historique, tire tant que E est maintenu
                        if (FireHeld) Fire();
                        break;
                }
            }

            if (ammo.InMag == 0 && !ammo.IsReloading)
                ammo.StartReload(); // l'événement ReloadingChanged partira au prochain Update via wasReloading
        }

        /// Burst manuel (T) : BurstSize coups espacés de BurstDelay, s'arrête si les munitions manquent en cours de rafale.
        IEnumerator BurstRoutine()
        {
            for (int i = 0; i < FireModeModel.BurstSize; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(FireModeModel.BurstDelay);
                    while (cooldownRemaining > 0f) yield return null; // respecte la cadence de l'arme
                }
                bool ammoReady = !ammo.IsReloading && ammo.InMag > 0;
                if (!ammoReady) break; // arrêt anticipé : plus de munitions en cours de rafale
                Fire();
            }
            burstRoutine = null;
        }

        void Fire()
        {
            if (!ammo.TryConsume()) return;
            cooldownRemaining = EffectiveCooldown;

            bool precision = AimHeld && (motor == null || !motor.IsMoving);
            bool adrenaline = health != null && CombatModifiers.IsAdrenaline(health.Health, health.MaxHealth);
            float dmgMult = EffectiveDmgMult * CombatModifiers.DamageMult(adrenaline, precision);
            bool longShotAwarded = false;

            int pellets = Mathf.Max(1, Weapon.pellets);
            for (int i = 0; i < pellets; i++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * EffectiveRecoil * 0.01f;
                Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f + spread.x, 0.5f + spread.y, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, Weapon.range, hitMask))
                {
                    TracerRequested?.Invoke(hit.point);
                    var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemy != null)
                    {
                        var result = enemy.TakeHit(hit.point, dmgMult, ray.origin);
                        if (result.blocked) CalloutEvents.Raise("BLOQUÉ!"); // bouclier frontal (web #119)
                        else if (result.valid)
                        {
                            HitConfirmed?.Invoke(result.zone, result.killed, enemy);
                            if (!longShotAwarded && CombatModifiers.IsLongShot(hit.distance))
                            {
                                longShotAwarded = true;
                                LongShot?.Invoke(CombatModifiers.LongShotMoney, CombatModifiers.LongShotXp);
                            }
                        }
                    }
                }
                else
                {
                    TracerRequested?.Invoke(ray.origin + ray.direction * Weapon.range);
                }
            }

            if (isLmg) overheat.OnShot(Time.time);

            Fired?.Invoke();
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }
    }
}
