using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Adanın ışık, gölge ve su sunumu; oyun durumundan bağımsız editör ayarları.</summary>
public static class IslandPresentation
{
    public static void Configure(Scene scene, GameObject island, Camera camera)
    {
        Light sun = IslandSceneSetup.Components<Light>(scene).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            sun.color = new Color(1f, .94f, .83f);
            sun.useColorTemperature = false;
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .88f;
            sun.shadowBias = .025f;
            sun.shadowNormalBias = .20f;
            RenderSettings.sun = sun;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.42f, .55f, .63f);
        RenderSettings.ambientEquatorColor = new Color(.34f, .40f, .36f);
        RenderSettings.ambientGroundColor = new Color(.18f, .22f, .19f);
        RenderSettings.ambientIntensity = 1;
        RenderSettings.fog = false;

        // The overview camera is hundreds of metres away; a 40–50 m default cuts all city shadows.
        ConfigurePipeline("Assets/Settings/PC_RPAsset.asset", 450, 4096);
        ConfigurePipeline("Assets/Settings/Mobile_RPAsset.asset", 420, 2048);
        ConfigureAO();
        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        camera.allowHDR = true;

        const string profilePath = "Assets/Settings/IslandVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);
        if (!profile.TryGet(out ColorAdjustments colors)) colors = profile.Add<ColorAdjustments>(true);
        colors.postExposure.Override(.12f);
        colors.contrast.Override(8);
        colors.saturation.Override(8);
        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.2f);
        bloom.intensity.Override(.08f);
        foreach (VolumeComponent component in profile.components)
        {
            if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
        }
        Volume volume = IslandSceneSetup.Components<Volume>(scene).FirstOrDefault(v => v.isGlobal);
        if (volume != null) { volume.sharedProfile = profile; volume.priority = 5; }
        EditorUtility.SetDirty(profile);

        ConfigureOcean(island);
    }

    public static void ConfigureOcean(GameObject island)
    {
        Shader shader = Shader.Find("CivilIsland/Stylized Ocean");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new System.InvalidOperationException("Ada deniz shader'ı derlenemedi.");
        const string materialPath = "Assets/Materials/LowPolyAda/Island_Ocean.mat";
        Material ocean = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (ocean == null) { ocean = new Material(shader); AssetDatabase.CreateAsset(ocean, materialPath); }
        ocean.shader = shader;
        ocean.SetColor("_DeepColor", new Color(.018f, .17f, .26f));
        ocean.SetColor("_ShallowColor", new Color(.025f, .38f, .45f));
        ocean.SetColor("_GlintColor", new Color(.35f, .73f, .77f));
        ocean.SetFloat("_WaveScale", .16f);
        ocean.SetFloat("_Speed", .36f);
        Renderer sea = island.GetComponentsInChildren<Renderer>().First(r => r.name == "Deniz");
        sea.transform.localScale = new Vector3(800, sea.transform.localScale.y, 800);
        sea.sharedMaterial = ocean;
        sea.shadowCastingMode = ShadowCastingMode.Off;
        // This is an instance override; both scenes retain the same generated art prefab.
        if (PrefabUtility.IsPartOfPrefabInstance(sea))
            PrefabUtility.RecordPrefabInstancePropertyModifications(sea);
        EditorUtility.SetDirty(ocean);
    }

    static void ConfigurePipeline(string path, float distance, int resolution)
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (pipeline == null) return;
        var data = new SerializedObject(pipeline);
        data.FindProperty("m_ShadowDistance").floatValue = distance;
        data.FindProperty("m_MainLightShadowmapResolution").intValue = resolution;
        data.FindProperty("m_ShadowCascadeCount").intValue = 4;
        data.FindProperty("m_Cascade4Split").vector3Value = new Vector3(.15f, .36f, .63f);
        data.FindProperty("m_ShadowNormalBias").floatValue = .20f;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureAO()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if (renderer == null) return;
        foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
        {
            if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
            var data = new SerializedObject(feature);
            data.FindProperty("m_Settings.Intensity").floatValue = .55f;
            data.FindProperty("m_Settings.Radius").floatValue = .9f;
            data.FindProperty("m_Settings.Falloff").floatValue = 420;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
