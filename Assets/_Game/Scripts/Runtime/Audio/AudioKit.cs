using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Banques de sons synthétisés (parité web audio.js) jouées par nom sur une AudioSource
    /// partagée + mute global (M). Construit une fois en Awake, aucune dépendance à un asset audio.
    public class AudioKit : MonoBehaviour
    {
        /// Référence de commodité pour le câblage cross-cutting (ex : son de tir posé à
        /// chaque spawn d'ennemi dans EnemySpawner, bien avant que SliceBootstrap ne
        /// puisse s'abonner directement à un EnemyController qui n'existe pas encore).
        public static AudioKit Instance { get; private set; }

        readonly Dictionary<string, AudioClip[]> banks = new Dictionary<string, AudioClip[]>();
        AudioSource src;
        bool muted;

        void Awake()
        {
            Instance = this;
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            banks["shot"] = new[] { ProceduralAudio.Noise(0.08f, 0.6f, 2200f), ProceduralAudio.Tone(180f, 0.07f, ProceduralAudio.Waveform.Square, 0.25f, 60f) };
            banks["enemyShot"] = new[] { ProceduralAudio.Noise(0.07f, 0.35f, 1200f), ProceduralAudio.Tone(120f, 0.07f, ProceduralAudio.Waveform.Square, 0.18f, 50f) };
            banks["hit"] = new[] { ProceduralAudio.Tone(900f, 0.06f, ProceduralAudio.Waveform.Triangle, 0.4f, 1300f) };
            banks["enemyDie"] = new[] { ProceduralAudio.Tone(220f, 0.35f, ProceduralAudio.Waveform.Sawtooth, 0.45f, 60f), ProceduralAudio.Noise(0.18f, 0.3f, 700f) };
            banks["playerHit"] = new[] { ProceduralAudio.Tone(110f, 0.18f, ProceduralAudio.Waveform.Sine, 0.6f, 70f) };
            banks["gameover"] = new[] { ProceduralAudio.Tone(440f, 0.9f, ProceduralAudio.Waveform.Sine, 0.5f, 90f) };
            banks["reload"] = new[] { ProceduralAudio.Noise(0.03f, 0.3f, 2500f), ProceduralAudio.Noise(0.03f, 0.3f, 2500f) };
            banks["melee"] = new[] { ProceduralAudio.Noise(0.09f, 0.10f, 5000f), ProceduralAudio.Tone(90f, 0.12f, ProceduralAudio.Waveform.Sawtooth, 0.35f, 180f) };
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetVolume(float v) => src.volume = v;

        public void ToggleMute()
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        public void Play(string name)
        {
            if (muted || !banks.TryGetValue(name, out var clips)) return;
            foreach (var c in clips) src.PlayOneShot(c);
        }
    }
}
