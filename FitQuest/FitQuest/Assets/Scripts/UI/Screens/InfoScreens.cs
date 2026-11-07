using System;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>Рекорды, статистика тренировок, достижения и история покупок.</summary>
    public class RecordsScreen : UiScreen
    {
        RectTransform content;
        readonly Button[] tabs = new Button[4];
        int tab;
        static readonly string[] TabNames = { "РЕКОРДЫ", "СТАТИСТИКА", "ДОСТИЖЕНИЯ", "ПОКУПКИ" };

        protected override void Build()
        {
            Ui.Dim(Root, 0.5f).raycastTarget = false;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                tabs[i] = Ui.Button(Root, TabNames[i], new Vector2(-660f + i * 440f, 450f), new Vector2(410f, 90f), Ui.Steel,
                                    () => { tab = index; Refresh(); }, 24);
            }
            content = Ui.Node(Root, "Content", new Vector2(0f, -10f), new Vector2(1800f, 780f));
            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, () => App.Go(App.Menu), 24, "icon_back");
        }

        public override void OnShow()
        {
            Refresh();
        }

        public override bool OnBack()
        {
            App.Go(App.Menu);
            return true;
        }

        void Refresh()
        {
            for (int i = 0; i < 4; i++) Ui.SetButtonColor(tabs[i], i == tab ? Ui.Amber : Ui.Steel);
            Ui.Clear(content);
            RectTransform panel = Ui.Panel(content, Vector2.zero, new Vector2(1800f, 780f)).rectTransform;
            switch (tab)
            {
                case 0: Leaderboard(panel); break;
                case 1: Stats(panel); break;
                case 2: AchievementsGrid(panel); break;
                default: Purchases(panel); break;
            }
        }

        void Leaderboard(RectTransform panel)
        {
            var board = SaveData.Current.leaderboard;
            Ui.Label(panel, " #  ИГРОК         ОЧКИ     РЕЖИМ              ВВОД        ДАТА", 20, Ui.Cyan,
                     new Vector2(0f, 330f), new Vector2(1700f, 40f), TextAnchor.MiddleLeft);
            if (board.Count == 0)
                Ui.Label(panel, "Пока пусто. Сыграйте первый уровень!", 24, Ui.Silver, Vector2.zero, new Vector2(1600f, 60f));
            for (int i = 0; i < board.Count; i++)
            {
                ScoreEntry e = board[i];
                string line = (i + 1).ToString().PadLeft(2) + "  " + Cut(e.name, 12).PadRight(14) + e.score.ToString().PadRight(9) +
                              Cut(e.mode, 18).PadRight(19) + Cut(e.input, 11).ToUpper().PadRight(12) + e.date;
                Ui.Label(panel, line, 20, i == 0 ? Ui.Gold : Color.white, new Vector2(0f, 270f - i * 56f), new Vector2(1700f, 40f), TextAnchor.MiddleLeft);
            }
        }

        static string Cut(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > n ? s.Substring(0, n) : s;
        }

        void Stats(RectTransform panel)
        {
            Profile p = SaveData.Current;
            PlayerStats s = p.stats;
            int total = s.obstaclesPassed + s.obstaclesMissed;
            string left =
                "ПРЫЖКИ:            " + s.jumps + "\n" +
                "ПРИСЕДАНИЯ:        " + s.squats + "\n" +
                "УДАРЫ:             " + s.punches + "\n" +
                "РУКИ ВВЕРХ:        " + s.handsUp + "\n" +
                "РУКИ В СТОРОНЫ:    " + s.armsSide + "\n" +
                "НЕДОДЕЛАННЫЕ:      " + s.weakMoves + "\n" +
                "КАЛОРИИ (ОЦЕНКА):  " + s.calories.ToString("0.0") + "\n" +
                "ВРЕМЯ В ИГРЕ:      " + ResultsScreen.FormatTime(s.playSeconds);
            string right =
                "ЗАБЕГОВ:           " + s.runs + "\n" +
                "ПОБЕД:             " + s.wins + "\n" +
                "ТОЧНОСТЬ:          " + (total > 0 ? Mathf.RoundToInt(100f * s.obstaclesPassed / total) : 0) + "%\n" +
                "ЛУЧШЕЕ КОМБО:      " + s.bestCombo + "\n" +
                "МАРАФОН (РЕКОРД):  " + s.marathonBest + "\n" +
                "МОНЕТ ЗАРАБОТАНО:  " + s.coinsEarned + "\n" +
                "ТЕСТОВ AI:         " + s.aiTests + "\n" +
                "РИТМ-БАТТЛЫ:       " + s.rhythmClears + " / " + s.rhythmPlays + " (НОТ " + s.rhythmNotes + ")";
            Ui.Label(panel, "СТАТИСТИКА ТРЕНИРОВОК: " + p.playerName, 24, Ui.Gold, new Vector2(0f, 320f), new Vector2(1700f, 40f));
            Ui.Label(panel, left, 24, Color.white, new Vector2(-430f, -20f), new Vector2(820f, 560f), TextAnchor.MiddleLeft);
            Ui.Label(panel, right, 24, Color.white, new Vector2(430f, -20f), new Vector2(820f, 560f), TextAnchor.MiddleLeft);
        }

        void AchievementsGrid(RectTransform panel)
        {
            AchievementDef[] all = Achievements.All;
            int got = 0;
            foreach (AchievementDef a in all) if (Achievements.Has(a.Id)) got++;
            Ui.Label(panel, "ПОЛУЧЕНО: " + got + " / " + all.Length + "   (+" + Achievements.Reward + " МОНЕТ ЗА КАЖДОЕ)", 20, Ui.Gold,
                     new Vector2(0f, 345f), new Vector2(1700f, 40f));
            for (int i = 0; i < all.Length; i++)
            {
                AchievementDef a = all[i];
                bool has = Achievements.Has(a.Id);
                float x = -660f + (i % 4) * 440f, y = 262f - (i / 4) * 132f;
                RectTransform cell = Ui.Panel(panel, new Vector2(x, y), new Vector2(420f, 124f), has).rectTransform;
                Image icon = Ui.Icon(cell, has ? "icon_" + a.Icon : "icon_lock", new Vector2(-150f, 0f), 64f);
                Text t = Ui.Label(cell, a.Title, 16, has ? Ui.Ink : Ui.Gray, new Vector2(50f, 26f), new Vector2(290f, 36f), TextAnchor.MiddleLeft);
                Text d = Ui.Label(cell, a.Description, 16, has ? Ui.Steel : Ui.Slate, new Vector2(50f, -20f), new Vector2(290f, 60f), TextAnchor.MiddleLeft);
                d.fontSize = 16;
                if (has) { t.GetComponent<Shadow>().enabled = false; d.GetComponent<Shadow>().enabled = false; }
                icon.color = has ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        void Purchases(RectTransform panel)
        {
            Profile p = SaveData.Current;
            Ui.Label(panel, "ИСТОРИЯ ПОКУПОК (ДЕМО-АНАЛИТИКА МОНЕТИЗАЦИИ)", 20, Ui.Gold, new Vector2(0f, 330f), new Vector2(1700f, 40f));
            string summary = "ПОКУПОК: " + p.purchases.Count + "   ДЕМО-ВЫРУЧКА: " + p.demoRevenue + " РУБ.   ПРОСМОТРОВ РЕКЛАМЫ: " + p.adsWatched +
                             "   ПРЕМИУМ: " + (p.premium ? "ДА" : "НЕТ");
            Ui.Label(panel, summary, 16, Ui.Cyan, new Vector2(0f, 270f), new Vector2(1700f, 40f));
            int count = Mathf.Min(9, p.purchases.Count);
            if (count == 0) Ui.Label(panel, "Покупок пока не было", 24, Ui.Silver, Vector2.zero, new Vector2(1600f, 60f));
            for (int i = 0; i < count; i++)
            {
                PurchaseRecord r = p.purchases[p.purchases.Count - 1 - i];
                Ui.Label(panel, r.date.PadRight(18) + Cut(r.item, 20).PadRight(22) + r.price, 20, Color.white,
                         new Vector2(0f, 200f - i * 56f), new Vector2(1600f, 40f), TextAnchor.MiddleLeft);
            }
        }
    }

    /// <summary>Настройки: имя, звук, управление, строгость AI, помощь, камера, сброс.</summary>
    public class SettingsScreen : UiScreen
    {
        InputField nameField;
        readonly Text[] values = new Text[9];

        protected override void Build()
        {
            Ui.Dim(Root, 0.5f).raycastTarget = false;
            RectTransform panel = Ui.Panel(Root, new Vector2(0f, 20f), new Vector2(1500f, 960f)).rectTransform;
            Ui.Label(panel, "НАСТРОЙКИ", 48, Ui.Gold, new Vector2(0f, 420f), new Vector2(1400f, 70f));

            // имя игрока
            Ui.Label(panel, "ИМЯ ИГРОКА", 24, Color.white, new Vector2(-380f, 320f), new Vector2(600f, 40f), TextAnchor.MiddleLeft);
            Image field = Ui.Box(panel, new Vector2(330f, 320f), new Vector2(620f, 70f), Ui.Ink);
            field.raycastTarget = true;
            Text text = Ui.Label(field.rectTransform, "", 24, Color.white, Vector2.zero, new Vector2(580f, 60f), TextAnchor.MiddleLeft);
            text.supportRichText = false;
            nameField = field.gameObject.AddComponent<InputField>();
            nameField.textComponent = text;
            nameField.characterLimit = 12;
            nameField.onEndEdit.AddListener(v =>
            {
                string clean = v.Trim().ToUpper();
                SaveData.Current.playerName = clean.Length > 0 ? clean : "ИГРОК";
                SaveData.Save();
            });

            string[] names = { "МУЗЫКА", "ЗВУКИ", "УПРАВЛЕНИЕ", "СТРОГОСТЬ AI", "ПОМОЩЬ (ЗАПАС ВРЕМЕНИ)", "AI-ПАНЕЛЬ В ИГРЕ (F1)",
                               "КАМЕРА ПРИ ЗАПУСКЕ", "НОМЕР КАМЕРЫ", "ПОЛНЫЙ ЭКРАН" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 235f - i * 68f;
                int index = i;
                Ui.Label(panel, names[i], 20, Color.white, new Vector2(-380f, y), new Vector2(600f, 40f), TextAnchor.MiddleLeft);
                Ui.Button(panel, "<", new Vector2(70f, y), new Vector2(80f, 64f), Ui.Steel, () => Change(index, -1), 24);
                values[i] = Ui.Label(panel, "", 20, Ui.Gold, new Vector2(330f, y), new Vector2(440f, 40f));
                Ui.Button(panel, ">", new Vector2(590f, y), new Vector2(80f, 64f), Ui.Steel, () => Change(index, 1), 24);
            }

            Ui.Button(panel, "СБРОСИТЬ ПРОГРЕСС", new Vector2(-470f, -390f), new Vector2(440f, 84f), Ui.Red, AskReset, 18);
            Ui.Button(panel, "СЮЖЕТ ЗАНОВО", new Vector2(0f, -390f), new Vector2(440f, 84f), Ui.Magenta, () =>
            {
                // катсцены снова покажутся перед уровнями и после побед
                SaveData.Current.seenScenes.Clear();
                SaveData.Save();
                App.Toasts.Push("СЮЖЕТ", "Катсцены покажутся снова", "icon_film");
            }, 18);
            Ui.Button(panel, "ПАПКА С ДАННЫМИ", new Vector2(470f, -390f), new Vector2(440f, 84f), Ui.Blue,
                      () => Application.OpenURL("file:///" + SaveData.Folder.Replace('\\', '/')), 18);
            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, Close, 24, "icon_back");
        }

        public override void OnShow()
        {
            nameField.text = SaveData.Current.playerName;
            Refresh();
        }

        public override void OnHide()
        {
            KeyboardMotionSource.Blocked = false;
            SaveData.Save();
        }

        public override void Tick()
        {
            KeyboardMotionSource.Blocked = nameField.isFocused;
        }

        public override bool OnBack()
        {
            Close();
            return true;
        }

        void Close()
        {
            App.Go(App.Menu);
        }

        void Change(int index, int delta)
        {
            GameSettings s = SaveData.Current.settings;
            switch (index)
            {
                case 0: s.music = Mathf.Clamp01(Mathf.Round((s.music + delta * 0.1f) * 10f) / 10f); break;
                case 1: s.sfx = Mathf.Clamp01(Mathf.Round((s.sfx + delta * 0.1f) * 10f) / 10f); break;
                case 2: s.inputMode = (s.inputMode + delta + 3) % 3; break;
                case 3: s.strictness = Mathf.Clamp(s.strictness + delta, 0, 2); break;
                case 4: s.assist = !s.assist; break;
                case 5: s.aiPanel = !s.aiPanel; break;
                case 6: s.autoCamera = !s.autoCamera; break;
                case 7:
                    s.cameraIndex = Mathf.Clamp(s.cameraIndex + delta, 0, 5);
                    App.Hub.Link.StopServer();
                    break;
                case 8: Screen.fullScreen = !Screen.fullScreen; break;
            }
            App.ApplySettings();
            Refresh();
        }

        void Refresh()
        {
            GameSettings s = SaveData.Current.settings;
            values[0].text = Mathf.RoundToInt(s.music * 100f) + "%";
            values[1].text = Mathf.RoundToInt(s.sfx * 100f) + "%";
            values[2].text = s.inputMode == 0 ? "КЛАВИАТУРА" : (s.inputMode == 1 ? "КАМЕРА" : "КАМЕРА + КЛАВИШИ");
            values[3].text = s.strictness == 0 ? "МЯГКО" : (s.strictness == 1 ? "ОБЫЧНО" : "СТРОГО");
            values[4].text = s.assist ? "ВКЛ" : "ВЫКЛ";
            values[5].text = s.aiPanel ? "ВКЛ" : "ВЫКЛ";
            values[6].text = s.autoCamera ? "ВКЛ" : "ВЫКЛ";
            values[7].text = s.cameraIndex.ToString();
            values[8].text = Screen.fullScreen ? "ВКЛ" : "ВЫКЛ";
        }

        void AskReset()
        {
            App.Modal.Show("СБРОСИТЬ ПРОГРЕСС?", "Будут удалены уровни, монеты, покупки,\nрекорды и достижения.\nНастройки сохранятся.",
                           "СБРОСИТЬ", () =>
                           {
                               SaveData.ResetProgress();
                               App.Player.SetSkin(SaveData.Current.skin);
                               App.Toasts.Push("ГОТОВО", "Прогресс сброшен", "icon_check");
                           }, "ОТМЕНА", null);
        }
    }
}
