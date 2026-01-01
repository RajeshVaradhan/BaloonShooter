using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BalloonShooter.Editor
{
    [InitializeOnLoad]
    public static class BalloonShooterAutoSetup
    {
        private const string SessionKey = "BalloonShooter.AutoSetup.RanThisSession";
        private const string SetupVersionKey = "BalloonShooter.SetupVersion";
        private const int CurrentSetupVersion = 2;

        static BalloonShooterAutoSetup()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string configPath = BalloonShooterSetupWizard.GeneratedConfigPath;
            string scenePath = BalloonShooterSetupWizard.GeneratedScenePath;

            int previousVersion = EditorPrefs.GetInt(SetupVersionKey, 0);
            bool needsSetup =
                AssetDatabase.LoadAssetAtPath<GameConfig>(configPath) == null ||
                !System.IO.File.Exists(scenePath) ||
                previousVersion < CurrentSetupVersion;

            if (!needsSetup) return;

            BalloonShooterSetupWizard.SetupOrUpdateGame();
            EditorPrefs.SetInt(SetupVersionKey, CurrentSetupVersion);

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Debug.Log("Balloon Shooter: auto-setup ran (generated scene + tuning assets).");
        }
    }
}
