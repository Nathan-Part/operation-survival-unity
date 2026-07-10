using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Rendu + actions de l'onglet Accessoires de la boutique (Task 13) : délégué par
    /// ShopScreenController comme ShopWeaponsTab/ShopCharacterTab (Tasks 11-12) — parité web
    /// `renderShop` branche par défaut sur l'onglet accessoires (main.js:1546-1602), 3 sous-onglets
    /// Masque/Costume/Danses (`shopAccSlot`, data-accslot, main.js:1548-1551).
    ///
    /// Masque/Costume : grille de tuiles (cadenas si non possédé, badge rareté), clic = essai en
    /// direct (`stage.PreviewAccessory` — mesh attaché à un os côté web, ici surimpression PNG près
    /// du perso, écart assumé spec §4.5) + une seule barre d'achat sous la grille, ciblant l'article
    /// "en focus" (aperçu courant, sinon l'équipé — parité web l.1567/1572).
    ///
    /// Danses : chaque tuile est autonome (pas de barre d'achat partagée) : « ▶ » joue toujours le
    /// clip (même non possédée, `stage.PlayDance`), puis Acheter si non possédée sinon
    /// Choisir/Sélect. (parité web l.1552-1563). Compteur "N/3" reflète `save.selectedDances.Count`
    /// réel (ToggleDanceSelection est FIFO au-delà de 3, jamais un refus par plafond).
    public class ShopAccessoriesTab
    {
        readonly GameDatabase db;
        readonly MetaProgress meta;
        readonly LobbyCharacterStage stage;

        VisualElement content;
        readonly Dictionary<string, Button> subTabButtons = new Dictionary<string, Button>();
        VisualElement accView, danceView;
        ScrollView accGrid, danceGrid;
        Label danceCounter;

        Label accName, accRarityBadge;
        Button accBuyButton, accEquipButton;
        Label accEquippedBadge;

        string activeSlot = "helmet"; // "helmet" | "costume" | "dance" — parité web shopAccSlot
        // web: shopAccPreview = {helmet:null, costume:null} (main.js:220) — aperçus indépendants par slot.
        readonly Dictionary<AccessoryType, string> preview = new Dictionary<AccessoryType, string>();

        string focusAccessoryId;

        public ShopAccessoriesTab(GameDatabase database, MetaProgress metaProgress, LobbyCharacterStage lobbyStage)
        {
            db = database;
            meta = metaProgress;
            stage = lobbyStage;
        }

        public void Wire(VisualElement tabContent)
        {
            content = tabContent;
            if (content == null) return;

            WireSubTab("helmet", "mask");
            WireSubTab("costume", "costume");
            WireSubTab("dance", "dance");

            accView = content.Q<VisualElement>("shop-acc-view");
            danceView = content.Q<VisualElement>("shop-dance-view");
            accGrid = content.Q<ScrollView>("shop-acc-grid");
            danceGrid = content.Q<ScrollView>("shop-dance-grid");
            danceCounter = content.Q<Label>("shop-dance-counter");

            accName = content.Q<Label>("shop-acc-name");
            accRarityBadge = content.Q<Label>("shop-acc-rarity-badge");
            accBuyButton = content.Q<Button>("shop-acc-buy");
            accEquipButton = content.Q<Button>("shop-acc-equip");
            accEquippedBadge = content.Q<Label>("shop-acc-equipped-badge");

            // Grille en flex-wrap façon grille (le ScrollView reste vertical, un seul réglage — pas de
            // classe USS dédiée par grille, comportement identique masque/costume/danses).
            SetupGridWrap(accGrid);
            SetupGridWrap(danceGrid);

            accBuyButton.clicked += () =>
            {
                var a = FindAccessory(focusAccessoryId);
                if (a == null || meta?.Data == null) return;
                if (ShopRules.BuyAccessory(meta.Data, a.id, a.price)) { meta.Persist(); Render(); }
            };
            accEquipButton.clicked += () =>
            {
                var a = FindAccessory(focusAccessoryId);
                if (a == null || meta?.Data == null) return;
                if (ShopRules.EquipAccessory(meta.Data, a.id, a.accessoryType)) { meta.Persist(); Render(); }
            };

            Render();
        }

        void WireSubTab(string key, string uxmlSuffix)
        {
            var button = content.Q<Button>($"shop-acc-subtab-{uxmlSuffix}");
            if (button == null) return;
            subTabButtons[key] = button;
            button.clicked += () => SelectSlot(key);
        }

        static void SetupGridWrap(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.contentContainer.style.flexDirection = FlexDirection.Row;
            scroll.contentContainer.style.flexWrap = Wrap.Wrap;
        }

        AccessoryData FindAccessory(string id) => string.IsNullOrEmpty(id) ? null
            : System.Array.Find(db.accessories, a => a != null && a.id == id);

        // web: data-accslot (main.js:1623) — pas de clearAccessoryPreview ici (les aperçus sont
        // indépendants par slot masque/costume, contrairement à un changement d'onglet complet de la
        // boutique — voir ShopScreenController.SwitchTab, qui appelle stage.ClearAccessoryPreview()).
        void SelectSlot(string slot)
        {
            activeSlot = slot;
            Render();
        }

        public void Render()
        {
            var save = meta?.Data;
            if (save == null || content == null) return;

            foreach (var kv in subTabButtons) kv.Value.EnableInClassList("on", kv.Key == activeSlot);

            bool isDance = activeSlot == "dance";
            if (accView != null) accView.style.display = isDance ? DisplayStyle.None : DisplayStyle.Flex;
            if (danceView != null) danceView.style.display = isDance ? DisplayStyle.Flex : DisplayStyle.None;

            if (isDance) RenderDances(save);
            else RenderAccessories(save, activeSlot == "helmet" ? AccessoryType.Helmet : AccessoryType.Costume);
        }

        void RenderAccessories(SaveData save, AccessoryType type)
        {
            if (accGrid == null) return;
            accGrid.Clear();

            var items = db.accessories.Where(a => a != null && a.accessoryType == type)
                .OrderBy(a => (int)a.rarity).ToList();

            string slotKey = type == AccessoryType.Helmet ? "helmet" : "costume";
            save.equipped.TryGetValue(slotKey, out string equippedId);
            preview.TryGetValue(type, out string previewId);

            // web l.1572 : focus = aperçu courant, sinon l'équipé, sinon le 1er article payant, sinon le 1er.
            var focus = FindAccessory(previewId) ?? FindAccessory(equippedId)
                ?? items.FirstOrDefault(i => i.price > 0) ?? items.FirstOrDefault();

            Rarity? lastRarity = null;
            foreach (var candidate in items)
            {
                if (lastRarity != candidate.rarity)
                {
                    AddRarityHeader(accGrid, candidate.rarity);
                    lastRarity = candidate.rarity;
                }

                bool owned = save.ownedAccessories.Contains(candidate.id);
                bool active = focus != null && candidate.id == focus.id;

                // Point 10 (parité shop-accessoires.png / shop-accessoires2.png) : vignette RONDE,
                // bordure colorée par rareté, coche verte si équipé — le nom et le badge de rareté
                // texte sortent de la vignette (portés par la barre d'achat + l'en-tête de section).
                var tile = new Button();
                tile.AddToClassList("acctile");
                tile.AddToClassList("acc-round");
                tile.AddToClassList(RoundRarityClass(candidate.rarity));
                tile.EnableInClassList("active", active);
                tile.EnableInClassList("owned", owned);

                var sprite = candidate.ResolveIcon(db.placeholders);
                if (sprite != null)
                {
                    var img = new Image { sprite = sprite };
                    img.AddToClassList("acctile-img");
                    tile.Add(img);
                }
                else
                {
                    // Glyphe vectoriel (les emojis 🎭/🥋/🔒 ne se rendent pas avec Barlow).
                    tile.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                        type == AccessoryType.Helmet ? Game.Runtime.UI.Tactical.TacticalIconType.Person
                                                     : Game.Runtime.UI.Tactical.TacticalIconType.Cube,
                        RarityColorOf(candidate.rarity), 48f));
                }

                bool isEquipped = candidate.id == equippedId;
                if (isEquipped)
                {
                    var check = new Label("✓");
                    check.AddToClassList("acc-check");
                    tile.Add(check);
                }
                else if (!owned)
                {
                    var lockBadge = new VisualElement();
                    lockBadge.AddToClassList("acc-lock");
                    lockBadge.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                        Game.Runtime.UI.Tactical.TacticalIconType.Lock, Color.white, 13f));
                    tile.Add(lockBadge);
                }

                var capturedId = candidate.id;
                var capturedType = type;
                tile.clicked += () =>
                {
                    preview[capturedType] = capturedId;
                    // Playtest 2026-07-10 soir : plus de stage.PreviewAccessory(...) — surimprimer
                    // le PNG plat sur le perso était mensonger tant qu'il n'y a pas de modèle 3D
                    // d'accessoire. La sélection n'alimente plus que la barre d'achat.
                    Render();
                };

                accGrid.Add(tile);
            }

            RenderAccessoryBuyBar(save, focus, equippedId);
        }

        void RenderAccessoryBuyBar(SaveData save, AccessoryData focus, string equippedId)
        {
            if (focus == null)
            {
                focusAccessoryId = null;
                accName.text = "—"; accRarityBadge.text = "";
                accBuyButton.style.display = DisplayStyle.None;
                accEquipButton.style.display = DisplayStyle.None;
                accEquippedBadge.style.display = DisplayStyle.None;
                return;
            }

            focusAccessoryId = focus.id;

            accName.text = focus.displayName;
            accRarityBadge.text = RarityName(focus.rarity);
            ApplyRarityClass(accRarityBadge, focus.rarity);

            bool owned = save.ownedAccessories.Contains(focus.id);
            bool equipped = equippedId == focus.id;

            accEquippedBadge.style.display = equipped ? DisplayStyle.Flex : DisplayStyle.None;
            accEquipButton.style.display = (owned && !equipped) ? DisplayStyle.Flex : DisplayStyle.None;
            accBuyButton.style.display = owned ? DisplayStyle.None : DisplayStyle.Flex;
            if (!owned)
            {
                accBuyButton.text = $"ACHETER · {focus.price} CR"; // pas de 🪙 : emoji non rendu par Barlow
                accBuyButton.SetEnabled(save.money >= focus.price);
            }
        }

        void RenderDances(SaveData save)
        {
            if (danceGrid == null) return;
            danceGrid.Clear();

            if (danceCounter != null) danceCounter.text = $"Sélection : {save.selectedDances.Count}/3";

            var dances = db.dances.Where(d => d != null).OrderBy(d => (int)d.rarity).ToList();
            Rarity? lastRarity = null;
            foreach (var dance in dances)
            {
                if (lastRarity != dance.rarity)
                {
                    AddRarityHeader(danceGrid, dance.rarity);
                    lastRarity = dance.rarity;
                }

                bool owned = save.ownedDances.Contains(dance.id);
                bool selected = save.selectedDances.Contains(dance.id);

                var tile = new VisualElement(); // pas un Button : contient 2 vrais boutons (▶ + Acheter/Choisir)
                tile.AddToClassList("acctile");
                tile.AddToClassList("dcard");
                tile.EnableInClassList("active", selected);
                tile.EnableInClassList("owned", owned);

                var name = new Label(dance.displayName);
                name.AddToClassList("acctile-name");
                tile.Add(name);

                var thumb = new VisualElement();
                thumb.AddToClassList("acctile-thumb");
                // Glyphe vectoriel (l'emoji 💃 ne se rend pas avec Barlow) : coureur = silhouette en mouvement.
                thumb.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                    Game.Runtime.UI.Tactical.TacticalIconType.Runner, RarityColorOf(dance.rarity), 48f));
                tile.Add(thumb);

                var foot = new VisualElement();
                foot.AddToClassList("acctile-foot");
                var rarityLabel = new Label(RarityName(dance.rarity).ToUpperInvariant());
                rarityLabel.AddToClassList("rarity-badge");
                rarityLabel.AddToClassList("acctile-rarity");
                ApplyRarityClass(rarityLabel, dance.rarity);
                foot.Add(rarityLabel);
                if (!owned)
                {
                    foot.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                        Game.Runtime.UI.Tactical.TacticalIconType.Lock, new Color(0.6f, 0.63f, 0.7f), 13f));
                }
                tile.Add(foot);

                var btnRow = new VisualElement();
                btnRow.AddToClassList("dcard-btnrow");

                var capturedId = dance.id;
                var capturedPrice = dance.price;

                // web: data-previewdance (main.js:1631) — toujours cliquable, même non possédée.
                var playButton = new Button { text = "▶" };
                playButton.AddToClassList("dcard-play");
                playButton.clicked += () => stage?.PlayDance(capturedId);
                btnRow.Add(playButton);

                if (!owned)
                {
                    var buy = new Button { text = $"{capturedPrice} CR" };
                    buy.AddToClassList("dcard-play");
                    buy.SetEnabled(save.money >= capturedPrice);
                    buy.clicked += () =>
                    {
                        if (ShopRules.BuyDance(meta.Data, capturedId, capturedPrice)) { meta.Persist(); Render(); }
                    };
                    btnRow.Add(buy);
                }
                else
                {
                    var toggle = new Button { text = selected ? "Sélect." : "Choisir" };
                    toggle.AddToClassList("dcard-play");
                    toggle.EnableInClassList("on", selected);
                    toggle.clicked += () =>
                    {
                        if (ShopRules.ToggleDanceSelection(meta.Data, capturedId)) { meta.Persist(); Render(); }
                    };
                    btnRow.Add(toggle);
                }

                tile.Add(btnRow);
                danceGrid.Add(tile);
            }
        }

        // Playtest Phase 5.1 (2026-07-09) : les items etaient deja tries par rarete (OrderBy) mais
        // affiches en liste plate — parite shop-accessoires.png (groupes COMMUN/RARE/EPIQUE/LEGENDAIRE
        // avec un en-tete par groupe). "rarity-group-header" force un retour a la ligne (width 100%)
        // dans le ScrollView en flex-wrap (SetupGridWrap).
        // Point 10 : en-tête de section façon web — label encadré + ligne horizontale, aux
        // couleurs de la rareté (inline : la table RarityColorOf est déjà la source de vérité).
        static void AddRarityHeader(ScrollView grid, Rarity rarity)
        {
            var row = new VisualElement();
            row.AddToClassList("rarity-header-row");

            var color = RarityColorOf(rarity);
            var label = new Label(RarityName(rarity).ToUpperInvariant());
            label.AddToClassList("rarity-header-label");
            label.style.color = color;
            label.style.borderTopColor = color;
            label.style.borderBottomColor = color;
            label.style.borderLeftColor = color;
            label.style.borderRightColor = color;
            row.Add(label);

            var line = new VisualElement();
            line.AddToClassList("rarity-header-line");
            line.style.backgroundColor = color;
            row.Add(line);

            grid.Add(row);
        }

        static string RoundRarityClass(Rarity r) => r switch
        {
            Rarity.Rare => "rar-rare",
            Rarity.Epic => "rar-epic",
            Rarity.Legendary => "rar-legendary",
            _ => "rar-common",
        };

        // web: RARITIES (main.js:82-87) — même convention dupliquée que ShopWeaponsTab/ShopCharacterTab.
        static string RarityName(Rarity r) => r switch
        {
            Rarity.Common => "Commun",
            Rarity.Rare => "Rare",
            Rarity.Epic => "Épique",
            Rarity.Legendary => "Légendaire",
            _ => "Commun",
        };

        static void ApplyRarityClass(Label label, Rarity r)
        {
            label.RemoveFromClassList("rarity-common");
            label.RemoveFromClassList("rarity-rare");
            label.RemoveFromClassList("rarity-epic");
            label.RemoveFromClassList("rarity-legendary");
            label.AddToClassList(r switch
            {
                Rarity.Common => "rarity-common",
                Rarity.Rare => "rarity-rare",
                Rarity.Epic => "rarity-epic",
                Rarity.Legendary => "rarity-legendary",
                _ => "rarity-common",
            });
        }

        // web: RARITIES[key].color (main.js:83-86) — même table que ShopWeaponsTab (classe indépendante).
        static Color RarityColorOf(Rarity r) => r switch
        {
            Rarity.Common => new Color(0.604f, 0.639f, 0.698f),
            Rarity.Rare => new Color(0.310f, 0.616f, 1f),
            Rarity.Epic => new Color(0.690f, 0.400f, 1f),
            Rarity.Legendary => new Color(1f, 0.690f, 0.180f),
            _ => new Color(0.604f, 0.639f, 0.698f),
        };
    }
}
