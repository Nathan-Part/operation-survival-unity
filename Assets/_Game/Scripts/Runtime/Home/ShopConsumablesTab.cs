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
            // Playtest Phase 5.1 (2026-07-09) : un seul en-tete "COMMUN" par coherence visuelle avec
            // les autres onglets groupes par rarete (ShopAccessoriesTab/ShopBoostTab) — tous les
            // consommables sont commun (pas de vraie variete de rarete ici, cf. commentaire ci-dessus).
            var consumableList = db.consumables.Where(c => c != null).ToList();
            if (consumableList.Count > 0)
            {
                // Point 11 : en-tête au même format que les autres grilles (label encadré + ligne).
                var row = new VisualElement();
                row.AddToClassList("rarity-header-row");
                var gray = new Color(0.604f, 0.639f, 0.698f);
                var header = new Label("COMMUN");
                header.AddToClassList("rarity-header-label");
                header.style.color = gray;
                header.style.borderTopColor = gray;
                header.style.borderBottomColor = gray;
                header.style.borderLeftColor = gray;
                header.style.borderRightColor = gray;
                row.Add(header);
                var line = new VisualElement();
                line.AddToClassList("rarity-header-line");
                line.style.backgroundColor = gray;
                row.Add(line);
                grid.Add(row);
            }
            foreach (var item in consumableList)
            {
                save.consumables.TryGetValue(item.key, out int stock);

                // Point 11 : même modèle circulaire que les autres grilles (tous les consommables
                // sont "commun" — web main.js:1543 — donc bordure grise commune, jamais de verrou).
                var tile = new VisualElement();
                tile.AddToClassList("gridcell");

                var circle = new VisualElement();
                circle.AddToClassList("acctile");
                circle.AddToClassList("acc-round");
                circle.AddToClassList("rar-common");
                circle.AddToClassList("owned"); // toujours pleine opacité (achat direct, sans possession préalable)
                if (item.icon != null)
                {
                    var img = new Image { sprite = item.icon };
                    img.AddToClassList("acctile-img");
                    circle.Add(img);
                }
                else
                {
                    // Glyphe vectoriel générique (les emojis 💣/⚡/… ne se rendent pas avec Barlow) :
                    // cible pour les grenades, croix médicale (Plus) pour le medkit.
                    circle.Add(Game.Runtime.UI.Tactical.TacticalIcon.Create(
                        item.iconName == "heart" ? Game.Runtime.UI.Tactical.TacticalIconType.Plus
                                                 : Game.Runtime.UI.Tactical.TacticalIconType.Target,
                        new Color(0.604f, 0.639f, 0.698f), 48f));
                }
                tile.Add(circle);

                var name = new Label(item.displayName);
                name.AddToClassList("gridcell-name");
                tile.Add(name);

                var stockLabel = new Label($"Possédé : {stock}");
                stockLabel.AddToClassList("gridcell-stat");
                tile.Add(stockLabel);

                var capturedKey = item.key;
                var capturedPrice = item.price;
                var buy = new Button { text = $"{capturedPrice} CR" }; // pas de 🪙 : emoji non rendu par Barlow
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

    }
}
