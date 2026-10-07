using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Authoring data for the day-1 tutorial visit ("tutorial dayı").
    /// All player-facing text lives here so it can be edited in the Inspector.
    ///
    /// Flow: the dayı walks to the counter, then the order panel stays open the
    /// whole time. Each <see cref="TutorialLine"/> shows its text with an advance
    /// button (label from the line); clicking it moves to the next line. After the
    /// last line a forced-accept order (built from <see cref="orderItems"/>) is
    /// shown. On delivery the day's clock starts ticking.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialData", menuName = "KosemBufe/Tutorial Data")]
    public class TutorialData : ScriptableObject
    {
        [Serializable]
        public class TutorialLine
        {
            [Tooltip("Dayı's line.")]
            [TextArea(1, 4)]
            public string text;

            [Tooltip("Next button text.")]
            public string buttonLabel = "Devam";
        }

        [Header("Intro Conversation")]
        [Tooltip("Lines, one per click.")]
        public List<TutorialLine> introLines = new List<TutorialLine>();

        [Header("Order")]
        [Tooltip("Order prompt line.")]
        [TextArea(1, 4)]
        public string orderPromptLine = "...";

        [Tooltip("Accept button text.")]
        public string orderButtonLabel = "Siparişi Al";

        [Tooltip("Fixed tutorial order.")]
        public List<OrderItem> orderItems = new List<OrderItem>();

        [Header("Exit")]
        [Tooltip("Line after delivery.")]
        [TextArea(1, 4)]
        public string thanksLine = "Eline sağlık!";
    }
}
