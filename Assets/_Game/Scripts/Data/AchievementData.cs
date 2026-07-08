using UnityEngine;

namespace Game.Data
{
    /// Succès (web `ACHIEVEMENTS`, main.js:1658-1723). conditionType + target
    /// pilotent AchievementRules.IsUnlocked — pas de logique en dur par succès.
    [CreateAssetMenu(menuName = "Game/Achievement Data", fileName = "AchievementData")]
    public class AchievementData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string desc;
        public string conditionType;
        public int target;
    }
}
