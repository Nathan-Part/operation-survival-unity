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
            weapon.health = health;
            weapon.motor = motor;
            weapon.isLmg = weaponData.id == "lmg";
            weapon.LongShot += (money, xp) => { waveManager.AddBonusMoney(money); meta.Data.xp += xp; };
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

            // Mêlée (web #45, X) : coup de crosse à courte portée.
            var melee = motor.gameObject.AddComponent<PlayerMelee>();
            melee.Init(weapon.aimCamera.transform, waveManager);
            if (view.inputBridge != null) view.inputBridge.melee = melee;

            // Ramassage d'arme sur cadavre (web #tranche23, G, 2.5 m) : loot run-based, n'altère pas la save.
            var pickup = motor.gameObject.AddComponent<WeaponPickup>();
            pickup.Init(motor.transform, weapon, db);
            if (view.inputBridge != null) view.inputBridge.pickup = pickup;

            // Power-ups (web #34+, Lot 5) : cadre + drops (12%) + effets prioritaires, composant unique sur le joueur.
            var powerups = motor.gameObject.AddComponent<PowerUpRuntime>();
            powerups.health = health;
            powerups.waves = waveManager;
            weapon.powerups = powerups;
            motor.powerups = powerups;

            // Killstreaks (web #58) : rechargement instantané / bouclier / argent ×2
            var killstreak = motor.gameObject.AddComponent<KillstreakController>();
            killstreak.Init(waveManager, weapon, health);
            killstreak.powerups = powerups; // compose avec kevlar sur PlayerHealth.incomingMult (pas d'écrasement)

            // Série de points loadout (web #156) : mine/turret/airstrike/carepackage/dog, seuils EconomyConfig
            var loadout = motor.gameObject.AddComponent<LoadoutController>();
            var loadoutThresholds = new System.Collections.Generic.Dictionary<string, int>
            {
                { "turret", db.economyConfig.loadoutThresholds.turret },
                { "mine", db.economyConfig.loadoutThresholds.mine },
                { "airstrike", db.economyConfig.loadoutThresholds.airstrike },
                { "carepackage", db.economyConfig.loadoutThresholds.carepackage },
                { "dog", db.economyConfig.loadoutThresholds.dog },
            };
            loadout.Init(save.selectedLoadout, loadoutThresholds, waveManager);
            loadout.SetTrigger("mine", () => ProximityMine.Place(GroundInFront(motor, 1.5f), waveManager));
            loadout.SetTrigger("turret", () => Turret.Deploy(GroundInFront(motor, 1.5f), waveManager));
            loadout.SetTrigger("airstrike", () => Airstrike.CallOnAim(this, weapon.aimCamera, waveManager));
            loadout.SetTrigger("carepackage", () => CarePackage.Spawn(GroundInFront(motor, 2f), motor.transform, meta, health, Time.frameCount));
            loadout.SetTrigger("dog", () => PetController.Summon(GroundInFront(motor, 1.2f), motor.transform, waveManager, null)); // modèle pets/chien2 à copier plus tard (T15)
            if (view.inputBridge != null) view.inputBridge.loadout = loadout;

            // Caisse de ravitaillement auto (web #156) : toutes les 4 vagues nettoyées
            waveManager.CarePackageDue += () => CarePackage.Spawn(GroundInFront(motor, 2f), motor.transform, meta, health, Time.frameCount);

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Init(health, weapon, waveManager, motor, motor.transform, weapon.aimCamera, view, meta, thrower, killstreak, loadout, powerups);
            view.hud = hud;

            if (save.settings.defaultView == "third") view.SetMode(ViewMode.Third); // réglage « vue » du web

            TouchControlsRoot.CreateIfNeeded(look);

            waveManager.Begin();
        }

        /// Projette un point devant le joueur sur le sol (raycast vers le bas) pour poser un gadget (T13) ;
        /// repli sur la position du joueur si le raycast ne touche rien.
        static Vector3 GroundInFront(PlayerMotor m, float dist)
        {
            Vector3 origin = m.transform.position + m.transform.forward * dist + Vector3.up * 2f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 10f)) return hit.point;
            return m.transform.position;
        }
    }
}
