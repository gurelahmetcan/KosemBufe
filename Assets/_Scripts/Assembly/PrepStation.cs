using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class PrepStation : MonoBehaviour, IAssemblyInteractable, ISecondaryInteractable
{
    [Header("Setup")]
    [Tooltip("Position where the bread is placed. Items stack upward from here.")]
    public Transform stackAnchor;

    [Tooltip("Cutting spot where a whole sucuk sits while it is being sliced (e.g. the middle of the board).")]
    public Transform sucukAnchor;

    [Tooltip("Rest spot (e.g. left side) the cut slices slide to and wait on until placed into the bread.")]
    public Transform sliceAnchor;

    [Header("Slice Layout")]
    [Tooltip("Spacing between sucuk slices once they line up on the slice anchor.")]
    public float sliceSpacing = 0.06f;

    [Tooltip("Seconds it takes for freshly cut slices to slide from the centre to the slice anchor.")]
    public float sliceSlideDuration = 0.25f;

    [Tooltip("Maximum sucuk slices that can rest on the board at once. Slices cut beyond this are discarded.")]
    public int maxSlices = 8;

    // ── Runtime state ────────────────────────────────────────────────────────

    private PreparedItem      _preparedItem;               // bread (centre)
    private SucukItem         _sucuk;                       // whole sucuk (side)

    // Fixed slots for cut slices on the side. Index → position along the anchor;
    // null = free. New cuts fill the first free slots, so re-cutting never stacks
    // slices on top of each other, and deposited slices free their slot.
    private GameObject[] _sliceSlots;

    // ── IAssemblyInteractable (left-click) ───────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Placing a bread — blocked while a whole sucuk occupies the board.
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem bread && bread.State != BreadState.Toasted)
            return _preparedItem == null && _sucuk == null;

        // Placing a whole sucuk — blocked while a bread occupies the board.
        if (!hand.IsHandEmpty && hand.HeldItem is SucukItem)
            return _sucuk == null && _preparedItem == null;

        // Bread on the station (its own colliders are disabled, so clicks route here):
        if (_preparedItem != null)
        {
            // Empty hand → left-click lifts the bread back into the hand.
            if (hand.IsHandEmpty)
                return true;

            // Knife / ingredient in hand → forward to the bread's own logic (cut / fill).
            return _preparedItem.CanInteract(hand);
        }

        return false;
    }

    public void Interact(PlayerHand hand)
    {
        // ── Bread placement ──────────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem bread && bread.State != BreadState.Toasted)
        {
            PlaceBread(bread, hand);
            return;
        }

        // ── Sucuk placement ──────────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is SucukItem sucuk)
        {
            PlaceSucuk(sucuk, hand);
            return;
        }

        // ── Bread resting on the station ─────────────────────────────────────
        if (_preparedItem != null)
        {
            // Empty hand → left-click takes the bread back.
            if (hand.IsHandEmpty)
            {
                hand.PickUp(_preparedItem);   // OnPickedUp clears _preparedItem
                return;
            }

            // Knife / ingredient → forward to the bread (cut / add filling).
            _preparedItem.Interact(hand);
        }
    }

    // ── ISecondaryInteractable (empty-hand right-click) ──────────────────────

    public bool CanSecondaryInteract(PlayerHand hand)
    {
        // Empty hand + bread on the station → right-click opens / closes it.
        return hand.IsHandEmpty && _preparedItem != null && _preparedItem.CanToggleOpen();
    }

    public void SecondaryInteract(PlayerHand hand)
    {
        if (hand.IsHandEmpty && _preparedItem != null)
            _preparedItem.ToggleOpen();
    }

    // ── Bread placement ──────────────────────────────────────────────────────

    private void PlaceBread(PreparedItem bread, PlayerHand hand)
    {
        hand.Release();

        bread.transform.SetParent(stackAnchor, worldPositionStays: false);
        bread.transform.localRotation = Quaternion.identity;
        bread.transform.localScale    = bread.OriginalLocalScale;
        bread.transform.localPosition = Vector3.zero;

        // Colliders intentionally left DISABLED (same as regular ingredients).
        // All interactions go through the PrepStation's collider to avoid
        // physics overlap pushing the CharacterController on placement.

        _preparedItem = bread;
        bread.PlaceOnStation(this);

        Debug.Log("[PrepStation] Bread placed on station.");
    }

    /// <summary>Called by PreparedItem.OnPickedUp when the player lifts the bread off the station.</summary>
    public void OnPreparedItemPickedUp()
    {
        _preparedItem = null;
        Debug.Log("[PrepStation] Bread removed from station.");
    }

    // ── Sucuk placement & slicing ────────────────────────────────────────────

    private void PlaceSucuk(SucukItem sucuk, PlayerHand hand)
    {
        hand.Release();

        Transform anchor = sucukAnchor != null ? sucukAnchor : stackAnchor;
        sucuk.transform.SetParent(anchor, worldPositionStays: false);
        sucuk.transform.localPosition = Vector3.zero;
        sucuk.transform.localRotation = Quaternion.Euler(0, 0, -90f);        
        sucuk.transform.localScale    = sucuk.OriginalLocalScale;

        // Re-enable the sucuk's colliders (PlayerHand disabled them on pickup) so
        // the player can still aim at it to cut it, and freeze it in place.
        foreach (Collider col in sucuk.GetComponentsInChildren<Collider>())
            col.enabled = true;

        Rigidbody rb = sucuk.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        _sucuk = sucuk;
        sucuk.PlaceOnStation(this);

        Debug.Log("[PrepStation] Sucuk placed on station.");
    }

    /// <summary>Called by SucukItem.OnPickedUp when the whole sucuk is lifted off the station.</summary>
    public void OnSucukPickedUp()
    {
        _sucuk = null;
    }

    /// <summary>
    /// Called by SucukItem right after it spawns its slices (at the sucuk's
    /// centre). The board takes ownership of the slices, makes them clickable
    /// straight into the bread, and slides them over to the slice anchor.
    /// </summary>
    public void OnSucukSliced(SucukItem sucuk, List<GameObject> slices)
    {
        if (_sucuk == sucuk) _sucuk = null;
        if (slices == null || slices.Count == 0) return;

        Transform anchor = sliceAnchor != null ? sliceAnchor
                         : sucukAnchor != null ? sucukAnchor
                         : stackAnchor;

        EnsureSlots();

        var moving  = new List<GameObject>();
        var targets = new List<Vector3>();

        foreach (GameObject slice in slices)
        {
            if (slice == null) continue;

            int slot = FirstFreeSlot();
            if (slot < 0)
            {
                // Board is full — discard the extra slice instead of stacking it.
                Debug.Log("[PrepStation] Slice board full — extra slice discarded.");
                Destroy(slice);
                continue;
            }

            _sliceSlots[slot] = slice;

            // Bind so an empty-hand click drops the slice into the open bread.
            SucukSlice sc = slice.GetComponent<SucukSlice>() ?? slice.AddComponent<SucukSlice>();
            sc.Bind(this);

            // Freeze physics so slices sit still on the board (and don't fight the slide).
            Rigidbody rb = slice.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            moving.Add(slice);
            targets.Add(SlicePosition(anchor, slot));
        }

        if (moving.Count > 0)
            StartCoroutine(SlideSlices(moving, targets.ToArray()));
    }

    // ── Slice slot helpers ────────────────────────────────────────────────────

    private void EnsureSlots()
    {
        int size = Mathf.Max(1, maxSlices);
        if (_sliceSlots == null || _sliceSlots.Length != size)
        {
            var resized = new GameObject[size];
            if (_sliceSlots != null)
                for (int i = 0; i < _sliceSlots.Length && i < size; i++)
                    resized[i] = _sliceSlots[i];
            _sliceSlots = resized;
        }
    }

    /// <summary>World position of a slice slot, laid out along the anchor from index 0.</summary>
    private Vector3 SlicePosition(Transform anchor, int index)
        => anchor.position + new Vector3(-index * sliceSpacing, 0f, 0f);

    /// <summary>How many slice slots are currently empty. Used to gate cutting a new sucuk.</summary>
    public int FreeSliceSlots
    {
        get
        {
            EnsureSlots();
            int free = 0;
            for (int i = 0; i < _sliceSlots.Length; i++)
                if (_sliceSlots[i] == null) free++;
            return free;
        }
    }

    /// <summary>First empty slot index, or -1 if the board is full.</summary>
    private int FirstFreeSlot()
    {
        for (int i = 0; i < _sliceSlots.Length; i++)
            if (_sliceSlots[i] == null) return i;
        return -1;
    }

    /// <summary>Frees whatever slot holds this slice (called when it's placed into the bread).</summary>
    private void FreeSlot(GameObject slice)
    {
        if (_sliceSlots == null) return;
        for (int i = 0; i < _sliceSlots.Length; i++)
            if (_sliceSlots[i] == slice) { _sliceSlots[i] = null; return; }
    }

    private IEnumerator SlideSlices(List<GameObject> slices, Vector3[] targets)
    {
        int count = slices.Count;
        var starts = new Vector3[count];
        for (int i = 0; i < count; i++)
            starts[i] = slices[i] != null ? slices[i].transform.position : targets[i];

        float duration = Mathf.Max(0.0001f, sliceSlideDuration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            for (int i = 0; i < count; i++)
                if (slices[i] != null)
                    slices[i].transform.position = Vector3.Lerp(starts[i], targets[i], e);
            yield return null;
        }

        for (int i = 0; i < count; i++)
            if (slices[i] != null)
                slices[i].transform.position = targets[i];
    }

    // ── Slice → bread transfer (called by SucukSlice) ────────────────────────

    /// <summary>True if there is an open bread on this station with a free socket for this type.</summary>
    public bool CanDepositSlice(Constants.IngredientType type)
    {
        return _preparedItem != null
            && _preparedItem.State == BreadState.Open
            && _preparedItem.HasFreeSocket(type);
    }

    /// <summary>Moves a slice from the board straight into the open bread's socket.</summary>
    public void DepositSlice(SucukSlice slice)
    {
        if (_preparedItem == null || slice == null) return;

        Ingredient       ing  = slice.GetComponent<Ingredient>();
        InteractableItem item = slice.GetComponent<InteractableItem>();
        if (ing == null || item == null) return;

        if (_preparedItem.PlaceIngredient(ing.ingredientType, item))
        {
            FreeSlot(slice.gameObject);
            slice.ClearStation();
            Destroy(slice);   // remove the SucukSlice behaviour; it's part of the bread now
        }
    }

} }
