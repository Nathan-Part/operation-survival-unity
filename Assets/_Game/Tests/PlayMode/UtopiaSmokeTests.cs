using System.Collections;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.PlayTests
{
    public class UtopiaSmokeTests
    {
        const string ScenePath = "Assets/_Game/Scenes/Utopia.unity";

        static IEnumerator LoadUtopiaAndStartFirstWave()
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            #endif
            yield return null;
            yield return null;

            Assert.IsNotNull(GameContext.Instance, "GameContext absent sur Utopia");
            var waves = Object.FindAnyObjectByType<WaveManager>();
            Assert.IsNotNull(waves, "WaveManager absent sur Utopia");

            float timeout = Time.time + 15f;
            while (waves.AliveCount == 0 && Time.time < timeout) yield return null;
            Assert.Greater(waves.AliveCount, 0, "la vague 1 doit spawner sur la NavMesh d'utopia");
        }

        [UnityTest]
        [Timeout(120000)] // la map est lourde (39k colliders) : marge de chargement
        public IEnumerator Utopia_BootstrapsAndWaveOneSpawns()
        {
            yield return LoadUtopiaAndStartFirstWave();
        }

        [UnityTest]
        [Timeout(120000)] // la map est lourde (39k colliders) : marge de chargement
        public IEnumerator Utopia_SpawnedEnemies_CanReachPlayer()
        {
            yield return LoadUtopiaAndStartFirstWave();

            var playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            Assert.IsNotNull(playerHealth, "PlayerHealth introuvable sur Utopia");
            var playerPos = playerHealth.transform.position;

            foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                var path = new UnityEngine.AI.NavMeshPath();
                bool ok = UnityEngine.AI.NavMesh.CalculatePath(e.transform.position, playerPos,
                    UnityEngine.AI.NavMesh.AllAreas, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
                Assert.IsTrue(ok, $"Ennemi {e.name} injoignable (îlot NavMesh — bug n°5).");
            }
        }
    }
}
