using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit setup and focused real-scene checks; never regenerates the island.</summary>
[InitializeOnLoad]
static class IslandEmergencyRevisionValidation
{
    const string Request="Temp/emergency_revision_istegi",Key="EmergencyRevision.Run";
    static bool busy;
    static IslandEmergencyRevisionValidation()
    {
        EditorApplication.update+=()=>
        {
            if(busy||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            busy=true;
            try
            {
                string mode=File.ReadAllText(Request).Trim();File.Delete(Request);
                if(mode=="setup")Install();
                else if(mode=="test"){EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
                else throw new InvalidOperationException("Use setup or test.");
            }
            catch(Exception error){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/EmergencyRevisionSetup.txt","FAIL: "+error);Debug.LogException(error);}
            finally{busy=false;}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("EmergencyRevisionProbe").AddComponent<IslandEmergencyRevisionProbe>();
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseBool(Key);
        };
    }
    static void Install()
    {
        EditorSceneManager.SaveOpenScenes();var prefab=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);int count=0;
        try
        {
            Transform roads=prefab.transform.Find("Yollar");if(roads==null)throw new InvalidOperationException("Yollar missing.");
            foreach(var building in roads.GetComponentsInChildren<BuildingVisuals>(true))
            {
                if(building.binaTuru!=BinaTuru.Altyapi)continue;
                Vector3 forward=building.transform.localRotation*Vector3.forward;
                Vector3 position=building.transform.localPosition;position.y=Mathf.Abs(forward.z)>Mathf.Abs(forward.x)?-.025f:0;
                building.transform.localPosition=position;count++;
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,IslandSceneSetup.IslandPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/EmergencyRevisionSetup.txt","PASS: "+count+" existing road beds separated by 25mm; original meshes, surfaces and levels preserved.");
    }
}
