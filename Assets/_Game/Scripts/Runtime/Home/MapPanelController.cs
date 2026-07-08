using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur du panneau Map (web : sélection de terrain, `MAP_VISUALS`/§ home) : clone
    /// MapPanel.uxml dans le conteneur vide `#map-panel` de Home.uxml (Task 6). Cliquabilité et
    /// état grisé de CHAQUE carte (utopia, urban) sont dérivés à l'exécution de
    /// `MapRules.IsAvailable` (voir WireMapItem/ApplyAvailability) — rien n'est figé en dur dans
    /// MapPanel.uxml, donc si `MapRules.IsAvailable("urban")` change un jour, ce panneau suit sans
    /// retouche manuelle. Persiste immédiatement le choix. Pas de préchargement de décor ici : le
    /// chargement réel de la scène se fait au déploiement (GameLauncher + MapRules.SceneNameFor),
    /// écart assumé par rapport à un hypothétique préchargement au menu.
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
        Button urbanItem;

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
            urbanItem = container.Q<Button>("map-item-urban");
            var backButton = container.Q<Button>("map-back");

            // Câblage du clic dérivé de MapRules.IsAvailable pour chaque carte (via ce helper
            // partagé, pas de bloc dupliqué par map) : seule une map disponible reçoit un handler,
            // "urban" n'en reçoit aucun tant que MapRules.IsAvailable("urban") == false. L'état
            // grisé/désactivé (lui) est réappliqué à chaque Render() plus bas.
            WireMapItem(utopiaItem, "utopia");
            WireMapItem(urbanItem, "urban");
            backButton.clicked += () => home?.CloseActivePanel();

            Render(meta.Data);
        }

        void WireMapItem(Button item, string mapId)
        {
            if (MapRules.IsAvailable(mapId)) item.clicked += () => SelectMap(mapId);
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

            // Grisé/désactivé dérivé de MapRules.IsAvailable à chaque rendu — pour les deux cartes,
            // via le même helper (pas figé dans MapPanel.uxml). "urban" retombe désactivé tant que
            // MapRules.IsAvailable("urban") == false ; si elle devient disponible un jour, ce
            // rendu (et le câblage de clic dans WireUi) suivra automatiquement.
            ApplyAvailability(utopiaItem, "utopia");
            ApplyAvailability(urbanItem, "urban");
        }

        void ApplyAvailability(Button item, string mapId)
        {
            bool available = MapRules.IsAvailable(mapId);
            item.SetEnabled(available);
            item.EnableInClassList("map-item-disabled", !available);
        }
    }
}
