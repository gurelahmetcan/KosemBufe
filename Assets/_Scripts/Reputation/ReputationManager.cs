using System;
using UnityEngine;

/// <summary>
/// Tracks the shop's reputation on a 0–max scale.
/// GameManager calls Init() to set max and starting values from GameConfig.
/// Reaching 0 fires OnGameOver which GameManager listens to.
/// Other scripts call ChangeReputation() to add or subtract points.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    /// <summary>Fired whenever the reputation value changes. Passes (current, delta).</summary>
    public event Action<int, int> OnReputationChanged;

    /// <summary>Fired once when reputation reaches 0.</summary>
    public event Action OnGameOver;

    /// <summary>Current reputation value.</summary>
    public int Reputation { get; private set; } = 50;

    /// <summary>Maximum reputation (configured by GameManager).</summary>
    public int MaxReputation { get; private set; } = 100;

    /// <summary>True after reputation has hit 0.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// Called by GameManager.ApplyConfig() to set values from GameConfig.
    /// Overrides the defaults above.
    /// </summary>
    public void Init(int max, int starting)
    {
        MaxReputation = max;
        Reputation    = Mathf.Clamp(starting, 0, max);
    }

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
