using System;
using UnityEngine;

namespace Ecos.Haunt
{
    enum SoundCue
    {
        Shot, MagicShot, EnemyHit, PlayerHit, Dodge, Wave, Purchase, Door, BossDeath, PlayerDeath, Impact
    }

    public sealed partial class HauntGame
    {
        const int AudioSampleRate = 22050, AudioVoiceCount = 8, AudioCueCount = 11;
        const float AudioMasterGain = .35f, AudioVoiceGain = .45f, AudioRetriggerDelay = .04f;
        readonly AudioSource[] audioVoices = new AudioSource[AudioVoiceCount];
        readonly float[] audioVoiceStarted = new float[AudioVoiceCount];
        readonly float[] audioCueLastPlayed = new float[AudioCueCount];
        readonly AudioClip[] audioClips = new AudioClip[AudioCueCount];
        AudioSource audioRay;
        AudioClip audioRayCoilClip, audioRayLanternClip;
        GameObject audioRoot;
        bool soundEnabled = true, audioInitialized, audioPaused;

        void InitAudio()
        {
            if (audioInitialized) return;
            audioRoot = new GameObject("Sons • Ecos do Vale");
            audioRoot.transform.SetParent(transform, false);
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                audioVoices[i] = CreateAudioSource();
                audioVoiceStarted[i] = float.NegativeInfinity;
            }
            for (int i = 0; i < AudioCueCount; i++)
            {
                audioCueLastPlayed[i] = float.NegativeInfinity;
                float[] samples = SynthesizeSound((SoundCue)i);
                audioClips[i] = CreateAudioClip(((SoundCue)i).ToString(), samples);
            }
            audioRay = CreateAudioSource();
            audioRay.loop = true;
            audioRay.volume = AudioMasterGain * .1f;
            audioRayCoilClip = CreateAudioClip("Raio • bobina", SynthesizeRay(false));
            audioRayLanternClip = CreateAudioClip("Raio • lanterna", SynthesizeRay(true));
            audioInitialized = true;
        }

        AudioSource CreateAudioSource()
        {
            var source = audioRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = 0;
            source.dopplerLevel = 0;
            source.priority = 128;
            return source;
        }

        static AudioClip CreateAudioClip(string label, float[] samples)
        {
            var clip = AudioClip.Create(label, samples.Length, 1, AudioSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void PlaySound(SoundCue cue, float gain = 1)
        {
            if (!soundEnabled || audioPaused || gain <= 0) return;
            int cueIndex = (int)cue;
            if (cueIndex < 0 || cueIndex >= AudioCueCount) return;
            InitAudio();
            float now = Time.unscaledTime;
            if (now - audioCueLastPlayed[cueIndex] < AudioRetriggerDelay) return;
            audioCueLastPlayed[cueIndex] = now;
            int voiceIndex = 0;
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (!audioVoices[i].isPlaying) { voiceIndex = i; break; }
                if (audioVoiceStarted[i] < audioVoiceStarted[voiceIndex]) voiceIndex = i;
            }
            AudioSource voice = audioVoices[voiceIndex];
            voice.Stop();
            voice.clip = audioClips[cueIndex];
            // Even eight simultaneous peaks leave ample room for the quiet beam loop.
            voice.volume = AudioMasterGain * AudioVoiceGain * Mathf.Clamp01(gain) * CueVolume(cue);
            voice.Play();
            audioVoiceStarted[voiceIndex] = now;
        }

        static float CueVolume(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Shot: return .8f;
                case SoundCue.MagicShot: return .85f;
                case SoundCue.EnemyHit: return .6f;
                case SoundCue.Impact: return .45f;
                case SoundCue.Dodge: return .7f;
                case SoundCue.Door: return .75f;
                default: return 1;
            }
        }

        void UpdateRaySound(bool active, bool lantern = false)
        {
            if (!active || !soundEnabled)
            {
                if (audioRay != null) audioRay.Stop();
                return;
            }
            if (audioPaused) return;
            InitAudio();
            AudioClip clip = lantern ? audioRayLanternClip : audioRayCoilClip;
            if (audioRay.clip != clip)
            {
                int phase = audioRay.clip == null ? 0 : audioRay.timeSamples;
                audioRay.Stop();
                audioRay.clip = clip;
                audioRay.timeSamples = phase % clip.samples;
            }
            if (!audioRay.isPlaying) audioRay.Play();
        }

        void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
            if (enabled) return;
            for (int i = 0; i < AudioVoiceCount; i++)
                if (audioVoices[i] != null) audioVoices[i].Stop();
            if (audioRay != null) audioRay.Stop();
        }

        void PauseAudio(bool paused)
        {
            if (audioPaused == paused) return;
            audioPaused = paused;
            if (!audioInitialized) return;
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (paused) audioVoices[i].Pause();
                else if (soundEnabled) audioVoices[i].UnPause();
            }
            if (paused) audioRay.Pause();
            else if (soundEnabled) audioRay.UnPause();
        }

        void DisposeAudio()
        {
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (audioVoices[i] != null) audioVoices[i].Stop();
                audioVoices[i] = null;
            }
            if (audioRay != null) audioRay.Stop();
            for (int i = 0; i < AudioCueCount; i++)
            {
                DestroyAudioObject(audioClips[i]);
                audioClips[i] = null;
            }
            DestroyAudioObject(audioRayCoilClip);
            DestroyAudioObject(audioRayLanternClip);
            DestroyAudioObject(audioRoot);
            audioRayCoilClip = audioRayLanternClip = null;
            audioRay = null;
            audioRoot = null;
            audioInitialized = false;
            audioPaused = false;
        }

        static void DestroyAudioObject(UnityEngine.Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(owned);
            else UnityEngine.Object.DestroyImmediate(owned);
        }

        // Short original mono sounds: low-passed noise provides texture without a sharp click.
        static float[] SynthesizeSound(SoundCue cue)
        {
            float duration;
            switch (cue)
            {
                case SoundCue.Shot: duration = .10f; break;
                case SoundCue.MagicShot: duration = .16f; break;
                case SoundCue.EnemyHit: duration = .08f; break;
                case SoundCue.PlayerHit: duration = .23f; break;
                case SoundCue.Dodge: duration = .18f; break;
                case SoundCue.Wave: duration = .42f; break;
                case SoundCue.Purchase: duration = .36f; break;
                case SoundCue.Door: duration = .28f; break;
                case SoundCue.BossDeath: duration = .72f; break;
                case SoundCue.Impact: duration = .065f; break;
                default: duration = .65f; break;
            }
            int count = (int)(duration * AudioSampleRate);
            var samples = new float[count];
            uint noiseState = 0x5EED1234u + (uint)cue * 7919u;
            double phase = 0;
            float filteredNoise = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)AudioSampleRate;
                float progress = i / (float)(count - 1);
                filteredNoise += .22f * (NextAudioNoise(ref noiseState) - filteredNoise);
                float frequency, toneWeight, noiseWeight, decay;
                switch (cue)
                {
                    case SoundCue.Shot:
                        frequency = 185f * (float)Math.Exp(-t * 20) + 65;
                        toneWeight = .55f; noiseWeight = .55f; decay = 32; break;
                    case SoundCue.MagicShot:
                        frequency = 620 - 300 * progress;
                        toneWeight = .7f; noiseWeight = .16f; decay = 17; break;
                    case SoundCue.EnemyHit:
                        frequency = 115 + 65 * (1 - progress);
                        toneWeight = .35f; noiseWeight = .8f; decay = 37; break;
                    case SoundCue.Impact:
                        frequency = 145 - 70 * progress;
                        toneWeight = .45f; noiseWeight = .5f; decay = 43; break;
                    case SoundCue.PlayerHit:
                        frequency = 75 + 85 * (1 - progress) * (1 - progress);
                        toneWeight = .65f; noiseWeight = .45f; decay = 16; break;
                    case SoundCue.Dodge:
                        frequency = 340 - 230 * progress;
                        toneWeight = .1f; noiseWeight = .9f; decay = 12; break;
                    case SoundCue.Wave:
                        frequency = 90 + 65 * progress;
                        toneWeight = .65f; noiseWeight = .25f; decay = 6; break;
                    case SoundCue.Purchase:
                        frequency = progress < .38f ? 523.25f : 783.99f;
                        toneWeight = .7f; noiseWeight = .015f; decay = 7; break;
                    case SoundCue.Door:
                        frequency = 92 + 16 * (float)Math.Sin(t * 41);
                        toneWeight = .45f; noiseWeight = .6f; decay = 12; break;
                    case SoundCue.BossDeath:
                        frequency = 42 + 130 * (1 - progress) * (1 - progress);
                        toneWeight = .65f; noiseWeight = .55f; decay = 5; break;
                    default:
                        frequency = 220 - 175 * progress;
                        toneWeight = .75f; noiseWeight = .12f; decay = 5; break;
                }
                phase += Math.PI * 2 * frequency / AudioSampleRate;
                float tone = (float)(Math.Sin(phase) + .24 * Math.Sin(phase * 2));
                if (cue == SoundCue.MagicShot || cue == SoundCue.Purchase)
                    tone += .18f * (float)Math.Sin(phase * 3);
                float attack = Math.Min(1, t / .006f);
                if (cue == SoundCue.Dodge || cue == SoundCue.Wave) attack = Math.Min(1, t / .035f);
                float tail = Math.Min(1, (duration - t) / .035f);
                float envelope = attack * tail * tail * (float)Math.Exp(-t * decay);
                samples[i] = envelope * (tone * toneWeight + filteredNoise * noiseWeight);
            }
            // Smooth both boundaries and remove DC before peak normalization.
            float mean = 0;
            for (int i = 0; i < count; i++) mean += samples[i] / count;
            float peak = 0;
            for (int i = 0; i < count; i++)
            {
                float edge = Math.Min(1, Math.Min(i, count - 1 - i) / (AudioSampleRate * .004f));
                samples[i] = (samples[i] - mean) * edge;
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < count; i++) samples[i] *= .5f / peak;
            return samples;
        }

        static float NextAudioNoise(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xffffu) / 32767.5f - 1;
        }

        static float[] SynthesizeRay(bool lantern)
        {
            // Integer cycles over half a second make the loop seamless at its join.
            var samples = new float[AudioSampleRate / 2];
            double frequency = lantern ? 96 : 120;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)AudioSampleRate;
                double phase = Math.PI * 2 * frequency * t;
                double breath = .88 + .12 * Math.Cos(Math.PI * 2 * 4 * t);
                samples[i] = (float)(breath * (.18 * Math.Sin(phase) + .055 * Math.Sin(phase * 2)
                    + .025 * Math.Sin(phase * 4)));
            }
            return samples;
        }
    }
}
