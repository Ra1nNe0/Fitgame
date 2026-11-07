using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    public enum RhythmDifficulty { Easy, Normal, Hard }

    /// <summary>Песня ритм-режима: соперник, мир, темп и «зерно», из которого сочиняются музыка и ноты.</summary>
    public class RhythmSongDef
    {
        public string Id;
        public string Title;
        public string Opponent;        // имя соперника
        public string OpponentArt;     // спрайты boss_xxx_0/1
        public string World;           // фон: forest, cave, volcano, castle
        public string Intro;           // реплика соперника перед песней
        public int Bpm;
        public int Root;               // MIDI-нота тоники
        public bool Minor;
        public int[] Chords;           // ступени лада, по такту на аккорд
        public int Seed;
        public int Pairs;              // сколько раз «соперник поёт — игрок повторяет»
        public float Drive;            // плотность ударных 0..1
    }

    /// <summary>Одна стрелка. Время — в секундах от начала песни.</summary>
    public class RhythmNote
    {
        public int Lane;               // RhythmPoseTracker.Left/Down/Up/Right
        public double Time;
        public double Length;          // > 0 — длинная нота (позу нужно держать)
        public bool Player;            // false — ноту «поёт» соперник

        // состояние во время игры
        public RhythmJudge Result;
        public bool Holding;           // длинная нота засчитана, игрок держит позу
        public bool HoldDone;
        public double HoldLostAt = -1;

        public double End { get { return Time + Length; } }
        public bool IsHold { get { return Length > 0.01; } }
    }

    public enum RhythmJudge { None, Perfect, Good, Ok, Miss }

    public class RhythmChart
    {
        public RhythmSongDef Song;
        public RhythmDifficulty Difficulty;
        public readonly List<RhythmNote> Notes = new List<RhythmNote>();
        public double Beat;            // длительность доли, с
        public double Length;          // длительность песни, с
        public int PlayerNotes;
        /// <summary>Фразы: время начала и кто поёт (для подсветки «ТВОЯ ОЧЕРЕДЬ»).</summary>
        public readonly List<double> PhraseStarts = new List<double>();
        public readonly List<bool> PhrasePlayer = new List<bool>();
    }

    /// <summary>
    /// Песни ритм-режима в стиле «Friday Night Funkin'»: соперник показывает фразу стрелок, игрок повторяет её позами.
    /// Ноты и музыка сочиняются из одного «зерна», поэтому мелодия звучит ровно на стрелках:
    /// каждой стрелке соответствует своя ступень аккорда. Голос игрока — отдельная дорожка,
    /// она приглушается при промахе (как в оригинале).
    /// </summary>
    public static class RhythmSongs
    {
        public static readonly RhythmSongDef[] All =
        {
            new RhythmSongDef
            {
                Id = "slime", Title = "СЛИЗЬ-ШАФФЛ", Opponent = "КОРОЛЬ СЛИЗНЕЙ", OpponentArt = "boss_slime", World = "forest",
                Intro = "Буль! Покажи, как ты двигаешься!",
                Bpm = 92, Root = 62, Minor = false, Chords = new[] { 0, 3, 4, 0 }, Seed = 101, Pairs = 7, Drive = 0.4f
            },
            new RhythmSongDef
            {
                Id = "bat", Title = "ЭХО ПЕЩЕРЫ", Opponent = "КОРОЛЕВА МЫШЕЙ", OpponentArt = "boss_bat", World = "cave",
                Intro = "Пи-пи! Повторяй за мной, если успеешь.",
                Bpm = 104, Root = 57, Minor = true, Chords = new[] { 0, 5, 3, 4 }, Seed = 202, Pairs = 8, Drive = 0.55f
            },
            new RhythmSongDef
            {
                Id = "golem", Title = "МАГМА-БИТ", Opponent = "МАГМОВЫЙ ГОЛЕМ", OpponentArt = "boss_golem", World = "volcano",
                Intro = "Горячий ритм! Не остынь на полпути.",
                Bpm = 116, Root = 64, Minor = true, Chords = new[] { 0, 6, 5, 4 }, Seed = 303, Pairs = 8, Drive = 0.75f
            },
            new RhythmSongDef
            {
                Id = "sloth", Title = "ТАНЕЦ ПРОТИВ ЛЕНИ", Opponent = "ЛОРД ЛЕНЬ", OpponentArt = "boss_sloth", World = "castle",
                Intro = "Зачем двигаться? Ну попробуй перетанцевать меня...",
                Bpm = 128, Root = 60, Minor = true, Chords = new[] { 0, 3, 6, 4 }, Seed = 404, Pairs = 9, Drive = 1f
            },
        };

        public static string DifficultyName(RhythmDifficulty d)
        {
            switch (d)
            {
                case RhythmDifficulty.Easy: return "ЛЕГКО";
                case RhythmDifficulty.Normal: return "НОРМА";
                default: return "ХАРД";
            }
        }

        // ------------------------------------------------------------------ ноты

        struct Slot
        {
            public int Beat;      // доля внутри фразы 0..7
            public int Lane;
            public int Length;    // в долях, 0 — короткая
            public bool Chord;    // вместе с противоположной рукой (поза «руки в стороны»)
        }

        public static RhythmChart Build(RhythmSongDef song, RhythmDifficulty difficulty)
        {
            var chart = new RhythmChart { Song = song, Difficulty = difficulty };
            var random = new System.Random(song.Seed * 7 + (int)difficulty);
            chart.Beat = 60.0 / song.Bpm;
            double bar = chart.Beat * 4;

            List<Slot> previous = null;
            for (int p = 0; p < song.Pairs; p++)
            {
                // нечётные фразы — зеркальный повтор предыдущей: так рисунок легче запомнить
                List<Slot> phrase = p % 2 == 1 && previous != null ? Mirror(previous) : Phrase(random, difficulty, p, song.Pairs);
                previous = phrase;

                double opponentStart = (1 + 4 * p) * bar, playerStart = (3 + 4 * p) * bar;
                chart.PhraseStarts.Add(opponentStart); chart.PhrasePlayer.Add(false);
                chart.PhraseStarts.Add(playerStart); chart.PhrasePlayer.Add(true);
                foreach (Slot s in phrase)
                {
                    for (int who = 0; who < 2; who++)
                    {
                        double start = who == 0 ? opponentStart : playerStart;
                        AddNote(chart, s.Lane, start + s.Beat * chart.Beat, s.Length * chart.Beat, who == 1);
                        if (s.Chord) AddNote(chart, Opposite(s.Lane), start + s.Beat * chart.Beat, s.Length * chart.Beat, who == 1);
                    }
                }
            }
            chart.Notes.Sort((a, b) => a.Time.CompareTo(b.Time));
            chart.Length = (1 + 4 * song.Pairs + 2) * bar;
            return chart;
        }

        static void AddNote(RhythmChart chart, int lane, double time, double length, bool player)
        {
            chart.Notes.Add(new RhythmNote { Lane = lane, Time = time, Length = length, Player = player });
            if (player) chart.PlayerNotes++;
        }

        static int Opposite(int lane)
        {
            if (lane == RhythmPoseTracker.Left) return RhythmPoseTracker.Right;
            if (lane == RhythmPoseTracker.Right) return RhythmPoseTracker.Left;
            return lane;
        }

        static List<Slot> Mirror(List<Slot> phrase)
        {
            var result = new List<Slot>();
            foreach (Slot s in phrase)
            {
                Slot m = s;
                m.Lane = Opposite(s.Lane);
                result.Add(m);
            }
            return result;
        }

        /// <summary>
        /// Фраза на 8 долей. Позы тела медленнее пальцев, поэтому стрелки не чаще одной на долю,
        /// присед — не два раза подряд, а сложность растёт к концу песни.
        /// </summary>
        static List<Slot> Phrase(System.Random random, RhythmDifficulty difficulty, int index, int pairs)
        {
            float intensity = pairs > 1 ? index / (float)(pairs - 1) : 1f;
            int[] beats;
            int maxDowns;
            float holdChance, chordChance;
            switch (difficulty)
            {
                case RhythmDifficulty.Easy:
                    beats = new[] { 0, 2, 4, 6 };
                    maxDowns = 1; holdChance = intensity > 0.5f ? 0.15f : 0f; chordChance = 0f;
                    break;
                case RhythmDifficulty.Normal:
                    beats = index < 2 ? new[] { 0, 2, 4, 6 } : (intensity < 0.6f ? new[] { 0, 2, 3, 4, 6 } : new[] { 0, 1, 2, 4, 5, 6 });
                    maxDowns = 2; holdChance = 0.25f; chordChance = 0.1f;
                    break;
                default:
                    beats = index < 1 ? new[] { 0, 1, 2, 4, 5, 6 } : (intensity < 0.5f ? new[] { 0, 1, 2, 3, 4, 5, 6 } : new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                    maxDowns = 2; holdChance = 0.3f; chordChance = 0.2f;
                    break;
            }

            var phrase = new List<Slot>();
            int downs = 0, lastDownBeat = -10, lastLane = -1, lastBeat = -10, repeat = 0;
            // одну и ту же позу на соседних долях нужно успеть опустить и снова принять — это только для ХАРДА
            int sameLaneGap = difficulty == RhythmDifficulty.Hard ? 1 : 2;
            for (int i = 0; i < beats.Length; i++)
            {
                int beat = beats[i];
                int next = i + 1 < beats.Length ? beats[i + 1] : 8;

                int lane;
                int guard = 0;
                do
                {
                    double r = random.NextDouble() * 3.6;   // вес приседа меньше, чем у рук
                    lane = r < 1.0 ? RhythmPoseTracker.Left : r < 2.0 ? RhythmPoseTracker.Right : r < 3.0 ? RhythmPoseTracker.Up : RhythmPoseTracker.Down;
                    guard++;
                }
                while (guard < 20 && ((lane == RhythmPoseTracker.Down && (downs >= maxDowns || beat - lastDownBeat < 2))
                                      || (lane == lastLane && (repeat >= 1 || beat - lastBeat < sameLaneGap))));

                if (lane == RhythmPoseTracker.Down) { downs++; lastDownBeat = beat; }
                repeat = lane == lastLane ? repeat + 1 : 0;
                lastLane = lane;
                lastBeat = beat;

                var slot = new Slot { Beat = beat, Lane = lane };
                if (next - beat >= 2 && random.NextDouble() < holdChance) slot.Length = next - beat - 1;
                if ((lane == RhythmPoseTracker.Left || lane == RhythmPoseTracker.Right) && random.NextDouble() < chordChance)
                    slot.Chord = true;
                phrase.Add(slot);
            }
            return phrase;
        }

        // ------------------------------------------------------------------ музыка

        public class Audio
        {
            public AudioClip Backing;      // аккомпанемент + голос соперника
            public AudioClip Voice;        // голос игрока (приглушается при промахе)
        }

        static readonly Dictionary<string, Audio> cache = new Dictionary<string, Audio>();

        /// <summary>Ступень аккорда для каждой стрелки: влево — низ, вправо — верх.</summary>
        static readonly int[] LaneDegree = { 0, 2, 4, 7 };

        public static Audio Render(RhythmChart chart)
        {
            string key = chart.Song.Id + "/" + chart.Difficulty;
            Audio audio;
            if (cache.TryGetValue(key, out audio)) return audio;

            RhythmSongDef t = chart.Song;
            int rate = Music.Rate;
            var random = new System.Random(t.Seed);
            int[] scale = t.Minor ? Music.MinorScale : Music.MajorScale;
            double step = chart.Beat / 4.0;                 // шестнадцатая
            int bars = (int)Math.Round(chart.Length / (chart.Beat * 4));
            int steps = bars * 16;
            int total = (int)((chart.Length + 1.0) * rate);
            var backing = new float[total];
            var voice = new float[total];

            for (int s = 0; s < steps; s++)
            {
                int barIndex = s / 16, inBar = s % 16;
                int offset = (int)(s * step * rate);
                int stepSamples = (int)(step * rate);
                int chord = t.Chords[barIndex % t.Chords.Length];
                bool countIn = barIndex == 0, outro = barIndex >= bars - 2;

                if (countIn)
                {
                    // «раз, два, три, четыре» палочками
                    if (inBar % 4 == 0) Music.AddNoise(backing, offset, (int)(0.04f * rate), inBar == 0 ? 0.22f : 0.16f, random);
                    continue;
                }
                if (outro)
                {
                    // финальный аккорд и тишина
                    if (barIndex == bars - 2 && inBar == 0)
                    {
                        int len = (int)(chart.Beat * 4 * rate);
                        AddChord(backing, offset, len, t, scale, chord, 0.05f);
                        Music.AddKick(backing, offset);
                    }
                    continue;
                }

                // бас: восьмые, тоника и квинта
                if (s % 2 == 0)
                {
                    int bassDegree = chord + (inBar == 6 || inBar == 14 ? 4 : 0);
                    Music.AddNote(backing, offset, stepSamples * 2, Music.Freq(t.Root - 24 + Music.Note(scale, bassDegree)), 0.2f, Music.Wave.Triangle);
                }
                // тихое арпеджио, чтобы было слышно гармонию
                if (s % 2 == 0)
                {
                    int arp = chord + new[] { 0, 2, 4, 2 }[(s / 2) % 4];
                    Music.AddNote(backing, offset, stepSamples, Music.Freq(t.Root + Music.Note(scale, arp)), 0.03f, Music.Wave.Pulse25);
                }
                // ударные: бочка на сильные доли, малый на 2 и 4, хэт по восьмым
                if (inBar % 8 == 0 || (t.Drive > 0.7f && inBar == 10)) Music.AddKick(backing, offset);
                if (inBar == 4 || inBar == 12) Music.AddNoise(backing, offset, (int)(0.10f * rate), 0.15f, random);
                if (s % 2 == 0 && t.Drive > 0.3f) Music.AddNoise(backing, offset, (int)(0.02f * rate), 0.045f, random);
                if (t.Drive > 0.9f && s % 2 == 1 && random.NextDouble() < 0.4) Music.AddNoise(backing, offset, (int)(0.015f * rate), 0.03f, random);
            }

            // мелодия на стрелках: у соперника — «квакающий» пульс, у игрока — квадрат
            foreach (RhythmNote n in chart.Notes)
            {
                int barIndex = (int)(n.Time / (chart.Beat * 4) + 1e-6);
                int chord = t.Chords[barIndex % t.Chords.Length];
                float freq = Music.Freq(t.Root + 12 + Music.Note(scale, chord + LaneDegree[n.Lane]));
                double seconds = n.IsHold ? n.Length + chart.Beat * 0.5 : chart.Beat * 0.6;
                int offset = (int)(n.Time * rate), length = (int)(seconds * rate);
                if (n.Player) Music.AddNote(voice, offset, length, freq, 0.11f, Music.Wave.Square);
                else Music.AddNote(backing, offset, length, freq, 0.1f, Music.Wave.Pulse25);
            }

            for (int i = 0; i < total; i++)
            {
                backing[i] = Mathf.Clamp(backing[i], -1f, 1f);
                voice[i] = Mathf.Clamp(voice[i], -1f, 1f);
            }
            audio = new Audio
            {
                Backing = AudioClip.Create("rhythm_" + key, total, 1, rate, false),
                Voice = AudioClip.Create("rhythm_voice_" + key, total, 1, rate, false)
            };
            audio.Backing.SetData(backing, 0);
            audio.Voice.SetData(voice, 0);
            cache[key] = audio;
            return audio;
        }

        static void AddChord(float[] data, int offset, int length, RhythmSongDef t, int[] scale, int chord, float amp)
        {
            foreach (int d in new[] { 0, 2, 4 })
                Music.AddNote(data, offset, length, Music.Freq(t.Root + Music.Note(scale, chord + d)), amp, Music.Wave.Pulse25);
        }
    }
}
