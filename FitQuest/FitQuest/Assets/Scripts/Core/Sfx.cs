using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Ретро-звуки. Они не лежат в файлах, а вычисляются при запуске:
    /// прямоугольная волна (как в старых приставках) и шум.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioSource source;
        static float volume = 0.8f;

        public static float Volume
        {
            get { return volume; }
            set { volume = Mathf.Clamp01(value); }
        }
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Init(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;

            clips.Clear();
            clips["jump"] = Sweep("jump", 280f, 720f, 0.16f, false);
            clips["squat"] = Sweep("squat", 420f, 180f, 0.14f, false);
            clips["punch"] = Sweep("punch", 220f, 60f, 0.12f, true);
            clips["hands"] = Sweep("hands", 500f, 900f, 0.12f, false);
            clips["hurt"] = Sweep("hurt", 380f, 90f, 0.32f, false);
            clips["break"] = Sweep("break", 160f, 40f, 0.20f, true);
            clips["good"] = Notes("good", new[] { 660f, 880f }, 0.06f);
            clips["coin"] = Notes("coin", new[] { 988f, 1319f }, 0.07f);
            clips["heart"] = Notes("heart", new[] { 523f, 659f, 784f }, 0.08f);
            clips["bosshit"] = Notes("bosshit", new[] { 196f, 147f, 98f }, 0.09f);
            clips["start"] = Notes("start", new[] { 392f, 523f, 659f }, 0.09f);
            clips["win"] = Notes("win", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.11f);
            clips["lose"] = Notes("lose", new[] { 392f, 330f, 262f, 196f }, 0.18f);
            clips["click"] = Sweep("click", 900f, 600f, 0.04f, false);
            clips["buy"] = Notes("buy", new[] { 784f, 988f, 1175f, 1568f }, 0.06f);
            clips["achieve"] = Notes("achieve", new[] { 523f, 784f, 659f, 1047f }, 0.09f);
            clips["weak"] = Sweep("weak", 300f, 220f, 0.12f, false);
            clips["shield"] = Sweep("shield", 1200f, 300f, 0.25f, true);
            clips["tick"] = Notes("tick", new[] { 880f }, 0.05f);
            clips["blip"] = Notes("blip", new[] { 740f }, 0.022f);       // голос в диалогах
            clips["blip_lo"] = Notes("blip_lo", new[] { 220f }, 0.03f);
        }

        public static void Play(string name, float volume = 0.6f)
        {
            if (source == null) return;
            AudioClip clip;
            if (clips.TryGetValue(name, out clip) && clip != null) source.PlayOneShot(clip, volume * Sfx.volume);
        }

        /// <summary>Звук с плавно меняющейся высотой: от fromHz до toHz.</summary>
        static AudioClip Sweep(string name, float fromHz, float toHz, float duration, bool noisy)
        {
            int count = (int)(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(7);
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(fromHz, toHz, t);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float wave = Mathf.Sin(phase) >= 0f ? 1f : -1f;
                if (noisy) wave = wave * 0.5f + (float)(random.NextDouble() * 2.0 - 1.0) * 0.5f;
                data[i] = wave * (1f - t) * 0.25f;
            }

            return ToClip(name, data);
        }

        /// <summary>Короткая мелодия из нескольких нот одинаковой длины.</summary>
        static AudioClip Notes(string name, float[] frequencies, float noteDuration)
        {
            int perNote = (int)(SampleRate * noteDuration);
            var data = new float[perNote * frequencies.Length];
            float phase = 0f;

            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    phase += 2f * Mathf.PI * frequencies[n] / SampleRate;
                    float wave = Mathf.Sin(phase) >= 0f ? 1f : -1f;
                    float fade = 1f - 0.6f * i / perNote;
                    data[n * perNote + i] = wave * fade * 0.2f;
                }
            }

            return ToClip(name, data);
        }

        static AudioClip ToClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
