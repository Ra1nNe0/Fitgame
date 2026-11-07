using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Собирает игру из кода при нажатии Play: камеру, фон, героя, звук, распознавание движений,
    /// интерфейс и все экраны. В редакторе Unity настраивать ничего не нужно.
    /// </summary>
    public static class GameBootstrap
    {
        /// <summary>Половина высоты экрана: 180 пикселей при 16 пикселях на единицу.</summary>
        const float CameraSize = 5.625f;
        const float DespawnX = -13f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            SaveData.Load();

            Camera cam = SetupCamera();

            var background = new GameObject("Background").AddComponent<Parallax>();
            background.Build(-CameraSize, GameManager.GroundY);

            var player = new GameObject("Player").AddComponent<PlayerController>();
            player.Build(new Vector3(GameManager.PlayerX, GameManager.GroundY, 0f), SaveData.Current.skin);

            var game = new GameObject("Game");
            Sfx.Init(game);
            Music.Init(game);

            var spawner = game.AddComponent<ObstacleSpawner>();
            spawner.Setup(GameManager.PlayerX, GameManager.GroundY, DespawnX);

            // Движения: клавиатура + веб-камера (нейросеть MediaPipe через PoseLink)
            var link = game.AddComponent<PoseLink>();
            var keyboard = game.AddComponent<KeyboardMotionSource>();
            var cameraSource = game.AddComponent<CameraMotionSource>();
            cameraSource.Init(link);
            var hub = game.AddComponent<MotionHub>();
            hub.Init(keyboard, cameraSource, link);

            var manager = game.AddComponent<GameManager>();

            EnsureEventSystem();
            var ui = new GameObject("UI");
            var canvas = ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = ui.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            ui.AddComponent<GraphicRaycaster>();
            Ui.Init();

            var app = game.AddComponent<App>();
            app.Init(manager, hub, player, background, spawner, cam.transform, canvas, scaler);
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.094f, 0.078f, 0.145f);
            return cam;
        }
    }
}
