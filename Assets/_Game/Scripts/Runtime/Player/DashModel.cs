using Game.Data;

namespace Game.Runtime
{
    /// Dash web : ruée rectiligne de `distance` mètres à `speed` m/s, cooldown depuis le début du dash.
    public class DashModel
    {
        readonly DashSettings settings;
        float lastStart = float.NegativeInfinity;

        public DashModel(DashSettings settings) { this.settings = settings; }

        public float Duration => settings.speed > 0f ? settings.distance / settings.speed : 0f;
        public float Speed => settings.speed;

        public bool IsActive(float now) => now < lastStart + Duration;

        public bool TryStart(float now)
        {
            if (now < lastStart + settings.cooldown) return false;
            lastStart = now;
            return true;
        }
    }
}
