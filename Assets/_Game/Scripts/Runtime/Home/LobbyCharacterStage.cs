using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Perso 3D du lobby (parité web `spawnModel`/`playLobbyIdle`, main.js:497-536) : instancie
    /// le GLB du perso sélectionné devant la caméra fixe (HomeSceneBuilder) et joue une anim de
    /// locomotion en boucle. Simplification assumée : le web choisit un clip "walk" calme
    /// (Casual_Walk en priorité) et le boucle indéfiniment — ce n'est PAS une séquence en deux
    /// temps "marche puis idle", juste une boucle de marche perpétuelle tant que le lobby est
    /// affiché. On reproduit ça via AnimationDriver : AnimRole.Walk en boucle, repli Idle si le
    /// perso n'a pas de clip de marche.
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

        const float FadeSeconds = 0.2f; // web: crossfade loco générique du projet

        public void Init(GameDatabase database) => db = database;

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

            visual = Instantiate(model, transform);
            visual.transform.localPosition = Vector3.zero; // pivot GLB aux pieds
            visual.transform.localRotation = Quaternion.identity;

            if (db.animationLibrary != null)
            {
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                driver = visual.AddComponent<AnimationDriver>();
                driver.Bind(animator, db.animationLibrary);

                if (driver.HasClip(AnimRole.Walk)) driver.Play(AnimRole.Walk, FadeSeconds);
                else if (driver.HasClip(AnimRole.Idle)) driver.Play(AnimRole.Idle, FadeSeconds);
            }
        }

        /// Phase boutique (Task 12+) : surimpression de l'image PNG d'un accessoire sur le perso 3D
        /// (essai en direct — spec §4.5, pas d'accroche 3D). Stub v1 : pas de contenu de boutique
        /// encore construit à cette étape, juste la signature attendue par les tâches suivantes.
        public void PreviewAccessory(string accessoryId)
        {
        }
    }
}
