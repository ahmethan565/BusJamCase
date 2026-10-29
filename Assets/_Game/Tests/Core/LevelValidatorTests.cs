using System.Collections.Generic;
using System.Linq;
using Game.Core.Levels;
using Game.Core.Path;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class LevelValidatorTests
    {
        private readonly LevelValidator validator = new LevelValidator(new BfsPathFinder());

        private IReadOnlyList<string> Validate(string[] rows, string buses, int capacity, int slots = 5, int version = 1)
        {
            ParseResult parsed = LevelParser.Parse(rows, buses, capacity, slots, version);
            Assert.IsTrue(parsed.Success, "test level must parse");
            return validator.Validate(parsed.Level);
        }

        [Test]
        public void Validate_ValidLevel_NoErrors()
        {
            Assert.IsEmpty(Validate(new[] { "RR",
                                            "R." }, "R", capacity: 3));
        }

        [TestCaseSource(typeof(DocumentedLevels), nameof(DocumentedLevels.All))]
        public void Validate_DocumentedLevels_NoErrors(DocumentedLevels.Entry entry)
        {
            CollectionAssert.IsEmpty(validator.Validate(entry.Level));
        }

        [Test]
        public void Validate_ColorCountMismatch_Error()
        {
            // Blue passenger without a blue bus
            Assert.AreEqual(1, Validate(new[] { "RRB" }, "R", capacity: 2).Count);
            // Green bus without green passengers
            Assert.AreEqual(1, Validate(new[] { "RR" }, "RG", capacity: 2).Count);
            // Right colors, wrong amount
            Assert.AreEqual(1, Validate(new[] { "RRR" }, "R", capacity: 2).Count);
        }

        [Test]
        public void Validate_NoExit_Error()
        {
            IReadOnlyList<string> errors = Validate(new[] { "#",
                                                            "R" }, "R", capacity: 1);

            Assert.IsTrue(errors.Any(e => e.Contains("exit cell")), string.Join("\n", errors));
        }

        [Test]
        public void Validate_Island_Error()
        {
            IReadOnlyList<string> errors = Validate(new[] { "R.#",
                                                            "##.",
                                                            "-#B" }, "RB", capacity: 1);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("(2,2)", errors[0]);
        }

        [Test]
        public void Validate_PassengersBlockingEachOther_IsNotAnIsland()
        {
            // Passengers are ignored for reachability: the back passenger can leave once the front one does.
            Assert.IsEmpty(Validate(new[] { "R",
                                            "R" }, "R", capacity: 2));
        }

        [Test]
        public void Validate_NoPassengers_Error()
        {
            // "no passengers" + "red: 0 passengers vs 1 bus"
            Assert.AreEqual(2, Validate(new[] { "." }, "R", capacity: 1).Count);
        }

        [Test]
        public void Validate_ChecksAllRules_DoesNotStopAtFirstError()
        {
            // version, capacity, slots, no bus, red count mismatch
            IReadOnlyList<string> errors = Validate(new[] { "R" }, "", capacity: 0, slots: 0, version: 2);

            Assert.AreEqual(5, errors.Count, string.Join("\n", errors));
        }
    }
}
