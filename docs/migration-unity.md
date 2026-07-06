# Plan de migration — Web (Three.js) → Unity mobile

**Cible** : FPS mobile style Guns of Boom, Android + iOS, avec **PvP multijoueur** ET **coop contre vagues**.
**Principe directeur** : le jeu web fini sert de **spec fonctionnelle exacte** (formules, équilibrage, IA, économie). On ne redesigne rien, on transpose.

---

## Phase 0 — Finir le jeu web (maintenant, avant tout)

C'est la phase en cours. Objectif : geler le design pour que la version Unity ait une référence stable.

- **Refactor CSS boutique** + autres parties visuelles (workflow habituel : skill `ui-ux-pro-max`, agent `game-frontend`).
- **Nouveaux modèles 3D plus détaillés** — ⚠️ à faire directement « Unity-ready » pour ne pas travailler deux fois :
  - Matériaux **PBR metallic/roughness** (standard glTF, importés tels quels par Unity).
  - Budget mobile : ~10–30k triangles par personnage, textures ≤ 2K.
  - Personnages sur **rig humanoïde standard** (type Mixamo) → le retargeting Mecanim d'Unity remplacera `retarget.js` gratuitement.
  - Garder les sources Blender propres : c'est le `.blend` qui sera ré-exporté vers Unity, pas les GLB optimisés web.
- Continuer à mettre la logique dans des **tables data-driven** (`ENEMY_TYPES`, `POWER_DEBUG`, catalogues boutique, `MAPS`) : chaque table = un futur ScriptableObject, portage quasi mécanique.
- La suite Playwright (825 tests) devient le **catalogue des invariants** à re-vérifier côté Unity.

**Jalon de sortie de phase** : jeu web « fini », design gelé, tag git `v-web-final`.

---

## Phase 1 — Décisions & fondations Unity

- **Unity 6 LTS**, pipeline **URP** (le pipeline mobile — c'est celui de la catégorie Guns of Boom).
- **Choix netcode** (LA décision structurante du projet) :
  - **Photon Fusion** : la référence FPS mobile (Guns of Boom tourne sur Photon), hébergement inclus, gratuit jusqu'à 20 joueurs simultanés.
  - **Unity Netcode for GameObjects + Unity Gaming Services** (Relay/Lobby/Matchmaker) : intégré à Unity, bien pour coop, moins éprouvé pour du PvP compétitif.
  - Recommandation : **Photon Fusion**, parce que le PvP exige de la prédiction/compensation de lag et Photon la fournit clé en main.
- **Backend de progression** : en multi, la save DOIT être côté serveur (sinon triche triviale — le `localStorage` actuel serait éditable). Options : Unity Cloud Save, PlayFab, Firebase.
- **Comptes stores** : Google Play Console (25 $ une fois), Apple Developer (99 $/an, **Mac requis** pour builder iOS).
- Import 3D : export **FBX ou glTF depuis Blender** (package `glTFast` si glTF).

---

## Phase 2 — Vertical slice solo (le cœur, hors ligne)

Reconstruire une tranche jouable minimale, en portant `main.js` système par système. Ordre :

1. **Contrôleur joueur** : `CharacterController` Unity → gravité, marches, pentes **natifs**. Tout le moteur maison disparaît : `groundHeight(x,z)`, `STEP_UP_MAX`, colliders AABB en tables, `pushOutCircleAABB`.
2. **Input tactile** : Unity Input System, joysticks virtuels. Décision design à prendre : **tir automatique** quand le viseur est sur un ennemi (le choix de Guns of Boom, standard du FPS mobile) vs bouton de tir.
3. **Armes** : catalogue JS → **ScriptableObjects** (1 asset par arme : dégâts, cadence, chargeur, rareté…). Tir raycast, tracers, recul — mêmes formules que le web.
4. **Ennemis** : `ENEMY_TYPES` → ScriptableObjects. IA : **NavMesh Unity** remplace le pathfinding/flank/cover maison ; la FSM (idle/chase/cover/burst) se transpose telle quelle. Animator + retargeting Mecanim remplacent `retarget.js`.
5. **Vagues + boss** : `enemiesForWave`/`enemyHpForWave`/compositions — transposition directe des formules.
6. **HUD** : UI Toolkit (santé, munitions, radar, vague, combo, kill feed).

**Jalon** : tuer des vagues d'ennemis au tactile sur un téléphone, 60 fps.

---

## Phase 3 — Maps & assets

- **Blender → Unity en direct** (FBX/glTF). Les hacks web disparaissent : Draco, clipping planes des portes, textures réduites à 1024 — Unity gère les sources pleines et compresse au build (**ASTC**).
- **Colliders Unity** (Box/Mesh colliders posés dans l'éditeur) remplacent toutes les tables de colliders en code (`UTOPIA_CANALS`, les 217 colliders extraits par script…). Gros gain de maintenance.
- **Éclairage cuit** (lightmaps + light probes) + post-processing URP (bloom, AO, tone mapping) = le vrai bond visuel vs Three.js.
- LOD Groups, occlusion culling — la map utopia complète (152×184) en profite directement.
- Portage des 3 maps : utopia d'abord (la plus riche), puis portal, urban.

**Jalon** : utopia jouable en solo sur mobile, visuel nettement au-dessus du web.

---

## Phase 4 — Systèmes méta

- **Save** : structure de `cod-save-v1` → JSON local (solo) + **cloud** (multi). Migration : proposer un import du profil web est possible mais optionnel.
- **Boutique/économie** : catalogues, coûts croissants, upgrades — mêmes formules. Refaire l'UI en UI Toolkit (le refactor CSS de la phase 0 sert de maquette).
- **Progression** : XP, rangs, succès, contrats quotidiens, stats lifetime.
- **Gameplay annexe** : consommables, power-ups, grenades, mêlée, dash, chien de guerre, loadout, killstreaks.
- ⚠️ **Marquer dès maintenant ce qui est solo-only** : beaucoup de power-ups (slow-mo, invincibilité, rampage…) sont impossibles ou toxiques en PvP. Prévoir deux tables d'équilibrage : `PvE` (tout) et `PvP` (sous-ensemble rééquilibré).

**Jalon** : parité fonctionnelle solo avec le jeu web.

---

## Phase 5 — Multijoueur

Dans cet ordre (du plus simple au plus dur) :

1. **Coop contre vagues** (2–4 joueurs) : un joueur hôte ou serveur fait autorité sur les ennemis, les autres synchronisent position/tirs. Premier contact avec le netcode sur un gameplay déjà éprouvé.
2. **PvP** (deathmatch par équipes à la Guns of Boom) :
   - Serveur autoritatif ou host + prédiction client, compensation de lag sur les tirs (fourni par Fusion).
   - Matchmaking + lobbies.
   - **Rééquilibrage obligatoire** : le TTK et les armes sont pensés contre des bots — contre des humains tout change. Prévoir une vraie passe d'équilibrage PvP.
   - Anti-triche : validation des dégâts côté serveur, jamais côté client.

**Jalon** : un match PvP complet entre deux téléphones réels.

---

## Phase 6 — Qualité & sortie

- **Tests** : Unity Test Framework (EditMode pour les formules pures — dégâts, XP, économie ; PlayMode pour le gameplay). Porter en priorité les invariants critiques de la suite Playwright : jamais d'argent à découvert, XP monotone, vagues jamais vides, armure torse vs full…
- **Perf** : Profiler + Frame Debugger, cible 60 fps sur un Android milieu de gamme.
- **Distribution** : Play Console (internal testing → prod), TestFlight → App Store. Prévoir les review guidelines Apple (~1 semaine de délais/allers-retours la première fois).
- Monétisation éventuelle (si souhaitée) : à décider AVANT la sortie, ça influence l'économie.

---

## Récap : ce qui se transpose, disparaît, ou est nouveau

| | |
|---|---|
| **Transposé tel quel** | Formules de dégâts/XP/économie, tables (ennemis, armes, power-ups, rareté), FSM d'IA, structure des vagues, design des maps, modèles 3D |
| **Disparaît** (Unity le fait nativement) | `groundHeight`, colliders AABB en code, `retarget.js`, hacks d'export GLB (Draco, clipping, textures réduites), synthèse Web Audio (→ AudioSources), toute la gestion manuelle de scène Three.js |
| **Nouveau** (n'existe pas dans le web) | Netcode + backend, input tactile, équilibrage PvP, builds/stores/signing, optimisation mobile (ASTC, LOD, batching), éclairage cuit |

## Ordre global

```
Phase 0 (web fini, EN COURS) → 1 (fondations) → 2 (slice solo) → 3 (maps)
→ 4 (méta, parité web) → 5.1 (coop vagues) → 5.2 (PvP) → 6 (sortie)
```

Le solo complet (phases 2–4) sort AVANT le réseau : c'est une v1 publiable en soi, et chaque système aura été validé hors ligne avant d'être synchronisé.
