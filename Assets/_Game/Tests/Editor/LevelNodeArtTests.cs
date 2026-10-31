using Game.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Tests
{
    // The level node body is the level1.png art, tinted by the node state.
    public sealed class LevelNodeArtTests
    {
        [Test]
        public void NodeBackground_IsTheLevelArt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/UI/LevelNode.prefab");
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Content/Images/UI/level1.png");
            Assert.IsNotNull(art, "level1.png is a sprite");

            var background = (Image)new SerializedObject(prefab.GetComponent<LevelNodeView>())
                .FindProperty("background").objectReferenceValue;

            Assert.AreSame(art, background.sprite);
            Assert.AreEqual(Image.Type.Simple, background.type);
        }
    }
}
