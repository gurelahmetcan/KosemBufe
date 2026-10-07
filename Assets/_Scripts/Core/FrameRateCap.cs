using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Caps the frame rate before the first scene loads. vSync is off in the quality
    /// settings, so without this the GPU renders menus at hundreds of FPS for nothing.
    /// Ignored by Unity whenever vSync is turned on.
    /// </summary>
    public static class FrameRateCap
    {
        public const int Target = 144;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply() => Application.targetFrameRate = Target;
    }
}
