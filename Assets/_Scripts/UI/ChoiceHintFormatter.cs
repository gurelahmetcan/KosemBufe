using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Bufe.UI
{
    /// <summary>
    /// Builds a choice-button label: the choice text, then its outcomes on a new
    /// line (e.g. "(+₺20) (+2 REP)"). Money, family-relationship and faction
    /// deltas are colorized — green for gains, red for losses. Shared by the night
    /// event screen (EventScreenUI) and the day faction prompt (CustomerOrderUI)
    /// so both read identically.
    /// </summary>
    public static class ChoiceHintFormatter
    {
        /// <summary>Short label for relationship / faction reputation changes (e.g. "+2 REP").</summary>
        private const string RepLabel = "REP";

        public static string BuildLabel(EventChoice choice, Color positive, Color negative)
        {
            if (choice == null) return string.Empty;

            var sb = new StringBuilder(choice.label);

            bool first = true;
            foreach (var effect in choice.effects)
            {
                string hint = BuildEffectHint(effect, positive, negative);
                if (string.IsNullOrEmpty(hint)) continue;

                sb.Append(first ? " " : " ");   // outcomes drop to a new line under the label
                sb.Append(hint);
                first = false;
            }

            return sb.ToString();
        }

        private static string BuildEffectHint(EventEffect effect, Color positive, Color negative)
        {
            var parts = new List<string>();

            if (effect.moneyDelta != 0f)
            {
                string money = $"{(effect.moneyDelta > 0f ? "+" : "-")}₺{Mathf.Abs(effect.moneyDelta):F0}";
                parts.Add(Colorize($"({money})", effect.moneyDelta > 0f, positive, negative));
            }

            if (effect.targetMember != null && effect.relationshipDelta != 0)
            {
                int r = effect.relationshipDelta;
                string rel = $"{(r > 0 ? "+" : "-")}{Mathf.Abs(r)} {RepLabel}";
                parts.Add(Colorize($"({rel})", r > 0, positive, negative));
            }

            if (effect.targetFaction != null && effect.factionDelta != 0)
            {
                int f = effect.factionDelta;
                string fac = $"{(f > 0 ? "+" : "-")}{Mathf.Abs(f)} {RepLabel}";
                parts.Add(Colorize($"({fac})", f > 0, positive, negative));
            }

            return string.Join(" ", parts);
        }

        private static string Colorize(string text, bool positive, Color pos, Color neg)
        {
            Color c = positive ? pos : neg;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
        }
    }
}
