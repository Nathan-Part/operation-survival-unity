using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Runtime.Editor
{
    public static class SliceSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Slice.unity";

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

            GameSceneKit.BuildGameplay(new Vector3(0f, 1.2f, 0f), new[]
            {
                new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, 15f),
                new Vector3(15f, 0f, -15f), new Vector3(-15f, 0f, -15f)
            });

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            GameSceneKit.RegisterInBuildSettings(ScenePath);
            Debug.Log($"SliceSceneBuilder: scene sauvegardee -> {ScenePath}");
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
