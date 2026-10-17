using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class IslandSystemsValidation
{
    const string Key="AdaSistemTesti.Aktif";
    static IslandSystemsValidation()
    {
        EditorApplication.update+=()=>
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/ada_sistem_testi"))return;
            File.Delete("Temp/ada_sistem_testi");
            EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("AdaSistemTestRobotu").AddComponent<IslandSystemsTestRobotu>();
            else if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
        };
    }
}
