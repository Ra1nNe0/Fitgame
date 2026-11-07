using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Окно камеры: картинка с веб-камеры и поверх неё «скелет» — точки тела, которые нашла нейросеть.
    /// Используется на экране настройки камеры, в тесте AI, в обучении и мини-окном во время игры.
    /// </summary>
    public class SkeletonView
    {
        static readonly int[,] Bones =
        {
            { 11, 12 }, { 11, 13 }, { 13, 15 }, { 12, 14 }, { 14, 16 }, { 11, 23 }, { 12, 24 },
            { 23, 24 }, { 23, 25 }, { 25, 27 }, { 24, 26 }, { 26, 28 }
        };
        static readonly int[] Joints = { 0, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };

        public RectTransform Root { get; private set; }

        readonly RawImage preview;
        readonly RectTransform area;
        readonly Image[] bones = new Image[12];
        readonly Image[] joints = new Image[13];
        readonly Text status;
        readonly Vector2 size;

        public SkeletonView(Transform parent, Vector2 position, float width, Vector2? anchor = null)
        {
            size = new Vector2(width, width * 0.75f);
            Image frame = Ui.Panel(parent, position, size + new Vector2(36f, 36f), false, anchor);
            Root = frame.rectTransform;

            RectTransform holder = Ui.Node(Root, "Preview", Vector2.zero, size);
            holder.gameObject.AddComponent<Image>().color = Ui.Ink;
            preview = Ui.Node(holder, "Camera", Vector2.zero, size).gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false;
            area = Ui.Node(holder, "Skeleton", Vector2.zero, size);

            float thickness = Mathf.Max(4f, width / 90f);
            for (int i = 0; i < bones.Length; i++)
            {
                RectTransform r = Ui.Node(area, "Bone", Vector2.zero, new Vector2(10f, thickness), new Vector2(0.5f, 0.5f));
                r.pivot = new Vector2(0f, 0.5f);
                bones[i] = r.gameObject.AddComponent<Image>();
                bones[i].color = Ui.Cyan;
                bones[i].raycastTarget = false;
            }
            for (int i = 0; i < joints.Length; i++)
            {
                float s = i == 0 ? thickness * 4f : thickness * 2f;
                joints[i] = Ui.Box(area, Vector2.zero, new Vector2(s, s), i == 0 ? Ui.Gold : Color.white);
            }
            status = Ui.Label(holder, "", width > 500f ? 24 : 16, Color.white, Vector2.zero, size - new Vector2(40f, 40f));
        }

        public void SetActive(bool on)
        {
            Root.gameObject.SetActive(on);
        }

        public void Refresh(PoseLink link, PoseMotionClassifier classifier)
        {
            bool connected = link != null && link.Connected;
            preview.texture = connected ? link.Preview : null;
            preview.color = connected && link.Preview != null ? Color.white : new Color(0, 0, 0, 0);

            PoseFrame f = connected ? link.Latest : null;
            bool pose = f != null && f.HasPose;

            Color boneColor = Ui.Cyan;
            if (classifier != null && classifier.State == TrackerState.Calibrating) boneColor = Ui.Amber;
            if (classifier != null && classifier.Active != MotionType.None) boneColor = Ui.Green;

            for (int i = 0; i < bones.Length; i++)
            {
                int a = Bones[i, 0], b = Bones[i, 1];
                bool on = pose && Inside(f, a) && Inside(f, b);
                bones[i].enabled = on;
                if (!on) continue;
                Vector2 pa = Point(f, a), pb = Point(f, b);
                Vector2 d = pb - pa;
                RectTransform r = bones[i].rectTransform;
                r.anchoredPosition = pa;
                r.sizeDelta = new Vector2(d.magnitude, r.sizeDelta.y);
                r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                bones[i].color = boneColor;
            }
            for (int i = 0; i < joints.Length; i++)
            {
                int k = Joints[i];
                bool on = pose && Inside(f, k);
                joints[i].enabled = on;
                if (on) joints[i].rectTransform.anchoredPosition = Point(f, k);
            }

            if (!connected) status.text = link != null ? link.ServerStatus.ToUpper() : "НЕТ КАМЕРЫ";
            else if (!pose) status.text = "НЕ ВИЖУ ЧЕЛОВЕКА";
            else status.text = "";
        }

        /// <summary>Точка видна и лежит в кадре (за рамку окна не рисуем).</summary>
        static bool Inside(PoseFrame f, int i)
        {
            float x = f.Image[i * 3], y = f.Image[i * 3 + 1];
            return f.Image[i * 3 + 2] > 0.5f && x >= 0f && x <= 1f && y >= 0f && y <= 1f;
        }

        Vector2 Point(PoseFrame f, int index)
        {
            float x = f.Image[index * 3], y = f.Image[index * 3 + 1];
            return new Vector2((x - 0.5f) * size.x, (0.5f - y) * size.y);
        }
    }
}
