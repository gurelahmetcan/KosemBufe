using UnityEngine;

/// <summary>
/// Configurable parameters for the Ingredient Vending Machine.
/// Create via Assets → Create → KosemBufe → Vending Machine Config.
///
/// Default values match the spec JSON:
///   JamProbability  = 0.15
///   DispenseDuration = 2.0
///   KickForce       = 7.5
///   ItemCooldown    = 0.5
///   MaxStockPerSlot = 10
/// </summary>
[CreateAssetMenu(fileName = "VendingMachineConfig", menuName = "KosemBufe/Vending Machine Config")]
public class VendingMachineConfig : ScriptableObject
{
    [Tooltip("Probability (0–1) that a jam occurs during dispensing.")]
    [Range(0f, 1f)]
    public float jamProbability = 0.15f;

    [Tooltip("Seconds for the auger spiral to complete one full turn.")]
    public float dispenseDuration = 2.0f;

    [Tooltip("Impulse magnitude applied to the machine when the player kicks it.")]
    public float kickForce = 7.5f;

    [Tooltip("Seconds before buttons reactivate after the player picks up an item.")]
    public float itemCooldown = 0.5f;

    [Tooltip("Maximum number of items each slot can hold.")]
    public int maxStockPerSlot = 10;
}
