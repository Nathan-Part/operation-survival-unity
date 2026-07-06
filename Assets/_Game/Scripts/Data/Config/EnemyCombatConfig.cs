using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class EnemyBaseStats
    {
        public float hp; public float walkSpeed; public float runSpeed; public float radius;
        public float turnSpeed; public float shootRange; public float fireCooldown; public float accuracy;
        public float damage; public float burstGap; public float detectRadius; public float loseRadius;
        public float loseTime; public float meleeRange; public float meleeDamage; public float meleeCooldown;
    }

    [Serializable]
    public class HitZoneSettings
    {
        public float headAboveY;
        public int headDamage;
        public int torsoDamage;
        public float legsBelowY;
        public int legsDamage;
    }

    [CreateAssetMenu(menuName = "Game/Config/Enemy Combat Config", fileName = "EnemyCombatConfig")]
    public class EnemyCombatConfig : ScriptableObject
    {
        public EnemyBaseStats baseStats;
        public HitZoneSettings hitZones;
        public int vipMoneyMult;
        public int vipMinWave;
        public string[] vipExcludedTypes = Array.Empty<string>();
        public int bossKillBonus;
    }
}
