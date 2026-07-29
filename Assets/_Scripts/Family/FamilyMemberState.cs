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

            // Snapshot before this day so new states set below don't self-increment.
            bool wasSick     = SicknessDays > 0;
            bool wasCritical = CriticalDays > 0;

            // 1. Hunger / cold counters
            HungerDays = foodPaid ? Mathf.Max(0, HungerDays - 1) : HungerDays + 1;
            ColdDays   = heatPaid ? Mathf.Max(0, ColdDays   - 1) : ColdDays   + 1;

            // Crossing a threshold starts sickness. If already sick, don't restart the counter.
            if (HungerDays >= Data.hungerToSickDays) { HungerDays = 0; if (!wasSick) SicknessDays = 1; }
            if (ColdDays   >= Data.coldToSickDays)   { ColdDays   = 0; if (!wasSick) SicknessDays = 1; }

            // 2. Medicine fully cures
            if (medicinePaid) { SicknessDays = 0; CriticalDays = 0; return; }

            // 3. Sickness auto-increments every day without medicine.
            //    Only runs for members who were already sick at the start of this day
            //    so the counter doesn't double-fire on the day sickness first starts.
            if (wasSick)
            {
                SicknessDays++;
                if (SicknessDays >= Data.sickToCriticalDays)
                {
                    SicknessDays = 0;
                    if (!wasCritical) CriticalDays = 1;
                }
            }

            // 4. Critical auto-increments every day without medicine.
            if (wasCritical)
            {
                CriticalDays++;
                if (CriticalDays >= Data.criticalToGoneDays)
                    IsGone = true;
            }
        }

        // ── Forced health changes (from events) ────────────────────────────────

        /// <summary>Forces the member sick now, bypassing hunger/cold. No-op if gone or already sick/critical.</summary>
        public void ForceSick()
        {
            if (IsGone || SicknessDays > 0 || CriticalDays > 0) return;
            SicknessDays = 1;
        }

        /// <summary>Forces the member into a critical condition now. No-op if gone.</summary>
        public void ForceCritical()
        {
            if (IsGone) return;
            SicknessDays = 0;
            CriticalDays = 1;
        }

        /// <summary>Clears sickness/critical (full recovery). No-op if gone.</summary>
        public void Cure()
        {
            if (IsGone) return;
            SicknessDays = 0;
            CriticalDays = 0;
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
