using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.Feedback
{
    // A fixed pool of AudioSources used round-robin; no source is created at runtime (§13.4).
    public sealed class AudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource[] sources;

        private readonly int[] lastPlayedFrame = NewFrameTable();
        private int next;

        // Returns false if the same sound already played this frame (no pile-up when passengers board together).
        public bool Play(SoundId id, AudioClip clip, float volume, float pitch)
        {
            int frame = Time.frameCount;
            if (lastPlayedFrame[(int)id] == frame) return false;
            lastPlayedFrame[(int)id] = frame;

            AudioSource source = sources[next];
            next = (next + 1) % sources.Length;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            return true;
        }

        private static int[] NewFrameTable()
        {
            var table = new int[System.Enum.GetValues(typeof(SoundId)).Length];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            return table;
        }
    }
}
