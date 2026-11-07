using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FitQuest.EditorTools
{
    /// <summary>
    /// После сборки игры под Windows кладёт упакованный сервер камеры
    /// (Tools/pose_build/dist/pose_server, собирается build_pose_server.bat)
    /// в &lt;Игра&gt;_Data/StreamingAssets/PoseServer/bin. Тогда игроку не нужен Python.
    /// В сам проект Unity эти ~260 МБ не кладём, чтобы не раздувать репозиторий и импорт.
    /// </summary>
    public class PoseServerBuildStep : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows) return;

            // Application.dataPath = .../FitQuest/FitQuest/Assets → корень репозитория на три уровня выше
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string source = Path.Combine(repo, "Tools", "pose_build", "dist", "pose_server");
            if (!File.Exists(Path.Combine(source, "pose_server.exe")))
            {
                Debug.LogWarning("FitQuest: нет " + source + "\\pose_server.exe — запустите Tools/pose_build/build_pose_server.bat. " +
                                 "Без него камера в сборке потребует Python у игрока.");
                return;
            }

            string exe = report.summary.outputPath;
            string data = Path.Combine(Path.GetDirectoryName(exe), Path.GetFileNameWithoutExtension(exe) + "_Data");
            string dest = Path.Combine(data, "StreamingAssets", "PoseServer", "bin");
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Copy(source, dest);
            Debug.Log("FitQuest: сервер камеры скопирован в " + dest);
        }

        static void Copy(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(from))
                Copy(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
    }
}
