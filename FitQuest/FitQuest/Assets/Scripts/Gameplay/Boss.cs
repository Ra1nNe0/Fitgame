using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Босс уровня. Стоит справа и бросает снаряды (их выпускает ObstacleSpawner).
    /// Урон получает только от отбитых игроком энергошаров.
    /// </summary>
    public class Boss : MonoBehaviour
    {
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool Alive { get { return Hp > 0; } }

        SpriteRenderer view;
        Vector3 home;
        float hoverHeight;
        float time;
        float throwTime;
        float hitTime;
        float deathTime;

        public static Boss Create(string art, int hp, float hoverHeight, Vector3 home)
        {
            var go = new GameObject("Boss");
            var boss = go.AddComponent<Boss>();
            boss.Hp = hp;
            boss.MaxHp = hp;
            boss.home = home;
            boss.hoverHeight = hoverHeight;
            go.transform.position = home + Vector3.right * 10f;   // появляется из-за правого края

            boss.view = Art.Spawn(art + "_0", go.transform, Vector2.zero, 6);
            var animator = boss.view.gameObject.AddComponent<SpriteAnimator>();
            animator.Init(boss.view);
            animator.Play(Art.Frames(art, 2), 3f);
            return boss;
        }

        /// <summary>Короткий рывок вперёд в момент броска.</summary>
        public void Throw()
        {
            throwTime = 0.25f;
        }

        public void TakeHit()
        {
            if (!Alive) return;
            Hp--;
            hitTime = 0.5f;
            if (Hp <= 0) deathTime = 0.01f;
        }

        void Update()
        {
            if (view == null) return;
            time += Time.deltaTime;
            if (throwTime > 0f) throwTime -= Time.deltaTime;
            if (hitTime > 0f) hitTime -= Time.deltaTime;

            Vector3 target = home;
            target.y += hoverHeight + (hoverHeight > 0f ? Mathf.Sin(time * 2f) * 0.3f : 0f);
            if (throwTime > 0f) target.x -= 0.5f;
            if (hitTime > 0f) target.x += 0.4f;

            if (deathTime > 0f)
            {
                // Поражение: мигает и уходит вниз.
                deathTime += Time.deltaTime;
                target.y -= deathTime * 3f;
                view.enabled = (int)(deathTime * 16f) % 2 == 0;
                if (deathTime > 2f) view.enabled = false;
            }

            Vector3 p = Vector3.Lerp(transform.position, target, Mathf.Min(1f, Time.deltaTime * 6f));
            transform.position = new Vector3(Art.Snap(p.x), Art.Snap(p.y), 0f);

            view.color = hitTime > 0f && (int)(hitTime * 20f) % 2 == 0
                ? new Color(1f, 0.4f, 0.4f)
                : Color.white;
        }
    }
}
