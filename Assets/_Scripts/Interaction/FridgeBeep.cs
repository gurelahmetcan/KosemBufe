using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Beeps (looping, 3D from this object) if a HingedOpenable - e.g. the fridge door - is
    /// left fully open longer than <see cref="openThreshold"/> seconds. Stops the moment it
    /// closes. Put this on the fridge and point <see cref="door"/> at its door component.
    /// </summary>
    public class FridgeBeep : MonoBehaviour
    {
        [Tooltip("Door to watch.")]
        [SerializeField] private HingedOpenable door;

        [Tooltip("Open seconds before beeping.")]
        [SerializeField] private float openThreshold = 5f;

        [Tooltip("Looping beep sound.")]
        [SerializeField] private Sfx beepSound = Sfx.Beep;

        [Tooltip("Silent while UI is open. Auto-found.")]
        [SerializeField] private PlayerLook playerLook;

        private float       _openTime;
        private bool        _active;   // beep session started for the current open
        private AudioSource _loop;

        private void Start()
        {
            if (playerLook == null) playerLook = FindAnyObjectByType<PlayerLook>();
        }

        private void Update()
        {
            if (door == null) return;

            // Silence the beep while a full-screen UI covers the game (night events, day-end…).
            if (playerLook != null && playerLook.UIMode)
            {
                if (_active) { _active = false; StopBeep(); }
                return;
            }

            if (door.IsOpen)
            {
                _openTime += Time.deltaTime;
                if (!_active && _openTime >= openThreshold)
                {
                    _active = true;
                    StartBeep();
                }
            }
            else
            {
                _openTime = 0f;
                if (_active)
                {
                    _active = false;
                    StopBeep();
                }
            }
        }

        private void StartBeep()
        {
            if (AudioManager.Instance != null)
                _loop = AudioManager.Instance.StartLoop(beepSound, transform.position);
        }

        private void StopBeep()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopLoop(_loop);
            _loop = null;
        }

        private void OnDisable()
        {
            _active = false;
            StopBeep();
        }
    }
}
