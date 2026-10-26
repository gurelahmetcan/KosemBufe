using DG.Tweening;
using UnityEngine;
using TMPro;

namespace Bufe.UI
{
    /// <summary>Short one-line feedback inside the laptop panel ("Order placed").</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LaptopToast : MonoBehaviour
    {
        public TextMeshProUGUI messageText;

        [Min(0.1f)] public float holdSeconds = 2f;
        [Min(0f)]   public float fadeSeconds = 0.25f;

        private CanvasGroup _group;
        private Sequence    _seq;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha          = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnDisable()
        {
            _seq?.Kill();
            if (_group != null) _group.alpha = 0f;
        }

        public void Show(string message)
        {
            if (messageText != null) messageText.text = message;

            _seq?.Kill();
            _seq = DOTween.Sequence()
                .Append(_group.DOFade(1f, fadeSeconds))
                .AppendInterval(holdSeconds)
                .Append(_group.DOFade(0f, fadeSeconds))
                .SetUpdate(true);
        }
    }
}
