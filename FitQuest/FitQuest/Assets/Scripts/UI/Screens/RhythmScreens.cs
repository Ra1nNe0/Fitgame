using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Ввод ритм-режима: позы с камеры (RhythmPoseTracker) и стрелки/WASD с клавиатуры.
    /// Один на оба экрана, чтобы запомненная стойка игрока не терялась между выбором песни и игрой.
    /// </summary>
    public class RhythmInput
    {
        public readonly RhythmPoseTracker Tracker = new RhythmPoseTracker();
        readonly MotionHub hub;
        readonly List<int> cameraPresses = new List<int>();
        bool listening;

        public RhythmInput(MotionHub hub)
        {
            this.hub = hub;
            hub.Link.FrameReceived += OnFrame;
        }

        void OnFrame(PoseFrame frame, double time)
        {
            GameSettings s = SaveData.Current.settings;
            if (!listening || !s.UseCamera) return;
            Tracker.Strictness = s.StrictnessValue;
            Tracker.Feed(frame, time, cameraPresses);
        }

        /// <summary>Слушать камеру (пока открыт ритм-режим).</summary>
        public bool Listening
        {
            get { return listening; }
            set
            {
                listening = value;
                cameraPresses.Clear();
                if (!value) for (int i = 0; i < RhythmPoseTracker.LaneCount; i++) Tracker.Held[i] = false;
            }
        }

        public bool CameraOn
        {
            get { return SaveData.Current.settings.UseCamera && hub.Link.Connected; }
        }

        /// <summary>Забрать новые «нажатия» с камеры.</summary>
        public void TakeCameraPresses(List<int> target)
        {
            target.AddRange(cameraPresses);
            cameraPresses.Clear();
        }

        public bool KeyPressed(int lane)
        {
            return SaveData.Current.settings.UseKeyboard && !KeyboardMotionSource.Blocked && GameKeys.LanePressed(lane);
        }

        public bool KeyHeld(int lane)
        {
            return SaveData.Current.settings.UseKeyboard && !KeyboardMotionSource.Blocked && GameKeys.LaneHeld(lane);
        }

        public bool CameraHeld(int lane)
        {
            return listening && CameraOn && Tracker.Held[lane];
        }

        public bool AnyHeld(int lane)
        {
            return KeyHeld(lane) || CameraHeld(lane);
        }

        public string Status
        {
            get
            {
                GameSettings s = SaveData.Current.settings;
                if (!s.UseCamera) return "УПРАВЛЕНИЕ: КЛАВИАТУРА (СТРЕЛКИ ИЛИ WASD)";
                if (!hub.Link.Connected) return "КАМЕРА: " + hub.Link.ServerStatus.ToUpper() + (s.UseKeyboard ? "   (МОЖНО ИГРАТЬ СТРЕЛКАМИ)" : "");
                if (!Tracker.HasBody) return "КАМЕРА: " + Tracker.Hint.ToUpper();
                return Tracker.Calibrated ? "КАМЕРА ГОТОВА: ПОПРОБУЙ ПОЗЫ — ЗНАЧКИ ЗАГОРАЮТСЯ" : "КАМЕРА: " + Tracker.Hint.ToUpper();
            }
        }
    }

    /// <summary>Выбор песни и сложности ритм-баттла, проверка поз и задержки камеры.</summary>
    public class RhythmSelectScreen : UiScreen
    {
        RectTransform list;
        Text info, delayText, cameraText;
        readonly Button[] difficultyButtons = new Button[3];
        readonly Image[] poseIcons = new Image[4];
        readonly RectTransform[] poseBars = new RectTransform[4];
        int song;
        float shownTime;

        public RhythmInput Input { get; private set; }

        protected override void Build()
        {
            Input = new RhythmInput(App.Hub);
            Ui.Dim(Root, 0.5f).raycastTarget = false;
            Ui.Label(Root, "РИТМ-БАТТЛ", 48, Ui.Gold, new Vector2(0f, 465f), new Vector2(1600f, 70f));
            Ui.Label(Root, "Соперник показывает стрелки — повтори их позами точно в такт музыке", 20, Ui.Silver,
                     new Vector2(0f, 405f), new Vector2(1700f, 40f));

            list = Ui.Node(Root, "Songs", new Vector2(-470f, 40f), new Vector2(840f, 640f));

            RectTransform panel = Ui.Panel(Root, new Vector2(470f, 40f), new Vector2(860f, 640f)).rectTransform;
            info = Ui.Label(panel, "", 20, Color.white, new Vector2(0f, 220f), new Vector2(800f, 170f));

            for (int i = 0; i < 3; i++)
            {
                int d = i;
                difficultyButtons[i] = Ui.Button(panel, RhythmSongs.DifficultyName((RhythmDifficulty)i), new Vector2(-270f + i * 270f, 90f),
                                                 new Vector2(250f, 80f), Ui.Steel, () => SetDifficulty(d), 20);
            }

            for (int lane = 0; lane < 4; lane++)
            {
                float x = -300f + lane * 200f;
                Image arrow = Ui.Box(panel, new Vector2(x - 40f, -20f), new Vector2(60f, 60f), Color.white);
                arrow.sprite = RhythmArt.Arrow;
                arrow.color = RhythmArt.LaneColors[lane];
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, RhythmArt.LaneRotation[lane]);
                poseIcons[lane] = Ui.Box(panel, new Vector2(x + 35f, -20f), new Vector2(84f, 84f), Ui.Gray);
                poseIcons[lane].sprite = RhythmArt.Pose(lane);
                Ui.Label(panel, RhythmPoseTracker.PoseName(lane), 16, Color.white, new Vector2(x, -90f), new Vector2(200f, 30f));
                Image fill;
                poseBars[lane] = Ui.Bar(panel, new Vector2(x, -125f), new Vector2(140f, 10f), RhythmArt.LaneColors[lane], out fill);
            }

            Ui.Label(panel, "ЗАДЕРЖКА КАМЕРЫ", 20, Color.white, new Vector2(-170f, -205f), new Vector2(420f, 40f), TextAnchor.MiddleLeft);
            Ui.Button(panel, "<", new Vector2(110f, -205f), new Vector2(80f, 64f), Ui.Steel, () => ChangeDelay(-10), 24);
            delayText = Ui.Label(panel, "", 20, Ui.Gold, new Vector2(240f, -205f), new Vector2(180f, 40f));
            Ui.Button(panel, ">", new Vector2(370f, -205f), new Vector2(80f, 64f), Ui.Steel, () => ChangeDelay(10), 24);
            Ui.Label(panel, "Игра сама подскажет точное значение после песни", 16, Ui.Gray, new Vector2(0f, -265f), new Vector2(800f, 30f));

            cameraText = Ui.Label(Root, "", 16, Ui.Amber, new Vector2(0f, -330f), new Vector2(1800f, 30f));
            Ui.Label(Root, "ENTER — СТАРТ   •   С КАМЕРОЙ: ПОДНИМИ РУКИ ВВЕРХ", 16, Ui.Silver, new Vector2(0f, -375f), new Vector2(1800f, 30f));

            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, Back, 24, "icon_back");
            Ui.Button(Root, "СТАРТ!", new Vector2(640f, -470f), new Vector2(480f, 96f), Ui.Green, Start, 32, "icon_play");
        }

        public override void OnShow()
        {
            shownTime = 0f;
            Input.Listening = true;
            App.Player.gameObject.SetActive(false);   // герой забега не мешает списку песен
            if (!Unlocked(song)) song = 0;
            Refresh();
            Music.Play("menu");
            App.EnsureCamera();
        }

        static bool Unlocked(int index)
        {
            return index == 0 || SaveData.Current.RhythmCleared(RhythmSongs.All[index - 1].Id);
        }

        void Refresh()
        {
            Profile p = SaveData.Current;
            int difficulty = Mathf.Clamp(p.settings.rhythmDifficulty, 0, 2);
            Ui.Clear(list);
            for (int i = 0; i < RhythmSongs.All.Length; i++)
            {
                RhythmSongDef def = RhythmSongs.All[i];
                bool open = Unlocked(i);
                int index = i;
                Color color = !open ? Ui.Steel : (i == song ? Ui.Amber : Ui.Slate);
                Button b = Ui.Button(list, (i + 1) + ". " + def.Title, new Vector2(0f, 225f - i * 150f), new Vector2(820f, 135f), color,
                                     () => { song = index; Refresh(); }, 24, open ? null : "icon_lock");
                Ui.SetEnabled(b, open);
                b.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(0f, 22f);
                string sub = open ? "ПРОТИВ: " + def.Opponent + "   " + def.Bpm + " BPM" : "ПРОЙДИ ПРЕДЫДУЩУЮ ПЕСНЮ";
                Ui.Label(b.transform, sub, 16, i == song && open ? Ui.Ink : Ui.Silver, new Vector2(0f, -30f), new Vector2(780f, 30f))
                  .GetComponent<Shadow>().enabled = i != song;
                if (open)
                    for (int d = 0; d < 3; d++)
                    {
                        RhythmRecord r = p.Rhythm(def.Id, d);
                        if (r.grade.Length > 0)
                            Ui.Label(b.transform, r.grade, 24, d == 2 ? Ui.Red : (d == 1 ? Ui.Blue : Ui.Green),
                                     new Vector2(300f + d * 40f, 22f), new Vector2(40f, 40f));
                    }
            }

            RhythmSongDef s = RhythmSongs.All[song];
            RhythmRecord rec = p.Rhythm(s.Id, difficulty);
            string record = rec.bestScore > 0
                ? "РЕКОРД: " + rec.bestScore + "   ТОЧНОСТЬ: " + rec.bestAccuracy.ToString("0.0") + "%   ОЦЕНКА: " + rec.grade + (rec.fullCombo ? "  FC" : "")
                : "РЕКОРДА ПОКА НЕТ";
            info.text = s.Title + "\nСОПЕРНИК: " + s.Opponent + "\n\"" + s.Intro + "\"\n" + record;
            for (int i = 0; i < 3; i++) Ui.SetButtonColor(difficultyButtons[i], i == difficulty ? Ui.Green : Ui.Steel);
            delayText.text = p.settings.rhythmCameraDelayMs + " МС";
            App.Background.SetWorld(s.World);
        }

        void SetDifficulty(int d)
        {
            SaveData.Current.settings.rhythmDifficulty = d;
            SaveData.Save();
            Refresh();
        }

        void ChangeDelay(int delta)
        {
            GameSettings s = SaveData.Current.settings;
            s.rhythmCameraDelayMs = Mathf.Clamp(s.rhythmCameraDelayMs + delta, 0, 400);
            SaveData.Save();
            delayText.text = s.rhythmCameraDelayMs + " МС";
        }

        void Start()
        {
            App.Rhythm.Begin(RhythmSongs.All[song], (RhythmDifficulty)Mathf.Clamp(SaveData.Current.settings.rhythmDifficulty, 0, 2));
            App.Go(App.Rhythm);
        }

        void Back()
        {
            Input.Listening = false;
            App.Player.gameObject.SetActive(true);
            App.Background.SetWorld(Levels.Worlds[Levels.All[SaveData.Current.NextLevel].World].Art);
            App.Go(App.Menu);
        }

        public override bool OnBack()
        {
            Back();
            return true;
        }

        readonly List<int> presses = new List<int>();

        public override void Tick()
        {
            shownTime += Time.unscaledDeltaTime;
            cameraText.text = Input.Status;
            cameraText.color = Input.CameraOn && Input.Tracker.Calibrated ? Ui.Green : Ui.Amber;
            for (int lane = 0; lane < 4; lane++)
            {
                bool held = Input.AnyHeld(lane);
                poseIcons[lane].color = held ? RhythmArt.LaneColors[lane] : Ui.Gray;
                float level = Input.CameraOn ? Input.Tracker.Levels[lane] : (held ? 1f : 0f);
                Ui.SetBar(poseBars[lane], 140f, level);
            }

            presses.Clear();
            Input.TakeCameraPresses(presses);
            if (GameKeys.Confirm) Start();
            else if (shownTime > 1.5f && presses.Contains(RhythmPoseTracker.Up)) Start();
        }
    }

    /// <summary>
    /// Сам ритм-баттл в стиле Friday Night Funkin': слева дорожки соперника, справа — игрока.
    /// Стрелки поднимаются к приёмникам; когда стрелка совпала с приёмником, нужно встать в позу.
    /// Время берётся от звуковой карты (AudioSettings.dspTime), а позы с камеры сдвигаются
    /// на задержку камеры, поэтому оценка «идеально / хорошо» честная и для камеры, и для клавиатуры.
    /// </summary>
    public class RhythmScreen : UiScreen
    {
        const float StrumY = 330f;
        const float NoteSize = 112f;
        const float LaneStep = 124f;
        const float BarWidth = 760f;

        enum State { Idle, Playing, Paused, Done }

        // Окна попадания, с: камера медленнее и шумнее клавиатуры
        static readonly double[] CameraWindows = { 0.10, 0.17, 0.25, 0.30 };
        static readonly double[] KeyWindows = { 0.045, 0.09, 0.135, 0.16 };
        static readonly int[] Points = { 0, 350, 200, 100, 0 };
        static readonly float[] Weights = { 0f, 1f, 0.75f, 0.4f, 0f };
        static readonly float[] LaneKcal = { 0.15f, 0.4f, 0.2f, 0.15f };

        RectTransform stage, notesRoot;
        readonly Image[] receptors = new Image[8];
        readonly float[] receptorFlash = new float[8];
        readonly Image[] poseIcons = new Image[4];
        UiImageAnimator opponent, hero;
        RectTransform opponentRect, heroRect;
        Text judgeText, comboText, scoreText, turnText, countText, speech, titleText;
        Image healthRed, healthIcon;
        SkeletonView skeleton;
        RectTransform pausePanel, resultPanel;
        Text resultTitle, resultGrade, resultBody, resultLatency;
        Button fixDelayButton;

        AudioSource backingSource, voiceSource;
        RhythmSongDef song;
        RhythmDifficulty difficulty;
        RhythmChart chart;
        RhythmSongs.Audio audio;
        RectTransform[] noteViews;
        RectTransform[] tailViews;

        State state;
        double clockStart, dspStart, pausedAt, songTime;
        bool audioStarted;
        float speed, health, judgeTime, heroPoseTime, opponentBump, stageBump, voiceMuteTime, resultTime;
        int lastBeat, lastBar, phrase, score, combo, maxCombo, misses, cameraHits, keyHits;
        readonly int[] judgeCounts = new int[5];
        readonly int[] laneHits = new int[4];
        readonly List<double> cameraErrors = new List<double>();
        readonly List<int> presses = new List<int>();
        float holdPoints, calories;
        int suggestedDelay;
        StringBuilder log;

        RhythmInput Input { get { return App.RhythmSelect.Input; } }

        protected override void Build()
        {
            Ui.Dim(Root, 0.62f).raycastTarget = false;
            stage = Ui.Node(Root, "Stage", Vector2.zero, new Vector2(1920f, 1080f));

            // приёмники: 0..3 — соперник, 4..7 — игрок
            for (int i = 0; i < 8; i++)
            {
                int lane = i % 4;
                receptors[i] = Ui.Box(stage, new Vector2(LaneX(i < 4, lane), StrumY), new Vector2(NoteSize, NoteSize), Ui.Gray);
                receptors[i].sprite = RhythmArt.Receptor;
                receptors[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, RhythmArt.LaneRotation[lane]);
            }
            for (int lane = 0; lane < 4; lane++)
            {
                poseIcons[lane] = Ui.Box(stage, new Vector2(LaneX(true, lane), StrumY + 105f), new Vector2(60f, 60f), Ui.Gray);
                poseIcons[lane].sprite = RhythmArt.Pose(lane);
            }
            Ui.Label(stage, "СОПЕРНИК", 16, Ui.Silver, new Vector2(LaneX(false, 0) + LaneStep * 1.5f, StrumY + 105f), new Vector2(480f, 30f));

            opponent = Ui.Animated(stage, Art.Frames("boss_slime", 2), 2f, new Vector2(-215f, -150f), 5f);
            opponentRect = opponent.GetComponent<RectTransform>();
            hero = Ui.Animated(stage, Art.Hero("king", "idle", 2), 2f, new Vector2(215f, -150f), 5.5f);
            heroRect = hero.GetComponent<RectTransform>();

            notesRoot = Ui.Node(stage, "Notes", Vector2.zero, new Vector2(1920f, 1080f));

            // полоса здоровья: зелёная часть — игрок, красная — соперник
            Image back = Ui.Box(stage, new Vector2(0f, -455f), new Vector2(BarWidth + 12f, 36f), Ui.Ink);
            Ui.Box(back.rectTransform, Vector2.zero, new Vector2(BarWidth, 24f), Ui.Green);
            healthRed = Ui.Box(back.rectTransform, new Vector2(6f, 0f), new Vector2(BarWidth / 2f, 24f), Ui.Red, new Vector2(0f, 0.5f));
            healthIcon = Ui.Box(back.rectTransform, Vector2.zero, new Vector2(64f, 64f), Color.white);
            healthIcon.sprite = Art.Ui("heart_0");
            scoreText = Ui.Label(stage, "", 16, Color.white, new Vector2(0f, -500f), new Vector2(1500f, 30f));

            judgeText = Ui.Label(stage, "", 40, Color.white, new Vector2(215f, 120f), new Vector2(600f, 60f));
            comboText = Ui.Label(stage, "", 24, Ui.Gold, new Vector2(215f, 65f), new Vector2(600f, 40f));
            turnText = Ui.Label(stage, "", 32, Ui.Gold, new Vector2(0f, 205f), new Vector2(760f, 50f));
            countText = Ui.Label(stage, "", 96, Ui.Gold, new Vector2(0f, 20f), new Vector2(900f, 130f));
            titleText = Ui.Label(stage, "", 24, Color.white, new Vector2(0f, 120f), new Vector2(760f, 40f));
            speech = Ui.Label(stage, "", 20, Ui.Sand, new Vector2(-215f, 110f), new Vector2(560f, 90f));

            skeleton = new SkeletonView(stage, new Vector2(0f, 385f), 260f);

            BuildPause();
            BuildResults();

            GameObject host = App.gameObject;
            backingSource = host.AddComponent<AudioSource>();
            voiceSource = host.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { backingSource, voiceSource })
            {
                s.playOnAwake = false;
                s.loop = false;
            }
        }

        void BuildPause()
        {
            pausePanel = Ui.Stretch(Root, "Pause");
            Ui.Dim(pausePanel, 0.6f);
            RectTransform p = Ui.Panel(pausePanel, Vector2.zero, new Vector2(760f, 600f)).rectTransform;
            Ui.Label(p, "ПАУЗА", 48, Ui.Gold, new Vector2(0f, 200f), new Vector2(700f, 70f));
            Ui.Button(p, "ПРОДОЛЖИТЬ", new Vector2(0f, 60f), new Vector2(560f, 100f), Ui.Green, Resume, 24, "icon_play");
            Ui.Button(p, "ЗАНОВО", new Vector2(0f, -60f), new Vector2(560f, 100f), Ui.Amber, Restart, 24);
            Ui.Button(p, "К ПЕСНЯМ", new Vector2(0f, -180f), new Vector2(560f, 100f), Ui.Slate, Exit, 24, "icon_back");
            pausePanel.gameObject.SetActive(false);
        }

        void BuildResults()
        {
            resultPanel = Ui.Stretch(Root, "Results");
            Ui.Dim(resultPanel, 0.7f);
            RectTransform p = Ui.Panel(resultPanel, Vector2.zero, new Vector2(1300f, 900f)).rectTransform;
            resultTitle = Ui.Label(p, "", 48, Ui.Gold, new Vector2(0f, 370f), new Vector2(1200f, 70f));
            resultGrade = Ui.Label(p, "", 128, Ui.Gold, new Vector2(-400f, 110f), new Vector2(300f, 200f));
            resultBody = Ui.Label(p, "", 20, Color.white, new Vector2(150f, 110f), new Vector2(800f, 420f), TextAnchor.MiddleLeft);
            resultLatency = Ui.Label(p, "", 16, Ui.Cyan, new Vector2(0f, -170f), new Vector2(1200f, 60f));
            fixDelayButton = Ui.Button(p, "", new Vector2(0f, -250f), new Vector2(820f, 84f), Ui.Blue, FixDelay, 20, "icon_camera");
            Ui.Button(p, "ЕЩЁ РАЗ", new Vector2(-300f, -360f), new Vector2(460f, 100f), Ui.Green, Restart, 24, "icon_play");
            Ui.Button(p, "К ПЕСНЯМ", new Vector2(300f, -360f), new Vector2(460f, 100f), Ui.Slate, Exit, 24, "icon_back");
            resultPanel.gameObject.SetActive(false);
        }

        static float LaneX(bool player, int lane)
        {
            return player ? 488f + lane * LaneStep : -860f + lane * LaneStep;
        }

        // ------------------------------------------------------------------ старт

        /// <summary>Подготовить песню (вызывается перед App.Go).</summary>
        public void Begin(RhythmSongDef def, RhythmDifficulty level)
        {
            song = def;
            difficulty = level;
            chart = RhythmSongs.Build(def, level);
            audio = RhythmSongs.Render(chart);
            speed = level == RhythmDifficulty.Easy ? 600f : (level == RhythmDifficulty.Normal ? 700f : 800f);

            Ui.Clear(notesRoot);
            noteViews = new RectTransform[chart.Notes.Count];
            tailViews = new RectTransform[chart.Notes.Count];
            for (int i = 0; i < chart.Notes.Count; i++)
            {
                RhythmNote n = chart.Notes[i];
                Color color = RhythmArt.LaneColors[n.Lane];
                if (n.IsHold)
                {
                    Image tail = Ui.Box(notesRoot, Vector2.zero, new Vector2(36f, (float)(n.Length * speed)), new Color(color.r, color.g, color.b, 0.75f));
                    tail.rectTransform.pivot = new Vector2(0.5f, 1f);
                    tailViews[i] = tail.rectTransform;
                    tail.gameObject.SetActive(false);
                }
                Image arrow = Ui.Box(notesRoot, Vector2.zero, new Vector2(NoteSize, NoteSize), color);
                arrow.sprite = RhythmArt.Arrow;
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, RhythmArt.LaneRotation[n.Lane]);
                noteViews[i] = arrow.rectTransform;
                arrow.gameObject.SetActive(false);
            }

            float beatFps = def.Bpm / 60f;   // персонажи качаются в такт: смена кадра на каждую долю
            opponent.Play(Art.Frames(def.OpponentArt, 2), beatFps);
            hero.Play(Art.Hero(SaveData.Current.skin, "idle", 2), beatFps);
            opponentRect.localScale = new Vector3(-1f, 1f, 1f);
            heroRect.localScale = new Vector3(-1f, 1f, 1f);
            titleText.text = def.Title + "  •  " + RhythmSongs.DifficultyName(level);
        }

        public override void OnShow()
        {
            Input.Listening = true;
            Music.Stop();
            App.Background.SetWorld(song.World);
            StartSong();
        }

        public override void OnHide()
        {
            StopAudio();
            state = State.Idle;
        }

        void StartSong()
        {
            foreach (RhythmNote n in chart.Notes)
            {
                n.Result = RhythmJudge.None;
                n.Holding = false;
                n.HoldDone = false;
                n.HoldLostAt = -1;
            }
            score = combo = maxCombo = misses = cameraHits = keyHits = 0;
            Array.Clear(judgeCounts, 0, judgeCounts.Length);
            Array.Clear(laneHits, 0, laneHits.Length);
            cameraErrors.Clear();
            holdPoints = calories = 0f;
            health = 0.5f;
            phrase = -1;
            lastBeat = lastBar = -100;
            judgeText.text = comboText.text = turnText.text = countText.text = "";
            speech.text = song.Opponent + ":\n\"" + song.Intro + "\"";
            pausePanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);

            log = new StringBuilder();
            log.AppendLine("# FitQuest ритм-баттл: " + song.Title + " (" + RhythmSongs.DifficultyName(difficulty) + "); " +
                           DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "; задержка камеры " + SaveData.Current.settings.rhythmCameraDelayMs + " мс");
            log.AppendLine("время ноты;стрелка;оценка;отклонение мс;ввод");

            backingSource.clip = audio.Backing;
            voiceSource.clip = audio.Voice;
            backingSource.volume = voiceSource.volume = Music.Volume;
            StartClock(2.0);
            state = State.Playing;
        }

        void StartClock(double lead)
        {
            // lead — сколько секунд до начала песни (песня начинается со счёта «раз-два-три-четыре»)
            dspStart = AudioSettings.dspTime + lead;
            clockStart = Time.realtimeSinceStartupAsDouble + lead;
            backingSource.Stop();
            voiceSource.Stop();
            backingSource.PlayScheduled(dspStart);
            voiceSource.PlayScheduled(dspStart);
            audioStarted = true;
        }

        void StopAudio()
        {
            if (backingSource != null) backingSource.Stop();
            if (voiceSource != null) voiceSource.Stop();
            audioStarted = false;
        }

        // ------------------------------------------------------------------ пауза и выход

        public override bool OnBack()
        {
            if (state == State.Playing) Pause();
            else if (state == State.Paused) Resume();
            else if (state == State.Done) Exit();
            return true;
        }

        void Pause()
        {
            state = State.Paused;
            pausedAt = Time.realtimeSinceStartupAsDouble;
            backingSource.Pause();
            voiceSource.Pause();
            pausePanel.gameObject.SetActive(true);
            pausePanel.SetAsLastSibling();
        }

        void Resume()
        {
            if (state != State.Paused) return;
            pausePanel.gameObject.SetActive(false);
            double t = pausedAt - clockStart;   // место песни, на котором поставили паузу
            if (t < 0)
            {
                // пауза во время отсчёта: начинаем заново с того же места
                StartClock(-t);
            }
            else
            {
                // продолжаем с той же точки; звук догоняет часы через dspTime
                double gap = Time.realtimeSinceStartupAsDouble - pausedAt;
                clockStart += gap;
                dspStart += gap;
                backingSource.UnPause();
                voiceSource.UnPause();
            }
            state = State.Playing;
        }

        void Restart()
        {
            StopAudio();
            StartSong();
        }

        void Exit()
        {
            StopAudio();
            state = State.Idle;
            App.Go(App.RhythmSelect);
        }

        // ------------------------------------------------------------------ кадр

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            skeleton.SetActive(SaveData.Current.settings.UseCamera);
            if (SaveData.Current.settings.UseCamera) skeleton.Refresh(App.Hub.Link, App.Hub.Camera.Classifier);

            if (state == State.Done)
            {
                resultTime += dt;
                presses.Clear();
                Input.TakeCameraPresses(presses);
                if (GameKeys.Confirm || (resultTime > 2f && presses.Contains(RhythmPoseTracker.Up))) Restart();
                return;
            }
            if (state != State.Playing) return;

            UpdateClock();
            double t = songTime;

            // нажатия: клавиатура — сразу, камера — с поправкой на задержку
            double delay = SaveData.Current.settings.rhythmCameraDelayMs / 1000.0;
            for (int lane = 0; lane < 4; lane++)
                if (Input.KeyPressed(lane)) Press(lane, t, false);
            presses.Clear();
            Input.TakeCameraPresses(presses);
            foreach (int lane in presses) Press(lane, t - delay, true);

            UpdateNotes(t, dt);
            UpdateVisuals(t, dt);

            if (health <= 0f) Finish(false);
            else if (t >= chart.Length) Finish(true);
        }

        void UpdateClock()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            songTime = now - clockStart;
            // звуковая карта — главные часы: если разошлись больше чем на 40 мс, подтягиваемся к ней
            if (audioStarted && songTime > 0.2 && backingSource.isPlaying)
            {
                double dsp = AudioSettings.dspTime - dspStart;
                if (Math.Abs(dsp - songTime) > 0.04)
                {
                    clockStart = now - dsp;
                    songTime = dsp;
                }
            }
        }

        double[] Windows(bool camera)
        {
            return camera ? CameraWindows : KeyWindows;
        }

        float WindowScale
        {
            get
            {
                int s = SaveData.Current.settings.strictness;
                return s == 0 ? 1.2f : (s == 2 ? 0.85f : 1f);
            }
        }

        void Press(int lane, double time, bool camera)
        {
            receptorFlash[4 + lane] = 0.12f;
            double[] w = Windows(camera);
            float k = WindowScale;

            RhythmNote best = null;
            double bestError = double.MaxValue;
            foreach (RhythmNote n in chart.Notes)
            {
                if (!n.Player || n.Lane != lane || n.Result != RhythmJudge.None) continue;
                double error = time - n.Time;
                if (error < -w[3] * k) break;                    // дальше — только более поздние ноты
                if (Math.Abs(error) <= w[3] * k && Math.Abs(error) < Math.Abs(bestError)) { best = n; bestError = error; }
            }

            ShowPose(lane);
            if (best == null) return;   // нажатие мимо нот не наказываем: позу легко поймать случайно

            double a = Math.Abs(bestError);
            RhythmJudge j = a <= w[0] * k ? RhythmJudge.Perfect : (a <= w[1] * k ? RhythmJudge.Good : RhythmJudge.Ok);
            best.Result = j;
            judgeCounts[(int)j]++;
            score += Points[(int)j];
            combo++;
            maxCombo = Math.Max(maxCombo, combo);
            health = Mathf.Clamp01(health + (j == RhythmJudge.Perfect ? 0.04f : (j == RhythmJudge.Good ? 0.03f : 0.01f)));
            laneHits[lane]++;
            calories += LaneKcal[lane];
            if (camera) { cameraHits++; cameraErrors.Add(bestError); } else keyHits++;
            if (best.IsHold) best.Holding = true;
            voiceMuteTime = 0f;
            voiceSource.volume = Music.Volume;
            ShowJudge(j);
            Log(best, j, bestError, camera ? "камера" : "клавиатура");
        }

        void Log(RhythmNote n, RhythmJudge j, double error, string input)
        {
            log.Append(n.Time.ToString("0.000")).Append(';')
               .Append(RhythmPoseTracker.LaneName(n.Lane)).Append(';')
               .Append(JudgeName(j)).Append(';')
               .Append(j == RhythmJudge.Miss ? "" : Math.Round(error * 1000.0).ToString()).Append(';')
               .AppendLine(input);
        }

        void UpdateNotes(double t, float dt)
        {
            float k = WindowScale;
            double missAfter = (SaveData.Current.settings.UseCamera ? CameraWindows[3] : KeyWindows[3]) * k;
            for (int i = 0; i < chart.Notes.Count; i++)
            {
                RhythmNote n = chart.Notes[i];
                if (n.Player)
                {
                    if (n.Result == RhythmJudge.None && t - n.Time > missAfter) Miss(n);
                    if (n.Holding) UpdateHold(n, t, dt);
                }
                else if (n.Result == RhythmJudge.None && t >= n.Time)
                {
                    // соперник «поёт» свою ноту
                    n.Result = RhythmJudge.Perfect;
                    n.Holding = n.IsHold;
                    opponentBump = 1f;
                    receptorFlash[n.Lane] = n.IsHold ? (float)n.Length : 0.15f;
                }
                else if (n.Holding && t >= n.End)
                {
                    n.Holding = false;
                }

                PlaceNote(i, n, t);
            }
        }

        void UpdateHold(RhythmNote n, double t, float dt)
        {
            if (t >= n.End)
            {
                n.Holding = false;
                n.HoldDone = true;
                score += 50;
                return;
            }
            if (Input.AnyHeld(n.Lane))
            {
                n.HoldLostAt = -1;
                holdPoints += dt * 120f;
                calories += dt * 0.08f;
                health = Mathf.Clamp01(health + dt * 0.02f);
                receptorFlash[4 + n.Lane] = 0.05f;
                heroPoseTime = Mathf.Max(heroPoseTime, 0.1f);
                return;
            }
            if (n.HoldLostAt < 0) n.HoldLostAt = t;
            else if (t - n.HoldLostAt > 0.2)
            {
                // отпустил позу раньше времени: длинная нота обрывается без бонуса
                n.Holding = false;
                n.HoldDone = true;
                health = Mathf.Clamp01(health - 0.03f);
            }
        }

        void Miss(RhythmNote n)
        {
            n.Result = RhythmJudge.Miss;
            judgeCounts[(int)RhythmJudge.Miss]++;
            misses++;
            combo = 0;
            health = Mathf.Clamp01(health - (SaveData.Current.settings.assist ? 0.05f : 0.08f));
            voiceMuteTime = 0.35f;
            voiceSource.volume = 0f;
            Sfx.Play("weak", 0.35f);
            hero.Play(Art.Hero(SaveData.Current.skin, "hurt", 1), 1f);
            heroRect.localScale = new Vector3(-1f, 1f, 1f);
            heroPoseTime = 0.3f;
            ShowJudge(RhythmJudge.Miss);
            Log(n, RhythmJudge.Miss, 0, "-");
        }

        void PlaceNote(int i, RhythmNote n, double t)
        {
            RectTransform view = noteViews[i];
            RectTransform tail = tailViews[i];
            float x = LaneX(n.Player, n.Lane);
            float y = StrumY - (float)((n.Time - t) * speed);

            bool visible;
            if (n.Holding)
            {
                // голова стоит на приёмнике, хвост укорачивается
                y = StrumY;
                visible = true;
            }
            else if (n.Result == RhythmJudge.None) visible = y > -620f && y < 640f;
            else if (n.Result == RhythmJudge.Miss) visible = y < 640f;
            else visible = false;

            view.gameObject.SetActive(visible);
            if (visible)
            {
                view.anchoredPosition = new Vector2(x, y);
                view.GetComponent<Image>().color = n.Result == RhythmJudge.Miss
                    ? new Color(0.5f, 0.5f, 0.5f, 0.45f)
                    : RhythmArt.LaneColors[n.Lane];
            }

            if (tail == null) return;
            bool tailVisible = visible && !n.HoldDone && (n.Result == RhythmJudge.None || n.Holding);
            tail.gameObject.SetActive(tailVisible);
            if (!tailVisible) return;
            float remaining = (float)((n.Holding ? n.End - t : n.Length) * speed);
            tail.anchoredPosition = new Vector2(x, y);
            tail.sizeDelta = new Vector2(36f, Mathf.Max(0f, remaining));
            tail.localRotation = Quaternion.identity;
        }

        void ShowPose(int lane)
        {
            string anim;
            float facing = -1f;   // герой смотрит на соперника (влево)
            switch (lane)
            {
                case RhythmPoseTracker.Up: anim = "hands"; break;
                case RhythmPoseTracker.Down: anim = "squat"; break;
                case RhythmPoseTracker.Right: anim = "punch"; facing = 1f; break;
                default: anim = "punch"; break;
            }
            hero.Play(Art.Hero(SaveData.Current.skin, anim, 1), 1f);
            heroRect.localScale = new Vector3(facing, 1f, 1f);
            heroPoseTime = 0.35f;
        }

        void ShowJudge(RhythmJudge j)
        {
            judgeText.text = JudgeName(j).ToUpper() + "!";
            judgeText.color = j == RhythmJudge.Perfect ? Ui.Cyan : (j == RhythmJudge.Good ? Ui.Green : (j == RhythmJudge.Ok ? Ui.Amber : Ui.Red));
            judgeTime = 0.6f;
            comboText.text = combo >= 3 ? "КОМБО " + combo : "";
        }

        static string JudgeName(RhythmJudge j)
        {
            switch (j)
            {
                case RhythmJudge.Perfect: return "идеально";
                case RhythmJudge.Good: return "хорошо";
                case RhythmJudge.Ok: return "неплохо";
                case RhythmJudge.Miss: return "мимо";
                default: return "";
            }
        }

        void UpdateVisuals(double t, float dt)
        {
            double beatLen = chart.Beat;
            int beat = (int)Math.Floor(t / beatLen);
            int bar = (int)Math.Floor(t / (beatLen * 4));

            // перед песней — реплика соперника, затем отсчёт на первом такте
            if (t < 0) countText.text = "";
            else if (bar == 0)
            {
                speech.text = "";
                int b = Mathf.Clamp(beat, 0, 3);
                countText.text = b < 3 ? (3 - b).ToString() : "ВПЕРЁД!";
            }
            else countText.text = "";
            titleText.enabled = t < beatLen * 4;

            if (beat != lastBeat && t >= 0)
            {
                lastBeat = beat;
                for (int i = 0; i < 8; i++) receptors[i].rectTransform.localScale = Vector3.one * 1.08f;
            }
            if (bar != lastBar && bar > 0)
            {
                lastBar = bar;
                stageBump = 1f;
            }

            // чья очередь
            int current = -1;
            for (int i = 0; i < chart.PhraseStarts.Count; i++) if (t >= chart.PhraseStarts[i] - beatLen) current = i;
            if (current != phrase)
            {
                phrase = current;
                if (phrase >= 0)
                {
                    bool player = chart.PhrasePlayer[phrase];
                    turnText.text = player ? "ТВОЙ ХОД!" : "СМОТРИ И ЗАПОМИНАЙ";
                    turnText.color = player ? Ui.Green : Ui.Amber;
                }
            }

            // приёмники: светятся, когда соперник поёт или игрок держит позу
            for (int i = 0; i < 8; i++)
            {
                int lane = i % 4;
                bool player = i >= 4;
                receptorFlash[i] -= dt;
                bool lit = receptorFlash[i] > 0f || (player && Input.AnyHeld(lane));
                receptors[i].color = lit ? RhythmArt.LaneColors[lane] : Ui.Gray;
                RectTransform r = receptors[i].rectTransform;
                r.localScale = Vector3.Lerp(r.localScale, Vector3.one, dt * 10f);
            }
            for (int lane = 0; lane < 4; lane++)
                poseIcons[lane].color = Input.AnyHeld(lane) ? RhythmArt.LaneColors[lane] : Ui.Gray;

            // персонажи
            if (heroPoseTime > 0f)
            {
                heroPoseTime -= dt;
                if (heroPoseTime <= 0f)
                {
                    hero.Play(Art.Hero(SaveData.Current.skin, "idle", 2), song.Bpm / 60f);
                    heroRect.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            opponentBump = Mathf.Max(0f, opponentBump - dt * 5f);
            opponentRect.localScale = new Vector3(-(1f + 0.08f * opponentBump), 1f - 0.08f * opponentBump, 1f);
            stageBump = Mathf.Max(0f, stageBump - dt * 4f);
            stage.localScale = Vector3.one * (1f + 0.015f * stageBump);

            judgeTime -= dt;
            if (judgeTime <= 0f) judgeText.text = "";
            if (voiceMuteTime > 0f)
            {
                voiceMuteTime -= dt;
                if (voiceMuteTime <= 0f) voiceSource.volume = Music.Volume;
            }

            // здоровье и счёт
            Ui.SetBar(healthRed.rectTransform, BarWidth, 1f - health);
            healthIcon.rectTransform.anchoredPosition = new Vector2(BarWidth * (0.5f - health), 0f);
            int judged = 0;
            for (int j = 1; j <= 4; j++) judged += judgeCounts[j];
            int progress = Mathf.Clamp(Mathf.RoundToInt((float)(100.0 * t / chart.Length)), 0, 100);
            scoreText.text = "ОЧКИ: " + TotalScore + "   ПРОМАХИ: " + misses + "   ТОЧНОСТЬ: " +
                             (judged > 0 ? Accuracy.ToString("0.0") + "%" : "-") + "   ПЕСНЯ: " + progress + "%";
        }

        int TotalScore { get { return score + Mathf.RoundToInt(holdPoints); } }

        float Accuracy
        {
            get
            {
                int judged = 0;
                float sum = 0f;
                for (int j = 1; j <= 4; j++) { judged += judgeCounts[j]; sum += judgeCounts[j] * Weights[j]; }
                return judged > 0 ? 100f * sum / judged : 0f;
            }
        }

        static string Grade(float accuracy, bool cleared)
        {
            if (!cleared) return "F";
            if (accuracy >= 95f) return "S";
            if (accuracy >= 85f) return "A";
            if (accuracy >= 70f) return "B";
            if (accuracy >= 55f) return "C";
            return "D";
        }

        // ------------------------------------------------------------------ итоги

        void Finish(bool cleared)
        {
            state = State.Done;
            resultTime = 0f;
            if (!cleared) StopAudio();
            Sfx.Play(cleared ? "win" : "lose");

            Profile p = SaveData.Current;
            PlayerStats st = p.stats;
            float accuracy = Accuracy;
            string grade = Grade(accuracy, cleared);
            bool fullCombo = cleared && misses == 0;
            int total = TotalScore;

            // монеты: за счёт и оценку, Премиум удваивает; первое прохождение песни — бонус
            RhythmRecord record = p.Rhythm(song.Id, (int)difficulty);
            bool firstClear = cleared && !p.RhythmCleared(song.Id);
            int coins = 0;
            if (cleared)
            {
                int gradeBonus = grade == "S" ? 40 : grade == "A" ? 25 : grade == "B" ? 15 : grade == "C" ? 8 : 3;
                coins = (total / 400 + gradeBonus + (firstClear ? 30 : 0)) * (p.premium ? 2 : 1);
            }
            p.coins += coins;

            bool newBest = cleared && total > record.bestScore;
            if (cleared)
            {
                record.cleared = true;
                record.bestScore = Math.Max(record.bestScore, total);
                record.bestAccuracy = Math.Max(record.bestAccuracy, accuracy);
                if (GradeRank(grade) > GradeRank(record.grade)) record.grade = grade;
                record.fullCombo |= fullCombo;
            }

            st.rhythmPlays++;
            if (cleared) st.rhythmClears++;
            if (fullCombo) st.rhythmFullCombos++;
            int hits = laneHits[0] + laneHits[1] + laneHits[2] + laneHits[3];
            st.rhythmNotes += hits;
            st.rhythmBestCombo = Math.Max(st.rhythmBestCombo, maxCombo);
            st.bestCombo = Math.Max(st.bestCombo, maxCombo);
            st.handsUp += laneHits[RhythmPoseTracker.Up];
            st.squats += laneHits[RhythmPoseTracker.Down];
            st.armsSide += laneHits[RhythmPoseTracker.Left] + laneHits[RhythmPoseTracker.Right];
            st.calories += calories;
            st.playSeconds += (float)Math.Max(0.0, Math.Min(songTime, chart.Length));
            st.coinsEarned += coins;

            string input = cameraHits >= keyHits && cameraHits > 0 ? "камера" : "клавиатура";
            int place = cleared ? SaveData.AddScore(total, "РИТМ " + song.Title, input) : 0;
            log.AppendLine("# итог: " + (cleared ? "пройдено" : "проигрыш") + "; очки " + total + "; точность " + accuracy.ToString("0.0") +
                           "%; оценка " + grade + "; промахов " + misses + "; макс. комбо " + maxCombo);
            string logPath = RunLog.WriteFile("logs", "rhythm", log.ToString());
            SaveData.Save();
            Achievements.Evaluate();

            // экран итогов
            resultPanel.gameObject.SetActive(true);
            resultPanel.SetAsLastSibling();
            resultTitle.text = cleared ? "ПОБЕДА НАД: " + song.Opponent : "ЛЕНЬ ПОБЕДИЛА... ПОПРОБУЙ ЕЩЁ!";
            resultTitle.color = cleared ? Ui.Gold : Ui.Red;
            resultGrade.text = grade;
            resultGrade.color = grade == "S" ? Ui.Gold : (grade == "F" ? Ui.Red : Color.white);
            resultBody.text =
                "ОЧКИ:          " + total + (newBest ? "  НОВЫЙ РЕКОРД!" : "") + "\n" +
                "ТОЧНОСТЬ:      " + accuracy.ToString("0.0") + "%" + (fullCombo ? "  БЕЗ ПРОМАХОВ!" : "") + "\n" +
                "МАКС. КОМБО:   " + maxCombo + "\n" +
                "ИДЕАЛЬНО / ХОРОШО / НЕПЛОХО / МИМО:\n" +
                "  " + judgeCounts[1] + " / " + judgeCounts[2] + " / " + judgeCounts[3] + " / " + judgeCounts[4] + "\n" +
                "ПОЗЫ: ВВЕРХ " + laneHits[2] + ", ПРИСЕД " + laneHits[1] + ", В СТОРОНЫ " + (laneHits[0] + laneHits[3]) + "\n" +
                "КАЛОРИИ:       " + calories.ToString("0.0") + " ККАЛ\n" +
                "МОНЕТЫ:        +" + coins + (place > 0 ? "   МЕСТО В РЕКОРДАХ: " + place : "");

            // Подбор задержки камеры по среднему отклонению попаданий (если игрок стабильно опаздывает — сдвигаем)
            suggestedDelay = 0;
            if (cameraErrors.Count >= 8)
            {
                cameraErrors.Sort();
                double median = cameraErrors[cameraErrors.Count / 2];
                int ms = (int)Math.Round(median * 1000.0);
                resultLatency.text = "КАМЕРА: СРЕДНЕЕ ОТКЛОНЕНИЕ " + (ms >= 0 ? "+" : "") + ms + " МС (" + (ms >= 0 ? "ПОЗДНО" : "РАНО") +
                                     ") ПО " + cameraErrors.Count + " ПОПАДАНИЯМ";
                if (Math.Abs(ms) >= 25)
                {
                    int current = SaveData.Current.settings.rhythmCameraDelayMs;
                    suggestedDelay = Mathf.Clamp(Mathf.RoundToInt((current + ms) / 10f) * 10, 0, 400);
                }
            }
            else resultLatency.text = logPath != null ? "ЖУРНАЛ ПЕСНИ: " + logPath : "";
            fixDelayButton.gameObject.SetActive(suggestedDelay > 0);
            if (suggestedDelay > 0) Ui.SetButtonText(fixDelayButton, "ПОСТАВИТЬ ЗАДЕРЖКУ КАМЕРЫ " + suggestedDelay + " МС");
        }

        static int GradeRank(string grade)
        {
            return string.IsNullOrEmpty(grade) ? 0 : "FDCBAS".IndexOf(grade[0]) + 1;
        }

        void FixDelay()
        {
            SaveData.Current.settings.rhythmCameraDelayMs = suggestedDelay;
            SaveData.Save();
            fixDelayButton.gameObject.SetActive(false);
            App.Toasts.Push("ГОТОВО", "Задержка камеры: " + suggestedDelay + " мс", "icon_camera");
        }
    }
}
