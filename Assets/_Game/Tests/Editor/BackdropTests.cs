using System.Linq;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Tests
{
    // The board and stop backdrops (§9.3, §10.4): they cover their area and touch, so board and seats look joined.
    public sealed class BackdropTests
    {
        private const float BusHalfDepth = 0.5f;   // bus body is 1 deep, parked at BusParkPosition

        private LayoutSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LayoutSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);

        private static LevelCatalog Catalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids.Single()));
        }

        private static bool ContainsXZ(Bounds b, Vector3 p) =>
            p.x >= b.min.x - 1e-4f && p.x <= b.max.x + 1e-4f && p.z >= b.min.z - 1e-4f && p.z <= b.max.z + 1e-4f;

        [Test]
        public void Padding_Default()
        {
            Assert.AreEqual(0.2f, settings.BackdropPadding);
        }

        [Test]
        public void EveryCatalogLevel_BackdropsCoverTouchAndStayOffTheBusLane()
        {
            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                var layout = new BoardLayout(level, settings);
                Bounds board = layout.BoardBackdrop;
                Bounds stop = layout.StopBackdrop;
                string at = $"level {i}";

                // The whole board, cell corners and the outer edge pieces included
                for (int y = 0; y < level.Height; y++)
                    for (int x = 0; x < level.Width; x++)
                    {
                        if (level.Cells[y * level.Width + x] == CellType.Void) continue;
                        Vector3 c = layout.CellToWorld(new GridPos(x, y));
                        Assert.IsTrue(ContainsXZ(board, c + new Vector3(-0.56f, 0f, 0.56f)), $"{at} cell ({x},{y}) top-left");
                        Assert.IsTrue(ContainsXZ(board, c + new Vector3(0.56f, 0f, -0.56f)), $"{at} cell ({x},{y}) bottom-right");
                        Assert.IsTrue(ContainsXZ(board, layout.RoadPointAbove(new GridPos(x, y))), $"{at} road above ({x},{y})");
                    }

                // Every seat marker (0.8 wide)
                for (int s = 0; s < level.WaitingSlots; s++)
                {
                    Vector3 p = layout.SlotPosition(s);
                    Assert.IsTrue(ContainsXZ(stop, p + new Vector3(-0.4f, 0f, -0.4f)), $"{at} slot {s}");
                    Assert.IsTrue(ContainsXZ(stop, p + new Vector3(0.4f, 0f, 0.4f)), $"{at} slot {s}");
                }

                Assert.AreEqual(board.max.z, stop.min.z, 1e-4f, $"{at}: the two backdrops touch, no gap");
                Assert.Less(stop.max.z, layout.BusParkPosition.z - BusHalfDepth, $"{at}: the bus lane stays off the backdrop");

                Bounds content = layout.ContentBounds;
                Assert.IsTrue(ContainsXZ(content, board.min) && ContainsXZ(content, board.max), $"{at}: camera sees the board backdrop");
                Assert.IsTrue(ContainsXZ(content, stop.min) && ContainsXZ(content, stop.max), $"{at}: camera sees the stop backdrop");
            }
        }

        [Test]
        public void BackdropPrefab_IsFlatSimpleLitWithoutShadowsOrColliders()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/Backdrop.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Assert.AreEqual("Universal Render Pipeline/Simple Lit", renderer.sharedMaterial.shader.name);
                Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode);
            }
        }
    }
}
