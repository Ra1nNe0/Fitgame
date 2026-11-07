using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Герой Фит. Сам никуда не бежит (движется мир), а выполняет действия:
    /// прыжок, присед, удар, руки вверх. Пока действие длится, CurrentAction хранит его тип.
    /// Визуальные состояния: покой → бег → действие → урон / победа.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Движения с качеством ниже этого порога не засчитываются.</summary>
        public const float MinQuality = 0.35f;

        public enum Mode { Idle, Run, Victory }

        public MotionType CurrentAction { get; private set; }
        public float CurrentQuality { get; private set; }
        public bool HasShield { get; private set; }

        SpriteRenderer view;
        SpriteRenderer bubble;
        SpriteRenderer shadow;
        float dustTimer;
        float sparkTimer;
        bool fever;
        SpriteAnimator animator;
        Sprite[] idle, run, jump, squat, punch, hands, hurt, win;
        string skin;

        Mode mode = Mode.Idle;
        Vector3 basePosition;
        float actionTime;
        float actionDuration;
        float hurtTime;

        public void Build(Vector3 position, string skinName)
        {
            basePosition = position;
            transform.position = position;

            view = Art.Spawn("hero_king_idle_0", transform, Vector2.zero, 10);
            animator = view.gameObject.AddComponent<SpriteAnimator>();
            animator.Init(view);
            animator.Unscaled = true;

            shadow = Art.Spawn("shadow_0", null, position, 4, true);
            shadow.color = new Color(1f, 1f, 1f, 0.35f);

            bubble = Art.Spawn("shield_0", transform, new Vector2(0f, 1.1f), 11, true);
            bubble.enabled = false;

            SetSkin(skinName);
            ResetState();
        }

        /// <summary>Сменить образ героя (king, knight).</summary>
        public void SetSkin(string skinName)
        {
            if (skin == skinName) return;
            skin = skinName;
            idle = Art.Hero(skin, "idle", 2);
            run = Art.Hero(skin, "run", 4);
            jump = Art.Hero(skin, "jump", 1);
            squat = Art.Hero(skin, "squat", 1);
            punch = Art.Hero(skin, "punch", 1);
            hands = Art.Hero(skin, "hands", 1);
            hurt = Art.Hero(skin, "hurt", 1);
            win = Art.Hero(skin, "win", 2);
        }

        public void SetMode(Mode newMode)
        {
            mode = newMode;
        }

        // тень живёт отдельно от героя (не прыгает вместе с ним), поэтому прячем её вручную
        void OnEnable() { if (shadow != null) shadow.gameObject.SetActive(true); }
        void OnDisable() { if (shadow != null) shadow.gameObject.SetActive(false); }

        /// <summary>Режим «Фивер»: герой светится золотом и оставляет искры.</summary>
        public void SetFever(bool on)
        {
            fever = on;
        }

        /// <summary>Скорость земли — чтобы пыль из-под ног улетала назад вместе с миром.</summary>
        public float WorldSpeed { get; set; }

        public void SetShield(bool on)
        {
            HasShield = on;
        }

        public void ResetState()
        {
            CurrentAction = MotionType.None;
            CurrentQuality = 0f;
            actionTime = 0f;
            actionDuration = 0f;
            hurtTime = 0f;
        }

        /// <summary>
        /// Начать действие. Другое движение может прервать текущее (например, присед перед прыжком
        /// превращается в прыжок). Вернёт false, если движение слишком слабое или уже выполняется.
        /// </summary>
        public bool TryPerform(MotionEvent motion)
        {
            if (motion.Type == MotionType.None || motion.Quality < MinQuality) return false;

            if (motion.IsUpdate)
            {
                // камера уточнила качество: человек присел глубже, прыгнул выше
                if (motion.Type == CurrentAction && motion.Quality > CurrentQuality) CurrentQuality = motion.Quality;
                return false;
            }

            if (motion.Type == CurrentAction && actionTime < actionDuration * 0.6f) return false;

            CurrentAction = motion.Type;
            CurrentQuality = motion.Quality;
            actionDuration = DurationOf(motion.Type);
            actionTime = 0f;
            return true;
        }

        /// <summary>Получить урон. Вернёт true, если удар принял щит.</summary>
        public bool Hurt()
        {
            if (HasShield)
            {
                HasShield = false;
                hurtTime = 0.3f;
                return true;
            }
            hurtTime = 0.6f;
            return false;
        }

        static float DurationOf(MotionType type)
        {
            switch (type)
            {
                case MotionType.Jump: return 0.75f;
                case MotionType.Squat: return 0.8f;
                case MotionType.Punch: return 0.5f;
                case MotionType.HandsUp: return 0.9f;
                default: return 0f;
            }
        }

        void Update()
        {
            if (view == null) return;

            if (hurtTime > 0f) hurtTime -= Time.deltaTime;

            if (CurrentAction != MotionType.None)
            {
                actionTime += Time.deltaTime;
                if (actionTime >= actionDuration) CurrentAction = MotionType.None;
            }

            ApplyPose();
        }

        /// <summary>Выбирает кадр анимации и высоту героя под текущее действие.</summary>
        void ApplyPose()
        {
            float t = actionDuration > 0f ? Mathf.Clamp01(actionTime / actionDuration) : 0f;
            float lift = 0f;
            Sprite[] frames;
            float fps = 1f;

            switch (CurrentAction)
            {
                case MotionType.Jump:
                    frames = jump;
                    lift = Mathf.Sin(t * Mathf.PI) * 2.4f;
                    break;
                case MotionType.Squat:
                    frames = squat;
                    break;
                case MotionType.Punch:
                    frames = punch;
                    break;
                case MotionType.HandsUp:
                    frames = hands;
                    lift = Mathf.Sin(t * Mathf.PI) * 0.6f;
                    break;
                default:
                    if (mode == Mode.Run) { frames = run; fps = 10f; }
                    else if (mode == Mode.Victory) { frames = win; fps = 4f; }
                    else { frames = idle; fps = 2f; }
                    break;
            }

            if (hurtTime > 0.3f && CurrentAction == MotionType.None) frames = hurt;

            animator.Play(frames, fps);
            transform.position = basePosition + Vector3.up * Art.Snap(lift);

            // тень на земле: чем выше прыжок, тем она меньше и бледнее
            if (shadow != null)
            {
                float k = 1f - Mathf.Clamp01(lift / 3f) * 0.5f;
                shadow.transform.position = basePosition + new Vector3(0f, 0.06f, 0f);
                shadow.transform.localScale = new Vector3(k, 1f, 1f);
                shadow.color = new Color(1f, 1f, 1f, 0.35f * k);
            }

            // пыль из-под ног на бегу
            if (mode == Mode.Run && CurrentAction == MotionType.None && Time.timeScale > 0f)
            {
                dustTimer -= Time.deltaTime;
                if (dustTimer <= 0f)
                {
                    dustTimer = 0.3f;
                    Fx.Dust(basePosition + new Vector3(-0.35f, 0.15f, 0f), WorldSpeed > 0f ? WorldSpeed : 5f);
                }
            }

            // «Фивер»: золотое свечение и искры
            if (fever)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
                view.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.35f), 0.35f + 0.35f * pulse);
                sparkTimer -= Time.deltaTime;
                if (sparkTimer <= 0f)
                {
                    sparkTimer = 0.12f;
                    Fx.Sparkle(transform.position + new Vector3(Random.Range(-0.6f, 0.4f), Random.Range(0.4f, 2f), 0f), 1, 0.4f);
                }
            }
            else view.color = Color.white;

            // После удара герой недолго мигает
            view.enabled = hurtTime <= 0f || (int)(hurtTime * 20f) % 2 == 0;
            bubble.enabled = HasShield && (int)(Time.time * 4f) % 8 != 0;
        }
    }
}
