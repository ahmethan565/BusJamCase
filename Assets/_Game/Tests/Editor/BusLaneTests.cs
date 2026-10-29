using System.Collections.Generic;
using Game.Runtime.Animation;
using NUnit.Framework;

namespace Game.Editor.Tests
{
    public sealed class BusLaneTests
    {
        // Records when it was ticked; finishes only when the test says so.
        private sealed class FakeStep : IStep
        {
            private readonly string name;
            private readonly List<string> log;

            public FakeStep(string name, List<string> log, bool finishOnFirstTick = false)
            {
                this.name = name;
                this.log = log;
                FinishOnFirstTick = finishOnFirstTick;
            }

            public bool FinishOnFirstTick;
            public bool IsDone { get; set; }

            public void Tick()
            {
                log.Add(name);
                if (FinishOnFirstTick) IsDone = true;
            }
        }

        private List<string> log;
        private BusLane lane;

        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            lane = new BusLane();
        }

        [Test]
        public void Empty_IsIdle()
        {
            Assert.IsTrue(lane.IsIdle);
            Assert.DoesNotThrow(() => lane.Tick());
        }

        [Test]
        public void OnlyFrontStepIsTicked_UntilItIsDone()
        {
            var first = new FakeStep("first", log);
            var second = new FakeStep("second", log);
            lane.Enqueue(first);
            lane.Enqueue(second);

            lane.Tick();
            lane.Tick();
            CollectionAssert.AreEqual(new[] { "first", "first" }, log);
            Assert.IsFalse(lane.IsIdle);

            first.IsDone = true;
            log.Clear();
            lane.Tick();

            CollectionAssert.AreEqual(new[] { "first", "second" }, log);
        }

        [Test]
        public void StepsThatFinishImmediately_AllRunInOneTick_InOrder()
        {
            lane.Enqueue(new FakeStep("a", log, finishOnFirstTick: true));
            lane.Enqueue(new FakeStep("b", log, finishOnFirstTick: true));
            lane.Enqueue(new FakeStep("c", log));

            lane.Tick();

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log);
            Assert.IsFalse(lane.IsIdle, "c is still running");
        }

        [Test]
        public void AllDone_BecomesIdle()
        {
            var step = new FakeStep("a", log);
            lane.Enqueue(step);
            lane.Tick();
            step.IsDone = true;
            lane.Tick();

            Assert.IsTrue(lane.IsIdle);
        }

        [Test]
        public void Clear_DropsAllSteps()
        {
            lane.Enqueue(new FakeStep("a", log));
            lane.Enqueue(new FakeStep("b", log));

            lane.Clear();
            lane.Tick();

            Assert.IsTrue(lane.IsIdle);
            Assert.IsEmpty(log);
        }
    }
}
