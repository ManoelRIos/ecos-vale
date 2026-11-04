using System;
using UnityEngine;

namespace Ecos.Haunt
{
    enum SoundCue
    {
        Shot, MagicShot, EnemyHit, PlayerHit, Dodge, Wave, Purchase, Door, BossDeath, PlayerDeath, Impact,
        Explosion, Splash, RelicPickup, BossIntro
    }

    public sealed partial class HauntGame
    {
        const int AudioSampleRate = 22050, AudioVoiceCount = 8, AudioCueCount = 15, MusicThemeCount = 6;
        const int BossMusicTheme = 5, DialogueVoiceCount = 4, DialoguePitchCount = 3;
        const float AudioMasterGain = .35f, AudioVoiceGain = .45f, AudioRetriggerDelay = .04f;
        const float DialogueRetriggerDelay = .035f;
        readonly AudioSource[] audioVoices = new AudioSource[AudioVoiceCount];
        readonly float[] audioVoiceStarted = new float[AudioVoiceCount];
        readonly float[] audioCueLastPlayed = new float[AudioCueCount];
        readonly AudioClip[] audioClips = new AudioClip[AudioCueCount];
        readonly AudioClip[] dialogueClips = new AudioClip[DialogueVoiceCount * DialoguePitchCount];
        float dialogueLastPlayed = float.NegativeInfinity;
        AudioSource audioRay;
        AudioClip audioRayCoilClip, audioRayLanternClip;
        readonly AudioClip[] musicBedClips = new AudioClip[MusicThemeCount];
        readonly AudioClip[] musicRhythmClips = new AudioClip[MusicThemeCount];
        readonly AudioSource[] musicBeds = new AudioSource[2], musicRhythms = new AudioSource[2];
        readonly float[] musicWeights = new float[2];
        int musicActiveSlot, musicTheme = -1;
        float musicIntensity, musicGain;
        GameObject audioRoot;
        bool soundEnabled = true, audioInitialized, audioPaused, musicInitialized;
        bool audioFocused = true, audioApplicationPaused, audioSourcesSuspended;
        bool AudioSuspended => audioPaused || Paused || !audioFocused || audioApplicationPaused || !soundEnabled;

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
            if (AudioSuspended || gain <= 0) return;
            int cueIndex = (int)cue;
            if (cueIndex < 0 || cueIndex >= AudioCueCount) return;
            InitAudio();
            float now = Time.unscaledTime;
            if (now - audioCueLastPlayed[cueIndex] < AudioRetriggerDelay) return;
            audioCueLastPlayed[cueIndex] = now;
            PlayAudioVoice(audioClips[cueIndex], Mathf.Clamp01(gain) * CueVolume(cue), now);
        }

        void PlayAudioVoice(AudioClip clip, float gain, float now)
        {
            int voiceIndex = 0;
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (!audioVoices[i].isPlaying) { voiceIndex = i; break; }
                if (audioVoiceStarted[i] < audioVoiceStarted[voiceIndex]) voiceIndex = i;
            }
            AudioSource voice = audioVoices[voiceIndex];
            voice.Stop();
            voice.clip = clip;
            // Eight simultaneous SFX, the ray and both music layers retain mix headroom.
            voice.volume = AudioMasterGain * AudioVoiceGain * Mathf.Clamp01(gain);
            voice.Play();
            audioVoiceStarted[voiceIndex] = now;
        }

        void PlayBossIntroImpact() => PlaySound(SoundCue.BossIntro);

        void PlayDialogueBlip(char letter, int voiceVariant)
        {
            if (AudioSuspended || !char.IsLetterOrDigit(letter)) return;
            float now = Time.unscaledTime;
            if (now - dialogueLastPlayed < DialogueRetriggerDelay) return;
            InitAudio();
            int voice = ((voiceVariant % DialogueVoiceCount) + DialogueVoiceCount) % DialogueVoiceCount;
            int pitch = char.ToUpperInvariant(letter) % DialoguePitchCount;
            int index = voice * DialoguePitchCount + pitch;
            if (dialogueClips[index] == null)
                dialogueClips[index] = CreateAudioClip("Voz " + voice + " • " + pitch, SynthesizeDialogueBlip(voice, pitch));
            dialogueLastPlayed = now;
            PlayAudioVoice(dialogueClips[index], .32f, now);
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
                case SoundCue.Explosion: return .85f;
                case SoundCue.Splash: return .65f;
                case SoundCue.RelicPickup: return .8f;
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
            if (AudioSuspended) return;
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
            if (!enabled)
            {
                for (int i = 0; i < AudioVoiceCount; i++)
                    if (audioVoices[i] != null) audioVoices[i].Stop();
                if (audioRay != null) audioRay.Stop();
            }
            RefreshAudioSuspension();
        }

        void PauseAudio(bool paused)
        {
            audioPaused = paused;
            RefreshAudioSuspension();
        }

        void OnApplicationFocus(bool focused)
        {
            audioFocused = focused;
            RefreshAudioSuspension();
        }

        void OnApplicationPause(bool paused)
        {
            audioApplicationPaused = paused;
            RefreshAudioSuspension();
        }

        void RefreshAudioSuspension()
        {
            bool suspended = AudioSuspended;
            if (!audioInitialized || suspended == audioSourcesSuspended) return;
            audioSourcesSuspended = suspended;
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (suspended) audioVoices[i].Pause();
                else if (soundEnabled) audioVoices[i].UnPause();
            }
            if (suspended) audioRay.Pause();
            else if (soundEnabled) audioRay.UnPause();
            if (!musicInitialized) return;
            for (int i = 0; i < 2; i++)
            {
                if (suspended) { musicBeds[i].Pause(); musicRhythms[i].Pause(); }
                else if (soundEnabled) { musicBeds[i].UnPause(); musicRhythms[i].UnPause(); }
            }
        }

        // Call once per frame, including the menu. Clips are generated only on first
        // visiting a theme; transitions reuse two pairs of aligned looping sources.
        void UpdateMusic(float dt)
        {
            RefreshAudioSuspension();
            if (AudioSuspended) return;
            if (float.IsNaN(dt) || float.IsInfinity(dt)) dt = 0;
            dt = Mathf.Clamp(dt, 0, .1f);
            InitAudio();
            if (!musicInitialized)
            {
                for (int i = 0; i < 2; i++)
                {
                    musicBeds[i] = CreateAudioSource();
                    musicRhythms[i] = CreateAudioSource();
                    musicBeds[i].loop = musicRhythms[i].loop = true;
                    musicBeds[i].priority = musicRhythms[i].priority = 192;
                }
                musicInitialized = true;
            }
            bool alive = State == null || !State.IsDead;
            bool combat = Playing && !InShop && alive && CurrentRoom != null && !CurrentRoom.Cleared;
            bool boss = combat && CurrentRoom.Kind == RoomKind.Boss;
            bool intro = boss && BossIntroActive;
            // The first tenth of the three-second splash belongs to the impact.
            // Fade the room out first; the dedicated fight theme enters at ~0.3s.
            bool introImpactOnly = intro && BossIntroProgress < .1f;
            bool fadingLostBattle = Playing && !alive && musicTheme == BossMusicTheme;
            int ambient = !Playing || InShop || Floor == null ? 0 : Math.Max(0, Math.Min(4, ThemeVariant));
            int theme = (boss && !introImpactOnly) || fadingLostBattle ? BossMusicTheme : ambient;
            if (theme != musicTheme)
            {
                int next = musicTheme < 0 ? 0 : 1 - musicActiveSlot;
                bool bossTransition = theme == BossMusicTheme || musicTheme == BossMusicTheme;
                int phase = musicTheme < 0 || bossTransition ? 0 : musicBeds[musicActiveSlot].timeSamples;
                if (musicBedClips[theme] == null)
                {
                    string label = theme == BossMusicTheme ? "Confronto • guardião" : "Tema " + theme;
                    musicBedClips[theme] = CreateAudioClip(label + " • harmonia", SynthesizeMusic(theme, false));
                    musicRhythmClips[theme] = CreateAudioClip(label + " • ritmo", SynthesizeMusic(theme, true));
                }
                musicBeds[next].Stop(); musicRhythms[next].Stop();
                musicBeds[next].volume = musicRhythms[next].volume = 0;
                musicBeds[next].clip = musicBedClips[theme];
                musicRhythms[next].clip = musicRhythmClips[theme];
                musicBeds[next].timeSamples = musicRhythms[next].timeSamples = phase % musicBedClips[theme].samples;
                musicWeights[next] = 0;
                musicBeds[next].Play(); musicRhythms[next].Play();
                musicActiveSlot = next;
                musicTheme = theme;
            }
            float targetIntensity = boss ? 1 : combat ? .68f : InShop ? .08f : .2f;
            float targetGain = fadingLostBattle ? 0 : !alive ? .35f : !Playing ? .55f : InShop ? .7f : 1;
            musicIntensity = Mathf.MoveTowards(musicIntensity, targetIntensity, dt * 1.1f);
            musicGain = Mathf.MoveTowards(musicGain, targetGain, dt * .8f);
            for (int i = 0; i < 2; i++)
            {
                float target = i == musicActiveSlot && !introImpactOnly ? 1 : 0;
                float fadeDuration = introImpactOnly ? .18f : boss ? .45f : 1.2f;
                musicWeights[i] = Mathf.MoveTowards(musicWeights[i], target, dt / fadeDuration);
                musicBeds[i].volume = AudioMasterGain * .24f * musicWeights[i] * musicGain;
                musicRhythms[i].volume = AudioMasterGain * .20f * musicWeights[i] * musicGain * musicIntensity;
                if (i != musicActiveSlot && musicWeights[i] <= 0)
                {
                    musicBeds[i].Stop(); musicRhythms[i].Stop();
                }
            }
        }

        void DisposeAudio()
        {
            for (int i = 0; i < AudioVoiceCount; i++)
            {
                if (audioVoices[i] != null) audioVoices[i].Stop();
                audioVoices[i] = null;
            }
            if (audioRay != null) audioRay.Stop();
            for (int i = 0; i < 2; i++)
            {
                if (musicBeds[i] != null) musicBeds[i].Stop();
                if (musicRhythms[i] != null) musicRhythms[i].Stop();
                musicBeds[i] = musicRhythms[i] = null;
                musicWeights[i] = 0;
            }
            for (int i = 0; i < MusicThemeCount; i++)
            {
                DestroyAudioObject(musicBedClips[i]);
                DestroyAudioObject(musicRhythmClips[i]);
                musicBedClips[i] = musicRhythmClips[i] = null;
            }
            for (int i = 0; i < AudioCueCount; i++)
            {
                DestroyAudioObject(audioClips[i]);
                audioClips[i] = null;
            }
            for (int i = 0; i < dialogueClips.Length; i++)
            {
                DestroyAudioObject(dialogueClips[i]);
                dialogueClips[i] = null;
            }
            DestroyAudioObject(audioRayCoilClip);
            DestroyAudioObject(audioRayLanternClip);
            DestroyAudioObject(audioRoot);
            audioRayCoilClip = audioRayLanternClip = null;
            audioRay = null;
            audioRoot = null;
            audioInitialized = false;
            audioPaused = false;
            audioSourcesSuspended = musicInitialized = false;
            musicTheme = -1;
            musicIntensity = musicGain = 0;
            dialogueLastPlayed = float.NegativeInfinity;
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
                case SoundCue.Explosion: duration = .48f; break;
                case SoundCue.Splash: duration = .32f; break;
                case SoundCue.RelicPickup: duration = .48f; break;
                case SoundCue.BossIntro: duration = .92f; break;
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
                    case SoundCue.Explosion:
                        frequency = 38 + 110 * (1 - progress) * (1 - progress);
                        toneWeight = .7f; noiseWeight = .9f; decay = 10; break;
                    case SoundCue.Splash:
                        frequency = 430 - 330 * progress + 90 * (float)Math.Sin(t * 67);
                        toneWeight = .3f; noiseWeight = .75f; decay = 10; break;
                    case SoundCue.RelicPickup:
                        frequency = progress < .25f ? 440 : progress < .5f ? 554.37f : progress < .75f ? 659.25f : 880;
                        toneWeight = .65f; noiseWeight = .01f; decay = 5; break;
                    case SoundCue.BossIntro:
                        frequency = 27 + 150 * (float)Math.Exp(-t * 11);
                        toneWeight = .8f; noiseWeight = .65f; decay = 5; break;
                    default:
                        frequency = 220 - 175 * progress;
                        toneWeight = .75f; noiseWeight = .12f; decay = 5; break;
                }
                phase += Math.PI * 2 * frequency / AudioSampleRate;
                float tone = (float)(Math.Sin(phase) + .24 * Math.Sin(phase * 2));
                if (cue == SoundCue.MagicShot || cue == SoundCue.Purchase || cue == SoundCue.RelicPickup)
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

        static float[] SynthesizeDialogueBlip(int voice, int pitch)
        {
            voice = Math.Max(0, Math.Min(DialogueVoiceCount - 1, voice));
            pitch = Math.Max(0, Math.Min(DialoguePitchCount - 1, pitch));
            double duration = .036 + voice * .004;
            int count = (int)Math.Round(AudioSampleRate * duration);
            var samples = new float[count];
            double[] voices = { 185, 260, 120, 310 };
            double frequency = voices[voice] * Math.Pow(2, (pitch - 1) / 12.0);
            float peak = 0;
            for (int i = 0; i < count; i++)
            {
                double t = i / (double)AudioSampleRate;
                double progress = i / (double)(count - 1);
                double phase = Math.PI * 2 * frequency * (t + .8 * t * t);
                double body = Math.Sin(phase) + .22 * Math.Sin(phase * 2) + .1 * Math.Sin(phase * 3);
                double envelope = Math.Min(1, t / .004) * Math.Min(1, (duration - t) / .012)
                    * Math.Sin(Math.PI * progress);
                samples[i] = i == count - 1 ? 0 : (float)(body * envelope);
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < count; i++) samples[i] *= .35f / peak;
            return samples;
        }

        // Original sixteen-beat motifs at 112 BPM. Notes and percussion share the
        // exact sample count, so their phases stay aligned through theme changes.
        // Each note releases before the loop boundary; no sample is concatenated
        // with a nonzero edge. No file, download or realtime DSP callback is used.
        static float[] SynthesizeMusic(int theme, bool rhythm)
        {
            if (theme == BossMusicTheme) return SynthesizeBossMusic(rhythm);
            const double beatSeconds = 60.0 / 112, beatCount = 16;
            int count = (int)Math.Round(AudioSampleRate * beatSeconds * beatCount);
            var samples = new float[count];
            int[] roots = { 45, 50, 43, 46, 40 };
            int[][] progressions =
            {
                new[] { 0, 5, 3, 7 }, new[] { 0, 3, -2, 5 },
                new[] { 0, 7, 5, 3 }, new[] { 0, 5, -2, 3 }, new[] { 0, 3, 7, -2 }
            };
            int[] melody = { 0, 7, 10, 7, 3, 7, 12, 10, 0, 7, 3, 10, 7, 5, 3, 7 };
            theme = Math.Max(0, Math.Min(roots.Length - 1, theme));
            uint noise = 0x0EC05A11u + (uint)theme * 7919u;
            float lowNoise = 0;
            for (int i = 0; i < count; i++)
            {
                double beats = beatCount * i / count;
                int beat = (int)beats, bar = beat / 4;
                double local = (beats - beat) * beatSeconds;
                double eighth = (beats * 2 - Math.Floor(beats * 2)) * beatSeconds * .5;
                int root = roots[theme] + progressions[theme][bar];
                double value;
                if (rhythm)
                {
                    float rawNoise = NextAudioNoise(ref noise);
                    lowNoise += .18f * (rawNoise - lowNoise);
                    double kick = 0, snare = 0;
                    if ((beat & 1) == 0)
                    {
                        double phase = Math.PI * 2 * (44 * local + 2.6 * (1 - Math.Exp(-local / .04)));
                        kick = .42 * Math.Sin(phase) * MusicEnvelope(local, .005, .34, 13);
                    }
                    else
                        snare = (.21 * lowNoise + .045 * Math.Sin(Math.PI * 2 * 160 * local))
                            * MusicEnvelope(local, .003, .24, 19);
                    double hat = .035 * (rawNoise - lowNoise) * MusicEnvelope(eighth, .0015, .11, 43);
                    double bass = .08 * Math.Sin(Math.PI * 2 * MusicFrequency(root - 12) * local)
                        * MusicEnvelope(local, .01, .42, 5);
                    value = kick + snare + hat + bass;
                }
                else
                {
                    double barTime = (beats - bar * 4) * beatSeconds;
                    double barProgress = (beats - bar * 4) / 4;
                    double padEnvelope = Math.Pow(Math.Sin(Math.PI * barProgress), 2);
                    double pad = .045 * padEnvelope * (
                        Math.Sin(Math.PI * 2 * MusicFrequency(root + 12) * barTime)
                        + .7 * Math.Sin(Math.PI * 2 * MusicFrequency(root + 15) * barTime)
                        + .5 * Math.Sin(Math.PI * 2 * MusicFrequency(root + 19) * barTime));
                    int note = root + 12 + melody[(beat + theme * 3) % melody.Length];
                    double leadPhase = Math.PI * 2 * MusicFrequency(note) * local;
                    double lead = (.13 * Math.Sin(leadPhase) + .025 * Math.Sin(leadPhase * 2))
                        * MusicEnvelope(local, .012, .45, 5.5);
                    double bass = .13 * Math.Sin(Math.PI * 2 * MusicFrequency(root - 12) * local)
                        * MusicEnvelope(local, .015, .46, 4);
                    value = pad + lead + bass;
                }
                samples[i] = (float)value;
            }
            // A tiny release also bounds the join if a later motif adds sustained notes.
            float peak = 0;
            for (int i = 0; i < count; i++)
            {
                float edge = Math.Min(1, Math.Min(i, count - 1 - i) / (AudioSampleRate * .008f));
                samples[i] *= edge;
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < count; i++) samples[i] *= .5f / peak;
            return samples;
        }

        // This is a separate composition at 144 BPM, not an accelerated room song.
        // Its chromatic eighth-note motif, pulsing bass and syncopated percussion
        // leave the musical identity of the five exploration themes behind.
        static float[] SynthesizeBossMusic(bool rhythm)
        {
            const double beatSeconds = 60.0 / 144, beatCount = 16;
            int count = (int)Math.Round(AudioSampleRate * beatSeconds * beatCount);
            var samples = new float[count];
            int[] roots = { 40, 39, 43, 38 };
            int[] riff = { 0, 7, 1, 7, 3, 10, 6, 7, 12, 7, 10, 6, 7, 3, 1, 7,
                0, 12, 7, 13, 10, 7, 6, 3, 12, 10, 7, 6, 3, 1, 0, 7 };
            uint noise = 0xB055144u;
            float lowNoise = 0, peak = 0;
            for (int i = 0; i < count; i++)
            {
                double beats = beatCount * i / count;
                int beat = (int)beats, step = (int)(beats * 2), bar = beat / 4;
                double localBeat = (beats - beat) * beatSeconds;
                double localEighth = (beats * 2 - step) * beatSeconds * .5;
                double localSixteenth = (beats * 4 - Math.Floor(beats * 4)) * beatSeconds * .25;
                int root = roots[bar];
                double value;
                if (rhythm)
                {
                    float rawNoise = NextAudioNoise(ref noise);
                    lowNoise += .22f * (rawNoise - lowNoise);
                    double kickPhase = Math.PI * 2 * (43 * localEighth + 2.9 * (1 - Math.Exp(-localEighth / .035)));
                    bool kickStep = (step & 1) == 0 || step % 8 == 5;
                    double kick = kickStep ? .4 * Math.Sin(kickPhase) * MusicEnvelope(localEighth, .003, .18, 18) : 0;
                    double snare = (beat & 1) != 0 ? (.23 * lowNoise + .035 * Math.Sin(Math.PI * 2 * 180 * localBeat))
                        * MusicEnvelope(localBeat, .003, .19, 17) : 0;
                    double hat = .033 * (rawNoise - lowNoise) * MusicEnvelope(localSixteenth, .0015, .07, 47);
                    double lowPulse = .065 * Math.Sin(Math.PI * 2 * MusicFrequency(root - 12) * localEighth)
                        * MusicEnvelope(localEighth, .008, .19, 9);
                    value = kick + snare + hat + lowPulse;
                }
                else
                {
                    double notePhase = Math.PI * 2 * MusicFrequency(root + 12 + riff[step]) * localEighth;
                    double lead = (.13 * Math.Sin(notePhase) + .035 * Math.Sin(notePhase * 2) + .012 * Math.Sin(notePhase * 3))
                        * MusicEnvelope(localEighth, .006, .19, 7);
                    int bassNote = root - 12 + (step % 4 == 3 ? 7 : 0);
                    double bassPhase = Math.PI * 2 * MusicFrequency(bassNote) * localEighth;
                    double bass = (.16 * Math.Sin(bassPhase) + .025 * Math.Sin(bassPhase * 3))
                        * MusicEnvelope(localEighth, .009, .2, 7);
                    double barTime = (beats - bar * 4) * beatSeconds;
                    double barEnvelope = Math.Pow(Math.Sin(Math.PI * (beats - bar * 4) / 4), 2);
                    double pad = .03 * barEnvelope * (Math.Sin(Math.PI * 2 * MusicFrequency(root + 12) * barTime)
                        + .6 * Math.Sin(Math.PI * 2 * MusicFrequency(root + 15) * barTime)
                        + .35 * Math.Sin(Math.PI * 2 * MusicFrequency(root + 18) * barTime));
                    value = lead + bass + pad;
                }
                float edge = Math.Min(1, Math.Min(i, count - 1 - i) / (AudioSampleRate * .008f));
                samples[i] = (float)value * edge;
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < count; i++) samples[i] *= .5f / peak;
            return samples;
        }

        static double MusicEnvelope(double time, double attack, double release, double decay)
        {
            if (time < 0 || time >= release) return 0;
            return Math.Min(1, time / attack) * Math.Min(1, (release - time) / .035) * Math.Exp(-time * decay);
        }

        static double MusicFrequency(int midi) => 440 * Math.Pow(2, (midi - 69) / 12.0);
    }
}
