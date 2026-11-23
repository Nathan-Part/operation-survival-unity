# ROADMAP — le plan complet du projet, du début à la sortie

> **C'est LE fichier de suivi unique du projet**. Les anciens `docs/migration-unity.md` et `unity-spec/NEXT-STEPS.md` ont été fusionnés ici (2026-07-07) — ils n'existent plus.

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

**Depuis la Phase 5**, l'accueil réel est `Assets/_Game/Scenes/Home.unity` (première scène des Build Settings) : Play dessus pour tester le lobby → boutique → Déployer, qui enchaîne sur la map choisie (Utopia par défaut). `Tools → Game → Build Home Scene` la regénère entièrement. `Slice` reste l'outil de dev pour tester le gameplay brut sans passer par le menu.

---

## 📍 Où on en est (résumé)

- ✅ Phase 1 faite (projet Unity créé, données importées).
- ✅ Phase 2 faite dans sa version minimale et **mergée dans `main`** : le jeu est jouable.
- ✅ **Phase 3, chantiers 1 et 2 FAITS (2026-07-07, branche `feat/phase3-anims-player-hud`, en attente du test visuel de Nathan)** : ennemis animés (idle/course/attaque/**mort** avec cadavre conservé jusqu'à la vague suivante), vue 1ʳᵉ/3ᵉ personne (V), visée Q (sight + lunette sniper), tir manuel E, arme en main (1ʳᵉ ET 3ᵉ personne + ennemis), muzzle flash/tracers/recul/kick caméra, mort du joueur animée, HUD de combat parité web (hitmarker, kill feed, barres de vie ennemis, barre boss, indicateurs directionnels, blood screen/heal flash, callouts, sight/scope). 129 tests EditMode + 4 PlayMode verts. Spec/plan : `docs/superpowers/{specs,plans}/2026-07-07-*`.
- ✅ **Reste de la Phase 2 FAIT (2026-07-07)** : comportements spéciaux des 10 types, VIP doré, combo (détail dans la Phase 3 ci-dessous). 140 tests EditMode + 5 PlayMode verts.
- ✅ **Chantier 4, première passe FAITE (2026-07-07)** : scène **Utopia** jouable (GLB pleine qualité depuis le .blend, colliders, NavMesh, smoke test vert).
- ⚠️ **Chantier 3 bloqué** : les dossiers `perso/enemie-*` du web sont **vides** (modèles à produire, Phase 0).
- ✅✅✅ **PHASE 4 TERMINÉE À 100 % (2026-07-08, nuit du 07 au 08, branche `feat/phase4-systems` mergée dans `feat/phase3-anims-player-hud`)** — les 26 tâches du plan + 1 correctif imprévu (arme géante) + le correctif trouvé par la revue finale de branche, chacune implémentée puis relue par un agent indépendant. **220 tests EditMode verts (155→220), PlayMode 5/7 (2 ignorés, cause connue ci-dessous).** Détail complet dans `.superpowers/sdd/progress.md`. Livré :
  - **Lot 0 — bugs playtest** : marche/course clavier ✅, arme en main (bone + échelle) ✅ + **bug supplémentaire trouvé en live par Nathan** (arme géante — mesh `ar-15.glb` réellement ~2,1× trop grand à l'export, corrigé par un champ `WeaponData.modelScale`, stopgap en attendant un ré-export Blender propre), anim course vérifiée ✅ (pas de bug), collisions escaliers + spawns ennemis joignables ✅ **côté code** (garde anti-îlot livré) mais a révélé un problème de map plus grave (voir section Bugs, toujours ouvert, Blender).
  - **Lot 2 — consommables** : 6 grenades (frag/flashbang/fumigène/collante/molotov/EMP) + medkit, lancer balistique, dégâts de zone, HUD des stocks.
  - **Lot 3 — combat** : bouclier rechargeable, killstreaks 3/5/7, adrénaline/précision/longshot/surchauffe LMG.
  - **Lot 4 — loadout** : jauges 100 pts/kill, claymore, tourelle, frappe aérienne, caisse de ravitaillement, chien de guerre (FSM + NavMesh).
  - **Lot 5 — armes** : modes auto/burst/semi, mêlée (X), ramassage d'arme sur cadavre (G), 9 power-ups prioritaires (les 15 restants + 2 flags SOLO-ONLY à sweep sont des suivis notés).
  - **Lot 6 — méta** : 26 succès pilotés par données, 8 contrats quotidiens (tirage déterministe + migration de save sûre), rangs/prestige/leaderboard, suivi de partie + toasts.
  - **Lot 7 — modes** : difficulté/hardcore/entraînement consommés au lancement, menu pause (Échap) + mute (M), audio synthétisé (8 sons, parité `audio.js`).
  - **Suivis ouverts non bloquants** (voir `.superpowers/sdd/progress.md` pour le détail) : slowmo (power-up) sans effet ennemi, plusieurs compteurs `RunStats` d'succès jamais alimentés (perfectWaveStreak lifetime, distance, etc.), tag `// SOLO-ONLY` pas appliqué partout, un commentaire de code obsolète sur `incomingMult` — rien de bloquant, à sweep avant la Phase 6.
- ✅✅✅ **PHASE 5 TERMINÉE À 100 % (2026-07-09, branche `feat/phase5-home-ui`)** — les 16 tâches du plan (accueil UI Toolkit, reproduction à l'identique du web), chacune implémentée puis relue par un agent indépendant (revue allégée pour les tâches 14/15 sur accord Nathan ; Task 11, à l'inverse, a eu une revue renforcée — modèle opus — vu son rôle de fondation pour les tâches 12-14). **Task 16 (celle-ci) = passe de vérification complète dédiée** : suite entière relancée pour la première fois depuis le début de la branche (Tasks 13-15 avaient différé les `run_tests` par nouveau process) — **279/279 tests EditMode, 9 PlayMode (7 verts + 2 skips connus Utopia NavMesh-îlot, 0 échec)**, aucune régression sur les 16 tâches combinées. `HomeSmokeTests` ajouté (patron `SliceSmokeTests`). Cycle complet Home → Shop (5 onglets, achats/équipements réels testés) → Map/Réglages/Loadout/Comment-jouer → Deploy → Utopia → HUD → mort → Menu → Home vérifié via MCP en Play Mode. Passe de parité contre la spec §3 : catalogues boutique (armes/persos/accessoires/boosts/danses/consommables), textes des panneaux Map/Réglages/Loadout, gating Urban (`MapRules.IsAvailable`) tous confirmés exacts par inspection de code + `execute_code`. Détail complet dans `.superpowers/sdd/task-16-report.md`. TODO manuels restants (visuel, non bloquants) listés ci-dessous.
- 🐛 **Bugs trouvés par Nathan en playtest (2026-07-07)** : voir section dédiée ci-dessous — 3 corrigés, 1 aggravé (map Blender), 1 encore ouvert (décor déformé, Blender).
- 🎨 **Playtest Nathan sur la Phase 5 mergée (2026-07-09)** : gros lot d'écarts visuels + bugs relevés en jouant l'accueil et une partie (fond d'accueil absent, styles de boutons/boutique à refaire, panneaux → pop-ups modaux, HUD différent du web + minimap sans le joueur, perso déformé/flottant en 3ᵉ pers., écran de game-over riche manquant, on meurt trop vite, chargement trop long, plantes aplaties). **Images cibles fournies dans `docs/refactor/`.**
- ✅✅✅ **PHASE 5.1 TERMINÉE À 100 % (nuit du 2026-07-09, branche `feat/phase5.1-visual-parity`, en autonomie complète)** — les 9 chantiers/20 tâches du plan (`docs/superpowers/plans/2026-07-09-phase5.1-visual-parity.md`), codées d'une traite puis vérifiées en une passe complète en fin de nuit : **279/279 tests EditMode, 7/9 PlayMode (2 skips connus Utopia NavMesh-îlot, 0 échec), 1 régression de test trouvée et corrigée en cours de route** (arrondi flottant sur `HitZoneResolverTests`, pas un bug du code). Cycle Home → Shop (5 onglets) → Map/Réglages/Loadout/Comment-jouer (modales) → mort réelle (via `PlayerHealth.TakeDamage`) → `GameOverScreen` → tout vérifié via MCP en Play Mode, aucune exception. Livré :
  - **Chantier 1 — fondations tactiques** : `TacticalPanel`/`TacticalButton`/`TacticalModalFrame` (coins coupés en biseau dessinés via `Painter2D`, USS ne supporte pas `clip-path`). **Découverte en cours de route** : les tags UXML custom (`<Tactical.TacticalPanel>`) ne se résolvent pas dans cette version d'éditeur (6000.5.2f1) malgré `[UxmlElement]` (Unity 6) correctement posé — contourné via `TacticalUiKit`, qui construit ces éléments directement en C# et substitue les placeholders UXML existants (même nom, donc aucune requête de câblage à changer).
  - **Chantier 2 — Accueil** : fond `background.png` câblé (root cause réelle du bug trouvée via `execute_code` : le `Quad` Unity a sa face avant `(0,0,-1)`, invisible avec `Quaternion.identity` face à la caméra lobby — corrigé par une rotation 180°), cartes/bouton Déployer/tuiles restylés, thème basculé de sombre vers clair (le thème Phase 5 était inventé à l'envers).
  - **Chantier 3 — Boutique** : sidebar claire (fix du vrai bug derrière le « boutons partout » — des variables CSS cassées silencieusement par le passage au thème clair), persos alignés avec estompage, Accessoires/Boost/Consommables groupés par rareté avec en-tête.
  - **Chantier 4 — Modales** : Map/Réglages/Loadout/Comment jouer passent de panneaux plein écran à des `TacticalModalFrame` centrées (bouton Retour violet pleine largeur), l'Armurerie reste plein écran.
  - **Chantier 5 — HUD** : libellés de debug retirés (`HudConsumables`/`HudLoadout`), nouveau widget `HudChallenges` (3 barres de contrats), marqueur joueur sur la minimap (`HudRadar`, triangle vert, portée corrigée à 28 m — valeur réelle web, était 20 m).
  - **Chantier 6 — Avatar 3ᵉ personne** : bug « flotte » diagnostiqué et corrigé avec des données réelles (`execute_code` : les bones de pied du rig `blue-soldier.glb` étaient mesurés ~0,11 m au-dessus du sol en pose de bind) — `PlayerAvatar.ComputeGroundOffset()` mesure et compense automatiquement, pas de valeur en dur.
  - **Chantier 7 — Écran de fin de partie riche** : `RunStats` étendu (score/précision/meilleur coup/vague la + rapide), `RankRules.RunRank` (formule S/A/B/C/D exacte du web), `LeaderboardEntry` étendu (kills/headshots/précision), nouveau `GameOverScreen` (12 cartes stats + rang + classement) qui remplace l'écran minimal « Vous êtes mort ».
  - **Chantier 8 — Équilibrage** : armure ennemie corrigée (torse ×0.7 zone-dépendant / intégrale ×0.6, était un flat ×0.5), délai de régénération vie corrigé à 6 s (était 3 s) — le taux de régén (`StatFormulas.RegenRate`) était déjà correctement dérivé des stats du perso, pas touché.
  - **Chantier 9 — Perf** : chargement de map passé en `LoadSceneAsync` avec vraie barre de progression (au lieu d'un `LoadScene` synchrone bloquant derrière un voile sans retour visuel) — n'accélère pas le chargement d'Utopia lui-même (dette des 39k colliders, toujours hors périmètre).
  - **Hors périmètre confirmé** : plantes aplaties (bug n°6/H1, cause Blender) — reste un TODO manuel Nathan.
  - Détail complet des vérifications restantes (visuelles, à l'œil) dans `.superpowers/sdd/pending-verifications-phase5.1.md` (non versionné, local).
- 🔧 **Passe corrective Phase 5.1 (matin du 2026-07-10, après capture d'écran de Nathan)** — le rendu réel ne collait pas aux images cibles ; cette fois vérifié par **captures d'écran MCP** (`manage_camera`, outil découvert à cette occasion — les vérifications `execute_code` de la nuit ne voyaient pas le visuel). 4 causes racines trouvées et corrigées, chacune confirmée par capture avant/après :
  - **Shop.uss était ENTIÈREMENT mort depuis la Phase 5** : l'importeur USS ne supporte pas `:not()` et deux sélecteurs invalidaient tout le fichier (`importedWithErrors=true`) — c'était la vraie cause racine du « boutons partout » du playtest. Aucun style boutique ne s'était jamais appliqué.
  - **Rendu Painter2D cassé** (3 défauts) : bandes de dégradé toutes empilées à y=0 (`DrawCutRect` ignorait la position), fond gris par défaut de `.unity-button` par-dessus le dégradé, et dessin contre `contentRect` (excluant le padding, cartes 14px trop petites).
  - **Fond du lobby en letterbox** : quad à taille fixe (12×7) très inférieur au frustum réel (~20×11.3) — nouveau `LobbyBackgroundFitter` le dimensionne au runtime (correct sur tout aspect ratio).
  - **Layout accueil/HUD réaligné sur les images** : Déployer en colonne droite, sélecteur perso au pied du socle, armurerie remplace le lobby (au lieu de s'y superposer), vie en haut-droite près de la minimap, bouclier bas-centre, textes emoji → noms courts (la police uGUI ne rend pas les emojis).
  - Re-vérifié après corrections : 279/279 EditMode, 7/9 PlayMode (2 skips connus), 0 régression.
- 🎨 **Passe de parité visuelle n°2 (2026-07-10, après-midi, demande Nathan « la page ne ressemble pas à 100% aux photos »)** — refonte de l'accueil ET de la boutique contre les images `docs/refactor/`, chaque écran vérifié par capture MCP avant/après. Livré :
  - **Nouveau composant `TacticalIcon`** (Painter2D) : ~15 glyphes vectoriels (personne, ⊕, trophée, document, cube, play, cible, pistolet, anneau, coureur, carte, barres, soleil, cadenas, lame) — remplace TOUS les emojis de l'UI (ils ne se rendent pas avec les TTF Barlow) ; + équerres violettes de coin sur `TacticalPanel` (ShowBrackets), rayures diagonales clippées (Sutherland–Hodgman) sur le Déployer (`TacticalButton.Stripes`).
  - **Accueil (parité homepage.png)** : carte opérateur avec icône + RANG violet, titre OPÉRATION/SURVIE empilé en énorme condensé, cartes stats crédits/vague en haut-droite avec carrés violets, boutons carrés soleil/? , Intel/Équipement ancrés à gauche avec en-têtes à icône, Déployer 400px avec carré play + rayures, carte Objectif avec texte à surlignages violets (rich text), 4 tuiles compactes à icônes en bas-gauche + carte Difficulté à droite, sélecteur perso pilule avec dots intégrés — tous les textes en capitales (UXML + `ToUpperInvariant()` sur les textes dynamiques).
  - **Boutique (parité shop-guns/perso/accessoires.png)** : voile sombre supprimé (il délavait tout l'écran), topbar `// ARMURERIE` + carte crédits ⊕ + croix carrée, rail d'onglets en cartes blanches avec icônes (recolorées au switch), flèches ‹ › en carrés blancs latéraux, hero sombre condensé 42px + badge rareté contouré, panneau stats NOIR (barres violettes), chips catégories blanches, cartes du rail bas sombres (nom + rareté + glyphe teinté par rareté + cadenas vectoriel), onglet Personnage avec panneau stats bas-centre et boutons côte à côte, onglets Accessoires/Boost/Consommables en panneau latéral droit blanc (titre + souligné + sous-onglets + groupes par rareté + barre d'achat au pied), prix « 500 CR » (plus d'emoji 🪙).
  - **Modale Map (parité modal-map.png)** : tout centré, titre MAP condensé + souligné violet, tuiles blanches à carré-icône carte, RETOUR violet (fix des pseudo-états `:hover` du thème par défaut qui grisaient le bouton).
  - **Perso lobby « déformé » (retour Nathan)** : la boucle jouée était `walking_man` (marche rapide) puis `Alert` (posture de combat) — le web boucle `Casual_Walk`. Clip copié (`anim-lobby.glb`), nouveau rôle `AnimRole.Lobby` + champ `AnimationLibrary.lobby` + entrée Assign Imported Art ; **bug corrigé au passage dans `AnimationDriver`** : `RoleSlotCount=5` en dur faisait entrer le 6ᵉ rôle en collision avec le slot one-shot (désormais dérivé de l'enum).
  - Re-vérifié en fin de passe : **287/287 EditMode, 7/9 PlayMode (2 skips connus Utopia), 0 régression**.
  - **Écart restant assumé (TODO)** : le HUD en jeu est aux bonnes positions (passe du matin) mais plus brut que `hud.png` — pas de cartouches sombres derrière crédits/consommables/munitions, minimap carrée au lieu de ronde, jauges grises au lieu de violettes. Chantier de style uGUI dédié à planifier.
- 📝 **Playtest Nathan sur la passe n°2 (2026-07-10, midi)** : premier feedback complet reçu — 12 points (menu Échap qui ne fige pas la caméra, ennemis qui n'avancent pas/traversent les murs, Play doit démarrer sur le menu, boutons d'accueil coupés en bas + retour d'armurerie qui les fait disparaître, perso lobby torse rétréci/décentré, pilule Cyborg trop ronde, vitrine Armes = l'arme pas le perso, onglet Personnages à refaire sur `shop-perso.png`, accessoires trop petits à refaire en vignettes rondes bordées par rareté (`shop-accessoires.png` + nouveau `shop-accessoires2.png` pour les costumes), même style sur tous les onglets à grille, HUD à recaler sur le web dont vie en bas-gauche + minimap ronde, UI responsive). **Rédigé au propre dans la nouvelle section « Phase 5 bis » ci-dessous — c'est la liste de travail de la prochaine session.**
- ⬅️ **Prochaine session** : traiter la **Phase 5 bis bis** (6 points du 3e feedback playtest, voir la section dédiée — nouvelle référence `docs/refactor/amelioration.png`), puis passe de vérification complète (« lance tout » : suites EditMode/PlayMode + vérifs manuelles listées dans `.superpowers/sdd/pending-verifications-phase5bis.md`), re-validation Nathan, merge de la branche, puis Phase 6 (multijoueur, Photon Fusion — nécessite la mise en place d'un compte/projet Photon).

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
   - ⚠️ **Colliders à refaire — URGENT, reclassé depuis « plus tard » (2026-07-07, playtest Nathan)** : les **39 043 MeshColliders non convexes** (un par mesh de la map) ne sont pas qu'une dette de perf — ils **cassent la jouabilité** : escaliers infranchissables, vitres tantôt solides tantôt traversables (bug n°4), et la NavMesh qui en dépend piège des ennemis sur les toits, injouables (bug n°5). **À corriger avant la suite de la Phase 4** (détail technique et pistes de fix dans la section « 🐛 Bugs » ci-dessous) : soit des colliders convexes/primitifs sur les éléments clés (escaliers, vitres), soit un mesh de collision simplifié dédié fait dans Blender.
   - 💤 **Reste de la passe qualité (confort, non bloquant, peut attendre)** : éclairage cuit (lightmaps + probes) + post-processing URP, LOD/occlusion culling.
   - ⛔ **Urban : bloquée** — pas de `.blend` urban sur la machine (seule `map_utopia` existe dans `Desktop\3D\`).

*(✅ Reste de la Phase 2 FAIT (2026-07-07) : comportements spéciaux des 10 types (bomber explose au contact/mort, toxic/cryo zones au sol, medic soigne à 5 m, shielder bloque le cône frontal ±60°, grenadier grenades en cloche, commander aura ×1.2, stunner ralentit le joueur, berserker frenzy sous 25 % PV, ghost semi-transparent), VIP doré (vagues ≥ 3 hors boss, argent ×2), combo (fenêtre 3.5 s, mult ≤ 5, HUD dédié) — valeurs exactes du web, spec `docs/superpowers/specs/2026-07-07-enemy-specials-vip-combo-design.md`. Restent pour la phase 4 : killstreak rewards (#58, couplés au bouclier), changement d'arme, ramassage G, decoy/renforts, audio.)*

**Jalon :** jouer des vagues d'ennemis **animés**, arme en main, sur utopia — visuel nettement au-dessus du web.

### Phase 4 — Systèmes méta : toute la LOGIQUE du solo complet ✅ FAIT (2026-07-08)

*Première moitié de l'ancienne « Phase 4 », divisée en deux (décision 2026-07-07) : d'abord la mécanique, ensuite l'interface (Phase 5) — parce que reproduire l'accueil sans la logique derrière donnerait des boutons vides.*

- ✅ **Sauvegarde (lot 1, FAIT 2026-07-07)** : miroir exact du schéma `cod-save-v1` (`SaveData`/`SaveMigration`, Newtonsoft, mêmes migrations que le web : knife/medkit/merge sur défauts) → `persistentDataPath/cod-save-v1.json` via `MetaProgress` (persist à la mort/pause/quit). **La save pilote déjà le jeu** : perso/arme sélectionnés, upgrades d'arme (chargeur/cadence/dégâts/recul) et joueur (PV/régén/vitesse), sensibilité, vue par défaut.
- ✅ **Économie/boutique (logique, lot 1)** : `ShopRules` (achats armes/persos, upgrades à coût croissant) testée ; argent de kill = perKill × combo × difficulté × VIP (correctif parité au passage). L'UI attendra la Phase 5.
- **Progression** : ✅ XP/niveaux (`ProgressionRules` : 25/kill, +25 headshot, 100/vague, niveaux 1→50, HUD argent+niveau+barre XP). Restent : rangs, succès, contrats quotidiens, prestige (extraction JSON à faire).
- **Gameplay annexe** : consommables/grenades, power-ups complets, mêlée joueur, chien de guerre, loadout, killstreaks.
- **Audio** : la synthèse Web Audio du jeu web → AudioSources Unity (+ touche M mute).
- **Ce que l'analyse du web a révélé en plus (2026-07-07, à ne pas oublier)** : ramassage d'armes sur les cadavres (G), modes de tir auto/burst/semi (T/P), bouclier (barre séparée de la vie), pause (Échap) + game over + réglages en jeu (sensibilité/volume/difficulté/hardcore/pluie/vue), modes **hardcore** et **entraînement**, adrénaline/surchauffe/longshot/précision (mécaniques annexes visibles au HUD), tourelle/claymore/frappe aérienne avec ciblage sur carte/caisse de ravitaillement (= le loadout), barre de vie du chien.
- **HUD de cette phase** : chaque fonctionnalité arrive avec son élément de HUD identique au web (stocks consommables, jauges loadout, XP/niveau, argent, combo/killstreak, toasts succès/défis/level-up) — voir la checklist « Parité HUD & contrôles ».
- Extraction JSON restante (même méthode que les 7 JSON existants) : succès, contrats, killstreaks, stats consommables, chien (pet.js), prestige.
- ⚠️ Marquer ce qui est solo-only (slow-mo, invincibilité… toxiques en PvP) : tables `PvE` et `PvP` séparées.

**Jalon :** toute la logique du solo complet existe et est testée (même si l'interface finale n'est pas encore là).

### Phase 5 — L'interface : reproduction EXACTE de la page d'accueil web ✅ FAIT (2026-07-09)

**Décision actée (2026-07-07, très important pour Nathan) : la page d'accueil du jeu web est reproduite À L'IDENTIQUE — mêmes boutons, mêmes fonctionnalités, même comportement, tout pareil.** La référence : l'accueil du jeu web (`C:\wamp64\www\projet\games`, HTML/CSS dans `styles/` et `src/`).

- ✅ **Réflexion faite en autonomie (2026-07-07, sans code)** : inventaire exhaustif écran par écran, bouton par bouton de l'accueil web (jouer, boutique, persos, armes, loadout, réglages, stats, danses…) → checklist de parité. Spec `docs/superpowers/specs/2026-07-07-phase5-home-ui-design.md` (15 décisions D1-D15 + 5 points ouverts pour Nathan), plan `docs/superpowers/plans/2026-07-07-phase5-home-ui.md` (16 tâches, branche `feat/phase5-home-ui`). Découverte clé : l'accueil web vit dans `game.html` (pas d'`index.html` séparé), en lobby + panneaux overlays ; pas d'écran dédié succès/contrats/prestige (juste des compteurs "Intel") ; pas de musique de menu.
- ✅ **Les 16 tâches livrées (branche `feat/phase5-home-ui`)** : scène `Home.unity` (`HomeSceneBuilder`, premier UI Toolkit du projet) avec perso 3D animé sur fond `lobby-bg.jpg` (caméra fixe parité web) ; lobby complet (Opérateur/rang, Intel, Équipement, Déployer, sélecteur de perso, 4 tuiles + indicateur) ; boutique 5 onglets (Armes/Personnage/Accessoires/Boost/Consommables) avec achats/équipements réels sur `ShopRules` étendu ; panneaux Map (Utopia jouable + Urban grisée « Bientôt », gating dérivé de `MapRules.IsAvailable`), Réglages (6 contrôles persistés), Série de points/loadout (5 tuiles, max 3, touches L/Y/U/I/4), Comment jouer (7 onglets statiques copiés du web) ; `GameLauncher.Deploy` (persist → overlay anti-flash → charge la map résolue) ; bouton Menu sur l'écran de mort → retour Home. Polices Barlow/Barlow Condensed TTF importées et câblées dans les USS.
- ✅ **Task 16 (vérification complète dédiée, 2026-07-09)** : `HomeSmokeTests` ajouté (patron `SliceSmokeTests`), suite complète relancée pour la première fois sur la branche entière — **279/279 EditMode, 9 PlayMode (7 verts + 2 skips connus, 0 échec)**, aucune régression. Cycle Home → Shop → Map/Réglages/Loadout/Comment-jouer → Deploy → Utopia → HUD → mort → Menu → Home vérifié de bout en bout via MCP. Parité vérifiable sans humain confirmée (catalogues boutique exacts, textes des panneaux, gating Urban). Détail : `.superpowers/sdd/task-16-report.md`.
- 💤 **Reste (manuel, non bloquant)** : vérification visuelle de l'accueil par Nathan, clips de danse à importer du web (`PlayDance` no-op sans eux, ne plante pas), découpe « tactique » exacte des cartes si souhaitée — voir TODO manuels ci-dessous.

**Jalon atteint :** un joueur du jeu web retrouve exactement son accueil sur mobile → **c'est la v1 publiable**, sous réserve de la vérification visuelle manuelle de Nathan.

### Phase 5 bis — Feedback playtest Nathan du 2026-07-10 (sur la passe de parité n°2) ✅ TRAITÉE (2026-07-10 soir, en attente de re-validation Nathan)

*Les 12 points ci-dessous ont été traités en une session (plan : `docs/superpowers/plans/2026-07-10-phase5bis-playtest-fixes.md`, vérifications restantes : `.superpowers/sdd/pending-verifications-phase5bis.md`). Résumé des correctifs :*

1. **Pause** ✅ — `PlayerInputBridge` coupe toutes les entrées gameplay quand le menu est ouvert ; curseur libéré en pause/mort, verrouillé en jeu (parité pointer-lock web).
2. **Ennemis** ✅ — cause racine trouvée par diagnostic MCP : NavMesh d'Utopia fragmenté (départ joueur sur l'îlot du disque central, spawns du repli non validés → chemins partiels). Builder refait (départ + 5 spawns choisis sur la composante NavMesh principale, échec bruyant), et parité IA web complète : patrouille marchée en anneau autour du joueur, ligne de vue requise pour attaquer, hystérésis de sortie (×1,25/×1,4), détection ranged ≥ portée de tir, destination projetée sur le NavMesh, `Warp` au spawn. Vérifié en Play (patrouille → chasse → tir → mort du joueur immobile).
3. **Play → menu** ✅ — `Tools/Game/Play From Home` (coché par défaut) force le Play Mode à démarrer sur `Home.unity`.
4. **Boutons coupés** ✅ — PanelSettings en `Shrink` (la référence 1920×1080 tient toujours entière) + tuiles compressibles.
5. **Retour armurerie** ✅ — event `Closed` sur `ShopScreenController` : le lobby se ré-affiche sur tous les chemins de fermeture.
6. **Perso lobby** ✅ — torse rétréci : les clips GLB portaient des curves de position calibrées sur un autre rig ; `AssignImportedArt` génère des clips filtrés (rotations pures) dans `Art/Animations/Filtered/`. Centrage : stage décalé de +0,3 m (stèle non centrée dans `background.png`), mesuré par capture.
7. **Pilule** ✅ — hauteur 52 px, radius 26 (vraie pilule aux proportions web).
8. **Vitrine Armes** ✅ — perso masqué, arme seule centrée près de la caméra ; sans modèle 3D → vitrine vide (plus de capsule).
9. **Onglet Personnages** ✅ — trio 3D animé (voisins ±1,8 m), flèches ‹ ›, bloc stats centré bas (déplacé hors de la hero-row dans l'UXML).
10. **Accessoires** ✅ — vignettes rondes 116 px à bordure de rareté, coche verte sur l'équipé, cadenas sur le non-possédé, en-têtes de section encadrés + ligne colorée (masques ET costumes).
11. **Autres grilles** ✅ — Boost/Consommables déclinés sur le même modèle circulaire (cellule : cercle + nom + stat + bouton).
12. **HUD** ✅ — refonte complète parité `hud2.png` : crédits + consommables en cartouches sombres (haut-gauche), DÉFIS + vie segmentée verte + SPRINT + rang (bas-gauche), bannière sombre « VAGUE n · m ENNEMIS » (haut-centre), BOUCLIER (bas-centre), barre de progression de vague + minimap RONDE (haut-droite), jauges loadout + bandeau rouge « ✕ N ENNEMIS » + bloc munitions/SCORE (bas-droite). CanvasScaler en `Shrink` (rien ne sort jamais de l'écran).

**Jalon Phase 5 bis (inchangé) :** Nathan rejoue cette checklist point par point et ne trouve plus d'écart. Liste d'origine conservée ci-dessous pour cette re-validation.

#### Détail d'origine du feedback (pour la re-validation)

*Premier retour de Nathan sur la version de la branche `feat/phase5.1-visual-parity` après la passe de parité visuelle n°2 (commit `b93bb76a`). Rédigé au propre ici à sa demande — c'est la liste de travail de la prochaine session. Les images de référence restent `docs/refactor/*.png` (+ nouvelle image `shop-accessoires2.png` ajoutée par Nathan : les costumes, même modèle visuel que les masques).*

#### A. Bugs de jeu (bloquants pour le confort de jeu)

1. **Menu Échap : la caméra bouge encore derrière le menu.** Quand le menu pause est ouvert, les mouvements de souris continuent de faire tourner la vue. Attendu : ouvrir Échap **gèle la caméra** (et libère le curseur), la vue ne doit plus bouger tant que le menu est ouvert.
2. **Les ennemis buguent toujours** : certains n'avancent pas du tout, d'autres avancent tout droit **dans les murs**. À rediagnostiquer (NavMesh ? FSM ? obstacles ?) — c'est un retour récurrent depuis le playtest du 2026-07-07.
3. **Lancer Play dans Unity doit arriver sur le MENU, pas sur la map.** Aujourd'hui la game démarre directement en jeu ; attendu : le point d'entrée est toujours la page d'accueil (`Home.unity`), comme le web.

#### B. Page d'accueil

4. **Boutons mal placés / coupés en bas** : une partie de la rangée de tuiles du bas est **coupée par le bord de l'écran**. Hypothèse de Nathan : peut-être simplement un écran trop petit — dans ce cas **l'UI doit s'adapter à la taille de l'écran** (responsive : rien ne doit jamais être coupé, quelle que soit la résolution/l'aspect ratio).
5. **Retour de l'armurerie cassé** : en quittant l'armurerie on revient bien sur le perso du lobby, mais **les boutons de l'accueil ne s'affichent plus**.
6. **Perso du lobby bugué** : son **torse est rétréci** (déformation du mesh/rig à diagnostiquer), et il n'est **pas centré sur la stèle** — il est trop à droite. Attendu : perso bien au centre du socle.
7. **Pilule du nom (« CYBORG ») trop ronde** : le bloc-sélecteur de perso a un border-radius beaucoup trop prononcé, il ne ressemble pas à l'original de `homepage.png`. Reprendre la forme exacte du web.

#### C. Armurerie (boutique)

8. **Onglet Armes : retirer le personnage de la vitrine, afficher l'ARME à la place.** Actuellement on voit le perso 3D ; attendu : le modèle 3D de l'**arme sélectionnée** au centre. Une seule arme a un modèle aujourd'hui : l'afficher quand on clique dessus ; pour les autres (sans modèle), **laisser la vitrine vide** (pas de capsule de secours).
9. **Onglet Personnages : pas conforme à `shop-perso.png`.** Attendu : les personnages **visibles en 3D sur les côtés** (le sélectionné au centre, les voisins à côté), on **pivote avec les flèches** ‹ ›, et le **bloc statistiques centré** en bas (il ne l'est pas actuellement). Se rebaser entièrement sur l'image.
10. **Onglet Accessoires : pas conforme à `shop-accessoires.png`.** Les produits sont **beaucoup trop petits**, on ne les voit pas. Attendu, exactement comme l'image : vignettes **grandes et RONDES** (border-radius cercle), **bordure colorée selon la rareté**, **petite icône check** ✓ sur l'article équipé, etc. **Pareil pour les costumes** (référence : `shop-accessoires2.png`, même modèle que les masques).
11. **Cohérence de ce style sur TOUS les onglets à grille** : Consommables, Boost, etc. doivent suivre exactement le même modèle visuel (même sans image de référence dédiée).

#### D. HUD en jeu — pas à 100 % comme le web (screenshot de référence fourni par Nathan le 2026-07-10, archivé : `docs/refactor/hud2.png` — plus récent que `hud.png`, c'est LA référence HUD à suivre)

12. **Rien n'est centré / des éléments sont mal placés** par rapport au HUD web. Relevé de Nathan (+ lecture de son screenshot de référence) :
    - **Barre de vie : en BAS À GAUCHE** (barre segmentée verte au-dessus de SPRINT et du rang), **pas en haut à droite** — corrige la position posée lors de la passe du 2026-07-10 matin.
    - **« Série de points » (jauges loadout)** mal placées/pas conformes.
    - Consommables en **cartouches sombres à coins coupés** avec icône + compteur (colonne haut-gauche, crédits ⊕ doré en tête).
    - **Minimap RONDE** en haut-droite (pas carrée) avec la barre violette au-dessus.
    - Bannière de vague sombre en haut-centre (« VAGUE 1 · 2 ENNEMIS »), bouclier bas-centre, bloc munitions sombre bas-droite avec « ✕ N ENNEMIS » rouge + « 94 / 100 » + SCORE.
    - Défis en bas-gauche (3 barres sombres sous « DÉFIS »).
    - Comme pour l'accueil : si des éléments sortent de l'écran sur les petites résolutions, **le HUD doit s'adapter à la taille de l'écran**.

*(Point hors périmètre rappelé : la petite barre avec le soleil sur le screenshot de Nathan est un widget Windows, pas le jeu.)*

**Jalon Phase 5 bis :** Nathan rejoue cette checklist point par point et ne trouve plus d'écart.

### Phase 5 bis bis — Feedback playtest Nathan n°3 du 2026-07-10 (soir, après les corrections à chaud) ✅ TRAITÉE le 2026-07-11 (re-validation Nathan + vérifs de nuit en attente)

*Plan : `docs/superpowers/plans/2026-07-11-phase5bisbis-playtest-fixes.md` — vérifs différées : `.superpowers/sdd/pending-verifications-phase5bisbis.md`. Captures MCP en session : `Assets/Screenshots/p5bb-*.png` (armes+améliorer conformes `amelioration.png`, vitrine persos sur stèle, accessoires).*

*Troisième passe de retours sur `feat/phase5.1-visual-parity` (après les corrections à chaud du 2e feedback, commit `3ba0bcaa`). Nouvelle image de référence fournie par Nathan : `docs/refactor/amelioration.png` (le panneau statistiques + améliorations des armes, version web).*

#### A. Accueil / navigation

1. ✅ **Corrigé (commit `0f6ed45a`)** — `HomeController.Update` route désormais ← → selon `activePanel` : lobby → carrousel lobby ; boutique + onglet Personnage → `ShopScreenController.CharacterPrev/Next()` (nouvelles méthodes publiques + `IsCharacterTabActive`) ; partout ailleurs → inertes. À re-valider par Nathan (Input System non simulable par sonde MCP).

#### B. Boutique — onglet Personnage

2. ✅ **Corrigé (commit `23e01409`)** — cause confirmée : la translation +2,2 m passait les persos devant la stèle (peinte sur le quad de fond à z=−3) et la caméra plongeante les projetait plus bas. Remplacée par un **scale ×1,45 sur place** (pivot GLB aux pieds, y=0) : les pieds restent posés sur la stèle (capture `p5bb-perso-vitrine.png` ✓), retour à l'échelle 1 en quittant l'onglet. Constante `ShowcaseScale` à ajuster si Nathan veut plus gros.

#### C. Boutique — système d'amélioration (armes ET persos)

3. ✅ **Corrigé (commits `c9a20e04` + `fc964b04`)** — spec web vérifiée : le web a bien **4 upgrades d'arme PAR STAT** (`UPGRADE_SPECS` main.js:1139-1144 — Dégâts/Cadence/Recul/Chargeur, 5 niveaux, coût `baseCost×(niveau+1)` avec bases 300/300/200/250, save `weaponUpgrades[arme][stat]`), déjà extraites dans `unity-spec/items.json` (bloc `upgrades`).
   - **Schéma/règles étendus** : `SaveData.weaponUpgrades` devient `Dictionary<arme, Dictionary<stat, niveau>>` (miroir web), migration automatique de l'ancien niveau unique Unity (reporté sur les 4 stats — vérifiée sur la save réelle : VIPER-AR niveau 3 → « ★ 12/20 »), `ShopRules` par stat (+ `WeaponUpgradeTotal` pour le rating x/20), `WeaponController.Init` reçoit les 4 niveaux (`WeaponUpgradeLevels`), tests EditMode écrits (exécution à la vérif de nuit).
   - **UI refaite** (partagée armes/persos, `UpgradeRowUi`) : sous-panneau encadré, une ligne par stat avec 5 pastilles carrées + bouton violet ⊕ prix, MAX grisé — capture `p5bb-armes-ameliorer-hd.png` conforme `amelioration.png` ; côté persos idem avec effet dans le libellé (« Vie (+20) »).

#### D. Boutique — accessoires

4. ✅ **Corrigé (commit `589706ff`)** — écarts relevés à la re-comparaison : la barre d'achat web est un **bouton/badge SEUL pleine largeur** (`acc-buy-bar`, main.js:1592-1601 — la ligne nom+rareté en trop a été retirée de l'UXML), vignettes rondes agrandies 145→165 px (≈ 27 % de la largeur du panneau comme la référence) avec marges aérées, étiquettes de rareté en pastille à fond teinté (alpha 0,14). Capture `p5bb-accessoires-masque.png` conforme. Nathan re-compare ligne à ligne pour valider.
5. ✅ **Corrigé (commit `7be372b5`)** — la base `.subtab` passe à la grande taille (padding 12/28, 19 px) et les pseudo-états du thème Unity par défaut (hover/active/focus, cause du « saut » au clic) sont épinglés, comme le précédent `.panel-back`. S'applique aussi au rail de catégories d'armes (vérifié sans débordement en capture).

#### E. Map Utopia

6. ✅ **Diagnostic clos — correctif côté Blender (action manuelle Nathan).** Ce n'est PAS une régression du rebuild du 2026-07-10 : le GLB et son `.meta` n'ont jamais changé depuis l'import initial (`9fe9a8f2`, un seul commit chacun) et le rework NavMesh (`c1d3c866`) ne touche aucun transform de la map (diff vérifié ligne à ligne). Sonde MCP du 2026-07-11 sur `utopia.glb` : **29 862 instances de végétation sur 36 920 ont des échelles non uniformes aberrantes** (ex. `Proto_Fleur` scale (14.89, 2.58, 0.23) — `Proto_Fleur` : 23 137/27 779 distordues, `Proto_Touffe` : 6 725/9 141, ratio max/min > 3) alors que leurs meshes sont sains (bounds normaux). Cause : les **matrices des instances de particules/geometry nodes sont cisaillées à l'export** Blender→GLB et se décomposent en TRS dégénéré — d'où des plantes aléatoirement écrasées/étirées (même racine que le bug n°6/H1 du 2026-07-07).
   **À faire côté Blender avant re-export :** sélectionner la végétation → réaliser les instances (geometry nodes : modificateur « Realize Instances » ; systèmes de particules : Make Instances Real), puis **Ctrl+A → All Transforms** sur les objets générés, ré-exporter `utopia.glb` (même chaîne CLI) et relancer **Tools → Game → Build Utopia Scene**. ⚠️ Ça va probablement augmenter le nombre de meshes (dette 39k MeshColliders à resurveiller).

**Jalon Phase 5 bis bis :** Nathan rejoue ces 6 points et ne trouve plus d'écart ; alors seulement, re-validation globale de la 5.1 puis merge. *(État 2026-07-11 : points 1-5 corrigés, point 6 = re-export Blender chez Nathan ; vérifs de nuit listées dans `.superpowers/sdd/pending-verifications-phase5bisbis.md`.)*

### Phase 6 — Multijoueur 🚧 (cœur coop démarré)

Netcode : **Photon Fusion** (décidé). Dans l'ordre :

1. **Coop contre vagues** (2–4 joueurs) : l'hôte fait autorité sur les ennemis.
2. **PvP** (deathmatch par équipes) : prédiction client + compensation de lag (fournies par Fusion), matchmaking/lobbies, **rééquilibrage obligatoire** (le TTK actuel est pensé contre des bots), anti-triche (dégâts validés côté serveur).

La save passe côté serveur (sinon triche triviale). Options : Unity Cloud Save, PlayFab, Firebase.

**Jalon :** un match PvP complet entre deux téléphones réels.

**✅ Livré (2026-07-11/12, branche `feat/online-coop`, mergée dans `main` le 2026-11-23) — le CŒUR LOGIQUE de la coop en ligne, hôte autoritaire, indépendant du transport :** nouvel assembly **`Game.Net`** (engine-free, testé) contenant la simulation coop complète — registre de joueurs, directeur de vagues rééquilibré par nombre de joueurs (`CoopScaling`, réutilise `WaveRules`), validation des dégâts côté hôte (`DamageValidator`), boucle **down → relève → respawn → wipe**, `WorldSnapshot`, et un **`LoopbackTransport`** en mémoire pour tests/hôte-seul. Le tout derrière une interface **`INetTransport`** : **Photon n'est qu'un adaptateur** (`Game.Net.Photon`, **inerte** tant que Fusion n'est pas installé — contrainte de define `FUSION2`, aucun impact sur le build actuel). ≈35 tests EditMode (`Assets/_Game/Tests/Net/`) ; la partie engine-free a aussi été **exécutée** hors éditeur (`mcs`/`mono`, 41 assertions vertes). Spec : `docs/superpowers/specs/2026-07-11-online-coop-mode-design.md` · Plan : `docs/superpowers/plans/2026-07-11-online-coop-mode.md`.

**Reste pour la coop jouable :** vérifier la compilation/les tests **en éditeur** (MCP) ; écrire le **binder de scène** (instancier les ennemis sur `CoopRunner.EnemySpawnRequested`, réconcilier avec `EnemyHealth`/`EnemyController`, appliquer les snapshots côté client) ; **installer Photon Fusion 2** + App ID et compléter `FusionTransport` (voir `Assets/_Game/Scripts/Net/Photon/README.md`) ; régler l'équilibrage coop (`// TODO(coop-balance)`).

> ⚠️ Ordre voulu de la roadmap : le solo complet (Phases 3–5) sort **avant** le réseau. Ce cœur coop a été livré en avance à la demande de Nathan (« fais le mode en ligne ») ; il ne bloque pas les phases 3–5 (assembly séparé, aucun code existant modifié hormis l'ajout d'une référence à `Game.Runtime`).

### Phase 7 — Qualité & sortie 🔜

- **Tests** : porter les invariants critiques de la suite Playwright du web (jamais d'argent négatif, XP monotone, vagues jamais vides, armure torse vs intégrale…).
- **Perf** : Profiler, cible 60 fps sur un Android milieu de gamme.
- **Distribution** : Play Console (internal testing → prod), TestFlight → App Store (~1 semaine d'allers-retours avec Apple la première fois).
- Monétisation éventuelle : à décider AVANT la sortie (ça influence l'économie).

**Jalon :** le jeu est sur les stores. 🎉

**Ordre global : le solo complet (phases 2–5) sort AVANT le réseau (phase 6).**

---

## 🐛 Bugs trouvés par Nathan en playtest (2026-07-07) — à corriger en priorité

*Relevés sur la scène Utopia (branche `feat/phase3-anims-player-hud`) après le travail de la nuit. Captures d'écran fournies par Nathan (non jointes ici) : (1) arme mal positionnée dans le dos du perso en 3ᵉ personne, (2) un carré de décor complètement déchiqueté près d'un bâtiment.*

1. ✅ **CORRIGÉ (Phase 4 Task 2, 2026-07-08)** — Arme mal positionnée sur l'avatar en 3ᵉ personne. Le fusil flotte dans le dos/près de la tête au lieu d'être tenu en main. Suspect : `PlayerAvatar.AttachWeapon()` / `PlayerAvatar.FindHandBone()` (`Assets/_Game/Scripts/Runtime/Player/PlayerAvatar.cs`) — soit le bone `RightHand` du rig `blue-soldier` n'est pas trouvé et le fallback regex attrape un mauvais bone, soit l'offset local `(0, 0.02, 0.08)` / rotation `(90°,0,0)` ne convient pas à ce rig précis. À vérifier : dumper la hiérarchie d'os de `blue-soldier.glb` via `execute_code` (comme le spike du chantier 1) et confirmer quel bone est réellement récupéré.

2. ✅ **CORRIGÉ (Phase 4 Task 1, 2026-07-08)** — Marcher fait courir et vide la barre de stamina. En avançant simplement (sans appuyer sur Shift/R), le perso court et consomme sa stamina. Cause confirmée : `PlayerInputBridge.Update()` (`Assets/_Game/Scripts/Runtime/Player/PlayerInputBridge.cs`) — `bool wantsRun = sprint.IsPressed() || m.sqrMagnitude >= 0.81f;` (décision D11, pensée pour un stick de manette/joystick tactile à fond). Au clavier, le vecteur de déplacement WASD est quasiment toujours à magnitude ~1 dès qu'on appuie une touche → la condition `>= 0.81` est **quasiment toujours vraie**, donc on court en permanence sans le vouloir. **Fix : cette règle "stick à fond = courir" ne doit s'appliquer qu'à l'input manette/joystick tactile, pas au clavier** (détecter la source de l'input, ou ne l'appliquer que quand `move` vient de `<Gamepad>/leftStick`/joystick tactile).

3. ✅ **VÉRIFIÉ, PAS DE BUG (Phase 4 Task 3, 2026-07-08)** — L'animation de course ne semble pas être la bonne (à confirmer par Nathan, doute exprimé) — probablement lié au bug n°2 : comme le perso court en permanence, seule l'anim "Run" est visible et Nathan n'est pas sûr qu'elle corresponde à celle attendue. **À revérifier une fois le bug n°2 corrigé** (une fois que marcher redevient possible, comparer visuellement idle/walk/run). Si le souci persiste, vérifier le clip assigné dans `AnimationLibrary.asset` (rôle `run`) et le mapping dans `PlayerAvatar.Update()`.

4. ⚠️ **MITIGÉ CÔTÉ CODE (Phase 4 Task 4, 2026-07-08), fix définitif toujours Blender** — Impossible de monter dans les bâtiments (bloqué par une "vitre" invisible dans les escaliers) + on traverse parfois les vraies vitres — collision cassée sur la map Utopia. Cause probable : `UtopiaSceneBuilder.Build()` (`Assets/_Game/Scripts/Editor/Scene/UtopiaSceneBuilder.cs`) pose un `MeshCollider` **non convexe** sur chaque mesh individuel de la map (39 043 colliders — dette déjà notée). Les géométries fines/complexes des vitres et des marches d'escalier ne collisionnent pas de façon fiable avec le `CharacterController` (colliders non-convexes = comportement physique instable, `stepOffset` inefficace sur une géométrie d'escalier en un seul mesh compliqué). **C'est probablement la même cause racine que la dette de perf déjà notée** (39k MeshColliders) — la corriger réglera sans doute les deux à la fois : soit des colliders convexes/primitifs sur les éléments clés (escaliers, vitres), soit un mesh de collision simplifié dédié à faire dans Blender (géométrie basse-poly invisible, juste pour la physique). **Cette correction doit sans doute être traitée avant la Phase 4 lot 2**, vu qu'elle casse la jouabilité de la map.

5. **Les ennemis spawnent sur le toit et ne bougent pas, donc impossible de les tuer** — probablement une conséquence directe du bug n°4 (NavMesh) : `UtopiaSceneBuilder` bake la NavMesh avec `collectObjects = CollectObjects.All`, ce qui inclut les toits (marchables) dans la NavMesh. Si les escaliers ne connectent pas correctement le toit au sol (à cause des colliders cassés du bug n°4), la NavMesh du toit devient une **île isolée** : les ennemis y sont bien posés mais ne peuvent littéralement pas trouver de chemin vers le joueur au sol. Le calcul des points de spawn (`NavMesh.SamplePosition` dans un rayon de 30 m autour d'une sonde à 5 m au-dessus du joueur) peut aussi accrocher un toit proche par erreur. **Fix attendu : une fois le bug n°4 réglé (escaliers navigables), rebaker la NavMesh et vérifier qu'il n'y a plus d'îles déconnectées** (Window → AI → Navigation → onglet Areas/visualisation, ou vérifier via `NavMesh.CalculatePath` que le sol et les toits sont reliés). Si des îles persistent, envisager d'exclure les toits de la zone marchable ou de sonder les spawns par un raycast vertical depuis le sol plutôt que par un échantillonnage NavMesh à large rayon.

   **⚠️ MISE À JOUR — pire que prévu (Phase 4 Lot 0 Task 4, 2026-07-08, code + garde-fou anti-îlot livrés dans le commit `1fb7cd3c`) :** le correctif côté code est fait (spawns filtrés à la génération + garde runtime, tous deux exigeant un `NavMesh.CalculatePath` **complet** vers le joueur, pas juste un `SamplePosition` réussi). En le testant en conditions réelles, il a mis au jour quelque chose de plus grave que « des toits isolés » : **le point de spawn du joueur lui-même (le centre de la plaza, calculé par raycast au centre de la map) est posé sur un îlot NavMesh minuscule (rayon ≈ 2-4 m) totalement coupé du reste de la ville.** Preuve : 480 points sondés (24 directions × 3 à 60 m) → **0 connecté** au spawn joueur par un chemin NavMesh complet ; un essai de rebake avec des tolérances de pente/marche bien plus permissives que le brief ne change rien (donc pas un problème de réglage de bake, mais une vraie coupure géométrique dans le mesh exporté, probablement la plateforme/« médaillon » central de la plaza). **Conséquence concrète : sur Utopia, aucun ennemi ne peut actuellement spawner de façon joignable — le garde-fou anti-îlot (correct et voulu) rejette systématiquement tous les points de repli.** Les 2 tests PlayMode `Utopia_*` sont donc rouges pour une cause 100 % map/mesh, pas un bug de code (voir `.superpowers/sdd/task-4-report.md` pour le détail complet de l'investigation). **Ce point devient plus urgent que le bug n°6** puisqu'il bloque entièrement la boucle de gameplay coop sur cette map (pas juste une gêne de déplacement) — même atelier Blender que les bugs n°4/n°6 (probablement la même plateforme centrale à réparer/reconnecter). Décision pour la suite de la Phase 4 : le code reste tel quel (ne jamais spawner un ennemi injoignable est le comportement voulu), les 2 tests concernés sont marqués comme connus/trackés plutôt que silencieusement ignorés, et l'exécution de la Phase 4 continue sur le reste du plan (la majorité des tâches suivantes se vérifient via EditMode + la scène Slice, non affectée).

6. **Éléments de décor complètement déformés** (capture 2 : une grande forme verte déchiquetée, non-manifold, qui transperce le sol/les massifs près d'un bâtiment) — suspect côté **export Blender → GLB**, pas côté Unity. Le script d'export (`bpy.ops.export_scene.gltf(..., export_apply=True)`) applique les modifiers classiques, mais **les systèmes de particules (herbe/végétation) et les geometry nodes ne s'exportent pas fiablement en glTF** sans être d'abord convertis en géométrie réelle ("Convert to Mesh" / réalisation des instances) dans Blender. Ce type de forme fragmentée/spiky est une signature typique d'un scatter de particules ou d'un modifier non résolu à l'export. **Fix côté Blender (Nathan)** : identifier l'objet/collection concerné dans `map_utopia_export.blend`, convertir les particules/geometry nodes en mesh réel avant de relancer l'export, puis regénérer `utopia.glb` et la scène (`Tools → Game → Build Utopia Scene`).

## 🎨 Playtest Nathan sur la Phase 5 mergée (2026-07-09) — écarts visuels & bugs à corriger (PRIORITÉ)

*Premier vrai playtest de l'accueil + du jeu après merge de la Phase 5 dans `feat/phase3-anims-player-hud`. Nathan a fourni les images cibles (le vrai design web) dans `docs/refactor/` — à ouvrir en regard de chaque point. **Objectif : coller exactement au web.** Rien de tout ça n'est encore fait ; c'est le chantier de la prochaine session (probablement une « Phase 5.1 » de polish visuel + parité, avec sa propre spec + plan). Ordre indicatif ci-dessous : d'abord ce qui casse le ressenti/la jouabilité, puis le visuel.*

### A. Page d'accueil (réf. `docs/refactor/homepage.png`, fond `docs/refactor/background.png`)

- **A1 — Photo de fond absente.** Le lobby ne montre pas l'image de fond attendue. La cible est `background.png` (plaza futuriste avec le socle circulaire où se tient le perso). ⚠️ En Phase 5 (Task 1) j'avais copié `lobby-bg.jpg` du web, mais soit ce n'est pas la bonne image (`background.png` est la vraie), soit le quad de fond du `HomeSceneBuilder` ne s'affiche pas / est masqué. **À vérifier : copier `background.png` du web dans `Assets/_Game/Art/UI/`, et confirmer que `HomeSceneBuilder.BuildBackground` l'affiche bien derrière le perso (position/échelle du quad, caméra, ordre de rendu).**
- **A2 — Boutons pas au bon style.** Le bouton **Déployer** et tous les boutons de l'accueil ne ressemblent pas du tout à `homepage.png` (biseaux « tactiques », dégradé violet du Déployer, cartes Intel/Équipement/Objectif au style anguleux, tuiles du bas). Refonte USS de tout l'accueil pour matcher pixel-près l'image. C'est l'écart D6 (« découpe tactique ») noté comme optionnel en Phase 5 — Nathan le veut fait, donc il n'est plus optionnel.

### B. Armurerie / boutique (réfs. `shop-guns.png`, `shop-perso.png`, `shop-accessoires.png`)

- **B1 — Illisible, boutons partout.** L'armurerie actuelle est brouillonne (« plein de boutons PARTOUT »), rien à voir avec la propreté des onglets de référence. Refonte de la mise en page/USS de la boutique pour matcher les images.
- **B2 — Onglet Armes (`shop-guns.png`)** : un **seul modèle 3D d'arme** au centre (l'arme héro), le carrousel du bas = **cartes placeholder (icône)**, pas un modèle 3D par carte. (C'est déjà l'intention côté code — hero 3D + cartes icône — donc le gros du travail ici est la mise en page/lisibilité, pas la logique.) Le panneau Statistiques à droite (Dégâts/Cadence/Chargeur/Stabilité/Portée + ★ N/20 + Améliorer + Équipée) doit matcher l'image.
- **B3 — Onglet Personnage (`shop-perso.png`)** : 3 persos alignés (le sélectionné au centre, les autres estompés sur les côtés), barres Vie/Régén/Vitesse + Améliorer/Équipé en bas.
- **B4 — Onglet Accessoires (`shop-accessoires.png`)** : panneau latéral droit avec sous-onglets Masque/Costume/Danses, tuiles groupées par rareté (Commun/Rare/Épique…), essai en direct sur le perso 3D, bouton Équiper.
- **B5 — Boost & Consommables** : même style que l'onglet Accessoires. **Placeholder d'icône quelconque** là où il n'y a pas encore d'image ; Nathan fournira les vraies images plus tard.

### C. Navigation : boutons → pop-ups modaux (réf. `modal-map.png`)

- **C1 — Tous les boutons de l'accueil SAUF l'Armurerie doivent ouvrir un pop-up modal** centré, fond flouté, cadre à coins « tactiques », bouton Retour violet en bas — exactement le style de `modal-map.png`. Concerne : **Map, Série de points (loadout), Réglages, Comment jouer** (et ⚙/?). Actuellement ce sont des panneaux plein écran ; les transformer en modales overlay. (L'Armurerie reste un écran plein, elle.)

### D. HUD en jeu (réf. `hud.png`)

- **D1 — HUD globalement différent du web.** Le HUD en jeu ne correspond pas à `hud.png`. Cible à reproduire : haut-gauche crédits + stocks consommables + MINES + série de kills ; haut-centre « VAGUE N · X ENNEMIS » puis combo ; haut-droite **barre de vie + minimap ronde (radar)** + type d'ennemi ; bas-gauche **Défis** (3 barres) + Sprint + barre de rang/niveau ; bas-centre **Bouclier** ; bas-droite jauges loadout (Frappe aérienne/Caisse) + ennemis restants + munitions **84/100** + score. (Le HUD actuel montre encore des libellés de debug type « L Tourelle / Y Mine / B:14 N:8 … » — cf. `bug-entity.png` sur cette meme photo on peu voir un arbre applatis corrigé ce beug c surement un export blender mal exporter — à remplacer par le vrai HUD.)
- **D2 — Minimap : le joueur ne se voit pas** dans le radar en haut à droite. Le point du joueur (et idéalement l'orientation) doit apparaître au centre du radar comme sur le web.
- **D3 — Perso déformé en 3ᵉ personne + pieds pas collés au sol.** En vue 3ᵉ personne l'avatar est déformé et « flotte » (pieds sous/au-dessus du sol). Suspects : échelle du rig / `modelScale`, hauteur d'ancrage de l'avatar sur le `CharacterController` (offset Y entre le centre de la capsule et la plante des pieds), ou anim mal alignée. À diagnostiquer sur la scène Slice/Utopia.

### E. Écran de mort (réf. `game-over.png`)

- **E1 — Écran de statistiques de fin de partie manquant.** À la mort, on n'a que l'écran « Vous êtes mort » minimal + boutons Rejouer/Menu — alors que le web a un vrai **game over riche** : « MISSION TERMINÉE », **12 cartes de stats** (Vague, Score, Kills, Headshots, Précision, Meilleur coup, Distance, Meilleure série, Meilleure vague, Vague la + rapide, Record, Combo max), « Argent gagné +N », **rang lettre** (B…), **classement local** (top 5), boutons Rejouer/Menu. **Les données existent déjà** (Phase 4 `RunStats`/`RunTracker`, `LeaderboardRules`, argent gagné) — il « suffit » de construire l'écran et de le brancher dessus. C'était le point D11 de la spec Phase 5 (« game over riche viendra avec les stats de run ») — à faire maintenant.

### F. Équilibrage (parité web)

- **F1 — On meurt trop vite.** Les dégâts reçus sont trop violents / la vie descend trop vite par rapport au web. **Rendre exactement comme la version web** : re-vérifier dans `unity-spec/` (et au besoin `main.js`) les dégâts ennemis, la réduction d'armure, le délai + vitesse de régénération de vie, et confirmer qu'on lit bien ces valeurs sans multiplicateur parasite. (Lien avec le TODO manuel n°2 : réduction d'armure 0.5, délai régén 3 s — valeurs à confirmer et peut-être mal appliquées.)

### G. Performance

- **G1 — Chargement trop long au lancement d'une partie.** Le passage Accueil → partie (Deploy → Utopia) prend trop de temps. Pistes : chargement de scène asynchrone avec vraie barre de progression, alléger la scène Utopia (liée à la dette des 39k MeshColliders, bug n°4), ou pré-chargement. À profiler.

### Décor déformé — suite du bug n°6

- **H1 — Plantes complètement aplaties (`bug-entity.png`).** De grandes plantes/feuillages sont écrasés au sol (à plat) au lieu d'être dressés. C'est très probablement **la même cause racine que le bug n°6** (systèmes de particules / geometry nodes végétation mal exportés du `.blend` → GLB) — fix côté Blender (Nathan) : convertir les particules/geometry nodes en mesh réel avant export, puis regénérer `utopia.glb` + `Tools → Game → Build Utopia Scene`.

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
4. **Régler à l'œil le placement de l'arme en main** (`PlayerAvatar.cs`, constantes `GripLocalPos`/`GripLocalEuler`) — le mécanisme est corrigé (Phase 4 Task 2), il reste juste l'ajustement visuel fin.
5. **Ré-exporter `ar-15.glb` depuis Blender à la bonne échelle** — le mesh brut est ~2,1× trop grand ; un correctif cosmétique (`WeaponData.modelScale = 0.45`) compense en attendant, mais le vrai fix est un ré-export propre.
6. **Copier le modèle du chien de guerre** (`pets/chien2/chien-rigged.glb` du web) et les icônes/modèles des consommables/power-ups — placeholders en attendant, rien ne plante.
7. **Sweep mineur (non bloquant, avant la Phase 6)** : appliquer le tag `// SOLO-ONLY` partout où le plan Phase 4 le demandait (second souffle, bouclier killstreak, slowmo — actuellement seul `CombatModifiers.cs` l'a) ; corriger le commentaire obsolète sur `PlayerHealth.incomingMult` (mentionne la difficulté à tort, elle est appliquée côté ennemi).
8. **Vérifier visuellement l'accueil (Phase 5, scène `Assets/_Game/Scenes/Home.unity`, Play)** : lobby (cartes Opérateur/Zone de guerre/Intel/Équipement/Objectif, sélecteur de perso 3D animé sur le fond `lobby-bg.jpg`), les 5 onglets de la boutique, les panneaux Map/Réglages/Loadout/Comment jouer, le cycle Déployer → Utopia → mort → Menu → retour Home. Tout ce qui est vérifiable en code (textes, prix, états de boutons) a déjà été confirmé (Task 16) ; reste le regard humain : mise en page, alignements, lisibilité, couleurs, sensation générale.
9. **Importer les clips de danse depuis le web** (`pets`/`animation` du jeu web, danses du perso) : `DanceData.animationClip` n'a aucun clip assigné pour l'instant, donc `LobbyCharacterStage.PlayDance` ne fait rien du tout (no-op gracieux, aucun plantage) quand on clique ▶ dans l'onglet Accessoires → Danses. Une fois les clips copiés dans `Assets/_Game/Art/Animations/` et assignés aux `DanceData`, les danses joueront réellement.
10. ✅ **FAIT (Phase 5.1, 2026-07-09)** — ~~Découpe « tactique » exacte des cartes (optionnel)~~. `TacticalPanel`/`TacticalButton`/`TacticalModalFrame` dessinent les coins coupés en biseau via `Painter2D` (USS ne supportant pas `clip-path`), appliqués à l'accueil, la boutique et les modales. Reste à vérifier à l'œil (point 11).
11. **Vérifier visuellement la Phase 5.1 (playtest polish, scènes `Home.unity` + `Slice.unity`/`Utopia.unity`, Play)** — tout ce qui est vérifiable en code a été confirmé (compilation, 279 EditMode + 7/9 PlayMode, cycles Home→Shop→Modales→Deploy→mort→GameOverScreen sans exception via MCP) ; reste le regard humain, en particulier :
    - **Accueil** : le fond `background.png` s'affiche bien (pas juste en mémoire — le quad a été retourné de 180° pour corriger un bug de face culling, à confirmer visuellement), cartes/bouton Déployer/tuiles à coins coupés lisibles.
    - **Boutique** : sidebar claire lisible, persos alignés avec estompage, tuiles Accessoires/Boost/Consommables groupées par rareté.
    - **Modales** : Map/Réglages/Loadout/Comment-jouer centrées avec cadre à coins coupés, Comment-jouer (la plus large, 760px) ne déborde pas du cadre.
    - **HUD** : plus de libellés de debug, widget Défis (bas-gauche) et marqueur joueur sur le radar visibles en jeu.
    - **Avatar 3ᵉ personne** : la déformation a-t-elle disparu (le flottement est corrigé et vérifié numériquement, la déformation de proportions n'a pas pu être confirmée à l'œil) ? Si ça reste déformé, c'est probablement un problème de mesh Blender, pas de code.
    - **Écran de fin de partie** : jouer une run réelle jusqu'à la mort, vérifier que les 12 cartes/rang/classement affichent des valeurs cohérentes.
    - **Équilibrage** : le jeu doit sembler sensiblement moins punitif qu'avant (régén 6s/plus rapide qu'avant, armure ennemie ajustée).
    - **Chargement** : une barre de progression doit maintenant apparaître pendant Deploy → Utopia (le temps total peut rester long à cause des 39k colliders, hors périmètre — c'est le retour visuel qui compte).

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
| 2026-07-07 | Reste Phase 2 : comportements spéciaux des 10 types, VIP doré, combo + HUD | même branche |
| 2026-07-07 | Map utopia jouable (GLB pleine qualité du .blend, colliders, NavMesh, smoke test) | même branche |
| 2026-07-07 | Phase 4 lot 1 : save `cod-save-v1` + XP/niveaux + boutique (logique) + save→gameplay + HUD argent/XP (150 + 5 tests) | même branche |
| 2026-07-07 | Réflexion Phase 5 (spec + plan, sans code) : inventaire de parité accueil web, 16 tâches prêtes | même branche |
| 2026-07-07 | Playtest Nathan sur Utopia : 6 bugs relevés (arme mal placée, marche/course, anim course, collisions escaliers/vitres, ennemis bloqués sur toit, décor déformé) — voir section « Bugs » | à corriger |
| 2026-07-08 | **Phase 4 complète** (26 tâches, 7 lots) : consommables/grenades, bouclier, killstreaks, modificateurs de combat, loadout+gadgets+chien, modes de tir/mêlée/ramassage/power-ups, succès/contrats/rangs/prestige/leaderboard, difficulté/hardcore/entraînement, pause/mute, audio synthétisé — + 3 bugs playtest corrigés + 1 bug supplémentaire trouvé et corrigé en live (arme géante) + 1 correctif de la revue finale (155→220 tests EditMode) | branche `feat/phase4-systems`, mergée dans `feat/phase3-anims-player-hud` |
| 2026-07-09 | **Phase 5 complète** (16 tâches) : accueil UI Toolkit reproduit à l'identique du web (lobby, boutique 5 onglets, Map/Réglages/Loadout/Comment-jouer, Deploy, retour Menu). Task 16 : passe de vérification dédiée (`HomeSmokeTests`, 279 EditMode + 9 PlayMode, cycle complet Home→Shop→Deploy→Utopia→mort→Home vérifié via MCP, parité écran par écran confirmée) | branche `feat/phase5-home-ui` |
| 2026-07-09 (nuit) | **Phase 5.1 complète** (9 chantiers, 20 tâches, en autonomie complète) : fondations tactiques (`Painter2D`), accueil/boutique restylés (thème clair, coins coupés), Map/Réglages/Loadout/Comment-jouer en modales, HUD nettoyé (widget Défis, marqueur radar), avatar 3e personne recollé au sol, écran de fin de partie riche (12 cartes + rang + classement), équilibrage armure/régén corrigé (valeurs réelles web), chargement async + barre de progression. 279 EditMode + 7/9 PlayMode (2 skips connus), 0 échec | branche `feat/phase5.1-visual-parity` |
| 2026-07-10 (matin) | **Passe corrective Phase 5.1 après capture de Nathan**, vérifiée par captures d'écran MCP : Shop.uss mort depuis la Phase 5 (`:not()` non supporté → fichier entier invalidé, vraie cause du « boutons partout »), rendu Painter2D réparé (bandes de dégradé, fond bouton par défaut, contentRect vs padding), fond lobby ajusté au frustum (`LobbyBackgroundFitter`), Déployer/vie/bouclier/textes réalignés sur homepage.png + hud.png. 279 + 7/9, 0 régression | même branche |
| 2026-07-12 | Cœur logique du mode en ligne coop (assembly `Game.Net`, hôte autoritaire, ~35 tests + seam Photon inerte) | branche `feat/online-coop`, mergée dans `main` (2026-11-23) |

Détails d'archive : specs dans `docs/superpowers/specs/`, plans dans `docs/superpowers/plans/`, journal de l'import dans `.superpowers/sdd/progress.md`, dette technique de l'import dans `docs/superpowers/tech-debt-import-pipeline.md`.
