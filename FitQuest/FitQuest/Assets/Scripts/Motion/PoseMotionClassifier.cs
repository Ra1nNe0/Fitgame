using System;
using System.Collections.Generic;

namespace FitQuest
{
    public enum TrackerState
    {
        NoBody,       // человека нет в кадре
        Calibrating,  // запоминаем обычную стойку игрока
        Ready         // распознаём движения
    }

    /// <summary>
    /// AI-модуль распознавания движений (интеллектуальная механика №1).
    ///
    /// Вход: 33 точки тела от нейросети MediaPipe Pose (PoseFrame) — на картинке и в метрах.
    /// Обработка:
    ///   1) сглаживание шума;
    ///   2) калибровка — запоминаем, где у игрока плечи и таз в обычной стойке
    ///      и какой длины туловище (всё дальше считается в длинах туловища,
    ///      поэтому неважно, стоит игрок близко или далеко);
    ///   3) признаки движений: подъём тела (прыжок), опускание таза и угол в коленях (присед),
    ///      выпрямление руки на высоте плеча (удар), кисти выше головы (руки вверх);
    ///   4) пороги и конечный автомат: «готов → попытка → движение → пауза».
    /// Выход: MotionEvent — тип движения и качество 0..1 (глубина приседа, высота прыжка,
    /// прямота рук). Если игрок начал движение, но не довёл, выдаётся «слабое» движение
    /// с качеством ниже 0.35, и игра его не засчитывает.
    ///
    /// Класс не зависит от Unity, поэтому его можно проверять отдельными тестами.
    /// </summary>
    public sealed class PoseMotionClassifier
    {
        // Номера точек в модели MediaPipe Pose
        const int Nose = 0, LSh = 11, RSh = 12, LEl = 13, REl = 14, LWr = 15, RWr = 16,
                  LHip = 23, RHip = 24, LKnee = 25, RKnee = 26, LAnk = 27, RAnk = 28;

        const float CalibrationSeconds = 0.8f;
        const float MinValidQuality = 0.4f;   // засчитанное движение не бывает хуже этого

        enum Phase { Armed, Attempt, Active, Cooldown }

        /// <summary>Строгость: 0.8 — мягко, 1 — обычно, 1.25 — строго.</summary>
        public float Strictness = 1f;

        public TrackerState State { get; private set; }
        public float CalibrationProgress { get; private set; }
        public string Hint { get; private set; }
        public bool KneesVisible { get; private set; }
        /// <summary>Насколько каждое движение близко к порогу (0..1), индекс — (int)MotionType. Для шкал в интерфейсе.</summary>
        public readonly float[] Levels = new float[5];
        /// <summary>Движение, которое выполняется прямо сейчас.</summary>
        public MotionType Active { get; private set; }
        public float ActiveQuality { get; private set; }
        /// <summary>Сколько раз пришлось перекалибровываться (для отчёта).</summary>
        public int Recalibrations { get; private set; }

        // Сглаженные точки: картинка (x умножен на соотношение сторон, чтобы углы были честными) и метры
        readonly float[] img = new float[PoseFrame.PointCount * 3];
        readonly float[] wld = new float[PoseFrame.PointCount * 3];
        bool smoothed;

        // Калибровка
        bool calibrated;
        float neutralTime, sumShY, sumHipY, sumL;
        int sumCount;
        float baseShY, baseHipY, baseL;

        double lastTime = -1, lastSeen = -100;
        float prevHipY, hipSpeed;

        // Удар: отслеживаем каждую руку
        readonly double[] armBentAt = { -100, -100 };
        readonly float[] prevReach = new float[2];
        readonly float[] prevWristY = new float[2];
        readonly float[] reachSpeed = new float[2];
        readonly float[] wristSpeedY = new float[2];
        readonly float[] peakReachSpeed = new float[2];

        // Конечный автомат
        Phase phase;
        MotionType phaseMotion;
        bool emitted;
        float peakFeature, emittedQuality;
        double phaseStart, belowSince = -1, cooldownUntil, lastUpdateAt, suppressSquatUntil;
        int squatFrames;

        // Признаки текущего кадра
        float fJump, fSquat, fShDrop, fPunch, fHands, kneeAngle, minElbow, punchHeight;
        int punchArm = -1;
        bool bothArmsOut;

        public PoseMotionClassifier()
        {
            State = TrackerState.NoBody;
            Hint = "Встаньте перед камерой";
        }

        /// <summary>Сбросить калибровку (игрок сменился или отошёл).</summary>
        public void Recalibrate()
        {
            calibrated = false;
            neutralTime = 0f;
            sumCount = 0;
            phase = Phase.Armed;
            Active = MotionType.None;
        }

        // ------------------------------------------------------------------ пороги

        float Enter(MotionType m)
        {
            float s = Strictness;
            switch (m)
            {
                case MotionType.Jump: return 0.13f * s;
                case MotionType.Squat: return 0.16f * s;
                case MotionType.Punch: return 150f + 20f * (s - 1f);   // угол в локте, градусы
                case MotionType.HandsUp: return -0.05f + 0.1f * (s - 1f);   // кисти примерно на уровне носа и выше
                default: return 1f;
            }
        }

        static float Attempt(MotionType m)
        {
            switch (m)
            {
                case MotionType.Jump: return 0.07f;
                case MotionType.Squat: return 0.08f;
                case MotionType.Punch: return 118f;
                case MotionType.HandsUp: return -0.3f;
                default: return 1f;
            }
        }

        static float Release(MotionType m)
        {
            switch (m)
            {
                case MotionType.Jump: return 0.05f;
                case MotionType.Squat: return 0.06f;
                case MotionType.Punch: return 108f;
                case MotionType.HandsUp: return -0.45f;
                default: return 0f;
            }
        }

        float Feature(MotionType m)
        {
            switch (m)
            {
                case MotionType.Jump: return fJump;
                case MotionType.Squat: return fSquat;
                case MotionType.Punch: return fPunch;
                case MotionType.HandsUp: return fHands;
                default: return 0f;
            }
        }

        // ------------------------------------------------------------------ главный метод

        /// <summary>Обработать кадр. Новые движения добавляются в output.</summary>
        public void Feed(PoseFrame frame, double time, List<MotionEvent> output)
        {
            float dt = lastTime < 0 ? 1f / 30f : (float)Math.Max(0.001, Math.Min(0.2, time - lastTime));
            lastTime = time;

            if (!frame.HasPose || !Visible(frame, LSh) || !Visible(frame, RSh) || !Visible(frame, LHip) || !Visible(frame, RHip))
            {
                if (time - lastSeen > 0.5)
                {
                    State = TrackerState.NoBody;
                    Hint = frame.HasPose ? "Отойдите дальше: должны быть видны плечи и пояс" : "Встаньте перед камерой";
                    smoothed = false;
                    phase = Phase.Armed;
                    Active = MotionType.None;
                    Array.Clear(Levels, 0, Levels.Length);
                }
                if (time - lastSeen > 3.0) Recalibrate();
                return;
            }
            lastSeen = time;

            Smooth(frame);
            ComputeFeatures(frame, dt, time);

            if (!calibrated)
            {
                Calibrate(dt);
                return;
            }

            State = TrackerState.Ready;
            Hint = KneesVisible ? "Отлично, вас хорошо видно" : "Отойдите чуть дальше, чтобы были видны колени";

            for (int m = 1; m <= 4; m++)
            {
                MotionType type = (MotionType)m;
                float range = Enter(type) - Release(type);
                Levels[m] = Clamp01((Feature(type) - Release(type)) / range);
            }

            Step(time, output);
        }

        static bool Visible(PoseFrame f, int i)
        {
            return f.Image[i * 3 + 2] > 0.5f && f.Image[i * 3 + 1] < 1.05f && f.Image[i * 3 + 1] > -0.05f;
        }

        void Smooth(PoseFrame f)
        {
            float aspect = f.Aspect;
            const float a = 0.6f;   // доля нового кадра
            for (int i = 0; i < PoseFrame.PointCount; i++)
            {
                int k = i * 3;
                float x = f.Image[k] * aspect, y = f.Image[k + 1], v = f.Image[k + 2];
                if (!smoothed)
                {
                    img[k] = x; img[k + 1] = y; img[k + 2] = v;
                    wld[k] = f.World[k]; wld[k + 1] = f.World[k + 1]; wld[k + 2] = f.World[k + 2];
                }
                else
                {
                    img[k] += (x - img[k]) * a;
                    img[k + 1] += (y - img[k + 1]) * a;
                    img[k + 2] = v;
                    wld[k] += (f.World[k] - wld[k]) * a;
                    wld[k + 1] += (f.World[k + 1] - wld[k + 1]) * a;
                    wld[k + 2] += (f.World[k + 2] - wld[k + 2]) * a;
                }
            }
            if (!smoothed)
            {
                prevHipY = HipY;
                prevWristY[0] = img[LWr * 3 + 1];
                prevWristY[1] = img[RWr * 3 + 1];
            }
            smoothed = true;
        }

        float ShY { get { return (img[LSh * 3 + 1] + img[RSh * 3 + 1]) * 0.5f; } }
        float HipY { get { return (img[LHip * 3 + 1] + img[RHip * 3 + 1]) * 0.5f; } }

        float TorsoLength
        {
            get
            {
                float sx = (img[LSh * 3] + img[RSh * 3]) * 0.5f, hx = (img[LHip * 3] + img[RHip * 3]) * 0.5f;
                float dx = sx - hx, dy = ShY - HipY;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
        }

        void ComputeFeatures(PoseFrame f, float dt, double time)
        {
            float shY = ShY, hipY = HipY;
            float L = calibrated ? baseL : TorsoLength;
            if (L < 1e-3f) L = 1e-3f;

            hipSpeed = hipSpeed * 0.5f + ((hipY - prevHipY) / dt / L) * 0.5f;
            prevHipY = hipY;

            KneesVisible = Visible(f, LKnee) && Visible(f, RKnee) && Visible(f, LAnk) && Visible(f, RAnk);
            kneeAngle = KneesVisible
                ? (Angle3(LHip, LKnee, LAnk) + Angle3(RHip, RKnee, RAnk)) * 0.5f
                : 180f;

            fJump = calibrated ? Math.Min(baseShY - shY, baseHipY - hipY) / L : 0f;
            fSquat = calibrated ? (hipY - baseHipY) / L : 0f;
            fShDrop = calibrated ? (shY - baseShY) / L : 0f;

            // Руки. Высоту рук считаем по точкам в метрах: там нет искажения перспективы
            // (рука, вытянутая к камере, на картинке кажется выше, чем есть).
            float torsoW = Math.Max(Dist3Mid(LSh, RSh, LHip, RHip), 0.05f);
            float noseYw = wld[Nose * 3 + 1];
            float best = 0f;
            punchArm = -1;
            int armsOut = 0;
            minElbow = 180f;
            float upL = 0f, upR = 0f;
            for (int a = 0; a < 2; a++)
            {
                int sh = a == 0 ? LSh : RSh, el = a == 0 ? LEl : REl, wr = a == 0 ? LWr : RWr;
                float elbow = Angle3(sh, el, wr);
                float wrYw = wld[wr * 3 + 1];
                float height = Math.Abs(wrYw - wld[sh * 3 + 1]) / torsoW;   // 0 = кисть ровно на уровне плеча
                minElbow = Math.Min(minElbow, elbow);

                reachSpeed[a] = (elbow - prevReach[a]) / dt;                // скорость разгибания локтя, °/с
                float wrY = img[wr * 3 + 1];
                wristSpeedY[a] = wristSpeedY[a] * 0.5f + (Math.Abs(wrY - prevWristY[a]) / dt / L) * 0.5f;
                prevReach[a] = elbow;
                prevWristY[a] = wrY;

                if (elbow < 110f) armBentAt[a] = time;
                if (time - armBentAt[a] > 0.6) peakReachSpeed[a] = 0f;
                peakReachSpeed[a] = Math.Max(peakReachSpeed[a], reachSpeed[a]);

                bool atShoulder = height < 0.45f;
                if (atShoulder && elbow > 140f) armsOut++;

                // Удар — резкое выпрямление руки на высоте плеча, а не медленно поднятая прямая рука
                float value = atShoulder ? elbow : 0f;
                bool dynamic = time - armBentAt[a] < 0.6;
                if (!dynamic) value = Math.Min(value, Attempt(MotionType.Punch) - 1f);
                if (value > best) { best = value; punchArm = a; punchHeight = height; }

                float up = ArmUp((noseYw - wrYw) / torsoW, (noseYw - wld[el * 3 + 1]) / torsoW,
                                 (wld[sh * 3 + 1] - wld[el * 3 + 1]) / torsoW, img[wr * 3 + 2], img[wr * 3 + 1]);
                if (a == 0) upL = up; else upR = up;
            }
            bothArmsOut = armsOut >= 2;
            fPunch = bothArmsOut ? Math.Min(best, Attempt(MotionType.Punch) - 0.01f) : best;
            fHands = Math.Min(upL, upR);
        }

        /// <summary>
        /// Насколько рука поднята вверх (0 — кисть на уровне носа, больше — выше).
        /// Кисти над головой часто выходят за верх кадра, и нейросеть теряет их или ставит не туда,
        /// поэтому учитываем и локоть: локоть у лица или выше — рука поднята, даже если кисть не видна.
        /// </summary>
        public static float ArmUp(float wristOverNose, float elbowOverNose, float elbowOverShoulder,
                                  float wristVisibility, float wristImageY, float elbowBonus = 0.2f)
        {
            float byElbow = elbowOverNose + elbowBonus;
            bool wristLost = wristVisibility < 0.5f || wristImageY < 0.02f;
            if (wristLost && elbowOverShoulder > 0.15f) return Math.Max(Math.Max(wristOverNose, byElbow), 0.3f);
            return Math.Max(wristOverNose, byElbow);
        }

        void Calibrate(float dt)
        {
            State = TrackerState.Calibrating;
            float L = TorsoLength;
            bool wristsDown = img[LWr * 3 + 1] > ShY + 0.15f * L && img[RWr * 3 + 1] > ShY + 0.15f * L;
            bool legsStraight = !KneesVisible || kneeAngle > 150f;
            bool still = Math.Abs(hipSpeed) < 0.6f;

            if (wristsDown && legsStraight && still)
            {
                neutralTime += dt;
                sumShY += ShY; sumHipY += HipY; sumL += L; sumCount++;
                Hint = "Стойте ровно...";
            }
            else
            {
                neutralTime = 0f;
                sumShY = sumHipY = sumL = 0f; sumCount = 0;
                Hint = "Встаньте прямо, руки опустите";
            }

            CalibrationProgress = Clamp01(neutralTime / CalibrationSeconds);
            if (neutralTime < CalibrationSeconds || sumCount == 0) return;

            baseShY = sumShY / sumCount;
            baseHipY = sumHipY / sumCount;
            baseL = sumL / sumCount;
            calibrated = true;
            phase = Phase.Armed;
            State = TrackerState.Ready;
        }

        bool IsNeutral()
        {
            return fJump < Release(MotionType.Jump) && fSquat < Release(MotionType.Squat)
                   && fPunch < Release(MotionType.Punch) && fHands < Release(MotionType.HandsUp)
                   && Math.Abs(hipSpeed) < 0.3f;
        }

        // ------------------------------------------------------------------ конечный автомат

        void Step(double time, List<MotionEvent> output)
        {
            // Пока игрок спокойно стоит, медленно подстраиваем калибровку (он мог сделать шаг к камере).
            if (phase == Phase.Armed && IsNeutral())
            {
                const float k = 0.02f;
                baseShY += (ShY - baseShY) * k;
                baseHipY += (HipY - baseHipY) * k;
                baseL += (TorsoLength - baseL) * k;
            }

            squatFrames = fSquat >= Enter(MotionType.Squat) && fShDrop > 0.06f ? squatFrames + 1 : 0;

            switch (phase)
            {
                case Phase.Cooldown:
                    if (time >= cooldownUntil) phase = Phase.Armed;
                    break;

                case Phase.Armed:
                {
                    MotionType m = BestCandidate(time, Attempt);
                    if (m == MotionType.None) break;
                    if (Ready(m, time)) Begin(m, time, output);
                    else
                    {
                        phase = Phase.Attempt;
                        phaseMotion = m;
                        phaseStart = time;
                        peakFeature = Feature(m);
                    }
                    break;
                }

                case Phase.Attempt:
                {
                    MotionType strong = BestCandidate(time, Enter);
                    if (strong != MotionType.None && Ready(strong, time))
                    {
                        Begin(strong, time, output);
                        break;
                    }

                    float f = Feature(phaseMotion);
                    peakFeature = Math.Max(peakFeature, f);
                    if (f < Release(phaseMotion))
                    {
                        // Движение начато, но не доведено: «слабое» движение
                        float att = Attempt(phaseMotion), ent = Enter(phaseMotion);
                        float progress = Clamp01((peakFeature - att) / (ent - att));
                        if (time - phaseStart > 0.15 && progress > 0.3f)
                            output.Add(new MotionEvent(phaseMotion, 0.1f + 0.2f * progress));
                        EndPhase(time, 0.3);
                    }
                    else if (time - phaseStart > 2.0)
                    {
                        // Застыл в полудвижении: ждём, пока вернётся в стойку, ничего не засчитывая
                        phase = Phase.Active;
                        emitted = false;
                        Active = MotionType.None;
                    }
                    break;
                }

                case Phase.Active:
                {
                    // Присед перед прыжком или взмах руками — нормально: прыжок важнее
                    if (phaseMotion != MotionType.Jump && fJump >= Enter(MotionType.Jump) && time - phaseStart < 0.6)
                    {
                        Begin(MotionType.Jump, time, output);
                        break;
                    }

                    if (emitted)
                    {
                        float q = Quality(phaseMotion);
                        ActiveQuality = Math.Max(ActiveQuality, q);
                        if (q > emittedQuality + 0.05f && time - lastUpdateAt > 0.08)
                        {
                            emittedQuality = q;
                            lastUpdateAt = time;
                            output.Add(new MotionEvent(phaseMotion, q, true));
                        }
                    }

                    if (Feature(phaseMotion) < Release(phaseMotion))
                    {
                        if (belowSince < 0) belowSince = time;
                        if (time - belowSince >= 0.1)
                        {
                            if (phaseMotion == MotionType.Jump) suppressSquatUntil = time + 0.5;
                            EndPhase(time, 0.25);
                        }
                    }
                    else
                    {
                        belowSince = -1;
                    }

                    // Человек не может «висеть» в прыжке: значит, он сдвинулся — перекалибруемся
                    if (phaseMotion == MotionType.Jump && time - phaseStart > 1.5)
                    {
                        Recalibrations++;
                        Recalibrate();
                    }
                    break;
                }
            }
        }

        MotionType BestCandidate(double time, Func<MotionType, float> threshold)
        {
            if (fJump >= threshold(MotionType.Jump)) return MotionType.Jump;   // прыжок важнее всего

            MotionType best = MotionType.None;
            float bestScore = 0f;
            for (int m = 2; m <= 4; m++)
            {
                MotionType type = (MotionType)m;
                if (type == MotionType.Squat && (time < suppressSquatUntil || fShDrop < 0.04f)) continue;
                float f = Feature(type), t = threshold(type);
                if (f < t) continue;
                float score = (f - Attempt(type)) / (Enter(type) - Attempt(type));
                if (score > bestScore) { bestScore = score; best = type; }
            }
            return best;
        }

        /// <summary>Достаточно ли признаков, чтобы засчитать движение.</summary>
        bool Ready(MotionType m, double time)
        {
            if (Feature(m) < Enter(m)) return false;
            switch (m)
            {
                case MotionType.Squat:
                    return squatFrames >= 2;                       // не случайный кадр
                case MotionType.HandsUp:
                    return fJump < Attempt(MotionType.Jump) && hipSpeed > -1.0f;   // это не взмах руками в прыжке
                case MotionType.Punch:
                    return punchArm >= 0 && !bothArmsOut
                           && wristSpeedY[punchArm] < 2.5f           // рука идёт вперёд/вбок, а не вверх
                           && time - armBentAt[punchArm] < 0.6;      // рука только что была согнута
                default:
                    return true;
            }
        }

        void Begin(MotionType m, double time, List<MotionEvent> output)
        {
            float q = Math.Max(MinValidQuality, Quality(m));
            output.Add(new MotionEvent(m, q));
            phase = Phase.Active;
            phaseMotion = m;
            phaseStart = time;
            emitted = true;
            emittedQuality = q;
            lastUpdateAt = time;
            belowSince = -1;
            Active = m;
            ActiveQuality = q;
        }

        void EndPhase(double time, double cooldown)
        {
            phase = Phase.Cooldown;
            cooldownUntil = time + cooldown;
            Active = MotionType.None;
            belowSince = -1;
        }

        /// <summary>Качество движения 0..1 — насколько правильно оно выполнено.</summary>
        float Quality(MotionType m)
        {
            switch (m)
            {
                case MotionType.Jump:
                    // выше прыжок — лучше
                    return 0.45f + 0.55f * Clamp01((fJump - Enter(m)) / 0.17f);

                case MotionType.Squat:
                {
                    // глубже присед и меньше угол в коленях — лучше
                    float byDepth = Clamp01((fSquat - 0.10f) / 0.40f);
                    if (!KneesVisible) return byDepth;
                    float byKnees = Clamp01((165f - kneeAngle) / 70f);
                    return 0.5f * byKnees + 0.5f * byDepth;
                }

                case MotionType.Punch:
                {
                    if (punchArm < 0) return 0f;
                    int sh = punchArm == 0 ? LSh : RSh, el = punchArm == 0 ? LEl : REl, wr = punchArm == 0 ? LWr : RWr;
                    float straight = Clamp01((Angle3(sh, el, wr) - 150f) / 25f);   // рука прямая
                    float level = 1f - Clamp01((punchHeight - 0.1f) / 0.3f);      // точно на уровне плеча
                    float speed = Clamp01(peakReachSpeed[punchArm] / 600f);        // резко (°/с)
                    return 0.35f * straight + 0.35f * level + 0.3f * speed;
                }

                case MotionType.HandsUp:
                {
                    float straight = Clamp01((minElbow - 130f) / 40f);            // руки прямые
                    float height = Clamp01(fHands / 0.6f);                        // высоко
                    return 0.6f * straight + 0.4f * height;
                }

                default:
                    return 0f;
            }
        }

        // ------------------------------------------------------------------ геометрия

        float Dist3Mid(int a1, int a2, int b1, int b2)
        {
            float dx = (wld[a1 * 3] + wld[a2 * 3] - wld[b1 * 3] - wld[b2 * 3]) * 0.5f;
            float dy = (wld[a1 * 3 + 1] + wld[a2 * 3 + 1] - wld[b1 * 3 + 1] - wld[b2 * 3 + 1]) * 0.5f;
            float dz = (wld[a1 * 3 + 2] + wld[a2 * 3 + 2] - wld[b1 * 3 + 2] - wld[b2 * 3 + 2]) * 0.5f;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        float Dist3(int a, int b)
        {
            float dx = wld[a * 3] - wld[b * 3], dy = wld[a * 3 + 1] - wld[b * 3 + 1], dz = wld[a * 3 + 2] - wld[b * 3 + 2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Угол в точке b (в градусах) между отрезками b-a и b-c, по 3D-точкам в метрах.</summary>
        float Angle3(int a, int b, int c)
        {
            float ux = wld[a * 3] - wld[b * 3], uy = wld[a * 3 + 1] - wld[b * 3 + 1], uz = wld[a * 3 + 2] - wld[b * 3 + 2];
            float vx = wld[c * 3] - wld[b * 3], vy = wld[c * 3 + 1] - wld[b * 3 + 1], vz = wld[c * 3 + 2] - wld[b * 3 + 2];
            float dot = ux * vx + uy * vy + uz * vz;
            float len = (float)Math.Sqrt((ux * ux + uy * uy + uz * uz) * (vx * vx + vy * vy + vz * vz));
            if (len < 1e-6f) return 180f;
            double cos = Math.Max(-1.0, Math.Min(1.0, dot / len));
            return (float)(Math.Acos(cos) * 180.0 / Math.PI);
        }

        static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }
}
