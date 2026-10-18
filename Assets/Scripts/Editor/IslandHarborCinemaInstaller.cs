using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Incremental harbor update. Never reinstalls terrain, buildings, managers, HUDs or disaster wiring.</summary>
[InitializeOnLoad]
public static class IslandHarborCinemaInstaller
{
    const string RequestPath="Temp/harbor_cinema_istegi";
    const string LogPath="Logs/HarborCinemaSetup.txt";
    static bool handlingRequest;

    static IslandHarborCinemaInstaller() { EditorApplication.update+=PollRequest; }

    static void PollRequest()
    {
        if(handlingRequest||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||
            EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(RequestPath))return;
        handlingRequest=true;
        try
        {
            string mode=File.ReadAllText(RequestPath).Trim();
            if(mode!="setup")throw new InvalidOperationException("Unknown harbor request: "+mode);
            // Do not refresh assets here: it can re-enter this callback before the play guard runs.
            File.Delete(RequestPath);
            RefreshExistingHarbors();
        }
        catch(Exception error)
        {
            Directory.CreateDirectory("Logs");File.WriteAllText(LogPath,"FAIL: "+error);
            Debug.LogException(error);
        }
        finally { handlingRequest=false; }
    }

    [MenuItem("Oyun/Ada Haritası/Refresh Harbor Cinematic Markers")]
    public static void RefreshExistingHarbors()
    {
        if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Harbor setup requires Edit Mode.");
        EditorSceneManager.SaveOpenScenes();
        GameObject prefab=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);
        try
        {
            IslandLayoutInstaller.RefreshHarborForCinema(prefab);
            ValidateHarbor(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab,IslandSceneSetup.IslandPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        Scene active=SceneManager.GetActiveScene();
        var report=new System.Text.StringBuilder("PASS: existing harbor refreshed in shared prefab and both island scenes.\n");
        foreach(string path in new[]{IslandSceneSetup.MainScenePath,IslandSceneSetup.MapScenePath})
        {
            Scene scene=SceneManager.GetSceneByPath(path);
            bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                GameObject island=scene.GetRootGameObjects().FirstOrDefault(go=>go.name=="LowPolyAda");
                if(island==null)throw new InvalidOperationException("LowPolyAda is missing in "+path);
                // Shared instances already inherit the new subtree. Do not remove overrides or unpack them.
                bool shared=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(island)==IslandSceneSetup.IslandPrefabPath;
                if(!shared)IslandLayoutInstaller.RefreshHarborForCinema(island);
                ValidateHarbor(island);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Transform harbor=island.transform.Find("SabitAlanlar/Liman");
                report.AppendLine(path+": "+(shared?"shared prefab instance":"standalone harbor updated"));
                foreach(Transform marker in harbor.Find("CinematicMarkers"))
                    if(marker.name!="Footpath")report.AppendLine("  "+marker.name+" world="+marker.position.ToString("F3"));
            }
            finally { if(opened)EditorSceneManager.CloseScene(scene,true); }
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");File.WriteAllText(LogPath,report.ToString());
        Debug.Log("Harbor cinematic setup complete: independent supplied LowPolyGemi sailboat, clear low-pier boarding path, 40 stair markers.");
    }

    static void ValidateHarbor(GameObject island)
    {
        Transform harbor=island.transform.Find("SabitAlanlar/Liman");
        if(harbor==null)throw new InvalidOperationException("Harbor root missing.");
        Transform boat=harbor.Find("BalikciTeknesi"),infrastructure=harbor.Find("LimanAltyapisi"),markers=harbor.Find("CinematicMarkers");
        if(boat==null||infrastructure==null||markers==null)
            throw new InvalidOperationException("Harbor override blocks the new boat/marker hierarchy.");
        if(boat.GetComponentsInChildren<MeshRenderer>(true).Length!=1||infrastructure.GetComponentsInChildren<MeshRenderer>(true).Length!=1)
            throw new InvalidOperationException("Harbor must have one independent boat batch and one infrastructure batch.");
        foreach(string name in new[]{"BoatMooring","BoatApproachOrigin","BoatApproachTurn","PierDisembarkA","PierDisembarkB","CharacterStandA","CharacterStandB","LookToSea","LandExit"})
            if(markers.Find(name)==null)throw new InvalidOperationException("Missing cinematic marker "+name);
        if(markers.Find("Footpath")==null||markers.Find("Footpath").childCount!=43)
            throw new InvalidOperationException("Harbor footpath must contain lower pier, 40 stairs, upper pier and land exit.");
        foreach(string name in new[]{"PassengerA","PassengerB","DeckExit"})
            if(boat.Find("DeckMarkers/"+name)==null)throw new InvalidOperationException("Missing boat deck marker "+name);
        GameObject supplied=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Taha/LowPolyGemi.prefab");
        MeshFilter source=supplied!=null?supplied.GetComponentInChildren<MeshFilter>(true):null;
        MeshFilter installed=boat.GetComponentInChildren<MeshFilter>(true);
        if(source==null||installed==null||installed.sharedMesh!=source.sharedMesh)
            throw new InvalidOperationException("Harbor vessel must reuse the supplied LowPolyGemi mesh.");
        Renderer sourceRenderer=supplied.GetComponentInChildren<Renderer>(true),installedRenderer=boat.GetComponentInChildren<Renderer>(true);
        if(sourceRenderer==null||installedRenderer==null||installedRenderer.sharedMaterial!=sourceRenderer.sharedMaterial)
            throw new InvalidOperationException("Harbor vessel must preserve its supplied palette material.");
        if(markers.Find("BoatApproachOrigin").localPosition.z>-130)
            throw new InvalidOperationException("Boat approach must begin offshore south of the harbor.");
        foreach(MeshFilter filter in harbor.GetComponentsInChildren<MeshFilter>(true))
            if(filter.sharedMesh==null)throw new InvalidOperationException("Missing harbor mesh at "+filter.name);
    }
}
