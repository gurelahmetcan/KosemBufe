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
            [Tooltip("What the dayı says on this step.")]
            [TextArea(1, 4)]
            public string text;

            [Tooltip("Caption on the panel button that advances to the next line.")]
            public string buttonLabel = "Devam";
        }

        [Header("Intro Conversation")]
        [Tooltip("Conversation shown on the order panel, one line per button click.")]
        public List<TutorialLine> introLines = new List<TutorialLine>();

        [Header("Order")]
        [Tooltip("Line shown on the order prompt (with the Accept button).")]
        [TextArea(1, 4)]
        public string orderPromptLine = "...";

        [Tooltip("Caption on the Accept button for the order step.")]
        public string orderButtonLabel = "Siparişi Al";

        [Tooltip("The fixed order the dayı asks for (e.g. Yarım Karışık Tost + Ayran).")]
        public List<OrderItem> orderItems = new List<OrderItem>();

        [Header("Exit")]
        [Tooltip("Line shown after the order is delivered, just before the dayı leaves.")]
        [TextArea(1, 4)]
        public string thanksLine = "Eline sağlık!";
    }
}
