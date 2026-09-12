using UnityEngine;

/// <summary>
/// Draws a small round dot at the centre of the screen.
///
/// The dot is deliberately understated: the hover outline is what tells the
/// player what they are aiming at, so the crosshair only needs to mark the
/// centre of the screen without competing with it.
///
/// No Canvas or prefab required — attach this to any active GameObject in the
/// scene (e.g. the Player or Main Camera). It is only visible while the cursor
/// is locked, i.e. during normal gameplay.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Tint of the dot.")]
    public Color color = Color.white;

    [Tooltip("Diameter of the dot in pixels.")]
    public int size = 4;

    // Drawn from a generated circle rather than a 1x1 pixel: a square dot reads
    // as a speck of dirt on the screen, a round one reads as a crosshair.
    private Texture2D _dot;

    private void Awake()
    {
        _dot = BuildDot();
    }

    private void OnDestroy()
    {
        if (_dot != null)
            Destroy(_dot);
    }

    private void OnGUI()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float diameter = Mathf.Max(1, size);
        var rect = new Rect((Screen.width  - diameter) * 0.5f,
                            (Screen.height - diameter) * 0.5f,
                            diameter, diameter);

        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _dot);
        GUI.color = previous;   // restore so other OnGUI code isn't tinted
    }

    /// <summary>
    /// A white disc with a one-pixel feathered rim, drawn at a higher resolution
    /// than it is displayed so the bilinear downscale keeps the edge smooth at
    /// any dot size.
    /// </summary>
    private static Texture2D BuildDot()
    {
        const int resolution = 32;

        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            name       = "CrosshairDot"
        };

        float centre = resolution * 0.5f;
        float radius = centre - 1f;

        var pixels = new Color32[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x + 0.5f - centre;
                float dy = y + 0.5f - centre;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }
}
