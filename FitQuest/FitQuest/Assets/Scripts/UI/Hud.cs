using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Интерфейс во время забега: сердца, монеты, очки, комбо, темп, полоса прогресса или здоровья босса,
    /// бусты, сообщения, мини-окно камеры и AI-панель (F1) с внутренней «кухней» обеих интеллектуальных механик.
    /// </summary>
    public class Hud
    {
        const int MaxHearts = 5;
        const float BarWidth = 640f;

        readonly RectTransform root;
        readonly Image[] hearts = new Image[MaxHearts];
        readonly Sprite heartFull, heartEmpty;
        readonly Text coinsText, scoreText, comboText, tempoText, barTitle, bigText, smallText, flashText;
        readonly RectTransform barFill;
        readonly Image barFillImage, dim, shieldIcon, magnetIcon;
        readonly RectTransform aiPanel;
        readonly Text aiText;
        readonly RectTransform[] aiBars = new RectTransform[5];
        readonly SkeletonView mini;
        float flashTime;

        MotionHub hub;
        DifficultyDirector director;

        public Hud(Transform parent, MotionHub hub)
        {
            this.hub = hub;
            root = Ui.Stretch(parent, "HUD");
            heartFull = Art.Ui("heart_0");
            heartEmpty = Art.Ui("heart_empty_0");

            dim = Ui.Dim(root, 0.55f);
            dim.raycastTarget = false;

            Vector2 tl = new Vector2(0f, 1f), tr = new Vector2(1f, 1f), tc = new Vector2(0.5f, 1f);
            for (int i = 0; i < MaxHearts; i++)
            {
                hearts[i] = Ui.Icon(root, "heart_0", new Vector2(36f + i * 68f, -32f), 64f, tl);
            }
            Ui.Icon(root, "coin_0", new Vector2(36f, -108f), 64f, tl);
            coinsText = Ui.Label(root, "x0", 32, Ui.Gold, new Vector2(116f, -126f), new Vector2(300f, 40f), TextAnchor.MiddleLeft, tl);
            shieldIcon = Ui.Icon(root, "icon_shield", new Vector2(36f, -186f), 64f, tl);
            magnetIcon = Ui.Icon(root, "icon_magnet", new Vector2(108f, -186f), 64f, tl);

            scoreText = Ui.Label(root, "000000", 40, Color.white, new Vector2(-36f, -36f), new Vector2(400f, 48f), TextAnchor.MiddleRight, tr);
            comboText = Ui.Label(root, "", 24, Ui.Gold, new Vector2(-36f, -96f), new Vector2(400f, 32f), TextAnchor.MiddleRight, tr);
            tempoText = Ui.Label(root, "", 16, Ui.Silver, new Vector2(-36f, -136f), new Vector2(400f, 24f), TextAnchor.MiddleRight, tr);

            barTitle = Ui.Label(root, "", 24, Color.white, new Vector2(0f, -32f), new Vector2(1000f, 32f), TextAnchor.MiddleCenter, tc);
            barFill = Ui.Bar(root, new Vector2(0f, -88f), new Vector2(BarWidth, 24f), Ui.Green, out barFillImage, tc);

            bigText = Ui.Label(root, "", 64, Color.white, new Vector2(0f, 180f), new Vector2(1700f, 100f));
            smallText = Ui.Label(root, "", 32, Color.white, new Vector2(0f, -40f), new Vector2(1500f, 300f));
            flashText = Ui.Label(root, "", 40, Color.white, new Vector2(0f, 290f), new Vector2(1700f, 60f));

            // Мини-окно камеры
            mini = new SkeletonView(root, new Vector2(-24f, 24f), 320f, new Vector2(1f, 0f));

            // AI-панель (F1)
            aiPanel = Ui.Panel(root, new Vector2(24f, 24f), new Vector2(620f, 330f), false, new Vector2(0f, 0f)).rectTransform;
            Ui.Label(aiPanel, "AI-ПАНЕЛЬ (F1)", 16, Ui.Cyan, new Vector2(24f, -24f), new Vector2(560f, 24f), TextAnchor.MiddleLeft, new Vector2(0f, 1f));
            string[] names = { "", "ПРЫЖОК", "ПРИСЕД", "УДАР", "РУКИ" };
            for (int m = 1; m <= 4; m++)
            {
                float y = -60f - (m - 1) * 34f;
                Ui.Label(aiPanel, names[m], 16, Color.white, new Vector2(24f, y), new Vector2(160f, 24f), TextAnchor.MiddleLeft, new Vector2(0f, 1f));
                Image fill;
                aiBars[m] = Ui.Bar(aiPanel, new Vector2(190f, y), new Vector2(380f, 14f), Ui.Cyan, out fill, new Vector2(0f, 1f));
            }
            aiText = Ui.Label(aiPanel, "", 16, Ui.Silver, new Vector2(24f, -206f), new Vector2(580f, 120f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            HideMessage();
            Show(false);
        }

        public void Show(bool on)
        {
            root.gameObject.SetActive(on);
        }

        public void SetHearts(int lives)
        {
            for (int i = 0; i < MaxHearts; i++)
            {
                hearts[i].enabled = i < Mathf.Max(lives, GameManager.StartLives);
                hearts[i].sprite = i < lives ? heartFull : heartEmpty;
            }
        }

        public void SetStats(int score, int coins, int combo, float tempo)
        {
            scoreText.text = score.ToString("000000");
            coinsText.text = "x" + coins;
            comboText.text = combo >= 2 ? "КОМБО x" + combo : "";
            tempoText.text = "ТЕМП x" + tempo.ToString("0.00");
        }

        public void SetBoosts(bool shield, bool magnet)
        {
            shieldIcon.enabled = shield;
            magnetIcon.enabled = magnet;
            magnetIcon.rectTransform.anchoredPosition = new Vector2(shield ? 108f : 36f, -186f);
        }

        public void SetBar(string title, float value, Color color)
        {
            barTitle.text = title;
            Ui.SetBar(barFill, BarWidth, value);
            barFillImage.color = color;
        }

        public void SetDirector(DifficultyDirector d)
        {
            director = d;
        }

        public void ShowMessage(string big, string small)
        {
            dim.enabled = true;
            bigText.text = big;
            smallText.text = small;
        }

        public void HideMessage()
        {
            dim.enabled = false;
            bigText.text = "";
            smallText.text = "";
        }

        public void Flash(string message, Color color)
        {
            flashText.text = message;
            flashText.color = color;
            flashTime = 1.1f;
        }

        public void Tick()
        {
            if (!root.gameObject.activeSelf) return;

            if (flashTime > 0f)
            {
                flashTime -= Time.unscaledDeltaTime;
                if (flashTime <= 0f) flashText.text = "";
            }

            bool cameraOn = hub.CameraActive;
            mini.SetActive(cameraOn);
            if (cameraOn) mini.Refresh(hub.Link, hub.Camera.Classifier);

            if (GameKeys.DebugPanel)
            {
                SaveData.Current.settings.aiPanel = !SaveData.Current.settings.aiPanel;
                SaveData.Save();
            }
            bool showAi = SaveData.Current.settings.aiPanel;
            aiPanel.gameObject.SetActive(showAi);
            if (showAi) RefreshAi();
        }

        void RefreshAi()
        {
            PoseMotionClassifier c = hub.Camera.Classifier;
            for (int m = 1; m <= 4; m++) Ui.SetBar(aiBars[m], 380f, c.Levels[m]);

            string camera = hub.CameraActive
                ? "КАМЕРА: " + TrackerName(c.State) + "  " + hub.Link.Fps.ToString("0") + " К/С"
                : "КАМЕРА: НЕ ПОДКЛЮЧЕНА";
            string tempo = director == null ? "" :
                "СЛОЖНОСТЬ: x" + director.Multiplier.ToString("0.00") + "  ОКНО " + director.WindowFill + "/" + DifficultyDirector.Window +
                "\nУСПЕХ " + Mathf.RoundToInt(director.LastSuccessRate * 100f) + "%  КАЧ. " + director.LastQuality.ToString("0.00") +
                "\nРЕШЕНИЕ: " + director.LastDecision;
            aiText.text = camera + "\n" + tempo;
        }

        public static string TrackerName(TrackerState s)
        {
            switch (s)
            {
                case TrackerState.Ready: return "ГОТОВ";
                case TrackerState.Calibrating: return "КАЛИБРОВКА";
                default: return "НЕТ ЧЕЛОВЕКА";
            }
        }
    }
}
