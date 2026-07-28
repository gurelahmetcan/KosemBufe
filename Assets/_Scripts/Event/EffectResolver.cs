using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Single place that applies a list of EventEffects (money / family / faction / item grant).
    /// Used by both the night flow (EventScreenManager) and the büfe day flow (OrderManager) so
    /// the two can't drift apart.
    ///
    /// Item grant needs a physical spawn point, so it only happens when one is supplied (the büfe
    /// counter). Night events pass null and simply skip item grants.
    ///
    /// Follow-up scheduling lives on EventChoice (not EventEffect) and stays with the callers,
    /// since collect outcomes (onCollectedEffects) are bare effect lists with no follow-up.
    /// </summary>
    public static class EffectResolver
    {
        public static void Apply(
            IList<EventEffect>  effects,
            MoneyManager        money,
            RelationshipManager family,
            FactionManager      faction,
            Transform           itemGrantPoint = null,
            string              moneyReason    = "Event")
        {
            if (effects == null) return;

            foreach (EventEffect effect in effects)
            {
                if (effect.moneyDelta != 0f && money != null)
                {
                    if (effect.moneyDelta < 0f) money.DeductExpense(-effect.moneyDelta, moneyReason);
                    else                        money.AddIncome(effect.moneyDelta);
                }

                if (effect.targetMember != null && effect.relationshipDelta != 0)
                    family?.Modify(effect.targetMember, effect.relationshipDelta);

                if (effect.targetFaction != null && effect.factionDelta != 0)
                    faction?.Modify(effect.targetFaction, effect.factionDelta);

                if (effect.grantedItemPrefab != null)
                {
                    if (itemGrantPoint != null)
                        DailySpawn.Tag(Object.Instantiate(effect.grantedItemPrefab, itemGrantPoint.position, itemGrantPoint.rotation).gameObject);
                    else
                        Debug.LogWarning($"[EffectResolver] '{effect.grantedItemPrefab.itemName}' has no grant point — item skipped.");
                }
            }
        }
    }
}
