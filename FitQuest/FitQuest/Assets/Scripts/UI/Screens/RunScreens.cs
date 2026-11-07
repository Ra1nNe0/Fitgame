using System;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>Пауза (Esc во время забега).</summary>
    public class PauseScreen : UiScreen
    {
        protected override void Build()
        {
            Ui.Dim(Root, 0.6f);
            RectTransform p = Ui.Panel(Root, Vector2.zero, new Vector2(760f, 640f)).rectTransform;
            Ui.Label(p, "ПАУЗА", 48, Ui.Gold, new Vector2(0f, 220f), new Vector2(700f, 70f));
            Ui.Button(p, "ПРОДОЛЖИТЬ", new Vector2(0f, 80f), new Vector2(560f, 100f), Ui.Green, () => App.Resume(), 24, "icon_play");
            Ui.Button(p, "ЗАНОВО", new Vector2(0f, -40f), new Vector2(560f, 100f), Ui.Amber, () => App.RestartRun(), 24);
            Ui.Button(p, "НА КАРТУ", new Vector2(0f, -160f), new Vector2(560f, 100f), Ui.Slate, () => App.QuitRun(), 24, "icon_map");
        }

        public override bool OnBack()
        {
            App.Resume();
            return true;
        }
    }

    /// <summary>Жизни кончились: продолжить за рекламу / с Премиумом или завершить.</summary>
    public class GameOverScreen : UiScreen
    {
        Button premium, ad;
        float shownTime;

        protected override void Build()
        {
            Ui.Dim(Root, 0.6f);
            RectTransform p = Ui.Panel(Root, Vector2.zero, new Vector2(1000f, 700f)).rectTransform;
            Ui.Label(p, "НЕ СДАВАЙСЯ!", 48, Ui.Red, new Vector2(0f, 250f), new Vector2(900f, 70f));
            Ui.Animated(p, Ui.UiFrames("pulse", 3), 3f, new Vector2(0f, 120f), 5f);
            premium = Ui.Button(p, "ПРОДОЛЖИТЬ (ПРЕМИУМ)", new Vector2(0f, -40f), new Vector2(760f, 100f), Ui.Gold,
                                () => App.Game.Revive(), 24, "icon_crown");
            ad = Ui.Button(p, "ПРОДОЛЖИТЬ ЗА РЕКЛАМУ", new Vector2(0f, -40f), new Vector2(760f, 100f), Ui.Green,
                           () => App.ShowAd(false, () => { App.Go(null); App.Game.Revive(); }), 24, "icon_film");
            Ui.Button(p, "ЗАВЕРШИТЬ", new Vector2(0f, -170f), new Vector2(760f, 100f), Ui.Slate, () => App.Game.Abandon(), 24);
        }

        public override void OnShow()
        {
            shownTime = 0f;
            bool can = App.Game.CanRevive;
            bool isPremium = SaveData.Current.premium;
            premium.gameObject.SetActive(can && isPremium);
            ad.gameObject.SetActive(can && !isPremium);
            if (!can) App.Game.Abandon();
        }

        public override void Tick()
        {
            shownTime += Time.unscaledDeltaTime;
            if (shownTime > 1f && GameKeys.Confirm) App.Game.Abandon();
        }

        public override bool OnBack()
        {
            if (shownTime > 0.5f) App.Game.Abandon();
            return true;
        }

        public override void OnMotion(MotionEvent motion)
        {
            // Камерой: руки вверх — продолжить, прыжок — завершить. Первые 2 секунды движения не считаются,
            // чтобы последний прыжок в забеге случайно не закрыл это окно.
            if (shownTime < 2f || motion.IsUpdate || motion.Quality < PlayerController.MinQuality) return;
            if (motion.Type == MotionType.Jump) App.Game.Abandon();
            else if (motion.Type == MotionType.HandsUp)
            {
                if (premium.gameObject.activeSelf) premium.onClick.Invoke();
                else if (ad.gameObject.activeSelf) ad.onClick.Invoke();
            }
        }
    }

    /// <summary>Итоги забега.</summary>
    public class ResultsScreen : UiScreen
    {
        RectTransform content;
        RunResult result;
        Button next;
        Image[] stars = new Image[3];
        Text scoreText;
        float shownTime;

        protected override void Build()
        {
            Ui.Dim(Root, 0.55f);
            content = Ui.Node(Root, "Content", Vector2.zero, new Vector2(1500f, 1000f));
        }

        public void Set(RunResult r)
        {
            result = r;
        }

        public override void OnShow()
        {
            Ui.Clear(content);
            RunResult r = result;
            RectTransform p = Ui.Panel(content, Vector2.zero, new Vector2(1500f, 1000f)).rectTransform;

            string header = r.Marathon ? "МАРАФОН ОКОНЧЕН" : (r.Won ? "УРОВЕНЬ ПРОЙДЕН!" : "ПОРАЖЕНИЕ");
            Ui.Label(p, header, 48, r.Won || r.Marathon ? Ui.Gold : Ui.Red, new Vector2(0f, 430f), new Vector2(1400f, 70f));
            Ui.Label(p, r.Title, 24, Ui.Silver, new Vector2(0f, 370f), new Vector2(1400f, 40f));

            for (int i = 0; i < 3; i++)
            {
                stars[i] = Ui.Icon(p, "icon_star_empty", new Vector2(-140f + i * 140f, 270f), 120f);
                stars[i].enabled = !r.Marathon;
            }

            scoreText = Ui.Label(p, "0", 64, Color.white, new Vector2(0f, 160f), new Vector2(800f, 80f));
            string best = r.Place == 1 ? "НОВЫЙ РЕКОРД!" : (r.NewBest ? "ЛУЧШИЙ РЕЗУЛЬТАТ УРОВНЯ!" : (r.Place > 0 ? "МЕСТО В ТАБЛИЦЕ: " + r.Place : ""));
            Ui.Label(p, best, 24, Ui.Green, new Vector2(0f, 95f), new Vector2(1200f, 40f));

            // слева — общие показатели
            string coins = "+" + r.Coins + (r.CoinMultiplier > 1 ? " (x" + r.CoinMultiplier + ")" : "");
            string left =
                "ТОЧНОСТЬ:      " + r.Accuracy + "%\n" +
                "ЛУЧШЕЕ КОМБО:  " + r.BestCombo + "\n" +
                "МОНЕТЫ:        " + coins + "\n" +
                "КАЛОРИИ:       " + r.Calories.ToString("0.0") + " ККАЛ\n" +
                "ВРЕМЯ:         " + FormatTime(r.Duration) + "\n" +
                "ТЕМП (AI):     x" + r.Tempo.ToString("0.00") + "\n" +
                "ВВОД:          " + r.Input.ToUpper() +
                (r.Marathon ? "\nЭТАП:          " + r.Stage : "");
            Ui.Label(p, left, 20, Color.white, new Vector2(-360f, -120f), new Vector2(660f, 360f), TextAnchor.UpperLeft);

            // справа — по движениям
            Ui.Label(p, "ДВИЖЕНИЕ    ВОВРЕМЯ   КАЧЕСТВО", 16, Ui.Cyan, new Vector2(390f, 40f), new Vector2(660f, 30f), TextAnchor.MiddleLeft);
            string[] icons = { "", "cue_jump_0", "cue_squat_0", "cue_punch_0", "cue_hands_0" };
            for (int m = 1; m <= 4; m++)
            {
                float y = -10f - (m - 1) * 70f;
                Ui.Icon(p, icons[m], new Vector2(70f, y), 48f);
                string done = r.Needed[m] > 0 ? r.Passed[m] + "/" + r.Needed[m] : "-";
                string q = r.Passed[m] > 0 ? (r.Quality[m] / r.Passed[m] * 100f).ToString("0") + "%" : "-";
                Ui.Label(p, MotionNames.Ru((MotionType)m).PadRight(11) + done.PadRight(9) + q, 16, Color.white,
                         new Vector2(420f, y), new Vector2(600f, 40f), TextAnchor.MiddleLeft);
            }
            if (r.Weak > 0)
                Ui.Label(p, "НЕДОДЕЛАННЫХ ДВИЖЕНИЙ: " + r.Weak, 16, Ui.Amber, new Vector2(360f, -300f), new Vector2(660f, 30f), TextAnchor.MiddleLeft);

            if (r.NewAchievements.Count > 0)
            {
                string names = "";
                foreach (AchievementDef a in r.NewAchievements) names += (names.Length > 0 ? ", " : "") + a.Title;
                Ui.Label(p, "ДОСТИЖЕНИЯ: " + names, 16, Ui.Gold, new Vector2(0f, -345f), new Vector2(1400f, 30f));
            }

            bool hasNext = !r.Marathon && r.Won && r.Level + 1 < Levels.All.Length;
            next = Ui.Button(p, hasNext ? "ДАЛЕЕ" : "КАРТА", new Vector2(-440f, -420f), new Vector2(400f, 100f), Ui.Green,
                             () => { if (hasNext) App.OpenBriefing(r.Level + 1, false); else App.Go(App.Map); }, 24, "icon_play");
            Ui.Button(p, "ЕЩЁ РАЗ", new Vector2(0f, -420f), new Vector2(400f, 100f), Ui.Amber,
                      () => App.OpenBriefing(r.Level, r.Marathon), 24);
            Ui.Button(p, "МЕНЮ", new Vector2(440f, -420f), new Vector2(400f, 100f), Ui.Slate, () => App.Go(App.Menu), 24, "icon_back");

            shownTime = 0f;
            Music.Play("menu");
        }

        public override void Tick()
        {
            shownTime += Time.unscaledDeltaTime;
            // очки «набегают», звёзды загораются по очереди
            float k = Mathf.Clamp01(shownTime / 1.2f);
            scoreText.text = Mathf.RoundToInt(result.Score * k).ToString();
            for (int i = 0; i < 3; i++)
            {
                bool lit = i < result.Stars && shownTime > 0.5f + i * 0.35f;
                Sprite s = Art.Ui(lit ? "icon_star" : "icon_star_empty");
                if (stars[i].sprite != s)
                {
                    stars[i].sprite = s;
                    if (lit) Sfx.Play("coin", 0.5f);
                }
            }
            if (shownTime > 1f && GameKeys.Confirm) next.onClick.Invoke();
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (shownTime > 1.5f && !motion.IsUpdate && motion.Type == MotionType.Jump && motion.Quality >= PlayerController.MinQuality)
                next.onClick.Invoke();
        }

        public override bool OnBack()
        {
            App.Go(App.Menu);
            return true;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.RoundToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }

    /// <summary>
    /// Реклама с вознаграждением (демо). В настоящем продукте здесь был бы рекламный SDK;
    /// в прототипе — 5-секундный ролик-заглушка с рекламой собственного Премиума.
    /// </summary>
    public class AdScreen : UiScreen
    {
        Text counter;
        Button close;
        float time;
        Action reward;

        protected override void Build()
        {
            Ui.Box(Root, Vector2.zero, new Vector2(4000f, 3000f), Ui.Ink);
            Ui.Label(Root, "РЕКЛАМА", 24, Ui.Gray, new Vector2(40f, -40f), new Vector2(400f, 40f), TextAnchor.MiddleLeft, new Vector2(0f, 1f));
            counter = Ui.Label(Root, "", 24, Color.white, new Vector2(-40f, -40f), new Vector2(600f, 40f), TextAnchor.MiddleRight, new Vector2(1f, 1f));
            Ui.Picture(Root, "logo_0", new Vector2(0f, 260f), 5f);
            Ui.Icon(Root, "icon_crown", new Vector2(0f, 80f), 144f);
            Ui.Label(Root, "FITQUEST PREMIUM", 48, Ui.Gold, new Vector2(0f, -40f), new Vector2(1600f, 70f));
            Ui.Label(Root, "Двойные монеты • Марафон\nи продолжение без рекламы",
                     24, Color.white, new Vector2(0f, -150f), new Vector2(1600f, 100f));
            Ui.Label(Root, "Демонстрационная реклама учебного прототипа", 16, Ui.Gray, new Vector2(0f, 40f), new Vector2(1600f, 30f),
                     TextAnchor.MiddleCenter, new Vector2(0.5f, 0f));
            close = Ui.Button(Root, "ЗАКРЫТЬ", new Vector2(0f, -330f), new Vector2(460f, 100f), Ui.Green, Finish, 24);
        }

        public void Begin(Action onReward)
        {
            reward = onReward;
        }

        public override void OnShow()
        {
            time = 5f;
            close.gameObject.SetActive(false);
        }

        public override void Tick()
        {
            if (time > 0f)
            {
                time -= Time.unscaledDeltaTime;
                counter.text = "НАГРАДА ЧЕРЕЗ " + Mathf.CeilToInt(Mathf.Max(time, 0f));
                if (time <= 0f)
                {
                    counter.text = "НАГРАДА ПОЛУЧЕНА";
                    close.gameObject.SetActive(true);
                }
            }
            else if (GameKeys.Confirm) Finish();
        }

        void Finish()
        {
            Action r = reward;
            reward = null;
            if (r != null) r();
        }

        public override bool OnBack()
        {
            return true;   // рекламу нельзя пропустить до конца
        }
    }
}
