using NUnit.Framework;

namespace Game.Core.Tests
{
    // Phase 0: proves the Core test assembly compiles and the Test Runner picks it up.
    public sealed class TestRunnerSmokeTests
    {
        [Test]
        public void CoreTestAssembly_Runs()
        {
            Assert.Pass();
        }
    }
}
