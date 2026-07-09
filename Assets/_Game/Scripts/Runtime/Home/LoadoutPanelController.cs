using System.Collections.Generic;
using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using Game.Runtime.UI.Tactical;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur du panneau Série de points (killstreak loadout, web #156 : renderLoadoutPanel /
    /// toggleLoadoutBonus, main.js:720-731, 3290-3308) : clone LoadoutPanel.uxml dans le conteneur
    /// vide `#loadout-panel` de Home.uxml (Task 6). Les 5 bonus (nom affiché, touche) sont des
    /// constantes cosmétiques figées ici comme LOADOUT_NAMES/LOADOUT_KEYLABEL côté web — en
    /// revanche le palier (valeur d'équilibrage) vient toujours de `EconomyConfig.loadoutThresholds`,
    /// jamais recopié en dur. Sélection (max 3, décalage FIFO au 4e clic) déléguée à
    /// `ShopRules.ToggleLoadout`, jamais réimplémentée ici.
    [RequireComponent(typeof(UIDocument))]
    public class LoadoutPanelController : MonoBehaviour
    {
        readonly struct LoadoutTile
        {
            public readonly string Key;
            public readonly string KeyLabel;

            public LoadoutTile(string key, string keyLabel)
            {
                Key = key;
                KeyLabel = keyLabel;
            }
        }

        // Ordre + touches : parité web LOADOUT_BONUSES/LOADOUT_KEYLABEL (main.js:3291, 722-723).
        // Les noms affichés restent dans LoadoutPanel.uxml (LOADOUT_NAMES côté web) — seul le
        // palier (EconomyConfig.loadoutThresholds) est lu dynamiquement ici.
        static readonly LoadoutTile[] Tiles =
        {
            new LoadoutTile("turret", "L"),
            new LoadoutTile("mine", "Y"),
            new LoadoutTile("airstrike", "U"),
            new LoadoutTile("carepackage", "I"),
            new LoadoutTile("dog", "4"),
        };

        // [SerializeField] : mêmes raisons que MapPanelController/SettingsPanelController —
        // assignés par HomeSceneBuilder à l'édition, doivent survivre à la sérialisation de la
        // scène pour être encore présents quand le Play Mode recharge le domaine.
        [SerializeField] GameDatabase db;
        [SerializeField] MetaProgress meta;
        [SerializeField] HomeController home;
        [SerializeField] VisualTreeAsset panelAsset;

        bool wired;

        readonly Dictionary<string, Label> subLabels = new Dictionary<string, Label>();
        readonly Dictionary<string, Button> toggleButtons = new Dictionary<string, Button>();

        /// N'assigne QUE les références (pas de câblage ici) — voir le commentaire équivalent sur
        /// MapPanelController.Init : appelée par HomeSceneBuilder à l'édition, où WireUi() créerait
        /// des VisualElement instanciés qui seraient sérialisés puis dupliqués au Play Mode suivant.
        public void Init(GameDatabase database, MetaProgress metaProgress, HomeController homeController, VisualTreeAsset asset)
        {
            db = database;
            meta = metaProgress;
            home = homeController;
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
            var panel = root.Q<VisualElement>("loadout-panel");
            if (panel == null) return; // #loadout-panel absent de Home.uxml : rien à faire, pas de plantage

            var frame = panel.Q<TacticalModalFrame>("loadout-frame");
            var container = frame != null ? frame.Content : panel;
            container.Clear();
            panelAsset.CloneTree(container);

            subLabels.Clear();
            toggleButtons.Clear();

            foreach (var tile in Tiles)
            {
                var sub = container.Q<Label>($"loadout-sub-{tile.Key}");
                var button = container.Q<Button>($"loadout-toggle-{tile.Key}");
                if (sub == null || button == null) continue; // tuile absente de l'UXML : on l'ignore proprement

                subLabels[tile.Key] = sub;
                toggleButtons[tile.Key] = button;

                string key = tile.Key; // capture locale pour la fermeture (évite la variable de boucle partagée)
                button.clicked += () => Toggle(key);
            }

            var backButton = container.Q<Button>("loadout-back");
            backButton.clicked += () => home?.CloseActivePanel();

            Render(meta.Data);
        }

        // web (toggleLoadoutBonus) : bascule via ShopRules.ToggleLoadout -> persist() synchrone -> re-render.
        void Toggle(string key)
        {
            if (meta == null || meta.Data == null) return;
            ShopRules.ToggleLoadout(meta.Data, key);
            meta.Persist();
            Render(meta.Data);
        }

        /// Rendu idempotent (peut être rejoué à volonté, aucune mutation ici) — appelé après chaque
        /// changement et à l'ouverture du panneau (HomeController) pour refléter l'état le plus
        /// récent : palier lu depuis EconomyConfig (jamais figé en dur), état choisi/non choisi de
        /// chaque tuile depuis `save.selectedLoadout`.
        public void Render(SaveData save)
        {
            if (save == null || db == null || db.economyConfig == null || subLabels.Count == 0) return;

            var thresholds = db.economyConfig.loadoutThresholds;

            foreach (var tile in Tiles)
            {
                subLabels[tile.Key].text = $"Palier : {Threshold(thresholds, tile.Key)} pts";

                bool selected = save.selectedLoadout.Contains(tile.Key);
                var button = toggleButtons[tile.Key];
                button.text = (selected ? "✓ Choisi" : "Choisir") + $" — touche {tile.KeyLabel}";
                button.EnableInClassList("toggle-on", selected);
            }
        }

        static int Threshold(LoadoutThresholds thresholds, string key) => key switch
        {
            "turret" => thresholds.turret,
            "mine" => thresholds.mine,
            "airstrike" => thresholds.airstrike,
            "carepackage" => thresholds.carepackage,
            "dog" => thresholds.dog,
            _ => 0,
        };
    }
}
