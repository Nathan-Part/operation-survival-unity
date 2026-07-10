using UnityEngine;

namespace Game.Data
{
    public enum AnimRole { Idle, Walk, Run, Attack, Dead, Lobby }

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
        /// Marche calme du lobby (web : Casual_Walk, main.js:497-536) — le clip `idle` (Alert) est
        /// une posture de combat, illisible comme pose d'accueil (retour Nathan 2026-07-10).
        public AnimationClip lobby;

        public AnimationClip Get(AnimRole role)
        {
            switch (role)
            {
                case AnimRole.Idle: return idle;
                case AnimRole.Walk: return walk;
                case AnimRole.Run: return run;
                case AnimRole.Attack: return attack;
                case AnimRole.Dead: return dead;
                case AnimRole.Lobby: return lobby;
                default: return null;
            }
        }
    }
}
