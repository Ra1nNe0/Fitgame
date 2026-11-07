using UnityEngine;

namespace FitQuest
{
    /// <summary>Мир: набор фонов и название.</summary>
    public class WorldDef
    {
        public string Name;
        public string Art;      // forest, cave, volcano, castle
    }

    /// <summary>Описание одного уровня. Чтобы изменить игру, достаточно поменять числа здесь.</summary>
    public class LevelDef
    {
        public string Name;
        public int World;
        public string Hint;             // подсказка перед стартом

        public float Speed;             // скорость земли, единиц в секунду
        public float Gap;               // пауза между препятствиями, секунд
        public int Target;              // сколько препятствий в уровне
        public float ComboChance;       // шанс связки из двух препятствий подряд
        public float CoinChance;        // шанс цепочки монет
        public ObstacleKind[] Kinds;    // какие препятствия встречаются (повтор = чаще)

        public bool IsBoss;
        public string Music { get { return IsBoss ? "boss" : Levels.Worlds[World].Art; } }
        public string BossName;
        public string BossArt;
        public int BossHp;              // сколько шаров нужно отбить
        public int BossDodges;          // сколько снарядов максимум перед шаром
        public float BossHover;         // высота парения над землёй
    }

    public static class Levels
    {
        public static readonly WorldDef[] Worlds =
        {
            new WorldDef { Name = "ИЗУМРУДНЫЙ ЛЕС", Art = "forest" },
            new WorldDef { Name = "КРИСТАЛЬНАЯ ПЕЩЕРА", Art = "cave" },
            new WorldDef { Name = "ОГНЕННАЯ ГОРА", Art = "volcano" },
            new WorldDef { Name = "ЗАМОК ЛОРДА ЛЕНИ", Art = "castle" },
        };

        const ObstacleKind Spikes = ObstacleKind.Spikes, Log = ObstacleKind.Log, Bat = ObstacleKind.Bat,
            Fireball = ObstacleKind.Fireball, Slime = ObstacleKind.Slime, Crate = ObstacleKind.Crate,
            Vine = ObstacleKind.Vine, Mystery = ObstacleKind.Mystery, Rock = ObstacleKind.BossRock, Fire = ObstacleKind.BossFire;

        public static readonly LevelDef[] All =
        {
            // ---------------- Мир 1: лес ----------------
            new LevelDef
            {
                Name = "Лесная тропа", World = 0,
                Hint = "ШИПЫ - ПРЫГАЙ\nЛЕТУЧАЯ МЫШЬ - ПРИСЕДАЙ",
                Speed = 5.0f, Gap = 2.4f, Target = 8, ComboChance = 0f, CoinChance = 0f,
                Kinds = new[] { Spikes, Bat }
            },
            new LevelDef
            {
                Name = "Поляна слизней", World = 0,
                Hint = "СЛИЗНИ И ЯЩИКИ - БЕЙ\nМОНЕТЫ - РУКИ ВВЕРХ",
                Speed = 5.5f, Gap = 2.2f, Target = 12, ComboChance = 0.1f, CoinChance = 0.25f,
                Kinds = new[] { Spikes, Bat, Slime, Slime, Crate }
            },
            new LevelDef
            {
                Name = "Король слизней", World = 0, IsBoss = true,
                Hint = "УВОРАЧИВАЙСЯ ОТ КАМНЕЙ\nСИНИЙ ШАР ОТБИВАЙ УДАРОМ",
                Speed = 6.0f, Gap = 2.2f,
                Kinds = new[] { Rock },
                BossName = "КОРОЛЬ СЛИЗНЕЙ", BossArt = "boss_slime", BossHp = 3, BossDodges = 2
            },

            // ---------------- Мир 2: пещера ----------------
            new LevelDef
            {
                Name = "Тёмный спуск", World = 1,
                Hint = "ЯМА С ЛИАНОЙ - РУКИ ВВЕРХ\nБРЕВНО - ПРЫГАЙ",
                Speed = 6.0f, Gap = 2.1f, Target = 12, ComboChance = 0.15f, CoinChance = 0.2f,
                Kinds = new[] { Spikes, Bat, Slime, Vine, Vine, Log }
            },
            new LevelDef
            {
                Name = "Гнездо мышей", World = 1,
                Hint = "МЫШИ ЛЕТЯТ СТАЯМИ\nДЕРЖИ ТЕМП",
                Speed = 6.5f, Gap = 2.0f, Target = 14, ComboChance = 0.3f, CoinChance = 0.2f,
                Kinds = new[] { Bat, Bat, Bat, Log, Spikes, Crate, Vine }
            },
            new LevelDef
            {
                Name = "Королева мышей", World = 1, IsBoss = true,
                Hint = "ОГОНЬ - ПРИСЕДАЙ\nСИНИЙ ШАР ОТБИВАЙ",
                Speed = 6.5f, Gap = 2.0f,
                Kinds = new[] { Rock, Fire, Fire },
                BossName = "КОРОЛЕВА МЫШЕЙ", BossArt = "boss_bat", BossHp = 4, BossDodges = 2, BossHover = 1.2f
            },

            // ---------------- Мир 3: вулкан ----------------
            new LevelDef
            {
                Name = "Лавовые поля", World = 2,
                Hint = "ОГНЕННЫЕ ШАРЫ ОЧЕНЬ БЫСТРЫЕ\nБЛОК ? ПРЕВРАЩАЕТСЯ ВБЛИЗИ - РЕАГИРУЙ",
                Speed = 7.0f, Gap = 2.0f, Target = 14, ComboChance = 0.25f, CoinChance = 0.2f,
                Kinds = new[] { Fireball, Fireball, Spikes, Slime, Log, Vine, Mystery }
            },
            new LevelDef
            {
                Name = "Жаркий марафон", World = 2,
                Hint = "САМЫЙ ДЛИННЫЙ ЗАБЕГ\nБЕРЕГИ СИЛЫ",
                Speed = 7.0f, Gap = 1.9f, Target = 18, ComboChance = 0.35f, CoinChance = 0.25f,
                Kinds = new[] { Fireball, Bat, Spikes, Log, Slime, Crate, Vine, Mystery }
            },
            new LevelDef
            {
                Name = "Магмовый голем", World = 2, IsBoss = true,
                Hint = "ГОЛЕМ БРОСАЕТ ПО ТРИ СНАРЯДА\nЖДИ СИНИЙ ШАР",
                Speed = 7.0f, Gap = 1.9f,
                Kinds = new[] { Rock, Rock, Fire },
                BossName = "МАГМОВЫЙ ГОЛЕМ", BossArt = "boss_golem", BossHp = 5, BossDodges = 3
            },

            // ---------------- Мир 4: замок ----------------
            new LevelDef
            {
                Name = "Крепостная стена", World = 3,
                Hint = "СТРАЖА НЕ ДРЕМЛЕТ\nСВЯЗКИ ИДУТ ЧАЩЕ",
                Speed = 7.5f, Gap = 1.9f, Target = 18, ComboChance = 0.4f, CoinChance = 0.25f,
                Kinds = new[] { Spikes, Bat, Fireball, Slime, Log, Crate, Mystery }
            },
            new LevelDef
            {
                Name = "Тронный коридор", World = 3,
                Hint = "ПОСЛЕДНИЙ РЫВОК\nЛОРД ЛЕНЬ УЖЕ БЛИЗКО",
                Speed = 8.0f, Gap = 1.8f, Target = 22, ComboChance = 0.5f, CoinChance = 0.25f,
                Kinds = new[] { Spikes, Bat, Fireball, Slime, Log, Crate, Vine, Mystery, Mystery }
            },
            new LevelDef
            {
                Name = "Лорд Лень", World = 3, IsBoss = true,
                Hint = "ФИНАЛЬНАЯ БИТВА\nНЕ ДАЙ ЛЕНИ ПОБЕДИТЬ",
                Speed = 8.0f, Gap = 1.7f,
                Kinds = new[] { Rock, Fire },
                BossName = "ЛОРД ЛЕНЬ", BossArt = "boss_sloth", BossHp = 6, BossDodges = 3, BossHover = 0.5f
            },
        };

        /// <summary>
        /// Этап режима «Марафон»: бесконечный забег, каждые 15 препятствий — новый мир и выше скорость.
        /// </summary>
        public static LevelDef MarathonStage(int stage)
        {
            int world = stage % Worlds.Length;
            var kinds = new System.Collections.Generic.List<ObstacleKind> { Spikes, Bat, Slime, Crate };
            if (stage >= 1) { kinds.Add(Vine); kinds.Add(Log); }
            if (stage >= 2) { kinds.Add(Fireball); kinds.Add(Bat); kinds.Add(Mystery); }
            if (stage >= 3) { kinds.Add(Log); kinds.Add(Fireball); }
            return new LevelDef
            {
                Name = "Марафон: этап " + (stage + 1),
                World = world,
                Hint = "БЕГИ, ПОКА ЕСТЬ СИЛЫ\nКАЖДЫЙ ЭТАП БЫСТРЕЕ",
                Speed = Mathf.Min(5.5f + 0.5f * stage, 10f),
                Gap = Mathf.Max(1.5f, 2.2f - 0.1f * stage),
                Target = 15,
                ComboChance = Mathf.Min(0.1f + 0.08f * stage, 0.55f),
                CoinChance = 0.25f,
                Kinds = kinds.ToArray()
            };
        }
    }
}
