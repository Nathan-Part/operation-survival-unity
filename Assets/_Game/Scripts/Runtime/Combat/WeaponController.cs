using System;
using Game.Data;
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

        AmmoModel ammo;
        float cooldownRemaining;
        bool wasReloading;

        public WeaponData Weapon { get; private set; }

        public event Action<int, int> AmmoChanged;
        public event Action<bool> ReloadingChanged;
        public event Action Fired;                                       // chaque coup parti : recul, muzzle flash, kick caméra
        public event Action<HitZone, bool, EnemyHealth> HitConfirmed;    // zone, killed, cible : hitmarker/kill feed/callouts
        public event Action<Vector3> TracerRequested;                    // point d'impact ou fin de portée (l'origine vient du viewmodel)

        public void Init(WeaponData weapon)
        {
            Weapon = weapon;
            ammo = new AmmoModel(weapon.mag, weapon.reload);
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }

        public void RequestReload()
        {
            if (Weapon != null) ammo.StartReload();
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
            if (AutoFireDecider.ShouldFireWithManual(auto, FireHeld, cooldownRemaining, ammoReady))
                Fire();

            if (ammo.InMag == 0 && !ammo.IsReloading)
                ammo.StartReload(); // l'événement ReloadingChanged partira au prochain Update via wasReloading
        }

        void Fire()
        {
            if (!ammo.TryConsume()) return;
            cooldownRemaining = Weapon.cooldown;
            int pellets = Mathf.Max(1, Weapon.pellets);
            for (int i = 0; i < pellets; i++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * Weapon.recoil * 0.01f;
                Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f + spread.x, 0.5f + spread.y, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, Weapon.range, hitMask))
                {
                    TracerRequested?.Invoke(hit.point);
                    var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemy != null)
                    {
                        var result = enemy.TakeHit(hit.point, Weapon.dmgMult);
                        if (result.valid) HitConfirmed?.Invoke(result.zone, result.killed, enemy);
                    }
                }
                else
                {
                    TracerRequested?.Invoke(ray.origin + ray.direction * Weapon.range);
                }
            }
            Fired?.Invoke();
            AmmoChanged?.Invoke(ammo.InMag, ammo.MagSize);
        }
    }
}
