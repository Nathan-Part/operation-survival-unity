using System.Linq;
using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Rendu + actions de l'onglet Boost de la boutique (Task 14) : délégué par
    /// ShopScreenController comme les autres onglets — parité web `renderShop` branche 'boost'
    /// (main.js:1521-1537). Grille des 6 `BoostData` (1 slot vest/boots/medal par bonus, jamais
    /// de sous-onglet ni de barre d'achat partagée : chaque tuile porte son propre bouton d'action
    /// qui change de sens selon l'état — parité `itemCard`/`data-buyboost`/`data-equipboost`).
    public class ShopBoostTab
    {
        readonly GameDatabase db;
        readonly MetaProgress meta;

        VisualElement content;
        ScrollView grid;

        public ShopBoostTab(GameDatabase database, MetaProgress metaProgress)
        {
            db = database;
            meta = metaProgress;
        }

        public void Wire(VisualElement tabContent)
        {
            content = tabContent;
            if (content == null) return;

            grid = content.Q<ScrollView>("shop-boost-grid");
            SetupGridWrap(grid);

            Render();
        }

        static void SetupGridWrap(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.contentContainer.style.flexDirection = FlexDirection.Row;
            scroll.contentContainer.style.flexWrap = Wrap.Wrap;
        }

        public void Render()
        {
            var save = meta?.Data;
            if (save == null || grid == null) return;
            grid.Clear();

            var boosts = db.boosts.Where(b => b != null).OrderBy(b => (int)b.rarity).ToList();
            Rarity? lastRarity = null;
            foreach (var boost in boosts)
            {
                if (lastRarity != boost.rarity)
                {
                    AddRarityHeader(grid, boost.rarity);
                    lastRarity = boost.rarity;
                }

                bool owned = save.ownedBoosts.Contains(boost.id);
                string slotKey = boost.slot.ToString().ToLowerInvariant();
                bool equipped = owned && save.equippedBoosts.TryGetValue(slotKey, out string equippedId) && equippedId == boost.id;

                // Point 11 : même modèle visuel que les masques/costumes (point 10) — cellule
                // colonne transparente : cercle à bordure de rareté + nom + stat + bouton d'action.
                var tile = new VisualElement();
                tile.AddToClassList("gridcell");

                var circle = new VisualElement();
                circle.AddToClassList("acctile");
                circle.AddToClassList("acc-round");
                circle.AddToClassList(RoundRarityClass(boost.rarity));
                circle.EnableInClassList("active", equipped);
                circle.EnableInClassList("owned", owned);
                if (boost.icon != null)
                {
                    var img = new Image { sprite = boost.icon };
                    img.AddToClassList("acctile-img");
                    circle.Add(img);
                }
                else
                {
                    // Glyphe vectoriel (les emojis 🦺/👢/🎖 ne se rendent pas avec Barlow).
                    circle.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                        BoostIconType(boost.slot), RarityColorOf(boost.rarity), 48f));
                }
                if (equipped)
                {
                    var check = new Label("✓");
                    check.AddToClassList("acc-check");
                    circle.Add(check);
                }
                tile.Add(circle);

                var name = new Label(boost.displayName);
                name.AddToClassList("gridcell-name");
                tile.Add(name);

                var stat = new Label(StatText(boost));
                stat.AddToClassList("gridcell-stat");
                tile.Add(stat);

                var capturedId = boost.id;
                var capturedSlot = boost.slot;
                var capturedPrice = boost.price;
                var action = new Button();
                action.AddToClassList("tile-action");
                if (equipped)
                {
                    action.text = "✓ Équipé";
                    action.AddToClassList("on");
                }
                else if (owned)
                {
                    action.text = "ÉQUIPER";
                }
                else
                {
                    action.text = $"{capturedPrice} CR"; // pas de 🪙 : emoji non rendu par Barlow
                    action.AddToClassList("buy");
                    action.SetEnabled(save.money >= capturedPrice);
                }
                // Ré-équiper/déséquiper (owned) et acheter (non owned) sont deux chemins distincts —
                // ToggleBoost gère à la fois l'équipement, le remplacement de slot occupé et le
                // déséquipement au re-clic (parité `equipBoost`, main.js:3614-3619).
                action.clicked += () =>
                {
                    if (meta?.Data == null) return;
                    bool changed = owned
                        ? ShopRules.ToggleBoost(meta.Data, capturedId, capturedSlot)
                        : ShopRules.BuyBoost(meta.Data, capturedId, capturedPrice);
                    if (changed) { meta.Persist(); Render(); }
                };
                tile.Add(action);

                grid.Add(tile);
            }
        }

        // web: statTxt (main.js:1526) — mêmes 3 formulations, brief Task 14 précise l'espace avant %.
        static string StatText(BoostData b) => b.stat switch
        {
            BoostStat.Health => $"+{b.bonus:0} Vie",
            BoostStat.Speed => $"+{Mathf.RoundToInt(b.bonus * 100)} % Vit.",
            BoostStat.Regen => $"+{b.bonus:0} Régén",
            _ => "",
        };

        // web: boostIcon (main.js:1447) — glyphe vectoriel en repli faute de sprite dédié (les
        // emojis d'origine ne se rendent pas avec Barlow) : gilet=cube, bottes=coureur, médaille=trophée.
        static Game.Runtime.UI.Tactical.TacticalIconType BoostIconType(BoostSlot slot) => slot switch
        {
            BoostSlot.Vest => Game.Runtime.UI.Tactical.TacticalIconType.Cube,
            BoostSlot.Boots => Game.Runtime.UI.Tactical.TacticalIconType.Runner,
            BoostSlot.Medal => Game.Runtime.UI.Tactical.TacticalIconType.Trophy,
            _ => Game.Runtime.UI.Tactical.TacticalIconType.Ring,
        };

        // Point 10/11 : même convention de classe rar-* que ShopAccessoriesTab (dupliquée, pas de
        // classe de base partagée entre onglets boutique).
        static string RoundRarityClass(Rarity r) => r switch
        {
            Rarity.Rare => "rar-rare",
            Rarity.Epic => "rar-epic",
            Rarity.Legendary => "rar-legendary",
            _ => "rar-common",
        };

        // web: RARITIES[key].color (main.js:83-86) — même table que les autres onglets boutique.
        static Color RarityColorOf(Rarity r) => r switch
        {
            Rarity.Common => new Color(0.604f, 0.639f, 0.698f),
            Rarity.Rare => new Color(0.310f, 0.616f, 1f),
            Rarity.Epic => new Color(0.690f, 0.400f, 1f),
            Rarity.Legendary => new Color(1f, 0.690f, 0.180f),
            _ => new Color(0.604f, 0.639f, 0.698f),
        };

        // Playtest Phase 5.1 (2026-07-09) : meme en-tete de groupe que ShopAccessoriesTab.AddRarityHeader
        // (pas de classe de base partagee entre les onglets boutique, dupliquee a l'identique).
        static void AddRarityHeader(ScrollView grid, Rarity rarity)
        {
            // Point 11 : même en-tête encadré + ligne que ShopAccessoriesTab.AddRarityHeader.
            var row = new VisualElement();
            row.AddToClassList("rarity-header-row");
            var color = RarityColorOf(rarity);
            var header = new Label(RarityName(rarity).ToUpperInvariant());
            header.AddToClassList("rarity-header-label");
            header.style.color = color;
            header.style.borderTopColor = color;
            header.style.borderBottomColor = color;
            header.style.borderLeftColor = color;
            header.style.borderRightColor = color;
            row.Add(header);
            var line = new VisualElement();
            line.AddToClassList("rarity-header-line");
            line.style.backgroundColor = color;
            row.Add(line);
            grid.Add(row);
        }

        // web: RARITIES (main.js:82-87) — même convention dupliquée que les autres onglets boutique.
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
    }
}
