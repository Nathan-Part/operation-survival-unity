using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur du panneau Map (web : sélection de terrain, `MAP_VISUALS`/§ home) : clone
    /// MapPanel.uxml dans le conteneur vide `#map-panel` de Home.uxml (Task 6), affiche les maps
    /// livrées (seule "utopia" est jouable, `MapRules.IsAvailable`) et persiste immédiatement le
    /// choix. Pas de préchargement de décor ici : le chargement réel de la scène se fait au
    /// déploiement (GameLauncher + MapRules.SceneNameFor), écart assumé par rapport à un
    /// hypothétique préchargement au menu.
    [RequireComponent(typeof(UIDocument))]
    public class MapPanelController : MonoBehaviour
    {
        // [SerializeField] : mêmes raisons que SettingsPanelController — assignés par
        // HomeSceneBuilder à l'édition, doivent survivre à la sérialisation de la scène pour être
        // encore présents quand le Play Mode recharge le domaine et réinstancie la scène.
        [SerializeField] MetaProgress meta;
        [SerializeField] HomeController home;
        [SerializeField] VisualTreeAsset panelAsset;

        bool wired;

        Button utopiaItem;
        Label utopiaSub;

        /// N'assigne QUE les références (pas de câblage ici) — voir le commentaire équivalent sur
        /// SettingsPanelController.Init : appelée par HomeSceneBuilder à l'édition, où WireUi()
        /// créerait des VisualElement instanciés qui seraient sérialisés puis dupliqués au Play
        /// Mode suivant.
        public void Init(MetaProgress metaProgress, HomeController homeController, VisualTreeAsset asset)
        {
            meta = metaProgress;
            home = homeController;
            panelAsset = asset;
        }

        void Start()
        {
            if (!wired && meta != null && panelAsset != null) WireUi();
        }

        void WireUi()
        {
            if (wired) return;
            wired = true;

            if (meta.Data == null) meta.LoadOrCreate(); // au cas où ce composant s'initialise avant HomeController

            var root = GetComponent<UIDocument>().rootVisualElement;
            var container = root.Q<VisualElement>("map-panel");
            if (container == null) return; // #map-panel absent de Home.uxml : rien à faire, pas de plantage

            container.Clear();
            panelAsset.CloneTree(container);

            utopiaItem = container.Q<Button>("map-item-utopia");
            utopiaSub = container.Q<Label>("map-sub-utopia");
            var backButton = container.Q<Button>("map-back");

            // Seule "utopia" est cliquable : "urban" est un VisualElement (pas un Button) rendu
            // grisé avec le badge "Bientôt", jamais de handler de clic (MapRules.IsAvailable == false).
            utopiaItem.clicked += () => SelectMap("utopia");
            backButton.clicked += () => home?.CloseActivePanel();

            Render(meta.Data);
        }

        // web: sélection de map -> persist() synchrone, jamais de batch (même logique que setSetting).
        void SelectMap(string mapId)
        {
            if (meta == null || meta.Data == null || !MapRules.IsAvailable(mapId)) return;
            meta.Data.settings.mapId = mapId;
            meta.Persist();
            Render(meta.Data);
        }

        /// Rendu idempotent (peut être rejoué à volonté, aucune mutation ici) — appelé après chaque
        /// changement et à l'ouverture du panneau (HomeController) pour refléter l'état le plus récent.
        public void Render(SaveData save)
        {
            if (save == null || utopiaItem == null) return;

            bool utopiaCurrent = save.settings.mapId == "utopia";
            utopiaSub.text = utopiaCurrent ? "Terrain actuel" : "Choisir ce terrain";
            utopiaSub.EnableInClassList("map-sub-current", utopiaCurrent);

            // "urban" reste grisé/non cliquable en permanence (MapRules.IsAvailable("urban") == false) :
            // rien à mettre à jour côté rendu, son état est figé dans MapPanel.uxml.
        }
    }
}
