using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Загружает пиксельные картинки из папки Resources/Art и превращает их в спрайты.
    /// 16 пикселей картинки = 1 игровая единица. Точка привязки спрайта — низ по центру,
    /// поэтому объект, поставленный на уровень земли, стоит на ней ногами.
    /// </summary>
    public static class Art
    {
        public const float PixelsPerUnit = 16f;

        /// <summary>Сетка, к которой прилипают координаты: шаг — один пиксель HD-картинки.</summary>
        public const float SnapPixels = 32f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static HashSet<string> hd;

        /// <summary>
        /// Во сколько раз картинка детальнее обычной: 2 для HD-картинок (32 пикселя на единицу),
        /// 1 для остальных. Список HD-картинок — Resources/Art/hd_manifest.txt (его пишет gen_art.py).
        /// </summary>
        public static float Density(string name)
        {
            if (hd == null)
            {
                hd = new HashSet<string>();
                TextAsset manifest = Resources.Load<TextAsset>("Art/hd_manifest");
                if (manifest != null)
                    foreach (string line in manifest.text.Split('\n'))
                    {
                        string n = line.Trim();
                        if (n.Length > 0) hd.Add(n);
                    }
            }
            return hd.Contains(name) ? 2f : 1f;
        }

        /// <summary>Плотность спрайта, созданного этим классом (по имени его текстуры).</summary>
        public static float Density(Sprite sprite)
        {
            return sprite != null && sprite.texture != null ? Density(sprite.texture.name) : 1f;
        }

        public static Sprite Get(string name, bool centered = false)
        {
            string key = centered ? name + "#c" : name;
            Sprite sprite;
            if (cache.TryGetValue(key, out sprite) && sprite != null) return sprite;

            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            if (tex == null)
            {
                Debug.LogWarning("FitQuest: не найдена картинка Resources/Art/" + name);
                sprite = Shapes.Square;
            }
            else
            {
                tex.filterMode = FilterMode.Point;   // чёткие пиксели без размытия
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0.5f, centered ? 0.5f : 0f),
                                       PixelsPerUnit * Density(name), 0, SpriteMeshType.FullRect);
            }

            cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Спрайт для интерфейса: 1 пиксель картинки = 6 единиц экрана (при эталонном разрешении 1920x1080).
        /// border — ширина неизменяемой рамки в пикселях (для растягиваемых панелей и кнопок).
        /// </summary>
        public static Sprite Ui(string name, int border = 0)
        {
            string key = name + "#ui" + border;
            Sprite sprite;
            if (cache.TryGetValue(key, out sprite) && sprite != null) return sprite;

            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            if (tex == null)
            {
                Debug.LogWarning("FitQuest: не найдена картинка Resources/Art/" + name);
                return Shapes.Square;
            }
            tex.filterMode = FilterMode.Point;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                                   100f / 6f * Density(name), 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>Часть картинки как спрайт интерфейса (например, кусок фона для карточки мира).</summary>
        public static Sprite UiCrop(string name, Rect pixels)
        {
            string key = name + "#crop" + pixels;
            Sprite sprite;
            if (cache.TryGetValue(key, out sprite) && sprite != null) return sprite;
            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            if (tex == null) return Shapes.Square;
            tex.filterMode = FilterMode.Point;
            float d = Density(name);
            pixels = new Rect(pixels.x * d, pixels.y * d, pixels.width * d, pixels.height * d);
            sprite = Sprite.Create(tex, pixels, new Vector2(0.5f, 0.5f), 100f / 6f * d, 0, SpriteMeshType.FullRect);
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>Кадры героя в выбранном образе: hero_&lt;skin&gt;_&lt;anim&gt;_N.</summary>
        public static Sprite[] Hero(string skin, string anim, int count)
        {
            return Frames("hero_" + skin + "_" + anim, count);
        }

        /// <summary>Кадры анимации: name_0, name_1, ...</summary>
        public static Sprite[] Frames(string baseName, int count, bool centered = false)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Get(baseName + "_" + i, centered);
            return frames;
        }

        /// <summary>Создаёт объект со спрайтом.</summary>
        public static SpriteRenderer Spawn(string spriteName, Transform parent, Vector2 localPosition,
                                           int sortingOrder, bool centered = false)
        {
            var go = new GameObject(spriteName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Get(spriteName, centered);
            renderer.sortingOrder = sortingOrder;
            if (Shapes.UnlitMaterial != null) renderer.sharedMaterial = Shapes.UnlitMaterial;
            return renderer;
        }

        /// <summary>Округляет координату до целого пикселя, чтобы картинка не «дрожала».</summary>
        public static float Snap(float value)
        {
            return Mathf.Round(value * SnapPixels) / SnapPixels;
        }
    }
}
