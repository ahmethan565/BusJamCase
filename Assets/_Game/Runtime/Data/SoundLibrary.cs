using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Data
{
    // SoundId -> clip + volume. Changing a sound needs no code.
    [CreateAssetMenu(menuName = "Game/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public SoundId Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGet(SoundId id, out AudioClip clip, out float volume)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Id != id) continue;
                clip = entry.Clip;
                volume = entry.Volume;
                return clip != null;
            }
            clip = null;
            volume = 0f;
            return false;
        }
    }
}
