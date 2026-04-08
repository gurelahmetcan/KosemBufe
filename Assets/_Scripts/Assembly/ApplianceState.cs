/// <summary>
/// State machine for cooking appliances (Grill, Toaster).
///   Empty      → no item inside, ready to accept
///   Processing → item cooking/toasting, timer running
///   Done       → finished, waiting for player to collect (burn grace timer starts)
///   Burned     → left too long, item is ruined
/// </summary>
public enum ApplianceState
{
    Empty,
    Processing,
    Done,
    Burned
}
