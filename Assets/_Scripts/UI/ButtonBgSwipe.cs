using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class WipeInHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
    [SerializeField] Image background;
    [SerializeField] float duration = 0.25f;
    [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    Coroutine current;
    
    public void OnPointerEnter(PointerEventData e) => Play(1f);
    public void OnPointerExit(PointerEventData e) => Play(0f);
    public void OnSelect(BaseEventData e) => Play(1f);       // gamepad/klavye focus
    public void OnDeselect(BaseEventData e) => Play(0f);
    
    void Play(float target) {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Animate(target));
    }
    
    IEnumerator Animate(float target) {
        float start = background.fillAmount;
        float t = 0f;
        while (t < duration) {
            t += Time.unscaledDeltaTime;
            float k = curve.Evaluate(t / duration);
            background.fillAmount = Mathf.Lerp(start, target, k);
            yield return null;
        }
        background.fillAmount = target;
    }
}