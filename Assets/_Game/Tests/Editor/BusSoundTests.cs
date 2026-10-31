using System.IO;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The bus stop sound. Read-only.
    public sealed class BusSoundTests
    {
        [Test]
        public void Brake_UsesTheBusStopClip()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundLibrary");
            Assert.AreEqual(1, guids.Length);
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Assert.IsTrue(library.TryGet(SoundId.Brake, out AudioClip clip, out _));
            Assert.AreEqual("Assets/_Game/Content/Audio/BusStop.ogg", AssetDatabase.GetAssetPath(clip));
            Assert.IsFalse(File.Exists("Assets/_Game/Content/Audio/Brake.wav"));
        }
    }
}
