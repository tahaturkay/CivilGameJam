using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

// Trailer çekimi: Play modunda 60 saniyelik çekim listesini oynatır, her kareyi ve oyun içi sesleri kaydeder.
// Görüntü: Logs/TrailerKareler/kare_00000.jpg (30 fps), ses: Logs/TrailerKareler/oyun_sesleri.wav
// Müzik kanalı kayıtta susturulur; Taha'nın parçası sonradan ffmpeg ile kesintisiz eklenir.
// Önizleme modunda saniyede 1 kare kaydedilir, ses kaydedilmez.
public class TrailerRobotu : MonoBehaviour
{
    public static bool Onizleme;
    public const string Klasor = "Logs/TrailerKareler";
    const int Fps = 30;
    const float ToplamSure = 60f;

    // Çekim sınırları (saniye)
    const float S1 = 0f, S2 = 6f, S3 = 13f, S4 = 22f, S5 = 27f, S6 = 34f, S7 = 40f, S8 = 47f, S9 = 54f;

    GameManager gm;
    Camera kam;
    IslandCameraController oyunKamerasi;
    DisasterController afet;
    EconomyAndTime saat;
    FieldInfo gunSayaciAlani;
    AudioSource muzikKaynagi;
    readonly List<Canvas> oyunArayuzu = new List<Canvas>();

    Vector3 adaMerkezi, feneri, set, konut, okul, itfaiye, akut, ornekEv, atesHedefi;
    float tsunamiSetZ = -73f;
    float adaYaricapi;

    // Yazı katmanı
    Canvas yaziKatmani;
    TMP_Text baslik, altYazi, etiket;
    Image karartma;

    readonly List<float> sesOrnekleri = new List<float>();
    int sesKanallari;
    string log = "";

    void Start()
    {
        Application.runInBackground = true;
        StartCoroutine(Calistir());
    }

    IEnumerator Calistir()
    {
        yield return null;
        yield return null;
        Hazirla();

        if (Directory.Exists(Klasor)) Directory.Delete(Klasor, true);
        Directory.CreateDirectory(Klasor);

        Time.captureFramerate = Fps;
        if (!Onizleme) { AudioRenderer.Start(); sesKanallari = KanalSayisi(); }

        int toplamKare = Mathf.RoundToInt(ToplamSure * Fps);
        float sonCekim = -1f;
        for (int kare = 0; kare < toplamKare; kare++)
        {
            float t = kare / (float)Fps;
            float cekim = CekimBaslangici(t);
            if (cekim != sonCekim) { CekimBasladi(cekim); sonCekim = cekim; }
            Guncelle(t);

            yield return new WaitForEndOfFrame();

            if (!Onizleme || kare % Fps == 0)
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(Klasor, "kare_" + kare.ToString("00000") + ".jpg"), tex.EncodeToJPG(92));
                Destroy(tex);
            }
            if (!Onizleme) SesAl();
        }

        if (!Onizleme) { AudioRenderer.Stop(); WavYaz(Path.Combine(Klasor, "oyun_sesleri.wav")); }
        Time.captureFramerate = 0;
        File.WriteAllText(Path.Combine(Klasor, "bilgi.txt"), log + "\nkare: " + toplamKare + " fps: " + Fps + " ses kanalı: " + sesKanallari + " örnekleme: " + AudioSettings.outputSampleRate);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------- Hazırlık ----------

    void Hazirla()
    {
        gm = GameManager.Instance;
        kam = Camera.main;
        oyunKamerasi = FindAnyObjectByType<IslandCameraController>();
        afet = FindAnyObjectByType<DisasterController>();
        saat = FindAnyObjectByType<EconomyAndTime>();
        gunSayaciAlani = typeof(EconomyAndTime).GetField("gunSayaci", BindingFlags.NonPublic | BindingFlags.Instance);
        if (MusicPlayer.Instance != null) muzikKaynagi = MusicPlayer.Instance.GetComponent<AudioSource>();

        foreach (var c in FindObjectsByType<Canvas>()) if (c.isRootCanvas) oyunArayuzu.Add(c);
        var ipucu = FindAnyObjectByType<IpucuSistemi>();
        if (ipucu != null) { ipucu.enabled = false; if (ipucu.serit != null) ipucu.serit.SetActive(false); }

        // Duraklatma timeScale'i 0 yapar ve afetleri dondurur; bu yüzden oyun akar ama saat her karede elle ayarlanır
        gm.kullaniciDurdurdu = false;
        gm.hizCarpani = 1;
        gm.para = 5000;
        gm.temelBinaLevel = 1; gm.itfaiyeLevel = 1; gm.altyapiLevel = 1; gm.setLevel = 3; gm.okulLevel = 2; gm.akutLevel = 2;
        if (afet != null) afet.afetSuresi = 5.2f; // afet (müzik kısılması dahil) bir çekime sığsın

        adaMerkezi = oyunKamerasi != null ? oyunKamerasi.boundsCenter : Vector3.zero;
        adaYaricapi = oyunKamerasi != null ? Mathf.Max(oyunKamerasi.boundsHalfExtents.x, oyunKamerasi.boundsHalfExtents.y) * 0.8f : 70f;
        konut = BinaMerkezi(BinaTuru.TemelBina); okul = BinaMerkezi(BinaTuru.Okul);
        itfaiye = BinaMerkezi(BinaTuru.Itfaiye); akut = BinaMerkezi(BinaTuru.Akut); set = BinaMerkezi(BinaTuru.DenizSeti);
        ornekEv = MerkezeEnYakin(BinaTuru.TemelBina);
        if (afet != null) tsunamiSetZ = afet.tsunamiSetZ;
        var fenerObj = BulAdla("DenizFeneri");
        feneri = fenerObj != null ? fenerObj.position : set;
        adaMerkezi.y = konut.y;

        log += "merkez " + adaMerkezi + " yarıçap " + adaYaricapi + "\nkonut " + konut + " okul " + okul + " itfaiye " + itfaiye + " akut " + akut + " set " + set + " fener " + feneri + "\n";
        YaziKatmaniKur();
    }

    Vector3 BinaMerkezi(BinaTuru tur)
    {
        Vector3 toplam = Vector3.zero; int n = 0;
        foreach (var b in FindObjectsByType<BuildingVisuals>())
            if (b.binaTuru == tur) { toplam += b.transform.position; n++; }
        return n > 0 ? toplam / n : adaMerkezi;
    }

    Vector3 MerkezeEnYakin(BinaTuru tur)
    {
        Vector3 en = adaMerkezi; float d = float.MaxValue;
        foreach (var b in FindObjectsByType<BuildingVisuals>())
        {
            if (b.binaTuru != tur) continue;
            float m = (b.transform.position - adaMerkezi).sqrMagnitude;
            if (m < d) { d = m; en = b.transform.position; }
        }
        return en;
    }

    static Transform BulAdla(string ad)
    {
        foreach (var t in FindObjectsByType<Transform>()) if (t.name == ad) return t;
        return null;
    }

    void YaziKatmaniKur()
    {
        var go = new GameObject("TrailerYazilari", typeof(Canvas), typeof(CanvasScaler));
        yaziKatmani = go.GetComponent<Canvas>();
        yaziKatmani.renderMode = RenderMode.ScreenSpaceOverlay;
        yaziKatmani.sortingOrder = 5000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var kararti = new GameObject("Karartma", typeof(RectTransform), typeof(Image));
        kararti.transform.SetParent(go.transform, false);
        Doldur(kararti.GetComponent<RectTransform>());
        karartma = kararti.GetComponent<Image>();
        karartma.color = Color.black; karartma.raycastTarget = false;

        baslik = Yazi(go.transform, "Baslik", 190, new Vector2(0, 40));
        baslik.characterSpacing = 18;
        altYazi = Yazi(go.transform, "AltYazi", 44, new Vector2(0, -90));
        etiket = Yazi(go.transform, "Etiket", 64, new Vector2(0, -360));
        etiket.characterSpacing = 12;
    }

    TMP_Text Yazi(Transform ebeveyn, string ad, float boyut, Vector2 konum)
    {
        var go = new GameObject(ad, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(ebeveyn, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1800, boyut * 1.4f);
        rt.anchoredPosition = konum;
        var t = go.GetComponent<TextMeshProUGUI>();
        var font = TurkishTMPFontSupport.DisplayFont;
        if (font != null) t.font = font;
        t.fontSize = boyut; t.alignment = TextAlignmentOptions.Center; t.color = new Color(1, 1, 1, 0);
        t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
        var golge = go.AddComponent<Shadow>();
        golge.effectColor = new Color(0.03f, 0.07f, 0.12f, 0.75f); golge.effectDistance = new Vector2(4, -4);
        return t;
    }

    static void Doldur(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    // ---------- Çekimler ----------

    static float CekimBaslangici(float t)
    {
        float[] s = { S9, S8, S7, S6, S5, S4, S3, S2, S1 };
        foreach (float b in s) if (t >= b) return b;
        return 0;
    }

    void CekimBasladi(float c)
    {
        if (c == S3 || c == S4) OyunKamerasi(true); else OyunKamerasi(false);
        Arayuz(c == S3 || c == S4);
        RaporuKapat();

        if (c == S5)
        {
            gm.temelBinaLevel = 1; gm.itfaiyeLevel = 1; gm.altyapiLevel = 1; gm.setLevel = 3;
            atesHedefi = konut;
            StartCoroutine(Afet(0, S6 - S5));
        }
        if (c == S6) StartCoroutine(Afet(1, S7 - S6));
        if (c == S7) StartCoroutine(Afet(2, S8 - S7));
        if (c == S8)
        {
            // Oyunun 20 sn'lik tsunami bitiş sinematiği kamerayı devralır ve oyunu bitirir; trailer'da sadece dalgayı çekiyoruz
            var finale = afet != null ? afet.GetComponent<IslandTsunamiFinale>() : null;
            if (finale != null) DestroyImmediate(finale);
            StartCoroutine(Afet(3, S9 - S8));
        }
        if (c == S3) StartCoroutine(OynanisCekimi());
        if (c == S4) StartCoroutine(QuizCekimi());
    }

    void Guncelle(float t)
    {
        if (muzikKaynagi != null) muzikKaynagi.mute = true; // müzik sonradan eklenecek

        // Yazılar ve karartma
        karartma.color = new Color(0, 0, 0, Mathf.Max(1 - Mathf.InverseLerp(0f, 1.2f, t), Mathf.InverseLerp(59.2f, 60f, t)));
        Goster(baslik, "KALKAN'lı", Pencere(t, 1.4f, 5.4f) + Pencere(t, 55.2f, 59.6f));
        Goster(altYazi, t < 30 ? "Afetlere hazırlan, şehrini koru" : "Her hazırlık bir hayatı korur", Pencere(t, 2.4f, 5.4f) + Pencere(t, 56.2f, 59.6f));
        string e = ""; float ea = 0;
        if (t >= 10.2f && t < 13f) { e = "ŞEHRİNİ GÜÇLENDİR · HALKINI BİLİNÇLENDİR"; ea = Pencere(t, 10.2f, 12.8f); }
        else if (t >= S5 && t < S6) { e = "YANGIN"; ea = Pencere(t, S5 + 0.4f, S5 + 2.6f); }
        else if (t >= S6 && t < S7) { e = "SEL"; ea = Pencere(t, S6 + 0.4f, S6 + 2.6f); }
        else if (t >= S7 && t < S8) { e = "DEPREM"; ea = Pencere(t, S7 + 0.4f, S7 + 2.6f); }
        else if (t >= S8 && t < S9) { e = "TSUNAMİ"; ea = Pencere(t, S8 + 0.4f, S8 + 2.6f); }
        else if (t >= S9 - 0.01f && t < 55f) { e = "4 AFET · 20 GÜN"; ea = Pencere(t, S9 + 0.2f, 55f); }
        Goster(etiket, e, ea);

        // Günün saati ve kamera
        if (t < S2) { Saat(Mathf.Lerp(0.03f, 0.13f, Yumusak(t / S2))); Kamera(Uzaktan(205, 2.3f, 14), Uzaktan(205, 1.25f, 20), adaMerkezi, adaMerkezi + Vector3.up * 2, 38, t / S2); }
        else if (t < S3) { Saat(0.3f); float u = (t - S2) / (S3 - S2); Yorunge(feneri, 48, 20, 150, 215, u, 36, 9); }
        else if (t < S5) { Saat(0.35f); }
        else if (t < S6)
        {
            Saat(0.42f); float u = (t - S5) / (S6 - S5);
            if (afet != null && t > S5 + 1.0f) atesHedefi = Vector3.Lerp(atesHedefi, afet.FireOrigin, 0.08f);
            // Kamera hep ada içinden, ateşe yukarıdan baksın (ateş kıyıdaysa duvar kadrajı kapatmasın)
            Vector3 iceri = adaMerkezi - atesHedefi; iceri.y = 0;
            iceri = iceri.sqrMagnitude > 1 ? iceri.normalized : Vector3.back;
            Vector3 yan = Vector3.Cross(Vector3.up, iceri);
            Kamera(atesHedefi + iceri * 30 + yan * 10 + Vector3.up * 24, atesHedefi + iceri * 22 - yan * 6 + Vector3.up * 17, atesHedefi, atesHedefi + Vector3.up * 2, 40, u);
        }
        else if (t < S7) { Saat(0.38f); float u = (t - S6) / (S7 - S6); Kamera(adaMerkezi + new Vector3(-40, 62, -40), adaMerkezi + new Vector3(-30, 50, -30), adaMerkezi, adaMerkezi, 42, u); }
        else if (t < S8) { Saat(0.32f); float u = (t - S7) / (S8 - S7); Kamera(Etrafinda(ornekEv, 140, 30, 10), Etrafinda(ornekEv, 160, 22, 8), ornekEv, ornekEv + Vector3.up * 2, 42, u); Salla(t, u); }
        else if (t < S9)
        {
            Saat(Mathf.Lerp(0.50f, 0.56f, (t - S8) / (S9 - S8))); float u = (t - S8) / (S9 - S8);
            Vector3 setNoktasi = new Vector3(adaMerkezi.x, adaMerkezi.y, tsunamiSetZ);
            Kamera(setNoktasi + new Vector3(70, 26, -60), setNoktasi + new Vector3(52, 20, -46), setNoktasi + new Vector3(0, 2, 8), setNoktasi + new Vector3(-8, 3, 12), 46, u);
        }
        else { Saat(Mathf.Lerp(0.76f, 0.82f, (t - S9) / (60 - S9))); float u = (t - S9) / (60 - S9); Kamera(Uzaktan(200, 0.55f, 28), Uzaktan(200, 1.35f, 64), adaMerkezi, adaMerkezi, 40, u); }
    }

    IEnumerator OynanisCekimi()
    {
        yield return Bekle(0.8f);
        BinaPopup.Instance?.Ac(BinaTuru.TemelBina);
        yield return Bekle(1.6f); BinaPopup.Instance?.YukseltTiklandi();
        yield return Bekle(1.3f); BinaPopup.Instance?.YukseltTiklandi();
        yield return Bekle(1.2f); BinaPopup.Instance?.Kapat();
        yield return Bekle(0.6f);
        BinaPopup.Instance?.Ac(BinaTuru.Itfaiye);
        yield return Bekle(1.4f); BinaPopup.Instance?.YukseltTiklandi();
        yield return Bekle(1.2f); BinaPopup.Instance?.Kapat();
    }

    IEnumerator QuizCekimi()
    {
        yield return Bekle(0.3f);
        var q = QuizManager.Instance;
        if (q == null) yield break;
        q.QuizAc();
        yield return Bekle(1.6f);
        if (q.AktifSoru != null && q.sikButonlari != null && q.AktifSoru.dogruSikIndex < q.sikButonlari.Length)
            q.sikButonlari[q.AktifSoru.dogruSikIndex].onClick.Invoke();
        yield return Bekle(2.7f);
        q.QuizKapat();
    }

    IEnumerator Afet(int index, float cekimSuresi)
    {
        afet?.AfetBaslat(index);
        yield return Bekle(cekimSuresi - 0.15f);
        RaporuKapat();
    }

    void RaporuKapat()
    {
        if (gm == null || !gm.afetOlduMu) return;
        var rapor = FindAnyObjectByType<AfetRaporu>();
        if (rapor != null) rapor.Devam(); else gm.afetOlduMu = false;
    }

    // ---------- Yardımcılar ----------

    void OyunKamerasi(bool acik)
    {
        if (oyunKamerasi != null) oyunKamerasi.enabled = false; // girişleri okumasın, kamerayı biz yönetiyoruz
        kam.orthographic = acik;
        if (acik && oyunKamerasi != null) { oyunKamerasi.FrameCity(); kam.orthographicSize = 38f; kam.transform.position += kam.transform.up * 11f; }
        if (!acik) { kam.nearClipPlane = 0.3f; kam.farClipPlane = 1500f; }
    }

    void Arayuz(bool acik) { foreach (var c in oyunArayuzu) if (c != null) c.enabled = acik; }

    void Saat(float evre)
    {
        if (saat != null && gunSayaciAlani != null)
            gunSayaciAlani.SetValue(saat, Mathf.Clamp(evre, 0f, 0.95f) * Mathf.Max(0.1f, DengeAyarlari.Aktif.birGunSuresi));
    }

    void Kamera(Vector3 a, Vector3 b, Vector3 bakA, Vector3 bakB, float fov, float u)
    {
        float s = Yumusak(Mathf.Clamp01(u));
        kam.fieldOfView = fov;
        kam.transform.position = Vector3.Lerp(a, b, s);
        kam.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(bakA, bakB, s) - kam.transform.position, Vector3.up);
    }

    void Yorunge(Vector3 hedef, float yaricap, float yukseklik, float aciA, float aciB, float u, float fov, float bakisYuksekligi = 4)
    {
        float aci = Mathf.Lerp(aciA, aciB, Yumusak(u)) * Mathf.Deg2Rad;
        kam.fieldOfView = fov;
        kam.transform.position = hedef + new Vector3(Mathf.Cos(aci), 0, Mathf.Sin(aci)) * yaricap + Vector3.up * yukseklik;
        kam.transform.rotation = Quaternion.LookRotation(hedef + Vector3.up * bakisYuksekligi - kam.transform.position, Vector3.up);
    }

    void Salla(float t, float u)
    {
        float guc = 0.35f * Mathf.Clamp01(u * 4) * Mathf.Clamp01((1 - u) * 3);
        kam.transform.position += new Vector3(Mathf.PerlinNoise(t * 9, 1) - 0.5f, Mathf.PerlinNoise(2, t * 11) - 0.5f, Mathf.PerlinNoise(t * 7, 3) - 0.5f) * guc * 2;
    }

    Vector3 Uzaktan(float aciDerece, float yaricapCarpani, float yukseklik)
    {
        float a = aciDerece * Mathf.Deg2Rad;
        return adaMerkezi + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * adaYaricapi * yaricapCarpani + Vector3.up * yukseklik;
    }

    static Vector3 Etrafinda(Vector3 hedef, float aciDerece, float uzaklik, float yukseklik)
    {
        float a = aciDerece * Mathf.Deg2Rad;
        return hedef + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * uzaklik + Vector3.up * yukseklik;
    }

    Vector3 DenizTarafi(Vector3 nokta)
    {
        Vector3 yon = nokta - adaMerkezi; yon.y = 0;
        if (yon.sqrMagnitude < 1) yon = Vector3.back;
        return nokta + yon.normalized * 30f;
    }

    static float Pencere(float t, float a, float b) => Mathf.Clamp01(Mathf.Min(Mathf.InverseLerp(a, a + 0.6f, t), Mathf.InverseLerp(b, b - 0.6f, t)));
    static float Yumusak(float x) => x * x * (3 - 2 * x);
    static void Goster(TMP_Text y, string metin, float alfa) { if (y.text != metin) y.text = metin; y.color = new Color(1, 1, 1, Mathf.Clamp01(alfa)); }
    static IEnumerator Bekle(float saniye) { int kare = Mathf.RoundToInt(saniye * Fps); for (int i = 0; i < kare; i++) yield return null; }

    // ---------- Ses ----------

    static int KanalSayisi()
    {
        switch (AudioSettings.speakerMode)
        {
            case AudioSpeakerMode.Mono: return 1;
            case AudioSpeakerMode.Quad: return 4;
            case AudioSpeakerMode.Surround: return 5;
            case AudioSpeakerMode.Mode5point1: return 6;
            case AudioSpeakerMode.Mode7point1: return 8;
            default: return 2;
        }
    }

    void SesAl()
    {
        int n = AudioRenderer.GetSampleCountForCaptureFrame();
        if (n <= 0) return;
        var tampon = new NativeArray<float>(n * sesKanallari, Allocator.Temp);
        AudioRenderer.Render(tampon);
        for (int i = 0; i < tampon.Length; i++) sesOrnekleri.Add(tampon[i]);
        tampon.Dispose();
    }

    void WavYaz(string yol)
    {
        int oran = AudioSettings.outputSampleRate, kanal = sesKanallari;
        using (var w = new BinaryWriter(File.Create(yol)))
        {
            int veri = sesOrnekleri.Count * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + veri); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)kanal);
            w.Write(oran); w.Write(oran * kanal * 2); w.Write((short)(kanal * 2)); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(veri);
            foreach (float s in sesOrnekleri) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
    }
}
