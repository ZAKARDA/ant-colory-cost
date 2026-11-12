using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
        public bool MusicEnabled { get; private set; }
        public bool SoundEnabled { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = 0.24f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.volume = 0.35f;

            LoadAllAudioClips();
            ReloadPreferences();
        }

        public void ReloadPreferences()
        {
            // Existing installations keep their previous master sound preference.
            int legacyEnabled = PlayerPrefs.GetInt("Antchemy_Muted", 0) == 1 ? 0 : 1;
            MusicEnabled = PlayerPrefs.GetInt("Antchemy_MusicEnabled", legacyEnabled) == 1;
            SoundEnabled = PlayerPrefs.GetInt("Antchemy_SoundEnabled", legacyEnabled) == 1;
            _musicSource.mute = !MusicEnabled;
            _sfxSource.mute = !SoundEnabled;
        }

        private void OnDestroy() { if(Instance == this) Instance = null; }

        private void LoadAllAudioClips()
        {
            // Restrict discovery to audio; scanning every Resources asset can load
            // Editor-only SDK settings whose scripts do not exist in a player.
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
            foreach (var clip in clips)
            {
                if (!_clipCache.ContainsKey(clip.name))
                {
                    _clipCache[clip.name] = clip;
                }
            }
        }

        public void PlayMusic(string clipName)
        {
            AudioClip clip = GetClip(clipName);
            if (clip != null)
            {
                if (_musicSource.clip == clip && _musicSource.isPlaying) return;
                _musicSource.clip = clip;
                _musicSource.Play();
            }
        }

        public void PlaySfx(string clipName)
        {
            AudioClip clip = GetClip(clipName);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        private AudioClip GetClip(string clipName)
        {
            if (_clipCache.TryGetValue(clipName, out var clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                _clipCache[clipName] = clip;
                return clip;
            }

            return null;
        }

        public void ToggleSound(bool enabled)
        {
            SoundEnabled = enabled;
            _sfxSource.mute = !enabled;
            PlayerPrefs.SetInt("Antchemy_SoundEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleMusic(bool enabled)
        {
            MusicEnabled = enabled;
            _musicSource.mute = !enabled;
            PlayerPrefs.SetInt("Antchemy_MusicEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
