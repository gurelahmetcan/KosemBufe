using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    public class InteractableItem : MonoBehaviour
    {
        [Tooltip("Display name shown in the hand UI.")]
        public string itemName = "Item";
        
        public Quaternion InHandLocalRotation = Quaternion.identity;
        public Vector3 InHandLocalPos = Vector3.zero;

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
