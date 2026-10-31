using System.Collections.Generic;
using Game.Runtime.Flow;
using NUnit.Framework;

namespace Game.Editor.Tests
{
    // Exit of the current state runs before Enter of the next one.
    public sealed class AppStateMachineTests
    {
        private sealed class LoggingState : IAppState
        {
            private readonly string name;
            private readonly List<string> log;

            public LoggingState(string name, List<string> log)
            {
                this.name = name;
                this.log = log;
            }

            public void Enter() => log.Add(name + ".Enter");
            public void Exit() => log.Add(name + ".Exit");
        }

        [Test]
        public void FirstChange_OnlyEnters()
        {
            var log = new List<string>();
            var machine = new AppStateMachine();

            machine.ChangeTo(new LoggingState("A", log));

            CollectionAssert.AreEqual(new[] { "A.Enter" }, log);
        }

        [Test]
        public void ChangeTo_ExitsCurrentThenEntersNext()
        {
            var log = new List<string>();
            var machine = new AppStateMachine();
            machine.ChangeTo(new LoggingState("A", log));

            machine.ChangeTo(new LoggingState("B", log));

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, log);
        }

        [Test]
        public void ChangeTo_SameKindAgain_RebuildsWithExitThenEnter()
        {
            // "Restart" and "retry" are a change to a NEW gameplay state, never a reset.
            var log = new List<string>();
            var machine = new AppStateMachine();
            machine.ChangeTo(new LoggingState("Gameplay", log));

            machine.ChangeTo(new LoggingState("Gameplay", log));

            CollectionAssert.AreEqual(new[] { "Gameplay.Enter", "Gameplay.Exit", "Gameplay.Enter" }, log);
        }
    }
}
