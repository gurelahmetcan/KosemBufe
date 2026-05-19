using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class InteractableItem : MonoBehaviour
    {
        [Tooltip("Display name shown in the hand UI (e.g. 'Sucuk', 'Ekmek').")]
        public string itemName = "Item";

        [Tooltip("Which ingredient this item represents (used for recipe validation).")]
        public Constants.IngredientType ingredient = Constants.IngredientType.None;

        [HideInInspector] public List<Constants.IngredientType> preparedIngredients = new();
        [HideInInspector] public Constants.CookState cookState = Constants.CookState.Raw;

        public Quaternion OriginalLocalRotation { get; private set; }
        public Vector3    OriginalLocalScale    { get; private set; }

        protected virtual void Awake()
        {
            OriginalLocalRotation = transform.localRotation;
            OriginalLocalScale    = transform.localScale;
        }

        public virtual void OnPickedUp() { }
        public virtual void OnDropped()  { }
    }
}
