using UnityEngine;

namespace Game.Runtime
{
    /// Effets de statut du joueur (stun du stunner #121, gel des zones cryo #146) :
    /// applique le multiplicateur de vitesse au PlayerMotor.
    public class PlayerStatusEffects : MonoBehaviour
    {
        public PlayerMotor motor;

        readonly StatusEffectsModel model = new StatusEffectsModel();

        public void ApplyStun() => model.ApplyStun(Time.time);

        public void EnterFrost() => model.EnterFrost();
        public void ExitFrost() => model.ExitFrost();

        void Update()
        {
            if (motor != null) motor.ExternalSpeedMult = model.SpeedMult(Time.time);
        }
    }
}
