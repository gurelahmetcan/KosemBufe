namespace Bufe
{
    public enum FamilyMemberStatus
    {
        Healthy,
        Hungry,
        Cold,
        HungryAndCold,
        Sick,
        Critical,
        Gone
    }

    /// <summary>
    /// A forced health change an event can apply to a family member, bypassing the normal
    /// hunger/cold buildup (e.g. "mom suddenly falls sick").
    /// </summary>
    public enum FamilyHealthChange
    {
        None,
        MakeSick,
        MakeCritical,
        Cure
    }
}
