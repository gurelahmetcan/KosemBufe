using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical prep surface (cutting board / assembly area) where the player
/// stacks ingredients one by one to build a sandwich or other grouped item.
///
/// LIFO Stack Rules:
///   - Player holds an Ingredient → left-click station to push it onto the stack.
///   - Empty hand + stack NOT finalized → left-click to Pop the top ingredient back.
///   - When an Ingredient with isClosingBread is placed (and stack has 2+ items),
///     the stack is finalized into a FinalizedStack grouped object.
///   - Empty hand + stack IS finalized → left-click to pick up the whole group.
///
/// Toast Flow (TostBread):
///   - Player holds TostBread → left-click station to place it on the surface.
///   - Further bread interactions (open/fill/close) are handled by TostBread's
///     own IAssemblyInteractable — click on the bread itself, not the station.
///   - PrepStation is blocked for all other interactions while a TostBread is on it.
///
/// Setup:
///   1. Create an UNSCALED (1,1,1) root GameObject for the prep station.
///   2. Make the board model a child of the root and apply any visual scale there.
///   3. Add a GameObject with a Collider to the root (the prep surface) and
///      attach this component.
///   4. Create an empty child Transform with scale (1,1,1) under the root,
///      position it where the bottom of the stack should sit, and assign it
///      as stackAnchor. The anchor MUST have uniform (1,1,1) lossy scale so
///      stacked ingredients are not distorted.
///   5. Tune stackOffset for the vertical gap between stacked items.
/// </summary>
public class PrepStation : MonoBehaviour, IAssemblyInteractable
{
    [Header("Setup")]
    [Tooltip("Position where the first ingredient is placed. Items stack upward from here.")]
    public Transform stackAnchor;

    [Tooltip("Vertical distance between stacked ingredients.")]
    public float stackOffset = 0.05f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private readonly List<Ingredient> _stack = new List<Ingredient>();
    private FinalizedStack _finalizedStack;
    private bool _isFinalized;
    private TostBread _tostBread;

    // ── Public read-only state ───────────────────────────────────────────────

    public int  StackCount  => _stack.Count;
    public bool IsFinalized => _isFinalized;
    public bool IsEmpty     => _stack.Count == 0 && !_isFinalized && _tostBread == null;

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
        // ── TostBread placement ──────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread)
            return _tostBread == null && _stack.Count == 0 && !_isFinalized;

        // While a TostBread is on the station the player interacts with the bread
        // itself (TostBread.IAssemblyInteractable). Block all other station logic.
        if (_tostBread != null)
            return false;

        // ── Original stacking logic ──────────────────────────────────────────

        // Holding an ingredient → can place (unless a finalized stack is waiting)
        if (!hand.IsHandEmpty)
        {
            if (_isFinalized) return false;
            return hand.HeldItem is Ingredient;
        }

        // Empty hand → can pop from incomplete stack or pick up finalized stack
        return _stack.Count > 0 || _isFinalized;
    }

    public void Interact(PlayerHand hand)
    {
        // ── TostBread placement ──────────────────────────────────────────────
        if (!hand.IsHandEmpty && hand.HeldItem is TostBread bread)
        {
            PlaceTostBread(bread, hand);
            return;
        }

        // ── Holding an ingredient → place it on the stack ────────────────────
        if (!hand.IsHandEmpty)
        {
            Ingredient ingredient = hand.HeldItem as Ingredient;
            if (ingredient == null) return;

            PlaceIngredient(ingredient, hand);
            return;
        }

        // ── Empty hand interactions ──────────────────────────────────────────
        if (_isFinalized)
        {
            PickUpFinalizedStack(hand);
        }
        else if (_stack.Count > 0)
        {
            PopTopIngredient(hand);
        }
    }

    // ── TostBread placement ──────────────────────────────────────────────────

    private void PlaceTostBread(TostBread bread, PlayerHand hand)
    {
        hand.Release();

        bread.transform.SetParent(stackAnchor, worldPositionStays: false);
        bread.transform.localRotation = bread.OriginalLocalRotation;
        bread.transform.localScale    = bread.OriginalLocalScale;
        bread.transform.localPosition = Vector3.zero;

        // Re-enable colliders so the player can click the bread directly
        foreach (Collider col in bread.GetComponentsInChildren<Collider>())
            col.enabled = true;

        _tostBread = bread;
        bread.PlaceOnStation(this);

        Debug.Log("[PrepStation] TostBread placed on station.");
    }

    /// <summary>Called by TostBread.OnPickedUp when the player lifts it off the station.</summary>
    public void OnTostBreadPickedUp()
    {
        _tostBread = null;
        Debug.Log("[PrepStation] TostBread removed from station.");
    }

    // ── Place an ingredient onto the stack ───────────────────────────────────

    private void PlaceIngredient(Ingredient ingredient, PlayerHand hand)
    {
        // Release from hand (item stays dormant: kinematic, colliders disabled).
        // Release unparents the item.
        hand.Release();

        // Parent under stackAnchor so the stack follows the board if it moves.
        // stackAnchor MUST have a uniform (1,1,1) lossy scale — place it as a
        // child of an unscaled board root so parenting cannot distort the
        // ingredient's size. Rotation and scale are restored from the prefab's
        // authored values; position is stacked upward along the anchor's local
        // Y so the stack follows the board's orientation.
        ingredient.transform.SetParent(stackAnchor, worldPositionStays: false);
        ingredient.transform.localRotation = ingredient.OriginalLocalRotation;
        ingredient.transform.localScale    = ingredient.OriginalLocalScale;
        ingredient.transform.localPosition = new Vector3(0f, stackOffset * _stack.Count, 0f);

        _stack.Add(ingredient);

        Debug.Log($"[PrepStation] Placed '{ingredient.itemName}' ({ingredient.ingredientType})" +
                  $" → stack size: {_stack.Count}");

        // Finalize if this is a closing bread and we have at least 2 items
        if (ingredient.isClosingBread && _stack.Count >= 2)
            Finalize();
    }

    // ── Pop the top ingredient (LIFO error correction) ──────────────────────

    private void PopTopIngredient(PlayerHand hand)
    {
        int topIndex = _stack.Count - 1;
        Ingredient top = _stack[topIndex];
        _stack.RemoveAt(topIndex);

        // Unparent from station
        top.transform.SetParent(null);

        // Re-enable colliders so PickUp can cache them
        foreach (Collider col in top.GetComponentsInChildren<Collider>())
            col.enabled = true;

        hand.PickUp(top);

        Debug.Log($"[PrepStation] Popped '{top.itemName}' → stack size: {_stack.Count}");
    }

    // ── Finalize the stack into a grouped object ────────────────────────────

    private void Finalize()
    {
        // Create the group object and parent it under stackAnchor so it sits
        // exactly where the stack was built and moves with the board. Because
        // stackAnchor has uniform (1,1,1) lossy scale, local = world for size.
        GameObject groupObj = new GameObject("FinalizedStack");
        groupObj.transform.SetParent(stackAnchor, worldPositionStays: false);
        groupObj.transform.localPosition = Vector3.zero;
        groupObj.transform.localRotation = Quaternion.identity;
        groupObj.transform.localScale    = Vector3.one;

        // Add the FinalizedStack component
        FinalizedStack stack = groupObj.AddComponent<FinalizedStack>();
        stack.itemName = BuildStackName();
        stack.supportedProcesses = ProcessType.Toastable;

        // Reparent all ingredients under the group and record their types.
        // Strip each ingredient's own Rigidbody + Colliders — the parent's
        // single Rigidbody and fitted BoxCollider drive physics for the whole
        // group. Nested rigidbodies inside a kinematic parent cause the
        // "floating and spinning" glitch when the stack is thrown.
        foreach (Ingredient ing in _stack)
        {
            stack.ingredientTypes.Add(ing.ingredientType);
            ing.transform.SetParent(groupObj.transform);

            Rigidbody ingRb = ing.GetComponent<Rigidbody>();
            if (ingRb != null) Destroy(ingRb);

            foreach (Collider ingCol in ing.GetComponentsInChildren<Collider>())
                Destroy(ingCol);
        }

        // Add a Rigidbody (starts kinematic while on station)
        Rigidbody rb = groupObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Add a BoxCollider that encompasses all child meshes. Keep it enabled
        // so the player can click the stack directly to pick it up.
        BoxCollider box = groupObj.AddComponent<BoxCollider>();
        FitBoxColliderToChildren(groupObj, box);

        // Station state
        _finalizedStack = stack;
        _isFinalized = true;
        _stack.Clear();

        Debug.Log($"[PrepStation] Finalized! '{stack.itemName}' with " +
                  $"{stack.ingredientTypes.Count} ingredients: " +
                  $"[{string.Join(", ", stack.ingredientTypes)}]");
    }

    // ── Pick up the finalized stack ─────────────────────────────────────────

    private void PickUpFinalizedStack(PlayerHand hand)
    {
        if (_finalizedStack == null) return;

        // hand.PickUp reparents to holdPoint and disables physics/colliders.
        // The stack is already a child of stackAnchor with its collider
        // enabled, so no manual unparenting or collider toggling is needed.
        hand.PickUp(_finalizedStack);

        Debug.Log($"[PrepStation] Picked up finalized stack '{_finalizedStack.itemName}'");

        _finalizedStack = null;
        _isFinalized = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildStackName()
    {
        if (_stack.Count == 0) return "Stack";

        // Use the name of the bottom item as base (e.g. "Bread Stack")
        return $"{_stack[0].itemName} Stack";
    }

    /// <summary>
    /// Calculates a BoxCollider that fits all child Renderers.
    /// </summary>
    private static void FitBoxColliderToChildren(GameObject root, BoxCollider box)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size   = root.transform.InverseTransformVector(bounds.size);
    }
}
