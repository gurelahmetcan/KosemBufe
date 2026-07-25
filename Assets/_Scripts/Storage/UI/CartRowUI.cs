using UnityEngine;
using TMPro;

namespace Bufe.UI
{
    public class CartRowUI : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI priceText;

        public void Set(string label, string price)
        {
            if (labelText != null) labelText.text = label;
            if (priceText != null) priceText.text = price;
        }
    }
}
