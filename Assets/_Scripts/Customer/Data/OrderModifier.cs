using UnityEngine;

/// <summary>
/// A small cosmetic tweak to an order (e.g. "no tomato", "extra pickles").
/// Normal customers use these for flavour text only.
/// Create via: Assets → Create → KosemBufe → Order Modifier
/// </summary>
[CreateAssetMenu(fileName = "NewModifier", menuName = "KosemBufe/Order Modifier")]
public class OrderModifier : ScriptableObject
{
    [Tooltip("Internal name (e.g. 'NoTomato').")]
    public string modifierName;

    [Tooltip("How the customer phrases it (e.g. 'hold the tomato').")]
    public string displayText;
}
