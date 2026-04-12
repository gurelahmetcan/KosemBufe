using UnityEngine;

/// <summary>
/// Draws a simple crosshair at the center of the screen using OnGUI.
/// No Canvas or prefab required — attach this to any active GameObject
/// in the scene (e.g. the Player or Main Camera).
///
/// The crosshair is only visible while the cursor is locked
/// (i.e. during normal gameplay). It disappears when you press Escape.
///
/// Appearance is fully configurable in the Inspector:
///   color     → tint of all four arms
///   armLength → pixel length of each arm extending from the gap
///   armWidth  → pixel thickness of each arm
///   gap       → empty space around the center dot (keeps center clear)
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Appearance")]
    public Color color     = Color.white;
    public int   armLength = 8;
    public int   armWidth  = 2;
    public int   gap       = 3;

    private Texture2D _pixel;

    private void Awake()
    {
        _pixel = new Texture2D(1, 1);
        _pixel.SetPixel(0, 0, Color.white);
        _pixel.Apply();
    }

    private void OnDestroy()
    {
        if (_pixel != null)
            Destroy(_pixel);
    }

    private void OnGUI()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float cx = Screen.width  * 0.5f;
        float cy = Screen.height * 0.5f;
        float hw = armWidth * 0.5f;

        GUI.color = color;

        // Left arm
        Draw(cx - gap - armLength, cy - hw, armLength, armWidth);
        // Right arm
        Draw(cx + gap,             cy - hw, armLength, armWidth);
        // Top arm
        Draw(cx - hw, cy - gap - armLength, armWidth, armLength);
        // Bottom arm
        Draw(cx - hw, cy + gap,             armWidth, armLength);

        GUI.color = Color.white; // restore so other OnGUI code isn't tinted
    }

    private void Draw(float x, float y, float w, float h)
    {
        GUI.DrawTexture(new Rect(x, y, w, h), _pixel);
    }
}
