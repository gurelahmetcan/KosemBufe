using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

namespace Bufe.UI
{
    public class MainMenuController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private CinemachineCamera menuVcam;
        [SerializeField] private CinemachineCamera settingsVcam;
        [SerializeField] private CinemachineCamera creditsVcam;
        [SerializeField] private CinemachineCamera startVcam;

        [Header("Shutter")]
        [Tooltip("Shutter that rises on Start.")]
        [SerializeField] private Transform menuShutter;
        [Tooltip("Rise height.")]
        [SerializeField] private float shutterRise = 3f;
        [Tooltip("Open time.")]
        [SerializeField] private float shutterOpenDuration = 1.5f;

        [Header("Camera Priorities")]
        [Tooltip("Active camera priority.")]
        [SerializeField] private int activePriority   = 20;
        [SerializeField] private int inactivePriority = 0;

        [SerializeField] private float delayBeforeLoad = 2f;
        [SerializeField] private string nextSceneName = "GameScene";

        #endregion

        #region Variables

        private bool isStarting;
        private CinemachineCamera[] _cameras;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            // Safety net: cursor state is global and survives scene loads, so whatever the
            // gameplay scene left behind (locked + hidden) would carry into the menu and make
            // it unclickable. Free it on every entry into the menu, whatever the path in.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            _cameras = new[] { menuVcam, settingsVcam, creditsVcam, startVcam };

            // Set the menu live before the first Cinemachine update so the game opens on
            // the menu with no blend from another camera.
            ShowCamera(menuVcam);
        }

        #endregion

        #region Public Methods

        public void OnStartButtonPressed()
        {
            if (isStarting) return;
            isStarting = true;
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

            StartCoroutine(StartSequence());
        }

        public void OnCreditsButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);
            ShowCamera(creditsVcam);
        }

        public void OnSettingsButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);
            ShowCamera(settingsVcam);
        }

        public void OnBackButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);
            ShowCamera(menuVcam);
        }

        public void OnQuitButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Makes <paramref name="target"/> the live camera by giving it the highest priority
        /// and dropping every other camera to the inactive value. Deterministic - no reliance
        /// on Prioritize()/tie-breaks.
        /// </summary>
        private void ShowCamera(CinemachineCamera target)
        {
            if (_cameras == null) return;

            foreach (CinemachineCamera cam in _cameras)
                if (cam != null)
                    cam.Priority = (cam == target) ? activePriority : inactivePriority;
        }

        private IEnumerator StartSequence()
        {
            ShowCamera(startVcam);

            if (menuShutter != null)
                StartCoroutine(RaiseShutter());

            yield return new WaitForSeconds(delayBeforeLoad);

            SceneManager.LoadScene(nextSceneName);
        }

        /// <summary>Slides the shutter straight up over shutterOpenDuration.</summary>
        private IEnumerator RaiseShutter()
        {
            Vector3 startPos = menuShutter.position;
            Vector3 endPos   = startPos + Vector3.up * shutterRise;
            float   dur      = Mathf.Max(0.0001f, shutterOpenDuration);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                menuShutter.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }

            menuShutter.position = endPos;
        }

        #endregion
    }
}