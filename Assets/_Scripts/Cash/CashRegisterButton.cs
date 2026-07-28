using System.Collections;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// One physical key on the cash register. Each button is its own GameObject with a
    /// collider so it can be clicked individually. Clicking presses the key (or, if the
    /// player isn't operating the register yet, enters cashier mode first) and plays a
    /// small push-in/out animation.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CashRegisterButton : MonoBehaviour, IAssemblyInteractable
    {
        public enum Kind { Digit, Delete, Enter, Dot }

        [SerializeField] private CashRegister register;
        [SerializeField] private Kind kind = Kind.Digit;

        [Tooltip("Digit value 0–9. Used only when Kind = Digit.")]
        [Range(0, 9)]
        [SerializeField] private int digit;

        [Tooltip("Sound played on each key press. None = silent.")]
        [SerializeField] private Sfx clickSound = Sfx.Click;

        [Header("Press Animation")]
        [Tooltip("Local-space offset the key moves to when pressed (a small push into the body).")]
        [SerializeField] private Vector3 pressOffset = new Vector3(-0.003f, -0.003f, 0f);
        [Tooltip("Total seconds for the push-in and return.")]
        [SerializeField] private float pressDuration = 0.09f;

        private Vector3   _restLocalPos;
        private Coroutine _anim;

        private void Awake() => _restLocalPos = transform.localPosition;

        public bool CanInteract(PlayerHand hand)
            => register != null && register.IsArmed && hand.IsHandEmpty;

        public void Interact(PlayerHand hand)
        {
            // First click on the register (via a key) just steps up to it.
            if (!register.IsCashierMode)
            {
                register.EnterCashierMode();
                return;
            }

            switch (kind)
            {
                case Kind.Digit:  register.PressDigit(digit); break;
                case Kind.Delete: register.PressDelete();     break;
                case Kind.Enter:  register.PressEnter();      break;
                case Kind.Dot:    register.PressDot();        break;
            }

            if (clickSound != Sfx.None && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(clickSound);

            PlayPress();
        }

        private void PlayPress()
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(PressRoutine());
        }

        private IEnumerator PressRoutine()
        {
            Vector3 down = _restLocalPos + pressOffset;
            float   half = Mathf.Max(0.0001f, pressDuration * 0.5f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / half;
                transform.localPosition = Vector3.Lerp(_restLocalPos, down, Mathf.Clamp01(t));
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / half;
                transform.localPosition = Vector3.Lerp(down, _restLocalPos, Mathf.Clamp01(t));
                yield return null;
            }

            transform.localPosition = _restLocalPos;
            _anim = null;
        }
    }
}
