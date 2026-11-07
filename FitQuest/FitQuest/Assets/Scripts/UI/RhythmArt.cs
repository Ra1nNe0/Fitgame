using System.Collections.Generic;
using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Пиксельная графика ритм-режима, нарисованная прямо в коде: стрелки, приёмники и значки поз.
    /// Белые пиксели окрашиваются цветом Image (у каждой дорожки свой), контур остаётся тёмным.
    /// Масштаб как у всего интерфейса: 1 пиксель = 6 единиц экрана.
    /// </summary>
    public static class RhythmArt
    {
        /// <summary>Цвета дорожек как в FNF: влево — фиолетовый, вниз — голубой, вверх — зелёный, вправо — красный.</summary>
        public static readonly Color[] LaneColors = { Ui.Hex("b55088"), Ui.Hex("2ce8f5"), Ui.Hex("63c74d"), Ui.Hex("e43b44") };

        /// <summary>Поворот стрелки «вверх» для каждой дорожки.</summary>
        public static readonly float[] LaneRotation = { 90f, 180f, 0f, -90f };

        static readonly string[] ArrowMap =
        {
            "................",
            ".......##.......",
            "......#ww#......",
            ".....#wwww#.....",
            "....#wwwwww#....",
            "...#wwwwwwww#...",
            "..#wwwwwwwwww#..",
            ".#wwwwwwwwwwww#.",
            ".####wwwwww####.",
            "....#wwwwww#....",
            "....#wwwwww#....",
            "....#wwwwww#....",
            "....#wwwwww#....",
            "....#wwwwww#....",
            "....########....",
            "................",
        };

        static readonly string[] PoseUp =
        {
            "................",
            "..w....ww....w..",
            "..w...wwww...w..",
            "...w..wwww..w...",
            "...w...ww...w...",
            "....wwwwwwww....",
            ".......ww.......",
            ".......ww.......",
            ".......ww.......",
            "......wwww......",
            "......w..w......",
            "......w..w......",
            ".....w....w.....",
            ".....w....w.....",
            "....ww....ww....",
            "................",
        };

        static readonly string[] PoseLeft =
        {
            "................",
            ".......ww.......",
            "......wwww......",
            "......wwww......",
            ".......ww.......",
            "wwwwwwwwww......",
            ".......ww.w.....",
            ".......ww..w....",
            ".......ww...w...",
            "......wwww......",
            "......w..w......",
            "......w..w......",
            ".....w....w.....",
            ".....w....w.....",
            "....ww....ww....",
            "................",
        };

        static readonly string[] PoseDown =
        {
            "................",
            "................",
            "................",
            "................",
            ".......ww.......",
            "......wwww......",
            "......wwww......",
            ".......ww.......",
            "...wwwwwwwwww...",
            ".......ww.......",
            ".......ww.......",
            "......wwww......",
            "....ww....ww....",
            "....w......w....",
            "....ww....ww....",
            "................",
        };

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>Нота: заливка окрашивается, контур тёмный.</summary>
        public static Sprite Arrow { get { return Get("arrow", ArrowMap, Ui.Ink, Color.white, new Color(0, 0, 0, 0)); } }

        /// <summary>Приёмник: светлый контур, полупрозрачная серединка.</summary>
        public static Sprite Receptor { get { return Get("receptor", ArrowMap, Color.white, new Color(1f, 1f, 1f, 0.18f), new Color(0, 0, 0, 0)); } }

        /// <summary>Значок позы для дорожки (человечек).</summary>
        public static Sprite Pose(int lane)
        {
            switch (lane)
            {
                case RhythmPoseTracker.Left: return Get("pose_left", PoseLeft, Color.white, Color.white, new Color(0, 0, 0, 0));
                case RhythmPoseTracker.Right: return Get("pose_right", Mirror(PoseLeft), Color.white, Color.white, new Color(0, 0, 0, 0));
                case RhythmPoseTracker.Up: return Get("pose_up", PoseUp, Color.white, Color.white, new Color(0, 0, 0, 0));
                default: return Get("pose_down", PoseDown, Color.white, Color.white, new Color(0, 0, 0, 0));
            }
        }

        static string[] Mirror(string[] map)
        {
            var result = new string[map.Length];
            for (int i = 0; i < map.Length; i++)
            {
                char[] row = map[i].ToCharArray();
                System.Array.Reverse(row);
                result[i] = new string(row);
            }
            return result;
        }

        /// <summary>'#' — контур, 'w' — заливка, '.' — пусто. Первая строка — верх картинки.</summary>
        static Sprite Get(string key, string[] map, Color outline, Color fill, Color empty)
        {
            Sprite sprite;
            if (cache.TryGetValue(key, out sprite) && sprite != null) return sprite;
            int h = map.Length, w = map[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = map[h - 1 - y][x];
                    pixels[y * w + x] = c == '#' ? outline : (c == 'w' ? fill : empty);
                }
            tex.SetPixels(pixels);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f / 6f, 0, SpriteMeshType.FullRect);
            cache[key] = sprite;
            return sprite;
        }
    }
}
