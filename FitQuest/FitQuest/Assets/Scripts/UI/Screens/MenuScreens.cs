using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>Заставка: логотип и «подпрыгни, чтобы начать».</summary>
    public class TitleScreen : UiScreen
    {
        Text press;
        float time;

        protected override void Build()
        {
            Ui.Picture(Root, "logo_0", new Vector2(0f, 250f), 6f);
            Ui.Label(Root, "ПРИКЛЮЧЕНИЕ В ДВИЖЕНИИ", 32, Color.white, new Vector2(0f, 90f), new Vector2(1600f, 50f));
            Ui.Animated(Root, Ui.UiFrames("pulse", 4), 2.5f, new Vector2(520f, -120f), 7f);
            press = Ui.Label(Root, "НАЖМИ ENTER ИЛИ ПОДПРЫГНИ", 32, Ui.Gold, new Vector2(0f, -170f), new Vector2(1600f, 50f));
            Ui.Label(Root, "Лорд Лень усыпил королевство. Разбуди его движением!", 20, Ui.Silver,
                     new Vector2(0f, -250f), new Vector2(1400f, 40f));
            Ui.Label(Root, "Учебный проект • Unity 6 + MediaPipe Pose", 16, Ui.Gray, new Vector2(0f, 40f),
                     new Vector2(1600f, 30f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f));
            var click = Ui.Stretch(Root, "Click").gameObject.AddComponent<Button>();
            click.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            click.onClick.AddListener(Next);
            click.transform.SetAsFirstSibling();
        }

        public override void OnShow()
        {
            Music.Play("menu");
        }

        public override void Tick()
        {
            time += Time.unscaledDeltaTime;
            press.enabled = (int)(time * 2.5f) % 3 != 2;
            if (GameKeys.Confirm) Next();
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (!motion.IsUpdate && motion.Type == MotionType.Jump && motion.Quality >= PlayerController.MinQuality) Next();
        }

        void Next()
        {
            Sfx.Play("start");
            App.Go(App.Menu);
        }
    }

    /// <summary>Главное меню.</summary>
    public class MainMenuScreen : UiScreen
    {
        Text coins, stars, cameraStatus, tip, playerName;
        Image premiumBadge;
        UiImageAnimator hero;
        Text skinName;
        Button marathon;
        float tipTime;
        int tipIndex;

        static readonly string[] Tips =
        {
            "Чем глубже присед, тем больше очков!",
            "Прыгай выше: качество движения видит нейросеть",
            "Включи AI-панель клавишей F1 во время игры",
            "Игра подстраивает темп под твою форму",
            "Встань в 2-3 метрах от камеры, чтобы было видно колени",
            "Щит из магазина спасает от одной ошибки",
            "Премиум удваивает монеты и открывает Марафон",
            "Ритм-баттл: руки в стороны, вверх и присед в такт!",
            "В ритм-баттле обе руки в стороны — это две стрелки сразу",
            "8 удачных движений подряд включают ФИВЕР: очки x2!",
            "Блок ? превращается вблизи: шипы, мышь или слизень",
            "Дождь сокровищ: монеты наверху, самоцветы внизу",
            "Стрелками у героя можно выбрать Короля или Рыцаря",
        };

        protected override void Build()
        {
            Ui.Picture(Root, "logo_0", new Vector2(-430f, 380f), 4f);

            string[] labels = { "ИГРАТЬ", "РИТМ-БАТТЛ", "МАРАФОН", "ОБУЧЕНИЕ", "МАГАЗИН", "РЕКОРДЫ", "КАМЕРА И AI", "НАСТРОЙКИ", "ВЫХОД" };
            string[] icons = { "icon_play", "icon_flame", "icon_crown", "icon_check", "icon_cart", "icon_trophy", "icon_camera", "icon_gear", "icon_back" };
            Color[] colors = { Ui.Green, Ui.Magenta, Ui.Gold, Ui.Blue, Ui.Amber, Ui.Amber, Ui.Blue, Ui.Slate, Ui.Slate };
            System.Action[] actions =
            {
                () => App.Go(App.Map),
                () => App.Go(App.RhythmSelect),
                OpenMarathon,
                () => App.Go(App.Tutorial),
                () => App.Go(App.ShopScreen),
                () => App.Go(App.Records),
                () => App.Go(App.CameraSetup),
                () => App.Go(App.SettingsScreen),
                () => App.Quit(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                Button b = Ui.Button(Root, labels[i], new Vector2(-430f, 262f - i * 88f), new Vector2(620f, 80f), colors[i], actions[i], 24, icons[i]);
                if (i == 2) marathon = b;
            }

            // карточка игрока
            RectTransform card = Ui.Panel(Root, new Vector2(470f, 120f), new Vector2(760f, 640f)).rectTransform;
            playerName = Ui.Label(card, "", 32, Color.white, new Vector2(0f, 260f), new Vector2(700f, 50f));
            premiumBadge = Ui.Icon(card, "icon_crown", new Vector2(320f, 260f), 64f);
            hero = Ui.Animated(card, Art.Hero("king", "idle", 2), 2f, new Vector2(-180f, 130f), 7f);   // низ картинки = ноги героя
            // выбор персонажа: стрелки по бокам героя
            Ui.Button(card, "<", new Vector2(-330f, 60f), new Vector2(64f, 80f), Ui.Steel, () => CycleSkin(-1), 24);
            Ui.Button(card, ">", new Vector2(-30f, 60f), new Vector2(64f, 80f), Ui.Steel, () => CycleSkin(1), 24);
            skinName = Ui.Label(card, "", 16, Ui.Cyan, new Vector2(-180f, -110f), new Vector2(340f, 30f));
            Ui.Icon(card, "coin_0", new Vector2(70f, 120f), 64f);
            coins = Ui.Label(card, "", 32, Ui.Gold, new Vector2(270f, 120f), new Vector2(300f, 50f), TextAnchor.MiddleLeft);
            Ui.Icon(card, "icon_star", new Vector2(70f, 30f), 64f);
            stars = Ui.Label(card, "", 32, Ui.Gold, new Vector2(270f, 30f), new Vector2(300f, 50f), TextAnchor.MiddleLeft);
            cameraStatus = Ui.Label(card, "", 16, Ui.Silver, new Vector2(0f, -200f), new Vector2(700f, 60f));

            // Пульс с советом
            Ui.Animated(Root, Ui.UiFrames("pulse", 4), 2.5f, new Vector2(170f, -330f), 5f);
            RectTransform bubble = Ui.Panel(Root, new Vector2(560f, -330f), new Vector2(620f, 150f), true).rectTransform;
            tip = Ui.Label(bubble, "", 16, Ui.Ink, Vector2.zero, new Vector2(560f, 120f));
            tip.GetComponent<Shadow>().enabled = false;
        }

        void CycleSkin(int dir)
        {
            Profile p = SaveData.Current;
            var skins = Shop.OwnedSkins();
            if (skins.Count == 0) return;
            int index = 0;
            for (int i = 0; i < skins.Count; i++)
                if (skins[i].Skin == p.skin) index = i;
            index = (index + dir + skins.Count) % skins.Count;
            p.skin = skins[index].Skin;
            SaveData.Save();
            App.Player.SetSkin(p.skin);
            ShowSkin();
        }

        void ShowSkin()
        {
            Profile p = SaveData.Current;
            hero.Play(Art.Hero(p.skin, "idle", 2), 2f);
            string title = p.skin;
            foreach (ShopItem item in Shop.OwnedSkins())
                if (item.Skin == p.skin) title = item.Title;
            skinName.text = "ПЕРСОНАЖ: " + title;
        }

        void OpenMarathon()
        {
            if (SaveData.Current.premium) App.OpenBriefing(0, true);
            else App.Modal.Show("МАРАФОН", "Бесконечный забег по всем мирам\nс таблицей рекордов.\n\nДоступен с FitQuest Premium.",
                                "В МАГАЗИН", () => { App.ShopScreen.SetTab(ItemKind.Premium); App.Go(App.ShopScreen); }, "НАЗАД", null);
        }

        public override void OnShow()
        {
            Profile p = SaveData.Current;
            playerName.text = p.playerName;
            premiumBadge.enabled = p.premium;
            coins.text = p.coins.ToString();
            stars.text = p.TotalStars + " / " + (Levels.All.Length * 3);
            ShowSkin();
            Ui.SetButtonText(marathon, p.premium ? "МАРАФОН" : "МАРАФОН (PREMIUM)");
            Music.Play("menu");
            App.EnsureCamera();
        }

        public override void Tick()
        {
            GameSettings s = SaveData.Current.settings;
            string mode = s.inputMode == 0 ? "КЛАВИАТУРА" : (s.inputMode == 1 ? "КАМЕРА" : "КАМЕРА + КЛАВИАТУРА");
            string cam = App.Hub.Link.Connected
                ? "КАМЕРА: " + Hud.TrackerName(App.Hub.Camera.Classifier.State)
                : (s.UseCamera ? "КАМЕРА: " + App.Hub.Link.ServerStatus.ToUpper() : "КАМЕРА ВЫКЛЮЧЕНА");
            cameraStatus.text = "УПРАВЛЕНИЕ: " + mode + "\n" + cam;

            tipTime -= Time.unscaledDeltaTime;
            if (tipTime <= 0f)
            {
                tipTime = 5f;
                tip.text = Tips[tipIndex++ % Tips.Length];
            }
            if (GameKeys.Confirm) App.Go(App.Map);
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (!motion.IsUpdate && motion.Type == MotionType.Jump && motion.Quality >= PlayerController.MinQuality) App.Go(App.Map);
        }

        public override bool OnBack()
        {
            App.Go(App.Title);
            return true;
        }
    }

    /// <summary>Карта: 4 мира по 3 уровня.</summary>
    public class WorldMapScreen : UiScreen
    {
        RectTransform content;
        Text totals;

        protected override void Build()
        {
            Ui.Dim(Root, 0.35f).raycastTarget = false;
            Ui.Label(Root, "КАРТА ПРИКЛЮЧЕНИЯ", 48, Ui.Gold, new Vector2(0f, 460f), new Vector2(1600f, 70f));
            totals = Ui.Label(Root, "", 24, Color.white, new Vector2(0f, 400f), new Vector2(1600f, 40f));
            content = Ui.Node(Root, "Worlds", new Vector2(0f, -20f), new Vector2(1840f, 740f));
            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, () => App.Go(App.Menu), 24, "icon_back");
            Ui.Button(Root, "ПРОДОЛЖИТЬ", new Vector2(640f, -470f), new Vector2(480f, 96f), Ui.Green,
                      () => App.OpenBriefing(SaveData.Current.NextLevel, false), 24, "icon_play");
        }

        public override void OnShow()
        {
            Ui.Clear(content);
            Profile p = SaveData.Current;
            totals.text = "ЗВЁЗДЫ: " + p.TotalStars + " / " + (Levels.All.Length * 3);

            for (int w = 0; w < Levels.Worlds.Length; w++)
            {
                float x = -690f + w * 460f;
                RectTransform card = Ui.Panel(content, new Vector2(x, 0f), new Vector2(430f, 740f)).rectTransform;
                Image thumb = Ui.Node(card, "Thumb", new Vector2(0f, 230f), new Vector2(384f, 240f)).gameObject.AddComponent<Image>();
                thumb.sprite = Art.UiCrop("bg_" + Levels.Worlds[w].Art + "_far", new Rect(96, 24, 160, 100));
                thumb.raycastTarget = false;
                Ui.Label(card, Levels.Worlds[w].Name, 16, Color.white, new Vector2(0f, 75f), new Vector2(400f, 50f));

                for (int i = 0; i < 3; i++)
                {
                    int index = w * 3 + i;
                    LevelDef def = Levels.All[index];
                    bool unlocked = p.IsUnlocked(index);
                    LevelRecord rec = p.Level(index);
                    float y = -20f - i * 112f;
                    Color color = !unlocked ? Ui.Steel : (def.IsBoss ? Ui.Red : (rec.completed ? Ui.Green : Ui.Amber));
                    string label = (index + 1) + ". " + (def.IsBoss ? "БОСС" : def.Name.ToUpper());
                    int captured = index;
                    Button b = Ui.Button(card, label, new Vector2(0f, y), new Vector2(390f, 100f), color,
                                         () => App.OpenBriefing(captured, false), 16, unlocked ? null : "icon_lock");
                    Ui.SetEnabled(b, unlocked);
                    if (unlocked)
                        for (int s = 0; s < 3; s++)
                            Ui.Icon(b.transform, s < rec.stars ? "icon_star" : "icon_star_empty", new Vector2(-40f + s * 40f, -34f), 36f);
                }
            }
            Music.Play("menu");
        }

        public override bool OnBack()
        {
            App.Go(App.Menu);
            return true;
        }

        public override void Tick()
        {
            if (GameKeys.Confirm) App.OpenBriefing(SaveData.Current.NextLevel, false);
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (!motion.IsUpdate && motion.Type == MotionType.Jump && motion.Quality >= PlayerController.MinQuality)
                App.OpenBriefing(SaveData.Current.NextLevel, false);
        }
    }

    /// <summary>Перед стартом: описание уровня, рекорд, выбор бустов.</summary>
    public class BriefingScreen : UiScreen
    {
        RectTransform content;
        int level;
        bool marathon;
        bool useShield, useMagnet;
        Button shieldButton, magnetButton;
        Text cameraNote;

        protected override void Build()
        {
            Ui.Dim(Root, 0.55f);
            content = Ui.Node(Root, "Content", Vector2.zero, new Vector2(1300f, 900f));
        }

        public void Set(int levelIndex, bool isMarathon)
        {
            level = levelIndex;
            marathon = isMarathon;
        }

        public override void OnShow()
        {
            Ui.Clear(content);
            Profile p = SaveData.Current;
            LevelDef def = marathon ? Levels.MarathonStage(0) : Levels.All[level];
            RectTransform panel = Ui.Panel(content, Vector2.zero, new Vector2(1300f, 900f)).rectTransform;

            string world = marathon ? "ВСЕ МИРЫ" : Levels.Worlds[def.World].Name;
            Ui.Label(panel, world, 24, Ui.Silver, new Vector2(0f, 380f), new Vector2(1200f, 40f));
            string title = marathon ? "МАРАФОН" : "УРОВЕНЬ " + (level + 1) + ": " + def.Name.ToUpper();
            Ui.Label(panel, title, 40, Ui.Gold, new Vector2(0f, 320f), new Vector2(1200f, 60f));
            Ui.Label(panel, def.Hint, 24, Color.white, new Vector2(0f, 220f), new Vector2(1200f, 100f));

            string record = marathon
                ? "РЕКОРД МАРАФОНА: " + p.stats.marathonBest + " ПРЕПЯТСТВИЙ"
                : "РЕКОРД: " + p.Level(level).bestScore + "   ЗВЁЗДЫ: " + p.Level(level).stars + "/3";
            Ui.Label(panel, record, 20, Ui.Silver, new Vector2(0f, 130f), new Vector2(1200f, 40f));

            // какие движения понадобятся
            string[] cues = { "cue_jump_0", "cue_squat_0", "cue_punch_0", "cue_hands_0" };
            for (int i = 0; i < 4; i++)
            {
                Ui.Icon(panel, cues[i], new Vector2(-330f + i * 220f, 40f), 64f);
                Ui.Label(panel, MotionNames.Ru((MotionType)(i + 1)), 16, Color.white, new Vector2(-330f + i * 220f, -20f), new Vector2(220f, 30f));
            }

            // бусты
            Ui.Label(panel, "БУСТЫ НА ЗАБЕГ", 20, Ui.Cyan, new Vector2(0f, -90f), new Vector2(1200f, 30f));
            useShield = false;
            useMagnet = false;
            shieldButton = Ui.Button(panel, "", new Vector2(-270f, -170f), new Vector2(500f, 96f), Ui.Steel, ToggleShield, 16, "icon_shield");
            magnetButton = Ui.Button(panel, "", new Vector2(270f, -170f), new Vector2(500f, 96f), Ui.Steel, ToggleMagnet, 16, "icon_magnet");
            RefreshBoosts();

            cameraNote = Ui.Label(panel, "", 16, Ui.Amber, new Vector2(0f, -255f), new Vector2(1200f, 30f));

            Ui.Button(panel, "НАЗАД", new Vector2(-300f, -360f), new Vector2(420f, 100f), Ui.Slate,
                      () => App.Go(marathon ? (UiScreen)App.Menu : App.Map), 24, "icon_back");
            Ui.Button(panel, "СТАРТ!", new Vector2(300f, -360f), new Vector2(420f, 100f), Ui.Green, Start, 32, "icon_play");
            Music.Play("menu");
            App.EnsureCamera();
        }

        void RefreshBoosts()
        {
            Profile p = SaveData.Current;
            Ui.SetButtonText(shieldButton, p.shields > 0 ? (useShield ? "ЩИТ: ВЗЯТ" : "ЩИТ (ЕСТЬ " + p.shields + ")") : "ЩИТ: КУПИТЬ");
            Ui.SetButtonText(magnetButton, p.magnets > 0 ? (useMagnet ? "МАГНИТ: ВЗЯТ" : "МАГНИТ (ЕСТЬ " + p.magnets + ")") : "МАГНИТ: КУПИТЬ");
            Ui.SetButtonColor(shieldButton, useShield ? Ui.Blue : Ui.Steel);
            Ui.SetButtonColor(magnetButton, useMagnet ? Ui.Red : Ui.Steel);
        }

        void ToggleShield()
        {
            if (SaveData.Current.shields <= 0) { OpenShop(); return; }
            useShield = !useShield;
            RefreshBoosts();
        }

        void ToggleMagnet()
        {
            if (SaveData.Current.magnets <= 0) { OpenShop(); return; }
            useMagnet = !useMagnet;
            RefreshBoosts();
        }

        void OpenShop()
        {
            App.ShopScreen.SetTab(ItemKind.Boost);
            App.ShopScreen.ReturnTo = this;
            App.Go(App.ShopScreen);
        }

        void Start()
        {
            App.StartRun(new RunRequest { Level = level, Marathon = marathon, UseShield = useShield, UseMagnet = useMagnet });
        }

        public override void Tick()
        {
            GameSettings s = SaveData.Current.settings;
            if (!s.UseCamera) cameraNote.text = "";
            else if (!App.Hub.Link.Connected) cameraNote.text = "КАМЕРА НЕ ПОДКЛЮЧЕНА: " + App.Hub.Link.ServerStatus.ToUpper();
            else if (App.Hub.Camera.Classifier.State != TrackerState.Ready) cameraNote.text = "КАМЕРА: " + App.Hub.Camera.Classifier.Hint.ToUpper();
            else cameraNote.text = "КАМЕРА ГОТОВА. ПОДПРЫГНИ, ЧТОБЫ НАЧАТЬ!";
            cameraNote.color = App.Hub.Camera.Ready ? Ui.Green : Ui.Amber;

            if (GameKeys.Confirm) Start();
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (!motion.IsUpdate && motion.Type == MotionType.Jump && motion.Quality >= PlayerController.MinQuality) Start();
        }

        public override bool OnBack()
        {
            App.Go(marathon ? (UiScreen)App.Menu : App.Map);
            return true;
        }
    }
}
