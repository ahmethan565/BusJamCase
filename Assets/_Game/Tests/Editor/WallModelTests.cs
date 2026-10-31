using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The wall is a very flat dome so it never hides the passengers behind it (§10.2).
    public sealed class WallModelTests
    {
        private const float PassengerHeight = 0.92f;

        private static MeshFilter Model()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/WallBlock.prefab");
            Assert.IsNotNull(prefab);
            Transform model = prefab.transform.Find("Model");
            Assert.IsNotNull(model, "WallBlock -> Model");
            return model.GetComponent<MeshFilter>();
        }

        [Test]
        public void Wall_IsAFlatDome_LowerThanAQuarterOfAPassenger()
        {
            MeshFilter model = Model();
            Mesh mesh = model.sharedMesh;
            Assert.IsFalse(AssetDatabase.GetAssetPath(mesh).StartsWith("Library/"), "not the built-in cube any more");

            Bounds b = mesh.bounds;
            Vector3 scale = model.transform.localScale;
            float height = b.size.y * scale.y;
            float width = b.size.x * scale.x;

            Assert.AreEqual(0f, model.transform.localPosition.y + b.min.y * scale.y, 0.01f, "sits on the floor");
            Assert.Less(height, PassengerHeight / 4f, "flat: it must not hide the passengers");
            Assert.That(width, Is.InRange(0.8f, 1f), "fills most of the cell");
            Assert.AreEqual(width, b.size.z * scale.z, 0.01f, "round");
        }
    }
}
