using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur shell de la boutique (Task 11 : structure + onglet Armes ; Tasks 12-14 ajouteront
    /// le contenu des 4 autres onglets dans ce même shell) : clone ShopPanel.uxml dans le conteneur
    /// vide `#shop-panel` de Home.uxml (Task 6), rail latéral 5 onglets, topbar Crédits/Fermer,
    /// flèches Précédent/Suivant (navigation du carrousel de l'onglet actif — seul l'onglet Armes
    /// en a un cette tâche). Parité web : `renderShop`/`shopTab` (main.js:1197, 1461-1610).
    ///
    /// `stage` (LobbyCharacterStage) : gardé en référence sans y toucher, comme demandé (web : le
    /// perso lobby reste affiché, on ne recrée jamais son GameObject) — pas d'appel dessus cette
    /// tâche (aucun onglet ne montre encore un perso), disponible pour l'onglet Personnage (Task 12).
    ///
    /// Pas de référence à HomeController ici (contrairement à Map/Loadout/Settings) : le bouton
    /// "Fermer" du topbar appelle directement Close() (aucune dépendance externe nécessaire) : c'est
    /// HomeController qui tient la référence inverse (`shopScreen`) pour router Échap vers Close().
    [RequireComponent(typeof(UIDocument))]
    public class ShopScreenController : MonoBehaviour
    {
        // [SerializeField] : mêmes raisons que les autres contrôleurs de panneau (Map/Loadout/
        // Settings) — assignés par HomeSceneBuilder à l'édition, doivent survivre à la sérialisation
        // de la scène pour être encore présents quand le Play Mode recharge le domaine.
        [SerializeField] Game.Data.GameDatabase db;
        [SerializeField] MetaProgress meta;
        [SerializeField] LobbyCharacterStage stage;
        [SerializeField] VisualTreeAsset panelAsset;

        // web: WCATS de tabs (main.js:1197 shopTab, valeurs 'weapons'|'player'|'accessories'|'boost'|
        // 'consumables') — ordre du rail latéral, Task 11 ne construit que "weapons".
        static readonly string[] TabOrder = { "weapons", "player", "accessories", "boost", "consumables" };

        bool wired;
        VisualElement shopPanel;
        VisualElement navRow;
        Label creditsLabel;
        readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, VisualElement> tabContents = new Dictionary<string, VisualElement>();
        string activeTab = "weapons";

        Transform weaponPivot;
        ShopWeaponsTab weaponsTab;

        const float HeroRotationRadPerSec = 0.8f; // web: main.js:5450 (shopWeaponObj.rotation.y += dt*0.8)

        /// N'assigne QUE les références (pas de câblage ici) — même raison que sur les autres
        /// contrôleurs de panneau (HomeController.Init, MapPanelController.Init...) : appelée par
        /// HomeSceneBuilder à l'édition, où WireUi() créerait des VisualElement/GameObject
        /// instanciés qui seraient sérialisés puis dupliqués au Play Mode suivant.
        public void Init(Game.Data.GameDatabase database, MetaProgress metaProgress, LobbyCharacterStage characterStage, VisualTreeAsset asset)
        {
            db = database;
            meta = metaProgress;
            stage = characterStage;
            panelAsset = asset;
        }

        void Start()
        {
            if (!wired && db != null && meta != null && panelAsset != null) WireUi();
        }

        void WireUi()
        {
            if (wired) return;
            wired = true;

            if (meta.Data == null) meta.LoadOrCreate(); // au cas où ce composant s'initialise avant HomeController

            var root = GetComponent<UIDocument>().rootVisualElement;
            shopPanel = root.Q<VisualElement>("shop-panel");
            if (shopPanel == null) return; // #shop-panel absent de Home.uxml : rien à faire, pas de plantage

            shopPanel.Clear();
            panelAsset.CloneTree(shopPanel);

            creditsLabel = shopPanel.Q<Label>("shop-credits");
            navRow = shopPanel.Q<VisualElement>("shop-nav");
            var closeButton = shopPanel.Q<Button>("shop-close");
            var prevButton = shopPanel.Q<Button>("shop-prev");
            var nextButton = shopPanel.Q<Button>("shop-next");

            foreach (var tab in TabOrder)
            {
                var button = shopPanel.Q<Button>($"shop-tab-{tab}");
                var pane = shopPanel.Q<VisualElement>($"shop-tab-content-{tab}");
                if (button == null || pane == null) continue; // onglet absent de l'UXML : ignoré proprement
                tabButtons[tab] = button;
                tabContents[tab] = pane;
                var captured = tab; // capture locale (évite la variable de boucle partagée)
                button.clicked += () => SwitchTab(captured);
            }

            closeButton.clicked += Close;
            prevButton.clicked += () => { if (activeTab == "weapons") weaponsTab.Prev(); };
            nextButton.clicked += () => { if (activeTab == "weapons") weaponsTab.Next(); };

            // Pivot du modèle 3D héros de l'onglet Armes : GameObject dédié (pas un enfant de
            // LobbyCharacterStage — on ne touche jamais son visuel perso), positionné à côté du
            // perso dans le champ de la caméra lobby fixe (main.js:39-40 : cam à (0,1.95,6.8) qui
            // regarde (0,1.46,0)). Coordonnées approximatives (pas de caméra boutique dédiée comme
            // le web SHOP_CENTER_POS) : simplification assumée, documentée dans le rapport de tâche.
            var pivotGo = new GameObject("ShopWeaponPivot");
            pivotGo.transform.position = new Vector3(1.3f, 1.3f, 0.3f);
            weaponPivot = pivotGo.transform;

            weaponsTab = new ShopWeaponsTab(db, meta, weaponPivot);
            if (tabContents.TryGetValue("weapons", out var weaponsPane)) weaponsTab.Wire(weaponsPane);

            SwitchTab("weapons");
        }

        void SwitchTab(string tab)
        {
            activeTab = tab;
            foreach (var kv in tabButtons) kv.Value.EnableInClassList("on", kv.Key == tab);
            foreach (var kv in tabContents) kv.Value.EnableInClassList("hidden", kv.Key != tab);
            if (navRow != null) navRow.EnableInClassList("hidden", tab != "weapons"); // pas de carrousel sur les onglets stub
            Render();
        }

        public void Open()
        {
            if (!wired || shopPanel == null) return;
            shopPanel.RemoveFromClassList("hidden");
            Render();
        }

        public void Close()
        {
            if (shopPanel == null) return;
            shopPanel.AddToClassList("hidden");
        }

        /// Rendu idempotent (peut être rejoué à volonté) : topbar crédits + contenu de l'onglet actif.
        public void Render()
        {
            if (!wired || meta?.Data == null || creditsLabel == null) return;
            creditsLabel.text = meta.Data.money.ToString();
            if (activeTab == "weapons") weaponsTab?.Render();
        }

        // web: main.js:5450 — rotation du modèle héros uniquement pendant que la boutique est
        // ouverte ET que l'onglet Armes est actif (shopOpen && shopTab==='weapons').
        void Update()
        {
            if (weaponPivot == null || shopPanel == null) return;
            if (shopPanel.ClassListContains("hidden") || activeTab != "weapons") return;
            weaponPivot.Rotate(Vector3.up, HeroRotationRadPerSec * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
        }
    }
}
