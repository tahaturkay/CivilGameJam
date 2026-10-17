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
        Ensure<UIManager>(scene, "Assets/Prefabs/UI/Ana Canvas.prefab");
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
        disaster.tsunamiSuYuksekligi = groundY + 1.7f;
        disaster.selSuYuksekligi = groundY + 2.3f;

        // Önceki haritanın ölçeğindeki su/dalga modellerini kapat; efekt mantığını aynen kullan.
        if (disaster.tsunamiSuyu != null) disaster.tsunamiSuyu.gameObject.SetActive(false);
        if (disaster.tsunamiDalgasi != null) disaster.tsunamiDalgasi.gameObject.SetActive(false);
        Transform oldEffects = disaster.transform.Find("AdaEfektleri");
        if (oldEffects != null) UnityEngine.Object.DestroyImmediate(oldEffects.gameObject);
        GameObject effects = new GameObject("AdaEfektleri");
        effects.transform.SetParent(disaster.transform, false);

        GameObject flood = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flood.name = "SelVeTsunamiSuyu";
        flood.transform.SetParent(effects.transform, false);
        flood.transform.position = new Vector3(0, -4f, 0);
        flood.transform.localScale = new Vector3(land.size.x + 24, .2f, land.size.z + 24);
        UnityEngine.Object.DestroyImmediate(flood.GetComponent<Collider>());
        Renderer sea = island.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "Deniz");
        if (sea != null) flood.GetComponent<Renderer>().sharedMaterial = sea.sharedMaterial;
        flood.layer = 2; // Ignore Raycast: su bina etkileşimini yakalamasın.
        flood.SetActive(false);
        disaster.tsunamiSuyu = flood.transform;

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
        wave.SetActive(false);
        disaster.tsunamiDalgasi = wave.transform;

        if (disaster.yagmurEfekti != null)
        {
            disaster.yagmurEfekti.transform.position = new Vector3(land.center.x, groundY + 25, land.center.z);
            foreach (ParticleSystem particles in disaster.yagmurEfekti.GetComponentsInChildren<ParticleSystem>(true))
            {
                var shape = particles.shape;
                shape.scale = new Vector3(land.size.x / 3, land.size.z / 3, 1);
            }
            disaster.yagmurEfekti.SetActive(false);
        }
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
