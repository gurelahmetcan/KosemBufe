using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space overlay UI that shows the customer's order and lets the player
/// Accept, Reject, or Close (dismiss without deciding).
///
/// Scene Setup:
///   1. Create a Canvas (Screen Space – Overlay) with a panel as a child.
///   2. Inside the panel add:
///        - A TextMeshProUGUI for the recipe name.
///        - Three Buttons: Accept, Reject, Close.
///   3. Drag references into this component's fields.
///   The panel starts hidden and is shown/hidden via Show() / Hide().
/// </summary>
public class OrderPromptUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The panel that contains all prompt elements.")]
    public GameObject panel;

    [Tooltip("Text element that displays the recipe name.")]
    public TextMeshProUGUI recipeName;

    [Tooltip("Button to accept the order.")]
    public Button acceptButton;

    [Tooltip("Button to reject the order (customer leaves).")]
    public Button rejectButton;

    [Tooltip("Button to close the prompt without deciding.")]
    public Button closeButton;

    /// <summary>Fired when the player clicks Accept.</summary>
    public event Action OnAccept;

    /// <summary>Fired when the player clicks Reject.</summary>
    public event Action OnReject;

    /// <summary>Fired when the player clicks Close.</summary>
    public event Action OnClose;

    /// <summary>True while the prompt is visible on screen.</summary>
    public bool IsVisible => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (acceptButton != null) acceptButton.onClick.AddListener(HandleAccept);
        if (rejectButton != null) rejectButton.onClick.AddListener(HandleReject);
        if (closeButton != null)  closeButton.onClick.AddListener(HandleClose);

        Hide();
    }

    /// <summary>Show the prompt with the given recipe name.</summary>
    public void Show(string recipe)
    {
        if (recipeName != null)
            recipeName.text = recipe;

        if (panel != null)
            panel.SetActive(true);
    }

    /// <summary>Hide the prompt.</summary>
    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void HandleAccept()
    {
        Hide();
        OnAccept?.Invoke();
    }

    private void HandleReject()
    {
        Hide();
        OnReject?.Invoke();
    }

    private void HandleClose()
    {
        Hide();
        OnClose?.Invoke();
    }
}
