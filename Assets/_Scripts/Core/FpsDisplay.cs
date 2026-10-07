using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Ekranin kosesine anlik / min / max / ortalama FPS yazar.
    /// Bos bir GameObject'e ekle, build'de de calisir.
    /// </summary>
    public class FpsDisplay : MonoBehaviour
    {
        [Tooltip("Refresh interval (seconds).")]
        public float updateInterval = 0.5f;

        [Tooltip("Font size.")]
        public int fontSize = 28;

        private float _accum;      // interval icindeki fps toplami
        private int _frames;       // interval icindeki frame sayisi
        private float _timeLeft;
        private float _currentFps;
        private float _minFps = float.MaxValue;
        private float _maxFps;
        private GUIStyle _style;

        private void Start()
        {
            _timeLeft = updateInterval;
            // Vsync'i kapat ki gercek FPS'i gorelim (opsiyonel)
            // QualitySettings.vSyncCount = 0;
        }

        private void Update()
        {
            _timeLeft -= Time.unscaledDeltaTime;
            _accum += 1f / Time.unscaledDeltaTime;
            _frames++;

            if (_timeLeft <= 0f)
            {
                _currentFps = _accum / _frames;
                if (_currentFps < _minFps) _minFps = _currentFps;
                if (_currentFps > _maxFps) _maxFps = _currentFps;

                _timeLeft = updateInterval;
                _accum = 0f;
                _frames = 0;
            }
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold
                };
            }

            // Renk: 55+ yesil, 30-55 sari, altinda kirmizi
            Color c = _currentFps >= 55f ? Color.green
                    : _currentFps >= 30f ? Color.yellow
                    : Color.red;
            _style.normal.textColor = c;

            float ms = _currentFps > 0f ? 1000f / _currentFps : 0f;
            string text = $"FPS: {_currentFps:0.0}  ({ms:0.0} ms)\n" +
                          $"min: {_minFps:0.0}   max: {_maxFps:0.0}";

            // Golge (okunurluk icin)
            _style.normal.textColor = Color.black;
            GUI.Label(new Rect(12, 12, 400, 100), text, _style);
            _style.normal.textColor = c;
            GUI.Label(new Rect(10, 10, 400, 100), text, _style);
        }

        // Sag tik menusunden min/max sifirlamak icin
        [ContextMenu("Reset Min/Max")]
        public void ResetMinMax()
        {
            _minFps = float.MaxValue;
            _maxFps = 0f;
        }
    }
}
