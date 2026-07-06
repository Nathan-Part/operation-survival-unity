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

        const int SlotCount = 5; // un par AnimRole

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

            for (int slot = 0; slot < SlotCount; slot++)
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
            }
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
