using UnityEngine;

namespace FitQuest
{
    /// <summary>
    /// Основа для экранов (меню, магазин, настройки...). Экран — обычный класс,
    /// которым управляет App: создаёт, показывает, прячет и каждый кадр вызывает Tick.
    /// </summary>
    public abstract class UiScreen
    {
        public RectTransform Root { get; private set; }
        protected App App { get; private set; }

        public bool Visible { get { return Root != null && Root.gameObject.activeSelf; } }

        public void Create(App app, Transform parent)
        {
            App = app;
            Root = Ui.Stretch(parent, GetType().Name);
            Build();
            Root.gameObject.SetActive(false);
        }

        protected abstract void Build();

        public void Show()
        {
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            OnShow();
        }

        public void Hide()
        {
            if (!Visible) return;
            OnHide();
            Root.gameObject.SetActive(false);
        }

        public virtual void OnShow() { }
        public virtual void OnHide() { }
        /// <summary>Каждый кадр, пока экран виден (время — без учёта паузы).</summary>
        public virtual void Tick() { }
        /// <summary>Нажата Esc. Вернуть true, если экран обработал её сам.</summary>
        public virtual bool OnBack() { return false; }
        /// <summary>Движение игрока (с камеры или клавиатуры), пока экран виден.</summary>
        public virtual void OnMotion(MotionEvent motion) { }
    }
}
