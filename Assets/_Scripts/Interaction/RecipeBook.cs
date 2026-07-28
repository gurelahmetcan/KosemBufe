using UnityEngine;
using UnityEngine.InputSystem;

namespace Bufe
{
    /// <summary>
    /// A recipe book sitting in the world. Clicking it opens a full-screen UI version;
    /// pressing back (Escape or right-click) — or a Back button wired to <see cref="Close"/> —
    /// closes it again. Needs a Collider so the player's raycast can hit it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RecipeBook : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("UI")]
        [Tooltip("The full-screen recipe UI shown when the book is clicked (starts inactive).")]
        [SerializeField] private GameObject bookUI;

        [Header("Player")]
        [Tooltip("Player look — put into UIMode (cursor freed, look suppressed) while the book is open.")]
        [SerializeField] private PlayerLook playerLook;

        [Header("Hover")]
        [SerializeField] private string hoverLabelText = "Recipe Book";

        private bool _open;

        public bool IsOpen => _open;

        private void Start()
        {
            if (bookUI != null) bookUI.SetActive(false);
        }

        // ── IAssemblyInteractable ─────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand) => !_open;

        public void Interact(PlayerHand hand) => Open();

        public string HoverLabel => _open ? null : hoverLabelText;

        // ── Open / close ──────────────────────────────────────────────────────────

        public void Open()
        {
            if (_open) return;
            _open = true;
            if (bookUI != null) bookUI.SetActive(true);
            if (playerLook != null) playerLook.UIMode = true;   // frees cursor, blocks world clicks
        }

        /// <summary>Closes the book. Also wire this to a UI Back button if you add one.</summary>
        public void Close()
        {
            if (!_open) return;
            _open = false;
            if (bookUI != null) bookUI.SetActive(false);
            if (playerLook != null) playerLook.UIMode = false;  // re-locks cursor, resumes look
        }

        private void Update()
        {
            if (!_open) return;

            Keyboard kb    = Keyboard.current;
            Mouse    mouse = Mouse.current;

            bool back = (kb    != null && kb.escapeKey.wasPressedThisFrame)
                     || (mouse != null && mouse.rightButton.wasPressedThisFrame);

            if (back) Close();
        }
    }
}
