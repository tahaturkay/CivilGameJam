using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit request bridge for the focused real-scene tsunami finale probe.</summary>
[InitializeOnLoad]
static class TsunamiFinaleValidation
{
    const string RequestPath = "Temp/tsunami_finale_istegi";
    const string RunKey = "TsunamiFinaleValidation.Run";
    internal const string NextRunKey = "TsunamiFinaleValidation.NextRun";
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    static bool handlingRequest;

    static TsunamiFinaleValidation()
    {
        EditorApplication.update += PollRequest;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void PollRequest()
    {
        if (handlingRequest || !File.Exists(RequestPath) || Application.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        handlingRequest = true;
        try
        {
            string mode = File.ReadAllText(RequestPath).Trim().ToLowerInvariant();
            if (mode != "weak" && mode != "strong" && mode != "both")
                throw new InvalidOperationException("Use `weak`, `strong` or `both` in " + RequestPath + ".");

            File.Delete(RequestPath);
            SessionState.EraseString(NextRunKey);
            SessionState.SetString(RunKey, mode == "both" ? "weak-both" : mode);
            OpenMainSceneAndPlay();
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/TsunamiFinaleTests.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
        finally { handlingRequest = false; }
    }

    static void OpenMainSceneAndPlay()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        // The bridge is explicit; do not refresh assets or save a user's unrelated open scene.
        EditorSceneManager.OpenScene(MainScenePath);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        string run = SessionState.GetString(RunKey, "");
        if (run.Length == 0) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            bool strong = run == "strong";
            bool both = run == "weak-both";
            var probe = new GameObject("TsunamiFinaleProbe").AddComponent<TsunamiFinaleProbe>();
            probe.strongDefense = strong;
            probe.runBothBranches = both;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (SessionState.GetString(NextRunKey, "") == "strong")
            {
                SessionState.EraseString(NextRunKey);
                SessionState.SetString(RunKey, "strong");
                EditorApplication.delayCall += OpenMainSceneAndPlay;
            }
            else
            {
                SessionState.EraseString(RunKey);
            }
        }
    }
}
