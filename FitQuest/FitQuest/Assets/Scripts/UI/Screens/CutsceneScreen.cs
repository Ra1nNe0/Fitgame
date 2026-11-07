using System;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Катсцена сюжета: окно диалога на нижнюю треть экрана с портретом говорящего и печатающимся текстом.
    /// Дальше — Enter, клик или любое движение; Esc или кнопка — пропустить сцену целиком.
    /// </summary>
    public class CutsceneScreen : UiScreen
    {
        const float BoxHeight = 380f;          // ~35% экрана 1920x1080
        const float CharsPerSecond = 48f;
        const float PortraitScale = 5f;        // портрет 64x64 -> 320 точек

        RectTransform box, portraitFrame, nameplate;
        Image portrait;
        Text nameText, bodyText, nextMark, sceneTitle;

        string sceneId;
        StoryLine[] lines;
        Action done;
        int index;
        string fullText;
        float shown;            // сколько символов уже напечатано
        float lineTime;         // время с начала реплики
        float lastAdvance;
        float typedAt;          // когда реплика допечаталась
        int lastBlip;
        bool left;
        string lastWho;

        protected override void Build()
        {
            Image dim = Ui.Dim(Root, 0.25f);
            var click = dim.gameObject.AddComponent<Button>();      // клик в любом месте — дальше
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(Advance);

            // кинематографичная полоса сверху
            Ui.Box(Root, Vector2.zero, new Vector2(4000f, 96f), Ui.Ink, new Vector2(0.5f, 1f));
            sceneTitle = Ui.Label(Root, "", 22, Ui.Gold, new Vector2(0f, -48f), new Vector2(1200f, 40f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
            sceneTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Ui.Label(Root, "ENTER / КЛИК / ДВИЖЕНИЕ - ДАЛЕЕ", 14, Ui.Gray, new Vector2(40f, -48f), new Vector2(700f, 30f), TextAnchor.MiddleLeft, new Vector2(0f, 1f))
              .rectTransform.pivot = new Vector2(0f, 0.5f);
            Ui.Button(Root, "ПРОПУСТИТЬ", new Vector2(-24f, -14f), new Vector2(340f, 68f), Ui.Slate, Finish, 18, null, new Vector2(1f, 1f));

            // окно диалога
            box = Ui.Panel(Root, new Vector2(0f, 24f), new Vector2(1840f, BoxHeight), false, new Vector2(0.5f, 0f)).rectTransform;

            portraitFrame = Ui.Node(box, "PortraitFrame", Vector2.zero, new Vector2(348f, 348f));
            Ui.Box(portraitFrame, Vector2.zero, new Vector2(348f, 348f), Ui.Gold);
            Ui.Box(portraitFrame, Vector2.zero, new Vector2(340f, 340f), Ui.Ink);
            portrait = Ui.Picture(portraitFrame, "portrait_king_normal", Vector2.zero, PortraitScale);

            nameplate = Ui.Panel(box, Vector2.zero, new Vector2(400f, 68f), true).rectTransform;
            nameText = Ui.Label(nameplate, "", 22, Ui.Gold, Vector2.zero, new Vector2(380f, 50f));

            bodyText = Ui.Label(box, "", 26, Color.white, Vector2.zero, new Vector2(1380f, 290f), TextAnchor.UpperLeft);
            bodyText.lineSpacing = 1.6f;
            nextMark = Ui.Label(box, ">>", 24, Ui.Gold, Vector2.zero, new Vector2(80f, 40f));
        }

        /// <summary>Показать сцену, если её ещё не видели; иначе сразу done.</summary>
        public void Play(string id, string title, Action onDone)
        {
            StoryLine[] scene = Story.Unseen(id);
            if (scene == null || scene.Length == 0)
            {
                if (onDone != null) onDone();
                return;
            }
            sceneId = id;
            lines = scene;
            done = onDone;
            sceneTitle.text = title;
            index = -1;
            lastWho = null;
            App.Go(this);
            NextLine();
        }

        void NextLine()
        {
            index++;
            if (index >= lines.Length) { Finish(); return; }

            StoryLine line = lines[index];
            left = Story.OnLeft(line.Who);

            // портрет слева у героев, справа у противников; текст — с другой стороны
            float side = left ? -1f : 1f;
            portraitFrame.anchoredPosition = new Vector2(side * 728f, 0f);
            nameplate.anchoredPosition = new Vector2(side * 728f, BoxHeight / 2f + 28f);
            bodyText.rectTransform.anchoredPosition = new Vector2(-side * 186f, -10f);
            nextMark.rectTransform.anchoredPosition = new Vector2(left ? 830f : 330f, -150f);

            Sprite s = Art.Ui(Story.PortraitSprite(line));
            portrait.sprite = s;
            portrait.rectTransform.sizeDelta = new Vector2(s.rect.width, s.rect.height) * PortraitScale / Art.Density(s);
            nameText.text = Story.SpeakerName(line.Who);
            nameText.color = left ? Ui.Gold : Ui.Red;

            fullText = Story.TextFor(line);
            shown = 0f;
            lastBlip = 0;
            bodyText.text = "";
            // новый собеседник въезжает сбоку, тот же — просто меняет лицо
            lineTime = line.Who != lastWho ? 0f : 1f;
            lastWho = line.Who;
        }

        void Advance()
        {
            if (lines == null || Time.unscaledTime - lastAdvance < 0.25f) return;
            lastAdvance = Time.unscaledTime;
            if (shown < fullText.Length)
            {
                shown = fullText.Length;        // сначала допечатать реплику
                bodyText.text = fullText;
                typedAt = Time.unscaledTime;
                return;
            }
            Sfx.Play("click", 0.4f);
            NextLine();
        }

        void Finish()
        {
            if (lines == null) return;
            Story.MarkSeen(sceneId);
            lines = null;
            Action next = done;
            done = null;
            if (next != null) next();
        }

        public override void Tick()
        {
            if (lines == null) return;
            float dt = Time.unscaledDeltaTime;
            lineTime += dt;

            if (shown < fullText.Length)
            {
                shown = Mathf.Min(fullText.Length, shown + CharsPerSecond * dt);
                if (shown >= fullText.Length) typedAt = Time.unscaledTime;
                int n = (int)shown;
                bodyText.text = fullText.Substring(0, n);
                if (n - lastBlip >= 3 && n > 0 && fullText[n - 1] != ' ')
                {
                    lastBlip = n;
                    Sfx.Play(left ? "blip" : "blip_lo", 0.25f);
                }
            }

            // въезд портрета и лёгкое «дыхание», пока персонаж говорит
            float slide = Mathf.Clamp01(lineTime / 0.18f);
            float x = (1f - slide) * (left ? -90f : 90f);
            float talk = shown < fullText.Length ? Mathf.Round(Mathf.Sin(Time.unscaledTime * 14f)) * 3f : 0f;
            portrait.rectTransform.anchoredPosition = new Vector2(x, talk);
            portrait.color = new Color(1f, 1f, 1f, slide);

            nextMark.enabled = shown >= fullText.Length && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.5f;

            if (GameKeys.Confirm || GameKeys.Jump) Advance();
        }

        public override bool OnBack()
        {
            Finish();
            return true;
        }

        public override void OnMotion(MotionEvent motion)
        {
            if (motion.IsUpdate || motion.Quality < 0.35f || motion.Type == MotionType.None) return;
            // камера видит и случайные движения: листаем только дочитанную реплику и не раньше чем через секунду
            if (lines == null || shown < fullText.Length || Time.unscaledTime - typedAt < 1f) return;
            Advance();
        }
    }
}
