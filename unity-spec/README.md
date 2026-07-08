# Unity Spec — spec gameplay portable extraite du jeu web

Ce dossier est la **référence d'équilibrage et de contenu** pour la version Unity, extraite de `src/game/main.js` (2026-07-06). C'est un instantané **découplé du code web** : la version Unity se construit à partir de ces fichiers, pas en lisant main.js.

## Fichiers

| Fichier | Contenu | Cible Unity |
|---|---|---|
| `weapons.json` | 15 armes (stats, prix, rareté, descriptions) + système d'upgrades | ScriptableObjects `WeaponData` |
| `enemies.json` | 16 types d'ennemis + constantes IA/combat + zones de dégâts | ScriptableObjects `EnemyData` |
| `waves.json` | Formules de vagues (nombre, HP, composition, boss) | Classe statique `WaveRules` |
| `player.json` | 3 persos + upgrades + mouvement (vitesses, dash, stamina, chute) | `CharacterData` + config `PlayerController` |
| `economy.json` | Argent, XP, combo, difficultés, raretés, loadout | Classe statique `Economy` + enums |
| `items.json` | Accessoires, boosts, consommables, danses | ScriptableObjects par catégorie |
| `powerups.json` | 24 power-ups (durées, effets) + drop rates | ScriptableObjects `PowerUpData` |

## Principe de modularité (important)

**Les assets arrivent au fur et à mesure** — la spec est conçue pour ça :

- Chaque entrée a un bloc `assets` (`model`, `icon`, `image`) qui vaut `null` tant que l'asset n'existe pas. La logique ne dépend JAMAIS d'un asset : une arme sans modèle utilise un placeholder, exactement comme le web fait un fallback icône.
- Pour ajouter un asset plus tard : remplir le champ dans le JSON + déposer le fichier. Zéro changement de logique.
- Côté Unity, même principe : le champ modèle d'un `WeaponData` peut rester vide, le code doit prévoir un prefab placeholder par catégorie.
- Ajouter un contenu (arme, ennemi, power-up) = ajouter une entrée dans le JSON correspondant. Les règles (waves.json) référencent les types par id.

## Workflow d'import côté Unity

Deux options (à trancher en phase 2) :

1. **Import automatique** : un script Editor (`ScriptedImporter` ou menu custom) lit ces JSON et génère/actualise les ScriptableObjects. Avantage : ce dossier reste LA source de vérité, on peut rééquilibrer ici et resynchroniser.
2. **Saisie manuelle** : créer les ScriptableObjects à la main depuis ces valeurs. Plus simple au départ, mais risque de divergence.

Recommandation : option 1 — l'équilibrage va encore bouger (surtout avec le PvP).

## PvP : champ `pvp` dans powerups.json

Le jeu web est équilibré **contre des bots**. Chaque power-up porte une recommandation (`allowed` / `rebalance` / `exclude`) pour le futur mode PvP — à trancher lors de la vraie passe d'équilibrage PvP (phase 5). Même logique à prévoir pour le TTK des armes (probablement via une table d'override `weapons-pvp.json`, à créer à ce moment-là).

## Ce qui n'est PAS encore extrait (à compléter avant/pendant la phase 2)

- Succès (`ACHIEVEMENTS`), contrats quotidiens, prestige — extraction facile, même méthode.
- Killstreaks/bonus de kill (#139/#149/#161/#166/#182/#187/#190) — les valeurs sont dans main.js.
- Maps : la géométrie vient des `.blend` (voir `docs/ROADMAP.md`, Phase 3), pas de ce dossier.

## Rappel

La version web reste jouable et **continue d'évoluer** (phase 0). À chaque changement d'équilibrage côté web, mettre à jour le JSON correspondant ici (ou ré-extraire) pour que la spec reste exacte.
