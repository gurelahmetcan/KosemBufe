using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Resets the world to its start state at the beginning of every day.
    /// Subscribes to <see cref="DayManager.OnNewDayReset"/> (which fires while the
    /// day-end UI / fade still covers the screen) and:
    ///   • teleports the player back to the configured start point,
    ///   • clears whatever the player was holding,
    ///   • destroys every leftover runtime-spawned item (see <see cref="DailySpawn"/>).
    /// </summary>
    public class DayResetManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("DayManager whose OnNewDayReset drives the reset.")]
        public DayManager dayManager;

        [Tooltip("The Player root (the object with the CharacterController).")]
        public Transform player;

        [Tooltip("Empty transform marking where the player should stand each morning.")]
        public Transform startPoint;

        [Tooltip("Player's look script — its pitch is reset to level. Optional.")]
        public PlayerLook playerLook;

        [Tooltip("Player's hand — anything held is destroyed on reset. Optional.")]
        public PlayerHand playerHand;

        [Header("Options")]
        [Tooltip("Destroy leftover runtime-spawned items (cut bread, dropped pickups, etc.).")]
        public bool clearLeftoverItems = true;

        private void OnEnable()
        {
            if (dayManager != null) dayManager.OnNewDayReset += ResetWorld;
        }

        private void OnDisable()
        {
            if (dayManager != null) dayManager.OnNewDayReset -= ResetWorld;
        }

        /// <summary>Runs the full reset. Also exposed for manual/debug triggering.</summary>
        [ContextMenu("Reset World Now")]
        public void ResetWorld()
        {
            // Empty the hand first so we don't leave a dangling reference to an
            // item that ClearAll would otherwise destroy underneath it.
            if (playerHand != null)
                playerHand.DestroyHeldItem();

            if (clearLeftoverItems)
                DailySpawn.ClearAll();

            TeleportPlayerToStart();
        }

        private void TeleportPlayerToStart()
        {
            if (player == null || startPoint == null) return;

            // Disable the CharacterController while repositioning — otherwise its
            // internal collision solver overrides the transform write.
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Only take the start point's yaw for the body; keep it upright.
            Quaternion yaw = Quaternion.Euler(0f, startPoint.eulerAngles.y, 0f);
            player.SetPositionAndRotation(startPoint.position, yaw);

            if (cc != null) cc.enabled = true;

            if (playerLook != null)
                playerLook.ResetPitch();
        }
    }
}
