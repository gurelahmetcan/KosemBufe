using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add to the whole Sucuk prefab (alongside its Ingredient component).
///
/// When the player holds the knife and left-clicks the sucuk, it is destroyed
/// and replaced by a configurable number of SucukSlice objects.
///
/// If the sucuk was placed on a cutting board (<see cref="PrepStation"/>), the
/// slices spawn at the sucuk's centre and the board slides them over to its
/// slice anchor, where they can be clicked straight into the open bread. If the
/// sucuk is cut anywhere else, the slices simply spawn stacked in place and the
/// player picks them up one by one.
/// </summary>
namespace Bufe
{
    public class SucukItem : InteractableItem, IAssemblyInteractable
    {
        [Tooltip("Slice prefab. Needs Ingredient.")]
        public GameObject sucukSlicePrefab;

        [Tooltip("Slices per cut.")]
        public int sliceCount = 4;

        [Tooltip("Vertical gap between slices.")]
        public float sliceStackOffset = 0.1f;

        // Set when the sucuk is resting on a cutting board.
        private PrepStation _station;

        // ── Station tracking ──────────────────────────────────────────────────────

        /// <summary>Called by PrepStation when the whole sucuk is placed on the board.</summary>
        public void PlaceOnStation(PrepStation station) => _station = station;

        public override void OnPickedUp()
        {
            if (_station != null)
            {
                _station.OnSucukPickedUp();
                _station = null;
            }
        }

        // ── IAssemblyInteractable ─────────────────────────────────────────────────

        public bool CanInteract(PlayerHand hand)
        {
            if (hand.HeldItem is not KnifeItem) return false;

            // On a board: only cuttable when every slice fits in a free slot -
            // otherwise the overflow would be destroyed.
            if (_station != null && _station.FreeSliceSlots < sliceCount)
                return false;

            return true;
        }

        public void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (sucukSlicePrefab == null)
            {
                Debug.LogWarning("[SucukItem] sucukSlicePrefab is not assigned.");
                return;
            }

            Vector3 spawnPos = transform.position;

            var slices = new List<GameObject>(sliceCount);
            for (int i = 0; i < sliceCount; i++)
            {
                // On a board every slice starts at the centre, then the board
                // slides them to the anchor. Off a board they stack in place.
                Vector3 pos = _station != null
                    ? spawnPos
                    : spawnPos + Vector3.right * (sliceStackOffset * i);

                GameObject obj = Instantiate(sucukSlicePrefab);
                DailySpawn.Tag(obj);
                obj.transform.position = pos;
                slices.Add(obj);
            }

            if (_station != null)
            {
                PrepStation station = _station;
                _station = null;                 // avoid double free via OnPickedUp
                station.OnSucukSliced(this, slices);
            }

            // CanInteract already proved the knife is what did this.
            (hand.HeldItem as KnifeItem)?.ReturnAfterCut(hand);

            Log.Info($"[SucukItem] Sliced into {sliceCount} pieces.");
            Destroy(gameObject);
        }
    }
}
