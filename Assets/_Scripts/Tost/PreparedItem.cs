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
        [Tooltip("Whole bread visual.")]
        [SerializeField] private GameObject wholeBreadVisual;

        [Tooltip("Half bread visual.")]
        [SerializeField] private GameObject halfBreadVisual;

        [Tooltip("Open bread visual.")]
        [SerializeField] private GameObject openBreadVisual;

        [Tooltip("Toasted bread visual.")]
        [SerializeField] private GameObject cookedBreadVisual;

        [Header("Cutting")]
        [Tooltip("Spawned twice on cut. Needs PreparedItem.")]
        [SerializeField] private GameObject halfBreadPrefab;

        [Tooltip("Half the gap between the halves.")]
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
            // Knife + empty whole loaf → cut in half (works anywhere).
            if (hand.HeldItem is KnifeItem && CanBeCut())
                return true;

            // Everything below requires the bread to be resting on a station.
            if (_station == null)
                return false;

            // Ingredient in hand + Open → add to filling (only if a socket is free).
            if (hand.HeldItem is Ingredient heldIngredient && State == BreadState.Open)
                return HasFreeSocket(heldIngredient.ingredientType);

            return false;
        }

        public void Interact(PlayerHand hand)
        {
            if (hand.HeldItem is KnifeItem knife && CanBeCut())
            {
                // Only send the knife away if the cut actually happened - a
                // misconfigured prefab must not leave the player empty-handed.
                if (Cut()) knife.ReturnAfterCut(hand);
                return;
            }

            if (hand.HeldItem is Ingredient ing && State == BreadState.Open)
            {
                AddIngredient(ing.ingredientType, hand);
            }
        }

        /// <summary>
        /// True if a knife can split this bread: a whole loaf, empty inside, and not
        /// toasting/toasted. Works whether or not it has been opened/closed.
        /// </summary>
        public bool CanBeCut()
        {
            if (ingredientType != Constants.IngredientType.Bread_Full) return false;
            if (physicalIngredients.Count > 0)                          return false;
            return State == BreadState.Full || State == BreadState.Open || State == BreadState.Closed;
        }

        /// <summary>True if the bread can be opened or closed right now (empty-hand right-click on the station).</summary>
        public bool CanToggleOpen()
        {
            if (_station == null) return false;
            return State == BreadState.Full || State == BreadState.Half
                || State == BreadState.Closed || State == BreadState.Open;
        }

        /// <summary>Opens the bread if it's shut, closes it if it's open. Empty-hand right-click on the station.</summary>
        public void ToggleOpen()
        {
            if (!CanToggleOpen()) return;

            if (State == BreadState.Open) Close();
            else                          Open();
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
            Log.Info("[PreparedItem] Toasted!");
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

        /// <summary>Splits the loaf into two halves. Returns false when it could not.</summary>
        private bool Cut()
        {
            if (halfBreadPrefab == null)
            {
                Debug.LogWarning("[PreparedItem] halfBreadPrefab is not assigned - cannot spawn two halves.");
                return false;
            }

            // Free the prep station slot before we destroy ourselves.
            if (_station != null)
            {
                _station.OnPreparedItemPickedUp();
                _station = null;
            }
            
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Knife);

            Vector3    spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;
            Vector3    right    = transform.right;

            for (int i = 0; i < 2; i++)
            {
                float      sign = i == 0 ? -1f : 1f;
                Vector3    pos  = spawnPos + right * (halfSpawnOffset * sign);
                Quaternion rot  = i == 0 ? spawnRot : spawnRot * Quaternion.Euler(0f, 180f, 0f);
                GameObject obj  = Instantiate(halfBreadPrefab, pos, rot);
                DailySpawn.Tag(obj);
                PreparedItem half = obj.GetComponent<PreparedItem>();
                if (half != null)
                    half.InitAsHalf();
            }

            Log.Info("[PreparedItem] Cut into two halves.");
            Destroy(gameObject);
            return true;
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
            Log.Info($"[PreparedItem] Opened ({ingredientType}).");
        }

        public void AddIngredient(Constants.IngredientType type)
        {
            _ingredients.Add(type);
            
            Log.Info($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
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
                Debug.LogWarning($"[PreparedItem] No free {type} socket - cannot place ingredient.");
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
                Debug.LogWarning($"[PreparedItem] No socket group defined for {type} - cannot place ingredient.");
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
            item.RestoreWorldScale();

            // Disable colliders - ingredient is now part of the bread, not independently pickable
            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Remove Rigidbody to prevent physics interference with the bread
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            Log.Info($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
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
            Log.Info("[PreparedItem] Closed.");
        }

        #endregion
        
        #region Visuals

        private void RefreshVisuals()
        {
            bool toasted = State == BreadState.Toasted;

            bool isWhole  = !toasted && (State == BreadState.Full
                            || (State == BreadState.Closed && ingredientType == Constants.IngredientType.Bread_Full));

            bool isHalf   = !toasted && (State == BreadState.Half
                            || (State == BreadState.Closed && ingredientType == Constants.IngredientType.Bread_Half));

            bool isOpen   = !toasted && State == BreadState.Open;

            if (wholeBreadVisual  != null) wholeBreadVisual.SetActive(isWhole);
            if (halfBreadVisual   != null) halfBreadVisual.SetActive(isHalf);
            if (openBreadVisual   != null) openBreadVisual.SetActive(isOpen);
            if (cookedBreadVisual != null) cookedBreadVisual.SetActive(toasted);
            // Toasting state: everything off - the bread is hidden inside the toaster.
        }

        #endregion
    }
}
