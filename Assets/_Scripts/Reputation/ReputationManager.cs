using System;
using UnityEngine;

/// <summary>
/// Tracks the shop's reputation on a 0–100 scale.
/// Starts at 50. Reaching 0 triggers a Game Over.
/// Other scripts call ChangeReputation() to add or subtract points.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public const int MaxReputation = 100;
    public const int StartingReputation = 50;

    /// <summary>Fired whenever the reputation value changes. Passes (current, delta).</summary>
    public event Action<int, int> OnReputationChanged;

    /// <summary>Fired once when reputation reaches 0.</summary>
    public event Action OnGameOver;

    /// <summary>Current reputation value (0–100).</summary>
    public int Reputation { get; private set; } = StartingReputation;

    /// <summary>True after reputation has hit 0.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// Add or subtract reputation points.
    /// Positive = gain, negative = penalty.
    /// </summary>
    public void ChangeReputation(int delta)
    {
        if (IsGameOver) return;

        int previous = Reputation;
        Reputation = Mathf.Clamp(Reputation + delta, 0, MaxReputation);
        int actualDelta = Reputation - previous;

        if (actualDelta != 0)
        {
            Debug.Log($"[Reputation] {previous} → {Reputation} ({(actualDelta > 0 ? "+" : "")}{actualDelta})");
            OnReputationChanged?.Invoke(Reputation, actualDelta);
        }

        if (Reputation <= 0)
        {
            IsGameOver = true;
            Debug.Log("[Reputation] GAME OVER — reputation reached 0!");
            OnGameOver?.Invoke();
        }
    }
}
