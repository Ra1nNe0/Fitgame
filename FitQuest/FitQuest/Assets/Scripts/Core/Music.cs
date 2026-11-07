using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Фоновая музыка в стиле 8-битных приставок. Мелодии не лежат в файлах:
    /// они сочиняются при запуске по набору аккордов и «зерну» случайности,
    /// поэтому у каждого мира своя тема, а размер игры не растёт.
    /// Голоса: бас (треугольная волна), арпеджио и мелодия (прямоугольная волна), ударные (шум).
    /// </summary>
    public static class Music
    {
        internal const int Rate = 22050;

        class Track
        {
            public int Bpm;
            public int Root;          // MIDI-нота тоники
            public bool Minor;
            public int[] Chords;      // ступени лада, по 2 такта на аккорд
            public int Seed;
            public float Drive;       // плотность ударных и мелодии 0..1
        }

        static readonly Dictionary<string, Track> Tracks = new Dictionary<string, Track>
        {
            { "menu",    new Track { Bpm = 104, Root = 60, Minor = false, Chords = new[] { 0, 4, 5, 3 }, Seed = 3,  Drive = 0.3f } },
            { "forest",  new Track { Bpm = 126, Root = 67, Minor = false, Chords = new[] { 0, 3, 4, 0 }, Seed = 11, Drive = 0.6f } },
            { "cave",    new Track { Bpm = 112, Root = 57, Minor = true,  Chords = new[] { 0, 5, 3, 4 }, Seed = 23, Drive = 0.5f } },
            { "volcano", new Track { Bpm = 142, Root = 64, Minor = true,  Chords = new[] { 0, 6, 5, 4 }, Seed = 37, Drive = 0.8f } },
            { "castle",  new Track { Bpm = 120, Root = 62, Minor = true,  Chords = new[] { 0, 3, 6, 4 }, Seed = 41, Drive = 0.6f } },
            { "boss",    new Track { Bpm = 152, Root = 60, Minor = true,  Chords = new[] { 0, 0, 5, 4 }, Seed = 57, Drive = 1f } },
        };

        internal static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
        internal static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };

        static AudioSource source;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static string current;
        static float volume = 0.5f;

        public static void Init(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume * 0.6f;
        }

        public static float Volume
        {
            get { return volume; }
            set
            {
                volume = Mathf.Clamp01(value);
                if (source != null) source.volume = volume * 0.6f;
            }
        }

        public static void Play(string track)
        {
            if (source == null || current == track) return;
            current = track;
            AudioClip clip = Get(track);
            if (clip == null) return;
            source.clip = clip;
            source.Play();
        }

        public static void Stop()
        {
            current = null;
            if (source != null) source.Stop();
        }

        static AudioClip Get(string name)
        {
            AudioClip clip;
            if (cache.TryGetValue(name, out clip)) return clip;
            Track track;
            if (!Tracks.TryGetValue(name, out track)) return null;
            clip = Compose(name, track);
            cache[name] = clip;
            return clip;
        }

        internal static float Freq(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        static AudioClip Compose(string name, Track t)
        {
            var random = new System.Random(t.Seed);
            int[] scale = t.Minor ? MinorScale : MajorScale;
            int bars = t.Chords.Length * 2;
            float stepSeconds = 60f / t.Bpm / 4f;              // шестнадцатая
            int stepSamples = (int)(stepSeconds * Rate);
            int steps = bars * 16;
            var data = new float[steps * stepSamples];

            // Мелодия: случайное блуждание по ладу, на сильных долях — звуки аккорда
            var melody = new int[steps];
            int degree = 0;
            for (int s = 0; s < steps; s++)
            {
                melody[s] = -1;
                bool strong = s % 4 == 0;
                if (s % 2 == 1 && random.NextDouble() > t.Drive * 0.5) continue;
                if (!strong && random.NextDouble() < 0.35) continue;
                int chord = t.Chords[(s / 32) % t.Chords.Length];
                if (strong) degree = chord + new[] { 0, 2, 4 }[random.Next(3)];
                else degree += random.Next(-2, 3);
                degree = Math.Max(-2, Math.Min(9, degree));
                melody[s] = degree;
            }
            // последний такт — на тонику, чтобы петля замыкалась
            melody[steps - 16] = 0;
            for (int s = steps - 15; s < steps; s++) melody[s] = -1;

            for (int s = 0; s < steps; s++)
            {
                int chord = t.Chords[(s / 32) % t.Chords.Length];
                int offset = s * stepSamples;
                int beatStep = s % 16;

                // бас: восьмые, тоника и квинта аккорда
                if (s % 2 == 0)
                {
                    int bassDegree = chord + (beatStep == 6 || beatStep == 14 ? 4 : 0);
                    AddNote(data, offset, stepSamples * 2, Freq(t.Root - 24 + Note(scale, bassDegree)), 0.22f, Wave.Triangle);
                }

                // арпеджио: шестнадцатые по звукам аккорда
                int arp = chord + new[] { 0, 2, 4, 7 }[s % 4];
                AddNote(data, offset, stepSamples, Freq(t.Root + Note(scale, arp)), 0.045f, Wave.Pulse25);

                // мелодия
                if (melody[s] >= -2)
                {
                    int length = 1;
                    while (s + length < steps && melody[s + length] == -1 && length < 4) length++;
                    AddNote(data, offset, stepSamples * length, Freq(t.Root + 12 + Note(scale, melody[s])), 0.09f, Wave.Square);
                }

                // ударные
                if (beatStep % 8 == 0 || (t.Drive > 0.7f && beatStep == 10)) AddKick(data, offset);
                if (beatStep == 4 || beatStep == 12) AddNoise(data, offset, (int)(0.10f * Rate), 0.16f, random);
                if (s % 2 == 0 && t.Drive > 0.35f) AddNoise(data, offset, (int)(0.025f * Rate), 0.05f, random);
            }

            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            AudioClip clip = AudioClip.Create("music_" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        internal static int Note(int[] scale, int degree)
        {
            int octave = (int)Math.Floor(degree / 7.0);
            int index = degree - octave * 7;
            return scale[index] + octave * 12;
        }

        internal enum Wave { Square, Pulse25, Triangle }

        internal static void AddNote(float[] data, int offset, int length, float frequency, float amp, Wave wave)
        {
            float phase = 0f;
            float step = frequency / Rate;
            int attack = Rate / 400, release = Math.Min(length / 3, Rate / 30);
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                phase += step;
                phase -= (float)Math.Floor(phase);
                float v;
                switch (wave)
                {
                    case Wave.Pulse25: v = phase < 0.25f ? 1f : -1f; break;
                    case Wave.Triangle: v = 4f * Math.Abs(phase - 0.5f) - 1f; break;
                    default: v = phase < 0.5f ? 1f : -1f; break;
                }
                float env = 1f;
                if (i < attack) env = i / (float)attack;
                else if (i > length - release) env = (length - i) / (float)release;
                data[offset + i] += v * amp * env;
            }
        }

        internal static void AddKick(float[] data, int offset)
        {
            int length = (int)(0.12f * Rate);
            float phase = 0f;
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                float k = i / (float)length;
                phase += Mathf.Lerp(130f, 40f, k) / Rate;
                data[offset + i] += Mathf.Sin(phase * 2f * Mathf.PI) * 0.35f * (1f - k);
            }
        }

        internal static void AddNoise(float[] data, int offset, int length, float amp, System.Random random)
        {
            float last = 0f;
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                float high = n - last;     // простой фильтр высоких частот — звук «звонче»
                last = n;
                data[offset + i] += high * amp * (1f - i / (float)length);
            }
        }
    }
}
