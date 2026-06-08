using UnityEngine;

namespace Bufe
{
    public class FamilyMemberState
    {
        public FamilyMemberData Data { get; }

        public int HungerDays   { get; private set; }
        public int ColdDays     { get; private set; }
        public int SicknessDays { get; private set; }
        public int CriticalDays { get; private set; }
        public bool IsGone      { get; private set; }

        public FamilyMemberStatus Status => DeriveStatus();
        public bool NeedsMedicine => SicknessDays > 0 || CriticalDays > 0;

        public FamilyMemberState(FamilyMemberData data)
        {
            Data = data;
        }

        // Called once per day by FamilyManager after the player confirms the day-end screen.
        public void ProcessDayEnd(bool foodPaid, bool heatPaid, bool medicinePaid)
        {
            if (IsGone) return;

            // 1. Update hunger and cold counters (paying reduces them, not paying increases them)
            HungerDays = foodPaid ? Mathf.Max(0, HungerDays - 1) : HungerDays + 1;
            ColdDays   = heatPaid ? Mathf.Max(0, ColdDays   - 1) : ColdDays   + 1;

            // 2. Crossing a threshold converts to sickness and resets the counter
            if (HungerDays >= Data.hungerToSickDays)
            {
                HungerDays = 0;
                SicknessDays++;
            }

            if (ColdDays >= Data.coldToSickDays)
            {
                ColdDays = 0;
                SicknessDays++;
            }

            // 3. Medicine fully cures sickness and critical (apply before threshold checks)
            if (medicinePaid)
            {
                SicknessDays = 0;
                CriticalDays = 0;
            }

            // 4. Sustained sickness becomes critical
            if (SicknessDays >= Data.sickToCriticalDays)
            {
                SicknessDays = 0;
                CriticalDays++;
            }

            // 5. Critical eventually means gone
            if (CriticalDays >= Data.criticalToGoneDays)
                IsGone = true;
        }

        private FamilyMemberStatus DeriveStatus()
        {
            if (IsGone)           return FamilyMemberStatus.Gone;
            if (CriticalDays > 0) return FamilyMemberStatus.Critical;
            if (SicknessDays > 0) return FamilyMemberStatus.Sick;
            if (HungerDays > 0 && ColdDays > 0) return FamilyMemberStatus.HungryAndCold;
            if (HungerDays > 0)   return FamilyMemberStatus.Hungry;
            if (ColdDays > 0)     return FamilyMemberStatus.Cold;
            return FamilyMemberStatus.Healthy;
        }
    }
}
