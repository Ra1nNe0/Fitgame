using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Частицы окружения, которые оживляют фон: в лесу падают листья, в пещере мерцают
    /// кристальные искры, над вулканом поднимаются угольки, в замке кружат пылинки в свете факелов.
    /// Частицы двигаются вместе с миром (параллакс) и бесконечно переиспользуются.
    /// </summary>
    public class Ambient : MonoBehaviour
    {
        const int Count = 18;
        const float HalfWidth = 10.5f, Top = 5.8f, Bottom = -3.2f;

        class Mote
        {
            public SpriteRenderer View;
            public Vector2 Velocity;
            public float Phase;
            public float Depth;     // 0.4 — далеко (медленно и тускло), 1 — близко
        }

        readonly Mote[] motes = new Mote[Count];
        Sprite[] frames;
        string world;

        void Awake()
        {
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Mote");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = -60;
                if (Shapes.UnlitMaterial != null) r.sharedMaterial = Shapes.UnlitMaterial;
                motes[i] = new Mote { View = r };
            }
        }

        public void SetWorld(string art)
        {
            if (art == world) return;
            world = art;
            string kind = art == "forest" ? "leaf" : art == "cave" ? "spark" : art == "volcano" ? "ember" : "mote";
            frames = Art.Frames("particle_" + kind, 2, true);
            foreach (Mote m in motes) Respawn(m, true);
        }

        void Respawn(Mote m, bool anywhere)
        {
            m.Depth = Random.Range(0.4f, 1f);
            m.Phase = Random.Range(0f, 10f);
            float x = anywhere ? Random.Range(-HalfWidth, HalfWidth) : HalfWidth;
            float y = Random.Range(Bottom, Top);
            switch (world)
            {
                case "forest":   // листья падают и кружатся
                    m.Velocity = new Vector2(Random.Range(-0.6f, -0.2f), Random.Range(-0.9f, -0.5f));
                    if (!anywhere) { x = Random.Range(-HalfWidth, HalfWidth); y = Top; }
                    break;
                case "volcano":  // угольки летят вверх
                    m.Velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.4f));
                    if (!anywhere) { x = Random.Range(-HalfWidth, HalfWidth); y = Bottom; }
                    break;
                default:         // искры и пылинки висят в воздухе
                    m.Velocity = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.15f));
                    break;
            }
            m.View.transform.localPosition = new Vector3(x, y, 0f);
            float s = 0.6f + m.Depth * 0.6f;
            m.View.transform.localScale = new Vector3(s, s, 1f);
            m.View.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * m.Depth);
        }

        /// <summary>Мир сдвинулся на distance (как земля) — частицы уезжают вместе с ним.</summary>
        public void Scroll(float distance)
        {
            if (frames == null) return;
            foreach (Mote m in motes)
                m.View.transform.localPosition += Vector3.left * (distance * 0.6f * m.Depth);
        }

        void Update()
        {
            if (frames == null) return;
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            foreach (Mote m in motes)
            {
                Vector3 p = m.View.transform.localPosition;
                float sway = Mathf.Sin(t * 1.7f + m.Phase) * (world == "forest" ? 0.8f : 0.25f);
                p += new Vector3(m.Velocity.x + sway, m.Velocity.y, 0f) * (dt * m.Depth);
                m.View.transform.localPosition = p;

                // мерцание: кадры меняются, искры и пылинки то гаснут, то вспыхивают
                m.View.sprite = frames[(int)(t * 3f + m.Phase) % 2];
                if (world == "cave" || world == "castle")
                {
                    float a = (0.35f + 0.65f * m.Depth) * (0.55f + 0.45f * Mathf.Sin(t * 3f + m.Phase * 2f));
                    m.View.color = new Color(1f, 1f, 1f, a);
                }

                if (p.x < -HalfWidth || p.x > HalfWidth + 0.5f || p.y < Bottom - 0.5f || p.y > Top + 0.5f)
                    Respawn(m, false);
            }
        }
    }
}
