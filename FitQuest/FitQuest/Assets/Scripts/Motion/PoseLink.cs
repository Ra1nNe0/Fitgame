using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FitQuest
{
    /// <summary>
    /// Связь игры с сервером распознавания позы (StreamingAssets/PoseServer/pose_server.py).
    /// • запускает сервер (start_pose_server.bat создаёт окружение Python при первом запуске);
    /// • принимает кадры по UDP в отдельном потоке, чтобы игра не тормозила;
    /// • раз в секунду шлёт серверу «ping»: если игра закроется, сервер сам завершится;
    /// • показывает картинку с камеры (Preview) для экрана настройки и мини-окна в игре.
    /// </summary>
    public class PoseLink : MonoBehaviour
    {
        public const int GamePort = 50510;
        public const int CommandPort = 50511;

        struct Packet
        {
            public string Text;
            public double Time;
        }

        public event Action<PoseFrame, double> FrameReceived;

        /// <summary>Последний кадр (с позой или без).</summary>
        public PoseFrame Latest { get; private set; }
        public Texture2D Preview { get; private set; }
        public string ServerStatus { get; private set; }
        public float Fps { get { return Latest != null ? Latest.Fps : 0f; } }
        public bool Launched { get; private set; }
        public int PacketsReceived { get; private set; }

        /// <summary>Сервер присылает кадры прямо сейчас.</summary>
        public bool Connected
        {
            get { return Time.realtimeSinceStartup - lastPacketAt < 1.5f; }
        }

        readonly Queue<Packet> queue = new Queue<Packet>();
        readonly object gate = new object();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly PoseFrame parsed = new PoseFrame();

        UdpClient receiver;
        UdpClient sender;
        Thread thread;
        volatile bool running;
        float lastPacketAt = -100f;
        float pingTimer;
        string bindError;

        void Awake()
        {
            Latest = new PoseFrame();
            ServerStatus = "Сервер камеры не запущен";
            sender = new UdpClient();
            try
            {
                receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, GamePort));
                receiver.Client.ReceiveTimeout = 500;
                running = true;
                thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "FitQuest PoseLink" };
                thread.Start();
            }
            catch (Exception e)
            {
                bindError = "Порт " + GamePort + " занят другой программой";
                Debug.LogWarning("FitQuest: " + bindError + ": " + e.Message);
            }
        }

        void ReceiveLoop()
        {
            var any = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] data = receiver.Receive(ref any);
                    var packet = new Packet { Text = Encoding.UTF8.GetString(data), Time = clock.Elapsed.TotalSeconds };
                    lock (gate)
                    {
                        queue.Enqueue(packet);
                        while (queue.Count > 32) queue.Dequeue();   // игра зависла — старые кадры не нужны
                    }
                }
                catch (SocketException)
                {
                    // таймаут ожидания — просто проверяем, не пора ли выходить
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("FitQuest PoseLink: " + e.Message);
                }
            }
        }

        void Update()
        {
            pingTimer -= Time.unscaledDeltaTime;
            if (pingTimer <= 0f)
            {
                pingTimer = 1f;
                Send("ping");
            }

            string previewBase64 = null;
            while (true)
            {
                Packet packet;
                lock (gate)
                {
                    if (queue.Count == 0) break;
                    packet = queue.Dequeue();
                }

                if (!PosePacket.TryParse(packet.Text, parsed)) continue;
                if (parsed.Status != null)
                {
                    ServerStatus = parsed.Status;
                    continue;
                }

                PacketsReceived++;
                lastPacketAt = Time.realtimeSinceStartup;
                if (parsed.PreviewBase64 != null) previewBase64 = parsed.PreviewBase64;
                CopyFrame(parsed, Latest);
                if (FrameReceived != null) FrameReceived(Latest, packet.Time);
            }

            if (previewBase64 != null) DecodePreview(previewBase64);
            if (Connected) ServerStatus = "Камера работает";
            else if (bindError != null) ServerStatus = bindError;
        }

        static void CopyFrame(PoseFrame from, PoseFrame to)
        {
            to.Sequence = from.Sequence;
            to.Fps = from.Fps;
            to.Width = from.Width;
            to.Height = from.Height;
            to.HasPose = from.HasPose;
            Array.Copy(from.Image, to.Image, from.Image.Length);
            Array.Copy(from.World, to.World, from.World.Length);
        }

        void DecodePreview(string base64)
        {
            try
            {
                byte[] jpeg = Convert.FromBase64String(base64);
                if (Preview == null)
                {
                    Preview = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    Preview.filterMode = FilterMode.Bilinear;
                }
                Preview.LoadImage(jpeg);
            }
            catch (Exception)
            {
                // испорченная картинка — пропускаем кадр
            }
        }

        void Send(string text)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                sender.Send(data, data.Length, "127.0.0.1", CommandPort);
            }
            catch (Exception)
            {
                // сервер не запущен — это нормально
            }
        }

        /// <summary>Запустить сервер камеры. Возвращает текст ошибки или null.</summary>
        public string StartServer(int cameraIndex)
        {
            if (Connected) return null;
            string folder = Path.Combine(Application.streamingAssetsPath, "PoseServer");
            try
            {
                var info = new ProcessStartInfo { UseShellExecute = true, WorkingDirectory = folder };
                string script = "\"" + Path.Combine(folder, "pose_server.py") + "\" --camera " + cameraIndex;
                bool windows = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
                string venvPython = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "FitQuest", "venv", "Scripts", "pythonw.exe");
                // готовая сборка игры: сервер упакован в pose_server.exe вместе с Python и моделью,
                // игроку ничего ставить не нужно (папку bin кладёт Editor/PoseServerBuildStep.cs)
                string bundledExe = Path.Combine(folder, "bin", "pose_server.exe");
                if (windows && File.Exists(bundledExe))
                {
                    info.FileName = bundledExe;
                    info.Arguments = "--camera " + cameraIndex;
                    info.WorkingDirectory = Path.GetDirectoryName(bundledExe);
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    Process.Start(info);
                    Launched = true;
                    ServerStatus = "Запускаю сервер камеры (первый запуск до 20 секунд)...";
                    return null;
                }
                if (windows && File.Exists(venvPython))
                {
                    // окружение уже установлено — запускаем сервер скрыто, без окна консоли
                    info.FileName = venvPython;
                    info.Arguments = script;
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                }
                else if (windows)
                {
                    // первый запуск: батник ставит Python-пакеты, пусть пользователь видит прогресс
                    info.FileName = Path.Combine(folder, "start_pose_server.bat");
                    info.Arguments = "--camera " + cameraIndex;
                    info.WindowStyle = ProcessWindowStyle.Minimized;
                }
                else
                {
                    info.FileName = "python3";
                    info.Arguments = script;
                }

                if (!File.Exists(Path.Combine(folder, "pose_server.py")))
                    return "Не найден файл pose_server.py в StreamingAssets/PoseServer";

                Process.Start(info);
                Launched = true;
                ServerStatus = "Запускаю сервер камеры...";
                return null;
            }
            catch (Exception e)
            {
                ServerStatus = "Не удалось запустить сервер: " + e.Message;
                return ServerStatus;
            }
        }

        public void StopServer()
        {
            Send("quit");
            Launched = false;
            ServerStatus = "Сервер камеры остановлен";
        }

        void OnDestroy()
        {
            Send("quit");
            running = false;
            if (receiver != null) receiver.Close();
            if (sender != null) sender.Close();
        }
    }
}
