using System.IO;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // Which clip the tap and button sounds use (§13.3). Read-only.
    public sealed class InterfaceSoundTests
    {
        [TestCase(SoundId.Click, "Switch.ogg")]
        [TestCase(SoundId.Pop, "Drop.ogg")]
        [TestCase(SoundId.Bonk, "Error.ogg")]
        public void Sound_UsesItsClip(SoundId id, string file)
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundLibrary");
            Assert.AreEqual(1, guids.Length);
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));

            Assert.IsTrue(library.TryGet(id, out AudioClip clip, out _), id.ToString());
            Assert.AreEqual("Assets/_Game/Content/Audio/" + file, AssetDatabase.GetAssetPath(clip));
        }

        [TestCase("Click.wav")]
        [TestCase("Pop.wav")]
        [TestCase("Bonk.wav")]
        public void ReplacedClip_IsRemoved(string file)
        {
            Assert.IsFalse(File.Exists("Assets/_Game/Content/Audio/" + file));
        }
    }
}
