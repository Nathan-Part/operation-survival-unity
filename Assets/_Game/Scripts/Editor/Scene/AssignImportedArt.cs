using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Runtime.Editor
{
    /// Branche les GLB importés dans Assets/_Game/Art sur les fiches de données.
    /// Idempotent ; n'écrase jamais un champ déjà assigné à la main.
    public static class AssignImportedArt
    {
        const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";
        const string LibraryPath = "Assets/_Game/Data/Config/PlaceholderLibrary.asset";

        static readonly (string id, string path)[] CharacterModels =
        {
            ("perso:blue-soldier", "Assets/_Game/Art/Characters/blue-soldier.glb"),
            ("perso:guard", "Assets/_Game/Art/Characters/guard.glb"),
            ("perso:robot", "Assets/_Game/Art/Characters/robot.glb"),
        };

        const string GenericEnemyPath = "Assets/_Game/Art/Enemies/enemie-generic.glb";

        [MenuItem("Tools/Game/Assign Imported Art")]
        public static void Assign()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null) { Debug.LogError($"AssignImportedArt: database introuvable a {DatabasePath}"); return; }

            // 1. PlaceholderLibrary (créée si absente)
            var library = AssetDatabase.LoadAssetAtPath<PlaceholderLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<PlaceholderLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            var genericEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(GenericEnemyPath);
            if (library.enemyPlaceholder == null && genericEnemy != null) { library.enemyPlaceholder = genericEnemy; EditorUtility.SetDirty(library); }
            var soldier = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModels[0].path);
            if (library.characterPlaceholder == null && soldier != null) { library.characterPlaceholder = soldier; EditorUtility.SetDirty(library); }
            if (db.placeholders == null) { db.placeholders = library; EditorUtility.SetDirty(db); }

            // 2. Modèles des personnages jouables
            int assigned = 0;
            foreach (var (id, path) in CharacterModels)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) { Debug.LogWarning($"AssignImportedArt: GLB manquant {path}"); continue; }
                foreach (var character in db.characters)
                {
                    if (character == null || character.id != id || character.modelPrefab != null) continue;
                    character.modelPrefab = model;
                    EditorUtility.SetDirty(character);
                    assigned++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"AssignImportedArt: OK - placeholders branches, {assigned} modele(s) de perso assigne(s).");
        }
    }
}
