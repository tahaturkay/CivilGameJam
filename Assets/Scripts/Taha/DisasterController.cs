using System.Collections;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Temel Bağlantılar")]
    public Transform kamera;              // Main Camera (Deprem sarsıntısı için)
    public Transform tsunamiSuyu;         // Yerin altındaki büyük su küpü (Sel ve Tsunami için)

    [Header("Yeni Görsel Efektler (VFX)")]
    public Transform tsunamiDalgasi;      // Uzaktan gelen dalga küpü
    public GameObject yanginEfekti;       // Yangın Particle System objesi
    public GameObject yagmurEfekti;       // Yağmur Particle System objesi

    private int sonAfetGunu = 0;
    private bool afetDevamEdiyorMu = false; // Efekt oynarken üst üste basılmasın diye

    void Update()
    {
        if (afetDevamEdiyorMu) return;

        // 7, 14, 21. günlerde otomatik afet
        if (GameManager.Instance != null)
        {
            int mevcutGun = GameManager.Instance.gun;
            if (mevcutGun > 0 && mevcutGun % 7 == 0 && mevcutGun != sonAfetGunu)
            {
                sonAfetGunu = mevcutGun;
                RastgeleAfetBaslat(-1); // -1 = Rastgele seç
            }
        }

        // --- TEST TUŞLARI (İstediğin afeti anında denemek için) ---
        if (Input.GetKeyDown(KeyCode.Space)) RastgeleAfetBaslat(-1); // Rastgele
        if (Input.GetKeyDown(KeyCode.Alpha1)) RastgeleAfetBaslat(0); // 1: Sadece Deprem
        if (Input.GetKeyDown(KeyCode.Alpha2)) RastgeleAfetBaslat(1); // 2: Sadece Sel
        if (Input.GetKeyDown(KeyCode.Alpha3)) RastgeleAfetBaslat(2); // 3: Sadece Yangın
        if (Input.GetKeyDown(KeyCode.Alpha4)) RastgeleAfetBaslat(3); // 4: Sadece Tsunami
    }

    public void RastgeleAfetBaslat(int zorunluAfetTipi = -1)
    {
        if (afetDevamEdiyorMu) return;

        int gerekenLevel = (GameManager.Instance.gun / 7) + 1;
        if (gerekenLevel < 2) gerekenLevel = 2; // Test için minimum eşik 2

        int afetTipi = (zorunluAfetTipi == -1) ? Random.Range(0, 4) : zorunluAfetTipi;
        StartCoroutine(AfetSureciYonet(afetTipi, gerekenLevel));
    }

    IEnumerator AfetSureciYonet(int afetTipi, int gerekenLevel)
    {
        afetDevamEdiyorMu = true;
        bool hasarAldiMi = false;
        string afetAdi = "";

        switch (afetTipi)
        {
            case 0: // DEPREM
                afetAdi = "DEPREM (Gereken Bina Lvl: " + gerekenLevel + ")";
                Debug.Log("Deprem başladı! Kamera sallanıyor...");
                
                // Kamerayı 2.5 saniye boyunca salla (Bitmesini bekle)
                yield return StartCoroutine(KameraSalla(2.5f, 0.4f));

                if (GameManager.Instance.temelBinaLevel < gerekenLevel) {
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                if (GameManager.Instance.okulLevel < gerekenLevel) {
                    GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel);
                    hasarAldiMi = true;
                }
                if (GameManager.Instance.akutLevel < gerekenLevel) {
                    GameManager.Instance.akutLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.akutLevel);
                    hasarAldiMi = true;
                }
                if (GameManager.Instance.itfaiyeLevel < gerekenLevel) {
                    GameManager.Instance.itfaiyeLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.itfaiyeLevel);
                    hasarAldiMi = true;
                }
                break;

            case 1: // SEL (Yağmur + Koşullu Su Yükselmesi)
                afetAdi = "SEL (Gereken Altyapı Lvl: " + gerekenLevel + ")";
                bool selHasariVar = GameManager.Instance.altyapiLevel < gerekenLevel;

                // Yağmur yağmaya başlasın ve sadece altyapı yetersizse su yükselsin!
                yield return StartCoroutine(SelEfektiOynat(selHasariVar));

                if (selHasariVar) {
                    GameManager.Instance.altyapiLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.altyapiLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;

            case 2: // YANGIN (Particle System)
                afetAdi = "YANGIN (Gereken İtfaiye Lvl: " + gerekenLevel + ")";
                
                // Yangın efektini aç, 3 saniye bekle ve kapat
                yield return StartCoroutine(YanginEfektiOynat(3.0f));

                if (GameManager.Instance.itfaiyeLevel < gerekenLevel) {
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel);
                    hasarAldiMi = true;
                }
                break;

            case 3: // TSUNAMİ (Gelen Dalga + Su Yükselmesi)
                afetAdi = "TSUNAMİ (Gereken Set Lvl: " + gerekenLevel + ")";
                
                // Önce dalga gelsin ve geldiği yeri (suyu) yükseltsin
                yield return StartCoroutine(TsunamiDalgaVeSuEfekti(2.5f));

                if (GameManager.Instance.setLevel < gerekenLevel) {
                    GameManager.Instance.setLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.setLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;
        }

        // --- EFEKTLER BİTTİ, ŞİMDİ CAN KAYBI VE ÖZET EKRANI ---
        int vefatSayisi = 0;
        if (hasarAldiMi)
        {
            int bazKayip = 120;
            float korumaOrani = (GameManager.Instance.farkindalikYuzdesi / 100f) * 0.7f + (GameManager.Instance.akutLevel * 0.1f);
            korumaOrani = Mathf.Clamp(korumaOrani, 0f, 0.95f);

            vefatSayisi = Mathf.RoundToInt(bazKayip * (1f - korumaOrani));
            GameManager.Instance.nufus = Mathf.Max(0, GameManager.Instance.nufus - vefatSayisi);
        }

        // GameManager verilerini doldur ve oyunu durdur
        GameManager.Instance.sonVefatSayisi = vefatSayisi;
        GameManager.Instance.afetOlduMu = true;
        GameManager.Instance.oyunDurduMu = true;

        if (hasarAldiMi)
            GameManager.Instance.sonAfetMesaji = $"{afetAdi} adayı vurdu! Savunma yetersiz kaldı, binalar hasar aldı.";
        else
            GameManager.Instance.sonAfetMesaji = $"{afetAdi} gerçekleşti, ancak güçlü önlemler sayesinde hiçbir bina zarar görmedi!";

        Debug.Log("AFET ÖZETİ: " + GameManager.Instance.sonAfetMesaji + " | Ölen: " + GameManager.Instance.sonVefatSayisi);
        afetDevamEdiyorMu = false;
    }

    // 1. DEPREM: Kamera Sallama (Gittikçe azalan sarsıntı)
    IEnumerator KameraSalla(float sure, float siddet)
    {
        if (kamera == null) yield break;
        Vector3 orijinalPozisyon = kamera.localPosition;
        float gecenSure = 0f;

        while (gecenSure < sure)
        {
            float guncelSiddet = Mathf.Lerp(siddet, 0f, gecenSure / sure); // Sarsıntı yavaşça azalsın
            float x = Random.Range(-1f, 1f) * guncelSiddet;
            float y = Random.Range(-1f, 1f) * guncelSiddet;
            kamera.localPosition = orijinalPozisyon + new Vector3(x, y, 0);
            gecenSure += Time.deltaTime;
            yield return null;
        }
        kamera.localPosition = orijinalPozisyon;
    }

    // 2. SEL: Yağmur Yağdır ve Altyapı Yetersizse Suyu Yükselt
    IEnumerator SelEfektiOynat(bool suYukselsinMi)
    {
        if (yagmurEfekti != null) yagmurEfekti.SetActive(true);
        Debug.Log("Şiddetli yağmur başladı...");

        // 1.5 saniye sadece yağmur yağsın
        yield return new WaitForSeconds(1.5f);

        if (suYukselsinMi && tsunamiSuyu != null)
        {
            Debug.Log("Altyapı yetersiz! Sel suları yükseliyor!");
            yield return StartCoroutine(SuYukseltIndir(1.2f)); // Suyu yükselt ve indir
        }
        else
        {
            Debug.Log("Altyapı seviyesi yeterli! Sular tahliye edildi, sel olmadı.");
            yield return new WaitForSeconds(1.5f); // Sadece yağmur yağmaya devam etsin
        }

        if (yagmurEfekti != null) yagmurEfekti.SetActive(false);
    }

    // 3. YANGIN: Ateş Particle Aç/Kapat
    IEnumerator YanginEfektiOynat(float sure)
    {
        if (yanginEfekti != null) yanginEfekti.SetActive(true);
        yield return new WaitForSeconds(sure);
        if (yanginEfekti != null) yanginEfekti.SetActive(false);
    }

    // 4. TSUNAMİ: Dalga İlerler ve Çarptığında Suyu Yükseltir
    IEnumerator TsunamiDalgaVeSuEfekti(float hedefSuYuksekligi)
    {
        if (tsunamiDalgasi != null)
        {
            tsunamiDalgasi.gameObject.SetActive(true);
            Vector3 dalgaBaslangic = new Vector3(0, 1f, -30f); // Uzaktan başlar
            Vector3 dalgaBitis = new Vector3(0, 1f, 5f);       // Adaya çarpar

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.8f; // Dalga geliş hızı
                tsunamiDalgasi.position = Vector3.Lerp(dalgaBaslangic, dalgaBitis, t);
                yield return null;
            }
        }

        // Dalga adaya ulaştığı anda su hızla yükselir!
        if (tsunamiSuyu != null)
        {
            yield return StartCoroutine(SuYukseltIndir(hedefSuYuksekligi));
        }

        if (tsunamiDalgasi != null)
        {
            tsunamiDalgasi.gameObject.SetActive(false);
        }
    }

    // Ortak Su Yükseltme / İndirme Yardımcı Fonksiyonu
    IEnumerator SuYukseltIndir(float hedefYukseklik)
    {
        Vector3 baslangic = tsunamiSuyu.position;
        Vector3 tepeNoktasi = new Vector3(baslangic.x, hedefYukseklik, baslangic.z);

        float t = 0;
        while (t < 1f) { t += Time.deltaTime * 1.2f; tsunamiSuyu.position = Vector3.Lerp(baslangic, tepeNoktasi, t); yield return null; }
        yield return new WaitForSeconds(1.5f);
        t = 0;
        while (t < 1f) { t += Time.deltaTime; tsunamiSuyu.position = Vector3.Lerp(tepeNoktasi, baslangic, t); yield return null; }
    }
}