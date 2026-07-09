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

                var tile = new VisualElement();
                tile.AddToClassList("acctile");
                tile.AddToClassList("boosttile");
                tile.EnableInClassList("active", equipped);
                tile.EnableInClassList("owned", owned);

                var name = new Label(boost.displayName);
                name.AddToClassList("acctile-name");
                tile.Add(name);

                var thumb = new VisualElement();
                thumb.AddToClassList("acctile-thumb");
                if (boost.icon != null)
                {
                    var img = new Image { sprite = boost.icon };
                    img.AddToClassList("acctile-img");
                    thumb.Add(img);
                }
                else
                {
                    var glyph = new Label(BoostGlyph(boost.slot));
                    glyph.AddToClassList("acctile-glyph");
                    thumb.Add(glyph);
                }
                tile.Add(thumb);

                var stat = new Label(StatText(boost));
                stat.AddToClassList("acctile-stat");
                tile.Add(stat);

                var foot = new VisualElement();
                foot.AddToClassList("acctile-foot");
                var rarityLabel = new Label(RarityName(boost.rarity));
                rarityLabel.AddToClassList("rarity-badge");
                rarityLabel.AddToClassList("acctile-rarity");
                ApplyRarityClass(rarityLabel, boost.rarity);
                foot.Add(rarityLabel);
                tile.Add(foot);

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
                    action.text = "Équiper";
                }
                else
                {
                    action.text = $"🪙{capturedPrice}";
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

        // web: boostIcon (main.js:1447) — glyphes emoji en repli faute de sprite dédié (idem
        // ShopAccessoriesTab pour masque/costume/danses).
        static string BoostGlyph(BoostSlot slot) => slot switch
        {
            BoostSlot.Vest => "🦺",
            BoostSlot.Boots => "👢",
            BoostSlot.Medal => "🎖️",
            _ => "❔",
        };

        // Playtest Phase 5.1 (2026-07-09) : meme en-tete de groupe que ShopAccessoriesTab.AddRarityHeader
        // (pas de classe de base partagee entre les onglets boutique, dupliquee a l'identique).
        static void AddRarityHeader(ScrollView grid, Rarity rarity)
        {
            var header = new Label(RarityName(rarity).ToUpperInvariant());
            header.AddToClassList("rarity-group-header");
            ApplyRarityClass(header, rarity);
            grid.Add(header);
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
