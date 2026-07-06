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
        const string AnimationLibraryPath = "Assets/_Game/Data/Config/AnimationLibrary.asset";
        const string DefaultWeaponModelPath = "Assets/_Game/Art/Weapons/ar-15.glb";

        static readonly (string role, string path)[] AnimationClips =
        {
            ("idle", "Assets/_Game/Art/Animations/anim-idle.glb"),
            ("walk", "Assets/_Game/Art/Animations/anim-walk.glb"),
            ("run", "Assets/_Game/Art/Animations/anim-run.glb"),
            ("attack", "Assets/_Game/Art/Animations/anim-attack.glb"),
            ("dead", "Assets/_Game/Art/Animations/anim-dead.glb"),
        };

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

            // 3. AnimationLibrary (créée si absente, clips assignés si champs vides)
            var animLib = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(AnimationLibraryPath);
            if (animLib == null)
            {
                animLib = ScriptableObject.CreateInstance<AnimationLibrary>();
                AssetDatabase.CreateAsset(animLib, AnimationLibraryPath);
            }
            int clips = 0;
            foreach (var (role, path) in AnimationClips)
            {
                var clip = FirstClipAt(path);
                if (clip == null) { Debug.LogWarning($"AssignImportedArt: clip manquant {path}"); continue; }
                switch (role)
                {
                    case "idle": if (animLib.idle == null) { animLib.idle = clip; clips++; } break;
                    case "walk": if (animLib.walk == null) { animLib.walk = clip; clips++; } break;
                    case "run": if (animLib.run == null) { animLib.run = clip; clips++; } break;
                    case "attack": if (animLib.attack == null) { animLib.attack = clip; clips++; } break;
                    case "dead": if (animLib.dead == null) { animLib.dead = clip; clips++; } break;
                }
            }
            if (clips > 0) EditorUtility.SetDirty(animLib);
            if (db.animationLibrary == null) { db.animationLibrary = animLib; EditorUtility.SetDirty(db); }

            // 4. Modèle de l'arme de départ (Viper-AR / AR-15)
            var weaponModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultWeaponModelPath);
            if (weaponModel == null) Debug.LogWarning($"AssignImportedArt: GLB manquant {DefaultWeaponModelPath}");
            else foreach (var weapon in db.weapons)
            {
                if (weapon == null || weapon.id != "default" || weapon.modelPrefab != null) continue;
                weapon.modelPrefab = weaponModel;
                EditorUtility.SetDirty(weapon);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"AssignImportedArt: OK - placeholders branches, {assigned} modele(s) de perso, {clips} clip(s) d'animation assigne(s).");
        }

        static AnimationClip FirstClipAt(string assetPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (asset is AnimationClip clip)
                    return clip;
            return null;
        }
    }
}
