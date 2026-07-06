using UnityEngine;

namespace Game.Runtime
{
    /// Poids de fondu entre N slots d'animation (parité web : crossFadeTo linéaire).
    /// Pure : l'AnimationDriver applique les poids au mixer Playables.
    public class CrossfadeModel
    {
        readonly float[] weights;
        float fadeSeconds;

        public int Target { get; private set; }

        public CrossfadeModel(int slotCount)
        {
            weights = new float[slotCount];
            if (slotCount > 0) weights[0] = 1f;
        }

        public void SnapTo(int slot)
        {
            for (int i = 0; i < weights.Length; i++) weights[i] = i == slot ? 1f : 0f;
            Target = slot;
            fadeSeconds = 0f;
        }

        public void FadeTo(int slot, float seconds)
        {
            Target = slot;
            fadeSeconds = seconds;
            if (seconds <= 0f) SnapTo(slot);
        }

        public void Tick(float dt)
        {
            if (fadeSeconds <= 0f) return;
            // Le poids cible monte linéairement ; les autres se partagent le reste
            // au prorata de leurs poids courants (somme toujours égale à 1).
            float next = Mathf.MoveTowards(weights[Target], 1f, dt / fadeSeconds);
            float othersSum = 0f;
            for (int i = 0; i < weights.Length; i++)
                if (i != Target) othersSum += weights[i];
            float remaining = 1f - next;
            for (int i = 0; i < weights.Length; i++)
                weights[i] = i == Target ? next
                    : othersSum > 0f ? weights[i] * remaining / othersSum : 0f;
            if (next >= 1f) fadeSeconds = 0f;
        }

        public float WeightOf(int slot) => weights[slot];
    }
}
