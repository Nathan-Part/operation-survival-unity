using System;
using Game.Data;

namespace Game.Runtime
{
    /// Spec web (player.json -> movement.stamina). Une fois à 0, il faut remonter
    /// au-dessus de minToRun pour pouvoir recourir.
    public class StaminaModel
    {
        readonly StaminaSettings settings;
        bool exhausted;

        public float Current { get; private set; }

        public StaminaModel(StaminaSettings settings)
        {
            this.settings = settings;
            Current = settings.max;
        }

        public bool Tick(float dt, bool wantsRun)
        {
            if (exhausted && Current >= settings.minToRun) exhausted = false;
            bool running = wantsRun && !exhausted && Current > 0f;
            if (running)
            {
                Current = Math.Max(0f, Current - settings.drainPerSec * dt);
                if (Current <= 0f) { exhausted = true; running = false; }
            }
            else
            {
                Current = Math.Min(settings.max, Current + settings.regenPerSec * dt);
            }
            return running;
        }
    }
}
