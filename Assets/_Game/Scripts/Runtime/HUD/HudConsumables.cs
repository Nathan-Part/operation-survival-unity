using Game.Data.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Stocks des 7 consommables (bas-centre, parité web) : lettre + quantité,
    /// lu chaque frame depuis le save persistant (coût nul, valeurs rares).
    public class HudConsumables : MonoBehaviour
    {
        static readonly (string key, string label)[] Slots =
        {
            ("frag", "B"), ("flashbang", "N"), ("smoke", "H"), ("sticky", "J"),
            ("molotov", "K"), ("emp", "O"), ("medkit", "Z")
        };

        MetaProgress meta;
        ConsumableThrower thrower; // conservé pour usage futur (ex: griser un slot sans stock)
        Text line;

        public void Init(RectTransform root, MetaProgress meta, ConsumableThrower thrower)
        {
            this.meta = meta;
            this.thrower = thrower;
            line = HudBuild.MakeText(root, "", 22, TextAnchor.LowerCenter, "Consumables");
            var r = line.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, 120f);
            r.sizeDelta = new Vector2(900f, 30f);
        }

        void Update()
        {
            if (meta?.Data == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var s in Slots)
            {
                meta.Data.consumables.TryGetValue(s.key, out int n);
                sb.Append($"{s.label}:{n}  ");
            }
            line.text = sb.ToString();
        }
    }
}
