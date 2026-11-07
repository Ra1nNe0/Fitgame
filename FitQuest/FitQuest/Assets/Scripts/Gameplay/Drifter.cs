using UnityEngine;

namespace FitQuest
{
    /// <summary>Короткоживущий спрайт-эффект: летит с заданной скоростью, гаснет и исчезает.</summary>
    public class Drifter : MonoBehaviour
    {
        SpriteRenderer view;
        Vector3 velocity;
        float life, age;
        Color baseColor;

        public static Drifter Spawn(Sprite[] frames, Vector3 position, Vector3 velocity, float life, int order, Color color, float scale = 1f)
        {
            SpriteRenderer r = Art.Spawn(frames[0].texture.name, null, position, order, true);
            r.color = color;
            r.transform.localScale = new Vector3(scale, scale, 1f);
            if (frames.Length > 1)
            {
                var animator = r.gameObject.AddComponent<SpriteAnimator>();
                animator.Init(r);
                animator.Play(frames, frames.Length / life, false);
            }
            var d = r.gameObject.AddComponent<Drifter>();
            d.view = r;
            d.velocity = velocity;
            d.life = life;
            d.baseColor = color;
            return d;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            transform.position += velocity * Time.deltaTime;
            float k = 1f - age / life;
            view.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
        }
    }
}
