using System.Linq;
using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Rendu + actions de l'onglet Personnage de la boutique (Task 12) : délégué par
    /// ShopScreenController comme ShopWeaponsTab (Task 11) — parité web `renderShop` branche
    /// `shopTab==='player'` (main.js:1501-1520) + upgrades joueur `upgRow2`/`PLAYER_UPGRADE_SPECS`
    /// (main.js:1441-1446, 3548-3551).
    ///
    /// Différence clé avec l'onglet Armes : pas de pivot 3D dédié — le perso prévisualisé est
    /// affiché directement sur `LobbyCharacterStage` (le même GameObject que le lobby, comme
    /// demandé par la tâche : "essai visuel" façon web, PAS le perso réellement équipé tant que
    /// "Équiper" n'a pas été cliqué). ShopScreenController.Close() se charge de remettre le stage
    /// sur le perso réellement équipé à la fermeture de la boutique, quel que soit l'onglet actif.
    ///
    /// Les upgrades joueur (Vie/Régén/Vitesse) sont globales au compte (pas par personnage —
    /// `ShopRules.PlayerUpgradeLevel(save, stat)`, confirmé indépendant par personnage ET par
    /// stat via `save.playerUpgrades["health"|"regen"|"speed"]`) : le bouton "★ Améliorer" est
    /// donc toujours visible, jamais masqué par la possession du perso actuellement parcouru
    /// (contrairement à l'onglet Armes où "Améliorer" n'a de sens que pour une arme possédée) —
    /// parité web l.1516 : pas de condition `owned` sur le bouton ameliorer côté perso.
    public class ShopCharacterTab
    {
        readonly GameDatabase db;
        readonly MetaProgress meta;
        readonly LobbyCharacterStage stage;

        VisualElement content;
        Label cname, rarityBadge, cdesc;
        VisualElement statHealthFill, statRegenFill, statSpeedFill;
        Label statHealthValue, statRegenValue, statSpeedValue;
        VisualElement upgradesContainer;
        Button ameliorerButton, buyButton, equipButton;
        Label equippedBadge;
        ScrollView carousel;

        string heroCharacterId;
        bool ameliorerOpen;

        public ShopCharacterTab(GameDatabase database, MetaProgress metaProgress, LobbyCharacterStage lobbyStage)
        {
            db = database;
            meta = metaProgress;
            stage = lobbyStage;
        }

        public void Wire(VisualElement tabContent)
        {
            content = tabContent;
            if (content == null) return;

            cname = content.Q<Label>("shop-cname");
            rarityBadge = content.Q<Label>("shop-crarity-badge");
            cdesc = content.Q<Label>("shop-cdesc");

            statHealthFill = content.Q<VisualElement>("stat-chealth-fill");
            statRegenFill = content.Q<VisualElement>("stat-cregen-fill");
            statSpeedFill = content.Q<VisualElement>("stat-cspeed-fill");
            statHealthValue = content.Q<Label>("stat-chealth-value");
            statRegenValue = content.Q<Label>("stat-cregen-value");
            statSpeedValue = content.Q<Label>("stat-cspeed-value");

            upgradesContainer = content.Q<VisualElement>("shop-cupgrades");
            ameliorerButton = content.Q<Button>("shop-cameliorer");
            buyButton = content.Q<Button>("shop-cbuy");
            equipButton = content.Q<Button>("shop-cequip");
            equippedBadge = content.Q<Label>("shop-cequipped-badge");
            carousel = content.Q<ScrollView>("shop-char-carousel");

            ameliorerButton.clicked += () => { ameliorerOpen = !ameliorerOpen; Render(); };
            buyButton.clicked += () =>
            {
                var c = FindCharacter(heroCharacterId);
                if (c == null || meta?.Data == null) return;
                if (ShopRules.BuyCharacter(meta.Data, c.id, c.price)) { meta.Persist(); Render(); }
            };
            equipButton.clicked += () =>
            {
                if (heroCharacterId == null || meta?.Data == null) return;
                if (ShopRules.SelectCharacter(meta.Data, heroCharacterId)) { meta.Persist(); Render(); }
            };

            SelectByEquipped();
        }

        CharacterData FindCharacter(string id) => System.Array.Find(db.characters, c => c != null && c.id == id);

        string EquippedId(SaveData save) => !string.IsNullOrEmpty(save?.selectedCharacterId)
            ? save.selectedCharacterId
            : (db.characters.Length > 0 ? db.characters[0].id : null);

        /// Point d'entrée à chaque activation de l'onglet (pas seulement au premier Wire()) : repart
        /// toujours du perso réellement équipé, jamais d'un aperçu laissé par la visite précédente
        /// — parité web (switchTab, main.js:1426 : `setShopChar` rejoué à chaque entrée sur l'onglet).
        public void SelectByEquipped()
        {
            var save = meta?.Data;
            string equipped = EquippedId(save);
            int index = System.Array.FindIndex(db.characters, c => c != null && c.id == equipped);
            SelectCharacter(index < 0 ? 0 : index);
        }

        // web: setShopChar (implicite via shopCharIndex) — index avec wraparound, reset shopAmeliorerOpen.
        public void SelectCharacter(int index)
        {
            if (db.characters.Length == 0) { heroCharacterId = null; Render(); return; }
            int n = db.characters.Length;
            index = ((index % n) + n) % n;
            heroCharacterId = db.characters[index]?.id;
            ameliorerOpen = false;
            // Essai visuel immédiat (perso lobby, pas encore équipé tant que "Équiper" n'est pas cliqué).
            if (stage != null && heroCharacterId != null) stage.ShowCharacter(heroCharacterId);
            Render();
        }

        public void Prev() => SelectCharacter(CurrentIndex() - 1);
        public void Next() => SelectCharacter(CurrentIndex() + 1);

        int CurrentIndex()
        {
            int idx = System.Array.FindIndex(db.characters, c => c != null && c.id == heroCharacterId);
            return idx < 0 ? 0 : idx;
        }

        public void Render()
        {
            var save = meta?.Data;
            if (save == null || content == null || db.characters.Length == 0) return;

            var c = FindCharacter(heroCharacterId) ?? db.characters.FirstOrDefault(x => x != null);
            if (c == null)
            {
                cname.text = "—"; cdesc.text = ""; rarityBadge.text = "";
                carousel.Clear();
                return;
            }
            heroCharacterId = c.id;

            bool owned = save.ownedCharacters.Contains(c.id);
            bool equipped = EquippedId(save) == c.id;

            cname.text = c.displayName;
            rarityBadge.text = RarityName(c.rarity);
            ApplyRarityClass(rarityBadge, c.rarity);
            cdesc.text = c.desc;

            // web: statBar('Vie', c.base.health/250*100, ...) etc. (main.js:1510) — le web utilise des
            // dénominateurs fixes ; la tâche demande explicitement une normalisation sur le max du
            // catalogue (3 persos) plutôt que ces constantes web arbitraires (simplification assumée,
            // documentée dans le rapport de tâche).
            float maxHealth = db.characters.Where(x => x != null).Select(x => x.baseHealth).DefaultIfEmpty(1f).Max();
            float maxRegen = db.characters.Where(x => x != null).Select(x => x.baseRegen).DefaultIfEmpty(1f).Max();
            float maxSpeed = db.characters.Where(x => x != null).Select(x => x.baseSpeed).DefaultIfEmpty(1f).Max();
            if (maxHealth <= 0f) maxHealth = 1f;
            if (maxRegen <= 0f) maxRegen = 1f;
            if (maxSpeed <= 0f) maxSpeed = 1f;

            SetBar(statHealthFill, statHealthValue, c.baseHealth / maxHealth, Mathf.RoundToInt(c.baseHealth).ToString());
            SetBar(statRegenFill, statRegenValue, c.baseRegen / maxRegen, Mathf.RoundToInt(c.baseRegen).ToString());
            SetBar(statSpeedFill, statSpeedValue, c.baseSpeed / maxSpeed, $"{c.baseSpeed:F2}×");

            // Pas de condition `owned` ici (voir commentaire de tête de classe) : les upgrades
            // joueur sont globales au compte, pas au perso actuellement parcouru dans le carrousel.
            ameliorerButton.text = ameliorerOpen ? "★ Améliorer ▲" : "★ Améliorer";
            equippedBadge.style.display = equipped ? DisplayStyle.Flex : DisplayStyle.None;
            equipButton.style.display = (owned && !equipped) ? DisplayStyle.Flex : DisplayStyle.None;
            buyButton.style.display = owned ? DisplayStyle.None : DisplayStyle.Flex;
            if (!owned)
            {
                buyButton.text = $"Acheter 🪙{c.price}";
                buyButton.SetEnabled(save.money >= c.price); // web l.1519 : disabled si save.money < price
            }

            upgradesContainer.style.display = ameliorerOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (ameliorerOpen) RenderUpgradeRows(save);

            RenderCarousel(c.id);
        }

        static void SetBar(VisualElement fill, Label value, float normalized, string text)
        {
            fill.style.width = new Length(Mathf.Clamp(normalized * 100f, 0f, 100f), LengthUnit.Percent);
            value.text = text;
        }

        // web: upgRow2 (main.js:1441-1446) + PLAYER_UPGRADE_SPECS (main.js:3548-3551) — 3 lignes
        // GENUINEMENT indépendantes (ShopRules.PlayerUpgradeLevel/BuyPlayerUpgrade prennent bien un
        // paramètre `stat`, contrairement au niveau unique partagé des upgrades d'arme — voir
        // ShopWeaponsTab.RenderUpgradeRows et le Finding 2 de la revue Task 11).
        void RenderUpgradeRows(SaveData save)
        {
            upgradesContainer.Clear();
            if (db.upgradeConfig == null) return;

            foreach (var spec in db.upgradeConfig.playerUpgrades)
            {
                if (spec == null) continue;

                var row = new VisualElement();
                row.AddToClassList("upg-row");

                var name = new Label(spec.displayName);
                name.AddToClassList("upg-name");
                row.Add(name);

                var effect = new Label(EffectText(spec));
                effect.AddToClassList("upg-effect");
                row.Add(effect);

                int level = ShopRules.PlayerUpgradeLevel(save, spec.statKey);
                var levelLabel = new Label($"{level}/{spec.maxLevel}");
                levelLabel.AddToClassList("upg-level");
                row.Add(levelLabel);

                bool atMax = level >= spec.maxLevel;
                int cost = ShopRules.NextPlayerUpgradeCost(save, spec.statKey, spec.baseCost);
                var buy = new Button { text = atMax ? "MAX" : $"🪙{cost}" };
                buy.AddToClassList("upg-buy");
                buy.SetEnabled(!atMax && save.money >= cost);
                var statKey = spec.statKey; // capture locale (évite la variable de boucle partagée)
                var baseCost = spec.baseCost;
                var maxLevel = spec.maxLevel;
                buy.clicked += () =>
                {
                    if (ShopRules.BuyPlayerUpgrade(save, statKey, baseCost, maxLevel))
                    {
                        meta.Persist();
                        Render();
                    }
                };
                row.Add(buy);

                upgradesContainer.Add(row);
            }
        }

        // web: perLevel additif pour Vie/Régén (main.js:3623-3624 : + perLevel*level), multiplicatif
        // pour Vitesse (main.js:3625 : speedMult = base × (1 + perLevel*level)) — donc affiché en %
        // pour la vitesse (comme les boosts stat, main.js:1526 : `+${round(bonus*100)}% Vit.`).
        static string EffectText(PlayerUpgradeSpec spec) => spec.statKey == "speed"
            ? $"+{Mathf.RoundToInt(spec.perLevel * 100f)}%"
            : $"+{Mathf.RoundToInt(spec.perLevel)}";

        // web: carrousel perso (#25) — carte icône (nom + vignette + cadenas si non possédé), pas de
        // rating ★ (les upgrades joueur sont globales au compte, pas par personnage, voir plus haut :
        // contrairement au carrousel d'armes, une carte perso n'a pas de "niveau" propre à afficher).
        void RenderCarousel(string heroId)
        {
            carousel.Clear();
            var save = meta.Data;

            foreach (var candidate in db.characters)
            {
                if (candidate == null) continue;
                bool active = candidate.id == heroId;
                bool owned = save.ownedCharacters.Contains(candidate.id);

                var card = new Button();
                card.AddToClassList("wcard");
                card.EnableInClassList("active", active);
                card.EnableInClassList("owned", owned);
                // Parite shop-perso.png : le perso selectionne est en pleine taille/opacite, les
                // voisins sont estompes (pas de nouveau rendu 3D cote a cote, cf. plan Task 7).
                card.style.opacity = active ? 1f : 0.45f;
                card.style.scale = new Scale(active ? Vector3.one : new Vector3(0.85f, 0.85f, 1f));

                var top = new VisualElement(); top.AddToClassList("wcard-top");
                var name = new Label(candidate.displayName); name.AddToClassList("wcard-name");
                top.Add(name);
                card.Add(top);

                var thumb = new VisualElement(); thumb.AddToClassList("wcard-thumb");
                var sprite = candidate.ResolveIcon(db.placeholders);
                if (sprite != null)
                {
                    var img = new Image { sprite = sprite };
                    img.AddToClassList("thumb-img");
                    thumb.Add(img);
                }
                else
                {
                    var glyph = new Label("🧍");
                    glyph.AddToClassList("wcard-glyph");
                    thumb.Add(glyph);
                }
                card.Add(thumb);

                var foot = new VisualElement(); foot.AddToClassList("wcard-foot");
                if (!owned)
                {
                    var lock_ = new Label("🔒"); lock_.AddToClassList("wcard-lock");
                    foot.Add(lock_);
                }
                card.Add(foot);

                var capturedId = candidate.id; // capture locale
                card.clicked += () =>
                {
                    int idx = System.Array.FindIndex(db.characters, x => x != null && x.id == capturedId);
                    if (idx >= 0) SelectCharacter(idx);
                };

                carousel.Add(card);
            }
        }

        // web: RARITIES (main.js:82-87) — mêmes noms français que ShopWeaponsTab (dupliqué ici :
        // classe indépendante, même convention que Task 11).
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
