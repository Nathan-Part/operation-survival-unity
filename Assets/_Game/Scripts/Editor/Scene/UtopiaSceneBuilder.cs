using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime.Editor
{
    /// Scène de la map utopia : GLB pleine qualité exporté du .blend de Nathan
    /// (Blender CLI), colliders posés sur chaque mesh, NavMesh bakée, gameplay
    /// câblé par GameSceneKit. Régénérable à volonté (Tools → Game → Build Utopia Scene).
    public static class UtopiaSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Utopia.unity";
        const string MapPath = "Assets/_Game/Art/Maps/utopia.glb";

        [MenuItem("Tools/Game/Build Utopia Scene")]
        public static void Build()
        {
            var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath);
            if (mapPrefab == null)
            {
                Debug.LogError($"UtopiaSceneBuilder: map introuvable a {MapPath} (exporter le .blend en GLB d'abord).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);

            var map = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
            map.name = "Map_Utopia";
            map.transform.position = Vector3.zero;
            int colliders = 0;
            foreach (var filter in map.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                colliders++;
            }

            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            // Sol au centre de la map : point de départ du joueur
            Vector3 playerPos = new Vector3(0f, 1.5f, 0f);
            if (Physics.Raycast(new Vector3(0f, 200f, 0f), Vector3.down, out RaycastHit ground, 500f))
                playerPos = ground.point + Vector3.up * 0.6f;

            // Points de spawn : positions NavMesh valides autour du joueur
            var offsets = new[]
            {
                new Vector3(15f, 0f, 15f), new Vector3(-15f, 0f, 15f),
                new Vector3(15f, 0f, -15f), new Vector3(-15f, 0f, -15f),
                new Vector3(25f, 0f, 0f), new Vector3(-25f, 0f, 0f),
            };
            var spawns = new System.Collections.Generic.List<Vector3>();
            foreach (var offset in offsets)
            {
                var probe = playerPos + offset + Vector3.up * 5f;
                if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 30f, NavMesh.AllAreas))
                    spawns.Add(hit.position);
            }
            if (spawns.Count == 0)
            {
                Debug.LogWarning("UtopiaSceneBuilder: aucun point de spawn sur la NavMesh — spawns greybox autour du joueur.");
                foreach (var offset in offsets) spawns.Add(playerPos + offset);
            }

            GameSceneKit.BuildGameplay(playerPos, spawns.ToArray());

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            GameSceneKit.RegisterInBuildSettings(ScenePath);
            Debug.Log($"UtopiaSceneBuilder: scene sauvegardee -> {ScenePath} ({colliders} colliders, {spawns.Count} spawns, NavMesh bakee)");
        }
    }
}
