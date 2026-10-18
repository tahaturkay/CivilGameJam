using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Mevcut prefab ve oyun yöneticilerini aynı ada düzenine bağlayan editör kurulumu.</summary>
public static class IslandSceneSetup
{
    public const string MainScenePath = "Assets/Scenes/MainScene.unity";
    public const string MapScenePath = "Assets/Scenes/Scene_Harita.unity";
    public const string IslandPrefabPath = "Assets/Prefabs/Salih/LowPolyAda.prefab";

    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Ada kurulumu için Play modundan çıkın.");

        EnsureParticleShader();
        // Sahne değiştirirken editördeki mevcut çalışmayı koru.
        EditorSceneManager.SaveOpenScenes();
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject island;
            AssetDatabase.StartAssetEditing();
            try
            {
                island = IslandLayoutInstaller.BuildIsland(preview);
                IslandPresentation.ConfigureOcean(island);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            PrefabUtility.SaveAsPrefabAsset(island, IslandPrefabPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        InstallScene(MapScenePath);
        InstallScene(MainScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Ada kuruldu: MainScene ve Scene_Harita; ortak LowPolyAda prefabı, ekonomi, afetler ve UI bağlı.");
    }

    // A Resources reference keeps the procedurally used particle shader in player builds.
    static void EnsureParticleShader()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/IslandEffects")) AssetDatabase.CreateFolder("Assets/Resources", "IslandEffects");
        const string path = "Assets/Resources/IslandEffects/ParticleShader.mat";
        Material template = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (template == null) AssetDatabase.CreateAsset(new Material(Shader.Find("CivilIsland/Soft Particle")), path);
        else { template.shader = Shader.Find("CivilIsland/Soft Particle"); EditorUtility.SetDirty(template); }
        const string glowPath="Assets/Resources/IslandEffects/NightGlow.mat";
        Material glow=AssetDatabase.LoadAssetAtPath<Material>(glowPath);
        if(glow==null){glow=new Material(Shader.Find("CivilIsland/Night Glow")){enableInstancing=true};AssetDatabase.CreateAsset(glow,glowPath);}
        const string emissionPath="Assets/Resources/IslandEffects/NightEmission.mat";
        Material emission=AssetDatabase.LoadAssetAtPath<Material>(emissionPath);
        if(emission==null){emission=new Material(Shader.Find("Universal Render Pipeline/Lit")){enableInstancing=true};AssetDatabase.CreateAsset(emission,emissionPath);}
        emission.EnableKeyword("_EMISSION");emission.SetColor("_EmissionColor",new Color(1,.61f,.22f));EditorUtility.SetDirty(emission);
        const string foamPath="Assets/Resources/IslandEffects/CoastFoam.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(foamPath)==null)AssetDatabase.CreateAsset(new Material(Shader.Find("CivilIsland/Coastal Foam")),foamPath);
    }

    static void InstallScene(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "LowPolyAda" || root.name == "AdaHaritasi" || root.name == "LowPolyAda_Extension")
                UnityEngine.Object.DestroyImmediate(root);

        GameObject island = InstantiatePrefab(IslandPrefabPath, scene);
        GameManager gm = Ensure<GameManager>(scene, "Assets/Prefabs/GameManager.prefab");
        Ensure<EconomyAndTime>(scene, "Assets/Prefabs/Nisa/EconomyAndTime.prefab");
        var ui=Ensure<UIManager>(scene, "Assets/Prefabs/UI/Ana Canvas.prefab");
        if(ui.GetComponent<DereYatagiKarari>()==null)ui.gameObject.AddComponent<DereYatagiKarari>();
        DisasterController disaster = Ensure<DisasterController>(scene, "Assets/Prefabs/Taha/DisasterController.prefab");
        foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
        {
            gm.LevelAyarla(type, 1);
            gm.HasarliAyarla(type, false);
        }
        EditorUtility.SetDirty(gm);

        Camera camera = Components<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (camera == null)
        {
            GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.tag = "MainCamera";
            camera = go.GetComponent<Camera>();
        }
        IslandLayoutInstaller.FrameIsland(camera);
        if(camera.GetComponent<IslandPerformanceBudget>()==null)camera.gameObject.AddComponent<IslandPerformanceBudget>();
        IslandPresentation.Configure(scene, island, camera);
        BindDisasters(disaster, island, camera);
        Bounds land = LandBounds(island);
        Bounds viewBounds = land;
        foreach (Renderer renderer in island.GetComponentsInChildren<Renderer>())
            if (renderer.name != "Deniz") viewBounds.Encapsulate(renderer.bounds);
        IslandCameraController navigation = camera.GetComponent<IslandCameraController>();
        if (navigation == null) navigation = camera.gameObject.AddComponent<IslandCameraController>();
        navigation.disasterController = disaster;
        navigation.Configure(new Vector3(viewBounds.center.x, land.max.y, viewBounds.center.z),
            new Vector2(viewBounds.extents.x + 3, viewBounds.extents.z + 3), 18, 130, camera.orthographicSize);
        navigation.startOrthographicSize=58;
        navigation.FrameCity();
        IslandNightLighting night=island.GetComponent<IslandNightLighting>();
        if(night==null)night=island.AddComponent<IslandNightLighting>();
        if(island.GetComponent<IslandSirenController>()==null)island.AddComponent<IslandSirenController>();
        Texture2D chosenCursor=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Cursors/FenerOku.png");
        if(chosenCursor!=null)
        {
            IslandCursor cursor=camera.GetComponent<IslandCursor>();if(cursor==null)cursor=camera.gameObject.AddComponent<IslandCursor>();
            cursor.pointer=chosenCursor;cursor.hotspot=new Vector2(3,3);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static T Ensure<T>(Scene scene, string prefabPath) where T : Component
    {
        T existing = Components<T>(scene).FirstOrDefault();
        return existing != null ? existing : InstantiatePrefab(prefabPath, scene).GetComponentInChildren<T>(true);
    }

    static GameObject InstantiatePrefab(string path, Scene scene)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidOperationException("Prefab bulunamadı: " + path);
        return (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
    }

    public static IEnumerable<T> Components<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    static void BindDisasters(DisasterController disaster, GameObject island, Camera camera)
    {
        Bounds land = LandBounds(island);
        float groundY = land.max.y;
        BuildingVisuals[] buildings = island.GetComponentsInChildren<BuildingVisuals>(true);
        disaster.kamera = camera.transform;
        disaster.temelBinalar = buildings.Where(b => b.binaTuru == BinaTuru.TemelBina).Select(b => b.transform).ToArray();
        disaster.yanacakBinalar = buildings.Where(b => b.binaTuru == BinaTuru.TemelBina || b.binaTuru == BinaTuru.Okul)
            .Select(b => b.transform).ToArray();
        disaster.okulObj = buildings.First(b => b.binaTuru == BinaTuru.Okul).transform;
        disaster.akutObj = buildings.First(b => b.binaTuru == BinaTuru.Akut).transform;
        disaster.itfaiyeObj = buildings.First(b => b.binaTuru == BinaTuru.Itfaiye).transform;
        disaster.tsunamiSetZ = buildings.Where(b => b.binaTuru == BinaTuru.DenizSeti).Min(b => b.transform.position.z);
        disaster.tsunamiBaslangicZ = land.min.z - 28;
        disaster.tsunamiBitisZ = land.max.z + 30;
        disaster.tsunamiDalgaY = groundY - .6f;
        disaster.kuruZeminY = groundY;
        disaster.afetSuresi = 8;
        disaster.tsunamiSuYuksekligi = groundY + .62f;
        disaster.selSuYuksekligi = groundY + .30f;

        // Önceki haritanın ölçeğindeki su/dalga modellerini kapat; efekt mantığını aynen kullan.
        if (disaster.tsunamiSuyu != null) disaster.tsunamiSuyu.gameObject.SetActive(false);
        if (disaster.tsunamiDalgasi != null) disaster.tsunamiDalgasi.gameObject.SetActive(false);
        Transform oldEffects = disaster.transform.Find("AdaEfektleri");
        if (oldEffects != null) UnityEngine.Object.DestroyImmediate(oldEffects.gameObject);
        GameObject effects = new GameObject("AdaEfektleri");
        effects.transform.SetParent(disaster.transform, false);

        GameObject flood = new GameObject("SelVeTsunamiSuyu", typeof(MeshFilter), typeof(MeshRenderer));
        flood.transform.SetParent(effects.transform, false);
        flood.transform.position = new Vector3(0, groundY - .3f, 0);
        Mesh source = island.transform.Find("Arazi/Ada_OtlakYuzey").GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = source.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(vertices[i].x * .938f, 0, vertices[i].z * .938f);
        Mesh waterMesh = new Mesh { name = "Ada_IcSelSiniri" }; waterMesh.vertices = vertices;
        waterMesh.triangles = source.triangles; waterMesh.RecalculateNormals(); waterMesh.RecalculateBounds();
        string meshPath = "Assets/Models/LowPolyAda/Ada_IcSelSiniri.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null) { AssetDatabase.CreateAsset(waterMesh, meshPath); existing = waterMesh; }
        else { existing.Clear(); existing.vertices = waterMesh.vertices; existing.triangles = waterMesh.triangles; existing.RecalculateNormals(); existing.RecalculateBounds(); UnityEngine.Object.DestroyImmediate(waterMesh); EditorUtility.SetDirty(existing); }
        flood.GetComponent<MeshFilter>().sharedMesh = existing;
        string materialPath = "Assets/Materials/LowPolyAda/Ada_SaydamSelSuyu.mat";
        Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (waterMaterial == null) { waterMaterial = new Material(Shader.Find("CivilIsland/Transparent Interior Flood")); AssetDatabase.CreateAsset(waterMaterial, materialPath); }
        waterMaterial.SetColor("_BaseColor", new Color(.34f,.55f,.62f,1));
        flood.GetComponent<MeshRenderer>().sharedMaterial = waterMaterial;
        flood.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flood.layer = 2;
        flood.SetActive(false); disaster.tsunamiSuyu = flood.transform;
        Renderer sea = island.GetComponentsInChildren<Renderer>().First(r => r.name == "Deniz");

        GameObject wave = new GameObject("TsunamiDalgasi");
        wave.transform.SetParent(effects.transform, false);
        wave.transform.position = new Vector3(land.center.x, disaster.tsunamiDalgaY, disaster.tsunamiBaslangicZ);
        GameObject waveAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Taha/LowPolyDalga.prefab");
        if (waveAsset == null) throw new InvalidOperationException("LowPolyDalga prefabı bulunamadı.");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(waveAsset, disaster.gameObject.scene);
        model.transform.SetParent(wave.transform, false);
        // Efekt modelinde tıklamayı engelleyen fizik bileşeni olmamalı.
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Bounds bounds = RendererBounds(model);
        model.transform.localScale = Vector3.Scale(model.transform.localScale,
            new Vector3((land.size.x + 6) / Mathf.Max(bounds.size.x, .01f), 6f / Mathf.Max(bounds.size.y, .01f), 10f / Mathf.Max(bounds.size.z, .01f)));
        bounds = RendererBounds(model);
        model.transform.position += wave.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials = Enumerable.Repeat(sea.sharedMaterial, renderer.sharedMaterials.Length).ToArray();
        wave.SetActive(false);
        disaster.tsunamiDalgasi = wave.transform;

        if (disaster.yagmurEfekti != null) disaster.yagmurEfekti.SetActive(false);
        disaster.yagmurEfekti = null;
        IslandWeather weather = disaster.GetComponent<IslandWeather>();
        if (weather == null) weather = disaster.gameObject.AddComponent<IslandWeather>();
        weather.Configure(disaster, land);
        IslandGroundQuake quake=disaster.GetComponent<IslandGroundQuake>();
        if(quake==null)quake=disaster.gameObject.AddComponent<IslandGroundQuake>();
        quake.disaster=disaster;quake.land=land;
        IslandCityLife city = island.GetComponent<IslandCityLife>();
        if (city == null) city = island.AddComponent<IslandCityLife>();
        city.citizenCount=95;city.animalCount=5;
        Transform beach = island.transform.Find("SabitAlanlar/Kumsal/ToplanmaNoktasi");
        if (beach == null) { beach = new GameObject("ToplanmaNoktasi").transform; beach.SetParent(island.transform.Find("SabitAlanlar/Kumsal"), false); beach.position = new Vector3(-10,groundY+.35f,-58); }
        city.Configure(disaster, land, buildings.Where(b => b.binaTuru == BinaTuru.TemelBina).ToArray(),
            new[] { island.transform.Find("SabitAlanlar/Park"), island.transform.Find("SabitAlanlar/Otopark"), beach });
        const string vehiclePath = "Assets/Assets/Salih/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/";
        city.vehiclePrefabs = Enumerable.Range(1,3).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>(vehiclePath+"Vehicle_Car_color0"+i+".prefab")).ToArray();
        PrefabUtility.RecordPrefabInstancePropertyModifications(disaster);
        EditorUtility.SetDirty(disaster);
    }

    public static Bounds LandBounds(GameObject island)
    {
        return island.transform.Find("Arazi/Ada_OtlakYuzey").GetComponent<Renderer>().bounds;
    }

    static Bounds RendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Efekt modelinde renderer yok: " + go.name);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
