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
    [Header("Reputation")]
    [Tooltip("Maximum reputation the player can reach.")]
    public int maxReputation = 100;

    [Tooltip("Reputation the player starts with each run.")]
    public int startingReputation = 50;

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
