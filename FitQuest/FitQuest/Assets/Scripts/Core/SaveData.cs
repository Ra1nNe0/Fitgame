using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FitQuest
{
    [Serializable]
    public class LevelRecord
    {
        public bool completed;
        public int stars;
        public int bestScore;
    }

    /// <summary>Лучший результат песни ритм-режима на одной сложности.</summary>
    [Serializable]
    public class RhythmRecord
    {
        public string song;
        public int difficulty;
        public bool cleared;
        public int bestScore;
        public float bestAccuracy;
        public string grade = "";
        public bool fullCombo;
    }

    [Serializable]
    public class PlayerStats
    {
        public int jumps, squats, punches, handsUp;   // засчитанные движения
        public int weakMoves;                         // попытки, которые не засчитаны
        public float calories;
        public float playSeconds;
        public int runs, wins, losses;
        public int bestCombo;
        public int obstaclesPassed, obstaclesMissed;
        public int coinsEarned;
        public int perfectLevels;
        public int cameraWins;
        public int marathonBest;                      // лучший результат марафона (препятствий)
        public int aiTests;
        public int armsSide;                          // руки в стороны (ритм-режим)
        public int rhythmPlays, rhythmClears;         // ритм-баттлы: сыграно и пройдено
        public int rhythmNotes;                       // попадания по стрелкам
        public int rhythmFullCombos;
        public int rhythmBestCombo;

        public int TotalMoves { get { return jumps + squats + punches + handsUp; } }
    }

    [Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
        public string mode;
        public string input;
        public string date;
    }

    [Serializable]
    public class PurchaseRecord
    {
        public string item;
        public string price;
        public string date;
    }

    [Serializable]
    public class GameSettings
    {
        public float music = 0.5f;
        public float sfx = 0.8f;
        public int inputMode = 2;        // 0 — клавиатура, 1 — камера, 2 — камера и клавиатура
        public int strictness = 1;       // 0 — мягко, 1 — обычно, 2 — строго
        public bool assist = true;       // запас времени и сердце при последней жизни
        public bool aiPanel;             // показывать AI-панель во время игры
        public bool autoCamera = true;   // запускать сервер камеры вместе с игрой
        public int cameraIndex;
        public int rhythmCameraDelayMs = 120;   // задержка камеры в ритм-режиме (захват + нейросеть)
        public int rhythmDifficulty = 1;        // последняя выбранная сложность

        public bool UseKeyboard { get { return inputMode != 1; } }
        public bool UseCamera { get { return inputMode != 0; } }

        public float StrictnessValue
        {
            get { return strictness == 0 ? 0.8f : (strictness == 2 ? 1.25f : 1f); }
        }
    }

    /// <summary>Всё, что игра помнит об игроке. Хранится в файле profile.json.</summary>
    [Serializable]
    public class Profile
    {
        public int version = 2;
        public string playerName = "ИГРОК";
        public int coins;
        public bool premium;
        public string skin = "king";
        public List<string> ownedSkins = new List<string> { "king", "knight" };
        public bool oldSkinsRefunded;      // монеты за старые образы (до Короля и Рыцаря) возвращены
        public int shields;
        public int magnets;
        public bool tutorialDone;
        public List<string> seenScenes = new List<string>();   // показанные катсцены сюжета
        public List<LevelRecord> levels = new List<LevelRecord>();
        public PlayerStats stats = new PlayerStats();
        public List<string> achievements = new List<string>();
        public List<ScoreEntry> leaderboard = new List<ScoreEntry>();
        public List<PurchaseRecord> purchases = new List<PurchaseRecord>();
        public int demoRevenue;          // сумма демо-покупок, руб.
        public int adsWatched;
        public string adDay = "";
        public int adsToday;
        public GameSettings settings = new GameSettings();
        public List<RhythmRecord> rhythm = new List<RhythmRecord>();

        public RhythmRecord Rhythm(string song, int difficulty)
        {
            foreach (RhythmRecord r in rhythm)
                if (r.song == song && r.difficulty == difficulty) return r;
            var record = new RhythmRecord { song = song, difficulty = difficulty };
            rhythm.Add(record);
            return record;
        }

        /// <summary>Песня пройдена хотя бы на одной сложности.</summary>
        public bool RhythmCleared(string song)
        {
            foreach (RhythmRecord r in rhythm)
                if (r.song == song && r.cleared) return true;
            return false;
        }

        public LevelRecord Level(int index)
        {
            while (levels.Count <= index) levels.Add(new LevelRecord());
            return levels[index];
        }

        public bool IsUnlocked(int index)
        {
            return index == 0 || Level(index - 1).completed;
        }

        /// <summary>Первый непройденный уровень (с него предлагаем продолжить).</summary>
        public int NextLevel
        {
            get
            {
                for (int i = 0; i < Levels.All.Length; i++)
                    if (!Level(i).completed) return i;
                return Levels.All.Length - 1;
            }
        }

        public int TotalStars
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < Levels.All.Length; i++) sum += Level(i).stars;
                return sum;
            }
        }
    }

    /// <summary>
    /// Хранилище данных игрока (слой Data/Storage в архитектуре).
    /// Профиль сохраняется в JSON-файл в папке Application.persistentDataPath,
    /// там же лежат журналы забегов и отчёты тестирования AI.
    /// </summary>
    public static class SaveData
    {
        static Profile profile;

        public static string Folder { get { return Application.persistentDataPath; } }
        static string FilePath { get { return Path.Combine(Folder, "profile.json"); } }

        public static Profile Current
        {
            get
            {
                if (profile == null) Load();
                return profile;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    profile = JsonUtility.FromJson<Profile>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("FitQuest: не удалось прочитать профиль, создаю новый. " + e.Message);
                profile = null;
            }

            if (profile == null) profile = new Profile();
            Repair(profile);
        }

        static void Repair(Profile p)
        {
            if (p.ownedSkins == null) p.ownedSkins = new List<string>();
            // Старые образы убраны из игры: остались только Король и Рыцарь (бесплатно для всех).
            // Монеты за купленные раньше образы возвращаются один раз.
            if (!p.oldSkinsRefunded)
            {
                foreach (string old in new[] { "forest", "neon", "ninja" })
                    if (p.ownedSkins.Contains(old)) p.coins += old == "forest" ? 150 : (old == "neon" ? 300 : 500);
                p.oldSkinsRefunded = true;
            }
            p.ownedSkins.RemoveAll(s => s != "king" && s != "knight");
            foreach (string free in new[] { "king", "knight" })
                if (!p.ownedSkins.Contains(free)) p.ownedSkins.Add(free);
            if (p.levels == null) p.levels = new List<LevelRecord>();
            if (p.stats == null) p.stats = new PlayerStats();
            if (p.achievements == null) p.achievements = new List<string>();
            if (p.leaderboard == null) p.leaderboard = new List<ScoreEntry>();
            if (p.purchases == null) p.purchases = new List<PurchaseRecord>();
            if (p.seenScenes == null) p.seenScenes = new List<string>();
            if (p.settings == null) p.settings = new GameSettings();
            if (string.IsNullOrEmpty(p.playerName)) p.playerName = "ИГРОК";
            if (string.IsNullOrEmpty(p.skin) || !p.ownedSkins.Contains(p.skin)) p.skin = "king";
            if (p.adDay == null) p.adDay = "";
            if (p.rhythm == null) p.rhythm = new List<RhythmRecord>();
            if (p.settings.rhythmCameraDelayMs < 0 || p.settings.rhythmCameraDelayMs > 400) p.settings.rhythmCameraDelayMs = 120;
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("FitQuest: не удалось сохранить профиль. " + e.Message);
            }
        }

        /// <summary>Стереть прогресс. Настройки и имя остаются.</summary>
        public static void ResetProgress()
        {
            GameSettings settings = Current.settings;
            string name = Current.playerName;
            profile = new Profile { settings = settings, playerName = name };
            Save();
        }

        /// <summary>Добавить результат в таблицу рекордов. Вернёт место (с 1) или 0, если не попал в топ.</summary>
        public static int AddScore(int score, string mode, string input)
        {
            if (score <= 0) return 0;
            List<ScoreEntry> board = Current.leaderboard;
            var entry = new ScoreEntry
            {
                name = Current.playerName,
                score = score,
                mode = mode,
                input = input,
                date = DateTime.Now.ToString("dd.MM.yyyy")
            };
            board.Add(entry);
            board.Sort((a, b) => b.score.CompareTo(a.score));
            while (board.Count > 10) board.RemoveAt(board.Count - 1);
            int place = board.IndexOf(entry) + 1;
            Save();
            return place;
        }
    }
}
