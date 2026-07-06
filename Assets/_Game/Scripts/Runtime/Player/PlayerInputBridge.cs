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

        InputActionMap map;
        InputAction move, lookAction, jump, dash, reload, sprint;

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
        }

        void OnEnable() { map.Enable(); }
        void OnDisable() { map.Disable(); }

        void Update()
        {
            if (motor == null) return;
            Vector2 m = move.ReadValue<Vector2>();
            bool wantsRun = sprint.IsPressed() || m.sqrMagnitude >= 0.81f; // stick à fond = courir (D11)
            motor.SetMoveInput(m, wantsRun);
            if (look != null) look.AddLook(lookAction.ReadValue<Vector2>());
            if (jump.WasPressedThisFrame()) motor.RequestJump();
            if (dash.WasPressedThisFrame()) motor.RequestDash();
            if (reload.WasPressedThisFrame() && weapon != null) weapon.RequestReload();
        }
    }
}
