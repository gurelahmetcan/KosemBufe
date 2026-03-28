using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Image prefab representing a single fry slice.
/// Handles click-to-collect when placed in the fryer vat during Ready state.
///
/// Prefab setup: Create a small rectangular UI Image, add this component.
/// The Image color is driven by FryerUI at runtime.
/// </summary>
[RequireComponent(typeof(Image))]
public class FrySliceUI : MonoBehaviour, IPointerClickHandler
{
    private Image    _image;
    private FryerUI  _owner;
    private bool     _collected;
    private Animator _animator;

    public bool Collected => _collected;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _animator = GetComponent<Animator>();
    }

    public void Init(FryerUI owner)
    {
        _owner     = owner;
        _collected = false;
        SetCookingAnimation(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_collected && _owner != null)
            _owner.OnSliceClicked(this);
    }

    public void SetCollected(bool collected)
    {
        _collected = collected;
    }

    public void SetColor(Color color)
    {
        if (_image != null)
            _image.color = color;
    }
    
    public void SetCookingAnimation(bool cooking)
    {
        _animator.SetBool("isCooking", cooking);
    }
}
