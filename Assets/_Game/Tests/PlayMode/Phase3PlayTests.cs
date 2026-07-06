using System.Collections;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.PlayTests
{
    public class Phase3PlayTests
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

        static IEnumerator WaitForWaveOne(WaveManager waves)
        {
            float timeout = Time.time + 10f;
            while (waves.AliveCount == 0 && Time.time < timeout) yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyDeath_KeepsCorpseWithColliderOff()
        {
            LoadSlice();
            yield return null;
            var waves = Object.FindAnyObjectByType<WaveManager>();
            yield return WaitForWaveOne(waves);
            Assert.Greater(waves.AliveCount, 0);

            var enemy = waves.Alive[0];
            var go = enemy.gameObject;
            enemy.TakeHit(enemy.transform.position + Vector3.up * 1.6f, 1000f); // headshot létal
            yield return null;

            Assert.IsTrue(enemy.IsDead);
            Assert.IsNotNull(go, "le cadavre doit rester dans la scène");
            Assert.IsTrue(go != null && go.activeInHierarchy, "cadavre présent");
            var capsule = go.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(capsule);
            Assert.IsFalse(capsule.enabled, "collider du cadavre désactivé (non ciblable)");
            var controller = go.GetComponent<EnemyController>();
            Assert.AreEqual(EnemyState.Dead, controller.State);
        }

        [UnityTest]
        public IEnumerator ToggleView_SwitchesModeAndBack()
        {
            LoadSlice();
            yield return null;
            yield return null;
            var view = Object.FindAnyObjectByType<PlayerViewController>();
            Assert.IsNotNull(view, "PlayerViewController créé par le bootstrap");
            Assert.AreEqual(ViewMode.First, view.Mode, "défaut = 1ʳᵉ personne");

            view.ToggleView();
            yield return null;
            Assert.AreEqual(ViewMode.Third, view.Mode);

            view.ToggleView();
            yield return null;
            Assert.AreEqual(ViewMode.First, view.Mode);
        }
    }
}
