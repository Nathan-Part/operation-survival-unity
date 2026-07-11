using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.Rules;
using Game.Runtime.UI.Tactical;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Rendu + actions de l'onglet Armes de la boutique (Task 11) : délégué par ShopScreenController
    /// pour que ce dernier reste concentré sur le shell (rail d'onglets, topbar, ouverture/fermeture)
    /// — parité web `renderShop` branche `shopTab==='weapons'` (main.js:1465-1500) + `weaponCard`
    /// (main.js:1449-1460) pour les cartes du carrousel + `setShopWeapon` (main.js:1346-1354) pour
    /// la navigation Précédent/Suivant et le filtre par catégorie.
    ///
    /// Carrousel : cartes-icône (pas de modèle 3D par carte) — c'est en réalité la parité EXACTE du
    /// web : `weaponCard()` (main.js:1449-1460) affiche lui-même une icône/vignette statique, jamais
    /// un modèle 3D tournant par carte (le web réserve le modèle 3D tournant à l'arme "héros" au
    /// centre — `shopWeaponObj`, main.js:1357-1358 — via `SHOP_CENTER_POS`). Seule l'arme héros a un
    /// modèle 3D tournant ici aussi (ShopScreenController.Update, 0.8 rad/s — main.js:5450).
    public class ShopWeaponsTab
    {
        // web: WCATS (main.js:1467) — ordre + libellés figés, la disponibilité ne dépend que du
        // catalogue db.weapons (aucune catégorie n'est masquée/désactivée).
        static readonly (WeaponCategory? category, string label)[] Categories =
        {
            (null, "Tout"),
            (WeaponCategory.Assault, "Assaut"),
            (WeaponCategory.Smg, "SMG"),
            (WeaponCategory.Sniper, "Sniper"),
            (WeaponCategory.Shotgun, "Pompe"),
            (WeaponCategory.Pistol, "Pistol"),
            (WeaponCategory.Lmg, "LMG"),
            (WeaponCategory.Melee, "Mêlée"),
        };

        readonly GameDatabase db;
        readonly MetaProgress meta;
        readonly Transform weaponPivot;

        VisualElement content;
        // Clé = CatKey(cat) (string), pas WeaponCategory? directement : Dictionary<TKey,TValue> lève
        // ArgumentNullException sur une clé null même quand TKey est un Nullable<T> (comportement
        // .NET connu — la vérification interne teste `key == null`, qui est vrai pour un
        // WeaponCategory? sans valeur ("Tout")).
        readonly Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();

        Label wname, rarityBadge, rating, wdesc;
        VisualElement statDamageFill, statFireRateFill, statMagFill, statStabilityFill, statRangeFill;
        Label statDamageValue, statFireRateValue, statMagValue, statStabilityValue, statRangeValue;
        VisualElement upgradesContainer;
        Button ameliorerButton, buyButton, equipButton;
        Label equippedBadge;
        ScrollView carousel;

        WeaponCategory? activeCategory; // null = "Tout"
        string heroWeaponId;
        string builtHeroVisualId; // évite de détruire/recréer le modèle héros à chaque Render()
        bool ameliorerOpen;
        GameObject heroVisual;

        public ShopWeaponsTab(GameDatabase database, MetaProgress metaProgress, Transform pivot)
        {
            db = database;
            meta = metaProgress;
            weaponPivot = pivot;
        }

        public void Wire(VisualElement tabContent)
        {
            content = tabContent;
            if (content == null) return;

            foreach (var (cat, _) in Categories)
            {
                var button = content.Q<Button>($"shop-wcat-{CatKey(cat)}");
                if (button == null) continue;
                categoryButtons[CatKey(cat)] = button;
                var captured = cat; // capture locale (évite la variable de boucle partagée)
                button.clicked += () => SelectCategory(captured);
            }

            wname = content.Q<Label>("shop-wname");
            rarityBadge = content.Q<Label>("shop-rarity-badge");
            rating = content.Q<Label>("shop-rating");
            wdesc = content.Q<Label>("shop-wdesc");

            statDamageFill = content.Q<VisualElement>("stat-damage-fill");
            statFireRateFill = content.Q<VisualElement>("stat-firerate-fill");
            statMagFill = content.Q<VisualElement>("stat-mag-fill");
            statStabilityFill = content.Q<VisualElement>("stat-stability-fill");
            statRangeFill = content.Q<VisualElement>("stat-range-fill");
            statDamageValue = content.Q<Label>("stat-damage-value");
            statFireRateValue = content.Q<Label>("stat-firerate-value");
            statMagValue = content.Q<Label>("stat-mag-value");
            statStabilityValue = content.Q<Label>("stat-stability-value");
            statRangeValue = content.Q<Label>("stat-range-value");

            upgradesContainer = content.Q<VisualElement>("shop-upgrades");
            ameliorerButton = content.Q<Button>("shop-ameliorer");
            buyButton = content.Q<Button>("shop-buy");
            equipButton = content.Q<Button>("shop-equip");
            equippedBadge = content.Q<Label>("shop-equipped-badge");
            carousel = content.Q<ScrollView>("shop-carousel");

            ameliorerButton.clicked += () => { ameliorerOpen = !ameliorerOpen; Render(); };
            buyButton.clicked += () =>
            {
                var w = FindWeapon(heroWeaponId);
                if (w == null || meta?.Data == null) return;
                if (ShopRules.BuyWeapon(meta.Data, w.id, w.price)) { meta.Persist(); Render(); }
            };
            equipButton.clicked += () =>
            {
                if (heroWeaponId == null || meta?.Data == null) return;
                if (ShopRules.EquipWeapon(meta.Data, heroWeaponId)) { meta.Persist(); Render(); }
            };

            SelectWeaponByHero();
        }

        static string CatKey(WeaponCategory? cat) => cat == null ? "all" : cat.Value.ToString().ToLowerInvariant();

        List<WeaponData> Filtered() => activeCategory == null
            ? db.weapons.Where(w => w != null).ToList()
            : db.weapons.Where(w => w != null && w.category == activeCategory.Value).ToList();

        WeaponData FindWeapon(string id) => System.Array.Find(db.weapons, w => w != null && w.id == id);

        // web: b.onclick = () => { shopWeaponCategory = b.dataset.wcat; setShopWeapon(0); } (main.js:1613-1616)
        void SelectCategory(WeaponCategory? cat)
        {
            activeCategory = cat;
            SelectWeapon(0);
        }

        /// Sélectionne l'arme actuellement équipée si possible (état initial à l'ouverture), sinon
        /// la première de la catégorie active — pas de round-trip web exact ici (le web garde
        /// `shopWeaponIndex` en mémoire entre ouvertures), simplification assumée pour l'ouverture.
        void SelectWeaponByHero()
        {
            var save = meta?.Data;
            var fw = Filtered();
            int index = save != null ? fw.FindIndex(w => w.id == save.selectedWeapon) : -1;
            SelectWeapon(index < 0 ? 0 : index);
        }

        // web: setShopWeapon (main.js:1346-1348) — index avec wraparound, reset shopAmeliorerOpen.
        public void SelectWeapon(int index)
        {
            var fw = Filtered();
            if (fw.Count == 0) { heroWeaponId = null; Render(); return; }
            int n = fw.Count;
            index = ((index % n) + n) % n;
            heroWeaponId = fw[index].id;
            ameliorerOpen = false;
            Render();
        }

        public void Prev() => SelectWeapon(CurrentIndex() - 1);
        public void Next() => SelectWeapon(CurrentIndex() + 1);

        int CurrentIndex()
        {
            var fw = Filtered();
            int idx = fw.FindIndex(w => w.id == heroWeaponId);
            return idx < 0 ? 0 : idx;
        }

        public void Render()
        {
            var save = meta?.Data;
            if (save == null || content == null) return;

            string activeCatKey = CatKey(activeCategory);
            foreach (var kv in categoryButtons) kv.Value.EnableInClassList("on", kv.Key == activeCatKey);

            var fw = Filtered();
            var w = FindWeapon(heroWeaponId) ?? fw.FirstOrDefault();
            if (w == null)
            {
                // Catégorie sans arme (ne devrait pas arriver avec les 15 armes du catalogue couvrant
                // toutes les catégories) : contenu minimal, jamais de plantage.
                wname.text = "—"; wdesc.text = ""; rarityBadge.text = ""; rating.text = "";
                carousel.Clear();
                return;
            }
            heroWeaponId = w.id;

            bool owned = save.ownedWeapons.Contains(w.id);
            bool equipped = save.selectedWeapon == w.id;
            // Point 3 feedback n°3 : 4 niveaux indépendants par stat (parité web weaponUpgrades[arme][stat]).
            var levels = ShopRules.WeaponUpgradeLevelsOf(save, w.id);
            int totalLevel = ShopRules.WeaponUpgradeTotal(save, w.id);

            wname.text = w.displayName.ToUpperInvariant(); // parité shop-guns.png : titres en capitales
            rarityBadge.text = RarityName(w.rarity).ToUpperInvariant();
            ApplyRarityClass(rarityBadge, w.rarity);
            wdesc.text = w.desc;

            // web main.js:1452 : rating ★ = somme des 4 niveaux / (4 stats × 5) — « x/20 ».
            int weaponUpgradeMaxTotal = db.upgradeConfig != null
                ? db.upgradeConfig.weaponUpgrades.Sum(s => s.maxLevel)
                : 0;
            rating.text = $"★ {totalLevel}/{weaponUpgradeMaxTotal}";

            SetBar(statDamageFill, statDamageValue, WeaponStatsView.Damage(w, levels.damage), WeaponStatsView.DamageRaw(w, levels.damage).ToString());
            SetBar(statFireRateFill, statFireRateValue, WeaponStatsView.FireRate(w, levels.fireRate), WeaponStatsView.FireRateRaw(w, levels.fireRate).ToString());
            SetBar(statMagFill, statMagValue, WeaponStatsView.MagSize(w, levels.mag), WeaponStatsView.MagSizeRaw(w, levels.mag).ToString());
            SetBar(statStabilityFill, statStabilityValue, WeaponStatsView.Stability(w, levels.recoil), WeaponStatsView.StabilityRaw(w, levels.recoil).ToString("F1"));
            SetBar(statRangeFill, statRangeValue, WeaponStatsView.Range(w), $"{Mathf.RoundToInt(WeaponStatsView.RangeRaw(w))}m");

            ameliorerButton.style.display = owned ? DisplayStyle.Flex : DisplayStyle.None;
            ameliorerButton.text = ameliorerOpen ? "★ AMÉLIORER ▲" : "★ AMÉLIORER";
            equippedBadge.style.display = equipped ? DisplayStyle.Flex : DisplayStyle.None;
            equipButton.style.display = (owned && !equipped) ? DisplayStyle.Flex : DisplayStyle.None;
            buyButton.style.display = owned ? DisplayStyle.None : DisplayStyle.Flex;
            if (!owned)
            {
                buyButton.text = $"ACHETER · {w.price} CR"; // pas de 🪙 : emoji non rendu par Barlow
                buyButton.SetEnabled(save.money >= w.price); // web l.1496 : disabled si save.money < price
            }

            bool upgradesVisible = owned && ameliorerOpen;
            upgradesContainer.style.display = upgradesVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (upgradesVisible) RenderUpgradeRows(save, w);

            RenderCarousel(fw, w.id);
            UpdateHeroVisual(w);
        }

        static void SetBar(VisualElement fill, Label value, float normalized, string text)
        {
            fill.style.width = new Length(Mathf.Clamp(normalized * 100f, 0f, 100f), LengthUnit.Percent);
            value.text = text;
        }

        // web renderShop (main.js:1490-1494) : une ligne par stat de UPGRADE_SPECS, chacune avec
        // son propre niveau et son propre coût — 4 progressions GENUINEMENT indépendantes depuis
        // le point 3 du feedback n°3 (ShopRules/SaveData par stat). Le visuel pastilles+prix
        // (parité amelioration.png) arrive en Task 4 du plan — ici, rangées sobres par stat.
        void RenderUpgradeRows(Game.Data.Save.SaveData save, WeaponData w)
        {
            upgradesContainer.Clear();
            if (db.upgradeConfig == null) return;

            foreach (var spec in db.upgradeConfig.weaponUpgrades)
            {
                if (spec == null) continue;
                var statKey = spec.statKey; // captures locales (évite la variable de boucle partagée)
                var baseCost = spec.baseCost;
                var maxLevel = spec.maxLevel;
                int level = ShopRules.WeaponUpgradeLevel(save, w.id, statKey);

                var row = new VisualElement();
                row.AddToClassList("upg-row");

                var name = new Label(spec.displayName);
                name.AddToClassList("upg-name");
                row.Add(name);

                var levelLabel = new Label($"{level}/{maxLevel}");
                levelLabel.AddToClassList("upg-level");
                row.Add(levelLabel);

                bool atMax = level >= maxLevel;
                int cost = ShopRules.NextWeaponUpgradeCost(save, w.id, statKey, baseCost);
                var buy = new Button { text = atMax ? "MAX" : $"{cost} CR" };
                buy.AddToClassList("upg-buy");
                buy.SetEnabled(!atMax && save.money >= cost);
                buy.clicked += () =>
                {
                    if (ShopRules.BuyWeaponUpgrade(save, w.id, statKey, baseCost, maxLevel))
                    {
                        meta.Persist();
                        Render();
                    }
                };
                row.Add(buy);

                upgradesContainer.Add(row);
            }
        }

        // web: weaponCard (main.js:1449-1460) — carte icône (pas de modèle 3D par carte, voir
        // commentaire en tête de fichier), clic = arme héros (main.js:1633-1636, data-wcard).
        void RenderCarousel(List<WeaponData> fw, string heroId)
        {
            carousel.Clear();
            var save = meta.Data;
            // web weaponCard (main.js:1452) : rating de carte = somme des 4 stats / 20.
            int weaponUpgradeMaxTotal = db.upgradeConfig != null
                ? db.upgradeConfig.weaponUpgrades.Sum(s => s.maxLevel)
                : 0;

            foreach (var candidate in fw)
            {
                bool active = candidate.id == heroId;
                bool owned = save.ownedWeapons.Contains(candidate.id);
                int lvl = ShopRules.WeaponUpgradeTotal(save, candidate.id);

                var card = new Button();
                card.AddToClassList("wcard");
                card.EnableInClassList("active", active);
                card.EnableInClassList("owned", owned);

                var top = new VisualElement(); top.AddToClassList("wcard-top");
                var name = new Label(candidate.displayName.ToUpperInvariant()); name.AddToClassList("wcard-name");
                top.Add(name);
                var rarityLabel = new Label(RarityName(candidate.rarity).ToUpperInvariant());
                rarityLabel.AddToClassList("wcard-rarity");
                ApplyRarityClass(rarityLabel, candidate.rarity);
                top.Add(rarityLabel);
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
                    // Glyphe vectoriel teinté par rareté (parité shop-guns.png — les emojis 🔫/🗡 ne se rendent pas).
                    thumb.Add(TacticalIcon.Create(candidate.melee ? TacticalIconType.Blade : TacticalIconType.Gun,
                        RarityColor(candidate.rarity), 44f));
                }
                card.Add(thumb);

                var foot = new VisualElement(); foot.AddToClassList("wcard-foot");
                var ratingLabel = new Label($"★ {lvl}/{weaponUpgradeMaxTotal}"); ratingLabel.AddToClassList("wcard-rating");
                foot.Add(ratingLabel);
                if (!owned)
                {
                    foot.Add(TacticalIcon.Create(TacticalIconType.Lock, new Color(0.6f, 0.63f, 0.7f), 14f));
                }
                card.Add(foot);

                var capturedId = candidate.id; // capture locale
                card.clicked += () =>
                {
                    var list = Filtered();
                    int idx = list.FindIndex(x => x.id == capturedId);
                    if (idx >= 0) SelectWeapon(idx);
                };

                carousel.Add(card);
            }
        }

        // web: ShowCharacter détruit/instancie le GLB (LobbyCharacterStage.cs:35-42) — même approche
        // ici pour l'arme héros, mais seulement quand l'arme sélectionnée change réellement (pas à
        // chaque Render(), sinon le pivot repartirait de zéro à chaque clic sur ★ Améliorer).
        void UpdateHeroVisual(WeaponData w)
        {
            if (weaponPivot == null || w.id == builtHeroVisualId) return;
            builtHeroVisualId = w.id;

            if (heroVisual != null) Object.Destroy(heroVisual);

            // Phase 5 bis (point 8) : uniquement le VRAI modèle de l'arme (modelPrefab assigné) —
            // pas de capsule de secours ni de placeholder : arme sans modèle = vitrine vide,
            // comme demandé au playtest 2026-07-10 (la logique ne plante pas pour autant).
            if (w.modelPrefab == null) { heroVisual = null; return; }

            var instance = Object.Instantiate(w.modelPrefab, weaponPivot);
            instance.transform.localScale = Vector3.one * w.modelScale;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            heroVisual = instance;
        }

        // web: RARITIES (main.js:82-87) — noms/couleurs français ; les couleurs sont aussi déjà
        // exposées côté USS (HomeTheme.uss --rarity-*), réutilisées via ApplyRarityClass ci-dessous.
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

        static Color RarityColor(Rarity r) => r switch // web: RARITIES[key].color (main.js:83-86)
        {
            Rarity.Common => new Color(0.604f, 0.639f, 0.698f),
            Rarity.Rare => new Color(0.310f, 0.616f, 1f),
            Rarity.Epic => new Color(0.690f, 0.400f, 1f),
            Rarity.Legendary => new Color(1f, 0.690f, 0.180f),
            _ => new Color(0.604f, 0.639f, 0.698f),
        };
    }
}
