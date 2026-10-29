using System.Collections.Generic;
using Game.Core.Levels;
using Game.Core.Path;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;

namespace Game.Editor.Tests
{
    // §17.3 safety net: the real LevelCatalog asset must only contain valid, solvable levels.
    public sealed class CatalogTests
    {
        private static LevelCatalog LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            Assert.AreEqual(1, guids.Length, "exactly one LevelCatalog asset is expected in the project");
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [Test]
        public void Catalog_HasAtLeastFiveLevels()
        {
            Assert.GreaterOrEqual(LoadCatalog().Count, 5);
        }

        [Test]
        public void Catalog_AllLevels_ParseAndValidate()
        {
            LevelCatalog catalog = LoadCatalog();
            var validator = new LevelValidator(new BfsPathFinder());
            var problems = new List<string>();

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelAsset asset = catalog.GetAsset(i);
                if (asset == null)
                {
                    problems.Add($"#{i}: empty catalog entry");
                    continue;
                }

                ParseResult parsed = asset.Parse();
                if (!parsed.Success)
                {
                    problems.Add($"#{i} {asset.name}: {string.Join("; ", parsed.Errors)}");
                    continue;
                }

                foreach (string error in validator.Validate(parsed.Level))
                    problems.Add($"#{i} {asset.name}: {error}");
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Catalog_AllLevels_Solvable()
        {
            LevelCatalog catalog = LoadCatalog();
            var solver = new LevelSolver(new BfsPathFinder());
            var problems = new List<string>();

            for (int i = 0; i < catalog.Count; i++)
            {
                SolveResult result = solver.Solve(catalog.Get(i));
                if (result.HitLimit)
                    problems.Add($"#{i}: solver hit the state limit ({result.StatesExplored} states)");
                else if (!result.IsSolvable)
                    problems.Add($"#{i}: not solvable");
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
