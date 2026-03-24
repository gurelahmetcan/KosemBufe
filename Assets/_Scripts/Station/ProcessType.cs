/// <summary>
/// Flags enum describing what cooking processes an item supports.
/// Set per-prefab in the inspector. A station checks whether the held
/// item has the flag it requires before allowing interaction.
/// </summary>
[System.Flags]
public enum ProcessType
{
    None      = 0,
    Toastable = 1 << 0,
    Fryable   = 1 << 1,
    Grillable = 1 << 2,
    Blendable = 1 << 3,
}
