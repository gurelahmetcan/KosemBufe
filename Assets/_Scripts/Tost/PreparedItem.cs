using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
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
        
        public List<GameObject> physicalIngredients = new();
        public Constants.CookState cookState = Constants.CookState.Raw;

        #endregion

        // ── Runtime state ─────────────────────────────────────────────────────────

        public BreadState State    { get; private set; } = BreadState.Full;
        
        private List<Constants.IngredientType> Ingredients { get; } = new ();

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

            // Empty hand + Whole or Cut → open the bread
            if (hand.IsHandEmpty && (State == BreadState.Full || State == BreadState.Half))
                return true;

            // Ingredient in hand + Open → add to filling
            if (!hand.IsHandEmpty && hand.HeldItem is Ingredient && State == BreadState.Open)
                return true;

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

            if (hand.IsHandEmpty && (State == BreadState.Full || State == BreadState.Half))
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

        private void Cut()
        {
            State = BreadState.Half;
            ingredientType = Constants.IngredientType.Bread_Half;
            RefreshVisuals();
            Debug.Log("[PreparedItem] Cut to half.");
        }

        private void Open()
        {
            State = BreadState.Open;
            RefreshVisuals();
            Debug.Log($"[PreparedItem] Opened ({ingredientType}).");
        }

        public void AddIngredient(Constants.IngredientType type)
        {
            Ingredients.Add(type);
            
            Debug.Log($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
        }

        public void AddIngredient(Constants.IngredientType type, PlayerHand hand)
        {
            Ingredients.Add(type);

            // Keep the ingredient visible inside the open bread rather than destroying it.
            InteractableItem item = hand.Release();
            if (item != null)
            {
                Transform anchor = openBreadVisual != null
                    ? openBreadVisual.transform
                    : transform;

                physicalIngredients.Add(item.gameObject);
                item.transform.SetParent(anchor);
                // Stack each ingredient slightly above the previous one so they don't z-fight
                item.transform.localPosition = new Vector3(0f, 0f, 0.02f * (Ingredients.Count - 1));
                //item.transform.localRotation = Quaternion.identity;

                // Disable colliders — ingredient is now part of the bread, not independently pickable
                foreach (Collider col in item.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // Remove Rigidbody to prevent physics interference with the bread
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
            }

            Debug.Log($"[PreparedItem] Added {type}. Filling: [{string.Join(", ", Ingredients)}]");
        }

        private void Close()
        {
            State = BreadState.Closed;
            AddIngredient(ingredientType);
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
        }

        #endregion
    }
}
