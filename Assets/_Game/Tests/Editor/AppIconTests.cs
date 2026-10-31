using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The app icon (§4.2): one 1024x1024 default icon that every platform uses.
    public sealed class AppIconTests
    {
        private const string IconPath = "Assets/_Game/Content/Images/AppIcon.png";

        [Test]
        public void DefaultIcon_IsTheAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            Assert.IsNotNull(icon);
            Assert.AreEqual((1024, 1024), (icon.width, icon.height), "full size, not downscaled on import");

            Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            Assert.IsNotEmpty(icons);
            Assert.AreSame(icon, icons[0]);
        }
    }
}
