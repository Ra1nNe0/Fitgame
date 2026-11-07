using System;
using System.Collections.Generic;

namespace FitQuest
{
    /// <summary>
    /// Распознавание поз-стрелок для ритм-режима (по 33 точкам MediaPipe, как и PoseMotionClassifier).
    ///   ВЛЕВО  — рука вытянута в сторону, влево на экране;
    ///   ВПРАВО — рука вытянута в сторону, вправо на экране (обе руки в стороны — аккорд влево+вправо);
    ///   ВВЕРХ  — обе кисти выше носа;
    ///   ВНИЗ   — присед (таз и плечи опустились относительно обычной стойки).
    /// Картинка с камеры зеркальная, поэтому сторона берётся по экрану: игрок тянет руку туда, где стрелка.
    ///
    /// В отличие от классификатора движений здесь важно не «что за движение было», а «какая поза держится
    /// прямо сейчас»: Held — позы, которые держит игрок (для длинных нот), а в pressed попадают позы,
    /// которые только что начались (это и есть «нажатие» стрелки).
    /// Класс не зависит от Unity — его проверяет автотест Tools/ai_tests/RhythmHarness.cs.
    /// </summary>
    public sealed class RhythmPoseTracker
    {
        public const int Left = 0, Down = 1, Up = 2, Right = 3;
        public const int LaneCount = 4;

        const int Nose = 0, LSh = 11, RSh = 12, LEl = 13, REl = 14, LWr = 15, RWr = 16, LHip = 23, RHip = 24;
        const int CalibrationFrames = 12;
        const double Debounce = 0.12;       // после отпускания позу нельзя «нажать» снова раньше, с
        const float MaxSideSpeed = 2.5f;    // рука в стороне должна остановиться (длин туловища в секунду)

        /// <summary>Строгость: 0.8 — мягко, 1 — обычно, 1.25 — строго.</summary>
        public float Strictness = 1f;

        /// <summary>Позы, которые игрок держит сейчас (индекс — дорожка).</summary>
        public readonly bool[] Held = new bool[LaneCount];
        /// <summary>Насколько каждая поза близка к порогу 0..1 — для подсказок в интерфейсе.</summary>
        public readonly float[] Levels = new float[LaneCount];

        public bool HasBody { get; private set; }
        /// <summary>Запомнена обычная стойка (без неё не распознаётся присед).</summary>
        public bool Calibrated { get; private set; }
        public string Hint { get; private set; }

        readonly float[] img = new float[PoseFrame.PointCount * 3];
        readonly float[] wld = new float[PoseFrame.PointCount * 3];   // точки в метрах (y вниз), без перспективы
        readonly float[] feature = new float[LaneCount];
        readonly double[] releasedAt = { -100, -100, -100, -100 };
        readonly float[] prevWristY = new float[2];
        readonly float[] wristSpeed = new float[2];
        bool smoothed;
        double lastTime = -1, lastSeen = -100;

        float baseHipY, baseShY, baseL;
        float sumHipY, sumShY, sumL;
        int calibFrames;

        public RhythmPoseTracker()
        {
            Hint = "Встаньте перед камерой";
        }

        public void Reset()
        {
            smoothed = false;
            Calibrated = false;
            calibFrames = 0;
            sumHipY = sumShY = sumL = 0f;
            for (int i = 0; i < LaneCount; i++) { Held[i] = false; Levels[i] = 0f; releasedAt[i] = -100; }
        }

        // ------------------------------------------------------------------ пороги

        float Enter(int lane)
        {
            float s = Strictness;
            switch (lane)
            {
                case Up: return 0.05f * s;      // кисти выше носа (в длинах туловища)
                case Down: return 0.12f * s;    // таз ниже обычного
                default: return 1.0f * s;       // кисть далеко в стороне от середины плеч
            }
        }

        static float Release(int lane)
        {
            switch (lane)
            {
                case Up: return -0.12f;
                case Down: return 0.06f;
                default: return 0.78f;
            }
        }

        // ------------------------------------------------------------------ главный метод

        /// <summary>Обработать кадр. Дорожки, позы которых только что начались, добавляются в pressed.</summary>
        public void Feed(PoseFrame frame, double time, List<int> pressed)
        {
            float dt = lastTime < 0 ? 1f / 30f : (float)Math.Max(0.001, Math.Min(0.2, time - lastTime));
            lastTime = time;

            if (!frame.HasPose || !Visible(frame, LSh) || !Visible(frame, RSh) || !Visible(frame, LHip) || !Visible(frame, RHip))
            {
                if (time - lastSeen > 0.4)
                {
                    HasBody = false;
                    smoothed = false;
                    Hint = frame.HasPose ? "Отойдите дальше: должны быть видны плечи и пояс" : "Встаньте перед камерой";
                    for (int i = 0; i < LaneCount; i++) { Held[i] = false; Levels[i] = 0f; }
                }
                if (time - lastSeen > 3.0) { Calibrated = false; calibFrames = 0; sumHipY = sumShY = sumL = 0f; }
                return;
            }
            lastSeen = time;
            HasBody = true;

            Smooth(frame, dt);

            float shY = (Y(LSh) + Y(RSh)) * 0.5f, hipY = (Y(LHip) + Y(RHip)) * 0.5f;
            float shX = (X(LSh) + X(RSh)) * 0.5f, hipX = (X(LHip) + X(RHip)) * 0.5f;
            float L = (float)Math.Sqrt((shX - hipX) * (shX - hipX) + (shY - hipY) * (shY - hipY));
            if (L < 1e-3f) L = 1e-3f;

            // Руки: вверх (обе кисти выше носа) и в стороны (кисть далеко от середины плеч, примерно на их высоте)
            // Высоту рук считаем по точкам в метрах: на картинке рука, вытянутая к камере,
            // кажется выше, чем есть (особенно если камера стоит низко).
            float torsoW = Math.Max(0.05f, (float)Math.Sqrt(Sq(WX(LSh) + WX(RSh) - WX(LHip) - WX(RHip)) * 0.25f
                                                        + Sq(WY(LSh) + WY(RSh) - WY(LHip) - WY(RHip)) * 0.25f));
            float noseW = WY(Nose);
            float up = float.MaxValue, left = 0f, right = 0f;
            bool leftStill = false, rightStill = false;
            for (int a = 0; a < 2; a++)
            {
                int wr = a == 0 ? LWr : RWr, el = a == 0 ? LEl : REl;
                float wx = X(wr), wy = Y(wr);
                int sh = a == 0 ? LSh : RSh;
                up = Math.Min(up, PoseMotionClassifier.ArmUp((noseW - WY(wr)) / torsoW, (noseW - WY(el)) / torsoW,
                                                             (WY(sh) - WY(el)) / torsoW, img[wr * 3 + 2], wy));

                float dx = (wx - shX) / L;
                float vert = Math.Abs(wy - shY) / L;
                float ext = Math.Abs(dx) - Math.Max(0f, vert - 0.6f) * 2f;   // слишком высоко или низко — не «в сторону»
                bool still = wristSpeed[a] < MaxSideSpeed;
                if (img[wr * 3 + 2] < 0.5f || wy < 0.02f) continue;   // кисть за кадром (обычно над головой) — это не «в сторону»
                if (dx < 0f && ext > left) { left = ext; leftStill = still; }
                if (dx > 0f && ext > right) { right = ext; rightStill = still; }
            }

            // Ноги: присед относительно запомненной стойки
            if (!Calibrated) Calibrate(shY, hipY, L);
            float down = Calibrated ? Math.Min(hipY - baseHipY, shY - baseShY) / baseL : 0f;

            feature[Left] = left;
            feature[Right] = right;
            feature[Up] = up;
            feature[Down] = down;

            for (int lane = 0; lane < LaneCount; lane++)
            {
                float f = feature[lane];
                float enter = Enter(lane), release = Release(lane);
                Levels[lane] = Clamp01((f - release) / (enter - release));

                if (!Held[lane])
                {
                    bool ready = f >= enter && time - releasedAt[lane] > Debounce;
                    // Рука, которая проходит через «в сторону» по дороге вверх или вниз, — ещё не поза
                    if (lane == Left && !leftStill) ready = false;
                    if (lane == Right && !rightStill) ready = false;
                    if (lane == Down && !Calibrated) ready = false;
                    if (ready)
                    {
                        Held[lane] = true;
                        pressed.Add(lane);
                    }
                }
                else if (f < release)
                {
                    Held[lane] = false;
                    releasedAt[lane] = time;
                }
            }

            // Пока игрок спокойно стоит, медленно подстраиваем стойку (он мог шагнуть к камере)
            if (Calibrated && !Held[Down] && down < Release(Down) && Math.Abs(down) < 0.2f)
            {
                const float k = 0.03f;
                baseHipY += (hipY - baseHipY) * k;
                baseShY += (shY - baseShY) * k;
                baseL += (L - baseL) * k;
            }

            if (!Calibrated) Hint = "Встаньте прямо, руки опустите";
            else Hint = "Позы видны: руки в стороны, вверх, присед";
        }

        void Calibrate(float shY, float hipY, float L)
        {
            bool wristsDown = Y(LWr) > shY + 0.15f * L && Y(RWr) > shY + 0.15f * L;
            if (!wristsDown)
            {
                calibFrames = 0;
                sumHipY = sumShY = sumL = 0f;
                return;
            }
            calibFrames++;
            sumHipY += hipY; sumShY += shY; sumL += L;
            if (calibFrames < CalibrationFrames) return;
            baseHipY = sumHipY / calibFrames;
            baseShY = sumShY / calibFrames;
            baseL = sumL / calibFrames;
            Calibrated = true;
        }

        // ------------------------------------------------------------------ точки

        static bool Visible(PoseFrame f, int i)
        {
            return f.Image[i * 3 + 2] > 0.5f && f.Image[i * 3 + 1] < 1.05f && f.Image[i * 3 + 1] > -0.05f;
        }

        void Smooth(PoseFrame f, float dt)
        {
            float aspect = f.Aspect;
            const float a = 0.65f;   // доля нового кадра
            float L = 1f;
            if (smoothed)
            {
                float sx = (X(LSh) + X(RSh)) * 0.5f - (X(LHip) + X(RHip)) * 0.5f;
                float sy = (Y(LSh) + Y(RSh)) * 0.5f - (Y(LHip) + Y(RHip)) * 0.5f;
                L = Math.Max(1e-3f, (float)Math.Sqrt(sx * sx + sy * sy));
            }
            for (int i = 0; i < PoseFrame.PointCount; i++)
            {
                int k = i * 3;
                float x = f.Image[k] * aspect, y = f.Image[k + 1];
                if (!smoothed) { img[k] = x; img[k + 1] = y; }
                else { img[k] += (x - img[k]) * a; img[k + 1] += (y - img[k + 1]) * a; }
                img[k + 2] = f.Image[k + 2];
                for (int j = 0; j < 3; j++)
                    wld[k + j] = smoothed ? wld[k + j] + (f.World[k + j] - wld[k + j]) * a : f.World[k + j];
            }
            for (int w = 0; w < 2; w++)
            {
                float y = Y(w == 0 ? LWr : RWr);
                wristSpeed[w] = smoothed ? wristSpeed[w] * 0.5f + Math.Abs(y - prevWristY[w]) / dt / L * 0.5f : 0f;
                prevWristY[w] = y;
            }
            smoothed = true;
        }

        float WX(int i) { return wld[i * 3]; }
        float WY(int i) { return wld[i * 3 + 1]; }
        static float Sq(float v) { return v * v; }
        float X(int i) { return img[i * 3]; }
        float Y(int i) { return img[i * 3 + 1]; }

        static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        public static string LaneName(int lane)
        {
            switch (lane)
            {
                case Left: return "ВЛЕВО";
                case Down: return "ВНИЗ";
                case Up: return "ВВЕРХ";
                default: return "ВПРАВО";
            }
        }

        public static string PoseName(int lane)
        {
            switch (lane)
            {
                case Left: return "РУКА ВЛЕВО";
                case Down: return "ПРИСЕД";
                case Up: return "РУКИ ВВЕРХ";
                default: return "РУКА ВПРАВО";
            }
        }
    }
}
