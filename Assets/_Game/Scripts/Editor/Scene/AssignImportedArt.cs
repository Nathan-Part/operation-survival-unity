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

        // Task 13 (boutique Accessoires) : PNG copiés en Task 1 (Assets/_Game/Art/UI/Accessories/),
        // jamais branchés sur les ScriptableObjects AccessoryData jusqu'ici — mapping vérifié contre
        // le rapport de Task 1 (source web exacte par id, voir task-1-report.md).
        static readonly (string id, string path)[] AccessoryImages =
        {
            ("helm:insect", "Assets/_Game/Art/UI/Accessories/insect-viber-mask.png"),
            ("helm:skull-soldier", "Assets/_Game/Art/UI/Accessories/squelette-soldier.png"),
            ("helm:skull-ram", "Assets/_Game/Art/UI/Accessories/squelette-belier.png"),
            ("cost:insect", "Assets/_Game/Art/UI/Accessories/insect-viber-suit.png"),
            ("cost:bone", "Assets/_Game/Art/UI/Accessories/skeleton-ram-suit.png"),
        };
        // ar-15.glb mesuré ~2,00 m de long tel qu'exporté (mesh brut, aucune correction d'échelle
        // dans le GLB) — un AR-15 réel fait ~0,85-1,00 m : défaut d'export Blender (même famille de
        // problème que d'autres GLB du projet). Corrigé ici en attendant un ré-export à la bonne
        // échelle depuis Blender.
        const float DefaultWeaponModelScale = 0.45f;

        static readonly (string role, string path)[] AnimationClips =
        {
            ("idle", "Assets/_Game/Art/Animations/anim-idle.glb"),
            ("walk", "Assets/_Game/Art/Animations/anim-walk.glb"),
            ("run", "Assets/_Game/Art/Animations/anim-run.glb"),
            ("attack", "Assets/_Game/Art/Animations/anim-attack.glb"),
            ("dead", "Assets/_Game/Art/Animations/anim-dead.glb"),
            ("lobby", "Assets/_Game/Art/Animations/anim-lobby.glb"), // Casual_Walk web (marche calme du lobby)
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
                var raw = FirstClipAt(path);
                if (raw == null) { Debug.LogWarning($"AssignImportedArt: clip manquant {path}"); continue; }
                // Phase 5 bis (point 6, torse rétréci) : les clips GLB portent des curves de POSITION
                // locales calibrées sur le rig source — appliquées telles quelles à un perso aux
                // proportions différentes, elles écrasent ses longueurs d'os (torse compressé).
                // On assigne une copie filtrée : rotations + position des hanches uniquement.
                var clip = FilteredClipFor(role, raw);
                switch (role)
                {
                    case "idle": if (animLib.idle == null || animLib.idle == raw) { animLib.idle = clip; clips++; } break;
                    case "walk": if (animLib.walk == null || animLib.walk == raw) { animLib.walk = clip; clips++; } break;
                    case "run": if (animLib.run == null || animLib.run == raw) { animLib.run = clip; clips++; } break;
                    case "attack": if (animLib.attack == null || animLib.attack == raw) { animLib.attack = clip; clips++; } break;
                    case "dead": if (animLib.dead == null || animLib.dead == raw) { animLib.dead = clip; clips++; } break;
                    case "lobby": if (animLib.lobby == null || animLib.lobby == raw) { animLib.lobby = clip; clips++; } break;
                }
            }
            if (clips > 0) EditorUtility.SetDirty(animLib);
            if (db.animationLibrary == null) { db.animationLibrary = animLib; EditorUtility.SetDirty(db); }

            // 4. Modèle de l'arme de départ (Viper-AR / AR-15)
            var weaponModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultWeaponModelPath);
            if (weaponModel == null) Debug.LogWarning($"AssignImportedArt: GLB manquant {DefaultWeaponModelPath}");
            else foreach (var weapon in db.weapons)
            {
                if (weapon == null || weapon.id != "default") continue;
                if (weapon.modelPrefab == null) { weapon.modelPrefab = weaponModel; EditorUtility.SetDirty(weapon); }
                // Correctif d'échelle connu pour ce GLB précis ; ne touche pas si Nathan a déjà réglé la valeur à la main.
                if (Mathf.Approximately(weapon.modelScale, 1f)) { weapon.modelScale = DefaultWeaponModelScale; EditorUtility.SetDirty(weapon); }
            }

            // 5. Images des accessoires (Task 13 : essai en direct boutique) — idempotent, ne touche
            // jamais un `image` déjà assigné à la main.
            int accessoryImages = 0;
            foreach (var (id, path) in AccessoryImages)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) { Debug.LogWarning($"AssignImportedArt: sprite manquant {path}"); continue; }
                foreach (var accessory in db.accessories)
                {
                    if (accessory == null || accessory.id != id || accessory.image != null) continue;
                    accessory.image = sprite;
                    EditorUtility.SetDirty(accessory);
                    accessoryImages++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"AssignImportedArt: OK - placeholders branches, {assigned} modele(s) de perso, {clips} clip(s) d'animation assigne(s), {accessoryImages} image(s) d'accessoire assignee(s).");
        }

        static AnimationClip FirstClipAt(string assetPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (asset is AnimationClip clip)
                    return clip;
            return null;
        }

        /// Copie du clip sans les curves de position/échelle des os (les hanches gardent leur
        /// position : c'est le balancement de la marche). Les longueurs d'os restent donc celles
        /// du perso qui joue le clip — même rig partagé, proportions propres (point 6).
        static AnimationClip FilteredClipFor(string role, AnimationClip source)
        {
            const string dir = "Assets/_Game/Art/Animations/Filtered";
            string path = $"{dir}/{role}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) return existing; // idempotent : régénérer = supprimer le .anim d'abord

            System.IO.Directory.CreateDirectory(dir);
            var copy = new AnimationClip { name = $"{role} (filtered)", frameRate = source.frameRate, wrapMode = source.wrapMode };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                // Rotations pures : les positions des hanches du rig source (hauteur/bob) déplacent
                // un perso d'un autre gabarit (flottement) — retirées aussi, le perso reste ancré
                // à sa bind pose. Réévaluer si un vrai retarget humanoïde arrive un jour.
                bool isPosition = binding.propertyName.StartsWith("m_LocalPosition");
                bool isScale = binding.propertyName.StartsWith("m_LocalScale");
                if (isScale || isPosition) continue;
                AnimationUtility.SetEditorCurve(copy, binding, AnimationUtility.GetEditorCurve(source, binding));
            }
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }
    }
}
