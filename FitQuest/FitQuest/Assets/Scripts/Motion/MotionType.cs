using System;

namespace FitQuest
{
    /// <summary>Движения, которые понимает игра.</summary>
    public enum MotionType
    {
        None,
        Jump,     // прыжок
        Squat,    // приседание
        Punch,    // удар рукой
        HandsUp   // руки вверх
    }

    /// <summary>
    /// Одно распознанное движение. Quality — насколько правильно оно выполнено:
    /// 0 = совсем плохо, 1 = идеально. Ниже 0.35 игра движение не засчитывает.
    /// IsUpdate = true: это не новое движение, а уточнение качества уже начатого
    /// (камера видит, что человек присел глубже, чем в момент распознавания).
    /// </summary>
    public struct MotionEvent
    {
        public readonly MotionType Type;
        public readonly float Quality;
        public readonly bool IsUpdate;

        public MotionEvent(MotionType type, float quality, bool isUpdate = false)
        {
            Type = type;
            Quality = Math.Max(0f, Math.Min(1f, quality));
            IsUpdate = isUpdate;
        }

        public override string ToString()
        {
            return Type + (IsUpdate ? "(upd)" : "") + " q=" + Quality.ToString("0.00");
        }
    }

    public static class MotionNames
    {
        public static string Ru(MotionType type)
        {
            switch (type)
            {
                case MotionType.Jump: return "ПРЫЖОК";
                case MotionType.Squat: return "ПРИСЕД";
                case MotionType.Punch: return "УДАР";
                case MotionType.HandsUp: return "РУКИ ВВЕРХ";
                default: return "-";
            }
        }
    }
}
