using System;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Contrôleur racine du lobby (premier UI Toolkit du projet) : rend l'accueil depuis
    /// GameDatabase/MetaProgress (jamais de mutation directe de SaveData), navigation des
    /// panneaux (boutique/loadout/map/réglages/comment-jouer, encore vides à ce stade — Tasks
    /// 8-15), sélecteur de perso 3D (persos possédés uniquement), raccourcis clavier lobby
    /// (parité web : ←/→ ou A/D change de perso, Entrée/Espace déploie, Échap ferme le panneau).
    [RequireComponent(typeof(UIDocument))]
    public class HomeController : MonoBehaviour
    {
        // [SerializeField] : assignés par HomeSceneBuilder à l'édition et doivent survivre à la
        // sérialisation de la scène — le Play Mode recharge le domaine et réinstancie la scène
        // depuis le disque, donc tout câblage fait en édition (dont l'appel Init() du builder)
        // est perdu pour l'instance réelle ; Start() doit rejouer le câblage UI à partir de ces
        // références sérialisées (cf. WireUi()).
        [SerializeField] GameDatabase db;
        [SerializeField] MetaProgress meta;
        [SerializeField] LobbyCharacterStage stage;
        [SerializeField] GameLauncher launcher;
        [SerializeField] SettingsPanelController settingsPanel;

        bool wired;

        VisualElement root;

        Label rankLabel;
        VisualElement rankBarFill;
        Label creditsChip, waveChip, comboChip;
        Label intelBestWave, intelBestCombo, intelBestScore, intelPrestige, intelAchievements, intelCredits;
        Label equipWeapon, equipHelmet, equipCostume;
        Label charName;
        VisualElement charDots;
        Button charPrev, charNext;

        readonly Dictionary<string, VisualElement> panels = new Dictionary<string, VisualElement>();
        string activePanel;

        CharacterData[] ownedCharacters = Array.Empty<CharacterData>();

        InputActionMap map;
        InputAction leftAction, rightAction, deployAction, closeAction;

        /// N'assigne QUE les références (pas de câblage ici) : appelée par HomeSceneBuilder à
        /// l'édition, où les MonoBehaviours ne reçoivent ni Awake ni Start tant qu'on n'est pas en
        /// Play Mode. Si WireUi() créait des GameObjects (perso 3D) dès cet appel, ils seraient
        /// sérialisés tels quels dans la scène puis dupliqués par le vrai câblage runtime (Start())
        /// au Play Mode suivant — d'où la séparation stricte affectation (Init) / câblage (Start).
        public void Init(GameDatabase database, MetaProgress metaProgress, LobbyCharacterStage characterStage, GameLauncher gameLauncher, SettingsPanelController settingsPanelController)
        {
            db = database;
            meta = metaProgress;
            stage = characterStage;
            launcher = gameLauncher;
            settingsPanel = settingsPanelController;
        }

        void Start()
        {
            if (!wired && db != null && meta != null) WireUi();
        }

        void WireUi()
        {
            if (wired) return;
            wired = true;

            if (meta.Data == null) meta.LoadOrCreate(); // web: chargement de save au boot du lobby

            var document = GetComponent<UIDocument>();
            root = document.rootVisualElement;

            rankLabel = root.Q<Label>("rank-label");
            rankBarFill = root.Q<VisualElement>("rank-bar-fill");
            creditsChip = root.Q<Label>("credits-chip");
            waveChip = root.Q<Label>("wave-chip");
            comboChip = root.Q<Label>("combo-chip");

            intelBestWave = root.Q<Label>("intel-best-wave");
            intelBestCombo = root.Q<Label>("intel-best-combo");
            intelBestScore = root.Q<Label>("intel-best-score");
            intelPrestige = root.Q<Label>("intel-prestige");
            intelAchievements = root.Q<Label>("intel-achievements");
            intelCredits = root.Q<Label>("intel-credits");

            equipWeapon = root.Q<Label>("equip-weapon");
            equipHelmet = root.Q<Label>("equip-helmet");
            equipCostume = root.Q<Label>("equip-costume");

            charName = root.Q<Label>("char-name");
            charDots = root.Q<VisualElement>("char-dots");
            charPrev = root.Q<Button>("char-prev");
            charNext = root.Q<Button>("char-next");

            var deployButton = root.Q<Button>("deploy-button");
            var settingsButton = root.Q<Button>("settings-button");
            var howtoButton = root.Q<Button>("howto-button");
            var tileShop = root.Q<Button>("tile-shop");
            var tileLoadout = root.Q<Button>("tile-loadout");
            var tileMap = root.Q<Button>("tile-map");
            var tileTraining = root.Q<Button>("tile-training"); // pas de panneau : lancement direct (Task 7)

            panels["shop"] = root.Q<VisualElement>("shop-panel");
            panels["loadout"] = root.Q<VisualElement>("loadout-panel");
            panels["map"] = root.Q<VisualElement>("map-panel");
            panels["settings"] = root.Q<VisualElement>("settings-panel");
            panels["howto"] = root.Q<VisualElement>("howto-panel");

            charPrev.clicked += () => ChangeCharacter(-1);
            charNext.clicked += () => ChangeCharacter(1);
            settingsButton.clicked += () => { settingsPanel?.Render(meta.Data); OpenPanel("settings"); }; // web: openSettings() rend puis ouvre
            howtoButton.clicked += () => OpenPanel("howto");
            tileShop.clicked += () => OpenPanel("shop");
            tileLoadout.clicked += () => OpenPanel("loadout");
            tileMap.clicked += () => OpenPanel("map");
            tileTraining.clicked += () => launcher?.Deploy(meta, training: true);
            deployButton.clicked += TryDeploy;

            BuildInputMap();
            RenderLobby();
        }

        void BuildInputMap()
        {
            map = new InputActionMap("HomeLobby");

            leftAction = map.AddAction("Left");
            leftAction.AddBinding("<Keyboard>/leftArrow");
            leftAction.AddBinding("<Keyboard>/a");

            rightAction = map.AddAction("Right");
            rightAction.AddBinding("<Keyboard>/rightArrow");
            rightAction.AddBinding("<Keyboard>/d");

            deployAction = map.AddAction("Deploy");
            deployAction.AddBinding("<Keyboard>/enter");
            deployAction.AddBinding("<Keyboard>/numpadEnter");
            deployAction.AddBinding("<Keyboard>/space");

            closeAction = map.AddAction("Close", binding: "<Keyboard>/escape");

            map.Enable();
        }

        void OnDisable() => map?.Disable();
        void OnDestroy() => map?.Disable();

        void Update()
        {
            if (map == null) return;
            if (leftAction.WasPressedThisFrame()) ChangeCharacter(-1);
            if (rightAction.WasPressedThisFrame()) ChangeCharacter(1);
            if (deployAction.WasPressedThisFrame()) TryDeploy();
            if (closeAction.WasPressedThisFrame()) CloseActivePanel();
        }

        // Bouton "Déployer" + Entrée/Espace : lance la partie normale (pas d'entraînement).
        void TryDeploy() => launcher?.Deploy(meta, training: false);

        /// Affiche le panneau demandé ("shop"|"loadout"|"map"|"settings"|"howto") et masque le
        /// panneau concurrent éventuellement ouvert (un seul overlay actif à la fois, comme le web).
        public void OpenPanel(string name)
        {
            if (!panels.TryGetValue(name, out var panel) || panel == null) return;
            if (activePanel != null && activePanel != name && panels.TryGetValue(activePanel, out var previous))
                previous.AddToClassList("hidden");
            panel.RemoveFromClassList("hidden");
            activePanel = name;
        }

        /// Échap : ferme le panneau ouvert ; si aucun n'est ouvert, ne fait rien (comme au lobby web).
        public void CloseActivePanel()
        {
            if (activePanel == null) return;
            if (panels.TryGetValue(activePanel, out var panel)) panel.AddToClassList("hidden");
            activePanel = null;
        }

        /// Re-render idempotent de tout le lobby depuis SaveData/GameDatabase — aucune mutation ici.
        public void RenderLobby()
        {
            var save = meta != null ? meta.Data : null;
            if (save == null || db == null) return;

            int rank = RankRules.OperatorRank(save.bestWave); // web: updateHomeMeta (main.js:56-57)
            float fill = RankRules.OperatorRankFill(save.bestWave);
            rankLabel.text = $"Rang {rank}";
            rankBarFill.style.width = new Length(fill * 100f, LengthUnit.Percent);

            int comboMultiplier = RankRules.ComboMultiplier(save.bestCombo);
            creditsChip.text = save.money.ToString();
            waveChip.text = $"Vague {save.bestWave}";
            comboChip.text = $"Meilleur combo x{comboMultiplier}";

            int bestScore = save.leaderboard.Count > 0 ? save.leaderboard[0].score : 0;

            intelBestWave.text = save.bestWave.ToString();
            intelBestCombo.text = save.bestCombo.ToString();
            intelBestScore.text = bestScore.ToString();
            intelPrestige.text = save.prestige > 0 ? $"★{save.prestige}" : "—";
            intelAchievements.text = $"{save.unlockedAchievements.Count}/{db.achievements.Length}";
            intelCredits.text = save.money.ToString();

            equipWeapon.text = FindWeaponName(save.selectedWeapon);
            equipHelmet.text = FindAccessoryName(save.equipped.TryGetValue("helmet", out var helmet) ? helmet : null);
            equipCostume.text = FindAccessoryName(save.equipped.TryGetValue("costume", out var costume) ? costume : null);

            RenderCharacterSelector(save);
        }

        void RenderCharacterSelector(Game.Data.Save.SaveData save)
        {
            // web: rebuildLobbyCharacters — filtre le catalogue par possession, ordre du catalogue conservé.
            ownedCharacters = Array.FindAll(db.characters, c => c != null && save.ownedCharacters.Contains(c.id));

            if (ownedCharacters.Length == 0)
            {
                charName.text = "—";
                charDots.Clear();
                return;
            }

            int index = Array.FindIndex(ownedCharacters, c => c.id == save.selectedCharacterId);
            if (index < 0) index = 0;

            charName.text = ownedCharacters[index].displayName;
            stage?.ShowCharacter(ownedCharacters[index].id);

            charDots.Clear();
            for (int i = 0; i < ownedCharacters.Length; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList("dot");
                if (i == index) dot.AddToClassList("dot-active");
                charDots.Add(dot);
            }
        }

        void ChangeCharacter(int direction)
        {
            if (ownedCharacters.Length == 0) return;
            var save = meta.Data;
            int current = Array.FindIndex(ownedCharacters, c => c.id == save.selectedCharacterId);
            if (current < 0) current = 0;
            int next = ((current + direction) % ownedCharacters.Length + ownedCharacters.Length) % ownedCharacters.Length;

            if (!ShopRules.SelectCharacter(save, ownedCharacters[next].id)) return;
            meta.Persist();
            RenderLobby();
        }

        string FindWeaponName(string id)
        {
            foreach (var w in db.weapons) if (w != null && w.id == id) return w.displayName;
            return "—";
        }

        string FindAccessoryName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "—";
            foreach (var a in db.accessories) if (a != null && a.id == id) return a.displayName;
            return "—";
        }
    }
}
