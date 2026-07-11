using System.Collections.Generic;
using Game.Runtime.UI.Tactical;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur shell de la boutique (Task 11 : structure + onglet Armes ; Tasks 12-14 ont ajouté
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

        /// Flèches clavier du Home (point 1 feedback n°3) : le carrousel perso ne doit défiler
        /// que sur l'onglet Personnage — HomeController interroge cet état avant de router ← →.
        public bool IsCharacterTabActive => activeTab == "player";

        public void CharacterPrev() => characterTab?.Prev();
        public void CharacterNext() => characterTab?.Next();

        Transform weaponPivot;
        ShopWeaponsTab weaponsTab;
        ShopCharacterTab characterTab;
        ShopAccessoriesTab accessoriesTab;
        ShopBoostTab boostTab;
        ShopConsumablesTab consumablesTab;

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

            // Icônes vectorielles (parité shop-guns.png — les emojis ne se rendent pas avec Barlow).
            var statsPurple = new Color(0.486f, 0.302f, 1f);
            TacticalIcon.Mount(shopPanel, "shop-credits-icon", TacticalIconType.Plus);
            TacticalIcon.Mount(shopPanel, "shop-stats-icon", TacticalIconType.Bars, statsPurple);
            TacticalIcon.Mount(shopPanel, "shop-cstats-icon", TacticalIconType.Bars, statsPurple);
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
            prevButton.clicked += () =>
            {
                if (activeTab == "weapons") weaponsTab.Prev();
                else if (activeTab == "player") characterTab.Prev();
            };
            nextButton.clicked += () =>
            {
                if (activeTab == "weapons") weaponsTab.Next();
                else if (activeTab == "player") characterTab.Next();
            };

            // Pivot du modèle 3D héros de l'onglet Armes : GameObject dédié (pas un enfant de
            // LobbyCharacterStage — on ne touche jamais son visuel perso), positionné à côté du
            // perso dans le champ de la caméra lobby fixe (main.js:39-40 : cam à (0,1.95,6.8) qui
            // regarde (0,1.46,0)). Coordonnées approximatives (pas de caméra boutique dédiée comme
            // le web SHOP_CENTER_POS) : simplification assumée, documentée dans le rapport de tâche.
            var pivotGo = new GameObject("ShopWeaponPivot");
            // Phase 5 bis (point 8) : l'arme est CENTRÉE dans la vitrine (parité shop-guns.png) —
            // le perso du lobby est masqué pendant l'onglet Armes (voir UpdateHeroVisualVisibility).
            // z=4.5 : rapprochée de la caméra lobby (0,1.95,6.8) pour occuper l'écran comme le web.
            pivotGo.transform.position = new Vector3(0f, 1.7f, 4.5f);
            // Finding 1 (revue Task 11) : caché par défaut à la création — sinon le modèle héros (ou
            // la capsule de secours) reste visible dans le lobby avant même la première ouverture de
            // la boutique. Parité web : main.js:1365,1406 (shopWeaponObj.visible toggle sur
            // entrée/sortie de l'onglet Armes).
            pivotGo.SetActive(false);
            weaponPivot = pivotGo.transform;

            weaponsTab = new ShopWeaponsTab(db, meta, weaponPivot);
            if (tabContents.TryGetValue("weapons", out var weaponsPane)) weaponsTab.Wire(weaponsPane);

            characterTab = new ShopCharacterTab(db, meta, stage);
            if (tabContents.TryGetValue("player", out var playerPane)) characterTab.Wire(playerPane);

            accessoriesTab = new ShopAccessoriesTab(db, meta, stage);
            if (tabContents.TryGetValue("accessories", out var accessoriesPane)) accessoriesTab.Wire(accessoriesPane);

            boostTab = new ShopBoostTab(db, meta);
            if (tabContents.TryGetValue("boost", out var boostPane)) boostTab.Wire(boostPane);

            consumablesTab = new ShopConsumablesTab(db, meta);
            if (tabContents.TryGetValue("consumables", out var consumablesPane)) consumablesTab.Wire(consumablesPane);

            SwitchTab("weapons");
        }

        // Icône de chaque onglet du rail : blanche sur l'onglet actif (fond violet), sombre sinon
        // (fond blanc) — remontée à chaque SwitchTab, TacticalIcon ne suit pas la couleur USS.
        static readonly Dictionary<string, TacticalIconType> TabIcons = new Dictionary<string, TacticalIconType>
        {
            ["weapons"] = TacticalIconType.Gun,
            ["player"] = TacticalIconType.Person,
            ["accessories"] = TacticalIconType.Cube,
            ["boost"] = TacticalIconType.Trophy,
            ["consumables"] = TacticalIconType.Plus,
        };

        void UpdateTabIcons()
        {
            var dark = new Color(0.137f, 0.157f, 0.22f);
            foreach (var kv in TabIcons)
                TacticalIcon.Mount(shopPanel, $"shop-tab-{kv.Key}-icon", kv.Value, kv.Key == activeTab ? Color.white : dark);
        }

        void SwitchTab(string tab)
        {
            activeTab = tab;
            foreach (var kv in tabButtons) kv.Value.EnableInClassList("on", kv.Key == tab);
            UpdateTabIcons();
            foreach (var kv in tabContents) kv.Value.EnableInClassList("hidden", kv.Key != tab);
            // web: updateShopTabs (main.js:1393) — flèches Précédent/Suivant visibles sur les
            // onglets Armes ET Personnage (les seuls avec un carrousel), masquées sur les 3 stubs.
            if (navRow != null) navRow.EnableInClassList("hidden", tab != "weapons" && tab != "player");
            // web: switchTab (main.js:1426) — en entrant sur l'onglet Personnage, le carrousel
            // repart toujours du perso réellement équipé (jamais d'un aperçu laissé par la visite
            // précédente de cet onglet).
            if (tab == "player") characterTab?.SelectByEquipped();
            // Trio 3D du seul onglet Personnages (point 9) : nettoyé dès qu'on le quitte.
            else stage?.ClearNeighbors();
            // Zoom vitrine : persos rapprochés de la caméra sur l'onglet Personnages uniquement.
            stage?.SetShowcaseZoom(tab == "player");
            // web: setShopTab (main.js:1421-1423) — clearAccessoryPreview() à CHAQUE changement
            // d'onglet, quel que soit l'onglet de départ/d'arrivée (pas seulement en quittant
            // Accessoires) : un essai masque/costume ne doit jamais persister visuellement une fois
            // qu'on a quitté l'onglet (ou qu'on y revient après un détour par un autre onglet).
            stage?.ClearAccessoryPreview();
            UpdateHeroVisualVisibility();
            Render();
        }

        public void Open()
        {
            if (!wired || shopPanel == null) return;
            shopPanel.RemoveFromClassList("hidden");
            UpdateHeroVisualVisibility();
            Render();
        }

        public void Close()
        {
            if (shopPanel == null) return;
            shopPanel.AddToClassList("hidden");
            UpdateHeroVisualVisibility();
            RevertStageToEquippedCharacter();
            // web: closeShop (main.js:1404-1408) — clearAccessoryPreview() explicite à la fermeture
            // (en plus de celui de chaque SwitchTab, redondant si on a changé d'onglet juste avant,
            // mais nécessaire si on ferme direct sans changer d'onglet). Danse : interruption
            // explicite documentée (voir LobbyCharacterStage.StopDance) plutôt que la laisser
            // continuer en tâche de fond une fois la boutique refermée.
            stage?.ClearAccessoryPreview();
            stage?.StopDance();
            stage?.ClearNeighbors(); // le trio de l'onglet Personnages ne survit jamais à la fermeture
            stage?.SetShowcaseZoom(false);
            // Phase 5 bis (point 5) : le bouton "Fermer" appelle Close() en direct, sans passer
            // par HomeController — l'event permet à ce dernier de ré-afficher le lobby.
            Closed?.Invoke();
        }

        /// Levé à CHAQUE fermeture (bouton Fermer du topbar comme Échap via HomeController).
        public event System.Action Closed;

        // Annule tout essai visuel laissé par le carrousel de l'onglet Personnage (Task 12) —
        // s'applique quel que soit l'onglet actif à la fermeture (pas seulement "player" : l'aperçu
        // reste affiché sur le stage tant qu'on ne quitte pas la boutique, voir ShopCharacterTab).
        // Repli sur le 1er perso du catalogue si aucune sélection encore faite (save neuve, avant
        // le premier `SelectCharacter` — mêmes règles que HomeController.RenderCharacterSelector).
        void RevertStageToEquippedCharacter()
        {
            if (stage == null || meta?.Data == null) return;
            string equippedId = !string.IsNullOrEmpty(meta.Data.selectedCharacterId)
                ? meta.Data.selectedCharacterId
                : (db != null && db.characters.Length > 0 ? db.characters[0].id : null);
            if (equippedId != null) stage.ShowCharacter(equippedId);
        }

        // Finding 1 (revue Task 11) : le pivot (donc le modèle héros/la capsule de secours qu'il
        // contient) n'est actif que boutique ouverte ET onglet Armes actif — même condition que la
        // rotation dans Update() ci-dessous, et parité du toggle shopWeaponObj.visible du web.
        void UpdateHeroVisualVisibility()
        {
            if (weaponPivot == null) return;
            bool shopOpen = shopPanel != null && !shopPanel.ClassListContains("hidden");
            bool weaponsShowcase = shopOpen && activeTab == "weapons";
            weaponPivot.gameObject.SetActive(weaponsShowcase);
            // Phase 5 bis (point 8) : sur l'onglet Armes on retire le perso de la vitrine —
            // l'arme seule au centre (shop-guns.png). Il revient sur les autres onglets/fermeture.
            stage?.SetVisible(!weaponsShowcase);
        }

        /// Rendu idempotent (peut être rejoué à volonté) : topbar crédits + contenu de l'onglet actif.
        public void Render()
        {
            if (!wired || meta?.Data == null || creditsLabel == null) return;
            creditsLabel.text = meta.Data.money.ToString();
            if (activeTab == "weapons") weaponsTab?.Render();
            else if (activeTab == "player") characterTab?.Render();
            else if (activeTab == "accessories") accessoriesTab?.Render();
            else if (activeTab == "boost") boostTab?.Render();
            else if (activeTab == "consumables") consumablesTab?.Render();
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
