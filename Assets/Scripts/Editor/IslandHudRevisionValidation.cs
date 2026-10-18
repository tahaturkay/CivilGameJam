using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Passive bridge for the explicitly requested real-scene HUD validation.</summary>
[InitializeOnLoad]
static class IslandHudRevisionValidation
{
    const string RequestPath="Temp/hud_revision_istegi";
    const string SessionKey="IslandHudRevisionValidation.Run";
    const string MainScenePath="Assets/Scenes/MainScene.unity";
    static bool handling;

    static IslandHudRevisionValidation()
    {
        EditorApplication.update+=PollRequest;
        EditorApplication.playModeStateChanged+=OnPlayModeChanged;
    }

    static void PollRequest()
    {
        if(handling||!File.Exists(RequestPath)||Application.isPlaying||
           EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        handling=true;
        try
        {
            string request=File.ReadAllText(RequestPath).Trim().ToLowerInvariant();
            if(request!="test")throw new InvalidOperationException("Write `test` to "+RequestPath+" to run the focused HUD probe.");
            File.Delete(RequestPath);
            SessionState.SetBool(SessionKey,true);
            EditorSceneManager.OpenScene(MainScenePath);
            EditorApplication.isPlaying=true;
        }
        catch(Exception error)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/HudRevisionTests.txt","FAIL: "+error);
            Debug.LogException(error);
        }
        finally{handling=false;}
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(SessionKey,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
            new GameObject("IslandHudRevisionProbe").AddComponent<IslandHudRevisionProbe>();
        else if(state==PlayModeStateChange.EnteredEditMode)
            SessionState.EraseBool(SessionKey);
    }
}
