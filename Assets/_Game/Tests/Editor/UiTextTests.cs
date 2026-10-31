using Game.Runtime.UI;
using NUnit.Framework;

namespace Game.Editor.Tests
{
    // Code counts levels from 0, the player sees numbers from 1.
    public sealed class UiTextTests
    {
        [Test]
        public void LevelLabel_IsOneBased()
        {
            Assert.AreEqual("Level 1", UiText.LevelLabel(0));
            Assert.AreEqual("Level 5", UiText.LevelLabel(4));
        }
    }
}
