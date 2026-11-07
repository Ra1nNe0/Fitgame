using UnityEngine;

namespace FitQuest
{
    public enum ObstacleKind
    {
        Spikes,    // шипы — перепрыгнуть
        Log,       // катящееся бревно — перепрыгнуть (быстрое)
        Bat,       // летучая мышь — присесть (быстрая)
        Fireball,  // огненный шар — присесть (очень быстрый)
        Slime,     // слизень — ударить
        Crate,     // ящик с монетами — разбить ударом
        Vine,      // яма с лианой — поднять руки и перелететь
        Coin,      // монета над головой — руки вверх (бонус)
        Heart,     // сердце — руки вверх (бонус, +1 жизнь)
        BossRock,  // камень босса — перепрыгнуть
        BossFire,  // огонь босса — присесть
        BossOrb,   // энергошар босса — отбить ударом обратно
        Gem,       // самоцвет у земли — присесть и подобрать (бонус)
        Mystery    // блок «?» — вблизи превращается в шипы, мышь или слизня: надо успеть среагировать
    }

    /// <summary>
    /// Препятствие, враг или бонус. Знает, каким движением его проходят,
    /// как быстро летит и как выглядит.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        const float ReflectSpeed = 18f;

        public ObstacleKind Kind { get; private set; }
        public MotionType Required { get; private set; }
        /// <summary>Бонус: за пропуск жизнь не отнимается.</summary>
        public bool IsBonus { get; private set; }
        /// <summary>Исчезает при успехе (враг побеждён, монета собрана).</summary>
        public bool BreaksOnSuccess { get; private set; }
        /// <summary>На каком расстоянии от игрока проверяется движение.</summary>
        public float ContactDistance { get; private set; }
        /// <summary>Уже проверено (успех или ошибка).</summary>
        public bool Resolved { get; set; }
        /// <summary>Шар отбит и летит обратно в босса.</summary>
        public bool Reflected { get; private set; }

        SpriteRenderer view;
        SpriteRenderer cue;
        float baseY;
        float waveHeight;
        float waveSpeed;
        bool hops;
        float age;
        bool hidden;           // блок «?» ещё не раскрылся
        ObstacleKind disguise;  // во что он превратится

        /// <summary>Во сколько раз препятствие быстрее земли.</summary>
        public static float SpeedFactorOf(ObstacleKind kind)
        {
            switch (kind)
            {
                case ObstacleKind.Log: return 1.35f;
                case ObstacleKind.Bat: return 1.25f;
                case ObstacleKind.Fireball: return 1.6f;
                default: return 1f;
            }
        }

        public static bool IsBonusKind(ObstacleKind kind)
        {
            return kind == ObstacleKind.Coin || kind == ObstacleKind.Heart || kind == ObstacleKind.Gem;
        }

        public static Obstacle Create(ObstacleKind kind, Vector3 groundPoint)
        {
            var go = new GameObject("Obstacle_" + kind);
            go.transform.position = groundPoint;

            var o = go.AddComponent<Obstacle>();
            o.Kind = kind;
            o.IsBonus = IsBonusKind(kind);
            o.ContactDistance = 0.8f;

            if (kind == ObstacleKind.Mystery)
            {
                ObstacleKind[] options = { ObstacleKind.Spikes, ObstacleKind.Bat, ObstacleKind.Slime };
                o.disguise = options[Random.Range(0, options.Length)];
                o.hidden = true;
                o.Required = RequiredFor(o.disguise);
                o.ContactDistance = o.disguise == ObstacleKind.Slime ? 1.2f : 0.8f;
                o.BreaksOnSuccess = o.disguise == ObstacleKind.Slime;
                o.AddView("mystery", 2, 3f, 0.1f);
                return o;
            }

            o.Configure(kind);
            return o;
        }

        static MotionType RequiredFor(ObstacleKind kind)
        {
            switch (kind)
            {
                case ObstacleKind.Bat: return MotionType.Squat;
                case ObstacleKind.Slime: return MotionType.Punch;
                default: return MotionType.Jump;
            }
        }

        /// <summary>Внешний вид и правила препятствия данного вида.</summary>
        void Configure(ObstacleKind kind)
        {
            Obstacle o = this;
            GameObject go = gameObject;
            string cueName = null;
            float cueY = 2f;

            switch (kind)
            {
                case ObstacleKind.Spikes:
                    o.Required = MotionType.Jump;
                    o.AddView("spikes", 1, 0f, 0f);
                    cueName = "cue_jump_0"; cueY = 1.5f;
                    break;
                case ObstacleKind.Log:
                    o.Required = MotionType.Jump;
                    o.AddView("log", 4, 12f, 0f);
                    cueName = "cue_jump_0"; cueY = 1.5f;
                    break;
                case ObstacleKind.BossRock:
                    o.Required = MotionType.Jump;
                    o.AddView("rock", 4, 12f, 0f);
                    cueName = "cue_jump_0"; cueY = 1.5f;
                    break;
                case ObstacleKind.Bat:
                    o.Required = MotionType.Squat;
                    o.AddView("bat", 2, 8f, 1.5f);
                    o.waveHeight = 0.15f; o.waveSpeed = 7f;
                    cueName = "cue_squat_0"; cueY = 2.9f;
                    break;
                case ObstacleKind.Fireball:
                case ObstacleKind.BossFire:
                    o.Required = MotionType.Squat;
                    o.AddView("fireball", 2, 10f, 1.6f);
                    cueName = "cue_squat_0"; cueY = 2.9f;
                    break;
                case ObstacleKind.Slime:
                    o.Required = MotionType.Punch;
                    o.BreaksOnSuccess = true;
                    o.ContactDistance = 1.2f;
                    o.AddView("slime", 2, 4f, 0f);
                    o.waveHeight = 0.3f; o.waveSpeed = 6f; o.hops = true;
                    cueName = "cue_punch_0"; cueY = 1.7f;
                    break;
                case ObstacleKind.Crate:
                    o.Required = MotionType.Punch;
                    o.BreaksOnSuccess = true;
                    o.ContactDistance = 1.8f;
                    o.AddView("crate", 1, 0f, 0f);
                    cueName = "cue_punch_0"; cueY = 2.5f;
                    break;
                case ObstacleKind.BossOrb:
                    o.Required = MotionType.Punch;
                    o.ContactDistance = 1.2f;
                    o.AddView("orb", 2, 8f, 0.7f);
                    o.waveHeight = 0.1f; o.waveSpeed = 8f;
                    cueName = "cue_punch_0"; cueY = 2.2f;
                    break;
                case ObstacleKind.Vine:
                    o.Required = MotionType.HandsUp;
                    o.ContactDistance = 1.2f;
                    o.AddView("vine", 1, 0f, 2.5f);
                    Art.Spawn("pit_0", go.transform, new Vector2(0f, -2f), -40);
                    cueName = "cue_hands_0"; cueY = 1.3f;
                    break;
                case ObstacleKind.Coin:
                    o.Required = MotionType.HandsUp;
                    o.BreaksOnSuccess = true;
                    o.ContactDistance = 0.4f;
                    o.AddView("coin", 4, 10f, 2.7f);
                    break;
                case ObstacleKind.Heart:
                    o.Required = MotionType.HandsUp;
                    o.BreaksOnSuccess = true;
                    o.ContactDistance = 0.4f;
                    o.AddView("heart", 1, 0f, 2.7f);
                    o.waveHeight = 0.15f; o.waveSpeed = 5f;
                    cueName = "cue_hands_0"; cueY = 4f;
                    break;
                case ObstacleKind.Gem:
                    o.Required = MotionType.Squat;
                    o.BreaksOnSuccess = true;
                    o.ContactDistance = 0.4f;
                    o.AddView("gem", 4, 6f, 0.25f);
                    o.waveHeight = 0.12f; o.waveSpeed = 4f;
                    cueName = "cue_squat_0"; cueY = 1.6f;
                    break;
            }

            if (cueName != null)
                o.cue = Art.Spawn(cueName, go.transform, new Vector2(0f, cueY), 25);
        }

        /// <summary>Блок «?» раскрывается: превращается в настоящее препятствие.</summary>
        void Reveal()
        {
            hidden = false;
            if (view != null) Destroy(view.gameObject);
            view = null;
            waveHeight = 0f;
            hops = false;
            Configure(disguise);
            Fx.Poof(transform.position + Vector3.up * 0.6f);
            Sfx.Play("click", 0.5f);
        }

        void AddView(string art, int frameCount, float fps, float y)
        {
            baseY = y;
            view = Art.Spawn(art + "_0", transform, new Vector2(0f, y), 5);
            if (frameCount > 1)
            {
                var animator = view.gameObject.AddComponent<SpriteAnimator>();
                animator.Init(view);
                animator.Play(Art.Frames(art, frameCount), fps);
            }
        }

        /// <summary>Сдвиг за кадр. distance — путь, который прошла земля.</summary>
        public void Advance(float distance, float deltaTime)
        {
            age += deltaTime;

            if (Reflected)
                transform.position += Vector3.right * (ReflectSpeed * deltaTime);
            else
                transform.position += Vector3.left * (distance * SpeedFactorOf(Kind));

            // Блок «?» раскрывается примерно за секунду до встречи с героем
            if (hidden && deltaTime > 0f)
            {
                float speed = distance / deltaTime;
                if (transform.position.x - GameManager.PlayerX < Mathf.Max(3f, speed * 1.1f)) Reveal();
            }

            if (view != null && waveHeight > 0f)
            {
                float wave = Mathf.Sin(age * waveSpeed);
                if (hops) wave = Mathf.Abs(wave);
                view.transform.localPosition = new Vector3(0f, Art.Snap(baseY + wave * waveHeight), 0f);
            }

            if (cue != null)
            {
                cue.enabled = !Resolved && (int)(age * 6f) % 4 != 3;   // лёгкое мигание
            }
        }

        public void MarkFailed()
        {
            if (view != null) view.color = new Color(0.55f, 0.55f, 0.6f);
        }

        /// <summary>Отбить шар обратно в босса.</summary>
        public void Reflect()
        {
            Reflected = true;
            if (view != null) view.flipX = true;
        }
    }
}
