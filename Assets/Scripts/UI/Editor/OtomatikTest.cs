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
    static double sonKontrol;

    static OtomatikTest()
    {
        EditorApplication.update += Guncelle;
        EditorApplication.playModeStateChanged += PlayDegisti;
    }

    [MenuItem("Araclar/Otomatik Oyun Testi")]
    public static void Baslat()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.OpenScene(TestSahnesi);
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

            File.Delete(BaslatDosyasi);
            Baslat();
        }
    }

    static void PlayDegisti(PlayModeStateChange durum)
    {
        if (!SessionState.GetBool(AktifAnahtari, false)) return;

        if (durum == PlayModeStateChange.EnteredPlayMode)
            new GameObject("OyunTestRobotu").AddComponent<OyunTestRobotu>();
        else if (durum == PlayModeStateChange.EnteredEditMode)
            SessionState.SetBool(AktifAnahtari, false);
    }
}
