using Game.Data;
using Game.Runtime;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Runtime.Editor
{
    public static class SliceSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Slice.unity";
        const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";

        [MenuItem("Tools/Game/Build Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var arena = new GameObject("Arena");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arena.transform);
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // plane 10 m -> 40 x 40 m
            AddCube(arena.transform, "WallN", new Vector3(0f, 1.5f, 20f), new Vector3(40f, 3f, 1f));
            AddCube(arena.transform, "WallS", new Vector3(0f, 1.5f, -20f), new Vector3(40f, 3f, 1f));
            AddCube(arena.transform, "WallE", new Vector3(20f, 1.5f, 0f), new Vector3(1f, 3f, 40f));
            AddCube(arena.transform, "WallW", new Vector3(-20f, 1.5f, 0f), new Vector3(1f, 3f, 40f));
            AddCube(arena.transform, "Crate1", new Vector3(5f, 0.75f, 6f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "Crate2", new Vector3(-7f, 0.75f, -4f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "Crate3", new Vector3(8f, 0.75f, -8f), new Vector3(1.5f, 1.5f, 1.5f));
            AddCube(arena.transform, "HighPlatform", new Vector3(-4f, 2.5f, 10f), new Vector3(4f, 5f, 4f)); // pour tester les dégâts de chute

            var surface = arena.AddComponent<NavMeshSurface>();
            surface.BuildNavMesh();

            var contextGo = new GameObject("GameContext");
            var context = contextGo.AddComponent<GameContext>();
            context.database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (context.database == null)
                Debug.LogError($"SliceSceneBuilder: GameDatabase introuvable a {DatabasePath}");

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.2f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;

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
            var offsets = new[]
            {
                new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, 15f),
                new Vector3(15f, 0f, -15f), new Vector3(-15f, 0f, -15f)
            };
            var points = new Transform[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var p = new GameObject($"SpawnPoint{i}");
                p.transform.SetParent(wavesGo.transform);
                p.transform.position = offsets[i];
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

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            Debug.Log($"SliceSceneBuilder: scene sauvegardee -> {ScenePath}");
        }

        /// Nécessaire pour recharger la scène par nom (bouton Rejouer) et pour les builds device.
        static void RegisterInBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == ScenePath) return;
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void AddCube(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = size;
        }
    }
}
