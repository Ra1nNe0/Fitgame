using UnityEngine;

namespace FitQuest
{
    /// <summary>Короткие визуальные эффекты.</summary>
    public static class Fx
    {
        /// <summary>Вспышка-«пуф» на месте разбитого ящика, врага или собранной монеты.</summary>
        public static void Poof(Vector3 position)
        {
            SpriteRenderer renderer = Art.Spawn("poof_0", null, position, 30, true);
            renderer.transform.localScale = new Vector3(2f, 2f, 1f);
            var animator = renderer.gameObject.AddComponent<SpriteAnimator>();
            animator.Init(renderer);
            animator.Play(Art.Frames("poof", 3, true), 14f, false);
            Object.Destroy(renderer.gameObject, 0.25f);
        }

        /// <summary>Облачко пыли из-под ног; worldSpeed — скорость земли (пыль остаётся позади).</summary>
        public static void Dust(Vector3 position, float worldSpeed)
        {
            Drifter.Spawn(Art.Frames("dust", 3, true), position, new Vector3(-worldSpeed * 0.8f, 0.4f, 0f), 0.4f, 6,
                          new Color(1f, 1f, 1f, 0.8f));
        }

        /// <summary>Искры вокруг точки (сбор самоцвета, режим «Фивер»).</summary>
        public static void Sparkle(Vector3 position, int count, float spread = 0.8f)
        {
            Sprite[] frames = Art.Frames("particle_spark", 2, true);
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                Drifter.Spawn(frames, position + (Vector3)(dir * Random.Range(0f, spread * 0.3f)),
                              (Vector3)(dir * Random.Range(1.5f, 3.5f) * spread), Random.Range(0.3f, 0.6f), 31,
                              new Color(1f, 0.95f, 0.6f, 1f), 1.5f);
            }
        }
    }
}
