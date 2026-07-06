using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Dance Data", fileName = "DanceData")]
    public class DanceData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string clipName;
        public int price;
        public Rarity rarity;

        public AnimationClip animationClip;
    }
}
