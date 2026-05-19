using UnityEngine;

/// <summary>
/// Abstract base class for cooking appliances in the assembly system.
/// Manages a single processing slot with the state machine:
///   Empty → Processing → Done → Burned
///
/// Subclasses define what items are accepted, what happens on each
/// state transition, and any per-frame visual feedback (override Update).
///
/// Interaction (IAssemblyInteractable):
///   - Held item + Empty → insert (if CanAccept returns true)
///   - Empty hand + Done/Burned → collect the processed item
///
/// Setup:
///   1. Place a GameObject with a Collider in the scene.
///   2. Add a concrete subclass (Grill or Toaster).
///   3. Tune processTime and burnGraceTime in the inspector.
/// </summary>
namespace Bufe
{
    public abstract class Appliance : MonoBehaviour, IAssemblyInteractable
    {
        [Header("Timing")]
        [Tooltip("Seconds to complete processing (Raw → Cooked, or None → Toasted).")]
        public float processTime = 5f;

        [Tooltip("Grace seconds after Done before the item burns.")]
        public float burnGraceTime = 5f;

        // ── Runtime state ────────────────────────────────────────────────────────

        protected ApplianceState _state = ApplianceState.Empty;
        protected float _timer;
        protected InteractableItem _storedItem;

        // ── Public read-only ─────────────────────────────────────────────────────

        public ApplianceState CurrentState => _state;

        /// <summary>0–1 progress through the processing phase.</summary>
        public float ProcessProgress =>
            _state == ApplianceState.Processing ? Mathf.Clamp01(_timer / processTime) : 0f;

        /// <summary>0–1 progress toward burning once Done.</summary>
        public float BurnProgress =>
            _state == ApplianceState.Done ? Mathf.Clamp01(_timer / burnGraceTime) : 0f;

        // ── Timer logic ──────────────────────────────────────────────────────────

        protected virtual void Update()
        {
            switch (_state)
            {
                case ApplianceState.Processing:
                    _timer += Time.deltaTime;
                    if (_timer >= processTime)
                    {
                        _state = ApplianceState.Done;
                        _timer = 0f;
                        OnProcessingComplete();
                    }
                    break;

                case ApplianceState.Done:
                    _timer += Time.deltaTime;
                    if (_timer >= burnGraceTime)
                    {
                        _state = ApplianceState.Burned;
                        OnBurned();
                    }
                    break;
            }
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            // Holding an item → can insert if appliance is empty and item is accepted
            if (!hand.IsHandEmpty)
                return _state == ApplianceState.Empty && CanAccept(hand.HeldItem);

            // Empty hand → can collect if done or burned
            return _state == ApplianceState.Done || _state == ApplianceState.Burned;
        }

        public void Interact(PlayerHand hand)
        {
            // ── Insert item ──────────────────────────────────────────────────────
            if (!hand.IsHandEmpty && _state == ApplianceState.Empty)
            {
                _storedItem = hand.Release();
                _state = ApplianceState.Processing;
                _timer = 0f;
                OnItemInserted();
                return;
            }

            // ── Collect item ─────────────────────────────────────────────────────
            if (hand.IsHandEmpty && (_state == ApplianceState.Done || _state == ApplianceState.Burned))
            {
                if (_storedItem == null) return;

                OnBeforeCollect();

                // Re-enable colliders so PlayerHand.PickUp() can cache them
                foreach (Collider col in _storedItem.GetComponentsInChildren<Collider>())
                    col.enabled = true;

                hand.PickUp(_storedItem);

                _storedItem = null;
                _state = ApplianceState.Empty;
                _timer = 0f;
            }
        }

        // ── Subclass hooks ───────────────────────────────────────────────────────

        /// <summary>Can this appliance accept the given item?</summary>
        protected abstract bool CanAccept(InteractableItem item);

        /// <summary>Called once when the item is inserted. Position/hide the item here.</summary>
        protected abstract void OnItemInserted();

        /// <summary>Called once when processing finishes (state → Done). Apply the result here.</summary>
        protected abstract void OnProcessingComplete();

        /// <summary>Called once when the burn timer expires (state → Burned).</summary>
        protected abstract void OnBurned();

        /// <summary>Called just before the item is given back to the player. Restore visuals here.</summary>
        protected virtual void OnBeforeCollect() { }
    }

}
