using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// Câblage runtime de la scène Slice : perso par défaut, arme de départ,
    /// dégâts de chute, HUD, contrôles tactiles, puis lancement de la boucle de vagues.
    public class SliceBootstrap : MonoBehaviour
    {
        public GameContext context;
        public PlayerMotor motor;
        public PlayerLook look;
        public PlayerHealth health;
        public WeaponController weapon;
        public WaveManager waveManager;

        void Start()
        {
            if (context == null || context.database == null)
            {
                Debug.LogError("SliceBootstrap: missing GameContext or database.");
                return;
            }
            var db = context.database;

            // Save chargée d'abord : perso/arme sélectionnés + niveaux d'upgrade en dépendent
            var meta = gameObject.AddComponent<MetaProgress>();
            meta.LoadOrCreate();
            var save = meta.Data;

            string wantedCharacter = string.IsNullOrEmpty(save.selectedCharacterId) ? "perso:blue-soldier" : save.selectedCharacterId;
            CharacterData character = null;
            foreach (var c in db.characters)
                if (c != null && c.id == wantedCharacter) { character = c; break; }
            if (character == null && db.characters.Length > 0) character = db.characters[0];

            string wantedWeapon = string.IsNullOrEmpty(save.selectedWeapon) ? "default" : save.selectedWeapon;
            WeaponData weaponData = null;
            foreach (var w in db.weapons)
                if (w != null && w.id == wantedWeapon) { weaponData = w; break; }
            if (weaponData == null && db.weapons.Length > 0) weaponData = db.weapons[0];

            if (character == null || weaponData == null || db.movementConfig == null)
            {
                Debug.LogError("SliceBootstrap: database incomplete (character/weapon/movementConfig).");
                return;
            }

            int UpgLevel(string key) => save.playerUpgrades.TryGetValue(key, out int lvl) ? lvl : 0;
            motor.Init(db.movementConfig, StatFormulas.SpeedMult(character.baseSpeed, UpgLevel("speed"), 0f));
            health.Init(character, db.movementConfig, UpgLevel("health"), UpgLevel("regen"));
            health.shield = new ShieldModel();
            weapon.Init(weaponData, ShopRules.WeaponUpgradeLevel(save, weaponData.id));
            motor.FellFrom += health.ApplyFallDamage;
            if (look != null) look.sensitivity = 0.12f * save.settings.sensitivity; // réglage web × base slice

            // Effets de statut (stun du stunner, gel des zones cryo)
            var status = motor.gameObject.AddComponent<PlayerStatusEffects>();
            status.motor = motor;

            // Progression persistante : argent/XP/records alimentés par les vagues
            waveManager.EnemyKilled += (enemy, money) =>
                meta.OnKill(enemy != null && enemy.LastHitHead, enemy != null && enemy.IsVip, money);
            waveManager.WaveCleared += _ => meta.OnWaveCleared();
            health.Died += () => meta.OnRunEnded(waveManager.CurrentWave, waveManager.Money, waveManager.BestComboThisRun);

            // Avatar 3ᵉ personne (modèle du perso + anims + arme en main)
            var avatar = motor.gameObject.AddComponent<PlayerAvatar>();
            avatar.Init(context.ResolveCharacterModel(character), db.animationLibrary, motor,
                weaponData.ResolveModel(db.placeholders), weaponData.modelScale);

            // Feedback de tir : tracers partagés + viewmodel 1ʳᵉ personne
            var tracers = new GameObject("Tracers").AddComponent<TracerPool>();
            ViewmodelController viewmodel = null;
            if (weapon.aimCamera != null)
            {
                viewmodel = new GameObject("Viewmodel").AddComponent<ViewmodelController>();
                viewmodel.Init(weaponData, weapon.aimCamera, weapon, look, tracers);
            }

            // Vue 1ʳᵉ/3ᵉ personne (V) + visée (Q) + séquence de mort
            var view = motor.gameObject.AddComponent<PlayerViewController>();
            view.playerCamera = weapon.aimCamera;
            view.cameraPivot = look != null ? look.cameraPivot : null;
            view.look = look;
            view.avatar = avatar;
            view.viewmodel = viewmodel;
            view.weapon = weapon;
            view.health = health;
            view.motor = motor;
            view.inputBridge = motor.GetComponent<PlayerInputBridge>();
            if (view.inputBridge != null) view.inputBridge.viewController = view;
            if (viewmodel != null) viewmodel.BindView(view);

            // Lancer/usage de consommables (frag/collante/molotov/emp/flashbang/fumigène/medkit)
            var thrower = motor.gameObject.AddComponent<ConsumableThrower>();
            thrower.Init(db, meta, weapon.aimCamera, waveManager, health);
            if (view.inputBridge != null) view.inputBridge.thrower = thrower;

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Init(health, weapon, waveManager, motor, motor.transform, weapon.aimCamera, view, meta, thrower);
            view.hud = hud;

            if (save.settings.defaultView == "third") view.SetMode(ViewMode.Third); // réglage « vue » du web

            TouchControlsRoot.CreateIfNeeded(look);

            waveManager.Begin();
        }
    }
}
