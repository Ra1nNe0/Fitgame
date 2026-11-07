using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Журнал забега в CSV (открывается в Excel): каждое препятствие — строка.
    /// Нужен для тестирования: видно, какое движение требовалось, какое распознано,
    /// с каким качеством и как менялась адаптивная сложность.
    /// </summary>
    public class RunLog
    {
        readonly StringBuilder text = new StringBuilder();
        readonly float startTime;

        public string SavedPath { get; private set; }

        public RunLog(string title)
        {
            startTime = Time.time;
            text.AppendLine("# FitQuest: " + title + "; " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            text.AppendLine("время;препятствие;нужно;выполнено;качество;результат;темп;жизни;ввод");
        }

        public void Obstacle(string kind, MotionType required, MotionType performed, float quality, bool success,
                             float tempo, int lives, string input)
        {
            text.Append((Time.time - startTime).ToString("0.00")).Append(';')
                .Append(kind).Append(';')
                .Append(MotionNames.Ru(required)).Append(';')
                .Append(MotionNames.Ru(performed)).Append(';')
                .Append(quality.ToString("0.00")).Append(';')
                .Append(success ? "успех" : "ошибка").Append(';')
                .Append(tempo.ToString("0.00")).Append(';')
                .Append(lives).Append(';')
                .AppendLine(input);
        }

        public void Note(string line)
        {
            text.AppendLine("# " + line);
        }

        public void Save()
        {
            SavedPath = WriteFile("logs", "run", text.ToString());
        }

        /// <summary>Записать текст в папку данных игры. Возвращает путь к файлу или null.</summary>
        public static string WriteFile(string folder, string prefix, string content)
        {
            try
            {
                string dir = Path.Combine(SaveData.Folder, folder);
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, prefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                File.WriteAllText(path, content, new UTF8Encoding(true));   // BOM — чтобы Excel понял кириллицу
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning("FitQuest: не удалось записать " + folder + ": " + e.Message);
                return null;
            }
        }
    }
}
