# NEXT-STEPS — reprendre la migration Unity ici

**Contexte pour toute session (Nathan ou Claude Code) qui reprend ce chantier.** Mis à jour : 2026-07-06.

## Où on en est

Le jeu web (Three.js, ce dépôt) est **presque fini** et continue d'évoluer en parallèle (phase 0 : refactors CSS, nouveaux modèles 3D, map en cours dans Blender). Décision prise avec Nathan : **portage vers Unity (C#)** pour une sortie mobile **Android + iOS**, gameplay **PvP multijoueur + coop contre vagues**, niveau visuel visé « Guns of Boom ». Approche **incrémentale** : on démarre Unity avec les assets existants (GLB Meshy = placeholders valables), les nouveaux persos/maps s'ajoutent au fur et à mesure.

Documents de référence :

- **`docs/migration-unity.md`** — le plan complet en 6 phases (à lire en premier).
- **`unity-spec/*.json`** — la spec gameplay extraite de `src/game/main.js` : armes, ennemis, vagues, persos, économie, items, power-ups. C'est la **source de vérité d'équilibrage** pour Unity — ne pas relire main.js pour deviner les valeurs, elles sont ici.
- **`unity-spec/README.md`** — comment sont structurés les JSON (blocs `assets` remplissables à `null`, champ `pvp`, liste de ce qui n'est pas encore extrait).

## Prochaines actions, dans l'ordre

### 1. Installer Unity (action de Nathan, peut-être déjà fait)

Unity Hub → **Unity 6 LTS** + modules **Android Build Support** (SDK/NDK/OpenJDK) et **iOS Build Support**. Licence Personal gratuite.

### 2. Créer le projet Unity

- Template **Universal 3D** (URP) — PAS Built-in, PAS HDRP (URP = le pipeline mobile).
- Nom suggéré : `CodMobile` ou équivalent. **Hors de ce dépôt web** (projet séparé, son propre git).
- Réglages initiaux : Build Profiles → Android en plateforme active ; Player Settings → orientation paysage ; Quality → cible 60 fps.
- Structure de dossiers suggérée : `Assets/_Game/{Scripts,Data,Prefabs,Scenes,Art,UI}`.

### 3. Importer la spec (première tâche de code) — ✅ FAIT (2026-07-06)

Pipeline d'import complet livré et mergé dans `main` (10 tâches TDD, 31/31 tests, revue finale « ready to merge »). Le menu **`Tools/Game/Import Spec from JSON`** lit `unity-spec/*.json` et génère les ScriptableObjects dans `Assets/_Game/Data/` :

- `weapons.json` → `WeaponData` (15) · `enemies.json` → `EnemyData` (16) · `player.json` → `CharacterData` (3) + `MovementConfig`
- `economy.json`/`waves.json`/`powerups.json`/`items.json` → `EconomyConfig`, `WaveConfig`, `PowerUpData` (24), accessoires/boosts/consommables/danses
- `GameDatabase.asset` (point d'entrée runtime) référence tout ; 91 assets générés et commités.
- Champs d'art (`model`/`icon`/`image`) jamais écrits par l'import → assignation manuelle + `PlaceholderLibrary` en secours. Formules (vagues/éco/upgrades/stats) écrites à la main en C# et testées.

Détail : plan `docs/superpowers/plans/2026-07-06-spec-import-scriptableobjects.md`, dette technique connue `docs/superpowers/tech-debt-import-pipeline.md`. **À faire avant de dépendre du runtime :** remplir `PlaceholderLibrary` (prefabs/sprites de secours) à la main dans l'Inspector.

### 4. Phase 2 du plan : vertical slice solo — ✅ FAIT (2026-07-06, branche `feat/vertical-slice-solo`)

Slice jouable livrée : contrôleur joueur complet (marche/course/stamina/saut/gravité/dash/dégâts de chute/second wind depuis `MovementConfig`), **tir automatique** façon Guns of Boom (décision actée, spec `docs/superpowers/specs/2026-07-06-vertical-slice-solo-design.md`), zones de dégâts par hauteur d'impact, ennemi générique data-driven ranged/mêlée (NavMesh + FSM), vagues avec la composition exacte de `waves.json`, HUD uGUI (vie, stamina, munitions, vague, radar, écran de mort) et joysticks virtuels. Scène : menu **`Tools/Game/Build Slice Scene`** → `Assets/_Game/Scenes/Slice.unity`. 83 tests EditMode + 2 smoke PlayMode verts.

Différé (assumé, voir spec §6) : comportements spéciaux des types (bomber/medic/shielder…, ils utilisent leur archétype de base), VIP, power-ups, combo/killstreaks, changement d'arme, audio. 4 valeurs absentes des JSON implémentées avec défaut `// TODO(spec-web)` : mult. d'armure, délai de régén, réserve de munitions, portée radar — **à vérifier côté web puis reporter dans les JSON**.

### 5. Prochaine étape — à décider avec Nathan

Options : Phase 3 (maps Blender + vrai visuel) ou compléter la slice (comportements spéciaux des ennemis, extraction des valeurs manquantes). Assets provisoires : les GLB de `perso/` et `artefact/` du dépôt web restent importables comme placeholders (le runtime affiche des capsules en attendant).

## Règles à respecter

- **Ne pas toucher au jeu web depuis le projet Unity** — le web vit sa vie (phase 0), c'est un autre chantier avec ses propres règles (voir `CLAUDE.md` racine, notamment ZÉRO lancer Playwright par Claude).
- **Si l'équilibrage change côté web**, mettre à jour les JSON d'ici pour que la spec reste exacte (elles sont la référence Unity).
- **Les maps ne viennent PAS de ce dossier** : source = les `.blend` de Nathan (`Desktop\3D\map_utopia\map_utopia_export.blend`), ré-export pleine qualité pour Unity (PAS les GLB web, qui sont volontairement dégradés : textures 1024 JPEG, Draco).
- **Multi** : rien à coder avant la phase 5. Choix pressenti : **Photon Fusion**. La seule contrainte anticipée : penser la progression/save pour une future validation serveur (pas de logique d'achat purement client).
- **Extraction restante** (à faire au fil de la phase 4, même méthode que les JSON existants) : succès, contrats quotidiens, killstreaks/bonus de kill, stats détaillées des consommables, chien de guerre (pet.js), prestige.

## Décisions déjà actées (ne pas re-débattre)

- Unity + C# (pas de C++, pas de moteur maison, pas Unreal) — niveau visuel Guns of Boom, URP suffit.
- Android + iOS.
- PvP + coop vagues, mais **le solo complet sort d'abord** (v1 publiable), réseau ensuite (coop avant PvP).
- Le code web sert de spec fonctionnelle, pas de base à traduire ligne à ligne.
- Assets ajoutés au fur et à mesure → tout doit rester modulable (placeholders partout).
