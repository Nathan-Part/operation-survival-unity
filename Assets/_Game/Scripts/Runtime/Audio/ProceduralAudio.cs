using UnityEngine;

namespace Game.Runtime
{
    /// Synthèse procédurale des sons (parité audio.js : tone = oscillateur + enveloppe exp, noise = bruit + lowpass).
    public static class ProceduralAudio
    {
        public enum Waveform { Sine, Square, Triangle, Sawtooth }
        const int Rate = 44100;

        public static AudioClip Tone(float freq, float dur, Waveform wf, float vol, float glideToHz)
        {
            int n = Mathf.Max(1, (int)(Rate * dur)); var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate; float k = (float)i / n;
                float f = Mathf.Lerp(freq, glideToHz, k);          // glissando
                float ph = f * t; float raw = Osc(wf, ph);
                float env = Mathf.Exp(-5f * k);                     // décroissance exponentielle
                s[i] = raw * vol * env;
            }
            var clip = AudioClip.Create("tone", n, 1, Rate, false); clip.SetData(s, 0); return clip;
        }

        public static AudioClip Noise(float dur, float vol, float lowpassHz)
        {
            int n = Mathf.Max(1, (int)(Rate * dur)); var s = new float[n];
            float a = Mathf.Clamp01(lowpassHz / Rate); float prev = 0f; var rng = new System.Random(12345);
            for (int i = 0; i < n; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev += a * (white - prev);                         // lowpass 1-pôle
                float env = Mathf.Exp(-5f * ((float)i / n));
                s[i] = prev * vol * env;
            }
            var clip = AudioClip.Create("noise", n, 1, Rate, false); clip.SetData(s, 0); return clip;
        }

        static float Osc(Waveform wf, float phase)
        {
            float x = phase - Mathf.Floor(phase);
            switch (wf)
            {
                case Waveform.Square: return x < 0.5f ? 1f : -1f;
                case Waveform.Sawtooth: return 2f * x - 1f;
                case Waveform.Triangle: return 1f - 4f * Mathf.Abs(Mathf.Round(x) - x);
                default: return Mathf.Sin(2f * Mathf.PI * x);
            }
        }
    }
}
