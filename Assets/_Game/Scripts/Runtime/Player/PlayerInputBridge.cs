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

        InputActionMap map;
        InputAction move, lookAction, jump, dash, reload, sprint, aim, fire, toggleView, fragKey, stickyKey;

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

            // Lancer de consommables (web: B = frag, J = collante ; N/H/K/O au T8)
            fragKey = map.AddAction("Frag", binding: "<Keyboard>/b");
            stickyKey = map.AddAction("Sticky", binding: "<Keyboard>/j");
        }

        void OnEnable() { map.Enable(); }
        void OnDisable() { map.Disable(); }

        void Update()
        {
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
            }
        }
    }
}
