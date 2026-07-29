using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// The game's opening conversation, played on the living-room event screen before the
    /// player takes over the büfe. Linear (no choices) — advanced line by line.
    ///
    /// Create via: Assets → Create → Kosem Bufe → Intro Sequence
    /// </summary>
    [CreateAssetMenu(fileName = "IntroSequence", menuName = "Kosem Bufe/Intro Sequence")]
    public class IntroSequence : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("Who speaks this line. Empty = narration (no portrait / name).")]
            public FamilyMemberData speaker;

            [TextArea(2, 5)]
            public string text;

            [Tooltip("Advance-button caption for this line (e.g. 'Next', 'Devam', 'Başla').")]
            public string buttonLabel = "Next";
        }

        public List<Line> lines = new List<Line>();
    }
}
