# ROADMAP — le plan complet du projet, du début à la sortie

> **C'est LE fichier de suivi unique du projet** (avec `CLAUDE.md` pour le contexte des sessions Claude). Les anciens `docs/migration-unity.md` et `unity-spec/NEXT-STEPS.md` ont été fusionnés ici (2026-07-07) — ils n'existent plus.

**Le projet en une phrase :** porter le jeu de tir web (Three.js, presque fini) vers **Unity C#** pour une sortie mobile **Android + iOS**, avec du solo (vagues d'ennemis) puis du multijoueur (coop, puis PvP), niveau visuel visé « Guns of Boom ».

**Principe directeur :** le jeu web sert de **spécification exacte** (formules, équilibrage, IA, économie). On ne redesigne rien, on transpose. Ses données de gameplay sont extraites dans `unity-spec/*.json` — c'est la source de vérité, on ne relit jamais `main.js` pour deviner une valeur.

---

## 🎮 Comment tester le jeu aujourd'hui

1. Ouvrir Unity sur ce projet.
2. Panneau **Project** → `Assets/_Game/Scenes/` → double-cliquer **Slice**.
3. Appuyer sur **Play** (▶).
4. Contrôles clavier : **ZQSD** bouger · **souris** viser (le tir est automatique quand le viseur est sur un ennemi) · **Shift** courir · **Espace** sauter · **Ctrl gauche** dash · **R** recharger.
5. Les vagues s'enchaînent toutes seules. Vague 1 = 3 tireurs, vague 2 = des rushers au corps-à-corps, vague 5 = le boss.

Si la scène est cassée ou supprimée : menu **Tools → Game → Build Slice Scene** la regénère entièrement. Si les données JSON changent : menu **Tools → Game → Import Spec from JSON** regénère les fiches de données.

Sur mobile/tactile (Device Simulator ou build Android) : joystick virtuel à gauche, glisser à droite pour viser, boutons Saut/Dash/Recharge.

---

## 📍 Où on en est (résumé)

- ✅ Phase 1 faite (projet Unity créé, données importées).
- ✅ Phase 2 faite dans sa version minimale : **le jeu est jouable** (branche `feat/vertical-slice-solo`, **pas encore mergée dans `main`** — en attente du test de Nathan).
- ⬅️ **Prochaine action : Nathan teste la slice, on merge, puis on décide : attaquer la Phase 3 (vraies maps + visuel) ou compléter d'abord la Phase 2 (comportements spéciaux des ennemis).**

---

## Les 7 phases, du début à la fin

### Phase 0 — Finir le jeu web ⏳ (en cours, ailleurs)

*Se passe dans le dépôt web, pas ici. Pour mémoire.*

Geler le design du jeu web pour que Unity ait une référence stable : refactors visuels, nouveaux modèles 3D (à faire directement « Unity-ready » : PBR metallic/roughness, 10–30k triangles/perso, textures ≤ 2K, rig humanoïde type Mixamo, garder les `.blend` propres). Si l'équilibrage change côté web → **mettre à jour les JSON d'ici**.

**Jalon :** jeu web fini, design gelé, tag git `v-web-final`.

### Phase 1 — Fondations Unity ✅ FAIT

- ✅ Projet Unity 6 + **URP** (le pipeline mobile), structure `Assets/_Game/{Scripts,Data,Prefabs,Scenes,Art,UI}`.
- ✅ **Import de la spec** (terminé 2026-07-06, mergé dans `main`) : menu `Tools/Game/Import Spec from JSON` → 91 fiches de données (armes, ennemis, persos, économie, items, power-ups, vagues) + `GameDatabase.asset` qui référence tout. L'import ne touche jamais aux champs d'art assignés à la main. 31 tests verts.
- Décision netcode prise d'avance : **Photon Fusion** (rien à coder avant la phase 5).
- 💤 Restera à faire plus tard dans cette phase : comptes stores (Google Play 25 $ une fois, Apple Developer 99 $/an — Mac requis pour builder iOS).

### Phase 2 — Version solo jouable minimale ✅ FAIT (version minimale, 2026-07-06)

*La « vertical slice » : branche `feat/vertical-slice-solo`, à merger après test de Nathan.*

- ✅ Contrôleur joueur FPS (`CharacterController`) : marche/course/stamina, saut, gravité, dash, dégâts de chute, « second wind » — toutes les valeurs depuis `MovementConfig` (issu de `player.json`).
- ✅ **Tir automatique** façon Guns of Boom (décision actée) : l'arme tire quand le viseur est sur un ennemi. Raycast, zones de dégâts tête/torse/jambes (60/25/15), armure torse/intégrale, pellets shotgun, rechargement.
- ✅ Ennemi générique **piloté par les données** (NavMesh + FSM idle/poursuite/attaque) : tous les types spawnent avec leurs stats (PV, vitesse, taille, dégâts, rafales, portées sniper) ; les tireurs tirent en rafales, les mêlées chargent au contact.
- ✅ Vagues fidèles à `waves.json` (composition exacte par vague, boss toutes les 5 vagues, PV qui montent en 1.18^vague).
- ✅ HUD (vie, stamina, munitions, vague + ennemis restants, radar coloré, écran de mort) + joysticks virtuels tactiles.
- ✅ 83 tests EditMode + 2 tests PlayMode verts.
- 💤 **Reste de la Phase 2 (différé volontairement)** : comportements spéciaux des types d'ennemis (bomber qui explose, medic qui soigne, shielder qui bloque de face, toxic/cryo qui laissent des zones, grenadier, commander qui buffe, ghost semi-transparent — pour l'instant ils utilisent leur archétype de base), ennemi VIP doré, combo/killstreaks, changement d'arme, audio.

**Jalon (atteint pour la version minimale) :** tuer des vagues d'ennemis. **Jalon complet :** pareil sur un téléphone à 60 fps.

### Phase 3 — Maps & vrai visuel ⬅️ PROBABLE PROCHAINE PHASE

- Import des maps **directement depuis les `.blend` de Nathan** (`Desktop\3D\map_utopia\map_utopia_export.blend`), ré-export pleine qualité FBX/glTF — **PAS les GLB du web** (volontairement dégradés).
- Colliders posés dans l'éditeur Unity (fini les tables de colliders en code).
- **Éclairage cuit** (lightmaps + light probes) + post-processing URP (bloom, AO, tone mapping) = le vrai bond visuel.
- LOD, occlusion culling. Ordre des maps : utopia d'abord, puis portal, urban.
- Modèles 3D persos/armes au fur et à mesure (en attendant : capsules colorées générées par le code + `PlaceholderLibrary`).

**Jalon :** utopia jouable en solo sur mobile, visuel nettement au-dessus du web.

### Phase 4 — Systèmes méta (le solo complet) 🔜

- **Sauvegarde** : structure du save web → JSON local (solo), cloud plus tard (multi).
- **Boutique/économie** : catalogues, coûts croissants, upgrades — formules déjà importées. UI en UI Toolkit.
- **Progression** : XP, niveaux, succès, contrats quotidiens, stats.
- **Gameplay annexe** : consommables, power-ups, grenades, mêlée joueur, chien de guerre, loadout, killstreaks.
- Extraction JSON restante à faire au fil de cette phase (même méthode que les 7 JSON existants) : succès, contrats, killstreaks, stats consommables, chien (pet.js), prestige.
- ⚠️ Marquer dès maintenant ce qui est solo-only (slow-mo, invincibilité… toxiques en PvP) : prévoir tables `PvE` et `PvP` séparées.

**Jalon :** parité fonctionnelle solo avec le jeu web → **c'est la v1 publiable**.

### Phase 5 — Multijoueur 🔜

Netcode : **Photon Fusion** (décidé). Dans l'ordre :

1. **Coop contre vagues** (2–4 joueurs) : l'hôte fait autorité sur les ennemis.
2. **PvP** (deathmatch par équipes) : prédiction client + compensation de lag (fournies par Fusion), matchmaking/lobbies, **rééquilibrage obligatoire** (le TTK actuel est pensé contre des bots), anti-triche (dégâts validés côté serveur).

La save passe côté serveur (sinon triche triviale). Options : Unity Cloud Save, PlayFab, Firebase.

**Jalon :** un match PvP complet entre deux téléphones réels.

### Phase 6 — Qualité & sortie 🔜

- **Tests** : porter les invariants critiques de la suite Playwright du web (jamais d'argent négatif, XP monotone, vagues jamais vides, armure torse vs intégrale…).
- **Perf** : Profiler, cible 60 fps sur un Android milieu de gamme.
- **Distribution** : Play Console (internal testing → prod), TestFlight → App Store (~1 semaine d'allers-retours avec Apple la première fois).
- Monétisation éventuelle : à décider AVANT la sortie (ça influence l'économie).

**Jalon :** le jeu est sur les stores. 🎉

**Ordre global : le solo complet (phases 2–4) sort AVANT le réseau.**

---

## ✋ TODO manuels ouverts (côté Nathan)

1. **Tester la slice** (voir « Comment tester » ci-dessus) puis donner le feu vert pour merger `feat/vertical-slice-solo` dans `main`.
2. **Vérifier 4 valeurs dans le `main.js` du web** (absentes des JSON, implémentées avec des défauts marqués `// TODO(spec-web)` dans le code) : multiplicateur de réduction d'armure (défaut 0.5), délai de régénération de vie après dégâts (défaut 3 s), réserve de munitions (défaut : infinie), portée du radar (défaut 20 m). Puis les reporter dans les JSON.
3. **Remplir le `PlaceholderLibrary`** (clic droit dans `Assets/_Game/Data/` → Create → Game → Config → Placeholder Library, puis glisser des prefabs/sprites dans l'Inspector). Non bloquant : le code génère des capsules colorées en secours.

## 📜 Règles à respecter (toutes sessions)

- **Ne jamais toucher au jeu web depuis ce projet** (il n'est pas dans ce dépôt et vit sa vie).
- Les JSON de `unity-spec/` sont la **source de vérité d'équilibrage**. Un changement côté web → mettre à jour les JSON.
- Les maps viennent des `.blend` de Nathan, jamais des GLB web.
- La logique n'exige jamais un asset d'art (placeholders partout, rien ne plante si un modèle manque).
- Identifiants/code en **anglais**, textes affichés en **français**.
- Vérification du code uniquement via le pont MCP Unity (l'éditeur doit être ouvert) : compilation `refresh_unity` + `read_console`, tests `run_tests`/`get_test_job`.

## ✅ Décisions actées (ne pas re-débattre)

Unity 6 + C#, URP (pas Built-in/HDRP/Unreal) · Android + iOS · solo d'abord (v1 publiable), puis coop, puis PvP · Photon Fusion pressenti · tir automatique façon Guns of Boom (bouton manuel possible plus tard en option) · uGUI pour le HUD in-game de la slice, UI Toolkit pour les menus/boutique en phase 4 · le code web = spec fonctionnelle, pas une base à traduire ligne à ligne.

## 🗄️ Historique des livraisons

| Date | Quoi | Où |
|---|---|---|
| 2026-07-06 | Pipeline d'import JSON → ScriptableObjects (91 assets, 31 tests) | mergé dans `main` |
| 2026-07-06 | Vertical slice solo jouable (83 + 2 tests) | branche `feat/vertical-slice-solo` |

Détails d'archive : specs dans `docs/superpowers/specs/`, plans dans `docs/superpowers/plans/`, journal de l'import dans `.superpowers/sdd/progress.md`, dette technique de l'import dans `docs/superpowers/tech-debt-import-pipeline.md`.
