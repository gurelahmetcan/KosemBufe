using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Bufe
{
    /// <summary>
    /// The büfe cash register. Flow:
    ///   1. Order accepted → <see cref="SetOrder"/> shows the amount to charge (keypad locked).
    ///   2. Food fully delivered → <see cref="Arm"/> enables the keypad and stores the pay callback.
    ///   3. Player clicks the register → enters "cashier mode": teleported to the stance point,
    ///      movement locked, keypad live. Digits build up on the screen; Enter only succeeds when
    ///      the typed number equals the amount; Delete fixes mistakes.
    ///   4. Correct entry → pays (callback), clears, releases the player. Right-click steps away
    ///      without paying - the amount stays pending until the player comes back.
    /// </summary>
    public class CashRegister : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("Display")]
        [Tooltip("Screen. Top: amount due. Bottom: typed input.")]
        [SerializeField] private TextMeshProUGUI display;
        [SerializeField] private string currencyPrefix = "₺";

        [Tooltip("Balance shown on the screen while idle.")]
        [SerializeField] private MoneyManager moneyManager;

        [Header("Player Stance")]
        [Tooltip("Where the player stands to operate.")]
        [SerializeField] private Transform playerStance;
        [Tooltip("Player root (has the CharacterController).")]
        [SerializeField] private Transform player;
        [Tooltip("Disabled while operating.")]
        [SerializeField] private PlayerMovement playerMovement;

        [Tooltip("Turning is limited while operating.")]
        [SerializeField] private PlayerLook playerLook;

        [Header("Cashier View")]
        [Tooltip("Click-to-use colliders, off while operating. Auto-filled.")]
        [SerializeField] private Collider[] bodyColliders;

        [Tooltip("Max yaw (degrees) while operating. Pitch stays free.")]
        [SerializeField] private float lookYawRange = 15f;

        [Header("Hover")]
        [SerializeField] private string hoverLabelText = "Cash Register";

        // ── Runtime ──────────────────────────────────────────────────────────────
        private string _targetText = "";   // formatted price to charge, e.g. "12.00"
        private string _input      = "";
        private bool   _hasOrder;      // an amount is displayed
        private bool   _armed;         // keypad accepts entry (order fully delivered)
        private bool   _cashierMode;   // player is operating the register
        private Action _onPaid;

        public bool IsArmed       => _armed;
        public bool IsCashierMode => _cashierMode;

        private void Awake()
        {
            if (bodyColliders == null || bodyColliders.Length == 0)
                bodyColliders = GetComponents<Collider>();
        }

        private void OnEnable()
        {
            if (moneyManager != null) moneyManager.OnBalanceChanged += HandleBalanceChanged;
        }

        private void OnDisable()
        {
            if (moneyManager != null) moneyManager.OnBalanceChanged -= HandleBalanceChanged;
        }

        private void Start() => RefreshDisplay();   // show the balance right away

        private void HandleBalanceChanged(float balance, float delta) => RefreshDisplay();

        // ── Order lifecycle (called by OrderManager) ──────────────────────────────

        /// <summary>Order accepted: show the amount to charge (e.g. "12.00"). Keypad stays locked until Arm().</summary>
        public void SetOrder(float amount)
        {
            _targetText = amount.ToString("F2");
            _input      = "";
            _armed      = false;
            _hasOrder   = true;
            RefreshDisplay();
        }

        /// <summary>Order fully delivered: enable the keypad. onPaid fires on correct entry.</summary>
        public void Arm(Action onPaid)
        {
            _onPaid = onPaid;
            _armed  = true;
            RefreshDisplay();
        }

        /// <summary>Reset everything (paid, or the order/customer went away).</summary>
        public void Clear()
        {
            if (_cashierMode) ExitCashierMode();
            _hasOrder   = false;
            _armed      = false;
            _onPaid     = null;
            _input      = "";
            _targetText = "";
            RefreshDisplay();
        }

        // ── IAssemblyInteractable (click the register to operate it) ──────────────

        public bool CanInteract(PlayerHand hand)
            => _armed && !_cashierMode && hand.IsHandEmpty;

        public void Interact(PlayerHand hand) => EnterCashierMode();

        public string HoverLabel => (_armed && !_cashierMode) ? hoverLabelText : null;

        // ── Cashier mode ──────────────────────────────────────────────────────────

        /// <summary>Snaps the player to the stance point and locks movement. Public so a
        /// keypad button click can also enter cashier mode (not just the body).</summary>
        public void EnterCashierMode()
        {
            if (_cashierMode || !_armed) return;
            _cashierMode = true;

            if (player != null && playerStance != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.SetPositionAndRotation(playerStance.position,
                    Quaternion.Euler(0f, playerStance.eulerAngles.y, 0f));
                if (cc != null) cc.enabled = true;
            }

            if (playerMovement != null) playerMovement.enabled = false;

            // Free the raycast so clicks reach the keypad, and pin the player's view.
            SetBodyCollidersEnabled(false);
            if (playerLook != null)
                playerLook.BeginLookConstraint(lookYawRange);
        }

        private void ExitCashierMode()
        {
            _cashierMode = false;
            if (playerMovement != null) playerMovement.enabled = true;

            SetBodyCollidersEnabled(true);
            if (playerLook != null) playerLook.ClearLookConstraint();
        }

        private void SetBodyCollidersEnabled(bool on)
        {
            if (bodyColliders == null) return;
            foreach (Collider c in bodyColliders)
                if (c != null) c.enabled = on;
        }

        private void Update()
        {
            if (!_cashierMode) return;

            // Right-click steps away from the register without paying.
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                ExitCashierMode();
        }

        // ── Keypad (called by CashRegisterButton) ─────────────────────────────────

        public void PressDigit(int d)
        {
            if (!_cashierMode || !_armed) return;
            if (_input.Length >= 9) return;      // sane cap
            if (_input == "0") _input = "";      // no leading zeros
            _input += Mathf.Clamp(d, 0, 9).ToString();
            RefreshDisplay();
        }

        /// <summary>Appends a decimal point (only one allowed).</summary>
        public void PressDot()
        {
            if (!_cashierMode || !_armed) return;
            if (_input.Contains(".")) return;    // one decimal point only
            if (_input.Length >= 9) return;
            if (_input.Length == 0) _input = "0"; // ".5" → "0.5"
            _input += ".";
            RefreshDisplay();
        }

        public void PressDelete()
        {
            if (!_cashierMode || !_armed) return;
            if (_input.Length > 0) _input = _input.Substring(0, _input.Length - 1);
            RefreshDisplay();
        }

        public void PressEnter()
        {
            if (!_cashierMode || !_armed) return;
            if (_input != _targetText) return;   // must match exactly, e.g. "12.00"

            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.CashRegister);
            
            Action cb = _onPaid;
            Clear();                             // resets state + exits cashier mode
            cb?.Invoke();                        // OrderManager pays and sends the customer off
        }

        // ── Display ───────────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (display == null) return;

            if (!_hasOrder)
            {
                // Idle → show the player's total money.
                float bal = moneyManager != null ? moneyManager.Balance : 0f;
                display.text = $"{currencyPrefix}{bal:F2}";
                return;
            }

            string top = $"{currencyPrefix}{_targetText}";
            display.text = _armed
                ? $"{top}\n{currencyPrefix}{(_input.Length > 0 ? _input : "0")}"
                : top;   // amount only - not deliverable yet
        }
    }
}
