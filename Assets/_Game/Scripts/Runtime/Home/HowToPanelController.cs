using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur du panneau Comment jouer (web : #howto-panel, onglets .howto-tab / main.js) :
    /// clone HowToPanel.uxml dans le conteneur vide `#howto-panel` de Home.uxml (Task 6). Panneau
    /// purement informatif — 7 onglets de texte statique recopiés tels quels du web (voir
    /// HowToPanel.uxml, source : game.html:281-486 + tableau des touches de docs/ROADMAP.md
    /// « Parité HUD & contrôles »). Aucune dépendance à SaveData/MetaProgress ici (contrairement à
    /// Map/Loadout/Settings) : juste la navigation d'onglets et le bouton Retour, délégué à
    /// HomeController.CloseActivePanel() comme les autres panneaux.
    [RequireComponent(typeof(UIDocument))]
    public class HowToPanelController : MonoBehaviour
    {
        // web: valeurs data-howto (game.html:287-293) — ordre du rail d'onglets.
        static readonly string[] TabOrder = { "controles", "combat", "ennemis", "powerups", "gadgets", "boutique", "vagues" };

        // [SerializeField] : mêmes raisons que les autres contrôleurs de panneau (Map/Loadout/
        // Settings) — assignés par HomeSceneBuilder à l'édition, doivent survivre à la sérialisation
        // de la scène pour être encore présents quand le Play Mode recharge le domaine.
        [SerializeField] HomeController home;
        [SerializeField] VisualTreeAsset panelAsset;

        bool wired;

        readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, VisualElement> tabPages = new Dictionary<string, VisualElement>();

        /// N'assigne QUE les références (pas de câblage ici) — voir le commentaire équivalent sur
        /// MapPanelController.Init : appelée par HomeSceneBuilder à l'édition, où WireUi()
        /// créerait des VisualElement instanciés qui seraient sérialisés puis dupliqués au Play
        /// Mode suivant.
        public void Init(HomeController homeController, VisualTreeAsset asset)
        {
            home = homeController;
            panelAsset = asset;
        }

        void Start()
        {
            if (!wired && panelAsset != null) WireUi();
        }

        void WireUi()
        {
            if (wired) return;
            wired = true;

            var root = GetComponent<UIDocument>().rootVisualElement;
            var container = root.Q<VisualElement>("howto-panel");
            if (container == null) return; // #howto-panel absent de Home.uxml : rien à faire, pas de plantage

            container.Clear();
            panelAsset.CloneTree(container);

            foreach (var tab in TabOrder)
            {
                var button = container.Q<Button>($"howto-tab-{tab}");
                var page = container.Q<VisualElement>($"howto-page-{tab}");
                if (button == null || page == null) continue; // onglet absent de l'UXML : ignoré proprement
                tabButtons[tab] = button;
                tabPages[tab] = page;
                var captured = tab; // capture locale (évite la variable de boucle partagée)
                button.clicked += () => SwitchTab(captured);
            }

            var backButton = container.Q<Button>("howto-back");
            if (backButton != null) backButton.clicked += () => home?.CloseActivePanel();

            SwitchTab("controles");
        }

        void SwitchTab(string tab)
        {
            foreach (var kv in tabButtons) kv.Value.EnableInClassList("on", kv.Key == tab);
            foreach (var kv in tabPages) kv.Value.EnableInClassList("hidden", kv.Key != tab);
        }
    }
}
