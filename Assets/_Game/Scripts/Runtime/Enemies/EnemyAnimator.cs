using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Traduit l'état FSM en clip joué (parité web setEnemyLoco : fondu 0.2 s,
    /// mort 0.18 s puis pose figée à la fin du clip — « frozen pose » du web).
    public class EnemyAnimator : MonoBehaviour
    {
        EnemyController controller;
        AnimationDriver driver;
        AnimRole current = AnimRole.Idle;
        bool started;
        bool frozen;

        const float LocoFade = 0.2f;  // web: setEnemyLoco crossFadeTo 0.2
        const float DeathFade = 0.18f; // web: killEnemy crossFadeTo 0.18

        public void Init(EnemyController controller, AnimationDriver driver)
        {
            this.controller = controller;
            this.driver = driver;
        }

        void Update()
        {
            if (controller == null || driver == null || frozen) return;

            var role = EnemyLocoMap.RoleFor(controller.State);
            if (!started || role != current)
            {
                driver.Play(role, role == AnimRole.Dead ? DeathFade : LocoFade, loop: role != AnimRole.Dead);
                current = role;
                started = true;
            }

            if (current == AnimRole.Dead)
            {
                float length = driver.ClipLength(AnimRole.Dead);
                if (length <= 0f || driver.TimeIn(AnimRole.Dead) >= length)
                {
                    driver.FreezePose();
                    frozen = true;
                }
            }
        }
    }
}
