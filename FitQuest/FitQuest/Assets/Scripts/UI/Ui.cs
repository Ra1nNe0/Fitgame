using System;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Конструктор интерфейса в едином пиксельном стиле: панели, кнопки, надписи, иконки.
    /// Все размеры — в единицах эталонного экрана 1920x1080; 1 пиксель графики = 6 единиц.
    /// Координаты: (0,0) — центр родителя, Y вверх.
    /// </summary>
    public static class Ui
    {
        public static readonly Color Ink = Hex("181425");
        public static readonly Color Night = Hex("262b44");
        public static readonly Color Steel = Hex("3a4466");
        public static readonly Color Slate = Hex("5a6988");
        public static readonly Color Gray = Hex("8b9bb4");
        public static readonly Color Silver = Hex("c0cbdc");
        public static readonly Color Gold = Hex("fee761");
        public static readonly Color Amber = Hex("feae34");
        public static readonly Color Orange = Hex("f77622");
        public static readonly Color Red = Hex("e43b44");
        public static readonly Color Green = Hex("63c74d");
        public static readonly Color Forest = Hex("3e8948");
        public static readonly Color Blue = Hex("0099db");
        public static readonly Color Cyan = Hex("2ce8f5");
        public static readonly Color Magenta = Hex("b55088");
        public static readonly Color Sand = Hex("ead4aa");

        public static Font Font { get; private set; }

        static Sprite panel, panelLight, button, buttonPressed;

        public static Color Hex(string hex)
        {
            int v = Convert.ToInt32(hex, 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        }

        public static void Init()
        {
            Font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = Art.Ui("ui_panel", 4);
            panelLight = Art.Ui("ui_panel_light", 4);
            button = Art.Ui("ui_button", 4);
            buttonPressed = Art.Ui("ui_button_pressed", 4);
        }

        // ------------------------------------------------------------------ разметка

        /// <summary>Пустой узел. anchor — точка привязки к родителю (0..1), по умолчанию центр.</summary>
        public static RectTransform Node(Transform parent, string name, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
            rect.anchorMin = a;
            rect.anchorMax = a;
            rect.pivot = a;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Узел на всю площадь родителя.</summary>
        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void Clear(Transform parent)
        {
            // Destroy срабатывает только в конце кадра, поэтому сначала отцепляем детей:
            // новые элементы, созданные в этом же кадре, не должны путаться со старыми.
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        // ------------------------------------------------------------------ элементы

        public static Image Panel(Transform parent, Vector2 position, Vector2 size, bool light = false, Vector2? anchor = null)
        {
            RectTransform rect = Node(parent, "Panel", position, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = light ? panelLight : panel;
            image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>Прямоугольник сплошного цвета.</summary>
        public static Image Box(Transform parent, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
        {
            RectTransform rect = Node(parent, "Box", position, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Затемнение на весь экран, которое заодно не пропускает клики к элементам позади.</summary>
        public static Image Dim(Transform parent, float alpha = 0.7f)
        {
            RectTransform rect = Node(parent, "Dim", Vector2.zero, new Vector2(6000f, 4000f));
            rect.SetAsFirstSibling();
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            return image;
        }

        public static Text Label(Transform parent, string text, int size, Color color, Vector2 position, Vector2 box,
                                 TextAnchor align = TextAnchor.MiddleCenter, Vector2? anchor = null)
        {
            RectTransform rect = Node(parent, "Text", position, box, anchor);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.3f;
            label.raycastTarget = false;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Ink;
            float d = Mathf.Max(2f, size / 8f);
            shadow.effectDistance = new Vector2(d, -d);
            return label;
        }

        public static Image Icon(Transform parent, string sprite, Vector2 position, float size, Vector2? anchor = null)
        {
            RectTransform rect = Node(parent, "Icon", position, new Vector2(size, size), anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art.Ui(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Картинка в родном размере × масштаб (по умолчанию 6 — как вся графика).</summary>
        public static Image Picture(Transform parent, string sprite, Vector2 position, float scale = 6f, Vector2? anchor = null)
        {
            Sprite s = Art.Ui(sprite);
            Vector2 size = new Vector2(s.rect.width, s.rect.height) * scale / Art.Density(s);
            RectTransform rect = Node(parent, "Picture", position, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = s;
            image.raycastTarget = false;
            return image;
        }

        public static Button Button(Transform parent, string text, Vector2 position, Vector2 size, Color color, Action onClick,
                                    int fontSize = 24, string icon = null, Vector2? anchor = null)
        {
            RectTransform rect = Node(parent, "Button", position, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = button;
            image.type = Image.Type.Sliced;
            image.color = color;

            var b = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.transition = Selectable.Transition.SpriteSwap;
            var states = new SpriteState { pressedSprite = buttonPressed, highlightedSprite = button, selectedSprite = button };
            b.spriteState = states;
            b.onClick.AddListener(() =>
            {
                Sfx.Play("click", 0.5f);
                if (onClick != null) onClick();
            });
            rect.gameObject.AddComponent<UiHover>();

            float textX = 0f;
            if (icon != null)
            {
                float iconSize = Mathf.Min(size.y - 30f, 48f);
                Icon(rect, icon, new Vector2(-size.x / 2f + 24f + iconSize / 2f, 3f), iconSize);
                textX = iconSize / 2f + 6f;
            }
            Text label = Label(rect, text, fontSize, ButtonTextColor(color), new Vector2(textX, 3f), size - new Vector2(30f, 0f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return b;
        }

        static Color ButtonTextColor(Color background)
        {
            float lum = background.r * 0.3f + background.g * 0.59f + background.b * 0.11f;
            return lum > 0.72f ? Ink : Color.white;
        }

        public static void SetButtonText(UnityEngine.UI.Button b, string text)
        {
            Text label = b.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        public static void SetButtonColor(UnityEngine.UI.Button b, Color color)
        {
            b.GetComponent<Image>().color = color;
            Text label = b.GetComponentInChildren<Text>();
            if (label != null) label.color = ButtonTextColor(color);
        }

        public static void SetEnabled(UnityEngine.UI.Button b, bool enabled)
        {
            b.interactable = enabled;
            var group = b.GetComponent<CanvasGroup>();
            if (group == null) group = b.gameObject.AddComponent<CanvasGroup>();
            group.alpha = enabled ? 1f : 0.45f;
        }

        /// <summary>Горизонтальная шкала 0..1. Возвращает заливку, чтобы менять ширину.</summary>
        public static RectTransform Bar(Transform parent, Vector2 position, Vector2 size, Color color, out Image fill, Vector2? anchor = null)
        {
            Image back = Box(parent, position, size + new Vector2(12f, 12f), Ink, anchor);
            RectTransform holder = Node(back.rectTransform, "Fill", new Vector2(6f, 0f), size, new Vector2(0f, 0.5f));
            fill = holder.gameObject.AddComponent<Image>();
            fill.color = color;
            fill.raycastTarget = false;
            return holder;
        }

        public static void SetBar(RectTransform fill, float width, float value)
        {
            fill.sizeDelta = new Vector2(width * Mathf.Clamp01(value), fill.sizeDelta.y);
        }

        /// <summary>Анимированная картинка (например, Пульс или герой в меню).</summary>
        public static UiImageAnimator Animated(Transform parent, Sprite[] frames, float fps, Vector2 position, float scale, Vector2? anchor = null)
        {
            Vector2 size = new Vector2(frames[0].rect.width, frames[0].rect.height) * scale / Art.Density(frames[0]);
            RectTransform rect = Node(parent, "Animated", position, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = frames[0];
            image.raycastTarget = false;
            var animator = rect.gameObject.AddComponent<UiImageAnimator>();
            animator.Play(frames, fps);
            return animator;
        }

        public static Sprite[] UiFrames(string baseName, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Art.Ui(baseName + "_" + i);
            return frames;
        }

        public static string Plural(int n, string one, string few, string many)
        {
            int m10 = n % 10, m100 = n % 100;
            if (m10 == 1 && m100 != 11) return one;
            if (m10 >= 2 && m10 <= 4 && (m100 < 10 || m100 >= 20)) return few;
            return many;
        }
    }
}
