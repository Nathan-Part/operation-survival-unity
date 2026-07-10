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
        // Noms courts et pas des glyphes emoji : LegacyRuntime.ttf (police uGUI du HUD) ne contient
        // pas les emojis — ils rendaient en cases vides, ne laissant que des nombres nus a l'ecran
        // (constate sur capture, playtest 2026-07-10). Les vraies icones viendront avec les sprites.
        static readonly (string key, string glyph, string label)[] Slots =
        {
            ("frag", "Grenade", "B"), ("flashbang", "Flash", "N"), ("smoke", "Fumigène", "H"), ("sticky", "Collante", "J"),
            ("molotov", "Molotov", "K"), ("emp", "EMP", "O"), ("medkit", "Medkit", "Z")
        };

        MetaProgress meta;
        ConsumableThrower thrower; // conservé pour usage futur (ex: griser un slot sans stock)
        Text[] rows;

        public void Init(RectTransform root, MetaProgress meta, ConsumableThrower thrower)
        {
            this.meta = meta;
            this.thrower = thrower;

            // Parité hud2.png (point 12) : colonne de cartouches sombres sous le bloc crédits,
            // un par consommable (nom + compteur).
            const float rowHeight = 34f, gap = 6f, startY = -68f;
            rows = new Text[Slots.Length];
            for (int i = 0; i < Slots.Length; i++)
            {
                var cartouche = HudBuild.MakeCartouche(root, $"Consumable_{Slots[i].key}",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, startY - i * (rowHeight + gap)), new Vector2(130f, rowHeight));
                var text = HudBuild.MakeText(cartouche.rectTransform, "", 15, TextAnchor.MiddleLeft, "Label");
                var r = text.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.sizeDelta = new Vector2(-20f, 0f);
                r.anchoredPosition = new Vector2(10f, 0f);
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
