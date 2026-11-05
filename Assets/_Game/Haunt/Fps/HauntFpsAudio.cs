using System;
using UnityEngine;

namespace Ecos.Haunt.Fps
{
    public enum FpsCue
    {
        Shot, Reload, ReloadEnd, DryFire, PlayerHit, EnemyHit,
        EnemyDeath, Explosion, Dodge, Victory, Step, HostileShot
    }

    /// <summary>Original cached PCM sounds, positional voices and two aligned ambient layers.</summary>
    public sealed class HauntFpsAudio : MonoBehaviour
    {
        const int SampleRate = 22050, VoiceCount = 10, CueCount = 12;
        const float MasterGain = .7f, VoiceGain = .24f, LoopSeconds = 8;
        readonly AudioSource[] voices = new AudioSource[VoiceCount];
        readonly float[] voiceStarted = new float[VoiceCount];
        readonly AudioClip[] clips = new AudioClip[CueCount];
        readonly float[] lastPlayed = new float[CueCount];
        AudioSource ambient, pulse;
        AudioClip ambientClip, pulseClip;
        GameObject audioRoot;
        Transform listener;
        float combatWeight;
        bool initialized, suspended, soundEnabled = true, musicSuspended;

        public bool SoundEnabled => soundEnabled;

        public void Initialize(Transform listener)
        {
            this.listener = listener;
            if (initialized) { RefreshPlayback(); return; }
            audioRoot = new GameObject("FPS audio / original procedural sounds");
            audioRoot.transform.SetParent(transform, false);
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = CreateSource("Voice " + i);
                voiceStarted[i] = float.NegativeInfinity;
            }
            for (int i = 0; i < CueCount; i++)
            {
                lastPlayed[i] = float.NegativeInfinity;
                clips[i] = CreateClip("FPS / " + (FpsCue)i, SynthesizeCue((FpsCue)i));
            }
            ambientClip = CreateClip("FPS / ossuary night", SynthesizeAtmosphere(false));
            pulseClip = CreateClip("FPS / combat heartbeat", SynthesizeAtmosphere(true));
            ambient = CreateSource("Ossuary ambience");
            pulse = CreateSource("Combat pulse");
            ambient.clip = ambientClip; pulse.clip = pulseClip;
            ambient.loop = pulse.loop = true;
            ambient.priority = 200; pulse.priority = 210;
            ambient.volume = MasterGain * .15f; pulse.volume = 0;
            initialized = true; musicSuspended = true;
            RefreshPlayback();
        }

        AudioSource CreateSource(string label)
        {
            // Every positional voice needs its own transform: moving one sound
            // must not move the sources of other sounds already playing.
            var node = new GameObject(label);
            node.transform.SetParent(audioRoot.transform, false);
            var source = node.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0;
            source.volume = 0; source.dopplerLevel = 0;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.5f; source.maxDistance = 28;
            source.priority = 128;
            return source;
        }

        static AudioClip CreateClip(string label, float[] samples)
        {
            var clip = AudioClip.Create(label, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Play(FpsCue cue, float gain = 1, Vector3? position = null)
        {
            int index = (int)cue;
            if (!initialized || suspended || !soundEnabled || index < 0 || index >= CueCount
                || !(gain > 0) || float.IsInfinity(gain)) return;
            Vector3 point = position ?? (listener ? listener.position : transform.position);
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z)
                || float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z)) return;
            float now = Time.unscaledTime;
            float delay = cue == FpsCue.Step ? .1f : cue == FpsCue.DryFire ? .18f : .035f;
            if (now - lastPlayed[index] < delay) return;
            lastPlayed[index] = now;
            int selected = 0;
            for (int i = 0; i < VoiceCount; i++)
            {
                if (!voices[i].isPlaying) { selected = i; break; }
                if (voiceStarted[i] < voiceStarted[selected]) selected = i;
            }
            var voice = voices[selected];
            voice.Stop(); voice.transform.position = point;
            voice.clip = clips[index]; voice.spatialBlend = position.HasValue ? 1 : 0;
            voice.volume = MasterGain * VoiceGain * Mathf.Clamp01(gain) * CueGain(cue);
            voice.Play(); voiceStarted[selected] = now;
        }

        public void Tick(float dt, bool combat)
        {
            if (!initialized) return;
            RefreshPlayback();
            if (suspended || !soundEnabled) return;
            if (!(dt > 0) || float.IsInfinity(dt)) dt = 0;
            combatWeight = Mathf.MoveTowards(combatWeight, combat ? 1 : 0, Mathf.Min(dt, .1f) * 1.4f);
            ambient.volume = MasterGain * .15f;
            pulse.volume = MasterGain * .08f * combatWeight;
        }

        public void SetSuspended(bool value)
        {
            suspended = value;
            RefreshPlayback();
        }

        public void SetEnabled(bool value)
        {
            soundEnabled = value;
            RefreshPlayback();
        }

        void RefreshPlayback()
        {
            if (!initialized) return;
            bool shouldSuspend = suspended || !soundEnabled;
            if (shouldSuspend)
            {
                if (musicSuspended) return;
                // One-shots are discarded on pause/focus loss, so returning to the
                // room cannot replay an old gunshot or impact from the background.
                foreach (var voice in voices) if (voice) voice.Stop();
                ambient.Pause(); pulse.Pause(); musicSuspended = true;
                return;
            }
            if (!musicSuspended) return;
            ambient.UnPause();
            if (!ambient.isPlaying) ambient.Play();
            pulse.UnPause();
            if (!pulse.isPlaying)
            {
                pulse.timeSamples = ambient.timeSamples % pulseClip.samples;
                pulse.Play();
            }
            musicSuspended = false;
        }

        static float CueGain(FpsCue cue)
        {
            switch (cue)
            {
                case FpsCue.Step: return .3f;
                case FpsCue.DryFire: return .4f;
                case FpsCue.Reload: return .65f;
                case FpsCue.ReloadEnd: return .65f;
                case FpsCue.EnemyHit: return .65f;
                case FpsCue.HostileShot: return .75f;
                case FpsCue.Shot: return .9f;
                case FpsCue.Dodge: return .65f;
                default: return 1;
            }
        }

        public void Dispose()
        {
            if (!initialized && !audioRoot) return;
            foreach (var voice in voices) if (voice) voice.Stop();
            if (ambient) ambient.Stop(); if (pulse) pulse.Stop();
            for (int i = 0; i < clips.Length; i++)
            { DestroyOwned(clips[i]); clips[i] = null; }
            DestroyOwned(ambientClip); DestroyOwned(pulseClip);
            DestroyOwned(audioRoot);
            for (int i = 0; i < voices.Length; i++) voices[i] = null;
            audioRoot = null; ambient = pulse = null; ambientClip = pulseClip = null;
            listener = null; combatWeight = 0; initialized = false; musicSuspended = true;
        }

        void OnDestroy() => Dispose();

        static void DestroyOwned(UnityEngine.Object owned)
        {
            if (!owned) return;
            if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
        }

        static float[] SynthesizeCue(FpsCue cue)
        {
            float duration = cue == FpsCue.Explosion ? .66f : cue == FpsCue.EnemyDeath ? .38f
                : cue == FpsCue.Victory ? .72f : cue == FpsCue.Reload ? .3f
                : cue == FpsCue.ReloadEnd ? .16f : cue == FpsCue.PlayerHit ? .25f
                : cue == FpsCue.HostileShot ? .2f : cue == FpsCue.Dodge ? .18f
                : cue == FpsCue.Step ? .09f : cue == FpsCue.DryFire ? .065f : .11f;
            int length = (int)(duration * SampleRate);
            var result = new float[length];
            uint seed = 0xA31F927Bu + (uint)cue * 7919u;
            float noise = 0; double phase = 0;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate, p = i / (float)(length - 1);
                noise += .21f * (NextNoise(ref seed) - noise);
                float frequency, tone, hiss, decay;
                switch (cue)
                {
                    case FpsCue.Shot:
                        frequency = 58 + 225 * (float)Math.Exp(-t * 30); tone = .75f; hiss = .85f; decay = 26; break;
                    case FpsCue.Reload:
                        frequency = p < .46f ? 135 : 255; tone = .4f; hiss = .7f; decay = 8; break;
                    case FpsCue.ReloadEnd:
                        frequency = 215 - p * 95; tone = .55f; hiss = .65f; decay = 23; break;
                    case FpsCue.DryFire:
                        frequency = 125 - p * 60; tone = .25f; hiss = .65f; decay = 46; break;
                    case FpsCue.PlayerHit:
                        frequency = 65 + 140 * (1 - p) * (1 - p); tone = .6f; hiss = .55f; decay = 12; break;
                    case FpsCue.EnemyHit:
                        frequency = 180 - p * 110; tone = .45f; hiss = .8f; decay = 34; break;
                    case FpsCue.EnemyDeath:
                        frequency = 70 + 190 * (1 - p) * (1 - p); tone = .7f; hiss = .45f; decay = 9; break;
                    case FpsCue.Explosion:
                        frequency = 30 + 160 * (float)Math.Exp(-t * 15); tone = .8f; hiss = 1; decay = 7; break;
                    case FpsCue.Dodge:
                        frequency = 220 - p * 150; tone = .08f; hiss = 1; decay = 10; break;
                    case FpsCue.Victory:
                        frequency = p < .25f ? 261.6256f : p < .5f ? 311.127f : p < .75f ? 391.9954f : 523.2511f;
                        tone = .8f; hiss = .02f; decay = 5; break;
                    case FpsCue.Step:
                        frequency = 65 - p * 20; tone = .4f; hiss = .75f; decay = 30; break;
                    default:
                        frequency = 240 + 300 * (1 - p); tone = .65f; hiss = .22f; decay = 14; break;
                }
                phase += Math.PI * 2 * frequency / SampleRate;
                double harmonic = Math.Sin(phase) + .2 * Math.Sin(phase * 2.71);
                float attack = Math.Min(1, t / (cue == FpsCue.Dodge ? .025f : .004f));
                float tail = Math.Min(1, (duration - t) / .025f);
                result[i] = attack * tail * tail * (float)Math.Exp(-t * decay)
                    * ((float)harmonic * tone + noise * hiss);
            }
            Normalize(result, .45f);
            return result;
        }

        static float[] SynthesizeAtmosphere(bool rhythm)
        {
            var result = new float[(int)(SampleRate * LoopSeconds)];
            int[] notes = { 0, 3, 7, 10, 7, 3, 12, 10 };
            for (int i = 0; i < result.Length; i++)
            {
                double t = i / (double)SampleRate, value;
                if (!rhythm)
                {
                    // Quantized frequencies complete whole cycles per loop.
                    double root = Math.Round(65.4064 * LoopSeconds) / LoopSeconds;
                    double fifth = Math.Round(97.9989 * LoopSeconds) / LoopSeconds;
                    value = .13 * Math.Sin(2 * Math.PI * root * t)
                        + .055 * Math.Sin(2 * Math.PI * fifth * t)
                        + .024 * Math.Sin(2 * Math.PI * root * 2 * t);
                    double swell = .76 + .24 * Math.Sin(2 * Math.PI * t / LoopSeconds - Math.PI / 2);
                    value *= swell;
                    for (int note = 0; note < notes.Length; note++)
                    {
                        double age = (t - note + LoopSeconds) % LoopSeconds;
                        double frequency = 130.8128 * Math.Pow(2, notes[note] / 12.0);
                        double envelope = Math.Min(1, age / .018) * Math.Exp(-age * 3.6);
                        value += .075 * envelope * (Math.Sin(2 * Math.PI * frequency * age)
                            + .23 * Math.Sin(2 * Math.PI * frequency * 2.76 * age));
                    }
                }
                else
                {
                    double age = t % .5;
                    double beat = Math.Min(1, age / .009) * Math.Exp(-age * 15);
                    value = .18 * beat * Math.Sin(2 * Math.PI * 52 * age)
                        + .04 * beat * Math.Sin(2 * Math.PI * 104 * age);
                    double chimeAge = (t + .25) % 1;
                    double chime = Math.Min(1, chimeAge / .004) * Math.Exp(-chimeAge * 30);
                    value += .035 * chime * Math.Sin(2 * Math.PI * 1568 * chimeAge);
                }
                result[i] = (float)value;
            }
            Normalize(result, .35f);
            return result;
        }

        static void Normalize(float[] samples, float peakGain)
        {
            double mean = 0;
            for (int i = 0; i < samples.Length; i++) mean += samples[i] / (double)samples.Length;
            float peak = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                // Both ends meet at zero; the short seam is below one frame and
                // prevents abrupt changes from noise, bell tails or the pulse.
                float edge = Math.Min(1, Math.Min(i, samples.Length - 1 - i) / (SampleRate * .006f));
                samples[i] = (samples[i] - (float)mean) * edge;
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            if (peak > 0)
                for (int i = 0; i < samples.Length; i++) samples[i] *= peakGain / peak;
        }

        static float NextNoise(ref uint state)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state & 0xffffu) / 32767.5f - 1;
        }
    }
}
