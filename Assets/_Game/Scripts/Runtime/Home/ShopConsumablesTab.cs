using System.Linq;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Rendu + actions de l'onglet Consommables de la boutique (Task 14) : délégué par
    /// ShopScreenController comme les autres onglets — parité web `renderShop` branche
    /// 'consumables' (main.js:1538-1544). Grille des 7 `ConsumableData` : stock persistant acheté
    /// à l'unité (#155), pas de notion de possédé/équipé (contrairement à Boost/Accessoires) — un
    /// seul bouton d'achat par tuile, jamais désactivé par un plafond (`maxStock` sert seulement le
    /// jeu en cours, pas la boutique — ShopRules.BuyConsumable ne le vérifie pas).
    public class ShopConsumablesTab
    {
        readonly GameDatabase db;
        readonly MetaProgress meta;

        VisualElement content;
        ScrollView grid;

        public ShopConsumablesTab(GameDatabase database, MetaProgress metaProgress)
        {
            db = database;
            meta = metaProgress;
        }

        public void Wire(VisualElement tabContent)
        {
            content = tabContent;
            if (content == null) return;

            grid = content.Q<ScrollView>("shop-consumables-grid");
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

            // web: pas de tri par rareté (CONSUMABLES est parcouru dans l'ordre déclaré, tous "commun").
            foreach (var item in db.consumables.Where(c => c != null))
            {
                save.consumables.TryGetValue(item.key, out int stock);

                var tile = new VisualElement();
                tile.AddToClassList("acctile");
                tile.AddToClassList("constile");
                tile.AddToClassList("owned"); // toujours pleine opacité : jamais verrouillé (achat direct, sans possession préalable)

                var name = new Label(item.displayName);
                name.AddToClassList("acctile-name");
                tile.Add(name);

                var thumb = new VisualElement();
                thumb.AddToClassList("acctile-thumb");
                if (item.icon != null)
                {
                    var img = new Image { sprite = item.icon };
                    img.AddToClassList("acctile-img");
                    thumb.Add(img);
                }
                else
                {
                    var glyph = new Label(ConsumableGlyph(item.iconName));
                    glyph.AddToClassList("acctile-glyph");
                    thumb.Add(glyph);
                }
                tile.Add(thumb);

                var stockLabel = new Label($"Possédé : {stock}");
                stockLabel.AddToClassList("acctile-stat");
                tile.Add(stockLabel);

                // web: itemCard force rarity:'commun' pour tous les consommables (main.js:1543) —
                // pas de champ rarity sur ConsumableData, badge fixe (parité visuelle avec les
                // autres grilles boutique plutôt qu'une omission).
                var foot = new VisualElement();
                foot.AddToClassList("acctile-foot");
                var rarityLabel = new Label("Commun");
                rarityLabel.AddToClassList("rarity-badge");
                rarityLabel.AddToClassList("acctile-rarity");
                rarityLabel.AddToClassList("rarity-common");
                foot.Add(rarityLabel);
                tile.Add(foot);

                var capturedKey = item.key;
                var capturedPrice = item.price;
                var buy = new Button { text = $"🪙{capturedPrice}" };
                buy.AddToClassList("tile-action");
                buy.AddToClassList("buy");
                buy.SetEnabled(save.money >= capturedPrice);
                buy.clicked += () =>
                {
                    if (ShopRules.BuyConsumable(meta.Data, capturedKey, capturedPrice)) { meta.Persist(); Render(); }
                };
                tile.Add(buy);

                grid.Add(tile);
            }
        }

        // web: CONSUMABLES icon (main.js:3659-3667) — glyphes emoji en repli faute de sprite dédié.
        static string ConsumableGlyph(string iconName) => iconName switch
        {
            "grenade" => "💣",
            "flash" => "⚡",
            "smoke" => "💨",
            "sticky" => "🧷",
            "molotov" => "🔥",
            "emp" => "🌀",
            "heart" => "❤️",
            _ => "❔",
        };
    }
}
