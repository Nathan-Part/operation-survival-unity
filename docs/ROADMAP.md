# ROADMAP — le plan complet du projet, du début à la sortie

> **C'est LE fichier de suivi unique du projet** (avec `CLAUDE.md` pour le contexte des sessions Claude). Les anciens `docs/migration-unity.md` et `unity-spec/NEXT-STEPS.md` ont été fusionnés ici (2026-07-07) — ils n'existent plus.

**Le projet en une phrase :** porter le jeu de tir web (Three.js, presque fini) vers **Unity C#** pour une sortie mobile **Android + iOS**, avec du solo (vagues d'ennemis) puis du multijoueur (coop, puis PvP), niveau visuel visé « Guns of Boom ».

**Principe directeur :** le jeu web sert de **spécification exacte** (formules, équilibrage, IA, économie). On ne redesigne rien, on transpose. Ses données de gameplay sont extraites dans `unity-spec/*.json` — c'est la source de vérité, on ne relit jamais `main.js` pour deviner une valeur.

---

## 🎮 Comment tester le jeu aujourd'hui

1. Ouvrir Unity sur ce projet.
2. Panneau **Project** → `Assets/_Game/Scenes/` → double-cliquer **Slice**.
3. Appuyer sur **Play** (▶).
4. Contrôles clavier (alignés sur le jeu web) : **ZQSD** bouger · **souris** viser (le tir est automatique quand le viseur est sur un ennemi) · **E ou clic gauche** tir manuel · **Q ou clic droit** viser (sight ; lunette plein écran au sniper) · **V** vue 1ʳᵉ/3ᵉ personne · **Shift ou R** courir · **Espace** sauter · **C** dash · **F** recharger.
5. Les vagues s'enchaînent toutes seules. Vague 1 = 3 tireurs, vague 2 = des rushers au corps-à-corps, vague 5 = le boss.

Si la scène est cassée ou supprimée : menu **Tools → Game → Build Slice Scene** la regénère entièrement. Si les données JSON changent : menu **Tools → Game → Import Spec from JSON** regénère les fiches de données.

Sur mobile/tactile (Device Simulator ou build Android) : joystick virtuel à gauche, glisser à droite pour viser, boutons Saut/Dash/Recharge.

---

## 📍 Où on en est (résumé)

- ✅ Phase 1 faite (projet Unity créé, données importées).
- ✅ Phase 2 faite dans sa version minimale et **mergée dans `main`** : le jeu est jouable.
- ✅ **Phase 3, chantiers 1 et 2 FAITS (2026-07-07, branche `feat/phase3-anims-player-hud`, en attente du test visuel de Nathan)** : ennemis animés (idle/course/attaque/**mort** avec cadavre conservé jusqu'à la vague suivante), vue 1ʳᵉ/3ᵉ personne (V), visée Q (sight + lunette sniper), tir manuel E, arme en main (1ʳᵉ ET 3ᵉ personne + ennemis), muzzle flash/tracers/recul/kick caméra, mort du joueur animée, HUD de combat parité web (hitmarker, kill feed, barres de vie ennemis, barre boss, indicateurs directionnels, blood screen/heal flash, callouts, sight/scope). 129 tests EditMode + 4 PlayMode verts. Spec/plan : `docs/superpowers/{specs,plans}/2026-07-07-*`.
- ✅ **Reste de la Phase 2 FAIT (2026-07-07)** : comportements spéciaux des 10 types, VIP doré, combo (détail dans la Phase 3 ci-dessous). 140 tests EditMode + 5 PlayMode verts.
- ✅ **Chantier 4, première passe FAITE (2026-07-07)** : scène **Utopia** jouable (GLB pleine qualité depuis le .blend, colliders, NavMesh, smoke test vert). Reste : lightmaps/post-pro/LOD + dette perf des colliders. **Urban bloquée** (pas de .blend).
- ⚠️ **Chantier 3 bloqué** : les dossiers `perso/enemie-*` du web sont **vides** (modèles à produire, Phase 0).
- ⬅️ **Prochaine action : Nathan vérifie visuellement tout ça à son réveil (liste ci-dessous), pendant ce temps on attaque la Phase 4 (systèmes méta : sauvegarde, économie/boutique, progression…)** — brainstorm + spec + plan dédiés, comme d'habitude.

---

## Les 8 phases, du début à la fin

> Restructuré le 2026-07-07 avec Nathan : l'ancienne « Phase 4 » est divisée en deux (systèmes puis interface), la Phase 3 est réordonnée (animations d'abord, maps en dernier, **portal abandonnée**), et la **reproduction à l'identique de la page d'accueil web** devient un jalon central du projet. Chaque phase aura sa propre spec + plan au moment de l'attaquer.

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

### Phase 3 — Faire vivre le jeu : animations d'abord, maps en dernier ⬅️ PROCHAINE PHASE

*Ordre voulu : du plus léger au plus lourd. Le jeu web (`C:\wamp64\www\projet\games`) est la référence exacte du comportement attendu.*

1. ✅ **Animations des ennemis** (FAIT 2026-07-07) : clips Meshy copiés du web (`Assets/_Game/Art/Animations/`, rigs 100 % compatibles — spike validé), lecteur Playables (`AnimationDriver` + `CrossfadeModel`), FSM étendue d'un état **Dead** (anim de mort 0.18 s, collider off, pose figée, cadavre purgé à la vague suivante), `AnimationLibrary` remplie par Assign Imported Art.
2. ✅ **Côté joueur, comme sur le web** (FAIT 2026-07-07) : mort animée en 3ᵉ personne + écran différé, arme en main (avatar + ennemis tireurs), viewmodel 1ʳᵉ personne avec recul, muzzle flash, tracers, kick caméra, locomotion animée de l'avatar.
   - ✅ **Vue commutable 1ʳᵉ/3ᵉ personne (touche V)** — avatar visible et animé en 3ᵉ (le réglage persistant arrivera avec l'écran réglages, phase 4).
   - ✅ **Visée manuelle** : Q = viser (sight + lunette sniper plein écran + FOV 55→42→20), E = tir manuel — coexiste avec le tir auto mobile.
   - ✅ HUD de cette phase : hitmarker (variante headshot), kill feed, barres de vie au-dessus des ennemis, barre de boss, indicateurs directionnels de dégâts (4 secteurs), effets écran (sang/soin), callouts (HEADSHOT/DOUBLE/TRIPLE/MULTI KILL/BOSS DOWN).
3. **Un modèle par type d'ennemi** au fur et à mesure : copier le GLB du web → le glisser dans la fiche `EnemyData` (ou via `Tools → Game → Assign Imported Art`). ⚠️ **BLOQUÉ (constaté 2026-07-07) : les dossiers `perso/enemie-*` du jeu web sont vides** — ces modèles restent à produire (Phase 0, côté web). Dès qu'un GLB apparaît là-bas, le copier dans `Assets/_Game/Art/Enemies/` et étendre la table de `AssignImportedArt`. En attendant, tous les types utilisent `enemie-generic.glb` (animé).
4. **Les maps** : **utopia puis urban — portal est ABANDONNÉE (décision actée 2026-07-07)**.
   - ✅ **Utopia, première passe (FAIT 2026-07-07)** : `map_utopia_export.blend` exporté en GLB pleine qualité via Blender CLI (`Assets/_Game/Art/Maps/utopia.glb`, textures embarquées), scène **`Assets/_Game/Scenes/Utopia.unity`** générée par **Tools → Game → Build Utopia Scene** (MeshColliders sur chaque mesh, NavMesh bakée, 6 spawns, gameplay complet via `GameSceneKit` partagé avec la Slice). Smoke test PlayMode vert (le jeu tourne sur la map).
   - 💤 **Reste de la passe qualité maps (plus tard dans la phase)** : éclairage cuit (lightmaps + probes) + post-processing URP, LOD/occlusion culling. ⚠️ **Dette perf connue : 39 043 MeshColliders** (un par mesh de la map) — à simplifier (colliders convexes/primitifs ou mesh de collision dédié dans le .blend) pendant la passe perf mobile.
   - ⛔ **Urban : bloquée** — pas de `.blend` urban sur la machine (seule `map_utopia` existe dans `Desktop\3D\`).

*(✅ Reste de la Phase 2 FAIT (2026-07-07) : comportements spéciaux des 10 types (bomber explose au contact/mort, toxic/cryo zones au sol, medic soigne à 5 m, shielder bloque le cône frontal ±60°, grenadier grenades en cloche, commander aura ×1.2, stunner ralentit le joueur, berserker frenzy sous 25 % PV, ghost semi-transparent), VIP doré (vagues ≥ 3 hors boss, argent ×2), combo (fenêtre 3.5 s, mult ≤ 5, HUD dédié) — valeurs exactes du web, spec `docs/superpowers/specs/2026-07-07-enemy-specials-vip-combo-design.md`. Restent pour la phase 4 : killstreak rewards (#58, couplés au bouclier), changement d'arme, ramassage G, decoy/renforts, audio.)*

**Jalon :** jouer des vagues d'ennemis **animés**, arme en main, sur utopia — visuel nettement au-dessus du web.

### Phase 4 — Systèmes méta : toute la LOGIQUE du solo complet 🔜

*Première moitié de l'ancienne « Phase 4 », divisée en deux (décision 2026-07-07) : d'abord la mécanique, ensuite l'interface (Phase 5) — parce que reproduire l'accueil sans la logique derrière donnerait des boutons vides.*

- **Sauvegarde** : structure du save web (`cod-save-v1`) → JSON local (solo), cloud plus tard (multi).
- **Économie/boutique (logique)** : catalogues, coûts croissants, achats, upgrades — formules déjà importées.
- **Progression** : XP, niveaux, rangs, succès, contrats quotidiens, stats lifetime, prestige.
- **Gameplay annexe** : consommables/grenades, power-ups complets, mêlée joueur, chien de guerre, loadout, killstreaks.
- **Audio** : la synthèse Web Audio du jeu web → AudioSources Unity (+ touche M mute).
- **Ce que l'analyse du web a révélé en plus (2026-07-07, à ne pas oublier)** : ramassage d'armes sur les cadavres (G), modes de tir auto/burst/semi (T/P), bouclier (barre séparée de la vie), pause (Échap) + game over + réglages en jeu (sensibilité/volume/difficulté/hardcore/pluie/vue), modes **hardcore** et **entraînement**, adrénaline/surchauffe/longshot/précision (mécaniques annexes visibles au HUD), tourelle/claymore/frappe aérienne avec ciblage sur carte/caisse de ravitaillement (= le loadout), barre de vie du chien.
- **HUD de cette phase** : chaque fonctionnalité arrive avec son élément de HUD identique au web (stocks consommables, jauges loadout, XP/niveau, argent, combo/killstreak, toasts succès/défis/level-up) — voir la checklist « Parité HUD & contrôles ».
- Extraction JSON restante (même méthode que les 7 JSON existants) : succès, contrats, killstreaks, stats consommables, chien (pet.js), prestige.
- ⚠️ Marquer ce qui est solo-only (slow-mo, invincibilité… toxiques en PvP) : tables `PvE` et `PvP` séparées.

**Jalon :** toute la logique du solo complet existe et est testée (même si l'interface finale n'est pas encore là).

### Phase 5 — L'interface : reproduction EXACTE de la page d'accueil web ⭐ 🔜

**Décision actée (2026-07-07, très important pour Nathan) : la page d'accueil du jeu web est reproduite À L'IDENTIQUE — mêmes boutons, mêmes fonctionnalités, même comportement, tout pareil.** La référence : l'accueil du jeu web (`C:\wamp64\www\projet\games`, HTML/CSS dans `styles/` et `src/`).

- Étape 1 du brainstorm de cette phase : **inventaire exhaustif écran par écran, bouton par bouton** de l'accueil web (jouer, boutique, persos, armes, loadout, réglages, stats, succès, contrats, danses… tout), pour en faire la checklist de parité.
- Refaire en **UI Toolkit**, branché sur les systèmes de la Phase 4.
- Vérification de parité **bouton par bouton** contre le web avant de clore la phase.

**Jalon :** un joueur du jeu web retrouve exactement son accueil sur mobile → **c'est la v1 publiable**.

### Phase 6 — Multijoueur 🔜

Netcode : **Photon Fusion** (décidé, rien à coder avant cette phase). Dans l'ordre :

1. **Coop contre vagues** (2–4 joueurs) : l'hôte fait autorité sur les ennemis.
2. **PvP** (deathmatch par équipes) : prédiction client + compensation de lag (fournies par Fusion), matchmaking/lobbies, **rééquilibrage obligatoire** (le TTK actuel est pensé contre des bots), anti-triche (dégâts validés côté serveur).

La save passe côté serveur (sinon triche triviale). Options : Unity Cloud Save, PlayFab, Firebase.

**Jalon :** un match PvP complet entre deux téléphones réels.

### Phase 7 — Qualité & sortie 🔜

- **Tests** : porter les invariants critiques de la suite Playwright du web (jamais d'argent négatif, XP monotone, vagues jamais vides, armure torse vs intégrale…).
- **Perf** : Profiler, cible 60 fps sur un Android milieu de gamme.
- **Distribution** : Play Console (internal testing → prod), TestFlight → App Store (~1 semaine d'allers-retours avec Apple la première fois).
- Monétisation éventuelle : à décider AVANT la sortie (ça influence l'économie).

**Jalon :** le jeu est sur les stores. 🎉

**Ordre global : le solo complet (phases 2–5) sort AVANT le réseau (phase 6).**

---

## 🎯 Parité HUD & contrôles (décision actée 2026-07-07 — s'applique aux phases 3 à 5)

**Le HUD en jeu et les touches clavier doivent être EXACTEMENT comme le jeu web** (relevé fait dans `game.html` + `main.js` du web — c'est la checklist de parité, à re-vérifier contre le web au moment de chaque phase).

### Touches clavier (identiques au web, pour tout tester en éditeur)

| Touche | Action web |
|---|---|
| ZQSD/WASD + flèches | se déplacer |
| Shift **ou R** | courir |
| Espace | sauter |
| **E** | tirer (manuel) |
| **Q (A sur AZERTY)** | viser (réticule/lunette) |
| F | recharger |
| C | dash |
| X | mêlée |
| G | ramasser l'arme d'un cadavre |
| B (maintien) | arc de grenade · N flashbang · H fumigène · J collante · K molotov · O EMP |
| Z | trousse de soin |
| T | mode auto/burst · P semi-auto |
| Y / L / U / I / 4 | loadout : claymore / tourelle / frappe aérienne / caisse / chien |
| 1 / 2 / 3 | danses |
| **V** | **changer de vue 1ʳᵉ/3ᵉ personne** |
| M | couper le son · Échap : pause / fermer les panneaux |

⚠️ **Compatibilité avec le tir auto mobile (décision D1)** : le tir auto reste le mode mobile ; au clavier, E = tir manuel et Q = visée **comme le web**. Les deux coexistent (l'abstraction « déclencheur de tir » était prévue pour ça).

### HUD en jeu (inventaire relevé du web — tout doit y être à terme)

- **Vitals** : barre de vie, **bouclier** (barre séparée), stamina, barre d'XP + niveau.
- **Tir** : munitions + barre de rechargement + flash « chargeur vide », **réticule, viseur quand on vise (sight) et lunette sniper (scope + overlay)**, hitmarker, indicateur de mode de tir (auto/burst/semi).
- **Combat** : kill feed, annonces de kill (callout), combo, killstreak + bannière, **barres de vie au-dessus des ennemis**, barre de vie du **boss**, barre de vie du **chien**, indicateurs directionnels de dégâts (4 directions), effets écran (sang, flash de soin, flashbang, vignette).
- **Vagues** : n° de vague + bannière + compte à rebours d'intermission, compteur d'ennemis restants, radar.
- **Ressources** : argent, stocks des 6 consommables + medkit, 5 jauges de loadout, jauge de dash, power-ups actifs, invite « ramasser l'arme » (G).
- **Toasts** : succès, défis/contrats, level-up. **Écrans** : pause (Échap : reprendre/recommencer), game over (score/record, rejouer/menu), réglages en jeu (sensibilité, volume, difficulté, hardcore, pluie, **vue**).

*(Le HUD actuel de la slice — vie/stamina/munitions/vague/radar/mort — est la v0 de cette liste ; le reste arrive avec les fonctionnalités correspondantes, phases 3 et 4.)*

## ✋ TODO manuels ouverts (côté Nathan)

0bis. **Vérifier la map Utopia** (scène `Assets/_Game/Scenes/Utopia.unity`, Play) : la map s'affiche avec ses textures, on marche dessus sans passer à travers, les ennemis naviguent, l'échelle paraît correcte (1 unité = 1 m ; si c'est géant/minuscule → me le dire, on corrigera l'échelle d'export). Échéant : orientation, position de départ du joueur (actuellement le centre de la map), les 6 spawns.
0ter. **Vérifier les comportements spéciaux** (scène Slice ou Utopia) : bomber explose au contact (dégâts + sphère orange), zones vertes (poison, dégâts périodiques) et bleues (gel, ralentit) à la mort des toxic/cryo, le shielder bloque de face (« BLOQUÉ! »), le grenadier lance des grenades en cloche, un ennemi doré (VIP) apparaît dès la vague 3 (argent ×2, « VIP ÉLIMINÉ! »), le combo s'affiche à droite en enchaînant les kills. NB : ces types apparaissent au fil des vagues (composition `waves.json`).
0. **Vérifier visuellement la Phase 3 chantiers 1–2** (branche `feat/phase3-anims-player-hud`, scène Slice, Play) :
   - Les ennemis **marchent/courent** vers toi, jouent une **anim d'attaque**, et à leur mort jouent l'**anim de mort** puis restent au sol (cadavre non ciblable, purgé à la vague suivante).
   - **V** bascule 1ʳᵉ/3ᵉ personne : en 3ᵉ on voit le soldat bleu animé avec l'**AR-15 en main** ; en 1ʳᵉ l'arme est visible en bas à droite.
   - **Q** (ou clic droit) : le réticule devient un viseur, le FOV zoome ; **E** (ou clic gauche) tire même sans ennemi sous le viseur.
   - À chaque tir : **flash de bouche + tracer + recul** de l'arme + petit kick caméra. Les ennemis tireurs ont aussi leur flash.
   - HUD : **hitmarker** en touchant (rouge si tête), **kill feed** en haut à droite, **barres de vie** au-dessus des ennemis (< 20 m), **barre de boss** à la vague 5, **indicateurs rouges directionnels** quand on te tire dessus, **voile de sang** sur gros dégâts, **callouts** (HEADSHOT, DOUBLE KILL…).
   - Mort du joueur : bascule 3ᵉ personne + anim de mort, puis écran « Vous êtes mort » ~1,6 s après.
   - Pour tester la **lunette sniper** : dans `SliceBootstrap.cs`, remplacer temporairement l'id d'arme `"default"` par `"sniper"` (ligne du `w.id == "default"`), Play, puis Q maintenu.
1. ~~Tester la slice puis merger `feat/vertical-slice-solo`~~ ✅ mergée dans `main`.
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

Unity 6 + C#, URP (pas Built-in/HDRP/Unreal) · Android + iOS · solo d'abord (v1 publiable), puis coop, puis PvP · Photon Fusion pressenti (phase 6) · tir automatique façon Guns of Boom (bouton manuel possible plus tard en option) · uGUI pour le HUD in-game, UI Toolkit pour les menus/accueil (phase 5) · **la page d'accueil web est reproduite à l'identique — boutons, fonctionnalités, comportement (phase 5)** · **le HUD en jeu et les touches clavier sont identiques au web** (section « Parité HUD & contrôles ») · **vue 1ʳᵉ/3ᵉ personne commutable (V) comme le web** · **maps : utopia + urban seulement, portal abandonnée** · dans la phase 3 : animations d'abord, maps en dernier · le code web = spec fonctionnelle, pas une base à traduire ligne à ligne.

## 🗄️ Historique des livraisons

| Date | Quoi | Où |
|---|---|---|
| 2026-07-06 | Pipeline d'import JSON → ScriptableObjects (91 assets, 31 tests) | mergé dans `main` |
| 2026-07-06 | Vertical slice solo jouable (83 + 2 tests) | mergé dans `main` |
| 2026-07-07 | Phase 3 chantiers 1–2 : animations ennemis + mort, vues V, visée Q/tir E, arme en main, feedback tir, HUD combat parité web (129 + 4 tests) | branche `feat/phase3-anims-player-hud` |

Détails d'archive : specs dans `docs/superpowers/specs/`, plans dans `docs/superpowers/plans/`, journal de l'import dans `.superpowers/sdd/progress.md`, dette technique de l'import dans `docs/superpowers/tech-debt-import-pipeline.md`.
