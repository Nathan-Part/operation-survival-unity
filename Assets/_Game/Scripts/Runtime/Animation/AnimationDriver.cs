using Game.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Runtime
{
    /// Lecture de clips via Playables (pas d'AnimatorController asset) :
    /// un slot de mixer par rôle, fondus pilotés par CrossfadeModel (parité web).
    /// Clip absent → rôle ignoré, aucun plantage (règle : la logique n'exige jamais un asset).
    public class AnimationDriver : MonoBehaviour
    {
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] playables;
        AnimationClip[] clips;
        CrossfadeModel fade;
        Animator animator;
        int currentSlot = -1;
        bool currentLoops = true;
        bool built;
        System.Action oneShotCallback; // Task 13 (danses) : invoqué une seule fois, à la fin naturelle du one-shot

        // Un slot par valeur d'AnimRole (Idle/Walk/Run/Attack/Dead/Lobby) — dérivé de l'enum plutôt
        // qu'un 5 en dur : l'ajout du rôle Lobby (2026-07-10) faisait entrer son index en collision
        // avec le slot one-shot et HasClip(Lobby) répondait toujours false.
        static readonly int RoleSlotCount = System.Enum.GetValues(typeof(AnimRole)).Length;
        // Task 13 (danses boutique) : slot supplémentaire dédié aux clips ad-hoc hors AnimRole
        // (DanceData.animationClip, pas dans l'AnimationLibrary) — un seul à la fois, recréé à
        // chaque PlayOneShot (retrigger inclus).
        static readonly int OneShotSlot = RoleSlotCount;
        static readonly int SlotCount = RoleSlotCount + 1;

        public void Bind(Animator animator, AnimationLibrary library)
        {
            if (animator == null || library == null) return;
            this.animator = animator;

            graph = PlayableGraph.Create($"AnimDriver_{name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, SlotCount);
            playables = new AnimationClipPlayable[SlotCount];
            clips = new AnimationClip[SlotCount];
            fade = new CrossfadeModel(SlotCount);

            for (int slot = 0; slot < RoleSlotCount; slot++)
            {
                var clip = library.Get((AnimRole)slot);
                if (clip == null) continue;
                clips[slot] = clip;
                playables[slot] = AnimationClipPlayable.Create(graph, clip);
                graph.Connect(playables[slot], 0, mixer, slot);
                mixer.SetInputWeight(slot, 0f);
            }

            var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
            built = true;
        }

        public bool HasClip(AnimRole role) => built && clips[(int)role] != null;

        public void Play(AnimRole role, float fadeSeconds, bool loop = true)
        {
            int slot = (int)role;
            if (!built || clips[slot] == null || slot == currentSlot) return;
            playables[slot].SetTime(0);
            playables[slot].SetSpeed(1);
            if (currentSlot < 0) fade.SnapTo(slot);
            else fade.FadeTo(slot, fadeSeconds);
            currentSlot = slot;
            currentLoops = loop;
        }

        public bool IsPlaying(AnimRole role) => currentSlot == (int)role;

        /// Task 13 (danses boutique) : joue un clip ad-hoc (hors AnimRole, ex. DanceData.animationClip)
        /// une seule fois, puis invoque `onFinished` à la fin naturelle du clip (jamais en boucle — c'est
        /// à l'appelant de relancer la loco walk/idle depuis ce callback, voir LobbyCharacterStage.PlayDance).
        /// Rejouer un clip (même le même) recrée toujours le Playable du slot : un retrigger (re-clic ▶,
        /// ou clic sur une autre danse pendant que la précédente joue) remplace proprement l'ancien one-shot
        /// sans jamais laisser deux clips actifs en même temps sur ce slot.
        public void PlayOneShot(AnimationClip clip, float fadeSeconds, System.Action onFinished)
        {
            if (!built || clip == null) return; // pas de clip assigné : no-op propre, jamais de plantage

            if (playables[OneShotSlot].IsValid())
            {
                mixer.DisconnectInput(OneShotSlot);
                playables[OneShotSlot].Destroy();
            }

            var playable = AnimationClipPlayable.Create(graph, clip);
            playables[OneShotSlot] = playable;
            clips[OneShotSlot] = clip;
            graph.Connect(playable, 0, mixer, OneShotSlot);
            playable.SetTime(0);
            playable.SetSpeed(1);

            if (currentSlot < 0) fade.SnapTo(OneShotSlot);
            else fade.FadeTo(OneShotSlot, fadeSeconds);

            currentSlot = OneShotSlot;
            currentLoops = false;
            oneShotCallback = onFinished;
        }

        public bool IsPlayingOneShot => currentSlot == OneShotSlot;

        /// Interruption explicite (ex. fermeture de la boutique pendant une danse, voir
        /// LobbyCharacterStage.StopDance) : annule le callback de fin en attente SANS l'invoquer (c'est
        /// une interruption, pas une fin naturelle) — laisse volontairement l'état de lecture inchangé,
        /// c'est à l'appelant d'enchaîner avec Play(role, ...) pour transitionner hors du one-shot
        /// (mêmes règles de repli walk/idle que Bind()/ShowCharacter, pas dupliquées ici).
        public void CancelOneShot() => oneShotCallback = null;

        public float TimeIn(AnimRole role)
        {
            int slot = (int)role;
            if (!built || clips[slot] == null) return 0f;
            return (float)playables[slot].GetTime();
        }

        public float ClipLength(AnimRole role)
        {
            int slot = (int)role;
            return built && clips[slot] != null ? clips[slot].length : 0f;
        }

        /// Fige la pose courante (fin d'anim de mort) : coupe l'évaluation, coût quasi nul.
        public void FreezePose()
        {
            if (!built) return;
            if (graph.IsValid()) graph.Stop();
            if (animator != null) animator.enabled = false;
        }

        void Update()
        {
            if (!built || !graph.IsPlaying()) return;
            fade.Tick(Time.deltaTime);
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (clips[slot] == null) continue;
                mixer.SetInputWeight(slot, fade.WeightOf(slot));
            }
            if (currentSlot < 0) return;
            var active = playables[currentSlot];
            float length = clips[currentSlot].length;
            if (currentLoops)
            {
                if (active.GetTime() > length) active.SetTime(active.GetTime() % length); // boucle manuelle (wrap mode d'import non garanti)
            }
            else if (active.GetTime() >= length)
            {
                active.SetTime(length);
                active.SetSpeed(0); // maintien de la dernière frame (clampWhenFinished du web)

                // Task 13 : fin naturelle d'un one-shot (danse) — callback consommé une seule fois (pas
                // à chaque frame suivante tant que rien n'a relancé de lecture) ; ne concerne jamais les
                // rôles AnimRole non bouclés (aucun n'utilise onFinished aujourd'hui, ex. Dead/FreezePose).
                if (currentSlot == OneShotSlot && oneShotCallback != null)
                {
                    var callback = oneShotCallback;
                    oneShotCallback = null;
                    callback.Invoke();
                }
            }
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
