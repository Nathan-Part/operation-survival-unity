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

L'**étape 2** ci-dessus ("reconstruire une version solo jouable") est **livrée dans sa version minimale** (2026-07-06, branche `feat/vertical-slice-solo`) : **le jeu est maintenant visible et jouable** — on se déplace en vue FPS, on tue des vagues d'ennemis (tir automatique façon Guns of Boom), avec HUD et joysticks virtuels. Le préalable "import des données" (les 10 sous-tâches de l'ancienne tâche en cours) était déjà terminé et mergé dans `main`.

Ce qui reste volontairement en dehors de cette première slice (fait plus tard) : comportements spéciaux des types d'ennemis (bomber qui explose, medic qui soigne…), power-ups en partie, combo, changement d'arme, audio, vrais modèles 3D (des capsules colorées servent de placeholders).

**Prochaine étape à décider : passer à l'étape 3 (vraies maps + visuel) ou compléter la slice d'abord.**
