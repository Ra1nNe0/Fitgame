using System;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Приложение: экраны и переходы между ними, пауза, реклама, демо-оплата, запуск камеры.
    /// Забегом управляет GameManager, App только реагирует на его итоги. Ритм-баттл целиком живёт в RhythmScreen.
    /// </summary>
    public class App : MonoBehaviour
    {
        public GameManager Game { get; private set; }
        public MotionHub Hub { get; private set; }
        public PlayerController Player { get; private set; }
        public Parallax Background { get; private set; }
        public Hud Hud { get; private set; }
        public Modal Modal { get; private set; }
        public Toasts Toasts { get; private set; }

        public TitleScreen Title { get; private set; }
        public MainMenuScreen Menu { get; private set; }
        public WorldMapScreen Map { get; private set; }
        public BriefingScreen Briefing { get; private set; }
        public ShopScreen ShopScreen { get; private set; }
        public RecordsScreen Records { get; private set; }
        public SettingsScreen SettingsScreen { get; private set; }
        public CameraScreen CameraSetup { get; private set; }
        public AiTestScreen AiTest { get; private set; }
        public TutorialScreen Tutorial { get; private set; }
        public PauseScreen PauseMenu { get; private set; }
        public ResultsScreen Results { get; private set; }
        public GameOverScreen GameOver { get; private set; }
        public AdScreen Ad { get; private set; }
        public RhythmSelectScreen RhythmSelect { get; private set; }
        public RhythmScreen Rhythm { get; private set; }
        public CutsceneScreen Cutscene { get; private set; }

        CanvasScaler scaler;
        UiScreen current;
        RunRequest lastRun;

        public void Init(GameManager game, MotionHub hub, PlayerController player, Parallax background,
                         ObstacleSpawner spawner, Transform cam, Canvas canvas, CanvasScaler canvasScaler)
        {
            Game = game;
            Hub = hub;
            Player = player;
            Background = background;
            scaler = canvasScaler;

            Hud = new Hud(canvas.transform, hub);
            game.Init(player, spawner, background, Hud, cam, hub);
            game.Finished += OnRunFinished;
            game.Failed += () => Go(GameOver);

            RectTransform screens = Ui.Stretch(canvas.transform, "Screens");
            Title = Make(new TitleScreen(), screens);
            Menu = Make(new MainMenuScreen(), screens);
            Map = Make(new WorldMapScreen(), screens);
            Briefing = Make(new BriefingScreen(), screens);
            ShopScreen = Make(new ShopScreen(), screens);
            Records = Make(new RecordsScreen(), screens);
            SettingsScreen = Make(new SettingsScreen(), screens);
            CameraSetup = Make(new CameraScreen(), screens);
            AiTest = Make(new AiTestScreen(), screens);
            Tutorial = Make(new TutorialScreen(), screens);
            PauseMenu = Make(new PauseScreen(), screens);
            Results = Make(new ResultsScreen(), screens);
            GameOver = Make(new GameOverScreen(), screens);
            Ad = Make(new AdScreen(), screens);
            RhythmSelect = Make(new RhythmSelectScreen(), screens);
            Rhythm = Make(new RhythmScreen(), screens);
            Cutscene = Make(new CutsceneScreen(), screens);

            Modal = new Modal(canvas.transform);
            Toasts = new Toasts(canvas.transform);   // уведомления поверх всего, даже окон

            hub.MotionDetected += e =>
            {
                if (current != null && !Modal.Visible) current.OnMotion(e);
            };
            Achievements.Unlocked += a => { Toasts.Push("ДОСТИЖЕНИЕ: " + a.Title, a.Description + "  +" + Achievements.Reward, "icon_" + a.Icon); Sfx.Play("achieve"); };

            ApplySettings();
            Background.SetWorld(Levels.Worlds[Levels.All[SaveData.Current.NextLevel].World].Art);
            EnsureCamera();
            Go(Title);
        }

        T Make<T>(T screen, Transform parent) where T : UiScreen
        {
            screen.Create(this, parent);
            return screen;
        }

        // ------------------------------------------------------------------ навигация

        /// <summary>Показать экран (null — никакого, идёт забег).</summary>
        public void Go(UiScreen screen)
        {
            UiScreen old = current;
            current = screen;
            if (old != null && old != screen) old.Hide();
            if (screen != null) screen.Show();
        }

        public void OpenBriefing(int level, bool marathon)
        {
            Briefing.Set(Mathf.Clamp(level, 0, Levels.All.Length - 1), marathon);
            Go(Briefing);
        }

        public void StartRun(RunRequest request)
        {
            lastRun = request;
            Time.timeScale = 1f;
            if (!request.Marathon && Story.Unseen("pre_" + request.Level) != null)
            {
                // сюжет перед уровнем — на фоне его мира, забег начнётся после сцены
                LevelDef level = Levels.All[request.Level];
                Background.SetWorld(Levels.Worlds[level.World].Art);
                Cutscene.Play("pre_" + request.Level, SceneTitle(request.Level), () => { Go(null); Game.Begin(request); });
                return;
            }
            Go(null);
            Game.Begin(request);
        }

        void OnRunFinished(RunResult result)
        {
            Time.timeScale = 1f;
            Results.Set(result);
            if (result.Won && !result.Marathon)
                Cutscene.Play("post_" + result.Level, SceneTitle(result.Level), () => Go(Results));
            else
                Go(Results);
        }

        static string SceneTitle(int level)
        {
            LevelDef def = Levels.All[level];
            return Levels.Worlds[def.World].Name + "  -  " + def.Name.ToUpper();
        }

        public void Pause()
        {
            if (!Game.Running || current != null) return;
            Time.timeScale = 0f;
            Go(PauseMenu);
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            Go(null);
        }

        public void RestartRun()
        {
            Time.timeScale = 1f;
            Game.Quit();
            if (lastRun != null) StartRun(lastRun);
            else Go(Map);
        }

        public void QuitRun()
        {
            Time.timeScale = 1f;
            Game.Quit();
            Go(Map);
        }

        public void Quit()
        {
            SaveData.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ сервисы

        public void ApplySettings()
        {
            GameSettings s = SaveData.Current.settings;
            Music.Volume = s.music;
            Sfx.Volume = s.sfx;
            SaveData.Save();
        }

        /// <summary>Запустить сервер камеры, если управление камерой включено.</summary>
        public void EnsureCamera()
        {
            GameSettings s = SaveData.Current.settings;
            if (!s.UseCamera || !s.autoCamera) return;
            if (Hub.Link.Connected || Hub.Link.Launched) return;
            Hub.Link.StartServer(s.cameraIndex);
        }

        /// <summary>Показать рекламу (демо). forCoins — награда монетами, иначе — продолжение забега.</summary>
        public void ShowAd(bool forCoins, Action done)
        {
            Ad.Begin(() =>
            {
                Shop.RewardAdWatched(forCoins);
                if (done != null) done();
            });
            Go(Ad);
        }

        /// <summary>Покупка за деньги: демо-оплата с подтверждением и выдачей товара.</summary>
        public void BuyForMoney(ShopItem item, Action done)
        {
            string period = item.Kind == ItemKind.Premium ? " В МЕСЯЦ" : "";
            Modal.Show("ДЕМО-ОПЛАТА",
                       item.Title + "\nЦЕНА: " + item.RubPrice + " РУБ." + period +
                       "\n\nЭто учебный прототип:\nнастоящая оплата не проводится, деньги не списываются.",
                       "ОПЛАТИТЬ (ДЕМО)", () =>
                       {
                           Modal.Busy("ОПЛАТА", "Обработка платежа (демо)...", 1.3f, () =>
                           {
                               Shop.CompleteDemoPayment(item);
                               Sfx.Play("buy");
                               if (item.Kind == ItemKind.Premium) Player.SetSkin(SaveData.Current.skin);
                               Modal.Show("ГОТОВО!", item.Title + "\nПокупка выдана.", "ОТЛИЧНО", done);
                           });
                       }, "ОТМЕНА", null);
        }

        // ------------------------------------------------------------------ кадр

        void Update()
        {
            // интерфейс всегда помещается: на узких экранах подстраиваемся по ширине
            scaler.matchWidthOrHeight = Screen.height > 0 && (float)Screen.width / Screen.height < 1.77f ? 0f : 1f;

            Hud.Tick();
            Toasts.Tick();

            if (Modal.Visible)
            {
                Modal.Tick();
                return;
            }

            if (current != null) current.Tick();

            if (GameKeys.Pause)
            {
                if (current == null) Pause();
                else if (!current.OnBack()) Go(Menu);
            }

            // меню на фоне бегущего героя
            if (!Game.Running)
            {
                Background.Scroll(Time.unscaledDeltaTime * 2f);
                Player.SetMode(PlayerController.Mode.Run);
            }
        }

        void OnApplicationQuit()
        {
            SaveData.Save();
        }
    }
}
