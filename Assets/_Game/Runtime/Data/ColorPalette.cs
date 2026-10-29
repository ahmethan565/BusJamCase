using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Runtime.Data
{
    // PassengerColor -> one shared material (passengers and buses) and a UI color (§10.3).
    // One material per color; no MaterialPropertyBlock (keeps SRP Batcher working).
    [CreateAssetMenu(menuName = "Game/Color Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public PassengerColor Color;
            public Material Material;
            public Color UiColor;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public Material GetMaterial(PassengerColor color) => Find(color).Material;
        public Color GetColor(PassengerColor color) => Find(color).UiColor;

        private Entry Find(PassengerColor color)
        {
            foreach (Entry entry in entries)
                if (entry.Color == color) return entry;
            throw new InvalidOperationException($"ColorPalette has no entry for {color}.");
        }
    }
}
