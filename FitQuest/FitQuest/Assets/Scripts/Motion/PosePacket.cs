using System;
using System.Globalization;

namespace FitQuest
{
    /// <summary>
    /// Один кадр от сервера распознавания позы (pose_server.py).
    /// Image — 33 точки на картинке: x, y (0..1 от размера кадра) и видимость (0..1).
    /// World — те же 33 точки в метрах относительно центра таза (ось Y вниз).
    /// </summary>
    public sealed class PoseFrame
    {
        public const int PointCount = 33;

        public int Sequence;
        public float Fps;
        public int Width;
        public int Height;
        public bool HasPose;
        public readonly float[] Image = new float[PointCount * 3];
        public readonly float[] World = new float[PointCount * 3];
        public string PreviewBase64;   // маленькая картинка с камеры (JPEG) или null
        public string Status;          // текстовое сообщение сервера или null

        public float Aspect
        {
            get { return Height > 0 ? (float)Width / Height : 4f / 3f; }
        }
    }

    /// <summary>Разбор текстового пакета "FQ1|..." в PoseFrame. Не зависит от Unity.</summary>
    public static class PosePacket
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Возвращает false, если строка не похожа на пакет FitQuest.</summary>
        public static bool TryParse(string text, PoseFrame frame)
        {
            if (string.IsNullOrEmpty(text) || !text.StartsWith("FQ1|")) return false;
            string[] f = text.Split('|');
            if (f.Length < 3) return false;

            frame.Status = null;
            frame.PreviewBase64 = null;

            if (f[1] == "status")
            {
                frame.Status = text.Substring(11);   // всё после "FQ1|status|"
                return true;
            }

            if (f.Length < 9 || (f[1] != "pose" && f[1] != "none")) return false;

            int.TryParse(f[2], NumberStyles.Integer, Inv, out frame.Sequence);
            float.TryParse(f[3], NumberStyles.Float, Inv, out frame.Fps);
            int.TryParse(f[4], NumberStyles.Integer, Inv, out frame.Width);
            int.TryParse(f[5], NumberStyles.Integer, Inv, out frame.Height);
            frame.PreviewBase64 = f[8].Length > 0 ? f[8] : null;

            frame.HasPose = f[1] == "pose"
                            && ParseFloats(f[6], frame.Image)
                            && ParseFloats(f[7], frame.World);
            return true;
        }

        static bool ParseFloats(string csv, float[] target)
        {
            string[] parts = csv.Split(',');
            if (parts.Length != target.Length) return false;
            for (int i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i], NumberStyles.Float, Inv, out target[i])) return false;
            return true;
        }
    }
}
