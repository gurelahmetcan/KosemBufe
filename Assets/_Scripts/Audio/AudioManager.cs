using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bufe
{
    /// <summary>
    /// Central audio hub. All sounds are mapped in the Inspector (SFX + Music libraries)
    /// and played by enum id from anywhere:
    ///   AudioManager.Instance.PlaySfx(Sfx.CashRegister);
    ///   AudioManager.Instance.PlayMusic(MusicTrack.Main);
    ///
    /// SFX play through a small pool of AudioSources; music is one looping source with
    /// crossfade. Put one AudioManager in the scene; it builds its sources at runtime.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Inspector library entries ────────────────────────────────────────────

        [System.Serializable]
        public class SfxEntry
        {
            public Sfx id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random pitch range for variety. Keep both at 1 for none.")]
            public float pitchMin = 1f;
            public float pitchMax = 1f;

            [Header("3D (only used when played positionally, e.g. the toaster)")]
            [Tooltip("Within this distance the sound is at full volume.")]
            public float minDistance = 1.5f;
            [Tooltip("Beyond this distance the sound fades to silence (linear falloff).")]
            public float maxDistance = 15f;
        }

        [System.Serializable]
        public class MusicEntry
        {
            public MusicTrack id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Libraries")]
        [Tooltip("Map each Sfx id to its clip + settings.")]
        [SerializeField] private SfxEntry[] sfxLibrary;
        [Tooltip("Map each MusicTrack id to its clip + settings.")]
        [SerializeField] private MusicEntry[] musicLibrary;

        [Header("Mixing (0–1)")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume    = 1f;
        [Range(0f, 1f)] public float musicVolume  = 1f;

        [Header("SFX")]
        [Tooltip("How many SFX can overlap before the oldest voice is reused.")]
        [SerializeField] private int sfxVoices = 12;

        [Header("Music")]
        [Tooltip("Track to start on load (None = silent).")]
        [SerializeField] private MusicTrack startingMusic = MusicTrack.None;
        [Tooltip("Seconds to crossfade when music changes or stops.")]
        [SerializeField] private float musicFadeDuration = 1f;

        [Header("Lifetime")]
        [Tooltip("Keep this manager (and its music) alive across scene loads.")]
        [SerializeField] private bool persistAcrossScenes = false;

        // ── Runtime ────────────────────────────────────────────────────────────
        private readonly Dictionary<Sfx, SfxEntry>          _sfx   = new Dictionary<Sfx, SfxEntry>();
        private readonly Dictionary<MusicTrack, MusicEntry> _music = new Dictionary<MusicTrack, MusicEntry>();

        private AudioSource[] _sfxSources;
        private int           _sfxCursor;
        private AudioSource   _musicSource;
        private MusicTrack    _currentMusic = MusicTrack.None;
        private Coroutine     _musicFade;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

            BuildLibraries();

            _sfxSources = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < _sfxSources.Length; i++)
                _sfxSources[i] = CreateSource($"SfxVoice_{i}", loop: false);

            _musicSource = CreateSource("Music", loop: true);
        }

        private void Start()
        {
            if (startingMusic != MusicTrack.None) PlayMusic(startingMusic);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildLibraries()
        {
            if (sfxLibrary != null)
                foreach (var e in sfxLibrary)
                {
                    if (e == null || e.id == Sfx.None) continue;
                    if (_sfx.ContainsKey(e.id))
                        Debug.LogWarning($"[AudioManager] Duplicate SFX id '{e.id}' — keeping the first.", this);
                    else
                        _sfx.Add(e.id, e);
                }

            if (musicLibrary != null)
                foreach (var e in musicLibrary)
                {
                    if (e == null || e.id == MusicTrack.None) continue;
                    if (_music.ContainsKey(e.id))
                        Debug.LogWarning($"[AudioManager] Duplicate music id '{e.id}' — keeping the first.", this);
                    else
                        _music.Add(e.id, e);
                }
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.loop         = loop;
            src.spatialBlend = 0f; // 2D by default
            return src;
        }

        // ── SFX ────────────────────────────────────────────────────────────────

        /// <summary>Plays a one-shot SFX in 2D (UI / global sounds).</summary>
        /// <summary>Plays a one-shot SFX in 2D (UI / global sounds).</summary>
        public void PlaySfx(Sfx id) => PlayInternal(id, spatial: false, Vector3.zero);

        /// <summary>Plays a one-shot SFX positioned in the world (3D): louder up close, quieter far.</summary>
        public void PlaySfxAt(Sfx id, Vector3 position) => PlayInternal(id, spatial: true, position);

        private AudioSource PlayInternal(Sfx id, bool spatial, Vector3 position)
        {
            if (id == Sfx.None) return null;
            if (!_sfx.TryGetValue(id, out SfxEntry e) || e.clip == null)
            {
                Debug.LogWarning($"[AudioManager] No clip mapped for Sfx.{id}.", this);
                return null;
            }

            AudioSource src = NextSfxSource();
            src.clip   = e.clip;
            src.volume = e.volume * sfxVolume * masterVolume;
            src.pitch  = e.pitchMin >= e.pitchMax ? e.pitchMin : Random.Range(e.pitchMin, e.pitchMax);
            src.loop   = false;

            if (spatial)
            {
                src.spatialBlend       = 1f;                       // full 3D
                src.rolloffMode        = AudioRolloffMode.Linear;  // intuitive near→far falloff
                src.minDistance        = Mathf.Max(0.01f, e.minDistance);
                src.maxDistance        = Mathf.Max(e.minDistance + 0.01f, e.maxDistance);
                src.transform.position = position;
            }
            else
            {
                src.spatialBlend = 0f;                             // 2D
            }

            src.Play();
            return src;
        }

        /// <summary>
        /// Plays a one-shot SFX <paramref name="count"/> times back-to-back (each after the
        /// previous finishes), then invokes <paramref name="onComplete"/>. 2D version.
        /// </summary>
        public void PlaySfxSequence(Sfx id, int count, System.Action onComplete = null)
            => StartCoroutine(SfxSequenceRoutine(id, count, false, Vector3.zero, onComplete));

        /// <summary>Same as <see cref="PlaySfxSequence"/> but 3D, positioned in the world (e.g. the toaster).</summary>
        public void PlaySfxSequenceAt(Sfx id, int count, Vector3 position, System.Action onComplete = null)
            => StartCoroutine(SfxSequenceRoutine(id, count, true, position, onComplete));

        private IEnumerator SfxSequenceRoutine(Sfx id, int count, bool spatial, Vector3 position, System.Action onComplete)
        {
            if (_sfx.TryGetValue(id, out SfxEntry e) && e.clip != null)
            {
                int n = Mathf.Max(1, count);
                for (int i = 0; i < n; i++)
                {
                    PlayInternal(id, spatial, position);
                    yield return new WaitForSeconds(e.clip.length);
                }
            }
            else
            {
                Debug.LogWarning($"[AudioManager] PlaySfxSequence: no clip for Sfx.{id}.", this);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Starts a looping SFX on a dedicated 3D source at <paramref name="position"/> and
        /// returns it. Keep the reference and pass it to <see cref="StopLoop"/> to stop.
        /// Used for sustained sounds like the fridge-left-open beep.
        /// </summary>
        public AudioSource StartLoop(Sfx id, Vector3 position)
        {
            if (id == Sfx.None) return null;
            if (!_sfx.TryGetValue(id, out SfxEntry e) || e.clip == null)
            {
                Debug.LogWarning($"[AudioManager] StartLoop: no clip for Sfx.{id}.", this);
                return null;
            }

            var go = new GameObject($"Loop_{id}");
            go.transform.SetParent(transform);
            go.transform.position = position;

            var src = go.AddComponent<AudioSource>();
            src.clip         = e.clip;
            src.volume       = e.volume * sfxVolume * masterVolume;
            src.loop         = true;
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.minDistance  = Mathf.Max(0.01f, e.minDistance);
            src.maxDistance  = Mathf.Max(e.minDistance + 0.01f, e.maxDistance);
            src.Play();
            return src;
        }

        /// <summary>Stops and disposes a looping source returned by <see cref="StartLoop"/>.</summary>
        public void StopLoop(AudioSource loopSource)
        {
            if (loopSource != null) Destroy(loopSource.gameObject);
        }

        private AudioSource NextSfxSource()
        {
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                AudioSource s = _sfxSources[(_sfxCursor + i) % _sfxSources.Length];
                if (!s.isPlaying)
                {
                    _sfxCursor = (_sfxCursor + i + 1) % _sfxSources.Length;
                    return s;
                }
            }
            AudioSource stolen = _sfxSources[_sfxCursor];
            _sfxCursor = (_sfxCursor + 1) % _sfxSources.Length;
            return stolen;
        }

        // ── Music ──────────────────────────────────────────────────────────────

        /// <summary>Crossfades to a looping music track. No-op if it's already playing.</summary>
        public void PlayMusic(MusicTrack id)
        {
            if (id == MusicTrack.None) { StopMusic(); return; }
            if (!_music.TryGetValue(id, out MusicEntry e) || e.clip == null)
            {
                Debug.LogWarning($"[AudioManager] No clip mapped for MusicTrack.{id}.", this);
                return;
            }
            if (_currentMusic == id && _musicSource.isPlaying) return;

            _currentMusic = id;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(FadeToMusic(e));
        }

        /// <summary>Fades the current music out and stops it.</summary>
        public void StopMusic()
        {
            _currentMusic = MusicTrack.None;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(FadeOutMusic());
        }

        /// <summary>Re-applies the music volume live (e.g. after a settings slider moves).</summary>
        public void RefreshMusicVolume()
        {
            if (_musicFade != null || _currentMusic == MusicTrack.None) return;
            if (_music.TryGetValue(_currentMusic, out MusicEntry e))
                _musicSource.volume = e.volume * musicVolume * masterVolume;
        }

        private IEnumerator FadeToMusic(MusicEntry e)
        {
            float half   = Mathf.Max(0.0001f, musicFadeDuration * 0.5f);
            float target = e.volume * musicVolume * masterVolume;

            if (_musicSource.isPlaying)
                yield return FadeVolume(_musicSource, _musicSource.volume, 0f, half);

            _musicSource.clip  = e.clip;
            _musicSource.pitch = 1f;
            _musicSource.loop  = true;
            _musicSource.Play();

            yield return FadeVolume(_musicSource, 0f, target, half);
            _musicFade = null;
        }

        private IEnumerator FadeOutMusic()
        {
            yield return FadeVolume(_musicSource, _musicSource.volume, 0f, musicFadeDuration);
            _musicSource.Stop();
            _musicFade = null;
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float dur)
        {
            if (dur <= 0f) { src.volume = to; yield break; }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;   // survives pauses / timeScale 0
                src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t));
                yield return null;
            }
            src.volume = to;
        }
    }
}
