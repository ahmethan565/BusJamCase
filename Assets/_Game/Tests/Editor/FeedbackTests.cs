using System;
using System.Collections.Generic;
using System.Linq;
using Game.Runtime.Data;
using Game.Runtime.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor.Tests
{
    // Sound and effects. Players are built in a preview scene; real assets are only read.
    public sealed class FeedbackTests
    {
        private const string VfxFolder = "Assets/_Game/Content/Prefabs/Vfx/";

        private Scene preview;
        private SoundLibrary library;
        private AudioPlayer audioPlayer;
        private AudioSource[] sources;
        private VfxPlayer vfxPlayer;

        private static T LoadSingle<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            Assert.AreEqual(1, guids.Length, $"exactly one {typeof(T).Name} asset expected");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static PooledParticle LoadVfx(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<PooledParticle>(VfxFolder + name + ".prefab");
            Assert.IsNotNull(prefab, $"missing VFX prefab {name}");
            return prefab;
        }

        private static IEnumerable<T> AllValues<T>() => Enum.GetValues(typeof(T)).Cast<T>();

        [SetUp]
        public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            library = LoadSingle<SoundLibrary>();

            var audioGo = new GameObject("AudioPlayer");
            SceneManager.MoveGameObjectToScene(audioGo, preview);
            sources = new AudioSource[6];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = audioGo.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }
            audioPlayer = audioGo.AddComponent<AudioPlayer>();
            var audioSo = new SerializedObject(audioPlayer);
            SerializedProperty array = audioSo.FindProperty("sources");
            array.arraySize = sources.Length;
            for (int i = 0; i < sources.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            // The VfxPlayer is a copy of the scene's one, so its prefab list is the real wiring.
            var vfxGo = new GameObject("VfxPlayer");
            SceneManager.MoveGameObjectToScene(vfxGo, preview);
            vfxPlayer = vfxGo.AddComponent<VfxPlayer>();
            var vfxSo = new SerializedObject(vfxPlayer);
            vfxSo.FindProperty("exhaustPuffPrefab").objectReferenceValue = LoadVfx("ExhaustPuff");
            vfxSo.FindProperty("confettiPrefab").objectReferenceValue = LoadVfx("Confetti");
            vfxSo.ApplyModifiedPropertiesWithoutUndo();
            vfxPlayer.Init();
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(preview);

        private AudioSource SourceWith(SoundId id)
        {
            library.TryGet(id, out AudioClip clip, out _);
            return sources.FirstOrDefault(s => s.clip == clip);
        }

        // --- SoundLibrary and import settings

        [Test]
        public void SoundLibrary_HasAClipForEverySound()
        {
            foreach (SoundId id in AllValues<SoundId>())
            {
                Assert.IsTrue(library.TryGet(id, out AudioClip clip, out float volume), id.ToString());
                Assert.IsNotNull(clip, id.ToString());
                Assert.That(volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), id.ToString());
            }
        }

        [Test]
        public void SoundClips_UseShortEffectImportSettings()
        {
            foreach (SoundId id in AllValues<SoundId>())
            {
                library.TryGet(id, out AudioClip clip, out _);
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, id.ToString());
                Assert.AreEqual(AudioCompressionFormat.ADPCM, settings.compressionFormat, id.ToString());
                Assert.IsTrue(importer.forceToMono, id.ToString());
            }
        }

        // --- AudioPlayer

        [Test]
        public void AudioPlayer_SameSoundTwiceInOneFrame_PlaysOnce()
        {
            library.TryGet(SoundId.Board, out AudioClip clip, out float volume);

            Assert.IsTrue(audioPlayer.Play(SoundId.Board, clip, volume, 1f));
            Assert.IsFalse(audioPlayer.Play(SoundId.Board, clip, volume, 1.1f), "no pile-up when waiting passengers board together");
        }

        [Test]
        public void AudioPlayer_DifferentSounds_UseSourcesRoundRobin()
        {
            library.TryGet(SoundId.Pop, out AudioClip pop, out float popVolume);
            library.TryGet(SoundId.Click, out AudioClip click, out float clickVolume);

            audioPlayer.Play(SoundId.Pop, pop, popVolume, 1f);
            audioPlayer.Play(SoundId.Click, click, clickVolume, 1f);

            Assert.AreSame(pop, sources[0].clip);
            Assert.AreSame(click, sources[1].clip);
        }

        // --- VfxPlayer and prefabs

        [Test]
        public void VfxPrefabs_ReturnToPoolOnStop_AreSmallAndUnlit()
        {
            foreach ((string name, int maxParticles) in new[] { ("ExhaustPuff", 12), ("Confetti", 60) })
            {
                PooledParticle prefab = LoadVfx(name);
                var system = prefab.GetComponent<ParticleSystem>();
                Assert.AreEqual(ParticleSystemStopAction.Callback, system.main.stopAction, name);
                Assert.IsFalse(system.main.loop, name);
                Assert.LessOrEqual(system.main.maxParticles, maxParticles, $"{name} (§13.6)");
                var renderer = prefab.GetComponent<ParticleSystemRenderer>();
                StringAssert.Contains("Unlit", renderer.sharedMaterial.shader.name, name);
            }
        }

        [Test]
        public void VfxPlayer_Play_PlacesAnActiveInstance()
        {
            var at = new Vector3(1f, 2f, 3f);

            PooledParticle first = vfxPlayer.Play(VfxId.ExhaustPuff, at);
            PooledParticle second = vfxPlayer.Play(VfxId.ExhaustPuff, at);

            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(at, first.transform.position);
            Assert.AreNotSame(first, second, "a playing effect is not taken again");
        }

        [Test]
        public void VfxPlayer_StoppedEffect_GoesBackToThePool()
        {
            PooledParticle first = vfxPlayer.Play(VfxId.Confetti, Vector3.zero);

            first.SendMessage("OnParticleSystemStopped");   // what Unity calls when stopAction = Callback

            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreSame(first, vfxPlayer.Play(VfxId.Confetti, Vector3.zero), "the pooled instance is reused");
        }

        // --- FeedbackPlayer

        [Test]
        public void Boarding_PitchRisesWithTheSeat()
        {
            var feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, library);

            feedback.PassengerBoarded(2);

            AudioSource source = SourceWith(SoundId.Board);
            Assert.IsNotNull(source);
            Assert.AreEqual(1.2f, source.pitch, 1e-4f, "1 + seat * 0.1");
        }

        [Test]
        public void EveryEvent_PlaysItsSound()
        {
            var feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, library);
            var cases = new (Action call, SoundId sound)[]
            {
                (() => feedback.PassengerTapped(Vector3.zero), SoundId.Pop),
                (() => feedback.PassengerBlocked(Vector3.zero), SoundId.Bonk),
                (() => feedback.PassengerBoarded(0), SoundId.Board),
                (() => feedback.PassengerWaiting(), SoundId.Seat),
                (() => feedback.BusDeparted(Vector3.zero), SoundId.Horn),
            };

            foreach ((Action call, SoundId sound) in cases)
            {
                call();
                Assert.IsNotNull(SourceWith(sound), sound.ToString());
            }

            // Only 6 sources: the rest in a fresh round.
            SetUpFreshAudio();
            feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, library);
            feedback.BusArrived();
            feedback.Won();
            feedback.Lost();
            feedback.UiClick();
            Assert.IsNotNull(SourceWith(SoundId.Brake));
            Assert.IsNotNull(SourceWith(SoundId.Win));
            Assert.IsNotNull(SourceWith(SoundId.Lose));
            Assert.IsNotNull(SourceWith(SoundId.Click));
        }

        [Test]
        public void BusDeparted_PuffsExhaustAtTheBus_Won_ThrowsConfetti()
        {
            var feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, library);
            var busAt = new Vector3(-2f, 0f, 3.5f);

            feedback.BusDeparted(busAt);
            feedback.Won();

            PooledParticle[] active = vfxPlayer.GetComponentsInChildren<PooledParticle>(false);
            Assert.IsTrue(active.Any(p => p.Id == VfxId.ExhaustPuff && p.transform.position == busAt));
            Assert.IsTrue(active.Any(p => p.Id == VfxId.Confetti));
        }

        private void SetUpFreshAudio()
        {
            foreach (AudioSource source in sources) source.clip = null;
        }
    }
}
