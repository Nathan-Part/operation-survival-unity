using System.Collections;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.PlayTests
{
    public class SliceSmokeTests
    {
        const string ScenePath = "Assets/_Game/Scenes/Slice.unity";

        static void LoadSlice()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        }

        [UnityTest]
        public IEnumerator Scene_BootstrapsWithoutErrors()
        {
            LoadSlice();
            yield return null; // Awake/Start
            yield return null;

            Assert.IsNotNull(GameContext.Instance, "GameContext.Instance absent");
            Assert.IsNotNull(GameContext.Instance.database, "GameDatabase non assignée");
            Assert.IsNotNull(Object.FindAnyObjectByType<PlayerMotor>(), "PlayerMotor absent");
            Assert.IsNotNull(Object.FindAnyObjectByType<WaveManager>(), "WaveManager absent");
        }

        [UnityTest]
        public IEnumerator FirstWave_SpawnsThreeShootersAndTheyDie()
        {
            LoadSlice();
            yield return null;
            var waves = Object.FindAnyObjectByType<WaveManager>();
            Assert.IsNotNull(waves);

            // intermission 2.5 s (WaveConfig) + marge
            float timeout = Time.time + 10f;
            while (waves.AliveCount == 0 && Time.time < timeout) yield return null;

            Assert.AreEqual(1, waves.CurrentWave);
            Assert.AreEqual(3, waves.AliveCount, "vague 1 = 3 shooters");

            // Tue tout le monde par headshots surpuissants
            var alive = new System.Collections.Generic.List<EnemyHealth>(waves.Alive);
            foreach (var enemy in alive)
                enemy.TakeHit(enemy.transform.position + Vector3.up * 1.6f, 1000f);
            yield return null;

            Assert.AreEqual(0, waves.AliveCount, "tous les ennemis doivent être morts");
        }
    }
}
