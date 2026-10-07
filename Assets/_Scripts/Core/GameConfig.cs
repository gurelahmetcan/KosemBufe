using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Central configuration asset for game balance values.
    /// Replaces hardcoded constants scattered across managers.
    ///
    /// Create via: Assets → Create → KosemBufe → Game Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "KosemBufe/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Demo")]
        [Tooltip("Last playable day, then demo-end. 0 = no limit.")]
        public int demoLastDay = 5;

        [Tooltip("Rent the player must save by demo end.")]
        public float rentTarget = 1000f;

        [Header("Money")]
        [Tooltip("Starting money.")]
        public float startingMoney = 500f;

        [Header("Orders")]
        [Tooltip("Seconds to deliver an order.")]
        public float orderDuration = 30f;

        [Tooltip("Delay before the next customer spawns.")]
        public float timeBetweenCustomers = 3f;

        [Header("Customer")]
        [Tooltip("Customer walk speed.")]
        public float customerWalkSpeed = 2f;
    }
}
