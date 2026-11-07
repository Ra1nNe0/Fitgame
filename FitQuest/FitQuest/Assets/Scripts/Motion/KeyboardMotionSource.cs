using System;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Движения с клавиатуры — для отладки и как запасной вариант на демо.
    /// Shift имитирует неточное движение (качество 0.55),
    /// Ctrl — слишком слабое (качество 0.2, игра его не засчитает).
    /// </summary>
    public class KeyboardMotionSource : MonoBehaviour, IMotionSource
    {
        public event Action<MotionEvent> MotionDetected;

        /// <summary>Игнорировать клавиши (например, пока игрок вводит имя).</summary>
        public static bool Blocked;

        void Update()
        {
            if (Blocked) return;

            MotionType type = MotionType.None;
            if (GameKeys.Jump) type = MotionType.Jump;
            else if (GameKeys.Squat) type = MotionType.Squat;
            else if (GameKeys.Punch) type = MotionType.Punch;
            else if (GameKeys.HandsUp) type = MotionType.HandsUp;

            if (type == MotionType.None) return;

            float quality = 1f;
            if (GameKeys.WeakHeld) quality = 0.2f;
            else if (GameKeys.SloppyHeld) quality = 0.55f;

            if (MotionDetected != null) MotionDetected(new MotionEvent(type, quality));
        }
    }
}
