using System;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Единая точка входа движений для всей игры: объединяет клавиатуру и камеру
    /// с учётом настройки «Управление» и помнит, откуда пришло последнее движение.
    /// </summary>
    public class MotionHub : MonoBehaviour, IMotionSource
    {
        public event Action<MotionEvent> MotionDetected;

        public KeyboardMotionSource Keyboard { get; private set; }
        public CameraMotionSource Camera { get; private set; }
        public PoseLink Link { get; private set; }

        /// <summary>"камера" или "клавиатура".</summary>
        public string LastSource { get; private set; }

        public void Init(KeyboardMotionSource keyboard, CameraMotionSource camera, PoseLink link)
        {
            Keyboard = keyboard;
            Camera = camera;
            Link = link;
            LastSource = "клавиатура";
            keyboard.MotionDetected += e => Forward(e, false);
            camera.MotionDetected += e => Forward(e, true);
        }

        void Forward(MotionEvent e, bool fromCamera)
        {
            GameSettings s = SaveData.Current.settings;
            if (fromCamera ? !s.UseCamera : !s.UseKeyboard) return;
            if (!e.IsUpdate) LastSource = fromCamera ? "камера" : "клавиатура";
            if (MotionDetected != null) MotionDetected(e);
        }

        public bool CameraActive
        {
            get { return SaveData.Current.settings.UseCamera && Link != null && Link.Connected; }
        }
    }
}
