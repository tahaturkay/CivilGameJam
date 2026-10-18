using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in, cloth-only white-field/red-emblem patch. No map or civic building regeneration.</summary>
[InitializeOnLoad]
public static class IslandWhiteFlagInstaller
{
    const string Request="Temp/white_flags_istegi",Log="Logs/WhiteFlagsSetup.txt";
    static bool busy;
    static IslandWhiteFlagInstaller(){EditorApplication.update+=Poll;}

    static void Poll()
    {
        if(busy||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||
            EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        busy=true;
        try
        {
            string mode=File.ReadAllText(Request).Trim();File.Delete(Request);
            if(mode!="setup")throw new InvalidOperationException("Unknown white flag request: "+mode);
            Install();
        }
        catch(Exception error)
        {
            Directory.CreateDirectory("Logs");File.WriteAllText(Log,"FAIL: "+error);Debug.LogException(error);
        }
        finally{busy=false;}
    }

    [MenuItem("Oyun/Ada Haritası/White ODTÜ Flag Cloths")]
    public static void Install()
    {
        if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Flag cloth setup requires Edit Mode.");
        EditorSceneManager.SaveOpenScenes();
        GameObject prefab=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);
        try
        {
            IslandLayoutInstaller.RefreshOdtuFlagCloth(prefab);Validate(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab,IslandSceneSetup.IslandPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        Scene active=SceneManager.GetActiveScene();
        var report=new System.Text.StringBuilder("PASS: white cloth/red emblem, existing five flag geometries and poles preserved.\n");
        foreach(string path in new[]{IslandSceneSetup.MainScenePath,IslandSceneSetup.MapScenePath})
        {
            Scene scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                GameObject island=scene.GetRootGameObjects().FirstOrDefault(go=>go.name=="LowPolyAda");
                if(island==null)throw new InvalidOperationException("LowPolyAda missing in "+path);
                bool shared=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(island)==IslandSceneSetup.IslandPrefabPath;
                if(!shared)IslandLayoutInstaller.RefreshOdtuFlagCloth(island);
                Validate(island);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.AppendLine(path+": five validated flags ("+(shared?"shared prefab":"cloth-only patch")+").");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText(Log,report.ToString());
    }

    static void Validate(GameObject island)
    {
        var flags=island.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ODTU_Bayragi").ToArray();
        if(flags.Length!=5)throw new InvalidOperationException("Expected five ODTÜ flags, found "+flags.Length);
        foreach(Transform flag in flags)
        {
            var renderers=flag.GetComponentsInChildren<MeshRenderer>(true);
            if(renderers.Length!=1)throw new InvalidOperationException("Flag cloth must use one combined renderer.");
            Material[] palette=renderers[0].sharedMaterials;
            bool white=palette.Length>0&&palette[0]!=null&&Vector3.Distance(new Vector3(palette[0].color.r,palette[0].color.g,palette[0].color.b),Vector3.one)<.03f;
            bool red=palette.Length>1&&palette[1]!=null&&palette[1].color.r>.8f&&palette[1].color.g<.1f&&palette[1].color.b<.12f;
            if(palette.Length!=2||!white||!red)throw new InvalidOperationException("Flag must contain white cloth and red emblem materials only.");
        }
    }
}
