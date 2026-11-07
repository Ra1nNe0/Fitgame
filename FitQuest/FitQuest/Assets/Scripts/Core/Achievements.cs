using System;
using System.Collections.Generic;

namespace FitQuest
{
    public class AchievementDef
    {
        public string Id;
        public string Title;
        public string Description;
        public string Icon;
        public Func<Profile, bool> Condition;
    }

    /// <summary>Достижения. Проверяются после каждого забега, ритм-баттла и покупки; за каждое — 25 монет.</summary>
    public static class Achievements
    {
        public const int Reward = 25;

        public static event Action<AchievementDef> Unlocked;

        public static readonly AchievementDef[] All =
        {
            Def("first_step", "ПЕРВЫЙ ШАГ", "Пройдите первый уровень", "medal", p => p.Level(0).completed),
            Def("slime_king", "ГРОЗА СЛИЗНЕЙ", "Победите Короля слизней", "trophy", p => p.Level(2).completed),
            Def("bat_queen", "ОХОТНИК НА МЫШЕЙ", "Победите Королеву мышей", "trophy", p => p.Level(5).completed),
            Def("golem", "УКРОТИТЕЛЬ МАГМЫ", "Победите Магмового голема", "trophy", p => p.Level(8).completed),
            Def("champion", "КОНЕЦ ЛЕНИ", "Победите Лорда Лень", "crown", p => p.Level(11).completed),
            Def("combo10", "СЕРИЯ", "Сделайте комбо из 10 движений", "flame", p => p.stats.bestCombo >= 10),
            Def("combo25", "НЕУДЕРЖИМЫЙ", "Сделайте комбо из 25 движений", "flame", p => p.stats.bestCombo >= 25),
            Def("jumps100", "КУЗНЕЧИК", "Сделайте 100 прыжков", "medal", p => p.stats.jumps >= 100),
            Def("squats100", "СТАЛЬНЫЕ НОГИ", "Сделайте 100 приседаний", "medal", p => p.stats.squats >= 100),
            Def("punches100", "БОКСЁР", "Сделайте 100 ударов", "medal", p => p.stats.punches >= 100),
            Def("perfect", "ИДЕАЛЬНО", "Пройдите уровень на 3 звезды", "star", p => p.stats.perfectLevels >= 1),
            Def("kcal50", "ЖИРОСЖИГАТЕЛЬ", "Сожгите 50 ккал", "flame", p => p.stats.calories >= 50f),
            Def("camera_win", "ЖИВОЕ ДВИЖЕНИЕ", "Пройдите уровень, управляя камерой", "camera", p => p.stats.cameraWins >= 1),
            Def("stylist", "МОДНИК", "Купите новый образ героя", "cart", p => p.ownedSkins.Count >= 2),
            Def("marathon50", "МАРАФОНЕЦ", "Пройдите 50 препятствий в Марафоне", "map", p => p.stats.marathonBest >= 50),
            Def("rich", "КОПИЛКА", "Заработайте 1000 монет", "magnet", p => p.stats.coinsEarned >= 1000),
            Def("rhythm_first", "В РИТМЕ", "Пройдите песню в ритм-баттле", "medal", p => p.stats.rhythmClears >= 1),
            Def("rhythm_fc", "ЧИСТЫЙ РИТМ", "Пройдите песню без промахов", "star", p => p.stats.rhythmFullCombos >= 1),
            Def("rhythm_boss", "ПЕРЕТАНЦЕВАЛ ЛЕНЬ", "Победите Лорда Лень в ритм-баттле", "crown", p => p.RhythmCleared("sloth")),
            Def("rhythm_s", "ЗВЕЗДА ТАНЦПОЛА", "Получите оценку S на сложности ХАРД", "trophy", p => HasHardS(p)),
        };

        static bool HasHardS(Profile p)
        {
            foreach (RhythmRecord r in p.rhythm)
                if (r.difficulty == (int)RhythmDifficulty.Hard && r.grade == "S") return true;
            return false;
        }

        static AchievementDef Def(string id, string title, string description, string icon, Func<Profile, bool> condition)
        {
            return new AchievementDef { Id = id, Title = title, Description = description, Icon = icon, Condition = condition };
        }

        public static bool Has(string id)
        {
            return SaveData.Current.achievements.Contains(id);
        }

        /// <summary>Проверить все условия; новые достижения сохраняются и возвращаются списком.</summary>
        public static List<AchievementDef> Evaluate()
        {
            Profile p = SaveData.Current;
            var fresh = new List<AchievementDef>();
            foreach (AchievementDef a in All)
            {
                if (p.achievements.Contains(a.Id) || !a.Condition(p)) continue;
                p.achievements.Add(a.Id);
                p.coins += Reward;
                fresh.Add(a);
            }

            if (fresh.Count > 0)
            {
                SaveData.Save();
                foreach (AchievementDef a in fresh)
                    if (Unlocked != null) Unlocked(a);
            }
            return fresh;
        }
    }
}
