using System.Collections;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Temel Bağlantılar")]
    public Transform kamera;              
    public Transform tsunamiSuyu;         

    [Header("Yeni Görsel Efektler (VFX)")]
    public Transform tsunamiDalgasi;      
    public GameObject yanginEfekti;       
    public GameObject yagmurEfekti;       

    private int sonAfetGunu = 0;
    private bool afetDevamEdiyorMu = false;

    void Update()
    {
        if (afetDevamEdiyorMu) return;

        if (GameManager.Instance != null)
        {
            int mevcutGun = GameManager.Instance.gun;
            if (mevcutGun > 0 && mevcutGun % 7 == 0 && mevcutGun != sonAfetGunu)
            {
                sonAfetGunu = mevcutGun;
                RastgeleAfetBaslat(-1);
            }
        }

        // Test Tuşları
        if (Input.GetKeyDown(KeyCode.Space)) RastgeleAfetBaslat(-1); 
        if (Input.GetKeyDown(KeyCode.Alpha1)) RastgeleAfetBaslat(0); 
        if (Input.GetKeyDown(KeyCode.Alpha2)) RastgeleAfetBaslat(1); 
        if (Input.GetKeyDown(KeyCode.Alpha3)) RastgeleAfetBaslat(2); 
        if (Input.GetKeyDown(KeyCode.Alpha4)) RastgeleAfetBaslat(3); 
    }

    public void RastgeleAfetBaslat(int zorunluAfetTipi = -1)
    {
        if (afetDevamEdiyorMu) return;

        int gerekenLevel = (GameManager.Instance.gun / 7) + 1;
        if (gerekenLevel < 2) gerekenLevel = 2;

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
            case 0: // DEPREM (15 Saniye)
                afetAdi = "DEPREM (Gereken Lvl: " + gerekenLevel + ")";
                yield return StartCoroutine(KameraSalla(15.0f, 0.4f)); 

                if (GameManager.Instance.temelBinaLevel < gerekenLevel) { GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel); hasarAldiMi = true; }
                if (GameManager.Instance.okulLevel < gerekenLevel) { GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel); hasarAldiMi = true; }
                if (GameManager.Instance.akutLevel < gerekenLevel) { GameManager.Instance.akutLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.akutLevel); hasarAldiMi = true; }
                if (GameManager.Instance.itfaiyeLevel < gerekenLevel) { GameManager.Instance.itfaiyeLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.itfaiyeLevel); hasarAldiMi = true; }
                break;

            case 1: // SEL (15 Saniye)
                afetAdi = "SEL (Gereken Lvl: " + gerekenLevel + ")";
                bool selHasariVar = GameManager.Instance.altyapiLevel < gerekenLevel;
                yield return StartCoroutine(SelEfektiOynat(selHasariVar));

                if (selHasariVar) {
                    GameManager.Instance.altyapiLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.altyapiLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;

            case 2: // YANGIN (15 Saniye)
                afetAdi = "YANGIN (Gereken Lvl: " + gerekenLevel + ")";
                yield return StartCoroutine(YanginEfektiOynat(15.0f));

                if (GameManager.Instance.itfaiyeLevel < gerekenLevel) {
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel);
                    hasarAldiMi = true;
                }
                break;

            case 3: // TSUNAMİ (15 Saniye)
                afetAdi = "TSUNAMİ (Gereken Lvl: " + gerekenLevel + ")";
                // Haritanız küçüldüğü için su yüksekliği (hedefSuYuksekligi) 1.5f olarak ayarlandı
                yield return StartCoroutine(TsunamiDalgaVeSuEfekti(1.5f)); 

                if (GameManager.Instance.setLevel < gerekenLevel) {
                    GameManager.Instance.setLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.setLevel);
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    hasarAldiMi = true;
                }
                break;
        }

        int vefatSayisi = 0;
        if (hasarAldiMi)
        {
            int bazKayip = 120;
            float korumaOrani = (GameManager.Instance.farkindalikYuzdesi / 100f) * 0.7f + (GameManager.Instance.akutLevel * 0.1f);
            korumaOrani = Mathf.Clamp(korumaOrani, 0f, 0.95f);
            vefatSayisi = Mathf.RoundToInt(bazKayip * (1f - korumaOrani));
            GameManager.Instance.nufus = Mathf.Max(0, GameManager.Instance.nufus - vefatSayisi);
        }

        GameManager.Instance.sonVefatSayisi = vefatSayisi;
        GameManager.Instance.afetOlduMu = true;
        GameManager.Instance.oyunDurduMu = true;

        if (hasarAldiMi) GameManager.Instance.sonAfetMesaji = $"{afetAdi} adayı vurdu! Savunma yetersiz kaldı.";
        else GameManager.Instance.sonAfetMesaji = $"{afetAdi} gerçekleşti, ancak güçlü önlemler sayesinde hasar yok!";

        afetDevamEdiyorMu = false;
    }

    // --- 15 SANİYELİK GÜNCELLENMİŞ EFEKT KORUTİNLERİ ---

    IEnumerator KameraSalla(float sure, float siddet)
    {
        if (kamera == null) yield break;
        Vector3 orijinalPozisyon = kamera.localPosition;
        float gecenSure = 0f;

        while (gecenSure < sure)
        {
            float guncelSiddet = Mathf.Lerp(siddet, 0f, gecenSure / sure);
            float x = Random.Range(-1f, 1f) * guncelSiddet;
            float y = Random.Range(-1f, 1f) * guncelSiddet;
            kamera.localPosition = orijinalPozisyon + new Vector3(x, y, 0);
            gecenSure += Time.deltaTime;
            yield return null;
        }
        kamera.localPosition = orijinalPozisyon;
    }

    IEnumerator SelEfektiOynat(bool suYukselsinMi)
    {
        if (yagmurEfekti != null) yagmurEfekti.SetActive(true);

        if (suYukselsinMi && tsunamiSuyu != null)
        {
            yield return new WaitForSeconds(2.0f); // 2s yağmur yağar
            // Su 3s çıkar, 7s bekler, 3s iner (Toplam 13s + 2s = 15s)
            yield return StartCoroutine(SuYukseltIndir(1.2f, 3f, 7f, 3f)); 
        }
        else
        {
            // Sadece 15s boyunca yağmur yağar
            yield return new WaitForSeconds(15.0f);
        }

        if (yagmurEfekti != null) yagmurEfekti.SetActive(false);
    }

    IEnumerator YanginEfektiOynat(float sure)
    {
        if (yanginEfekti != null) yanginEfekti.SetActive(true);
        yield return new WaitForSeconds(sure); // 15s yanar
        if (yanginEfekti != null) yanginEfekti.SetActive(false);
    }

    IEnumerator TsunamiDalgaVeSuEfekti(float hedefSuYuksekligi)
    {
        if (tsunamiDalgasi != null)
        {
            tsunamiDalgasi.gameObject.SetActive(true);
            Vector3 dalgaBaslangic = new Vector3(0, 1f, -15f); // Küçük haritaya göre -15'ten başlar
            Vector3 dalgaBitis = new Vector3(0, 1f, 2f);

            float gecenSure = 0f;
            float dalgaSuresi = 4.0f; // 4s'de adaya çarpar
            
            while (gecenSure < dalgaSuresi)
            {
                tsunamiDalgasi.position = Vector3.Lerp(dalgaBaslangic, dalgaBitis, gecenSure / dalgaSuresi);
                gecenSure += Time.deltaTime;
                yield return null;
            }
        }

        if (tsunamiSuyu != null)
        {
            // Su 3s çıkar, 5s adayı kaplar, 3s iner (Toplam 11s + 4s dalga = 15s)
            yield return StartCoroutine(SuYukseltIndir(hedefSuYuksekligi, 3f, 5f, 3f));
        }

        if (tsunamiDalgasi != null) tsunamiDalgasi.gameObject.SetActive(false);
    }

    // Ortak Gelişmiş Su Kontrolcüsü
    IEnumerator SuYukseltIndir(float hedefYukseklik, float cikmaSuresi, float beklemeSuresi, float inmeSuresi)
    {
        Vector3 baslangic = tsunamiSuyu.position;
        Vector3 tepeNoktasi = new Vector3(baslangic.x, hedefYukseklik, baslangic.z);
        float gecenSure = 0;

        // Çıkış
        while (gecenSure < cikmaSuresi) { 
            tsunamiSuyu.position = Vector3.Lerp(baslangic, tepeNoktasi, gecenSure / cikmaSuresi); 
            gecenSure += Time.deltaTime; 
            yield return null; 
        }
        tsunamiSuyu.position = tepeNoktasi;

        // Bekleme
        yield return new WaitForSeconds(beklemeSuresi);

        // İniş
        gecenSure = 0;
        while (gecenSure < inmeSuresi) { 
            tsunamiSuyu.position = Vector3.Lerp(tepeNoktasi, baslangic, gecenSure / inmeSuresi); 
            gecenSure += Time.deltaTime; 
            yield return null; 
        }
        tsunamiSuyu.position = baslangic;
    }
}