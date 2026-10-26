using UnityEngine;
using UnityEngine.Serialization;

namespace Bufe.UI
{
    /// <summary>
    /// Base for one page of the laptop panel (Overview, Stock, Restock, …). LaptopPanel binds
    /// itself on init and calls Show/Hide when the side menu switches pages.
    /// </summary>
    public abstract class LaptopSection : MonoBehaviour
    {
        [Tooltip("Root toggled on show/hide. Default: this.")]
        [FormerlySerializedAs("screenRoot")]
        [SerializeField] private GameObject sectionRoot;

        protected LaptopPanel Panel { get; private set; }

        public bool IsShown { get; private set; }

        private GameObject Root => sectionRoot != null ? sectionRoot : gameObject;

        public void Bind(LaptopPanel panel)
        {
            Panel = panel;
            OnBound();
        }

        public void Show()
        {
            IsShown = true;
            Root.SetActive(true);
            OnShown();
        }

        public void Hide()
        {
            IsShown = false;
            Root.SetActive(false);
            OnHidden();
        }

        /// <summary>Called once when the panel binds this section. Wire buttons here.</summary>
        protected virtual void OnBound() { }

        /// <summary>Called every time the page becomes visible. Rebuild from live data here.</summary>
        protected virtual void OnShown() { }

        protected virtual void OnHidden() { }
    }
}
