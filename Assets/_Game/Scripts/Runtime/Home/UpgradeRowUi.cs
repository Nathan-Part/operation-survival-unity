using System;
using Game.Runtime.UI.Tactical;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Ligne du sous-panneau d'amélioration (parité amelioration.png) : nom de la stat,
    /// pastilles carrées de niveau (remplie = acquis), bouton violet « ⊕ prix » (ou MAX).
    /// Partagée par l'onglet Armes (4 stats) et l'onglet Personnage (3 stats).
    public static class UpgradeRowUi
    {
        public static VisualElement BuildRow(string displayName, int level, int maxLevel, int cost, bool canAfford, Action onBuy)
        {
            var row = new VisualElement();
            row.AddToClassList("upg-row");

            var name = new Label(displayName);
            name.AddToClassList("upg-name");
            row.Add(name);

            var pips = new VisualElement();
            pips.AddToClassList("upg-pips");
            for (int i = 0; i < maxLevel; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("upg-pip");
                pip.EnableInClassList("filled", i < level);
                pips.Add(pip);
            }
            row.Add(pips);

            bool atMax = level >= maxLevel;
            var buy = new Button();
            buy.AddToClassList("upg-buy");
            if (atMax) buy.text = "MAX";
            else
            {
                // ⊕ vectoriel (les glyphes/emoji ne se rendent pas avec Barlow) + montant.
                buy.Add(TacticalIcon.Create(TacticalIconType.Plus, Color.white, 12f));
                var amount = new Label(cost.ToString());
                amount.AddToClassList("upg-buy-amount");
                buy.Add(amount);
            }
            buy.SetEnabled(!atMax && canAfford);
            if (onBuy != null) buy.clicked += onBuy;
            row.Add(buy);

            return row;
        }
    }
}
