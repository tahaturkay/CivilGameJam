using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class CityFeedbackValidation
{
    const string Request="Temp/city_feedback_istegi",Key="CityFeedback.Test";
    static CityFeedbackValidation()
    {
        EditorApplication.update+=()=>
        {
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            AssetDatabase.Refresh();if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            string mode=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                EditorSceneManager.SaveOpenScenes();
                if(mode=="setup"){Install();return;}
                EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
                SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
            }
            catch(Exception error){File.WriteAllText("Logs/CityFeedbackSetup.txt",error.ToString());Debug.LogException(error);}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("CityFeedbackTestRobotu").AddComponent<CityFeedbackTestRobotu>();
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseBool(Key);
        };
    }
    static void Install()
    {
        var root=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);
        try
        {
            if(root.GetComponent<IslandSirenController>()==null)root.AddComponent<IslandSirenController>();
            PrefabUtility.SaveAsPrefabAsset(root,IslandSceneSetup.IslandPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(string path in new[]{IslandSceneSetup.MainScenePath,IslandSceneSetup.MapScenePath})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var city in IslandSceneSetup.Components<IslandCityLife>(scene))
                if(city.GetComponent<IslandSirenController>()==null)city.gameObject.AddComponent<IslandSirenController>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
        File.WriteAllText("Logs/CityFeedbackSetup.txt","PASS: siren component saved on common island prefab and both gameplay scenes; existing art and state preserved.");
    }
}
