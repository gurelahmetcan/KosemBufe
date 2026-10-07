using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bufe.UI
{
    public class FamilyRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Hidden if no portrait is set.")]
        public Image portrait;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;

        [Tooltip("Parent for medicine rows.")]
        public Transform needsContainer;

        [Header("Status Colors")]
        public Color healthyColor      = Color.white;
        public Color hungryColor       = new Color(1f,    0.70f, 0.10f, 1f);
        public Color coldColor         = new Color(0.40f, 0.80f, 1.00f, 1f);
        public Color sickColor         = new Color(0.85f, 0.50f, 0.10f, 1f);
        public Color criticalColor     = new Color(0.90f, 0.10f, 0.10f, 1f);
        public Color goneColor         = new Color(0.50f, 0.50f, 0.50f, 1f);

        // ── Setup ─────────────────────────────────────────────────────────────────

        public void Setup(FamilyMemberState state)
        {
            if (portrait != null)
            {
                bool hasSprite = state.Data.portrait != null;
                portrait.gameObject.SetActive(hasSprite);
                if (hasSprite) portrait.sprite = state.Data.portrait;
            }

            if (nameText != null)
                nameText.text = state.Data.memberName.ToUpper();

            RefreshStatus(state.Status);
        }

        /// <summary>
        /// Spawns a medicine expense row inside needsContainer and returns it.
        /// DayEndScreen tracks the returned row to know if medicine was paid.
        /// Returns null if needsContainer or prefab is missing.
        /// </summary>
        public ExpenseRowUI SpawnMedicineRow(GameObject expenseRowPrefab, float cost)
        {
            if (needsContainer == null || expenseRowPrefab == null) return null;

            var go  = Instantiate(expenseRowPrefab, needsContainer);
            var row = go.GetComponent<ExpenseRowUI>();
            if (row != null) row.Setup("MEDICINE", cost, isMandatory: false);
            return row;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void RefreshStatus(FamilyMemberStatus status)
        {
            if (statusText == null) return;

            (statusText.text, statusText.color) = status switch
            {
                FamilyMemberStatus.Healthy       => ("HEALTHY",        healthyColor),
                FamilyMemberStatus.Hungry        => ("HUNGRY",         hungryColor),
                FamilyMemberStatus.Cold          => ("COLD",           coldColor),
                FamilyMemberStatus.HungryAndCold => ("HUNGRY & COLD",  sickColor),
                FamilyMemberStatus.Sick          => ("SICK",           sickColor),
                FamilyMemberStatus.Critical      => ("CRITICAL",       criticalColor),
                FamilyMemberStatus.Gone          => ("GONE",           goneColor),
                _                                => ("UNKNOWN",        Color.white)
            };
        }
    }
}
