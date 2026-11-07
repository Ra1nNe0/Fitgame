using UnityEngine;

namespace FitQuest
{
    /// <summary>Покадровая анимация: по очереди показывает спрайты из массива.</summary>
    public class SpriteAnimator : MonoBehaviour
    {
        SpriteRenderer target;
        Sprite[] frames;
        float fps = 8f;
        bool loop = true;
        float time;

        /// <summary>Анимировать даже на паузе (для экранов меню).</summary>
        public bool Unscaled;

        public void Init(SpriteRenderer renderer)
        {
            target = renderer;
        }

        /// <summary>Запустить анимацию. Если она уже играет, ничего не меняется.</summary>
        public void Play(Sprite[] newFrames, float newFps, bool newLoop = true)
        {
            if (frames == newFrames && loop == newLoop) return;
            frames = newFrames;
            fps = newFps;
            loop = newLoop;
            time = 0f;
            Apply();
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            time += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            int index = (int)(time * fps);
            if (loop) index %= frames.Length;
            else if (index >= frames.Length) index = frames.Length - 1;
            target.sprite = frames[index];
        }
    }
}
