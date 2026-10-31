using System.Linq;
using Game.Runtime.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // [Audio] in Main.unity. Read-only.
    public sealed class AudioWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";

        private Scene scene;
        private bool openedByTest;

        [OneTimeSetUp]
        public void OpenScene()
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedByTest = true;
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (openedByTest) EditorSceneManager.CloseScene(scene, true);
        }

        private T SceneComponent<T>() where T : Component
        {
            T[] found = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
            Assert.AreEqual(1, found.Length, $"exactly one {typeof(T).Name} expected in Main.unity");
            return found[0];
        }

        [Test]
        public void AudioPlayer_HasSixQuietSources()
        {
            AudioPlayer player = SceneComponent<AudioPlayer>();
            SerializedProperty array = new SerializedObject(player).FindProperty("sources");

            Assert.AreEqual(6, array.arraySize);
            for (int i = 0; i < array.arraySize; i++)
            {
                var source = (AudioSource)array.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(source, $"source {i}");
                Assert.IsFalse(source.playOnAwake, $"source {i}");
                Assert.IsFalse(source.loop, $"source {i}");
            }
        }

        [Test]
        public void VfxPlayer_HasBothPrefabs()
        {
            var so = new SerializedObject(SceneComponent<VfxPlayer>());

            Assert.IsNotNull(so.FindProperty("exhaustPuffPrefab").objectReferenceValue);
            Assert.IsNotNull(so.FindProperty("confettiPrefab").objectReferenceValue);
        }

        [Test]
        public void Scene_HasExactlyOneEnabledAudioListener_OnTheCamera()
        {
            AudioListener listener = SceneComponent<AudioListener>();

            Assert.IsTrue(listener.enabled, "without a listener nothing is heard");
            Assert.IsNotNull(listener.GetComponent<Camera>(), "the listener sits on the camera");
        }

        [Test]
        public void AudioAndVfx_SitUnderTheAudioObject()
        {
            Assert.AreEqual("[Audio]", SceneComponent<AudioPlayer>().transform.root.name);
            Assert.AreEqual("[Audio]", SceneComponent<VfxPlayer>().transform.root.name);
        }
    }
}
