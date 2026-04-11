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
/// Setup:
///   1. Place a GameObject with a Collider in the scene (the prep surface).
///   2. Add this component.
///   3. Create an empty child Transform and assign it as stackAnchor.
///   4. Tune stackOffset for the vertical gap between stacked items.
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

    // ── Public read-only state ───────────────────────────────────────────────

    public int  StackCount  => _stack.Count;
    public bool IsFinalized => _isFinalized;
    public bool IsEmpty     => _stack.Count == 0 && !_isFinalized;

    // ── IAssemblyInteractable ────────────────────────────────────────────────

    public bool CanInteract(PlayerHand hand)
    {
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

    // ── Place an ingredient onto the stack ───────────────────────────────────

    private void PlaceIngredient(Ingredient ingredient, PlayerHand hand)
    {
        // Release from hand (item stays dormant: kinematic, colliders disabled)
        hand.Release();

        // Position on top of the stack
        Vector3 pos = stackAnchor.position + Vector3.up * (stackOffset * _stack.Count);
        ingredient.transform.SetParent(stackAnchor);
        ingredient.transform.position = pos;
        ingredient.transform.rotation = stackAnchor.rotation;

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
        // Create a parent object for the entire group
        GameObject groupObj = new GameObject("FinalizedStack");
        groupObj.transform.position = stackAnchor.position;
        groupObj.transform.rotation = stackAnchor.rotation;

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

        // Add a BoxCollider that encompasses all child meshes
        BoxCollider box = groupObj.AddComponent<BoxCollider>();
        FitBoxColliderToChildren(groupObj, box);
        box.enabled = false; // disabled until picked up

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

        // Unparent from station anchor
        _finalizedStack.transform.SetParent(null);

        // Enable collider so PlayerHand.PickUp() can cache and disable it
        Collider col = _finalizedStack.GetComponent<Collider>();
        if (col != null) col.enabled = true;

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
