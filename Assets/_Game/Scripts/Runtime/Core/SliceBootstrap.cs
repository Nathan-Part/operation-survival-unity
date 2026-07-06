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

            CharacterData character = null;
            foreach (var c in db.characters)
                if (c != null && c.id == "perso:blue-soldier") { character = c; break; }
            if (character == null && db.characters.Length > 0) character = db.characters[0];

            WeaponData weaponData = null;
            foreach (var w in db.weapons)
                if (w != null && w.id == "default") { weaponData = w; break; }
            if (weaponData == null && db.weapons.Length > 0) weaponData = db.weapons[0];

            if (character == null || weaponData == null || db.movementConfig == null)
            {
                Debug.LogError("SliceBootstrap: database incomplete (character/weapon/movementConfig).");
                return;
            }

            motor.Init(db.movementConfig, StatFormulas.SpeedMult(character.baseSpeed, 0, 0f));
            health.Init(character, db.movementConfig);
            weapon.Init(weaponData);
            motor.FellFrom += health.ApplyFallDamage;

            // Effets de statut (stun du stunner, gel des zones cryo)
            var status = motor.gameObject.AddComponent<PlayerStatusEffects>();
            status.motor = motor;

            // Avatar 3ᵉ personne (modèle du perso + anims + arme en main)
            var avatar = motor.gameObject.AddComponent<PlayerAvatar>();
            avatar.Init(context.ResolveCharacterModel(character), db.animationLibrary, motor, weaponData);

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

            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<HudController>();
            hud.Init(health, weapon, waveManager, motor, motor.transform, weapon.aimCamera, view);
            view.hud = hud;

            TouchControlsRoot.CreateIfNeeded(look);

            waveManager.Begin();
        }
    }
}
