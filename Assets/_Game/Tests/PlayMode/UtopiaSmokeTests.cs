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

        [UnityTest]
        [Timeout(120000)] // la map est lourde (39k colliders) : marge de chargement
        public IEnumerator Utopia_BootstrapsAndWaveOneSpawns()
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
    }
}
