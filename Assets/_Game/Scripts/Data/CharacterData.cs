using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class CharacterSkill
    {
        public string icon;
        public string name;
        public string desc;
    }

    [CreateAssetMenu(menuName = "Game/Character Data", fileName = "CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string displayName;
        public int price;
        public Rarity rarity;
        public string desc;
        public float baseHealth;
        public float baseRegen;
        public float baseSpeed;
        public CharacterSkill[] skills = Array.Empty<CharacterSkill>();

        public GameObject modelPrefab;
        public Sprite icon;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetCharacterPlaceholder();

        public Sprite ResolveIcon(PlaceholderLibrary placeholders) =>
            icon != null ? icon : placeholders.GetGenericIcon();
    }
}
