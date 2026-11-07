using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>Окно поверх всего: вопрос, подтверждение, демо-оплата.</summary>
    public class Modal
    {
        readonly RectTransform root;
        readonly Text title, body;
        readonly Button ok, cancel;
        Action onOk, onCancel;
        float busyTime;
        Action afterBusy;

        public bool Visible { get { return root.gameObject.activeSelf; } }

        public Modal(Transform parent)
        {
            root = Ui.Stretch(parent, "Modal");
            Ui.Dim(root, 0.75f);
            RectTransform panel = Ui.Panel(root, Vector2.zero, new Vector2(1100f, 600f)).rectTransform;
            title = Ui.Label(panel, "", 40, Ui.Gold, new Vector2(0f, 220f), new Vector2(1000f, 60f));
            body = Ui.Label(panel, "", 24, Color.white, new Vector2(0f, 30f), new Vector2(960f, 280f));
            ok = Ui.Button(panel, "OK", new Vector2(-230f, -210f), new Vector2(420f, 100f), Ui.Green, () => Click(true), 24);
            cancel = Ui.Button(panel, "ОТМЕНА", new Vector2(230f, -210f), new Vector2(420f, 100f), Ui.Slate, () => Click(false), 24);
            root.gameObject.SetActive(false);
        }

        public void Show(string titleText, string bodyText, string okText, Action okAction,
                         string cancelText = null, Action cancelAction = null)
        {
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            title.text = titleText;
            body.text = bodyText;
            onOk = okAction;
            onCancel = cancelAction;
            busyTime = 0f;
            ok.gameObject.SetActive(true);
            Ui.SetButtonText(ok, okText);
            bool hasCancel = cancelText != null;
            cancel.gameObject.SetActive(hasCancel);
            if (hasCancel) Ui.SetButtonText(cancel, cancelText);
            ok.GetComponent<RectTransform>().anchoredPosition = new Vector2(hasCancel ? -230f : 0f, -210f);
        }

        /// <summary>Окно без кнопок на несколько секунд (например, «идёт оплата...»).</summary>
        public void Busy(string titleText, string bodyText, float seconds, Action then)
        {
            Show(titleText, bodyText, "", null);
            ok.gameObject.SetActive(false);
            busyTime = seconds;
            afterBusy = then;
        }

        public void Close()
        {
            root.gameObject.SetActive(false);
        }

        void Click(bool yes)
        {
            Action action = yes ? onOk : onCancel;
            Close();
            if (action != null) action();
        }

        public void Tick()
        {
            if (!Visible) return;
            if (busyTime > 0f)
            {
                busyTime -= Time.unscaledDeltaTime;
                if (busyTime <= 0f && afterBusy != null)
                {
                    Action a = afterBusy;
                    afterBusy = null;
                    a();
                }
                return;
            }
            if (GameKeys.Confirm && ok.gameObject.activeSelf) Click(true);
            else if (GameKeys.Pause) Click(false);
        }
    }

    /// <summary>Всплывающие уведомления сверху (достижения, покупки).</summary>
    public class Toasts
    {
        struct Item
        {
            public string Title, Text, Icon;
        }

        readonly RectTransform panel;
        readonly Text title, text;
        readonly Image icon;
        readonly Queue<Item> queue = new Queue<Item>();
        float time;

        public Toasts(Transform parent)
        {
            panel = Ui.Panel(parent, new Vector2(0f, 200f), new Vector2(900f, 150f), false, new Vector2(0.5f, 1f)).rectTransform;
            icon = Ui.Icon(panel, "icon_trophy", new Vector2(-370f, 0f), 96f);
            title = Ui.Label(panel, "", 24, Ui.Gold, new Vector2(60f, 24f), new Vector2(720f, 40f), TextAnchor.MiddleLeft);
            text = Ui.Label(panel, "", 16, Color.white, new Vector2(60f, -24f), new Vector2(720f, 40f), TextAnchor.MiddleLeft);
            panel.gameObject.SetActive(false);
        }

        public void Push(string titleText, string bodyText, string iconName)
        {
            queue.Enqueue(new Item { Title = titleText, Text = bodyText, Icon = iconName });
        }

        public void Tick()
        {
            if (time <= 0f)
            {
                if (queue.Count == 0)
                {
                    panel.gameObject.SetActive(false);
                    return;
                }
                Item item = queue.Dequeue();
                title.text = item.Title;
                text.text = item.Text;
                icon.sprite = Art.Ui(item.Icon);
                panel.gameObject.SetActive(true);
                panel.SetAsLastSibling();
                time = 3f;
            }

            time -= Time.unscaledDeltaTime;
            // выезжает сверху и уезжает обратно
            float shown = Mathf.Min(1f, Mathf.Min(3f - time, time) * 4f);
            panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(200f, -30f, shown));
        }
    }
}
