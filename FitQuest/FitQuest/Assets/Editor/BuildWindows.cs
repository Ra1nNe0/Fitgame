using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FitQuest.EditorTools
{
    /// <summary>
    /// Сборка игры для Windows: меню FitQuest → Собрать Windows, или из командной строки:
    /// Unity.exe -batchmode -quit -projectPath FitQuest/FitQuest -executeMethod FitQuest.EditorTools.BuildWindows.Build
    /// Результат — папка Builds/FitQuest_Windows рядом с проектом (в .gitignore).
    /// </summary>
    public static class BuildWindows
    {
        [MenuItem("FitQuest/Собрать Windows")]
        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "FitQuest_Windows", "FitQuest.exe"));
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("FitQuest: сборка " + report.summary.result + ", " + output);
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
