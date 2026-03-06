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

    [Header("Mash Meter (Jam Recovery)")]
    [Tooltip("Number of key presses required to clear a jam.")]
    public int mashPressesRequired = 7;

    [Tooltip("Progress gained per key press (0–1). Auto-calculated from mashPressesRequired if left at 0.")]
    public float mashProgressPerPress = 0f;

    [Tooltip("Progress lost per second when the player stops mashing.")]
    public float mashDecayRate = 0.15f;

    /// <summary>Effective progress per press — uses mashPressesRequired as fallback.</summary>
    public float EffectiveMashPerPress =>
        mashProgressPerPress > 0f ? mashProgressPerPress : 1f / Mathf.Max(1, mashPressesRequired);
}
