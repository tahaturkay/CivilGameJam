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
    const string FotoAnahtari = "OtomatikTest.Foto";
    const string TrailerAnahtari = "OtomatikTest.Trailer"; // "", "tam" ya da "onizleme"
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
        // "foto:" ile başlarsa sahne açılır, birkaç saniye sonra ekran görüntüsü alınır (Logs/Ekran_<sahne>.png)
        bool foto = sahne != null && sahne.Trim().StartsWith("foto:");
        if (foto) sahne = sahne.Trim().Substring("foto:".Length);
        SessionState.SetBool(FotoAnahtari, foto);
        // "trailer:" tam çekim (her kare + ses), "trailer-onizleme:" saniyede bir kare (Logs/TrailerKareler)
        string trailer = "";
        if (sahne != null && sahne.Trim().StartsWith("trailer-onizleme:")) { trailer = "onizleme"; sahne = sahne.Trim().Substring("trailer-onizleme:".Length); }
        else if (sahne != null && sahne.Trim().StartsWith("trailer:")) { trailer = "tam"; sahne = sahne.Trim().Substring("trailer:".Length); }
        SessionState.SetString(TrailerAnahtari, trailer);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // An interrupted Play run can leave in-memory ScriptableObject test values behind.
        AssetDatabase.ImportAsset("Assets/Resources/DengeAyarlari.asset", ImportAssetOptions.ForceUpdate);
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
            string trailer = SessionState.GetString(TrailerAnahtari, "");
            if (trailer != "") { TrailerRobotu.Onizleme = trailer == "onizleme"; new GameObject("TrailerRobotu").AddComponent<TrailerRobotu>(); }
            else if (SessionState.GetBool(FotoAnahtari, false)) new GameObject("EkranGoruntusu").AddComponent<EkranGoruntusu>();
            else if (SessionState.GetBool(OlcumAnahtari, false)) new GameObject("PerformansOlcumu").AddComponent<PerformansOlcumu>();
            else new GameObject("OyunTestRobotu").AddComponent<OyunTestRobotu>();
        }
        else if (durum == PlayModeStateChange.EnteredEditMode)
            SessionState.SetBool(AktifAnahtari, false);
    }
}
