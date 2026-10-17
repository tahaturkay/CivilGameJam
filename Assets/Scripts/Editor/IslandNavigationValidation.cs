using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Incremental cursor wiring and focused day-cycle/navigation checks; never rebuilds the island art.</summary>
[InitializeOnLoad]
static class IslandNavigationValidation
{
    const string Key="AdaGezinmeTesti.Aktif";
    static IslandNavigationValidation()
    {
        EditorApplication.update+=()=>
        {
            const string request="Temp/ada_kontrol_istegi";
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            string mode=File.ReadAllText(request).Trim();File.Delete(request);
            if(mode=="setup"){InstallCursor();return;}
            EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("AdaDonguVeKameraTesti").AddComponent<IslandNavigationTestRobotu>();
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
        };
    }
    [MenuItem("Oyun/Ada Haritası/Apply Lighthouse Cursor")]
    static void InstallCursor()
    {
        EditorSceneManager.SaveOpenScenes();
        const string path="Assets/Art/Cursors/FenerOku.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Cursor;importer.mipmapEnabled=false;importer.isReadable=true;
        importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        PlayerSettings.defaultCursor=texture;PlayerSettings.cursorHotspot=new Vector2(6,5);
        foreach(string scenePath in new[]{IslandSceneSetup.MapScenePath,IslandSceneSetup.MainScenePath})
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            Camera camera=IslandSceneSetup.Components<Camera>(scene).First(c=>c.CompareTag("MainCamera"));
            IslandCursor cursor=camera.GetComponent<IslandCursor>();if(cursor==null)cursor=camera.gameObject.AddComponent<IslandCursor>();
            cursor.pointer=texture;cursor.hotspot=new Vector2(6,5);EditorUtility.SetDirty(cursor);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Logs/AdaKontrolKurulumu.txt","PASS: Fener Oku cursor installed in both island scenes and player defaults.");
    }
}
