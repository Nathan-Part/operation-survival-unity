using Game.Data.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Stocks des 7 consommables (haut-gauche, parité web hud.png) : une ligne icone+quantite par
    /// consommable, lue chaque frame depuis le save persistant (cout nul, valeurs rares).
    /// Playtest Phase 5.1 (2026-07-09) : remplace l'ancienne ligne unique "B:14  N:8  H:5 ..." (relevee
    /// par Nathan comme un libelle de debug, cf. bug-entity.png) par des lignes lisibles empilees.
    public class HudConsumables : MonoBehaviour
    {
        static readonly (string key, string glyph, string label)[] Slots =
        {
            ("frag", "💣", "B"), ("flashbang", "⚡", "N"), ("smoke", "💨", "H"), ("sticky", "🧷", "J"),
            ("molotov", "🔥", "K"), ("emp", "🌀", "O"), ("medkit", "❤", "Z")
        };

        MetaProgress meta;
        ConsumableThrower thrower; // conservé pour usage futur (ex: griser un slot sans stock)
        Text[] rows;

        public void Init(RectTransform root, MetaProgress meta, ConsumableThrower thrower)
        {
            this.meta = meta;
            this.thrower = thrower;

            const float rowHeight = 26f, startY = -84f; // sous le bloc credits (voir HudMeta)
            rows = new Text[Slots.Length];
            for (int i = 0; i < Slots.Length; i++)
            {
                var text = HudBuild.MakeText(root, "", 18, TextAnchor.UpperLeft, $"Consumable_{Slots[i].key}");
                var r = text.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(24f, startY - i * rowHeight);
                r.sizeDelta = new Vector2(160f, rowHeight);
                rows[i] = text;
            }
        }

        void Update()
        {
            if (meta?.Data == null) return;
            for (int i = 0; i < Slots.Length; i++)
            {
                meta.Data.consumables.TryGetValue(Slots[i].key, out int n);
                rows[i].text = $"{Slots[i].glyph} {n}";
            }
        }
    }
}
