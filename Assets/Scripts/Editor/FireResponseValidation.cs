using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class FireResponseValidation
{
    const string Request="Temp/fire_response_istegi",Key="FireResponse.Test";
    static bool handling;
    static FireResponseValidation()
    {
        EditorApplication.update+=()=>
        {
            if(handling||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            handling=true;
            try
            {
                AssetDatabase.Refresh();
                if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                string mode=File.ReadAllText(Request).Trim();
                EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
                SessionState.SetBool(Key+".Engine",mode=="engine");
                File.Delete(Request);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
            }
            catch(Exception error){File.WriteAllText("Logs/FireResponseSetup.txt",error.ToString());Debug.LogException(error);}
            finally{handling=false;}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("FireResponseProbe").AddComponent<FireResponseProbe>().engineOnly=SessionState.GetBool(Key+".Engine",false);
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseBool(Key);
        };
    }
}
