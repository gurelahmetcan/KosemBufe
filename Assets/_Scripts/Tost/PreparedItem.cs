using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    [System.Serializable]
    public class IngredientSocketGroup
    {
        public Constants.IngredientType ingredientType;
        public List<Transform> sockets;
    }

    [RequireComponent(typeof(Collider))]
    public class PreparedItem : Ingredient, IAssemblyInteractable
    {
        #region Serialized Fields
        
        [Header("Placeholder Visuals")]
        [Tooltip("Active when bread is Whole (uncut).")]
        [SerializeField] private GameObject wholeBreadVisual;

        [Tooltip("Active when bread has been cut in half.")]
        [SerializeField] private GameObject halfBreadVisual;

        [Tooltip("Active when bread is open and ready for filling.")]
        [SerializeField] private GameObject openBreadVisual;

        [Tooltip("Material with the toasted (pişmiş) texture, applied when toasting finishes.")]
        [SerializeField] private Material cookedMaterial;

        [Header("Cutting")]
        [Tooltip("Prefab spawned twice when the full bread is cut. Must have a PreparedItem component.")]
        [SerializeField] private GameObject halfBreadPrefab;

        [Tooltip("Half the gap between the two spawned halves (world units).")]
        [SerializeField] private float halfSpawnOffset = 0.15f;
        
        public List<GameObject> physicalIngredients = new();
        public Constants.CookState cookState = Constants.CookState.Raw;

        [SerializeField] private List<IngredientSocketGroup> socketGroups;
        private Dictionary<Constants.IngredientType, List<GameObject>> placedIngredients = new();

        #endregion

        // ── Runtime state ─────────────────────────────────────────────────────────

        public BreadState State    { get; private set; } = BreadState.Full;

        private readonly List<Constants.IngredientType> _ingredients = new();
        public IReadOnlyList<Constants.IngredientType> Ingredients => _ingredients;

        // The bread counts itself as an ingredient once it's closed. Tracked so
        // repeated open/close toggling never double-counts it in the recipe.
        private bool _selfCounted;

        private PrepStation _station;
        private Toaster     _toaster;

        // ── Unity ─────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            RefreshVisuals();
        }

        // ── IAssemblyInteractable ─────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            // Knife + Whole → cut in half (works anywhere)
            if (hand.HeldItem is KnifeItem && State == BreadState.Full)
                return true;

            // Everything below requires the bread to be resting on a station
            if (_station == null)
                return false;

            // Empty hand + Whole / Cut / Closed → open the bread (Closed can be
            // re-opened so the player can keep editing the filling)
            if (hand.IsHandEmpty && (State == BreadState.Full || State == BreadState.Half || State == BreadState.Closed))
                return true;

            // Ingredient in hand + Open → add to filling (only if a socket is free)
            if (hand.HeldItem is Ingredient heldIngredient && State == BreadState.Open)
                return HasFreeSocket(heldIngredient.ingredientType);

            // Empty hand + Open → close the bread
            if (hand.IsHandEmpty && State == BreadState.Open)
                return true;

            return false;
        }

        public void Interact(PlayerHand hand)
        {
            if (hand.HeldItem is KnifeItem && State == BreadState.Full)
            {
                Cut();
                return;
            }

            if (hand.IsHandEmpty && (State == BreadState.Full || State == BreadState.Half || State == BreadState.Closed))
            {
                Open();
                return;
            }

            if (hand.HeldItem is Ingredient ing && State == BreadState.Open)
            {
                AddIngredient(ing.ingredientType, hand);
                return;
            }

            if (hand.IsHandEmpty && State == BreadState.Open)
            {
                Close();
            }
        }

        // ── Called by PrepStation ─────────────────────────────────────────────────

        /// <summary>Registers which station this bread is currently resting on.</summary>
        public void PlaceOnStation(PrepStation station)
        {
            _station = station;
        }

        // ── Called by Toaster ─────────────────────────────────────────────────────

        /// <summary>Registers the toaster this bread is currently inside.</summary>
        public void PlaceInToaster(Toaster toaster)
        {
            _toaster = toaster;
        }

        /// <summary>Notifies the station or toaster that this bread has been picked up.</summary>
        public override void OnPickedUp()
        {
            if (_station != null)
            {
                _station.OnPreparedItemPickedUp();
                _station = null;
            }

            if (_toaster != null)
            {
                _toaster.OnPreparedItemPickedUp();
                _toaster = null;
            }
        }

        #region Toaster Functions

        public void StartToasting()
        {
            State = BreadState.Toasting;
            RefreshVisuals();
        }

        public void FinishToasting()
        {
            State = BreadState.Toasted;
            cookState = Constants.CookState.Cooked;
            RefreshVisuals();
            Debug.Log("[PreparedItem] Toasted!");
        }

        #endregion

        #region State Transitions

        /// <summary>Sets this instance to the Half state. Call on freshly spawned half-bread objects.</summary>
        public void InitAsHalf()
        {
            State = BreadState.Half;
            ingredientType = Constants.IngredientType.Bread_Half;
            RefreshVisuals();
        }

        private void Cut()
        {
            if (halfBreadPrefab == null)
            {
                Debug.LogWarning("[PreparedItem] halfBreadPrefab is not assigned — cannot spawn two halves.");
                return;
            }

            // Free the prep station slot before we destroy ourselves.
            if (_station != null)
            {
                _station.OnPreparedItemPickedUp();
                _station = null;
            }

            Vector3    spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;
            Vector3    right    = transform.right;

            for (int i = 0; i < 2; i++)
            {
                float      sign = i == 0 ? -1f : 1f;
                Vector3    pos  = spawnPos + right * (halfSpawnOffset * sign);
                Quaternion rot  = i == 0 ? spawnRot : spawnRot * Quaternion.Euler(0f, 180f, 0f);
                GameObject obj  = Instantiate(halfBreadPrefab, pos, rot);
                PreparedItem half = obj.GetComponent<PreparedItem>();
                if (half != null)
                    half.InitAsHalf();
            }

            Debug.Log("[PreparedItem] Cut into two halves.");
            Destroy(gameObject);
        }

        private void Open()
        {
            State = BreadState.Open;

            // Re-opening a closed bread: drop the self ingredient so it isn't
            // counted twice once the player closes it again.
            if (_selfCounted)
            {
                _ingredients.Remove(ingredientType);
                _selfCounted = false;
            }

            RefreshVisuals();
            Debug.Log($"[PreparedItem] Opened ({ingredientType}).");
        }

        public void AddIngredient(Constants.IngredientType type)
        {
            _ingredients.Add(type);
            
            Debug.Log($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
        }

        /// <summary>True if this ingredient type has a defined socket group with at least one free socket.</summary>
        public bool HasFreeSocket(Constants.IngredientType type)
        {
            IngredientSocketGroup group = socketGroups?.Find(g => g.ingredientType == type);
            if (group == null || group.sockets == null || group.sockets.Count == 0)
                return false;

            int placedCount = placedIngredients.TryGetValue(type, out List<GameObject> placed) ? placed.Count : 0;
            return placedCount < group.sockets.Count;
        }

        public bool AddIngredient(Constants.IngredientType type, PlayerHand hand)
        {
            if (!HasFreeSocket(type))
            {
                Debug.LogWarning($"[PreparedItem] No free {type} socket — cannot place ingredient.");
                return false;
            }

            // Keep the ingredient visible inside the open bread rather than destroying it.
            InteractableItem item = hand.Release();
            return PlaceIngredient(type, item);
        }

        /// <summary>
        /// Places an already-detached ingredient object into a free socket for its
        /// type. Used both by hand placement and by direct transfers (e.g. sucuk
        /// slices clicked straight off the cutting board). Returns false if there
        /// is no free socket or the item is null.
        /// </summary>
        public bool PlaceIngredient(Constants.IngredientType type, InteractableItem item)
        {
            if (item == null) return false;

            IngredientSocketGroup group = socketGroups?.Find(g => g.ingredientType == type);
            if (group == null || group.sockets == null || group.sockets.Count == 0)
            {
                Debug.LogWarning($"[PreparedItem] No socket group defined for {type} — cannot place ingredient.");
                return false;
            }

            if (!placedIngredients.TryGetValue(type, out List<GameObject> placedOfType))
            {
                placedOfType = new List<GameObject>();
                placedIngredients[type] = placedOfType;
            }

            if (placedOfType.Count >= group.sockets.Count)
            {
                Debug.LogWarning($"[PreparedItem] All {type} sockets are full.");
                return false;
            }

            Transform socket = group.sockets[placedOfType.Count];

            _ingredients.Add(type);
            physicalIngredients.Add(item.gameObject);
            placedOfType.Add(item.gameObject);

            item.transform.SetParent(socket);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            // Disable colliders — ingredient is now part of the bread, not independently pickable
            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Remove Rigidbody to prevent physics interference with the bread
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            Debug.Log($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
            return true;
        }

        private void Close()
        {
            State = BreadState.Closed;

            // Count the bread itself exactly once (see _selfCounted).
            if (!_selfCounted)
            {
                _ingredients.Add(ingredientType);
                _selfCounted = true;
            }

            RefreshVisuals();
            Debug.Log("[PreparedItem] Closed.");
        }

        #endregion
        
        #region Visuals

        private void RefreshVisuals()
        {
            bool isWhole  = State == BreadState.Full
                            || (State == BreadState.Closed  && ingredientType == Constants.IngredientType.Bread_Full)
                            || (State == BreadState.Toasted && ingredientType == Constants.IngredientType.Bread_Full);

            bool isHalf   = State == BreadState.Half
                            || (State == BreadState.Closed  && ingredientType == Constants.IngredientType.Bread_Half)
                            || (State == BreadState.Toasted && ingredientType == Constants.IngredientType.Bread_Half);

            bool isOpen   = State == BreadState.Open;

            if (wholeBreadVisual != null) wholeBreadVisual.SetActive(isWhole);
            if (halfBreadVisual  != null) halfBreadVisual.SetActive(isHalf);
            if (openBreadVisual  != null) openBreadVisual.SetActive(isOpen);
            // Toasting state: all visuals off — bread is hidden inside the toaster

            // Once toasted, swap the visible visual's renderers to the cooked material.
            // Uses instanced material (.material) so toasting one bread never recolours others.
            if (State == BreadState.Toasted && cookedMaterial != null)
            {
                GameObject cooked = isWhole ? wholeBreadVisual : isHalf ? halfBreadVisual : null;
                if (cooked != null)
                    foreach (Renderer r in cooked.GetComponentsInChildren<Renderer>())
                        r.material = cookedMaterial;
            }
        }

        #endregion
    }
}
