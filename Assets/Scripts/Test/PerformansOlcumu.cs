using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Performans ölçümü: Play modunda farklı durumları sırayla ölçer, Logs/PerformansOlcumu.txt'ye yazar.
// Her şüpheli (deniz animasyonu, gölge, SSAO) geçici olarak kapatılıp maliyeti ayrı ayrı görülür; sonunda hepsi geri açılır.
// Editörde ölçüldüğü için mutlak değerler build'den yüksektir; karşılaştırma için kullanılır.
public class PerformansOlcumu : MonoBehaviour
{
    public const string LogYolu = "Logs/PerformansOlcumu.txt";
    const float Isinma = 1.5f, Sure = 6f;

    readonly StringBuilder log = new StringBuilder();
    ProfilerRecorder anaThread, gcAlloc, drawCalls, batches, setPass, ucgen, kullanilanBellek, gcBellek;

    void OnEnable()
    {
        anaThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 15);
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        ucgen = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        kullanilanBellek = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        gcBellek = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
    }

    void OnDisable()
    {
        anaThread.Dispose(); gcAlloc.Dispose(); drawCalls.Dispose(); batches.Dispose();
        setPass.Dispose(); ucgen.Dispose(); kullanilanBellek.Dispose(); gcBellek.Dispose();
    }

    void Start()
    {
        Application.runInBackground = true;
        StartCoroutine(Calistir());
    }

    IEnumerator Calistir()
    {
        log.AppendLine("=== PERFORMANS ÖLÇÜMÜ " + DateTime.Now + " · " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " · Editör (Game görünümü) ===");
        log.AppendLine("Kolonlar: ort/p95/max kare süresi (ms) · ana thread ort (ms) · GC alloc/kare (KB) · draw call · batch · setpass · üçgen (bin)");
        yield return new WaitForSecondsRealtime(1f);

        var gm = GameManager.Instance;
        if (gm != null) { gm.hizCarpani = 3; gm.kullaniciDurdurdu = false; }

        yield return Olc("1) Normal oyun (3x)");

        // Deniz/dalga vertex animasyonu
        var animatorler = FindObjectsByType<LowPolyDenizVeDalgaAnimator>();
        var acikAnimatorler = new List<Behaviour>();
        foreach (var a in animatorler) if (a.isActiveAndEnabled) { a.enabled = false; acikAnimatorler.Add(a); }
        yield return Olc("2) Deniz animasyonu KAPALI (" + acikAnimatorler.Count + " açık animatör vardı)");
        foreach (var a in acikAnimatorler) a.enabled = true;

        // Gölgeler
        var isiklar = FindObjectsByType<Light>();
        var golgeler = new Dictionary<Light, LightShadows>();
        foreach (var l in isiklar) { golgeler[l] = l.shadows; l.shadows = LightShadows.None; }
        yield return Olc("3) Gölgeler KAPALI (" + isiklar.Length + " ışık)");
        foreach (var kv in golgeler) kv.Key.shadows = kv.Value;

        // SSAO (URP renderer feature)
        var ssao = SsaoBul();
        if (ssao != null && ssao.isActive)
        {
            ssao.SetActive(false);
            yield return Olc("4) SSAO KAPALI");
            ssao.SetActive(true);
        }
        else log.AppendLine("4) SSAO bulunamadı ya da zaten kapalı, ölçülmedi");

        // Bina popup'ı açık
        if (BinaPopup.Instance != null)
        {
            BinaPopup.Instance.Ac(BinaTuru.TemelBina);
            yield return Olc("5) Bina popup'ı AÇIK");
            yield return new WaitForSecondsRealtime(0.3f);
            BinaPopup.Instance.Kapat();
        }

        // Yangın afeti sırasında
        var afet = FindAnyObjectByType<DisasterController>();
        if (afet != null)
        {
            afet.AfetBaslat(0);
            yield return Olc("6) Yangın afeti sırasında");
            while (gm != null && !gm.afetOlduMu) yield return null;
            var rapor = FindAnyObjectByType<AfetRaporu>();
            if (rapor != null) { yield return null; rapor.Devam(); }
        }

        log.AppendLine("\nBellek: toplam kullanılan " + (kullanilanBellek.LastValue / 1048576f).ToString("0") + " MB, GC " + (gcBellek.LastValue / 1048576f).ToString("0") + " MB");
        FrameTimingManager.CaptureFrameTimings();
        var zamanlar = new FrameTiming[1];
        if (FrameTimingManager.GetLatestTimings(1, zamanlar) > 0 && zamanlar[0].gpuFrameTime > 0)
            log.AppendLine("GPU kare süresi (son kare): " + zamanlar[0].gpuFrameTime.ToString("0.00") + " ms");
        else
            log.AppendLine("GPU kare süresi: bu ortamda okunamadı (Frame Timing Stats kapalı ya da editör desteklemiyor)");

        File.WriteAllText(LogYolu, log.ToString());
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator Olc(string ad)
    {
        yield return new WaitForSecondsRealtime(Isinma);
        var kareler = new List<float>();
        double thread = 0, gc = 0, dc = 0, bt = 0, sp = 0, tri = 0;
        int n = 0;
        float bitis = Time.realtimeSinceStartup + Sure;
        while (Time.realtimeSinceStartup < bitis)
        {
            yield return null;
            kareler.Add(Time.unscaledDeltaTime * 1000f);
            thread += anaThread.LastValue / 1e6; gc += gcAlloc.LastValue / 1024.0;
            dc += drawCalls.LastValue; bt += batches.LastValue; sp += setPass.LastValue; tri += ucgen.LastValue;
            n++;
        }
        kareler.Sort();
        float ort = 0; foreach (float k in kareler) ort += k; ort /= Mathf.Max(1, kareler.Count);
        float p95 = kareler[Mathf.Clamp((int)(kareler.Count * 0.95f), 0, kareler.Count - 1)];
        float max = kareler[kareler.Count - 1];
        log.AppendLine(string.Format("{0,-52} {1,6:0.00} / {2,6:0.00} / {3,6:0.00} ms · thread {4,6:0.00} · GC {5,6:0.0} KB · dc {6,5:0} · batch {7,5:0} · setpass {8,4:0} · tri {9,6:0.0}k  ({10} kare, ~{11:0} FPS)",
            ad, ort, p95, max, thread / n, gc / n, dc / n, bt / n, sp / n, tri / n / 1000.0, n, 1000f / ort));
    }

    static ScriptableRendererFeature SsaoBul()
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) return null;
        var alan = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
        var liste = alan != null ? alan.GetValue(asset) as ScriptableRendererData[] : null;
        if (liste == null) return null;
        foreach (var veri in liste)
        {
            if (veri == null) continue;
            foreach (var ozellik in veri.rendererFeatures)
                if (ozellik != null && ozellik.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")) return ozellik;
        }
        return null;
    }
}
