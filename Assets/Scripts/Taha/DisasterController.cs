using System.Collections;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Görsel Efekt Bağlantıları")]
    public Transform kamera;       // Depremde sallanacak kamera
    public Transform tsunamiSuyu;  // Tsunamide yükselecek mavi küp

    private int sonAfetGunu = 0;   // Aynı günde 2 kere afet olmasın diye

    void Update()
    {
        // 1. Kişinin gün sayacı 7, 14, 21... olduğunda otomatik tetiklenir
        if (GameManager.Instance != null)
        {
            int mevcutGun = GameManager.Instance.gun;
            if (mevcutGun > 0 && mevcutGun % 7 == 0 && mevcutGun != sonAfetGunu)
            {
                sonAfetGunu = mevcutGun;
                RastgeleAfetBaslat();
            }
        }

        // TEST İÇİN: Klavyeden SPACE tuşuna basınca anında afet başlatır!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RastgeleAfetBaslat();
        }
    }

    public void RastgeleAfetBaslat()
    {
        // Haftaya göre afetin zorluk derecesi (7. gün: Lvl 2 gerekir, 14. gün: Lvl 3 gerekir...)
        int gerekenLevel = (GameManager.Instance.gun / 7) + 1;
        if (gerekenLevel < 2) gerekenLevel = 2; // Test ederken en az Level 2 istesin

        int afetTipi = Random.Range(0, 4); // 0: Deprem, 1: Sel, 2: Yangın, 3: Tsunami
        bool hasarAldiMi = false;
        string afetAdi = "";

        switch (afetTipi)
        {
            case 0:
                afetAdi = "DEPREM (Şiddet Eşiği: Lvl " + gerekenLevel + ")";
                StartCoroutine(KameraSalla(1.5f, 0.3f)); // 1.5 saniye kamerayı salla
                
                // Deprem tüm binaları kontrol eder, eşikten düşük olanların leveli 1 düşer
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

            case 1:
                afetAdi = "SEL (Gereken Altyapı: Lvl " + gerekenLevel + ")";
                if (tsunamiSuyu != null) StartCoroutine(SuYukseltIndir(1.0f));

                // Selde Yol ve Altyapı leveli kontrol edilir
                if (GameManager.Instance.altyapiLevel < gerekenLevel) {
                    GameManager.Instance.altyapiLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.altyapiLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;

            case 2:
                afetAdi = "YANGIN (Gereken İtfaiye: Lvl " + gerekenLevel + ")";
                // Yangında İtfaiye birimi kontrol edilir
                if (GameManager.Instance.itfaiyeLevel < gerekenLevel) {
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel);
                    hasarAldiMi = true;
                }
                break;

            case 3:
                afetAdi = "TSUNAMİ (Gereken Deniz Seti: Lvl " + gerekenLevel + ")";
                if (tsunamiSuyu != null) StartCoroutine(SuYukseltIndir(2.5f)); // Suyu çok yükselt

                // Tsunamide ada etrafındaki Set kontrol edilir
                if (GameManager.Instance.setLevel < gerekenLevel) {
                    GameManager.Instance.setLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.setLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;
        }

        // CAN KAYBI HESAPLAMA (Okul Farkındalığı ve AKUT Leveli koruma sağlar)
        int vefatSayisi = 0;
        if (hasarAldiMi)
        {
            int bazKayip = 120;
            // Farkındalık %100 ise %70 korur, AKUT her levelde %10 korur
            float korumaOrani = (GameManager.Instance.farkindalikYuzdesi / 100f) * 0.7f + (GameManager.Instance.akutLevel * 0.1f);
            korumaOrani = Mathf.Clamp(korumaOrani, 0f, 0.95f); // En fazla %95 koruma

            vefatSayisi = Mathf.RoundToInt(bazKayip * (1f - korumaOrani));
            GameManager.Instance.nufus = Mathf.Max(0, GameManager.Instance.nufus - vefatSayisi);
        }

        Debug.Log($"[AFET OLDU] {afetAdi} | Hasar Var Mı: {hasarAldiMi} | Vefat: {vefatSayisi} | Kalan Nüfus: {GameManager.Instance.nufus}");

        // --- YENİ EKLENEN KISIM: GameManager'a Rapor Ver ve Zamanı Durdur ---
        
        GameManager.Instance.sonVefatSayisi = vefatSayisi;
        GameManager.Instance.afetOlduMu = true; // UI bunu görünce paneli açacak
        GameManager.Instance.oyunDurduMu = true; // Zaman akışı duracak

        if (hasarAldiMi)
        {
            GameManager.Instance.sonAfetMesaji = $"{afetAdi} adayı vurdu! Maalesef savunma yetersiz kaldı ve binalar hasar aldı.";
        }
        else
        {
            GameManager.Instance.sonAfetMesaji = $"{afetAdi} gerçekleşti, ancak yüksek savunma sayesinde binalarımız zarar görmedi!";
        }

        Debug.Log(GameManager.Instance.sonAfetMesaji);
    }

    // Deprem için Kamera Sallama Efekti
    IEnumerator KameraSalla(float sure, float siddet)
    {
        if (kamera == null) yield break;
        Vector3 orijinalPozisyon = kamera.localPosition;
        float gecenSure = 0f;

        while (gecenSure < sure)
        {
            float x = Random.Range(-1f, 1f) * siddet;
            float y = Random.Range(-1f, 1f) * siddet;
            kamera.localPosition = orijinalPozisyon + new Vector3(x, y, 0);
            gecenSure += Time.deltaTime;
            yield return null;
        }
        kamera.localPosition = orijinalPozisyon;
    }

    // Tsunami ve Sel için Su Yükselme Efekti
    IEnumerator SuYukseltIndir(float hedefYukseklik)
    {
        Vector3 baslangic = tsunamiSuyu.position;
        Vector3 tepeNoktasi = new Vector3(baslangic.x, hedefYukseklik, baslangic.z);

        float t = 0;
        while (t < 1f) { t += Time.deltaTime; tsunamiSuyu.position = Vector3.Lerp(baslangic, tepeNoktasi, t); yield return null; }
        yield return new WaitForSeconds(1.5f);
        t = 0;
        while (t < 1f) { t += Time.deltaTime; tsunamiSuyu.position = Vector3.Lerp(tepeNoktasi, baslangic, t); yield return null; }
    }
}