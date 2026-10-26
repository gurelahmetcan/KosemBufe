using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    /// <summary>
    /// Generic confirm / info dialog shown over the laptop panel (purchase confirmation,
    /// insufficient balance, …). With no cancel label only the confirm button is shown.
    /// </summary>
    public class LaptopModal : MonoBehaviour
    {
        [Header("UI")]
        public GameObject      root;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public Button          confirmButton;
        public TextMeshProUGUI confirmLabel;
        public Button          cancelButton;
        public TextMeshProUGUI cancelLabel;

        private Action _onConfirm;
        private Action _onCancel;
        private bool   _wired;
        private bool   _shownOnce;

        public bool IsOpen => root != null && root.activeSelf;

        private void Start()
        {
            EnsureWired();

            // Start can run right after the first Show() when root is this inactive object.
            if (!_shownOnce && root != null) root.SetActive(false);
        }

        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            if (confirmButton != null) confirmButton.onClick.AddListener(() => Resolve(_onConfirm));
            if (cancelButton  != null) cancelButton.onClick.AddListener(() => Resolve(_onCancel));
        }

        public void Show(string title, string message, string confirmText, Action onConfirm,
                         string cancelText = null, Action onCancel = null)
        {
            EnsureWired();

            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            if (titleText    != null) titleText.text    = title;
            if (messageText  != null) messageText.text  = message;
            if (confirmLabel != null) confirmLabel.text = confirmText;

            bool hasCancel = !string.IsNullOrEmpty(cancelText);
            if (cancelButton != null) cancelButton.gameObject.SetActive(hasCancel);
            if (cancelLabel  != null && hasCancel) cancelLabel.text = cancelText;

            _shownOnce = true;
            if (root != null) root.SetActive(true);
        }

        /// <summary>Closes as if cancelled (back key).</summary>
        public void Dismiss() => Resolve(_onCancel);

        private void Resolve(Action callback)
        {
            if (AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            _onConfirm = null;
            _onCancel  = null;
            if (root != null) root.SetActive(false);

            callback?.Invoke();
        }
    }
}
