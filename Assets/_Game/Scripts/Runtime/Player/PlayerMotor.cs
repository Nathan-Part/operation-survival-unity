using System;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        CharacterController controller;
        MovementConfig config;
        StaminaModel stamina;
        DashModel dash;
        float speedMult = 1f;

        Vector2 moveInput;
        bool wantsRun;
        bool jumpRequested;
        float verticalVelocity;
        Vector3 dashDirection;
        bool isAirborne;
        float highestY;

        public event Action<float> FellFrom;

        public bool IsRunning { get; private set; }
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

        /// Multiplicateur de vitesse externe (stun/gel — web #121/#146), posé par PlayerStatusEffects.
        [NonSerialized] public float ExternalSpeedMult = 1f;

        /// Power-ups (web #34+, Lot 5) : speed_boost ×2 (MoveMult), branché par SliceBootstrap.
        public PowerUpRuntime powerups;
        public float StaminaCurrent => stamina != null ? stamina.Current : 0f;
        public float StaminaMax => config != null ? config.stamina.max : 0f;

        public void Init(MovementConfig config, float speedMult)
        {
            this.config = config;
            this.speedMult = speedMult;
            stamina = new StaminaModel(config.stamina);
            dash = new DashModel(config.dash);
            controller = GetComponent<CharacterController>();
            controller.stepOffset = config.stepUpMax;
        }

        public void SetMoveInput(Vector2 input, bool run)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
            wantsRun = run;
        }

        public void RequestJump() { jumpRequested = true; }

        public void RequestDash()
        {
            if (config == null || !dash.TryStart(Time.time)) return;
            Vector3 dir = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
            dashDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        }

        void Update()
        {
            if (config == null) return;
            float dt = Time.deltaTime;

            bool moving = moveInput.sqrMagnitude > 0.01f;
            IsRunning = stamina.Tick(dt, wantsRun && moving);
            float speed = (IsRunning ? config.runSpeed : config.walkSpeed) * speedMult * ExternalSpeedMult
                * (powerups != null ? powerups.MoveMult : 1f); // web: speed_boost ×2
            Vector3 horizontal = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * speed;
            if (dash.IsActive(Time.time)) horizontal = dashDirection * dash.Speed;

            if (controller.isGrounded)
            {
                if (isAirborne)
                {
                    float fallHeight = highestY - transform.position.y;
                    if (fallHeight > 0f) FellFrom?.Invoke(fallHeight);
                    isAirborne = false;
                }
                verticalVelocity = -2f; // colle au sol (stepDownSnap approximé par le CharacterController)
                if (jumpRequested) verticalVelocity = config.jumpSpeed;
            }
            else
            {
                if (!isAirborne) { isAirborne = true; highestY = transform.position.y; }
                highestY = Mathf.Max(highestY, transform.position.y);
                verticalVelocity -= config.gravity * dt;
            }
            jumpRequested = false;

            Vector3 velocity = horizontal;
            velocity.y = verticalVelocity;
            controller.Move(velocity * dt);
        }
    }
}
