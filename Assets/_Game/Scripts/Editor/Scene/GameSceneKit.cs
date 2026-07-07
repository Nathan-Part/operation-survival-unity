using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Runtime.Editor
{
    /// Câblage gameplay commun aux scènes générées (Slice, Utopia) :
    /// contexte + joueur complet + vagues + bootstrap. Les builders ne gèrent
    /// que le décor (arène greybox / map GLB) et fournissent positions de spawn.
    public static class GameSceneKit
    {
        public const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";

        public static void BuildGameplay(Vector3 playerPosition, Vector3[] spawnPositions)
        {
            var contextGo = new GameObject("GameContext");
            var context = contextGo.AddComponent<GameContext>();
            context.database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (context.database == null)
                Debug.LogError($"GameSceneKit: GameDatabase introuvable a {DatabasePath}");

            var player = new GameObject("Player");
            player.transform.position = playerPosition;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            cc.stepOffset = 0.4f; // franchir des marches jusqu'à 40 cm (escaliers utopia)
            cc.slopeLimit = 55f;  // pentes/rampes raides

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var motor = player.AddComponent<PlayerMotor>();
            var look = player.AddComponent<PlayerLook>();
            look.cameraPivot = pivot.transform;
            var health = player.AddComponent<PlayerHealth>();
            var weapon = player.AddComponent<WeaponController>();
            weapon.aimCamera = cam;
            var bridge = player.AddComponent<PlayerInputBridge>();
            bridge.motor = motor;
            bridge.look = look;
            bridge.weapon = weapon;

            var wavesGo = new GameObject("Waves");
            var spawner = wavesGo.AddComponent<EnemySpawner>();
            spawner.context = context;
            spawner.player = player.transform;
            var waveManager = wavesGo.AddComponent<WaveManager>();
            waveManager.context = context;
            waveManager.spawner = spawner;
            var points = new Transform[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var p = new GameObject($"SpawnPoint{i}");
                p.transform.SetParent(wavesGo.transform);
                p.transform.position = spawnPositions[i];
                points[i] = p.transform;
            }
            waveManager.spawnPoints = points;

            var bootstrapGo = new GameObject("SliceBootstrap");
            var bootstrap = bootstrapGo.AddComponent<SliceBootstrap>();
            bootstrap.context = context;
            bootstrap.motor = motor;
            bootstrap.look = look;
            bootstrap.health = health;
            bootstrap.weapon = weapon;
            bootstrap.waveManager = waveManager;
        }

        /// Enregistre la scène dans les Build Settings (rechargement par nom + builds device).
        public static void RegisterInBuildSettings(string scenePath)
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == scenePath) return;
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
