using UnityEngine;
using UnityEngine.Audio;

namespace Ecos
{
    public enum AudioCategory
    {
        Music,
        Ambient,
        Footsteps,
        Interaction,
        Weather,
        Magic,
        UI
    }

    public sealed class AmbientAudio : MonoBehaviour
    {
        public AudioMixer mixer;
        public AudioClip[] clips = new AudioClip[7];
        AudioSource[] sources;
        void Awake()
        {
            sources = new AudioSource[7];
            for (int i = 0; i < 7; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = i < 2 || i == 4;
                s.spatialBlend = i == 2 || i == 3 || i == 5 ? 1 : 0;
                sources[i] = s;
            }
        }

        public void Play(AudioCategory c)
        {
            int i = (int)c;
            if (clips[i] == null)
                return;
            var source = sources[i];
            if (mixer)
            {
                var groups = mixer.FindMatchingGroups(c.ToString());
                if (groups.Length > 0)
                    source.outputAudioMixerGroup = groups[0];
            }

            source.clip = clips[i];
            source.Play();
        }
    }
}
