using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class IslandRevisionValidation
{
    const string Request="Temp/island_revision_istegi",Key="IslandRevision.Test";
    static IslandRevisionValidation()
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
                EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Traffic",mode=="traffic");EditorApplication.isPlaying=true;
            }
            catch(Exception error){File.WriteAllText("Logs/IslandRevisionSetup.txt",error.ToString());Debug.LogException(error);}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("IslandRevisionTestRobotu").AddComponent<IslandRevisionTestRobotu>().trafficOnly=SessionState.GetBool(Key+".Traffic",false);
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseBool(Key);
        };
    }
    static void Install()
    {
        string art;
        var island=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);
        try{art=IslandLayoutInstaller.RefreshOdtuCivicIdentity(island);PrefabUtility.SaveAsPrefabAsset(island,IslandSceneSetup.IslandPrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(island);}
        const string controllerPath="Assets/Prefabs/Taha/DisasterController.prefab";
        var controllerRoot=PrefabUtility.LoadPrefabContents(controllerPath);
        try{var c=controllerRoot.GetComponent<DisasterController>();c.selSuYuksekligi=c.kuruZeminY+.52f;PrefabUtility.SaveAsPrefabAsset(controllerRoot,controllerPath);}
        finally{PrefabUtility.UnloadPrefabContents(controllerRoot);}
        foreach(string path in new[]{IslandSceneSetup.MainScenePath,IslandSceneSetup.MapScenePath})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var legacy in IslandSceneSetup.Components<DereYatagiKarari>(scene))UnityEngine.Object.DestroyImmediate(legacy);
            foreach(var ui in IslandSceneSetup.Components<UIManager>(scene))
                if(ui.GetComponent<IslandSimulationPause>()==null)ui.gameObject.AddComponent<IslandSimulationPause>();
            foreach(var c in IslandSceneSetup.Components<DisasterController>(scene))
            {c.selSuYuksekligi=c.kuruZeminY+.52f;EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        var data=AssetDatabase.LoadAssetAtPath<AnlatiVerisi>("Assets/Resources/AnlatiVerisi.asset");
        data.okulIpucu="ODTÜ'de kısa bir quiz çöz; doğru bilgi, afet anında hayat kurtarır.";
        for(int i=0;i<data.afetIpuclari.Length;i++)
        {var hint=data.afetIpuclari[i];hint.yonlendirme=hint.yonlendirme.Replace("Okul","ODTÜ");data.afetIpuclari[i]=hint;}
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
        File.WriteAllText("Logs/IslandRevisionSetup.txt","PASS: riverbed event removed, flood waterline saved, pause driver installed. "+art);
    }
}
