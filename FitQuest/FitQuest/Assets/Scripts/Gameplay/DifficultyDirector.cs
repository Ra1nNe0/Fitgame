using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Адаптивная сложность (интеллектуальная механика №2).
    /// Смотрит на последние препятствия: если игрок проходит их уверенно и чисто,
    /// игра ускоряется и чаще даёт связки; если часто ошибается — замедляется.
    /// </summary>
    public class DifficultyDirector
    {
        const int WindowSize = 5;
        const float MinMultiplier = 0.75f;
        const float MaxMultiplier = 1.35f;

        // Для каждого препятствия: качество движения при успехе или 0 при ошибке.
        readonly Queue<float> results = new Queue<float>();

        /// <summary>Множитель скорости: 1 — как задумано в уровне.</summary>
        public float Multiplier { get; private set; }
        /// <summary>Сколько раз сложность менялась за уровень (для отчёта).</summary>
        public int Adjustments { get; private set; }
        /// <summary>Последнее решение: доля успехов и среднее качество в окне, текст решения.</summary>
        public float LastSuccessRate { get; private set; }
        public float LastQuality { get; private set; }
        public string LastDecision { get; private set; }
        /// <summary>Сколько препятствий уже набрано в текущее окно.</summary>
        public int WindowFill { get { return results.Count; } }
        public const int Window = WindowSize;

        public DifficultyDirector()
        {
            Multiplier = 1f;
            LastDecision = "ЖДУ ДАННЫЕ";
        }

        public void Reset()
        {
            results.Clear();
            Multiplier = 1f;
            Adjustments = 0;
            LastDecision = "ЖДУ ДАННЫЕ";
        }

        /// <summary>Сообщить результат очередного препятствия.</summary>
        public void Report(bool success, float quality)
        {
            results.Enqueue(success ? Mathf.Max(quality, 0.01f) : 0f);
            if (results.Count < WindowSize) return;

            int hits = 0;
            float qualitySum = 0f;
            foreach (float r in results)
            {
                if (r > 0f)
                {
                    hits++;
                    qualitySum += r;
                }
            }

            float successRate = (float)hits / WindowSize;
            float averageQuality = hits > 0 ? qualitySum / hits : 0f;
            float before = Multiplier;

            LastSuccessRate = successRate;
            LastQuality = averageQuality;
            if (successRate >= 0.8f && averageQuality >= 0.7f) { Multiplier += 0.1f; LastDecision = "БЫСТРЕЕ"; }
            else if (successRate <= 0.4f) { Multiplier -= 0.15f; LastDecision = "МЕДЛЕННЕЕ"; }
            else LastDecision = "ТЕМП ТОТ ЖЕ";

            Multiplier = Mathf.Clamp(Multiplier, MinMultiplier, MaxMultiplier);
            if (!Mathf.Approximately(before, Multiplier)) Adjustments++;
            results.Clear(); // следующее решение — по следующим пяти препятствиям
        }

        /// <summary>Шанс связки из двух препятствий с учётом формы игрока.</summary>
        public float ScaleComboChance(float baseChance)
        {
            float factor = Mathf.InverseLerp(MinMultiplier, MaxMultiplier, Multiplier) * 1.6f + 0.2f;
            return Mathf.Clamp01(baseChance * factor);
        }
    }
}
