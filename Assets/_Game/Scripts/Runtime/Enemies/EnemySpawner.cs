using Game.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameContext context;
        public Transform player;

        const float ReferenceHeight = 1.8f; // modèle web de référence

        public EnemyHealth Spawn(EnemyData data, Vector3 position, int wave)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"EnemySpawner: no NavMesh near {position} for '{data.id}' - skipping spawn.");
                return null;
            }

            var db = context.database;
            var stats = db.enemyCombatConfig.baseStats;
            var difficulty = GameContext.ResolveDifficulty(db);

            var root = new GameObject($"Enemy_{data.id}");
            root.transform.position = navHit.position;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = stats.runSpeed * data.speedMult;
            agent.radius = stats.radius;
            agent.height = ReferenceHeight * data.scale;
            agent.angularSpeed = stats.turnSpeed * 60f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = ReferenceHeight * data.scale;
            capsule.radius = stats.radius;
            capsule.center = new Vector3(0f, ReferenceHeight * data.scale * 0.5f, 0f);

            var health = root.AddComponent<EnemyHealth>();
            health.Init(data, wave, difficulty.enemyHpMult, db.enemyCombatConfig.hitZones);

            var controller = root.AddComponent<EnemyController>();
            controller.Init(data, db.enemyCombatConfig, player, difficulty);

            var model = context.ResolveEnemyModel(data);
            if (model != null)
            {
                var visual = Instantiate(model, root.transform);
                visual.transform.localPosition = Vector3.zero; // pivot GLB aux pieds
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * data.scale;
            }
            else
            {
                var capsule2 = PrimitiveFactory.CreateCapsule("Placeholder", RadarColor(data));
                capsule2.transform.SetParent(root.transform, false);
                capsule2.transform.localPosition = new Vector3(0f, ReferenceHeight * data.scale * 0.5f, 0f); // capsule : pivot au centre
                capsule2.transform.localScale = Vector3.one * (0.9f * data.scale); // capsule Unity = 2 m -> 1.8 m
            }

            return health;
        }

        public static Color RadarColor(EnemyData data)
        {
            string hex = string.IsNullOrEmpty(data.radarColor) ? "#ffb84d" : data.radarColor;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.red;
        }
    }
}
