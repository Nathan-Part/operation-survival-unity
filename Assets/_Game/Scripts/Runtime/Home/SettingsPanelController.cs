using Game.Data.Rules;
using Game.Data.Save;
using Game.Runtime.UI.Tactical;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur du panneau Réglages (parité web `renderSettings`/`setSetting`, main.js:739-753,
    /// 771-777) : clone SettingsPanel.uxml dans le conteneur vide `#settings-panel` de Home.uxml
    /// (Task 6), affiche l'état courant de `save.settings`, et persiste immédiatement chaque
    /// changement (jamais de mutation batchée — le web appelle persist() de façon synchrone dans
    /// setSetting). Toute mutation de SaveData passe par SettingsRules (logique pure testée
    /// en EditMode) ; ce MonoBehaviour ne fait jamais `save.settings.x = y` lui-même.
    [RequireComponent(typeof(UIDocument))]
    public class SettingsPanelController : MonoBehaviour
    {
        // [SerializeField] : mêmes raisons que HomeController/LobbyCharacterStage — assignés par
        // HomeSceneBuilder à l'édition, doivent survivre à la sérialisation de la scène pour être
        // encore présents quand le Play Mode recharge le domaine et réinstancie la scène.
        [SerializeField] MetaProgress meta;
        [SerializeField] HomeController home;
        [SerializeField] VisualTreeAsset panelAsset;

        bool wired;

        Slider sensitivitySlider;
        Label sensitivityValue;
        Slider volumeSlider;
        Label volumeValue;
        Button viewButton;
        Button diffFacile, diffNormal, diffDifficile;
        Button hardcoreButton;
        Button rainButton;

        /// N'assigne QUE les références (pas de câblage ici) — voir le commentaire équivalent sur
        /// HomeController.Init : appelée par HomeSceneBuilder à l'édition, où WireUi() créerait des
        /// VisualElement instanciés qui seraient sérialisés puis dupliqués au Play Mode suivant.
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
            var panel = root.Q<VisualElement>("settings-panel");
            if (panel == null) return; // #settings-panel absent de Home.uxml : rien à faire, pas de plantage

            // Playtest Phase 5.1 (2026-07-09) : le contenu clone va desormais DANS le cadre de modale
            // tactique (TacticalModalFrame.Content), pas directement dans le voile plein ecran.
            var frame = panel.Q<TacticalModalFrame>("settings-frame");
            var container = frame != null ? frame.Content : panel;
            container.Clear();
            panelAsset.CloneTree(container);

            sensitivitySlider = container.Q<Slider>("set-sensitivity");
            sensitivityValue = container.Q<Label>("set-sensitivity-val");
            volumeSlider = container.Q<Slider>("set-volume");
            volumeValue = container.Q<Label>("set-volume-val");
            viewButton = container.Q<Button>("set-view");
            diffFacile = container.Q<Button>("set-diff-facile");
            diffNormal = container.Q<Button>("set-diff-normal");
            diffDifficile = container.Q<Button>("set-diff-difficile");
            hardcoreButton = container.Q<Button>("set-hardcore");
            rainButton = container.Q<Button>("set-rain");
            var backButton = container.Q<Button>("settings-back");

            sensitivitySlider.RegisterValueChangedCallback(e => Apply(() => SettingsRules.SetSensitivity(meta.Data, e.newValue)));
            volumeSlider.RegisterValueChangedCallback(e => Apply(() => SettingsRules.SetVolume(meta.Data, e.newValue)));
            viewButton.clicked += () => Apply(() => SettingsRules.ToggleView(meta.Data));
            diffFacile.clicked += () => Apply(() => SettingsRules.SetDifficulty(meta.Data, "facile"));
            diffNormal.clicked += () => Apply(() => SettingsRules.SetDifficulty(meta.Data, "normal"));
            diffDifficile.clicked += () => Apply(() => SettingsRules.SetDifficulty(meta.Data, "difficile"));
            hardcoreButton.clicked += () => Apply(() => SettingsRules.ToggleHardcore(meta.Data));
            rainButton.clicked += () => Apply(() => SettingsRules.ToggleRain(meta.Data));
            backButton.clicked += () => home?.CloseActivePanel();

            Render(meta.Data);
        }

        // web: setSetting(key, value) -> persist() synchrone, jamais de batch (parité #94/#93/#46).
        void Apply(System.Action mutate)
        {
            if (meta == null || meta.Data == null) return;
            mutate();
            meta.Persist();
            Render(meta.Data);
        }

        /// Rendu idempotent (peut être rejoué à volonté, aucune mutation ici) — appelé après chaque
        /// changement et à l'ouverture du panneau (HomeController) pour refléter l'état le plus récent.
        public void Render(SaveData save)
        {
            if (save == null || sensitivitySlider == null) return;
            var s = save.settings;

            sensitivitySlider.SetValueWithoutNotify(s.sensitivity);
            sensitivityValue.text = s.sensitivity.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "×"; // web l.742

            volumeSlider.SetValueWithoutNotify(s.volume);
            volumeValue.text = Mathf.RoundToInt(s.volume * 100f) + "%"; // web l.744 (Math.round(volume*100)+'%', pas d'espace)

            viewButton.text = s.defaultView == "first" ? "1re personne" : "3e personne"; // web l.746

            string difficulty = string.IsNullOrEmpty(s.difficulty) ? "normal" : s.difficulty; // web l.747
            diffFacile.EnableInClassList("diff-active", difficulty == "facile");
            diffNormal.EnableInClassList("diff-active", difficulty == "normal");
            diffDifficile.EnableInClassList("diff-active", difficulty == "difficile");

            hardcoreButton.text = s.hardcore ? "☠ Activé" : "Désactivé"; // web l.750
            hardcoreButton.EnableInClassList("toggle-on", s.hardcore);

            rainButton.text = s.rain ? "🌧 Activé" : "Désactivé"; // web l.752
            rainButton.EnableInClassList("toggle-on", s.rain);
        }
    }
}
