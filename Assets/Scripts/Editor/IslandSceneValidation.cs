using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Ada kurulumunun bağlantılarını ve gerçek Play modundaki seviye/ekonomi akışını doğrular.</summary>
[InitializeOnLoad]
public static class IslandSceneValidation
{
    const string RequestPath = "Temp/ada_kurulum_istegi";
    const string PlayKey = "AdaTesti.Aktif";
    const string LogPath = "Logs/AdaDogrulama.txt";
    static readonly StringBuilder Report = new StringBuilder();
    static int phase;
    static double nextCheck;
    static double playStarted;
    static readonly List<string> RuntimeErrors = new List<string>();
    static readonly Dictionary<BuildingVisuals, float> LevelOneHeights = new Dictionary<BuildingVisuals, float>();

    static IslandSceneValidation()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayChanged;
    }

    [MenuItem("Oyun/Ada Haritası/Validate Island")]
    public static void ValidateInstalledScenes()
    {
        Report.Clear();
        foreach (string path in new[] { IslandSceneSetup.MapScenePath, IslandSceneSetup.MainScenePath })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ValidateScene(scene);
        }
        WriteReport("PASS: İki sahnenin ada hiyerarşisi ve bağlantıları doğrulandı.");
    }

    [MenuItem("Oyun/Ada Haritası/Test Island In Play Mode")]
    public static void StartPlayTests()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
        SessionState.SetBool(PlayKey, true);
        EditorApplication.isPlaying = true;
    }

    static void Update()
    {
        if (SessionState.GetBool(PlayKey, false) && EditorApplication.isPlaying && phase > 0)
        {
            if (EditorApplication.timeSinceStartup - playStarted < .75) return;
            try { PlayStep(); }
            catch (Exception ex) { FinishPlay("FAIL: " + ex); }
            return;
        }

        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(RequestPath)) return;
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string request = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        try
        {
            if (request == "install") { IslandSceneSetup.Install(); ValidateInstalledScenes(); }
            else if (request == "validate") ValidateInstalledScenes();
            else if (request == "test") StartPlayTests();
            else if (request == "preview") Capture("Logs/LowPolyAda_Level1.png");
            File.WriteAllText("Logs/AdaKurulumSonuc.txt", "OK: " + request);
        }
        catch (Exception ex)
        {
            File.WriteAllText("Logs/AdaKurulumSonuc.txt", "FAIL: " + ex);
            Debug.LogException(ex);
        }
    }

    static void ValidateScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        GameObject island = roots.Single(r => r.name == "LowPolyAda");
        foreach (string group in new[] { "Arazi", "Yollar", "YukseltilebilirYapilar", "SabitAlanlar", "DenizSeti" })
            Check(island.transform.Find(group) != null, scene.name + ": " + group + " grubu");
        foreach (string name in new[] { "Belediye", "Park", "Kumsal", "Lunapark", "Otopark", "Orman", "Tepe" })
        {
            Transform place = island.transform.Find("SabitAlanlar/" + name);
            Check(place != null, "Sabit alan: " + name);
            Check(place.GetComponentsInChildren<BuildingVisuals>(true).Length == 0, name + ": seviye bileşeni yok");
        }
        BuildingVisuals[] buildings = island.GetComponentsInChildren<BuildingVisuals>(true);
        Bounds land = IslandSceneSetup.LandBounds(island);
        Check(land.size.x > 160 && land.size.z > 125, "Ada geniş alanı kapsıyor: " + land.size);
        Check(buildings.Count(b => b.binaTuru == BinaTuru.TemelBina) >= 35, "En az 35 yükseltilebilir konut");
        foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
            Check(buildings.Any(b => b.binaTuru == type), "Mevcut yapı türü bağlı: " + type);
        foreach (BuildingVisuals building in buildings)
        {
            Check(building.levelModelleri != null && building.levelModelleri.Length == 3 && building.levelModelleri.All(m => m != null), building.name + ": 3 model bağlı");
            Check(building.levelModelleri[0].activeSelf && !building.levelModelleri[1].activeSelf && !building.levelModelleri[2].activeSelf, building.name + ": başlangıç görseli Lv1");
            Check(building.GetComponent<BoxCollider>() != null, building.name + ": kök tıklama collider'ı");
        }
        Check(DengeAyarlari.Aktif.maxLevel == 3, "Seviye üst sınırı 3");
        Check(IslandSceneSetup.Components<GameManager>(scene).Count() == 1, "Tek GameManager");
        Check(IslandSceneSetup.Components<EconomyAndTime>(scene).Count() == 1, "Tek EconomyAndTime");
        Check(IslandSceneSetup.Components<BinaPopup>(scene).Count() == 1, "Mevcut BinaPopup bağlı");
        Check(IslandSceneSetup.Components<IslandCameraController>(scene).Count() == 1, "Ada yakınlaştırma/gezinme kamerası bağlı");
        DisasterController disaster = IslandSceneSetup.Components<DisasterController>(scene).Single();
        Check(disaster.kamera != null && disaster.tsunamiSuyu != null && disaster.tsunamiDalgasi != null && disaster.yanginPrefab != null && disaster.tozBulutuPrefab != null, "Afet efektleri ve kamera bağlı");
        Check(disaster.okulObj != null && disaster.akutObj != null && disaster.itfaiyeObj != null, "Afet hizmet binaları bağlı");
        Check(disaster.temelBinalar.Length == buildings.Count(b => b.binaTuru == BinaTuru.TemelBina) && disaster.temelBinalar.All(t => t != null && t.IsChildOf(island.transform)), "Deprem konut referansları yeni adaya bağlı");
        Check(disaster.yanacakBinalar.Length > 0 && disaster.yanacakBinalar.All(t => t != null && t.IsChildOf(island.transform)), "Yangın hedefleri yeni adaya bağlı");
        Check(disaster.tsunamiBaslangicZ < land.min.z && disaster.tsunamiBitisZ > land.max.z && disaster.tsunamiSuYuksekligi > land.max.y,
            "Afet dalgası ve su seviyesi geniş adanın boyutuna uygun");
        foreach (Transform t in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Eksik script: " + t.name);
        Report.AppendLine(scene.name + ": " + buildings.Length + " yükseltilebilir nesne; eksik script yok.");
    }

    static void PlayChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(PlayKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 1;
            playStarted = EditorApplication.timeSinceStartup;
            Report.Clear();
            LevelOneHeights.Clear();
            RuntimeErrors.Clear();
            Application.logMessageReceived += RuntimeLog;
            Application.runInBackground = true;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(PlayKey, false);
            Application.logMessageReceived -= RuntimeLog;
            phase = 0;
        }
    }

    static void PlayStep()
    {
        GameManager gm = GameManager.Instance;
        Check(gm != null, "Play: GameManager aktif");
        gm.kullaniciDurdurdu = true;
        BuildingVisuals[] buildings = UnityEngine.Object.FindObjectsByType<BuildingVisuals>();
        if (phase == 1)
        {
            Camera camera = Camera.main;
            IslandCameraController navigation = camera.GetComponent<IslandCameraController>();
            camera.transform.position += new Vector3(18, 0, -12);
            camera.orthographicSize = navigation.minOrthographicSize;
            navigation.FrameHome();
            Vector3 viewport = camera.WorldToViewportPoint(navigation.boundsCenter);
            Check(Mathf.Abs(viewport.x - .5f) < .01f && Mathf.Abs(viewport.y - .5f) < .01f &&
                camera.orthographicSize >= navigation.homeOrthographicSize, "Kamera gezinmeden sonra tam ada görünümüne dönebiliyor");
            foreach (BuildingVisuals b in buildings)
            {
                Check(gm.Level(b.binaTuru) == 1 && b.levelModelleri[0].activeInHierarchy, b.name + ": Play Lv1");
                LevelOneHeights[b] = ActiveBounds(b).size.y;
            }
            Capture("Logs/LowPolyAda_Level1.png");
            UnityEngine.Object.FindAnyObjectByType<DisasterController>().tsunamiDalgasi.gameObject.SetActive(true);
            gm.para = 50000;
            foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru))) Check(Ekonomi.Yukselt(type), type + ": ekonomi ile 1→2");
            phase = 2;
        }
        else if (phase == 2)
        {
            CheckVisuals(buildings, 2);
            Transform wave = UnityEngine.Object.FindAnyObjectByType<DisasterController>().tsunamiDalgasi;
            LowPolyDenizVeDalgaAnimator animator = wave.GetComponentInChildren<LowPolyDenizVeDalgaAnimator>();
            Check(animator != null && animator.enabled && animator.GetComponent<MeshFilter>().sharedMesh.isReadable,
                "Tsunami modeli CPU dalga animasyonuna uygun ve aktif");
            wave.gameObject.SetActive(false);
            foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru))) Check(Ekonomi.Yukselt(type), type + ": ekonomi ile 2→3");
            phase = 3;
        }
        else if (phase == 3)
        {
            CheckVisuals(buildings, 3);
            foreach (BuildingVisuals b in buildings)
                Check(ActiveBounds(b).size.y > LevelOneHeights[b] + .05f, b.name + ": Lv3 görseli daha yüksek");
            foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
            {
                int money = gm.para;
                Check(!Ekonomi.Yukselt(type) && gm.Level(type) == 3 && gm.para == money, type + ": Lv3 üstü engelli");
            }
            Physics.SyncTransforms();
            GameObject island = GameObject.Find("LowPolyAda");
            Bounds land = IslandSceneSetup.LandBounds(island);
            for (int i = 0; i < 180; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 2, 0) * Vector3.forward;
                Check(Physics.RaycastAll(new Vector3(land.center.x, land.max.y + .7f, land.center.z), direction, land.extents.magnitude * 1.5f)
                    .Any(hit => hit.collider.GetComponent<BuildingVisuals>()?.binaTuru == BinaTuru.DenizSeti), "Set çevresi kapalı: " + i * 2 + "°");
            }
            Capture("Logs/LowPolyAda_Level3.png");
            CaptureDetail("Logs/LowPolyAda_Detail.png", new Vector3(0, land.max.y, -8), 43);
            CaptureDetail("Logs/LowPolyAda_Breakwater.png", new Vector3(-30, land.max.y, land.min.z + 6), 28);
            foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru))) gm.LevelAyarla(type, 2);
            phase = 4;
        }
        else if (phase == 4)
        {
            CheckVisuals(buildings, 2);
            foreach (BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
            {
                gm.LevelAyarla(type, 1);
                gm.HasarliAyarla(type, true);
                Check(!Ekonomi.Yukselt(type), type + ": hasarlı yapı yükseltilemiyor");
                int money = gm.para;
                Check(Ekonomi.Onar(type) && !gm.Hasarli(type) && gm.para == money - Ekonomi.OnarimMaliyeti(type), type + ": mevcut onarım akışı");
            }
            phase = 5;
        }
        else if (phase == 5)
        {
            CheckVisuals(buildings, 1);
            BuildingVisuals school = buildings.First(b => b.binaTuru == BinaTuru.Okul);
            school.SendMessage("OnMouseDown", SendMessageOptions.RequireReceiver);
            Check(BinaPopup.Instance.panel.activeSelf && BinaPopup.Instance.baslikText.text == "OKUL", "OnMouseDown → mevcut okul popup'ı");
            Check(BinaPopup.Instance.quizButonu.gameObject.activeSelf, "Okul quiz düğmesi aktif");
            Check(RuntimeErrors.Count == 0, "Oyun scriptlerinde çalışma zamanı hatası yok: " + string.Join("; ", RuntimeErrors));
            FinishPlay("PASS: Tüm türler 1→2→3, ortak görseller, büyüme, collider, maksimum seviye, gerileme, hasar/onarım, çevre seti ve tıklama/popup doğrulandı.");
        }
    }

    static void CheckVisuals(BuildingVisuals[] buildings, int level)
    {
        foreach (BuildingVisuals b in buildings)
        {
            Check(b.levelModelleri.Select((m, i) => m.activeSelf == (i == level - 1)).All(x => x), b.name + ": yalnız Lv" + level + " modeli aktif");
            Bounds bounds = ActiveBounds(b);
            Bounds collider = b.GetComponent<BoxCollider>().bounds;
            Check(collider.min.y <= bounds.min.y + .02f && collider.max.y >= bounds.max.y - .02f, b.name + ": collider aktif yüksekliği kapsıyor");
        }
    }

    static Bounds ActiveBounds(BuildingVisuals building)
    {
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static void Capture(string path)
    {
        Camera camera = Camera.main;
        RenderTexture target = RenderTexture.GetTemporary(1600, 1000, 24);
        RenderTexture previous = camera.targetTexture;
        RenderTexture active = RenderTexture.active;
        Texture2D image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    static void CaptureDetail(string path, Vector3 focus, float size)
    {
        Camera camera = Camera.main;
        Vector3 position = camera.transform.position;
        float previousSize = camera.orthographicSize;
        Ray ray = camera.ViewportPointToRay(new Vector3(.5f, .5f));
        new Plane(Vector3.up, focus).Raycast(ray, out float distance);
        camera.transform.position += focus - ray.GetPoint(distance);
        camera.orthographicSize = size;
        try { Capture(path); }
        finally { camera.transform.position = position; camera.orthographicSize = previousSize; }
    }

    static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        Report.AppendLine("PASS: " + message);
    }

    static void RuntimeLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && stack.Contains("Assets/Scripts/"))
            RuntimeErrors.Add(message);
    }

    static void WriteReport(string result)
    {
        Directory.CreateDirectory("Logs");
        Report.AppendLine(result);
        File.WriteAllText(LogPath, Report.ToString());
        Debug.Log(result);
    }

    static void FinishPlay(string result)
    {
        WriteReport(result);
        phase = 0;
        EditorApplication.isPlaying = false;
    }
}
