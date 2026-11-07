using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>Что запустить: уровень кампании или марафон, и какие бусты взять.</summary>
    public class RunRequest
    {
        public int Level;
        public bool Marathon;
        public bool UseShield;
        public bool UseMagnet;
    }

    /// <summary>Итоги забега — для экрана результатов, статистики и журналов.</summary>
    public class RunResult
    {
        public bool Won;
        public bool Marathon;
        public int Level;
        public string Title;
        public int Score;
        public int Coins;
        public int CoinMultiplier = 1;
        public int Stars;
        public int Accuracy;
        public int BestCombo;
        public int Hits;
        public int Misses;
        public float Calories;
        public float Duration;
        public float Tempo;
        public int TempoChanges;
        public int Stage;
        public readonly int[] Needed = new int[5];     // сколько раз требовалось движение
        public readonly int[] Passed = new int[5];     // сколько раз выполнено вовремя
        public readonly float[] Quality = new float[5];
        public readonly int[] Done = new int[5];       // сколько движений сделано всего
        public int Weak;
        public string Input;
        public int Place;
        public bool NewBest;
        public string LogPath;
        public List<AchievementDef> NewAchievements = new List<AchievementDef>();
    }

    /// <summary>
    /// Игровая логика забега: расписание препятствий, проверка движений, очки, жизни, комбо,
    /// боссы, адаптивная сложность, бусты, продолжение после проигрыша, итоги и сохранение.
    /// Экраны (меню, пауза, результаты) живут в App; здесь — только сам забег.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        enum State { Idle, Intro, Playing, Clearing, Failed }

        public const int StartLives = 3;
        const int MaxLives = 5;
        /// <summary>Сколько секунд игра «прощает» опоздание с движением (у камеры есть задержка).</summary>
        const float GraceTime = 0.25f;
        const float GraceTimeNoAssist = 0.12f;
        /// <summary>Меньше этой паузы между препятствиями не бывает: человек не успеет.</summary>
        const float MinGap = 0.95f;

        public const float GroundY = -3f;
        public const float PlayerX = -6f;
        const float RunOriginX = 12f;
        const float BossX = 6.5f;

        static readonly float[] Kcal = { 0f, 0.45f, 0.4f, 0.15f, 0.2f };   // оценка ккал за одно движение

        class Pending
        {
            public Obstacle Obstacle;
            public float TimeLeft;
        }

        public event Action<RunResult> Finished;   // забег окончен (победа или отказ продолжать)
        public event Action Failed;                 // жизни кончились — App спросит про продолжение

        readonly DifficultyDirector director = new DifficultyDirector();
        readonly List<Pending> pending = new List<Pending>();

        PlayerController player;
        ObstacleSpawner spawner;
        Parallax parallax;
        Hud hud;
        MotionHub hub;
        Transform cam;
        Vector3 camHome;
        Boss boss;

        State state = State.Idle;
        RunRequest request;
        LevelDef level;
        RunResult result;
        RunLog log;
        float stateTimer;
        float shake;
        float runTime;

        int stage;
        int lives;
        int combo;
        int planned;
        int resolvedInStage;
        bool heartGiven;
        bool treasureDone;      // «Дождь сокровищ» уже был в этом этапе
        float fever;            // сколько ещё длится режим «Фивер» (очки x2)
        const int FeverCombo = 8;
        const float FeverTime = 7f;
        bool revived;
        bool magnet;

        public bool Running { get { return state != State.Idle; } }
        public bool CanRevive { get { return !revived; } }
        public DifficultyDirector Director { get { return director; } }
        public LevelDef Level { get { return level; } }
        public int Lives { get { return lives; } }

        public void Init(PlayerController player, ObstacleSpawner spawner, Parallax parallax, Hud hud,
                         Transform cam, MotionHub hub)
        {
            this.player = player;
            this.spawner = spawner;
            this.parallax = parallax;
            this.hud = hud;
            this.cam = cam;
            this.hub = hub;
            camHome = cam.position;

            hub.MotionDetected += OnMotion;
            spawner.ReachedPlayer += OnObstacleReachedPlayer;
            spawner.Spawned += OnObstacleSpawned;
            spawner.OrbHitBoss += OnOrbHitBoss;
        }

        // ------------------------------------------------------------------ запуск и окончание

        public void Begin(RunRequest req)
        {
            request = req;
            Profile p = SaveData.Current;
            stage = 0;
            level = req.Marathon ? Levels.MarathonStage(0) : Levels.All[req.Level];

            result = new RunResult
            {
                Marathon = req.Marathon,
                Level = req.Level,
                Title = req.Marathon ? "МАРАФОН" : "УРОВЕНЬ " + (req.Level + 1) + ": " + level.Name.ToUpper()
            };
            log = new RunLog(result.Title);

            lives = StartLives;
            combo = 0;
            planned = 0;
            resolvedInStage = 0;
            heartGiven = false;
            revived = false;
            EndFever();
            runTime = 0f;
            director.Reset();
            pending.Clear();
            DestroyBoss();

            // бусты
            player.SetSkin(p.skin);
            player.ResetState();
            bool shield = req.UseShield && p.shields > 0;
            magnet = req.UseMagnet && p.magnets > 0;
            if (shield) p.shields--;
            if (magnet) p.magnets--;
            player.SetShield(shield);
            SaveData.Save();

            StartStage(true);
        }

        void StartStage(bool first)
        {
            parallax.SetWorld(Levels.Worlds[level.World].Art);
            Music.Play(level.Music);
            planned = 0;
            resolvedInStage = 0;
            treasureDone = false;

            if (level.IsBoss)
            {
                boss = Boss.Create(level.BossArt, level.BossHp, level.BossHover, new Vector3(BossX, GroundY, 0f));
                spawner.Begin(1.5f, BossX - 1.5f, BossX);
                player.SetMode(PlayerController.Mode.Idle);
            }
            else
            {
                spawner.Begin(first ? 1.5f : 1f, RunOriginX, 1000f);
                player.SetMode(PlayerController.Mode.Run);
            }

            if (first)
            {
                state = State.Intro;
                stateTimer = 3.2f;
                hud.ShowMessage(Levels.Worlds[level.World].Name, level.Name.ToUpper() + "\n\n" + level.Hint);
            }
            else
            {
                hud.Flash("ЭТАП " + (stage + 1) + "!  БЫСТРЕЕ!", Ui.Gold);
                Sfx.Play("start");
            }
            hud.Show(true);
        }

        /// <summary>Продолжить после проигрыша (за рекламу или с Премиумом).</summary>
        public void Revive()
        {
            if (state != State.Failed) return;
            revived = true;
            lives = 2;
            combo = 0;
            planned = resolvedInStage;      // непройденные препятствия этапа появятся заново
            pending.Clear();
            spawner.Clear();
            if (level.IsBoss) spawner.Begin(1.5f, BossX - 1.5f, BossX);
            else spawner.Begin(1.5f, RunOriginX, 1000f);
            player.ResetState();
            player.SetMode(level.IsBoss ? PlayerController.Mode.Idle : PlayerController.Mode.Run);
            state = State.Intro;
            stateTimer = 2f;
            hud.ShowMessage("ПРОДОЛЖАЕМ!", "ДВЕ ЖИЗНИ. НЕ СДАВАЙСЯ!");
            log.Note("Продолжение после проигрыша");
        }

        /// <summary>Отказаться от продолжения: забег окончен поражением.</summary>
        public void Abandon()
        {
            if (state == State.Idle) return;
            Finish(false);
        }

        /// <summary>Выйти из забега без итогов (из меню паузы).</summary>
        public void Quit()
        {
            if (state == State.Idle) return;
            state = State.Idle;
            EndFever();
            spawner.Clear();
            pending.Clear();
            DestroyBoss();
            hud.Show(false);
            player.ResetState();
            player.SetMode(PlayerController.Mode.Run);
            player.SetShield(false);
        }

        void Finish(bool won)
        {
            state = State.Idle;
            EndFever();
            spawner.Clear();
            pending.Clear();
            hud.Show(false);
            hud.HideMessage();
            player.SetShield(false);

            Profile p = SaveData.Current;
            PlayerStats st = p.stats;
            result.Won = won;
            result.Duration = runTime;
            result.Tempo = director.Multiplier;
            result.TempoChanges = director.Adjustments;
            result.Stage = stage + 1;
            result.Input = hub.LastSource;

            int total = result.Hits + result.Misses;
            result.Accuracy = total > 0 ? Mathf.RoundToInt(100f * result.Hits / total) : 0;
            result.Stars = 0;
            if (won)
            {
                result.Stars = 1;
                if (result.Accuracy >= 70) result.Stars = 2;
                if (result.Accuracy >= 90 && lives >= StartLives && !revived) result.Stars = 3;
            }

            // монеты: Премиум и магнит удваивают заработок
            int multiplier = (p.premium ? 2 : 1) * (magnet ? 2 : 1);
            int bonus = won ? 5 * result.Stars + (level.IsBoss ? 20 : 0) : 0;
            result.CoinMultiplier = multiplier;
            result.Coins = (result.Coins + bonus) * multiplier;
            p.coins += result.Coins;

            st.runs++;
            if (won) st.wins++; else st.losses++;
            st.playSeconds += runTime;
            st.calories += result.Calories;
            st.coinsEarned += result.Coins;
            st.obstaclesPassed += result.Hits;
            st.obstaclesMissed += result.Misses;
            st.bestCombo = Math.Max(st.bestCombo, result.BestCombo);
            if (result.Marathon) st.marathonBest = Math.Max(st.marathonBest, result.Hits);
            if (won && result.Stars == 3) st.perfectLevels++;
            if (won && result.Input == "камера") st.cameraWins++;

            if (!result.Marathon)
            {
                LevelRecord record = p.Level(result.Level);
                result.NewBest = result.Score > record.bestScore;
                if (won)
                {
                    record.completed = true;
                    record.stars = Math.Max(record.stars, result.Stars);
                }
                record.bestScore = Math.Max(record.bestScore, result.Score);
            }

            string mode = result.Marathon ? "МАРАФОН, ЭТАП " + result.Stage : "УР." + (result.Level + 1);
            result.Place = SaveData.AddScore(result.Score, mode, result.Input);
            SaveData.Save();
            result.NewAchievements = Achievements.Evaluate();

            log.Note("Итог: " + (won ? "победа" : "поражение") + ", очки " + result.Score + ", точность " + result.Accuracy +
                     "%, темп " + result.Tempo.ToString("0.00") + ", изменений темпа " + result.TempoChanges);
            log.Save();
            result.LogPath = log.SavedPath;

            DestroyBoss();
            if (Finished != null) Finished(result);
        }

        // ------------------------------------------------------------------ главный цикл

        void Update()
        {
            if (state == State.Idle || hud == null) return;
            float dt = Time.deltaTime;

            switch (state)
            {
                case State.Intro:
                    if (!level.IsBoss) parallax.Scroll(level.Speed * dt);
                    stateTimer -= dt;
                    int count = Mathf.CeilToInt(stateTimer);
                    if (stateTimer < 2.2f) hud.ShowMessage(count > 0 ? count.ToString() : "ВПЕРЁД!", "");
                    if (stateTimer <= 0f)
                    {
                        state = State.Playing;
                        hud.HideMessage();
                    }
                    break;

                case State.Playing:
                    runTime += dt;
                    player.WorldSpeed = level.IsBoss ? 0f : level.Speed * director.Multiplier;
                    if (fever > 0f)
                    {
                        fever -= dt;
                        if (fever <= 0f) { EndFever(); hud.Flash("ФИВЕР ЗАКОНЧИЛСЯ", Color.white); }
                    }
                    TickPlaying(dt);
                    break;

                case State.Clearing:
                    if (!level.IsBoss)
                    {
                        float speed = level.Speed * director.Multiplier;
                        spawner.Tick(dt, speed);
                        parallax.Scroll(speed * dt);
                    }
                    stateTimer -= dt;
                    if (stateTimer <= 0f) Finish(true);
                    break;
            }

            UpdateShake(dt);
            RefreshHud();
        }

        void TickPlaying(float dt)
        {
            float speed = level.Speed * director.Multiplier;

            PlanAhead();
            spawner.Tick(dt, speed);
            if (!level.IsBoss) parallax.Scroll(speed * dt);

            // Препятствия, по которым игрок опоздал: даём короткий шанс успеть
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Pending p = pending[i];
                if (player.CurrentAction == p.Obstacle.Required)
                {
                    pending.RemoveAt(i);
                    Resolve(p.Obstacle, true);
                }
                else
                {
                    p.TimeLeft -= dt;
                    if (p.TimeLeft > 0f) continue;
                    pending.RemoveAt(i);
                    Resolve(p.Obstacle, false);
                }
                if (state != State.Playing) return;
            }

            bool stageDone = !level.IsBoss && planned >= level.Target && spawner.Unresolved == 0 && pending.Count == 0;
            if (!stageDone) return;

            if (request.Marathon)
            {
                stage++;
                level = Levels.MarathonStage(stage);
                StartStage(false);
            }
            else
            {
                BeginClearing(1f);
            }
        }

        // ------------------------------------------------------------------ расписание препятствий

        void PlanAhead()
        {
            for (int guard = 0; guard < 8 && spawner.PlannedAhead < 4f; guard++)
            {
                float gap = Mathf.Max(MinGap + 0.25f, level.Gap / director.Multiplier);

                if (level.IsBoss)
                {
                    if (boss == null || !boss.Alive) return;
                    // Босс: несколько снарядов, от которых надо увернуться, затем шар, который можно отбить
                    int dodges = UnityEngine.Random.Range(1, level.BossDodges + 1);
                    for (int i = 0; i < dodges; i++)
                        spawner.Plan(level.Kinds[UnityEngine.Random.Range(0, level.Kinds.Length)], gap);
                    spawner.Plan(ObstacleKind.BossOrb, gap);
                    PlanHeartIfNeeded(gap);
                    continue;
                }

                if (planned >= level.Target) return;
                PlanHeartIfNeeded(gap);

                // «Дождь сокровищ»: в середине уровня (кроме самого первого) — серия бонусов:
                // монеты наверху (руки вверх) чередуются с самоцветами у земли (присед). Ошибки не наказываются.
                if (!treasureDone && planned >= level.Target / 2 && (request.Marathon || request.Level > 0))
                {
                    treasureDone = true;
                    hud.Flash("СКОРО: ДОЖДЬ СОКРОВИЩ!", Ui.Gold);
                    spawner.Plan(ObstacleKind.Coin, gap);
                    for (int i = 0; i < 5; i++)
                        spawner.Plan(i % 2 == 0 ? ObstacleKind.Gem : ObstacleKind.Coin, Mathf.Max(1.3f, gap * 0.6f));
                    spawner.Plan(Pick(null), gap);
                    planned++;
                    continue;
                }

                if (level.CoinChance > 0f && UnityEngine.Random.value < level.CoinChance)
                {
                    // Цепочка монет: одно движение «руки вверх» собирает все три
                    spawner.Plan(ObstacleKind.Coin, gap * 0.6f);
                    spawner.Plan(ObstacleKind.Coin, 0.2f);
                    spawner.Plan(ObstacleKind.Coin, 0.2f);
                }

                ObstacleKind kind = Pick(null);
                spawner.Plan(kind, gap);
                planned++;

                // Связка: второе препятствие почти сразу за первым. Чем лучше играет игрок, тем чаще
                if (planned < level.Target && UnityEngine.Random.value < director.ScaleComboChance(level.ComboChance))
                {
                    spawner.Plan(Pick(kind), Mathf.Max(MinGap, gap * 0.5f));
                    planned++;
                }
            }
        }

        /// <summary>Помощь: если осталась одна жизнь, игра подбрасывает сердце.</summary>
        void PlanHeartIfNeeded(float gap)
        {
            if (lives != 1 || heartGiven || !SaveData.Current.settings.assist) return;
            heartGiven = true;
            spawner.Plan(ObstacleKind.Heart, gap * 0.6f);
        }

        ObstacleKind Pick(ObstacleKind? avoid)
        {
            ObstacleKind kind = level.Kinds[UnityEngine.Random.Range(0, level.Kinds.Length)];
            for (int i = 0; i < 4 && avoid.HasValue && kind == avoid.Value; i++)
                kind = level.Kinds[UnityEngine.Random.Range(0, level.Kinds.Length)];
            return kind;
        }

        // ------------------------------------------------------------------ события

        void OnMotion(MotionEvent motion)
        {
            if (state != State.Playing && state != State.Intro) return;
            if (Time.timeScale <= 0f) return;

            if (!motion.IsUpdate && motion.Quality < PlayerController.MinQuality)
            {
                result.Weak++;
                SaveData.Current.stats.weakMoves++;
                hud.Flash("СЛАБО! " + MotionNames.Ru(motion.Type) + " ПОЛНОСТЬЮ", Ui.Red);
                Sfx.Play("weak", 0.5f);
                return;
            }

            bool started = player.TryPerform(motion);
            if (!started || state != State.Playing) return;

            // статистика движений (фитнес-часть)
            int t = (int)motion.Type;
            result.Done[t]++;
            result.Calories += Kcal[t];
            PlayerStats st = SaveData.Current.stats;
            switch (motion.Type)
            {
                case MotionType.Jump: st.jumps++; Sfx.Play("jump", 0.4f); break;
                case MotionType.Squat: st.squats++; Sfx.Play("squat", 0.4f); break;
                case MotionType.Punch: st.punches++; Sfx.Play("punch", 0.5f); break;
                case MotionType.HandsUp: st.handsUp++; Sfx.Play("hands", 0.4f); break;
            }
        }

        void OnObstacleSpawned(Obstacle obstacle)
        {
            if (boss != null && level.IsBoss && !obstacle.IsBonus) boss.Throw();
        }

        void OnObstacleReachedPlayer(Obstacle obstacle)
        {
            if (state != State.Playing) return;

            if (player.CurrentAction == obstacle.Required) Resolve(obstacle, true);
            else
            {
                float grace = SaveData.Current.settings.assist ? GraceTime : GraceTimeNoAssist;
                pending.Add(new Pending { Obstacle = obstacle, TimeLeft = grace });
            }
        }

        void Resolve(Obstacle obstacle, bool success)
        {
            float quality = player.CurrentQuality;
            Vector3 position = obstacle.transform.position;

            if (obstacle.IsBonus)
            {
                if (!success) return;
                if (obstacle.Kind == ObstacleKind.Heart)
                {
                    lives = Mathf.Min(lives + 1, MaxLives);
                    heartGiven = false;
                    hud.Flash("+1 ЖИЗНЬ", Ui.Red);
                    Sfx.Play("heart");
                }
                else if (obstacle.Kind == ObstacleKind.Gem)
                {
                    result.Coins += 3;
                    result.Score += 30 * (fever > 0f ? 2 : 1);
                    Sfx.Play("coin", 0.5f);
                    Fx.Sparkle(position + Vector3.up * 0.4f, 8);
                    hud.Flash("САМОЦВЕТ +3", Ui.Cyan);
                    spawner.Remove(obstacle);
                    return;
                }
                else
                {
                    result.Coins++;
                    result.Score += 10 * (fever > 0f ? 2 : 1);
                    Sfx.Play("coin", 0.4f);
                }
                Fx.Poof(position + Vector3.up * 3.2f);
                spawner.Remove(obstacle);
                return;
            }

            int r = (int)obstacle.Required;
            result.Needed[r]++;
            resolvedInStage++;

            if (success)
            {
                result.Hits++;
                result.Passed[r]++;
                result.Quality[r] += quality;
                combo++;
                result.BestCombo = Math.Max(result.BestCombo, combo);

                int comboMultiplier = Mathf.Min(1 + combo / 5, 4);
                int points = Mathf.RoundToInt(100f * quality) * comboMultiplier * (fever > 0f ? 2 : 1);
                result.Score += points;
                if (quality >= 0.85f) result.Coins++;
                if (fever > 0f) result.Coins++;
                hud.Flash(RatingOf(quality) + " +" + points, fever > 0f || quality >= 0.85f ? Ui.Gold : Color.white);

                // «Фивер»: каждые FeverCombo успехов подряд — очки x2 на несколько секунд
                if (combo % FeverCombo == 0)
                {
                    fever = FeverTime;
                    player.SetFever(true);
                    hud.Flash("ФИВЕР! ОЧКИ x2", Ui.Gold);
                    Sfx.Play("start", 0.6f);
                    Fx.Sparkle(player.transform.position + Vector3.up, 14, 1.2f);
                }

                switch (obstacle.Kind)
                {
                    case ObstacleKind.Crate:
                        result.Coins += 3;
                        Sfx.Play("break");
                        Fx.Poof(position + Vector3.up);
                        spawner.Remove(obstacle);
                        break;
                    case ObstacleKind.Slime:
                        Sfx.Play("break");
                        Fx.Poof(position + Vector3.up * 0.5f);
                        spawner.Remove(obstacle);
                        break;
                    case ObstacleKind.Mystery:
                        result.Coins += 2;
                        Sfx.Play(obstacle.BreaksOnSuccess ? "break" : "good", 0.5f);
                        Fx.Sparkle(position + Vector3.up, 6);
                        if (obstacle.BreaksOnSuccess)
                        {
                            Fx.Poof(position + Vector3.up * 0.5f);
                            spawner.Remove(obstacle);
                        }
                        break;
                    case ObstacleKind.BossOrb:
                        obstacle.Reflect();
                        Sfx.Play("good");
                        break;
                    default:
                        Sfx.Play("good", 0.35f);
                        break;
                }
            }
            else
            {
                result.Misses++;
                combo = 0;
                if (fever > 0f) EndFever();
                obstacle.MarkFailed();
                shake = 0.3f;
                if (player.Hurt())
                {
                    hud.Flash("ЩИТ СПАС!", Ui.Blue);
                    Sfx.Play("shield");
                }
                else
                {
                    lives--;
                    Sfx.Play("hurt");
                    hud.Flash("МИМО! НУЖНО: " + MotionNames.Ru(obstacle.Required), Ui.Red);
                }
            }

            director.Report(success, quality);
            log.Obstacle(obstacle.Kind.ToString(), obstacle.Required, player.CurrentAction, success ? quality : 0f,
                         success, director.Multiplier, lives, hub.LastSource);

            if (lives <= 0) Fail();
        }

        void OnOrbHitBoss()
        {
            if (boss == null || state != State.Playing) return;

            boss.TakeHit();
            shake = 0.2f;
            result.Score += 500;
            Sfx.Play("bosshit");
            hud.Flash("ПОПАДАНИЕ! +500", Ui.Gold);

            if (!boss.Alive)
            {
                spawner.Clear();
                pending.Clear();
                BeginClearing(1.8f);
            }
        }

        void EndFever()
        {
            fever = 0f;
            if (player != null) player.SetFever(false);
        }

        void Fail()
        {
            EndFever();
            state = State.Failed;
            spawner.Clear();
            pending.Clear();
            player.SetMode(PlayerController.Mode.Idle);
            Sfx.Play("lose");
            if (Failed != null) Failed();
        }

        void BeginClearing(float delay)
        {
            EndFever();
            state = State.Clearing;
            stateTimer = delay;
            player.SetMode(PlayerController.Mode.Victory);
            Sfx.Play("win");
        }

        // ------------------------------------------------------------------ вспомогательное

        void DestroyBoss()
        {
            if (boss == null) return;
            Destroy(boss.gameObject);
            boss = null;
        }

        void UpdateShake(float dt)
        {
            if (shake <= 0f) return;
            shake -= dt;
            cam.position = shake <= 0f
                ? camHome
                : camHome + new Vector3(UnityEngine.Random.Range(-0.12f, 0.12f), UnityEngine.Random.Range(-0.12f, 0.12f), 0f);
        }

        void RefreshHud()
        {
            hud.SetHearts(lives);
            hud.SetStats(result.Score, result.Coins, combo, director.Multiplier);
            hud.SetBoosts(player.HasShield, magnet);

            if (level.IsBoss)
                hud.SetBar(level.BossName, boss != null ? (float)boss.Hp / boss.MaxHp : 0f, Ui.Red);
            else
                hud.SetBar(level.Name.ToUpper(), (float)resolvedInStage / Mathf.Max(1, level.Target), Ui.Green);

            hud.SetDirector(director);
        }

        static string RatingOf(float quality)
        {
            if (quality >= 0.85f) return "ОТЛИЧНО!";
            if (quality >= 0.6f) return "ХОРОШО";
            return "ЗАСЧИТАНО";
        }
    }
}
