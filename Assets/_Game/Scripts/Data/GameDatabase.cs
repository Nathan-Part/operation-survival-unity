using System;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public WeaponData[] weapons = Array.Empty<WeaponData>();
        public EnemyData[] enemies = Array.Empty<EnemyData>();
        public CharacterData[] characters = Array.Empty<CharacterData>();
        public PowerUpData[] powerUps = Array.Empty<PowerUpData>();
        public AccessoryData[] accessories = Array.Empty<AccessoryData>();
        public BoostData[] boosts = Array.Empty<BoostData>();
        public ConsumableData[] consumables = Array.Empty<ConsumableData>();
        public DanceData[] dances = Array.Empty<DanceData>();

        public MovementConfig movementConfig;
        public EnemyCombatConfig enemyCombatConfig;
        public WaveConfig waveConfig;
        public EconomyConfig economyConfig;
        public UpgradeConfig upgradeConfig;
        public PowerUpConfig powerUpConfig;
        public PlaceholderLibrary placeholders;
    }
}
