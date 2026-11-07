using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>Покадровая анимация для картинок интерфейса. Работает и на паузе.</summary>
    public class UiImageAnimator : MonoBehaviour
    {
        Image image;
        Sprite[] frames;
        float fps;
        float time;

        public void Play(Sprite[] newFrames, float newFps)
        {
            if (image == null) image = GetComponent<Image>();
            frames = newFrames;
            fps = newFps;
            time = 0f;
            if (frames != null && frames.Length > 0) image.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || image == null) return;
            time += Time.unscaledDeltaTime;
            image.sprite = frames[(int)(time * fps) % frames.Length];
        }
    }
}
