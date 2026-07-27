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

    // ── Runtime state ────────────────────────────────────────────────────────

    private PreparedItem      _preparedItem;               // bread (centre)
    private SucukItem         _sucuk;                       // whole sucuk (side)
    private readonly List<GameObject> _slices = new();      // cut slices on the side

    // ── IAssemblyInteractable (left-click) ───────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // Placing a bread onto the station.
        if (!hand.IsHandEmpty && hand.HeldItem is PreparedItem bread && bread.State != BreadState.Toasted)
            return _preparedItem == null;

        // Placing a whole sucuk onto the station (to be sliced here).
        if (!hand.IsHandEmpty && hand.HeldItem is SucukItem)
            return _sucuk == null;

        // Bread already on the station — forward to its own interaction logic
        // (cut / open / close / add filling). All routed through this collider
        // because the bread's own colliders stay disabled while it rests here.
        if (_preparedItem != null)
            return _preparedItem.CanInteract(hand);

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

        // ── Forward to the bread resting on the station ──────────────────────
        if (_preparedItem != null)
            _preparedItem.Interact(hand);
    }

    // ── ISecondaryInteractable (empty-hand right-click) ──────────────────────

    public bool CanSecondaryInteract(PlayerHand hand)
    {
        // Empty hand + bread on the station → lift it back into the hand so the
        // player can keep editing (open/close/fill) it freely.
        return hand.IsHandEmpty && _preparedItem != null;
    }

    public void SecondaryInteract(PlayerHand hand)
    {
        if (hand.IsHandEmpty && _preparedItem != null)
            hand.PickUp(_preparedItem);   // OnPickedUp clears _preparedItem
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

        // Line the slices up along the anchor's right axis, centred on it.
        int   count = slices.Count;
        float span  = sliceSpacing * (count - 1);
        var   targets = new Vector3[count];
        for (int i = 0; i < count; i++)
            targets[i] = anchor.position + new Vector3(i * sliceSpacing, 0f, 0f);

        for (int i = 0; i < count; i++)
        {
            GameObject slice = slices[i];
            if (slice == null) continue;

            _slices.Add(slice);

            // Bind so an empty-hand click drops the slice into the open bread.
            SucukSlice sc = slice.GetComponent<SucukSlice>() ?? slice.AddComponent<SucukSlice>();
            sc.Bind(this);

            // Freeze physics so slices sit still on the board (and don't fight the slide).
            Rigidbody rb = slice.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        StartCoroutine(SlideSlices(new List<GameObject>(slices), targets));
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
            _slices.Remove(slice.gameObject);
            slice.ClearStation();
            Destroy(slice);   // remove the SucukSlice behaviour; it's part of the bread now
        }
    }

} }
