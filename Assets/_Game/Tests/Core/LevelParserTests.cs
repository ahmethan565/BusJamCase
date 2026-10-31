using Game.Core.Levels;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class LevelParserTests
    {
        private static ParseResult Parse(string[] rows, string buses = "R") =>
            LevelParser.Parse(rows, buses, busCapacity: 3, waitingSlots: 5, version: 1);

        [Test]
        public void Parse_ValidLevel_CopiesAllFields()
        {
            ParseResult result = LevelParser.Parse(new[] { ".#",
                                                           "-R" }, "RG", busCapacity: 2, waitingSlots: 4, version: 1);

            Assert.IsTrue(result.Success);
            Assert.IsEmpty(result.Errors);
            LevelData level = result.Level;
            Assert.AreEqual(1, level.Version);
            Assert.AreEqual(2, level.Width);
            Assert.AreEqual(2, level.Height);
            Assert.AreEqual(2, level.BusCapacity);
            Assert.AreEqual(4, level.WaitingSlots);
            CollectionAssert.AreEqual(new[] { CellType.Floor, CellType.Wall, CellType.Void, CellType.Floor }, level.Cells);
            CollectionAssert.AreEqual(new[] { PassengerColor.Red, PassengerColor.Green }, level.BusColors);
        }

        [Test]
        public void Parse_AllColorLetters()
        {
            ParseResult result = Parse(new[] { "RGBYPO" }, "RGBYPO");

            Assert.IsTrue(result.Success);
            var expected = new[] { PassengerColor.Red, PassengerColor.Green, PassengerColor.Blue,
                                   PassengerColor.Yellow, PassengerColor.Purple, PassengerColor.Orange };
            CollectionAssert.AreEqual(expected, result.Level.BusColors);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], result.Level.Passengers[i].Color);
        }

        [Test]
        public void Parse_UnknownChar_Error()
        {
            ParseResult result = Parse(new[] { "R.",
                                               ".X" });

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Level);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("(1,1)", result.Errors[0]);
        }

        [Test]
        public void Parse_RaggedRows_Error()
        {
            ParseResult result = Parse(new[] { "RR",
                                               "R" });

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Level);
            Assert.IsNotEmpty(result.Errors);
        }

        [Test]
        public void Parse_NoRows_Error()
        {
            Assert.IsFalse(Parse(new string[0]).Success);
            Assert.IsFalse(Parse(null).Success);
            Assert.IsNotEmpty(Parse(null).Errors);
        }

        [Test]
        public void Parse_UnknownBusLetter_Error()
        {
            ParseResult result = Parse(new[] { "R" }, "RX");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [Test]
        public void Parse_BusesIgnoreSpaces()
        {
            ParseResult result = Parse(new[] { "R" }, " R G  B ");

            Assert.IsTrue(result.Success);
            CollectionAssert.AreEqual(new[] { PassengerColor.Red, PassengerColor.Green, PassengerColor.Blue },
                                      result.Level.BusColors);
        }

        [Test]
        public void Parse_EmptyBuses_IsNotAParseError()
        {
            // "At least one bus" is a validation rule, not a parse rule.
            Assert.IsTrue(Parse(new[] { "R" }, "").Success);
            Assert.IsTrue(Parse(new[] { "R" }, null).Success);
        }

        [Test]
        public void Parse_PassengerIds_ReadingOrder()
        {
            ParseResult result = Parse(new[] { "R.G",
                                               "B#Y" });

            Assert.IsTrue(result.Success);
            var passengers = result.Level.Passengers;
            Assert.AreEqual(4, passengers.Count);

            Assert.AreEqual(0, passengers[0].Id);
            Assert.AreEqual(PassengerColor.Red, passengers[0].Color);
            Assert.AreEqual(new GridPos(0, 0), passengers[0].Position);

            Assert.AreEqual(1, passengers[1].Id);
            Assert.AreEqual(PassengerColor.Green, passengers[1].Color);
            Assert.AreEqual(new GridPos(2, 0), passengers[1].Position);

            Assert.AreEqual(2, passengers[2].Id);
            Assert.AreEqual(PassengerColor.Blue, passengers[2].Color);
            Assert.AreEqual(new GridPos(0, 1), passengers[2].Position);

            Assert.AreEqual(3, passengers[3].Id);
            Assert.AreEqual(PassengerColor.Yellow, passengers[3].Color);
            Assert.AreEqual(new GridPos(2, 1), passengers[3].Position);
        }
    }
}
