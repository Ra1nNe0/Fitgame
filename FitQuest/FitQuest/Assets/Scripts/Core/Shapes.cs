using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Простые цветные прямоугольники и общий материал для спрайтов.
    /// Используется для полосок интерфейса и как запасной вариант, если картинка не найдена.
    /// </summary>
    public static class Shapes
    {
        static Sprite square;
        static Material unlit;

        /// <summary>Белый квадрат 1x1 игровую единицу.</summary>
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                           new Vector2(0.5f, 0.5f), tex.width);
                }
                return square;
            }
        }

        /// <summary>Материал без освещения: спрайты видны, даже если в сцене нет 2D-света.</summary>
        public static Material UnlitMaterial
        {
            get
            {
                if (unlit == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader != null) unlit = new Material(shader);
                }
                return unlit;
            }
        }

        public static SpriteRenderer Box(string name, Transform parent, Color color,
                                         Vector2 size, Vector2 localPosition, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (UnlitMaterial != null) renderer.sharedMaterial = UnlitMaterial;
            return renderer;
        }
    }
}
