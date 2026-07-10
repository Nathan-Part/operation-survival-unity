using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Perso 3D du lobby (parité web `spawnModel`/`playLobbyIdle`, main.js:497-536) : instancie
    /// le GLB du perso sélectionné devant la caméra fixe (HomeSceneBuilder) et boucle le MÊME clip
    /// que le web : Casual_Walk (AnimRole.Lobby, anim-lobby.glb copié du web le 2026-07-10) — voir
    /// PlayLobbyLocomotion pour l'ordre des replis.
    public class LobbyCharacterStage : MonoBehaviour
    {
        // [SerializeField] (pas juste un champ privé) : assigné par HomeSceneBuilder à l'édition,
        // doit survivre à la sérialisation de la scène pour être encore présent quand le Play Mode
        // recharge le domaine et réinstancie la scène depuis le disque (contrairement à un champ
        // privé nu, qui repartirait à null au runtime réel).
        [SerializeField] GameDatabase db;
        GameObject visual;
        AnimationDriver driver;
        string currentCharacterId;
        GameObject leftNeighbor, rightNeighbor; // trio 3D de l'onglet Personnages (Phase 5 bis, point 9)

        // Task 13 (essai boutique) : un SpriteRenderer par type d'accessoire (Masque/Costume), pas un
        // seul partagé — parité web (`shopAccPreview = {helmet:null, costume:null}`, main.js:220) où
        // les deux slots sont indépendants : prévisualiser un masque n'efface pas un costume déjà en
        // essai (et inversement). Attachés à `transform` (pas à `visual`) : ils survivent à un Render()
        // de l'onglet, seul ShowCharacter (changement de perso) ou ClearAccessoryPreview les masque.
        readonly Dictionary<AccessoryType, SpriteRenderer> accessoryPreviewRenderers = new Dictionary<AccessoryType, SpriteRenderer>();

        const float FadeSeconds = 0.2f; // web: crossfade loco générique du projet

        public void Init(GameDatabase database) => db = database;

        /// Masque/ré-affiche le perso courant sans le détruire (vitrine Armes de la boutique,
        /// Phase 5 bis point 8) — l'anim et l'état de sélection survivent au passage d'onglet.
        public void SetVisible(bool visible)
        {
            if (visual != null) visual.SetActive(visible);
        }

        public void ShowCharacter(string characterId)
        {
            if (db == null || string.IsNullOrEmpty(characterId) || characterId == currentCharacterId) return;

            var data = System.Array.Find(db.characters, c => c != null && c.id == characterId);
            if (data == null) return; // id de save inconnu au catalogue : on ignore proprement, jamais de crash UI

            if (visual != null) Destroy(visual);
            driver = null;
            currentCharacterId = characterId;

            var model = data.ResolveModel(db.placeholders);
            if (model == null) return; // pas de placeholder configuré : rien à afficher, pas de plantage

            visual = SpawnAnimatedVisual(data, Vector3.zero, out driver);
            if (visual != null) PlayLobbyLocomotion();
        }

        /// Instancie un perso animé (loco lobby) enfant du stage — partagé entre le perso central
        /// (ShowCharacter) et les voisins du trio de l'onglet Personnages (point 9).
        GameObject SpawnAnimatedVisual(CharacterData data, Vector3 localPos, out AnimationDriver spawnedDriver)
        {
            spawnedDriver = null;
            var model = data.ResolveModel(db.placeholders);
            if (model == null) return null; // pas de placeholder configuré : rien à afficher, pas de plantage

            var go = Instantiate(model, transform);
            go.transform.localPosition = localPos; // pivot GLB aux pieds
            go.transform.localRotation = Quaternion.identity;

            if (db.animationLibrary != null)
            {
                var animator = go.GetComponent<Animator>();
                if (animator == null) animator = go.AddComponent<Animator>();
                spawnedDriver = go.AddComponent<AnimationDriver>();
                spawnedDriver.Bind(animator, db.animationLibrary);
            }
            return go;
        }

        /// Trio de l'onglet Personnages (parité shop-perso.png) : le voisin précédent à gauche,
        /// le suivant à droite, légèrement en retrait. Idempotent (re-appel = remplace).
        public void ShowNeighbors(CharacterData left, CharacterData right)
        {
            ClearNeighbors();
            leftNeighbor = SpawnNeighbor(left, new Vector3(-1.8f, 0f, -0.6f));
            rightNeighbor = SpawnNeighbor(right, new Vector3(1.8f, 0f, -0.6f));
        }

        public void ClearNeighbors()
        {
            if (leftNeighbor != null) Destroy(leftNeighbor);
            if (rightNeighbor != null) Destroy(rightNeighbor);
            leftNeighbor = rightNeighbor = null;
        }

        GameObject SpawnNeighbor(CharacterData data, Vector3 localPos)
        {
            if (data == null || data.id == currentCharacterId) return null; // jamais de doublon du perso central
            var go = SpawnAnimatedVisual(data, localPos, out var neighborDriver);
            if (go != null && neighborDriver != null)
            {
                if (neighborDriver.HasClip(AnimRole.Lobby)) neighborDriver.Play(AnimRole.Lobby, FadeSeconds);
                else if (neighborDriver.HasClip(AnimRole.Idle)) neighborDriver.Play(AnimRole.Idle, FadeSeconds);
            }
            return go;
        }

        /// Task 13 : surimpression de l'image PNG d'un accessoire sur le perso 3D (essai en direct —
        /// écart assumé spec §4.5 : le web attache un vrai mesh à un os (`attachToBoneWorld`,
        /// main.js:189-198+226-227), ici un plan 2D (SpriteRenderer) positionné approximativement près
        /// de la tête (Masque) ou du torse (Costume), sans accroche squelette réelle — l'accroche 3D
        /// viendra avec les modèles. Utilise `AccessoryData.image` directement (PAS `ResolveIcon`) :
        /// un repli sur l'icône générique donnerait un aperçu mensonger (silhouette générique flottant
        /// près du perso comme si c'était l'article réel) — id inconnu/pas d'image assignée → aucune
        /// surimpression, jamais de plantage (règle : la logique n'exige jamais un asset).
        public void PreviewAccessory(string accessoryId)
        {
            if (db == null || string.IsNullOrEmpty(accessoryId)) return;

            var data = System.Array.Find(db.accessories, a => a != null && a.id == accessoryId);
            if (data == null) return; // id inconnu au catalogue : ignoré proprement

            var renderer = EnsureAccessoryPreviewRenderer(data.accessoryType);
            if (data.image == null) { renderer.enabled = false; return; } // ex. "Aucun" (helm:none/cost:none)

            renderer.sprite = data.image;
            renderer.enabled = true;

            // Taille cible en mètres (approximative, pas de mesure réelle du perso) normalisée sur la
            // plus grande dimension du sprite — reste correcte quelle que soit la résolution du PNG
            // source (les 5 PNG copiés en Task 1 ne sont pas tous à la même échelle de pixels).
            float targetSize = data.accessoryType == AccessoryType.Helmet ? 0.3f : 0.6f;
            float height = data.accessoryType == AccessoryType.Helmet ? 1.65f : 1.1f;
            Vector2 boundsSize = data.image.bounds.size;
            float maxDim = Mathf.Max(boundsSize.x, boundsSize.y, 0.0001f);
            renderer.transform.localScale = Vector3.one * (targetSize / maxDim);
            renderer.transform.localPosition = new Vector3(0f, height, 0.16f);
        }

        /// Annule tout essai en cours sur les deux slots (masque + costume) — appelé à chaque
        /// changement d'onglet ET à la fermeture de la boutique (parité web : `clearAccessoryPreview`
        /// appelée par `setShopTab`, main.js:1423, et `closeShop`, main.js:1408). Le web restaure
        /// l'accessoire réellement équipé sur le perso (`applyAccessories`) ; ce projet n'a pas encore
        /// de rendu persistant des accessoires équipés dans le lobby (hors scope Task 13), donc
        /// "restaurer" se limite à masquer l'aperçu — état par défaut identique à "rien équipé de
        /// visible", jamais un plantage ni un aperçu qui traîne après la fermeture.
        public void ClearAccessoryPreview()
        {
            foreach (var renderer in accessoryPreviewRenderers.Values)
                if (renderer != null) renderer.enabled = false;
        }

        SpriteRenderer EnsureAccessoryPreviewRenderer(AccessoryType type)
        {
            if (accessoryPreviewRenderers.TryGetValue(type, out var existing) && existing != null) return existing;

            var go = new GameObject($"AccessoryPreview_{type}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10; // devant le perso, jamais un souci de tri avec le décor lobby
            renderer.enabled = false;
            accessoryPreviewRenderers[type] = renderer;
            return renderer;
        }

        /// Task 13 : joue une danse une seule fois, même non possédée (parité web `previewDance`,
        /// main.js:3727-3736 : toujours jouable en aperçu boutique, indépendant de `ownedDances`).
        /// `danceId` (pas le nom brut du clip Unity) : même convention que ShowCharacter(characterId) —
        /// LobbyCharacterStage résout lui-même l'entrée du catalogue et gère proprement l'absence
        /// d'asset, au lieu de faire porter cette résolution à l'appelant boutique.
        public void PlayDance(string danceId)
        {
            if (db == null || driver == null || string.IsNullOrEmpty(danceId)) return;

            var dance = System.Array.Find(db.dances, d => d != null && d.id == danceId);
            // Pas de clip assigné (aucune danse n'en a au moment de cette tâche, voir rapport de tâche) :
            // no-op propre, jamais de plantage — règle "la logique n'exige jamais un asset".
            if (dance == null || dance.animationClip == null) return;

            driver.PlayOneShot(dance.animationClip, FadeSeconds, ReturnToLocomotion);
        }

        /// Interruption explicite d'une danse en cours (choix documenté, voir rapport de tâche) :
        /// appelée à la fermeture de la boutique (ShopScreenController.Close). Le changement de
        /// personnage (ShowCharacter) et le déclenchement d'une AUTRE danse (PlayOneShot) n'ont pas
        /// besoin de cet appel : ShowCharacter détruit tout le GameObject+driver (rien à laisser
        /// propre), et PlayOneShot remplace déjà proprement le one-shot précédent sur retrigger.
        public void StopDance()
        {
            if (driver == null || !driver.IsPlayingOneShot) return;
            driver.CancelOneShot();
            ReturnToLocomotion();
        }

        // web: playLobbyIdle (implicite via previewDance/ShowCharacter) — même repli walk-sinon-idle
        // que ShowCharacter ci-dessus, factorisé ici pour servir à la fois de callback de fin naturelle
        // (PlayOneShot) et d'action d'interruption explicite (StopDance).
        void ReturnToLocomotion() => PlayLobbyLocomotion();

        /// Boucle du lobby, dans l'ordre du web : Casual_Walk (AnimRole.Lobby) d'abord — les replis
        /// Walk (walking_man, marche rapide) puis Idle (Alert, posture de combat) lisaient comme un
        /// perso "déformé" (retour Nathan 2026-07-10).
        void PlayLobbyLocomotion()
        {
            if (driver == null) return;
            if (driver.HasClip(AnimRole.Lobby)) driver.Play(AnimRole.Lobby, FadeSeconds);
            else if (driver.HasClip(AnimRole.Walk)) driver.Play(AnimRole.Walk, FadeSeconds);
            else if (driver.HasClip(AnimRole.Idle)) driver.Play(AnimRole.Idle, FadeSeconds);
        }
    }
}
