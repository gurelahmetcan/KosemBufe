using UnityEngine;

namespace Bufe
{
    public enum FamilyMemberType { Wife, Child, Grandparent }

    [CreateAssetMenu(fileName = "FamilyMember", menuName = "Kosem Bufe/Family Member")]
    public class FamilyMemberData : ScriptableObject
    {
        [Header("Identity")]
        public string memberName = "Family Member";
        public FamilyMemberType memberType;

        [Header("Portrait (optional)")]
        public Sprite portrait;

        [Header("Days until state worsens")]
        [Min(1)] public int hungerToSickDays    = 2;
        [Min(1)] public int coldToSickDays      = 2;
        [Min(1)] public int sickToCriticalDays  = 3;
        [Min(1)] public int criticalToGoneDays  = 3;

        [Header("Medicine")]
        [Min(0f)] public float medicineCost = 40f;
    }
}
