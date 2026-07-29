using UnityEngine;

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
    [Tooltip("Last playable day. When it ends, the demo-end screen shows instead of the next day. 0 = no limit.")]
    public int demoLastDay = 5;

    [Tooltip("Rent money the player is trying to save up by the end of the demo (the demo's goal).")]
    public float rentTarget = 1000f;

    [Header("Money")]
    [Tooltip("Starting money balance for a new game.")]
    public float startingMoney = 500f;

    [Header("Orders")]
    [Tooltip("Seconds the player has to deliver an order.")]
    public float orderDuration = 30f;

    [Tooltip("Seconds to wait after a customer leaves before the next one spawns.")]
    public float timeBetweenCustomers = 3f;

    [Header("Customer")]
    [Tooltip("Customer walk speed in units per second.")]
    public float customerWalkSpeed = 2f;
}
