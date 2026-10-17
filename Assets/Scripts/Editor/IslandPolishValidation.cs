using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class IslandPolishValidation
{
    const string Key = "AdaPolishTesti.Aktif";

    static IslandPolishValidation()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || !File.Exists("Temp/ada_polish_testi")) return;

            File.Delete("Temp/ada_polish_testi");
            Directory.CreateDirectory("Logs");
            EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        };

        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                new GameObject("IslandPolishTestRobotu").AddComponent<IslandPolishTestRobotu>();
            else if (state == PlayModeStateChange.EnteredEditMode)
                SessionState.SetBool(Key, false);
        };
    }
}
