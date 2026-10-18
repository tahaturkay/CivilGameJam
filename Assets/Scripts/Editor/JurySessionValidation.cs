using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
static class JurySessionValidation
{
    const string Request="Temp/jury_istegi",Key="JurySession.Mode";
    static JurySessionValidation()
    {
        EditorApplication.update+=()=>
        {
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            AssetDatabase.Refresh();if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            string mode=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(mode=="setup"){Install();return;}
                if(mode=="windows"){BuildWindows();return;}
                EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(mode=="menu"?"Assets/Scenes/MainMenu.unity":IslandSceneSetup.MainScenePath);
                SessionState.SetString(Key,mode);EditorApplication.isPlaying=true;
            }
            catch(Exception e){File.WriteAllText("Logs/JurySetup.txt","FAIL: "+e);Debug.LogException(e);}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            string mode=SessionState.GetString(Key,"");if(string.IsNullOrEmpty(mode))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                string name=mode=="menu"?"JuryMenuCapture":mode=="hazards"||mode=="impact"?"JuryHazardProbe":"JuryGameplayTestRobotu";
                Type type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
                if(type==null){Debug.LogError("Missing test component: "+name);EditorApplication.isPlaying=false;return;}
                Component test=new GameObject(name).AddComponent(type);
                type.GetField("timedSession")?.SetValue(test,mode=="timed");
                type.GetField("impactOnly")?.SetValue(test,mode=="impact");
            }
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseString(Key);
        };
    }
    [MenuItem("Oyun/Jüri/Apply 3 Minute Session")]
    static void Install()
    {
        Directory.CreateDirectory("Logs");EditorSceneManager.SaveOpenScenes();
        var settings=AssetDatabase.LoadAssetAtPath<DengeAyarlari>("Assets/Resources/DengeAyarlari.asset");
        settings.toplamGun=20;settings.birGunSuresi=12;settings.baslangicHizi=2;
        settings.dogruCevapOdulu=25;settings.konutGelirCarpanlari=new[]{1f,1.35f,1.75f};settings.yildizEsikleri=new[]{80,180,320};
        for(int i=0;i<settings.afetler.Length;i++){var a=settings.afetler[i];a.gun=(i+1)*5;settings.afetler[i]=a;}
        EditorUtility.SetDirty(settings);
        if(AssetDatabase.LoadAssetAtPath<AnlatiVerisi>("Assets/Resources/AnlatiVerisi.asset")==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AnlatiVerisi>(),"Assets/Resources/AnlatiVerisi.asset");
        const string foamPath="Assets/Resources/IslandEffects/CoastFoam.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(foamPath)==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("CivilIsland/Coastal Foam")),foamPath);
        var island=PrefabUtility.LoadPrefabContents(IslandSceneSetup.IslandPrefabPath);
        try
        {
            IslandLayoutInstaller.RefreshJunctionMarkings(island);
            if(island.GetComponent<IslandCoastWaves>()==null)island.AddComponent<IslandCoastWaves>();
            PrefabUtility.SaveAsPrefabAsset(island,IslandSceneSetup.IslandPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(island);}
        const string controllerPath="Assets/Prefabs/Taha/DisasterController.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(controllerPath);
        try{prefab.GetComponent<DisasterController>().afetSuresi=8;PrefabUtility.SaveAsPrefabAsset(prefab,controllerPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        foreach(string path in new[]{IslandSceneSetup.MapScenePath,IslandSceneSetup.MainScenePath,"Assets/Scenes/MainMenu.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var controller in IslandSceneSetup.Components<DisasterController>(scene))
            {
                controller.afetSuresi=8;controller.selSuYuksekligi=controller.kuruZeminY+.30f;
                controller.tsunamiSuYuksekligi=controller.kuruZeminY+.62f;Record(controller);
            }
            foreach(var ui in IslandSceneSetup.Components<UIManager>(scene))
                if(ui.GetComponent<DereYatagiKarari>()==null)ui.gameObject.AddComponent<DereYatagiKarari>();
            var camera=IslandSceneSetup.Components<Camera>(scene).FirstOrDefault(c=>c.CompareTag("MainCamera"));
            if(camera!=null&&camera.GetComponent<IslandPerformanceBudget>()==null)camera.gameObject.AddComponent<IslandPerformanceBudget>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        ConfigureRendering();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
        File.WriteAllText("Logs/JurySetup.txt","PASS: 20 days / 12s / initial2x / incidents5,10,15,20 / 8s effects; explicit prefab and scene values saved.\nWindows64 module: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64));
    }
    static void Record(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
    static void ConfigureRendering()
    {
        foreach(string path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Mobile_RPAsset.asset"})
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);var data=new SerializedObject(pipeline);
            data.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
            data.FindProperty("m_ShadowDistance").floatValue=280;data.FindProperty("m_ShadowCascadeCount").intValue=2;
            data.FindProperty("m_RequireOpaqueTexture").boolValue=false;data.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        foreach(var feature in renderer.rendererFeatures)if(feature!=null&&feature.GetType().Name=="ScreenSpaceAmbientOcclusion")
        {
            var data=new SerializedObject(feature);data.FindProperty("m_Settings.Downsample").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    static void BuildWindows()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
        {File.WriteAllText("Logs/JuryWindowsBuild.txt","UNAVAILABLE: Windows64 build module is missing.");return;}
        Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MainMenu.unity",IslandSceneSetup.MainScenePath},locationPathName="Builds/Windows/CivilGameJam.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Logs/JuryWindowsBuild.txt",report.summary.result+" · "+report.summary.totalErrors+" errors · "+report.summary.totalSize+" bytes · "+report.summary.totalTime);
    }
}
