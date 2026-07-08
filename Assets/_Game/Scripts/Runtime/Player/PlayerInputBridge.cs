using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime
{
    /// Actions Input System construites en code (décision D3) : clavier/souris en éditeur,
    /// et chemins Gamepad alimentés par les contrôles on-screen (joysticks virtuels) sur mobile.
    public class PlayerInputBridge : MonoBehaviour
    {
        public PlayerMotor motor;
        public PlayerLook look;
        public WeaponController weapon;
        public PlayerViewController viewController;
        public ConsumableThrower thrower;
        public LoadoutController loadout;
        public PlayerMelee melee;
        public WeaponPickup pickup;
        public HudPauseMenu pauseMenu;

        InputActionMap map;
        InputAction move, lookAction, jump, dash, reload, sprint, aim, fire, toggleView, fragKey, stickyKey;
        InputAction flashKey, smokeKey, molotovKey, empKey, medkitKey;
        InputAction mineKey, turretKey, airstrikeKey, carePackageKey, dogKey;
        InputAction burstModeKey, semiModeKey, meleeKey, pickupKey;
        InputAction pauseKey, muteKey;

        void Awake()
        {
            map = new InputActionMap("Gameplay");

            move = map.AddAction("Move");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            lookAction = map.AddAction("Look", binding: "<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jump = map.AddAction("Jump", binding: "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            // Touches identiques au jeu web (voir ROADMAP « Parité HUD & contrôles »)
            dash = map.AddAction("Dash", binding: "<Keyboard>/c");
            dash.AddBinding("<Gamepad>/buttonEast");

            reload = map.AddAction("Reload", binding: "<Keyboard>/f");
            reload.AddBinding("<Gamepad>/buttonWest");

            sprint = map.AddAction("Sprint", binding: "<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            sprint.AddBinding("<Keyboard>/r"); // le web accepte Shift OU R pour courir

            aim = map.AddAction("Aim", binding: "<Keyboard>/q"); // web: Q (A sur AZERTY) = viser
            aim.AddBinding("<Gamepad>/leftTrigger");
            aim.AddBinding("<Mouse>/rightButton");               // le web accepte aussi clic droit

            fire = map.AddAction("Fire", binding: "<Keyboard>/e"); // web: E = tirer (manuel)
            fire.AddBinding("<Gamepad>/rightTrigger");
            fire.AddBinding("<Mouse>/leftButton");                 // le web accepte aussi clic gauche

            toggleView = map.AddAction("ToggleView", binding: "<Keyboard>/v"); // web: V = vue 1re/3e

            // Modes de tir manuel (web #) : T = auto ↔ burst, P = auto ↔ semi
            burstModeKey = map.AddAction("ToggleBurst", binding: "<Keyboard>/t");
            semiModeKey = map.AddAction("ToggleSemi", binding: "<Keyboard>/p");

            // Lancer de consommables (web: B = frag, J = collante, N = flash, H = fumigène, K = molotov, O = EMP)
            fragKey = map.AddAction("Frag", binding: "<Keyboard>/b");
            stickyKey = map.AddAction("Sticky", binding: "<Keyboard>/j");
            flashKey = map.AddAction("Flashbang", binding: "<Keyboard>/n");
            smokeKey = map.AddAction("Smoke", binding: "<Keyboard>/h");
            molotovKey = map.AddAction("Molotov", binding: "<Keyboard>/k");
            empKey = map.AddAction("Emp", binding: "<Keyboard>/o");

            // Medkit (web: Z) : consommable instantané, pas un lancer.
            medkitKey = map.AddAction("Medkit", binding: "<Keyboard>/z");

            // Mêlée (web #45, X) : coup de crosse à courte portée.
            meleeKey = map.AddAction("Melee", binding: "<Keyboard>/x");

            // Ramassage d'arme sur cadavre (web #tranche23, G) : équipe l'arme du cadavre le plus proche.
            pickupKey = map.AddAction("Pickup", binding: "<Keyboard>/g");

            // Série de points loadout (web #156, Y/L/U/I/4 = mine/turret/airstrike/carepackage/dog)
            mineKey = map.AddAction("Mine", binding: "<Keyboard>/y");
            turretKey = map.AddAction("Turret", binding: "<Keyboard>/l");
            airstrikeKey = map.AddAction("Airstrike", binding: "<Keyboard>/u");
            carePackageKey = map.AddAction("CarePackage", binding: "<Keyboard>/i");
            dogKey = map.AddAction("Dog", binding: "<Keyboard>/digit4");

            // Menu pause (web: Échap) + mute global (web: M) — doivent marcher même à Time.timeScale=0
            // (l'Input System interroge les devices en temps réel, indépendamment du timeScale).
            pauseKey = map.AddAction("Pause", binding: "<Keyboard>/escape");
            muteKey = map.AddAction("Mute", binding: "<Keyboard>/m");
        }

        void OnEnable() { map.Enable(); }
        void OnDisable() { map.Disable(); }

        void Update()
        {
            // Échap/M avant le early-return : ces touches doivent marcher même sans moteur assigné (tests, écran de mort…)
            // et même à Time.timeScale=0 (l'Input System lit en temps non-scalé, cf. commentaire à la déclaration des actions).
            if (pauseKey.WasPressedThisFrame() && pauseMenu != null) pauseMenu.Toggle();
            if (muteKey.WasPressedThisFrame()) ToggleMute();

            if (motor == null) return;
            Vector2 m = move.ReadValue<Vector2>();
            // La source active du move : clavier → jamais de course auto ; stick/manette → règle D11.
            bool fromStick = move.activeControl != null && !(move.activeControl.device is Keyboard);
            bool wantsRun = RunDecision.WantsRun(sprint.IsPressed(), m.sqrMagnitude, fromStick);
            motor.SetMoveInput(m, wantsRun);
            if (look != null) look.AddLook(lookAction.ReadValue<Vector2>());
            if (jump.WasPressedThisFrame()) motor.RequestJump();
            if (dash.WasPressedThisFrame()) motor.RequestDash();
            if (reload.WasPressedThisFrame() && weapon != null) weapon.RequestReload();
            if (weapon != null)
            {
                weapon.FireHeld = fire.IsPressed();
                weapon.AimHeld = aim.IsPressed();
                if (burstModeKey.WasPressedThisFrame()) weapon.ToggleBurst();
                if (semiModeKey.WasPressedThisFrame()) weapon.ToggleSemi();
            }
            if (viewController != null)
            {
                viewController.AimHeld = aim.IsPressed();
                if (toggleView.WasPressedThisFrame()) viewController.ToggleView();
            }
            if (thrower != null)
            {
                if (fragKey.WasPressedThisFrame()) thrower.Throw("frag");
                if (stickyKey.WasPressedThisFrame()) thrower.Throw("sticky");
                if (flashKey.WasPressedThisFrame()) thrower.Throw("flashbang");
                if (smokeKey.WasPressedThisFrame()) thrower.Throw("smoke");
                if (molotovKey.WasPressedThisFrame()) thrower.Throw("molotov");
                if (empKey.WasPressedThisFrame()) thrower.Throw("emp");
                if (medkitKey.WasPressedThisFrame()) thrower.UseMedkit();
            }
            if (melee != null && meleeKey.WasPressedThisFrame()) melee.Strike();
            if (pickup != null && pickupKey.WasPressedThisFrame()) pickup.TryPickup();
            if (loadout != null)
            {
                if (mineKey.WasPressedThisFrame()) loadout.TryTrigger("mine");
                if (turretKey.WasPressedThisFrame()) loadout.TryTrigger("turret");
                if (airstrikeKey.WasPressedThisFrame()) loadout.TryTrigger("airstrike");
                if (carePackageKey.WasPressedThisFrame()) loadout.TryTrigger("carepackage");
                if (dogKey.WasPressedThisFrame()) loadout.TryTrigger("dog");
            }
        }

        /// Mute global provisoire (T25) : bascule le volume de l'AudioListener.
        /// Sera remplacé par AudioKit.ToggleMute() au T26 (vrai mixer/sons).
        static void ToggleMute()
        {
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
        }
    }
}
