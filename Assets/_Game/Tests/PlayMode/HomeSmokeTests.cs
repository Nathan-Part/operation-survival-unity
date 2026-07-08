using System.Collections;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Game.Runtime.PlayTests
{
    /// Smoke test PlayMode de l'accueil (Task 16, patron SliceSmokeTests) : la scène Home charge
    /// sans erreur, le lobby est rendu depuis la save (MetaProgress), et le panneau boutique
    /// s'ouvre/se ferme via l'API publique de HomeController (OpenPanel/CloseActivePanel).
    public class HomeSmokeTests
    {
        const string ScenePath = "Assets/_Game/Scenes/Home.unity";

        static void LoadHome()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        }

        [UnityTest]
        public IEnumerator Scene_BootstrapsAndRendersLobby()
        {
            LoadHome();
            yield return null; // Awake
            yield return null; // Start (WireUi + RenderLobby)

            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.IsNotNull(document, "UIDocument absent");
            var root = document.rootVisualElement;
            Assert.IsNotNull(root, "rootVisualElement absent");

            var lobby = root.Q<VisualElement>("lobby");
            Assert.IsNotNull(lobby, "#lobby absent");
            Assert.IsFalse(lobby.ClassListContains("hidden"), "#lobby devrait être visible au chargement");

            var creditsChip = root.Q<Label>("credits-chip");
            Assert.IsNotNull(creditsChip, "#credits-chip absent");
            Assert.IsFalse(string.IsNullOrEmpty(creditsChip.text), "le label crédits devrait être renseigné (non vide)");

            var meta = Object.FindAnyObjectByType<MetaProgress>();
            Assert.IsNotNull(meta, "MetaProgress absent");
            Assert.IsNotNull(meta.Data, "MetaProgress.Data devrait être chargé au Start de HomeController");

            var shopPanel = root.Q<VisualElement>("shop-panel");
            Assert.IsNotNull(shopPanel, "#shop-panel absent");
            Assert.IsTrue(shopPanel.ClassListContains("hidden"), "#shop-panel devrait être caché au chargement");
        }

        [UnityTest]
        public IEnumerator OpenPanel_Shop_ShowsAndCloseHidesIt()
        {
            LoadHome();
            yield return null;
            yield return null;

            var home = Object.FindAnyObjectByType<HomeController>();
            Assert.IsNotNull(home, "HomeController absent");

            var document = Object.FindAnyObjectByType<UIDocument>();
            var shopPanel = document.rootVisualElement.Q<VisualElement>("shop-panel");
            Assert.IsNotNull(shopPanel, "#shop-panel absent");
            Assert.IsTrue(shopPanel.ClassListContains("hidden"), "précondition : #shop-panel caché avant OpenPanel");

            home.OpenPanel("shop");
            yield return null;
            Assert.IsFalse(shopPanel.ClassListContains("hidden"), "#shop-panel devrait être visible après OpenPanel(\"shop\")");

            home.CloseActivePanel();
            yield return null;
            Assert.IsTrue(shopPanel.ClassListContains("hidden"), "#shop-panel devrait être caché après CloseActivePanel()");
        }
    }
}
