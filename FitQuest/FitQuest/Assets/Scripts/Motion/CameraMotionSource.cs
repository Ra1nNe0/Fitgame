using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Движения с веб-камеры: кадры от PoseLink → классификатор PoseMotionClassifier → события.
    /// </summary>
    public class CameraMotionSource : MonoBehaviour, IMotionSource
    {
        public event Action<MotionEvent> MotionDetected;

        public PoseMotionClassifier Classifier { get; private set; }
        public PoseLink Link { get; private set; }

        readonly List<MotionEvent> buffer = new List<MotionEvent>();

        public void Init(PoseLink link)
        {
            Classifier = new PoseMotionClassifier();
            Link = link;
            link.FrameReceived += OnFrame;
        }

        void OnFrame(PoseFrame frame, double time)
        {
            Classifier.Strictness = SaveData.Current.settings.StrictnessValue;
            buffer.Clear();
            Classifier.Feed(frame, time, buffer);
            foreach (MotionEvent e in buffer)
                if (MotionDetected != null) MotionDetected(e);
        }

        /// <summary>Камера подключена и игрок откалиброван.</summary>
        public bool Ready
        {
            get { return Link != null && Link.Connected && Classifier.State == TrackerState.Ready; }
        }
    }
}
