using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Bufe.UI
{
    public class EventMemberUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Optional — hidden if no portrait sprite is set on FamilyMemberData.")]
        public Image           portrait;
        public TextMeshProUGUI nameText;
        public Image           barFill;
        public TextMeshProUGUI valueText;

        [Header("Bar Colors")]
        public Color lowColor    = new Color(0.85f, 0.20f, 0.20f, 1f);
        public Color mediumColor = new Color(0.85f, 0.75f, 0.10f, 1f);
        public Color highColor   = new Color(0.20f, 0.75f, 0.20f, 1f);

        [Header("Floating Delta Text")]
        [Tooltip("Prefab that has a TextMeshProUGUI and a CanvasGroup on its root.")]
        public GameObject floatTextPrefab;
        public Transform  floatTextAnchor;

        // ── Setup ─────────────────────────────────────────────────────────────────

        public void Setup(FamilyMemberData data, int currentValue)
        {
            if (portrait != null)
            {
                bool hasSprite = data.portrait != null;
                portrait.gameObject.SetActive(hasSprite);
                if (hasSprite) portrait.sprite = data.portrait;
            }

            if (nameText != null) nameText.text = data.memberName.ToUpper();

            SetBarInstant(currentValue);
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Animates the relationship bar from oldValue to newValue.
        /// Returns the Sequence so the caller can track completion.
        /// </summary>
        public Sequence AnimateTo(int newValue, int oldValue)
        {
            float fillTarget  = newValue / 10f;
            Color targetColor = BarColor(newValue);
            int   delta       = newValue - oldValue;
            Color pulseColor  = delta > 0
                ? new Color(0.3f, 1f, 0.3f, 1f)
                : new Color(1f,  0.3f, 0.3f, 1f);

            var seq = DOTween.Sequence();

            if (barFill != null)
            {
                // Brief pulse colour, then settle to correct colour while fill animates
                seq.Append(barFill.DOColor(pulseColor, 0.15f).SetLoops(2, LoopType.Yoyo));
                seq.Join(barFill.DOFillAmount(fillTarget, 0.4f).SetEase(Ease.OutCubic));
                seq.Join(barFill.DOColor(targetColor, 0.4f));
            }

            if (valueText != null)
                seq.Join(DOTween.To(
                    () => oldValue,
                    x  => valueText.text = $"{x}/10",
                    newValue,
                    0.4f));

            SpawnFloatingDelta(delta);

            return seq;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void SetBarInstant(int value)
        {
            if (barFill  != null) { barFill.fillAmount = value / 10f; barFill.color = BarColor(value); }
            if (valueText != null) valueText.text = $"{value}/10";
        }

        private void SpawnFloatingDelta(int delta)
        {
            if (floatTextPrefab == null || floatTextAnchor == null) return;

            var go  = Instantiate(floatTextPrefab, floatTextAnchor.position, Quaternion.identity, transform.root);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            var cg  = go.GetComponent<CanvasGroup>();
            var rt  = go.GetComponent<RectTransform>();

            if (tmp != null)
            {
                tmp.text  = delta > 0 ? $"+{delta}" : $"{delta}";
                tmp.color = delta > 0 ? new Color(0.3f, 1f, 0.3f, 1f) : new Color(1f, 0.3f, 0.3f, 1f);
            }

            if (rt != null)
                rt.DOAnchorPosY(rt.anchoredPosition.y + 50f, 0.7f).SetEase(Ease.OutCubic);

            if (cg != null)
                cg.DOFade(0f, 0.7f).SetDelay(0.2f).OnComplete(() => Destroy(go));
        }

        private Color BarColor(int value)
        {
            if (value <= 3) return lowColor;
            if (value <= 6) return mediumColor;
            return highColor;
        }
    }
}
