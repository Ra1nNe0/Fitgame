using UnityEngine;
using UnityEngine.EventSystems;

namespace FitQuest
{
    /// <summary>Кнопка чуть увеличивается под курсором.</summary>
    public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        bool over;

        public void OnPointerEnter(PointerEventData eventData) { over = true; }
        public void OnPointerExit(PointerEventData eventData) { over = false; }

        void OnDisable()
        {
            over = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float target = over ? 1.05f : 1f;
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
