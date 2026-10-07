using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// A display rack holding individual pickup items (cigarette packs, chips bags…).
    /// The authored scene items define the slots: their starting poses are recorded at
    /// Start. When the player takes items off the rack (for orders) slots free up, and
    /// clicking the rack while holding a matching item snaps it into the first free slot.
    ///
    /// Setup:
    ///   1. Put this on the rack object with a Collider covering it.
    ///   2. Assign the scene display items (e.g. Cigarette (1..8)) to displayItems.
    ///   3. acceptedItem: the OrderItem this rack restocks with (auto-derived from the
    ///      first display item's PremadeItem definition when left empty).
    /// </summary>
    public class DisplayRack : MonoBehaviour, IAssemblyInteractable, IHoverLabel
    {
        [Header("Setup")]
        [Tooltip("Display items. Their poses become slots.")]
        [SerializeField] private List<InteractableItem> displayItems = new List<InteractableItem>();

        [Tooltip("Accepted item. Empty = from the first item.")]
        [SerializeField] private OrderItem acceptedItem;

        [Tooltip("Placement collider is on only while holding. Auto-found.")]
        [SerializeField] private PlayerHand playerHand;

        [Tooltip("Placement collider. Only this one is toggled.")]
        [SerializeField] private Collider placementCollider;

        // ── Slots ────────────────────────────────────────────────────────────────
        private class Slot
        {
            public Vector3          position;
            public Quaternion       rotation;
            public InteractableItem occupant;

            // Occupied while the item still exists AND still sits at the slot (picking it
            // up reparents it to the hand; delivering destroys it - both free the slot).
            public bool IsOccupied =>
                occupant != null && (occupant.transform.position - position).sqrMagnitude < 0.01f;
        }

        private readonly List<Slot> _slots = new List<Slot>();

        private void Start()
        {
            if (playerHand == null) playerHand = FindAnyObjectByType<PlayerHand>();

            if (placementCollider == null)
            {
                Debug.LogWarning("[DisplayRack] No placement collider assigned - it will stay " +
                                 "active and may block empty-hand pickups.", this);
            }
            else
            {
                // MUST be a trigger: raycasts still hit it (clicks work), but physics ignores
                // it - otherwise it depenetrates the display items' rigidbodies and launches
                // them off the shelf on the first physics step.
                placementCollider.isTrigger = true;
                placementCollider.enabled   = playerHand != null && !playerHand.IsHandEmpty;
            }

            foreach (InteractableItem item in displayItems)
            {
                if (item == null) continue;

                _slots.Add(new Slot
                {
                    position = item.transform.position,
                    rotation = item.transform.rotation,
                    occupant = item
                });

                if (acceptedItem == null && item is PremadeItem premade)
                    acceptedItem = premade.definition;
            }
        }

        private void Update()
        {
            // Empty hand → placement collider off, so clicks pass straight through to the
            // display items (normal pickup). Holding something → collider on, so the
            // click lands on the rack and can place the item.
            if (playerHand == null || placementCollider == null) return;

            bool active = !playerHand.IsHandEmpty;
            if (placementCollider.enabled != active)
                placementCollider.enabled = active;
        }

        // ── IAssemblyInteractable ────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            return !hand.IsHandEmpty && Matches(hand.HeldItem) && FirstFreeSlot() != null;
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            Slot slot = FirstFreeSlot();
            InteractableItem item = hand.Release();
            if (item == null) return;

            item.transform.SetPositionAndRotation(slot.position, slot.rotation);
            item.RestoreWorldScale();

            // Freeze it on the rack and make it clickable again (PlayerHand disabled
            // the colliders on pickup).
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                col.enabled = true;

            // It's shop display now, not daily junk - survive the day reset.
            DailySpawn marker = item.GetComponent<DailySpawn>();
            if (marker != null) Destroy(marker);

            slot.occupant = item;
            Log.Info($"[DisplayRack] {item.itemName} placed on the rack.");
        }

        public string HoverLabel
        {
            get
            {
                int free = 0;
                foreach (Slot s in _slots) if (!s.IsOccupied) free++;

                // Read every hovered frame; rebuild only when the free count changes.
                if (free != _labelFree || _label == null)
                {
                    string label = acceptedItem != null ? acceptedItem.itemName : name;
                    _labelFree = free;
                    _label     = free > 0 ? $"{label} rack - {free} empty" : $"{label} rack";
                }
                return _label;
            }
        }

        private int    _labelFree;
        private string _label;

        // ── Helpers ──────────────────────────────────────────────────────────────

        private bool Matches(InteractableItem held)
        {
            if (held == null || acceptedItem == null) return false;
            return held is PremadeItem premade && premade.definition == acceptedItem;
        }

        private Slot FirstFreeSlot()
        {
            foreach (Slot slot in _slots)
                if (!slot.IsOccupied) return slot;
            return null;
        }
    }
}
