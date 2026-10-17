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
    // --- YENİ YANGIN SİSTEMİ ---
    public GameObject yanginPrefab;        // Klasördeki Yangın Prefabını buraya atacağız
    public Transform[] yanacakBinalar;     // Yangın çıkmasını istediğin binaları buraya sürükle

    // --- YENİ DEPREM SİSTEMİ ---
    [Header("Deprem Efektleri")]
    public GameObject tozBulutuPrefab;     // Toz/Duman Particle Prefab'ı
    public Transform[] temelBinalar;
    public Transform okulObj;
    public Transform akutObj;
    public Transform itfaiyeObj;
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
                
                // 1. Kamerayı 15 saniye boyunca salla (yield yok, paralel çalışır)
                StartCoroutine(KameraSalla(15.0f, 0.4f)); 

                // 2. İlk 10 saniye sadece sarsıntı olsun
                yield return new WaitForSeconds(5.0f);

                // 3. Son 5 saniyeye girdik. Hasar alanları kontrol et ve efektleri başlat
                // Temel binaları tek tek kontrol et
                if (temelBinalar != null)
                {
                    foreach (Transform bina in temelBinalar)
                    {
                        CheckAndTriggerCollapse(GameManager.Instance.temelBinaLevel, gerekenLevel, bina);
                    }
                }
                CheckAndTriggerCollapse(GameManager.Instance.okulLevel, gerekenLevel, okulObj);
                CheckAndTriggerCollapse(GameManager.Instance.akutLevel, gerekenLevel, akutObj);
                CheckAndTriggerCollapse(GameManager.Instance.itfaiyeLevel, gerekenLevel, itfaiyeObj);

                // 4. Kalan 5 saniyeyi bekle (Bu sırada Level 1 binalar çökecek)
                yield return new WaitForSeconds(10.0f);

                // 5. Süre bitti, levelleri düşür
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
                
                // İtfaiye yetersizse yangın binalara sıçrar!
                bool yanginSivrildiMi = GameManager.Instance.itfaiyeLevel < gerekenLevel;
                
                yield return StartCoroutine(YanginEfektiOynat(15.0f, yanginSivrildiMi));

                if (yanginSivrildiMi) {
                    GameManager.Instance.temelBinaLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.temelBinaLevel);
                    GameManager.Instance.okulLevel = GameManager.Instance.LeveliniBirDusur(GameManager.Instance.okulLevel);
                    hasarAldiMi = true;
                }
                break;

            case 3: // TSUNAMİ (15 Saniye)
                afetAdi = "TSUNAMİ (Gereken Lvl: " + gerekenLevel + ")";
                
                // Set yeterli mi kontrol et
                bool setYeterliMi = GameManager.Instance.setLevel >= gerekenLevel;
                
                // Efekti başlat ve setin yeterli olup olmadığını bildir
                yield return StartCoroutine(TsunamiDalgaVeSuEfekti(1.5f, setYeterliMi)); 

                // Hasar sadece set yetersizse alınır
                if (!setYeterliMi) {
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

    IEnumerator YanginEfektiOynat(float sure, bool hasarVarMi)
    {
        System.Collections.Generic.List<GameObject> aktifAtesler = new System.Collections.Generic.List<GameObject>();

        // 1. AŞAMA: İlk 5 saniyede teker teker alev alma
        if (yanacakBinalar.Length > 0)
        {
            float yanmaAraligi = 5.0f / yanacakBinalar.Length; // 5 saniyeyi bina sayısına böl
            
            foreach (Transform bina in yanacakBinalar)
            {
                if (bina != null)
                {
                    // Başlangıç olarak binanın kendi pozisyonunu al
                    Vector3 tepeNoktasi = bina.position;

                    // Binanın üzerindeki Collider'ı (kutu sınırlarını) bul
                    Collider binaCollider = bina.GetComponent<Collider>();
                    
                    if (binaCollider != null)
                    {
                        // Collider varsa, ateşin Y (yükseklik) değerini binanın en üst noktasına sabitle
                        tepeNoktasi.y = binaCollider.bounds.max.y;
                    }
                    else
                    {
                        // Eğer arkadaşın binaya Collider eklemeyi unuttuysa, yedek olarak biraz yukarı kaldır
                        tepeNoktasi.y += 2f; 
                    }

                    // Ateşi tam çatıda (tepeNoktasi) oluştur
                    GameObject ates = Instantiate(yanginPrefab, tepeNoktasi, Quaternion.identity);
                    ates.SetActive(true);
                    aktifAtesler.Add(ates);
                }
                yield return new WaitForSeconds(yanmaAraligi);
            }
        }
        else
        {
            yield return new WaitForSeconds(5.0f); // Bina yoksa ilk 5 saniyeyi boş geç
        }

        // 2. AŞAMA: Orta kısımdaki bekleme süresi (Binalar alev alev yanıyor)
        // Toplam 15 saniyenin ilk 5'i yandı, son 5'i sönecek. Ortaya 5 saniye kalır.
        float ortaSure = sure - 10.0f; 
        if (ortaSure > 0) yield return new WaitForSeconds(ortaSure);

        // 3. AŞAMA: Son 5 saniye (Söndürme veya Yanmaya Devam Etme)
        if (!hasarVarMi) 
        {
            // İTFAİYE GÜÇLÜ: Ateşleri teker teker söndür
            Debug.Log("İtfaiye müdahale ediyor! Ateşler teker teker söndürülüyor.");
            if (aktifAtesler.Count > 0)
            {
                float sondurmeAraligi = 5.0f / aktifAtesler.Count;
                
                // Listeyi sondan başa doğru silerek söndürüyoruz
                for (int i = aktifAtesler.Count - 1; i >= 0; i--)
                {
                    Destroy(aktifAtesler[i]);
                    aktifAtesler.RemoveAt(i);
                    yield return new WaitForSeconds(sondurmeAraligi);
                }
            }
            else
            {
                yield return new WaitForSeconds(5.0f);
            }
        }
        else 
        {
            // İTFAİYE YETERSİZ: Binalar yanmaya devam eder
            Debug.Log("İtfaiye yetersiz! Binalar alev alev yanmaya devam ediyor.");
            yield return new WaitForSeconds(5.0f);
        }

        // GÜVENLİK TEMİZLİĞİ: Süre bitiminde sahnede kalan son ateşleri de temizle
        foreach (GameObject ates in aktifAtesler)
        {
            if (ates != null) Destroy(ates);
        }
        aktifAtesler.Clear();
    }

    IEnumerator TsunamiDalgaVeSuEfekti(float hedefSuYuksekligi, bool setYeterliMi)
    {
        if (tsunamiDalgasi != null)
        {
            tsunamiDalgasi.gameObject.SetActive(true);
            
            // Dalganın başlangıç ve bitiş noktaları
            Vector3 dalgaBaslangic = new Vector3(0, 1f, -45f); 
            Vector3 dalgaBitis = new Vector3(0, 1f, 102f);

            // *** YENİ AYAR: Setin Haritadaki Z Pozisyonu ***
            // Unity'den set modeline tıkla, Z değerine bak ve buraya yaz (Örn: -15f)
            float setinZPozisyonu = 10f; 

            // Dalganın sete yüzde kaçlık ilerlemede çarpacağını otomatik hesaplıyoruz!
            float carpmaYuzdesi = Mathf.InverseLerp(dalgaBaslangic.z, dalgaBitis.z, setinZPozisyonu);

            float gecenSure = 0f;
            float dalgaSuresi = 15.0f; // Saniye uzadı
            
            while (gecenSure < dalgaSuresi)
            {
                float ilerleme = gecenSure / dalgaSuresi; 
                Vector3 mevcutPozisyon = Vector3.Lerp(dalgaBaslangic, dalgaBitis, ilerleme);

                // EĞER SET YETERLİYSE VE DALGA SETE ULAŞTIYSA:
                if (setYeterliMi && ilerleme >= carpmaYuzdesi)
                {
                    // 1. Dalga seti delip geçemesin! İleri gitmeyi durdursun (Z'yi sete sabitliyoruz)
                    mevcutPozisyon.z = setinZPozisyonu; 

                    // 2. Çarptığı an duvarda parçalanıp hızla yerin dibine (Y: -4) girsin
                    // * 5f çarpanı erimenin çok hızlı (şak diye) olmasını sağlar
                    float erimeHizi = (ilerleme - carpmaYuzdesi) * 5f; 
                    mevcutPozisyon.y = Mathf.Lerp(1f, -4f, Mathf.Clamp01(erimeHizi)); 
                }

                tsunamiDalgasi.position = mevcutPozisyon;
                gecenSure += Time.deltaTime;
                yield return null;
            }
        }

        // Dalga çarpması bitti. Şimdi su basacak mı?
        if (!setYeterliMi)
        {
            // SET YETERSİZ: Arkasından su kopyası yükselir ve şehri basar
            if (tsunamiSuyu != null)
            {
                // Dalga süresini 15 saniyeye çektiğin için su yükselme sürelerini de ona göre dengeleyebilirsin
                yield return StartCoroutine(SuYukseltIndir(hedefSuYuksekligi, 3f, 5f, 3f));
            }
        }
        else
        {
            // SET YETERLİ: Su basmaz! 
            Debug.Log("Setler dalgayı durdurdu! Şehri su basmadı.");
            // Dalga 15 saniye ayarladığın için ve su basmadığı için beklemeye gerek yok, süreci tamamla.
        }

        if (tsunamiDalgasi != null) tsunamiDalgasi.gameObject.SetActive(false);
    }

    // Ortak Gelişmiş Su Kontrolcüsü
    IEnumerator SuYukseltIndir(float hedefYukseklik, float cikmaSuresi, float beklemeSuresi, float inmeSuresi)
    {
        // 1. HAREKETE BAŞLAMADAN ÖNCE SUYU GÖRÜNÜR YAP!
        if (tsunamiSuyu != null) tsunamiSuyu.gameObject.SetActive(true);

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

        // 2. SU ÇEKİLDİKTEN SONRA TEKRAR GİZLE!
        if (tsunamiSuyu != null) tsunamiSuyu.gameObject.SetActive(false);
    }

    // Binanın hasar durumunu kontrol eder ve sadece toz/duman çıkartır
    void CheckAndTriggerCollapse(int binaLevel, int gerekenLevel, Transform binaObj)
    {
        if (binaLevel < gerekenLevel && binaObj != null)
        {
            // Bina leveli 1'den büyükse çökecek (true), 1 ise sadece toz çıkacak (false)
            bool cokecekMi = (binaLevel > 1);
            StartCoroutine(BinaCokmeEfekti(binaObj, cokecekMi));
        }
    }

    // Toz çıkarma ve duruma göre yerin dibine girme animasyonu
    IEnumerator BinaCokmeEfekti(Transform bina, bool cokecekMi)
    {
        // 1. Toz bulutunu oluştur ve binanın boyutuna göre büyüt
        if (tozBulutuPrefab != null)
        {
            Collider col = bina.GetComponent<Collider>();
            Vector3 zeminPos = col != null ? new Vector3(bina.position.x, col.bounds.min.y + 0.5f, bina.position.z) : bina.position + new Vector3(0, 0.5f, 0);
            
            GameObject toz = Instantiate(tozBulutuPrefab, zeminPos, Quaternion.identity);
            
            // YENİ: Orantılı ve Sınırlı Büyütme
            if (col != null)
            {
                // Binanın en geniş tarafını bul
                float maxGenislik = Mathf.Max(col.bounds.size.x, col.bounds.size.z);
                
                // Formül: Genişliği biraz yumuşat (0.4 ile çarp) ve sınırlar arasına hapset
                // Min: 1.0f (Normal boy) | Max: 2.5f (En fazla 2.5 kat büyüyebilir)
                float orantiliScale = Mathf.Clamp(maxGenislik * 0.2f, 1.0f, 1.5f);
                
                toz.transform.localScale = new Vector3(orantiliScale, orantiliScale, orantiliScale);
            }

            toz.SetActive(true);
            Destroy(toz, 10.0f); 
        }

        // 2. Eğer bina Level 1'den büyükse son 5 saniyede çöksün
        if (cokecekMi)
        {
            Vector3 baslangic = bina.position;
            Vector3 bitis = baslangic + new Vector3(0, -20f, 0); // 4 birim aşağı göm
            
            float gecenSure = 0;
            while(gecenSure < 5.0f)
            {
                bina.position = Vector3.Lerp(baslangic, bitis, gecenSure / 5.0f);
                gecenSure += Time.deltaTime;
                yield return null;
            }
        }
    }
}