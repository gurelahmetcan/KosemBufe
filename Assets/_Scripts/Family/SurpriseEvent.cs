using UnityEngine;

namespace Bufe
{
    [CreateAssetMenu(fileName = "SurpriseEvent", menuName = "Kosem Bufe/Surprise Event")]
    public class SurpriseEvent : ScriptableObject
    {
        [Tooltip("Short description shown on the day-end screen.")]
        public string description = "Unexpected expense";

        [Min(0f)]
        public float cost;

        [Tooltip("Earliest day this event can trigger.")]
        [Min(1)]
        public int minDay = 1;

        [Tooltip("Probability this event fires on any eligible day.")]
        [Range(0f, 1f)]
        public float chance = 0.2f;
    }
}
