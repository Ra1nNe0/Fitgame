using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Настройка камеры: картинка со скелетом, состояние нейросети, калибровка,
    /// шкалы четырёх движений в реальном времени и последнее распознанное движение.
    /// </summary>
    public class CameraScreen : UiScreen
    {
        SkeletonView view;
        Text status, hint, last;
        RectTransform calibration;
        readonly RectTransform[] bars = new RectTransform[5];
        Button startButton;
        float lastTime;

        protected override void Build()
        {
            Ui.Dim(Root, 0.6f).raycastTarget = false;
            Ui.Label(Root, "КАМЕРА И РАСПОЗНАВАНИЕ ДВИЖЕНИЙ", 32, Ui.Gold, new Vector2(0f, 470f), new Vector2(1800f, 50f));
            view = new SkeletonView(Root, new Vector2(-440f, 50f), 900f);

            RectTransform side = Ui.Panel(Root, new Vector2(500f, 50f), new Vector2(820f, 760f)).rectTransform;
            status = Ui.Label(side, "", 20, Color.white, new Vector2(0f, 300f), new Vector2(760f, 120f), TextAnchor.UpperLeft);
            Ui.Label(side, "КАЛИБРОВКА", 16, Ui.Silver, new Vector2(-260f, 200f), new Vector2(240f, 30f), TextAnchor.MiddleLeft);
            Image fill;
            calibration = Ui.Bar(side, new Vector2(110f, 200f), new Vector2(480f, 20f), Ui.Amber, out fill);

            string[] names = { "", "ПРЫЖОК", "ПРИСЕД", "УДАР", "РУКИ ВВЕРХ" };
            string[] cues = { "", "cue_jump_0", "cue_squat_0", "cue_punch_0", "cue_hands_0" };
            for (int m = 1; m <= 4; m++)
            {
                float y = 120f - (m - 1) * 80f;
                Ui.Icon(side, cues[m], new Vector2(-340f, y), 48f);
                Ui.Label(side, names[m], 16, Color.white, new Vector2(-180f, y), new Vector2(220f, 30f), TextAnchor.MiddleLeft);
                bars[m] = Ui.Bar(side, new Vector2(150f, y), new Vector2(420f, 24f), Ui.Cyan, out fill);
            }
            last = Ui.Label(side, "", 32, Ui.Green, new Vector2(0f, -210f), new Vector2(760f, 60f));
            hint = Ui.Label(side, "", 16, Ui.Silver, new Vector2(0f, -300f), new Vector2(760f, 100f));

            startButton = Ui.Button(Root, "ЗАПУСТИТЬ КАМЕРУ", new Vector2(-520f, -440f), new Vector2(560f, 96f), Ui.Green, StartCamera, 20, "icon_camera");
            Ui.Button(Root, "КАЛИБРОВКА", new Vector2(-10f, -440f), new Vector2(420f, 96f), Ui.Amber,
                      () => App.Hub.Camera.Classifier.Recalibrate(), 20);
            Ui.Button(Root, "ТЕСТ AI", new Vector2(420f, -440f), new Vector2(400f, 96f), Ui.Magenta, () => App.Go(App.AiTest), 20, "icon_brain");
            Ui.Button(Root, "НАЗАД", new Vector2(780f, -440f), new Vector2(280f, 96f), Ui.Slate, () => App.Go(App.Menu), 20);
        }

        void StartCamera()
        {
            if (App.Hub.Link.Connected) App.Hub.Link.StopServer();
            else
            {
                string error = App.Hub.Link.StartServer(SaveData.Current.settings.cameraIndex);
                if (error != null) App.Modal.Show("КАМЕРА", error, "ПОНЯТНО", null);
            }
        }

        public override void OnShow()
        {
            last.text = "";
            if (SaveData.Current.settings.inputMode == 0)
            {
                App.Modal.Show("КАМЕРА ВЫКЛЮЧЕНА", "В настройках выбрано управление\nс клавиатуры. Включить камеру?", "ВКЛЮЧИТЬ", () =>
                {
                    SaveData.Current.settings.inputMode = 2;
                    SaveData.Save();
                    App.EnsureCamera();
                }, "НЕТ", null);
            }
            else App.EnsureCamera();
        }

        public override void Tick()
        {
            PoseLink link = App.Hub.Link;
            PoseMotionClassifier c = App.Hub.Camera.Classifier;
            view.Refresh(link, c);

            Ui.SetButtonText(startButton, link.Connected ? "ОСТАНОВИТЬ КАМЕРУ" : "ЗАПУСТИТЬ КАМЕРУ");
            string server = link.Connected ? "РАБОТАЕТ, " + link.Fps.ToString("0") + " КАДР/С" : link.ServerStatus.ToUpper();
            status.text = "СЕРВЕР: " + server + "\nНЕЙРОСЕТЬ: MEDIAPIPE POSE (33 ТОЧКИ)\nСОСТОЯНИЕ: " +
                          (link.Connected ? Hud.TrackerName(c.State) : "-") + "\nСТРОГОСТЬ: x" + c.Strictness.ToString("0.00");
            Ui.SetBar(calibration, 480f, c.State == TrackerState.Ready ? 1f : c.CalibrationProgress);
            for (int m = 1; m <= 4; m++) Ui.SetBar(bars[m], 420f, link.Connected ? c.Levels[m] : 0f);

            hint.text = link.Connected
                ? c.Hint.ToUpper()
                : "ПЕРВЫЙ ЗАПУСК УСТАНАВЛИВАЕТ PYTHON-ПАКЕТЫ (НЕСКОЛЬКО МИНУТ, ОКНО КОНСОЛИ). " +
                  "ВСТАНЬТЕ В 2-3 М ОТ КАМЕРЫ, ЧТОБЫ БЫЛО ВИДНО ВСЁ ТЕЛО.";

            lastTime -= Time.unscaledDeltaTime;
            if (lastTime <= 0f) last.text = "";
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (motion.IsUpdate && last.text.Length > 0)
            {
                last.text = MotionNames.Ru(motion.Type) + "  " + Mathf.RoundToInt(motion.Quality * 100f) + "%";
                return;
            }
            bool ok = motion.Quality >= PlayerController.MinQuality;
            last.text = (ok ? "" : "СЛАБО: ") + MotionNames.Ru(motion.Type) + "  " + Mathf.RoundToInt(motion.Quality * 100f) + "%";
            last.color = ok ? Ui.Green : Ui.Amber;
            lastTime = 2f;
            if (ok) Sfx.Play("good", 0.4f);
        }

        public override bool OnBack()
        {
            App.Go(App.Menu);
            return true;
        }
    }

    /// <summary>
    /// Тестирование AI-компонента (лабораторная №12): 20 заданий, по 5 на каждое движение, в случайном порядке.
    /// Считаются правильные ответы, ошибки, матрица ошибок, точность (Accuracy), среднее качество и время реакции.
    /// Результат сохраняется в CSV.
    /// </summary>
    public class AiTestScreen : UiScreen
    {
        const int PerMotion = 5;
        const float Window = 3.5f;
        const float Pause = 1.3f;

        enum Phase { Intro, Prompt, Feedback, Done }

        Phase phase;
        RectTransform introPanel, testPanel, resultPanel;
        SkeletonView view;
        Text prompt, feedback, progress, timerText;
        Image promptIcon;
        RectTransform timerBar;

        readonly List<MotionType> tasks = new List<MotionType>();
        readonly List<string> rows = new List<string>();
        int index;
        float time;
        MotionType detected;
        float detectedQuality, reaction;
        bool weakSeen;
        readonly int[,] matrix = new int[5, 5];       // [нужно, распознано]; 0 = ничего
        readonly float[] qualitySum = new float[5];
        readonly float[] reactionSum = new float[5];
        string source;

        protected override void Build()
        {
            Ui.Dim(Root, 0.7f).raycastTarget = false;
            Ui.Label(Root, "ТЕСТИРОВАНИЕ AI", 40, Ui.Gold, new Vector2(0f, 470f), new Vector2(1600f, 60f));

            introPanel = Ui.Panel(Root, new Vector2(0f, 20f), new Vector2(1300f, 700f)).rectTransform;
            Ui.Label(introPanel,
                     "20 заданий: по 5 раз каждое движение в случайном порядке.\n\n" +
                     "Когда появится название движения, выполните его.\n" +
                     "На каждое задание — " + Window.ToString("0.0") + " секунды.\n\n" +
                     "Программа посчитает правильные ответы, ошибки,\nточность (Accuracy), качество и время реакции\n" +
                     "и сохранит отчёт в файл CSV.",
                     24, Color.white, new Vector2(0f, 60f), new Vector2(1200f, 460f));
            Ui.Button(introPanel, "НАЧАТЬ ТЕСТ", new Vector2(0f, -260f), new Vector2(520f, 100f), Ui.Green, Begin, 24, "icon_play");

            testPanel = Ui.Node(Root, "Test", Vector2.zero, new Vector2(1900f, 900f));
            view = new SkeletonView(testPanel, new Vector2(-470f, 0f), 800f);
            RectTransform side = Ui.Panel(testPanel, new Vector2(470f, 0f), new Vector2(860f, 700f)).rectTransform;
            progress = Ui.Label(side, "", 24, Ui.Silver, new Vector2(0f, 290f), new Vector2(800f, 40f));
            Ui.Label(side, "СДЕЛАЙТЕ:", 24, Color.white, new Vector2(0f, 200f), new Vector2(800f, 40f));
            promptIcon = Ui.Icon(side, "cue_jump_0", new Vector2(0f, 70f), 144f);
            prompt = Ui.Label(side, "", 48, Ui.Gold, new Vector2(0f, -60f), new Vector2(800f, 70f));
            Image fill;
            timerBar = Ui.Bar(side, new Vector2(0f, -150f), new Vector2(600f, 24f), Ui.Amber, out fill);
            timerText = Ui.Label(side, "", 16, Ui.Silver, new Vector2(0f, -195f), new Vector2(800f, 30f));
            feedback = Ui.Label(side, "", 32, Ui.Green, new Vector2(0f, -270f), new Vector2(800f, 60f));

            resultPanel = Ui.Node(Root, "Result", Vector2.zero, new Vector2(1800f, 860f));
            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, () => App.Go(App.CameraSetup), 24, "icon_back");
        }

        public override void OnShow()
        {
            phase = Phase.Intro;
            introPanel.gameObject.SetActive(true);
            testPanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            App.EnsureCamera();
        }

        void Begin()
        {
            tasks.Clear();
            for (int m = 1; m <= 4; m++)
                for (int i = 0; i < PerMotion; i++) tasks.Add((MotionType)m);
            for (int i = tasks.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                MotionType t = tasks[i]; tasks[i] = tasks[j]; tasks[j] = t;
            }
            Array.Clear(matrix, 0, matrix.Length);
            Array.Clear(qualitySum, 0, 5);
            Array.Clear(reactionSum, 0, 5);
            rows.Clear();
            source = App.Hub.CameraActive ? "камера" : "клавиатура";

            introPanel.gameObject.SetActive(false);
            testPanel.gameObject.SetActive(true);
            resultPanel.gameObject.SetActive(false);
            index = -1;
            NextTask();
        }

        void NextTask()
        {
            index++;
            if (index >= tasks.Count)
            {
                Finish();
                return;
            }
            MotionType t = tasks[index];
            phase = Phase.Prompt;
            time = 0f;
            detected = MotionType.None;
            detectedQuality = 0f;
            weakSeen = false;
            progress.text = "ЗАДАНИЕ " + (index + 1) + " ИЗ " + tasks.Count;
            prompt.text = MotionNames.Ru(t);
            promptIcon.sprite = Art.Ui(Cue(t));
            feedback.text = "";
            Sfx.Play("tick");
        }

        static string Cue(MotionType t)
        {
            switch (t)
            {
                case MotionType.Jump: return "cue_jump_0";
                case MotionType.Squat: return "cue_squat_0";
                case MotionType.Punch: return "cue_punch_0";
                default: return "cue_hands_0";
            }
        }

        public override void Tick()
        {
            view.Refresh(App.Hub.Link, App.Hub.Camera.Classifier);
            if (phase == Phase.Prompt)
            {
                time += Time.unscaledDeltaTime;
                Ui.SetBar(timerBar, 600f, 1f - time / Window);
                timerText.text = Mathf.Max(0f, Window - time).ToString("0.0") + " С";
                if (time >= Window) Judge();
            }
            else if (phase == Phase.Feedback)
            {
                time += Time.unscaledDeltaTime;
                if (time >= Pause) NextTask();
            }
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (phase != Phase.Prompt) return;
            if (motion.IsUpdate)
            {
                if (motion.Type == detected) detectedQuality = Mathf.Max(detectedQuality, motion.Quality);
                return;
            }
            if (motion.Quality < PlayerController.MinQuality)
            {
                weakSeen = true;
                return;
            }
            if (detected != MotionType.None) return;
            detected = motion.Type;
            detectedQuality = motion.Quality;
            reaction = time;
            // ждём ещё немного: камера может уточнить качество
            time = Mathf.Max(time, Window - 0.6f);
        }

        void Judge()
        {
            MotionType need = tasks[index];
            matrix[(int)need, (int)detected]++;
            bool ok = need == detected;
            if (ok)
            {
                qualitySum[(int)need] += detectedQuality;
                reactionSum[(int)need] += reaction;
                feedback.text = "ВЕРНО! КАЧЕСТВО " + Mathf.RoundToInt(detectedQuality * 100f) + "%";
                feedback.color = Ui.Green;
                Sfx.Play("good");
            }
            else
            {
                feedback.text = detected == MotionType.None
                    ? (weakSeen ? "ДВИЖЕНИЕ НЕ ДОВЕДЕНО" : "НЕ РАСПОЗНАНО")
                    : "РАСПОЗНАНО: " + MotionNames.Ru(detected);
                feedback.color = Ui.Red;
                Sfx.Play("weak");
            }
            rows.Add((index + 1) + ";" + MotionNames.Ru(need) + ";" + (detected == MotionType.None ? "-" : MotionNames.Ru(detected)) + ";" +
                     (ok ? "верно" : "ошибка") + ";" + detectedQuality.ToString("0.00") + ";" + (detected == MotionType.None ? "-" : reaction.ToString("0.00")) +
                     ";" + (weakSeen ? "да" : "нет"));
            phase = Phase.Feedback;
            time = 0f;
        }

        void Finish()
        {
            phase = Phase.Done;
            testPanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(true);
            Ui.Clear(resultPanel);

            int correct = 0;
            for (int m = 1; m <= 4; m++) correct += matrix[m, m];
            float accuracy = 100f * correct / tasks.Count;

            var csv = new StringBuilder();
            csv.AppendLine("# FitQuest: тест распознавания движений; " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + "; ввод: " + source +
                           "; строгость: " + SaveData.Current.settings.StrictnessValue.ToString("0.00"));
            csv.AppendLine("№;нужно;распознано;результат;качество;реакция, с;слабая попытка");
            foreach (string r in rows) csv.AppendLine(r);
            csv.AppendLine();
            csv.AppendLine("движение;верно;всего;точность %;среднее качество;средняя реакция, с");

            RectTransform p = Ui.Panel(resultPanel, Vector2.zero, new Vector2(1800f, 860f)).rectTransform;
            Ui.Label(p, "ТОЧНОСТЬ: " + accuracy.ToString("0") + "%  (" + correct + " ИЗ " + tasks.Count + ")   ВВОД: " + source.ToUpper(),
                     32, accuracy >= 80f ? Ui.Green : Ui.Amber, new Vector2(0f, 360f), new Vector2(1700f, 50f));

            Ui.Label(p, "ДВИЖЕНИЕ      ВЕРНО   КАЧЕСТВО  РЕАКЦИЯ", 20, Ui.Cyan, new Vector2(-420f, 270f), new Vector2(860f, 40f), TextAnchor.MiddleLeft);
            for (int m = 1; m <= 4; m++)
            {
                int ok = matrix[m, m];
                string q = ok > 0 ? Mathf.RoundToInt(qualitySum[m] / ok * 100f) + "%" : "-";
                string rt = ok > 0 ? (reactionSum[m] / ok).ToString("0.00") + "С" : "-";
                Ui.Label(p, MotionNames.Ru((MotionType)m).PadRight(14) + (ok + "/" + PerMotion).PadRight(8) + q.PadRight(10) + rt,
                         20, Color.white, new Vector2(-420f, 210f - (m - 1) * 56f), new Vector2(860f, 40f), TextAnchor.MiddleLeft);
                csv.AppendLine(MotionNames.Ru((MotionType)m) + ";" + ok + ";" + PerMotion + ";" + (100f * ok / PerMotion).ToString("0") + ";" +
                               (ok > 0 ? (qualitySum[m] / ok).ToString("0.00") : "-") + ";" + (ok > 0 ? (reactionSum[m] / ok).ToString("0.00") : "-"));
            }

            // матрица ошибок
            string[] shortNames = { "НЕТ", "ПРЫЖ", "ПРИС", "УДАР", "РУКИ" };
            Ui.Label(p, "МАТРИЦА ОШИБОК (СТРОКА — НУЖНО, СТОЛБЕЦ — РАСПОЗНАНО)", 16, Ui.Cyan, new Vector2(430f, 270f), new Vector2(860f, 40f));
            string header = "      ";
            for (int c = 1; c <= 4; c++) header += shortNames[c].PadRight(6);
            header += shortNames[0];
            Ui.Label(p, header, 20, Ui.Silver, new Vector2(430f, 210f), new Vector2(860f, 40f), TextAnchor.MiddleLeft);
            csv.AppendLine();
            csv.AppendLine("матрица ошибок;" + "ПРЫЖОК;ПРИСЕД;УДАР;РУКИ ВВЕРХ;не распознано");
            for (int r = 1; r <= 4; r++)
            {
                string line = shortNames[r].PadRight(6);
                string csvLine = MotionNames.Ru((MotionType)r);
                for (int c = 1; c <= 4; c++) { line += matrix[r, c].ToString().PadRight(6); csvLine += ";" + matrix[r, c]; }
                line += matrix[r, 0];
                csvLine += ";" + matrix[r, 0];
                Ui.Label(p, line, 20, Color.white, new Vector2(430f, 150f - (r - 1) * 56f), new Vector2(860f, 40f), TextAnchor.MiddleLeft);
                csv.AppendLine(csvLine);
            }
            csv.AppendLine();
            csv.AppendLine("итого верно;" + correct + ";из;" + tasks.Count + ";accuracy %;" + accuracy.ToString("0.0"));

            string path = RunLog.WriteFile("ai_tests", "ai_test", csv.ToString());
            Ui.Label(p, path != null ? "ОТЧЁТ СОХРАНЁН:\n" + path : "НЕ УДАЛОСЬ СОХРАНИТЬ ОТЧЁТ", 16, Ui.Silver,
                     new Vector2(0f, -170f), new Vector2(1700f, 80f));
            Ui.Button(p, "ЕЩЁ РАЗ", new Vector2(-250f, -330f), new Vector2(420f, 100f), Ui.Green, Begin, 24);
            Ui.Button(p, "ОТКРЫТЬ ПАПКУ", new Vector2(250f, -330f), new Vector2(460f, 100f), Ui.Blue,
                      () => Application.OpenURL("file:///" + System.IO.Path.Combine(SaveData.Folder, "ai_tests").Replace('\\', '/')), 24);

            SaveData.Current.stats.aiTests++;
            SaveData.Save();
            Sfx.Play("win");
        }

        public override bool OnBack()
        {
            App.Go(App.CameraSetup);
            return true;
        }
    }

    /// <summary>Обучение с помощником Пульсом: по очереди пробуем все четыре движения.</summary>
    public class TutorialScreen : UiScreen
    {
        struct Step
        {
            public string Text;
            public MotionType Wait;
        }

        static readonly Step[] Steps =
        {
            new Step { Text = "Привет! Я Пульс, твой тренер.\nЛорд Лень усыпил королевство —\nразбудим его движением!" },
            new Step { Text = "Подпрыгни!\nТак перепрыгивают шипы и брёвна.\n(клавиша ПРОБЕЛ)", Wait = MotionType.Jump },
            new Step { Text = "Присядь поглубже!\nТак проходят под летучими мышами.\n(клавиша S)", Wait = MotionType.Squat },
            new Step { Text = "Удар рукой вперёд или в сторону!\nТак разбивают ящики и слизней.\n(клавиша D)", Wait = MotionType.Punch },
            new Step { Text = "Подними обе руки вверх!\nТак хватаются за лиану и ловят монеты.\n(клавиша W)", Wait = MotionType.HandsUp },
            new Step { Text = "Отлично! Глубже присед и выше прыжок —\nбольше очков. Держи 50 монет. Вперёд!" },
        };

        int step;
        Text bubble, status;
        Button next;
        UiImageAnimator pulse;
        SkeletonView view;
        float waitDone;

        protected override void Build()
        {
            Ui.Dim(Root, 0.35f).raycastTarget = false;
            pulse = Ui.Animated(Root, Ui.UiFrames("pulse", 4), 2.5f, new Vector2(-150f, 220f), 8f);
            RectTransform panel = Ui.Panel(Root, new Vector2(330f, 220f), new Vector2(900f, 330f), true).rectTransform;
            bubble = Ui.Label(panel, "", 24, Ui.Ink, Vector2.zero, new Vector2(840f, 300f));
            bubble.GetComponent<Shadow>().enabled = false;
            status = Ui.Label(Root, "", 32, Ui.Gold, new Vector2(0f, -80f), new Vector2(1600f, 50f));
            view = new SkeletonView(Root, new Vector2(-24f, 24f), 420f, new Vector2(1f, 0f));
            next = Ui.Button(Root, "ДАЛЕЕ", new Vector2(330f, -10f), new Vector2(420f, 96f), Ui.Green, Next, 24, "icon_play");
            Ui.Button(Root, "ВЫЙТИ", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, () => App.Go(App.Menu), 24, "icon_back");
        }

        public override void OnShow()
        {
            step = 0;
            Apply();
            App.EnsureCamera();
        }

        void Apply()
        {
            Step s = Steps[step];
            pulse.Play(Ui.UiFrames("pulse", 4), 2.5f);
            bubble.text = s.Text;
            next.gameObject.SetActive(s.Wait == MotionType.None);
            Ui.SetButtonText(next, step == Steps.Length - 1 ? "ГОТОВО" : "ДАЛЕЕ");
            status.text = s.Wait != MotionType.None ? "ЖДУ: " + MotionNames.Ru(s.Wait) : "";
            status.color = Ui.Gold;
            waitDone = 0f;
        }

        void Next()
        {
            if (step == Steps.Length - 1)
            {
                Profile p = SaveData.Current;
                if (!p.tutorialDone)
                {
                    p.tutorialDone = true;
                    p.coins += 50;
                    SaveData.Save();
                    App.Toasts.Push("ОБУЧЕНИЕ ПРОЙДЕНО", "+50 МОНЕТ", "coin_0");
                }
                App.Go(App.Map);
                return;
            }
            step++;
            Apply();
        }

        public override void Tick()
        {
            bool cameraOn = App.Hub.CameraActive;
            view.SetActive(cameraOn);
            if (cameraOn) view.Refresh(App.Hub.Link, App.Hub.Camera.Classifier);

            if (waitDone > 0f)
            {
                waitDone -= Time.unscaledDeltaTime;
                if (waitDone <= 0f) Next();
            }
            else if (GameKeys.Confirm && Steps[step].Wait == MotionType.None) Next();
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (motion.IsUpdate) return;
            App.Player.TryPerform(motion);     // герой на заднем плане повторяет движение
            Step s = Steps[step];
            if (s.Wait == MotionType.None || waitDone > 0f) return;

            if (motion.Quality < PlayerController.MinQuality)
            {
                status.text = "ПОЧТИ! СДЕЛАЙ " + MotionNames.Ru(s.Wait) + " ПОЛНОСТЬЮ";
                status.color = Ui.Amber;
                Sfx.Play("weak");
            }
            else if (motion.Type == s.Wait)
            {
                status.text = "ОТЛИЧНО! КАЧЕСТВО " + Mathf.RoundToInt(motion.Quality * 100f) + "%";
                status.color = Ui.Green;
                pulse.Play(new[] { Art.Ui("pulse_3") }, 1f);
                Sfx.Play("good");
                waitDone = 1.2f;
            }
            else
            {
                status.text = "ЭТО " + MotionNames.Ru(motion.Type) + ". НУЖНО: " + MotionNames.Ru(s.Wait);
                status.color = Ui.Amber;
            }
        }

        public override void OnHide()
        {
            pulse.Play(Ui.UiFrames("pulse", 4), 2.5f);
        }

        public override bool OnBack()
        {
            App.Go(App.Menu);
            return true;
        }
    }
}
