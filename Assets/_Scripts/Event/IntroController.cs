using System.Collections;
using UnityEngine;
using Bufe.UI;

namespace Bufe
{
    /// <summary>
    /// Plays the opening conversation on the living-room event screen when the GameScene
    /// loads, then fades to black and hands control to the büfe. The screen starts black
    /// (ScreenFader.startBlack) so the büfe never flashes before the intro; music starts
    /// only once the intro ends. Normal day flow (shutter, day-1 tutorial dayı) continues
    /// afterwards.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private IntroSequence intro;

        [Header("References")]
        [SerializeField] private EventScreenUI eventScreenUI;
        [SerializeField] private PlayerLook    playerLook;
        [SerializeField] private ScreenFader   screenFader;

        [Header("Music")]
        [Tooltip("Music started once the intro ends and the büfe appears. None = no music. " +
                 "Set AudioManager's Starting Music to None so it doesn't play during the intro.")]
        [SerializeField] private MusicTrack musicOnComplete = MusicTrack.Main;

        private int _index;

        private void Start()
        {
            bool hasIntro = intro != null && intro.lines != null && intro.lines.Count > 0;

            if (hasIntro)
            {
                if (playerLook != null) playerLook.UIMode = true;   // block movement, free cursor
                StartCoroutine(PlayIntro());
            }
            else
            {
                // No intro authored → just reveal the büfe (the fader may have started black) + music.
                StartCoroutine(RevealBufe());
            }
        }

        private IEnumerator PlayIntro()
        {
            yield return null;                 // let EventScreenUI.Start disable its root first

            _index = 0;
            ShowLine();                        // first line — still hidden behind the black fader

            if (screenFader != null)
                yield return screenFader.FadeIn();   // now reveal the intro screen
        }

        private void ShowLine()
        {
            if (intro == null || _index >= intro.lines.Count || eventScreenUI == null)
            {
                EndIntro();
                return;
            }

            IntroSequence.Line line = intro.lines[_index];
            eventScreenUI.ShowConversationLine(line.speaker, line.text, line.buttonLabel, Advance);
        }

        private void Advance()
        {
            _index++;
            ShowLine();
        }

        private void EndIntro()
        {
            StartCoroutine(EndRoutine());
        }

        private IEnumerator EndRoutine()
        {
            if (screenFader != null) yield return screenFader.FadeOut();   // to black

            if (eventScreenUI != null) eventScreenUI.Hide();
            if (playerLook    != null) playerLook.UIMode = false;          // hand control to the büfe

            StartMusic();

            if (screenFader != null) yield return screenFader.FadeIn();    // reveal the büfe
        }

        private IEnumerator RevealBufe()
        {
            if (screenFader != null) yield return screenFader.FadeIn();
            StartMusic();
        }

        private void StartMusic()
        {
            if (musicOnComplete != MusicTrack.None && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(musicOnComplete);
        }
    }
}
