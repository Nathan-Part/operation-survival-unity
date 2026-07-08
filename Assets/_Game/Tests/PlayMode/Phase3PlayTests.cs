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

        /// Régression revue finale (task-final) : melee15/melee5 (unity-spec/contracts.json)
        /// restaient bloqués à 0/target faute de signal distinct des kills à distance.
        /// Vérifie que PlayerMelee.MeleeKill se déclenche bien quand un coup de mêlée achève
        /// un ennemi (RunTracker s'y abonne pour avancer le contrat "melee").
        [UnityTest]
        public IEnumerator MeleeStrike_FiresMeleeKill_WhenItFinishesAnEnemy()
        {
            LoadSlice();
            yield return null;
            var waves = Object.FindAnyObjectByType<WaveManager>();
            yield return WaitForWaveOne(waves);
            Assert.Greater(waves.AliveCount, 0);

            var melee = Object.FindAnyObjectByType<PlayerMelee>();
            var view = Object.FindAnyObjectByType<PlayerViewController>();
            Assert.IsNotNull(melee, "PlayerMelee créé par le bootstrap");
            Assert.IsNotNull(view != null ? view.playerCamera : null, "caméra de visée absente");

            var enemy = waves.Alive[0];
            var aim = view.playerCamera.transform;
            enemy.transform.position = aim.position + aim.forward * 1.5f; // à portée (2.6 m) et dans le cône

            bool fired = false;
            melee.MeleeKill += () => fired = true;
            bool struck = melee.Strike();
            yield return null;

            Assert.IsTrue(struck, "le coup doit partir (hors cooldown)");
            Assert.IsTrue(enemy.IsDead, "150 dégâts doivent achever un ennemi de vague 1");
            Assert.IsTrue(fired, "MeleeKill doit se déclencher quand le coup achève l'ennemi");
        }

        [UnityTest]
        public IEnumerator ToggleView_SwitchesModeAndBack()
        {
            LoadSlice();
            yield return null;
            yield return null;
            var view = Object.FindAnyObjectByType<PlayerViewController>();
            Assert.IsNotNull(view, "PlayerViewController créé par le bootstrap");
            var initial = view.Mode; // dépend du réglage `defaultView` de la save (défaut web : third)

            view.ToggleView();
            yield return null;
            Assert.AreNotEqual(initial, view.Mode, "V doit changer de vue");

            view.ToggleView();
            yield return null;
            Assert.AreEqual(initial, view.Mode, "V doit revenir à la vue initiale");
        }
    }
}
