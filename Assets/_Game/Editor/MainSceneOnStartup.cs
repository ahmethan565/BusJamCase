using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    // The editor works on the game scene: Unity opens Main.unity when it starts, and Play always
    // starts from Main.unity, whichever scene is open (it comes back to that scene after Play).
    [InitializeOnLoad]
    public static class MainSceneOnStartup
    {
        public const string MainScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string OpenedThisSessionKey = "Game.MainSceneOpenedOnStartup";

        static MainSceneOnStartup()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);

            // Only once per editor session: after a script reload the designer's current scene stays.
            if (SessionState.GetBool(OpenedThisSessionKey, false)) return;
            SessionState.SetBool(OpenedThisSessionKey, true);
            EditorApplication.delayCall += OpenMainScene;
        }

        private static void OpenMainScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path == MainScenePath) return;
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
    }
}
