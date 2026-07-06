using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Config/Wave Config", fileName = "WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public int maxEnemiesPerWave;
        public float intermissionSeconds;
        public int bossEvery;
    }
}
