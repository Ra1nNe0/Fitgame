using System;

namespace FitQuest
{
    /// <summary>
    /// Источник движений. Игре всё равно, откуда они приходят:
    /// с клавиатуры (для отладки) или с веб-камеры (распознавание позы).
    /// Благодаря этому камеру можно подключить, не меняя остальной код.
    /// </summary>
    public interface IMotionSource
    {
        event Action<MotionEvent> MotionDetected;
    }
}
