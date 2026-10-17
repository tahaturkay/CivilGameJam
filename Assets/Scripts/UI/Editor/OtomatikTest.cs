using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Araclar > Otomatik Oyun Testi: Scene_Test'i açar, Play'e girer ve OyunTestRobotu'nu çalıştırır.
// Sonuç: Logs/OyunTesti.txt
// Temp/otomatik_test_baslat dosyası oluşturulursa test kendiliğinden başlar (Claude bu yolla çalıştırıyor).
[InitializeOnLoad]
static class OtomatikTest
{
    const string TestSahnesi = "Assets/Scenes/Scene_Test.unity";
    const string BaslatDosyasi = "Temp/otomatik_test_baslat";
    const string AktifAnahtari = "OtomatikTest.Aktif";
    const string OlcumAnahtari = "OtomatikTest.Olcum";
    static double sonKontrol;

    static OtomatikTest()
    {
        EditorApplication.update += Guncelle;
        EditorApplication.playModeStateChanged += PlayDegisti;
    }

    [MenuItem("Araclar/Otomatik Oyun Testi")]
    public static void Baslat() { Baslat(TestSahnesi); }

    // Başlatma dosyasının içine sahne yolu yazılırsa test o sahnede koşar.
    // "olcum:" ile başlarsa test yerine performans ölçümü yapılır (Logs/PerformansOlcumu.txt).
    public static void Baslat(string sahne)
    {
        if (EditorApplication.isPlaying) return;
        bool olcum = sahne != null && sahne.Trim().StartsWith("olcum:");
        if (olcum) sahne = sahne.Trim().Substring("olcum:".Length);
        SessionState.SetBool(OlcumAnahtari, olcum);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.OpenScene(string.IsNullOrWhiteSpace(sahne) ? TestSahnesi : sahne.Trim());
        SessionState.SetBool(AktifAnahtari, true);
        EditorApplication.isPlaying = true;
    }

    static void Guncelle()
    {
        if (EditorApplication.timeSinceStartup - sonKontrol < 1.0) return;
        sonKontrol = EditorApplication.timeSinceStartup;

        if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && File.Exists(BaslatDosyasi))
        {
            // Önce değişen script'ler derlensin; derleme başlarsa dosya kalır, yeniden yüklenince test başlar
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string sahne = File.ReadAllText(BaslatDosyasi);
            File.Delete(BaslatDosyasi);
            Baslat(sahne);
        }
    }

    static void PlayDegisti(PlayModeStateChange durum)
    {
        if (!SessionState.GetBool(AktifAnahtari, false)) return;

        if (durum == PlayModeStateChange.EnteredPlayMode)
        {
            if (SessionState.GetBool(OlcumAnahtari, false)) new GameObject("PerformansOlcumu").AddComponent<PerformansOlcumu>();
            else new GameObject("OyunTestRobotu").AddComponent<OyunTestRobotu>();
        }
        else if (durum == PlayModeStateChange.EnteredEditMode)
            SessionState.SetBool(AktifAnahtari, false);
    }
}
