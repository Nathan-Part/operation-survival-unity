# Roadmap simple — vue d'ensemble du projet

> Ce fichier ne remplace pas `docs/migration-unity.md` (le plan détaillé) ni `unity-spec/NEXT-STEPS.md` (le suivi précis). C'est juste une vue "une ligne par étape" pour voir tout le chemin d'un coup d'œil.

## Vue générale — les 7 grandes étapes du projet (Phase 0 à 6)

0. **Finir le jeu web** (Three.js) — geler le design pour qu'il serve de référence stable. *(Se passe ailleurs, pas dans ce dépôt Unity.)*
1. **Poser les fondations Unity** — créer le projet, choisir le réseau (Photon Fusion pressenti), les comptes stores.
2. **Reconstruire une version solo jouable minimale** — contrôleur du joueur, tir, un ennemi, les vagues, un HUD. *(Le jeu devient visible/jouable ici.)*
3. **Refaire les maps avec du vrai visuel Unity** — import Blender direct, éclairage, décor (niveau "Guns of Boom").
4. **Refaire tous les systèmes autour du jeu** — boutique, sauvegarde, progression, power-ups. Le solo est alors complet.
5. **Ajouter le multijoueur** — d'abord la coop contre des vagues, ensuite le PvP entre joueurs.
6. **Finitions et sortie** — tests, optimisation mobile, publication sur les stores (Android + iOS).

**Le solo complet (étapes 2 à 4) sort avant le multijoueur.**

---

## Où on en est précisément

On est dans l'**étape 2** ci-dessus ("reconstruire une version solo jouable"), mais **avant même son premier point** : on prépare d'abord la donnée qui va nourrir tout le reste. C'est un prérequis technique invisible, décrit comme "étape 3" dans `unity-spec/NEXT-STEPS.md` :

**Tâche en cours : importer automatiquement les fichiers JSON du jeu web (armes, ennemis, persos, économie...) en fiches de données Unity ("ScriptableObjects").**

Rien n'est encore jouable ni visible à l'écran — c'est de la plomberie de données. Le premier truc qu'on pourra voir/tester en jeu arrivera juste après, avec le contrôleur du joueur.

### Détail de cette tâche en cours (10 sous-tâches, faites une par une)

1. ✅ Squelette du projet (dossiers de code, catégories d'armes/raretés, système de "placeholder" pour les modèles 3D manquants) — **fait**
2. ✅ Formules de jeu (calcul des vagues, de l'argent, des upgrades, des stats) — **fait** (15 tests passent)
3. ✅ Import des **armes** (WeaponData + toute la plomberie d'import partagée) — **fait**
4. 🔄 Import des **ennemis** (16 types) — **en cours**
5. ⬜ Import des **personnages** jouables (3 persos + réglages de déplacement)
6. ⬜ Import de l'**économie** (prix, gains...)
7. ⬜ Import des **objets** (accessoires, boosts, consommables, danses)
8. ⬜ Import des **power-ups**
9. ⬜ Import des **vagues** (composition des vagues d'ennemis)
10. ⬜ Assemblage final : une base de données globale + un bouton de menu Unity "Importer tout"

Une fois ces 10 sous-tâches finies, cette tâche technique sera terminée — mais ce ne sera **toujours qu'un préalable** à l'étape 2 (le jeu jouable), pas la fin du projet.
