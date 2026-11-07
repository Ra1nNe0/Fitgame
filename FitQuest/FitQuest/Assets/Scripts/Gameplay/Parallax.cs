using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Фон из трёх слоёв: дальний, ближний и земля. Слои прокручиваются с разной скоростью —
    /// дальний медленнее, ближний быстрее. Так создаётся ощущение глубины (параллакс).
    /// </summary>
    public class Parallax : MonoBehaviour
    {
        class Layer
        {
            public SpriteRenderer[] Tiles = new SpriteRenderer[2];
            public float Factor;   // доля скорости земли
            public float Width;    // ширина одной картинки в игровых единицах
            public float Offset;
            public float Y;
        }

        readonly Layer[] layers = new Layer[3];
        Ambient ambient;

        public void Build(float cameraBottomY, float groundY)
        {
            layers[0] = MakeLayer("Far", 0.15f, cameraBottomY, -100);
            layers[1] = MakeLayer("Near", 0.5f, cameraBottomY, -90);
            layers[2] = MakeLayer("Ground", 1f, groundY - 4f, -50);
            ambient = gameObject.AddComponent<Ambient>();
        }

        Layer MakeLayer(string layerName, float factor, float y, int sortingOrder)
        {
            var layer = new Layer();
            layer.Factor = factor;
            layer.Y = y;
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(layerName + "_" + i);
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = sortingOrder;
                if (Shapes.UnlitMaterial != null) renderer.sharedMaterial = Shapes.UnlitMaterial;
                layer.Tiles[i] = renderer;
            }
            return layer;
        }

        /// <summary>Сменить мир: forest, cave, volcano или castle.</summary>
        public void SetWorld(string art)
        {
            string[] parts = { "far", "near", "ground" };
            for (int l = 0; l < layers.Length; l++)
            {
                Sprite sprite = Art.Get("bg_" + art + "_" + parts[l]);
                layers[l].Width = sprite.bounds.size.x;
                foreach (SpriteRenderer tile in layers[l].Tiles) tile.sprite = sprite;
            }
            if (ambient != null) ambient.SetWorld(art);
            Scroll(0f);
        }

        /// <summary>Прокрутить фон: distance — путь, который прошла земля.</summary>
        public void Scroll(float distance)
        {
            const float overlap = 1f / Art.PixelsPerUnit;   // картинки перекрываются на пиксель, чтобы не было щели
            if (ambient != null) ambient.Scroll(distance);

            foreach (Layer layer in layers)
            {
                if (layer == null || layer.Width <= 0f) continue;
                float period = layer.Width - overlap;
                layer.Offset = Mathf.Repeat(layer.Offset + distance * layer.Factor, period);
                float x = Art.Snap(-layer.Offset);
                layer.Tiles[0].transform.position = new Vector3(x, layer.Y, 0f);
                layer.Tiles[1].transform.position = new Vector3(x + period, layer.Y, 0f);
            }
        }
    }
}
