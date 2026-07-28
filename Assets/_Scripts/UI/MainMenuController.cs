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
        [SerializeField] private Animator shutterAnimator;
        
        [SerializeField] private float delayBeforeLoad = 2f;
        [SerializeField] private string nextSceneName = "GameScene";

        #endregion

        #region Variables

        private static readonly int OpenHash = Animator.StringToHash("Open");
        private bool isStarting;

        #endregion

        #region Unity Methods

        private void Start()
        {
            menuVcam.Prioritize();
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
            creditsVcam.Prioritize();
        }

        public void OnSettingsButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);
            settingsVcam.Prioritize();
        }

        public void OnBackButtonPressed()
        {
            if(AudioManager.Instance)
                AudioManager.Instance.PlaySfx(Sfx.Click);
            menuVcam.Prioritize();
        }

        #endregion

        #region Private Methods

        private IEnumerator StartSequence()
        {
            if (shutterAnimator != null)
                shutterAnimator.SetTrigger(OpenHash);

            if (startVcam != null)
                startVcam.Prioritize();
            
            yield return new WaitForSeconds(delayBeforeLoad);

            SceneManager.LoadScene(nextSceneName);
        }

        #endregion
    }
}