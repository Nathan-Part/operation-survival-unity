using Game.Data;

namespace Game.Runtime
{
    /// Arme lâchée par un cadavre (web weaponIdForType : `ENEMY_TYPES[type].weapon || 'default'`,
    /// main.js:4118). La table type→arme vit déjà dans `unity-spec/enemies.json` (champ "weapon" par
    /// type) et est importée dans `EnemyData.weapon` — pas de table dupliquée ici, juste le même repli
    /// que le web quand le type n'a pas d'arme assignée (ex. types 100% mêlée/support).
    public static class EnemyWeaponDrop
    {
        public static string ForEnemy(EnemyData data) =>
            data != null && data.weapon != null ? data.weapon.id : "default";
    }
}
