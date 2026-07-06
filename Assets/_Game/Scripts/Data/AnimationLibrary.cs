using UnityEngine;

namespace Game.Data
{
    public enum AnimRole { Idle, Walk, Run, Attack, Dead }

    /// Clips d'animation partagés (rig Meshy biped, compatibles persos + ennemis).
    /// La logique n'exige jamais un clip : champ null → pas d'animation, rien ne plante.
    [CreateAssetMenu(menuName = "Game/Config/Animation Library", fileName = "AnimationLibrary")]
    public class AnimationLibrary : ScriptableObject
    {
        public AnimationClip idle;
        public AnimationClip walk;
        public AnimationClip run;
        public AnimationClip attack;
        public AnimationClip dead;

        public AnimationClip Get(AnimRole role)
        {
            switch (role)
            {
                case AnimRole.Idle: return idle;
                case AnimRole.Walk: return walk;
                case AnimRole.Run: return run;
                case AnimRole.Attack: return attack;
                case AnimRole.Dead: return dead;
                default: return null;
            }
        }
    }
}
