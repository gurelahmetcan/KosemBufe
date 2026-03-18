using System;
using UnityEngine;

/// <summary>
/// Maps an OrderResult to a pool of possible reaction lines.
/// Used inside CustomerMood to give mood-appropriate feedback.
/// </summary>
[Serializable]
public struct ReactionLine
{
    public OrderResult result;

    [TextArea(1, 3)]
    public string[] lines;
}
