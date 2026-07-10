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

            // --- Départ joueur + spawns sur la composante NavMesh principale (Phase 5 bis, point 2) ---
            // Le centre de la place est un îlot (disque entouré du bassin) et les coins ±15/±15
            // tombent dans les bâtiments : l'ancien repli ajoutait des spawns non validés
            // -> chemins partiels, ennemis figés ou collés aux murs, voire vagues vides.
            Vector3 centerGround = new Vector3(0f, 0f, 0f);
            if (Physics.Raycast(new Vector3(0f, 200f, 0f), Vector3.down, out RaycastHit ground, 500f))
                centerGround = ground.point;

            var probes = new System.Collections.Generic.List<Vector3>();
            TryAddProbe(probes, centerGround, centerGround.y);
            foreach (float radius in new[] { 6f, 10f, 14f, 18f, 22f, 26f })
                for (int deg = 0; deg < 360; deg += 30)
                {
                    var dir = Quaternion.Euler(0f, deg, 0f) * Vector3.forward;
                    var pos = new Vector3(centerGround.x, 0f, centerGround.z) + dir * radius;
                    if (Physics.Raycast(pos + Vector3.up * 60f, Vector3.down, out RaycastHit h, 120f))
                        TryAddProbe(probes, h.point, centerGround.y);
                }

            // Regroupement par composante connexe (chemin NavMesh complet vers le représentant).
            var groups = new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
            foreach (var probe in probes)
            {
                bool placed = false;
                foreach (var group in groups)
                {
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(group[0], probe, NavMesh.AllAreas, path)
                        && path.status == NavMeshPathStatus.PathComplete)
                    { group.Add(probe); placed = true; break; }
                }
                if (!placed) groups.Add(new System.Collections.Generic.List<Vector3> { probe });
            }
            groups.Sort((a, b) => b.Count - a.Count);
            var main = groups.Count > 0 ? groups[0] : new System.Collections.Generic.List<Vector3>();
            if (main.Count < 8)
            {
                Debug.LogError($"UtopiaSceneBuilder: composante NavMesh principale trop petite ({main.Count} sondes / {probes.Count}) — vérifier le bake. Scène NON générée.");
                return;
            }

            // Départ joueur : le point de la composante principale le plus proche du centre.
            main.Sort((a, b) =>
                (a - centerGround).sqrMagnitude.CompareTo((b - centerGround).sqrMagnitude));
            Vector3 playerPos = main[0] + Vector3.up * 0.6f;

            // Spawns : 6 secteurs de 60° autour du joueur, un point par secteur à 10-30 m
            // (le plus proche de 16 m), tous garantis joignables (même composante).
            var spawns = new System.Collections.Generic.List<Vector3>();
            for (int sector = 0; sector < 6; sector++)
            {
                Vector3 best = Vector3.zero;
                float bestScore = float.MaxValue;
                foreach (var candidate in main)
                {
                    Vector3 flat = candidate - main[0];
                    float dist = new Vector2(flat.x, flat.z).magnitude;
                    if (dist < 10f || dist > 30f) continue;
                    float angle = Mathf.Repeat(Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg, 360f);
                    if ((int)(angle / 60f) != sector) continue;
                    float score = Mathf.Abs(dist - 16f);
                    if (score < bestScore) { bestScore = score; best = candidate; }
                }
                if (bestScore < float.MaxValue) spawns.Add(best);
            }
            if (spawns.Count < 3)
            {
                Debug.LogError($"UtopiaSceneBuilder: seulement {spawns.Count} spawns joignables trouvés — vérifier le bake. Scène NON générée.");
                return;
            }

            GameSceneKit.BuildGameplay(playerPos, spawns.ToArray());

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            GameSceneKit.RegisterInBuildSettings(ScenePath);
            Debug.Log($"UtopiaSceneBuilder: scene sauvegardee -> {ScenePath} ({colliders} colliders, {spawns.Count} spawns, joueur a {playerPos}, NavMesh bakee)");
        }

        /// Ajoute une sonde si un NavMesh existe à la surface visée : rejette les toits
        /// (écart de hauteur vs le centre) et la nappe souterraine (mesh loin du sol sondé).
        static void TryAddProbe(System.Collections.Generic.List<Vector3> probes, Vector3 groundPoint, float centerY)
        {
            if (Mathf.Abs(groundPoint.y - centerY) > 3f) return;                       // toit/balcon
            if (!NavMesh.SamplePosition(groundPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return;
            if (Mathf.Abs(hit.position.y - groundPoint.y) > 1f) return;                // nappe sous la dalle
            probes.Add(hit.position);
        }
    }
}
