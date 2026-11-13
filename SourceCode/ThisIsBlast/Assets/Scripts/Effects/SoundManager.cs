using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Effects
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        private AudioClip _shootClip;
        private AudioClip _hitClip;
        private AudioClip _destroyClip;
        private AudioClip _mergeClip;
        private AudioClip _gemClip;
        private AudioClip _unlockClip;
        private AudioClip _winClip;
        private AudioClip _failClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;

            // Ensure AudioListener is always present
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
                {
                    Camera.main.gameObject.AddComponent<AudioListener>();
                }
                else if (GetComponent<AudioListener>() == null)
                {
                    gameObject.AddComponent<AudioListener>();
                }
            }

            GenerateProceduralAudioClips();
        }

        private void GenerateProceduralAudioClips()
        {
            _shootClip = CreateToneClip("Shoot", 0.08f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(800f, 300f, t) * t) * (1f - t));
            _hitClip = CreateToneClip("Hit", 0.06f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(450f, 150f, t) * t) * (1f - t));
            _destroyClip = CreateNoiseClip("Destroy", 0.15f, 0.7f);
            _mergeClip = CreateChimeClip("Merge", 0.25f);
            _gemClip = CreateToneClip("Gem", 0.12f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1200f, 1800f, t) * t) * (1f - t));
            _unlockClip = CreateToneClip("Unlock", 0.2f, t => Mathf.Sin(2f * Mathf.PI * 650f * t) * (1f - t) + UnityEngine.Random.Range(-0.1f, 0.1f));
            _winClip = CreateFanfareClip("Win");
            _failClip = CreateToneClip("Fail", 0.4f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(400f, 120f, t) * t) * (1f - t));
        }

        private AudioClip CreateToneClip(string name, float duration, Func<float, float> waveFunc)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                samples[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float decay)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 5f / decay);
                samples[i] = UnityEngine.Random.Range(-1f, 1f) * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }; // C, E, G, C (Major Chord)

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float sample = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float noteStartTime = f * 0.05f;
                    if (t >= noteStartTime)
                    {
                        float localT = t - noteStartTime;
                        sample += Mathf.Sin(2f * Mathf.PI * freqs[f] * localT) * Mathf.Exp(-localT * 8f);
                    }
                }
                samples[i] = Mathf.Clamp(sample * 0.4f, -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateFanfareClip(string name)
        {
            float duration = 0.6f;
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.5f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                int noteIndex = Mathf.Min((int)(t * 6f), freqs.Length - 1);
                float f = freqs[noteIndex];
                samples[i] = Mathf.Sin(2f * Mathf.PI * f * t) * (1f - (t * 0.8f)) * 0.5f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayShoot() => _sfxSource.PlayOneShot(_shootClip, 0.6f);
        public void PlayHit() => _sfxSource.PlayOneShot(_hitClip, 0.7f);
        public void PlayDestroy() => _sfxSource.PlayOneShot(_destroyClip, 0.8f);
        public void PlayMerge() => _sfxSource.PlayOneShot(_mergeClip, 0.9f);
        public void PlayGem() => _sfxSource.PlayOneShot(_gemClip, 0.85f);
        public void PlayUnlock() => _sfxSource.PlayOneShot(_unlockClip, 0.9f);
        public void PlayWin() => _sfxSource.PlayOneShot(_winClip, 1f);
        public void PlayFail() => _sfxSource.PlayOneShot(_failClip, 0.9f);
    }
}
