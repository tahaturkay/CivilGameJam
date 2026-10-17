using System.Collections;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Temel Bağlantılar")]
    public Transform kamera;              
    public Transform tsunamiSuyu;         

    [Header("Yeni Görsel Efektler (VFX)")]
    public Transform tsunamiDalgasi;      
    [Tooltip("Güneyden gelen tsunami dalgasının kıyı setine çarptığı dünya Z koordinatı.")]
    public float tsunamiSetZ = 10f;
    public float tsunamiBaslangicZ = -45f;
    public float tsunamiBitisZ = 102f;
    public float tsunamiDalgaY = 1f;
    public float tsunamiSuYuksekligi = 1.5f;
    public float selSuYuksekligi = 5f;
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
    private int sonKontrolEdilenGun = 0;
    private bool afetDevamEdiyorMu = false;
    private readonly System.Collections.Generic.HashSet<int> gerceklesenAfetler = new System.Collections.Generic.HashSet<int>();

    public bool AfetDevamEdiyorMu { get { return afetDevamEdiyorMu; } }

    void Update()
    {
        if (afetDevamEdiyorMu || GameManager.Instance == null) return;

        // Afetler sabit sırayla ve sabit günlerde gelir (DengeAyarlari.afetler)
        int gun = GameManager.Instance.gun;
        if (gun != sonKontrolEdilenGun)
        {
            sonKontrolEdilenGun = gun;
            int index = DengeAyarlari.Aktif.BuGunkuAfetIndex(gun);
            if (index >= 0 && !gerceklesenAfetler.Contains(index)) AfetBaslat(index);
        }

#if UNITY_EDITOR
        // Sadece editörde test için: 1-4 tuşları sıradaki afetleri (Yangın, Sel, Deprem, Tsunami) hemen başlatır
        if (Input.GetKeyDown(KeyCode.Alpha1)) AfetBaslat(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AfetBaslat(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AfetBaslat(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) AfetBaslat(3);
#endif
    }

    public void AfetBaslat(int index)
    {
        if (afetDevamEdiyorMu) return;
        var afetler = DengeAyarlari.Aktif.afetler;
        if (index < 0 || index >= afetler.Length) return;

        gerceklesenAfetler.Add(index);
        StartCoroutine(AfetSureciYonet(afetler[index]));
    }

    IEnumerator AfetSureciYonet(DengeAyarlari.AfetAyari afet)
    {
        afetDevamEdiyorMu = true;
        var gm = GameManager.Instance;
        var d = DengeAyarlari.Aktif;

        // 1) Savunma yeterli mi? (kontrol edilen yapıların hepsi gereken seviyede ve hasarsız olmalı)
        var yetersizler = new System.Collections.Generic.List<BinaTuru>();
        foreach (BinaTuru b in afet.kontrolEdilen)
            if (gm.Level(b) < afet.gerekenLevel || gm.Hasarli(b)) yetersizler.Add(b);
        bool yeterli = yetersizler.Count == 0;

        if (MusicPlayer.Instance != null) MusicPlayer.Instance.AfetMuzigiBaslat(afet.afet);

        // 2) Görsel efekt
        switch (afet.afet)
        {
            case AfetTuru.Deprem:
                StartCoroutine(KameraSalla(15.0f, 0.4f));
                yield return new WaitForSeconds(5.0f);
                if (temelBinalar != null)
                    foreach (Transform bina in temelBinalar)
                        TozEfekti(yetersizler.Contains(BinaTuru.TemelBina), bina);
                TozEfekti(yetersizler.Contains(BinaTuru.Okul), okulObj);
                TozEfekti(yetersizler.Contains(BinaTuru.Akut), akutObj);
                TozEfekti(yetersizler.Contains(BinaTuru.Itfaiye), itfaiyeObj);
                yield return new WaitForSeconds(10.0f);
                break;

            case AfetTuru.Sel:
                yield return StartCoroutine(SelEfektiOynat(!yeterli));
                break;

            case AfetTuru.Yangin:
                yield return StartCoroutine(YanginEfektiOynat(15.0f, !yeterli));
                break;

            case AfetTuru.Tsunami:
                yield return StartCoroutine(TsunamiDalgaVeSuEfekti(tsunamiSuYuksekligi, yeterli));
                break;
        }

        // 3) Hasar: depremde seviyesi yetmeyenler, diğerlerinde savunma yetersizse listedeki yapılar
        var hasarAlanlar = afet.afet == AfetTuru.Deprem
            ? yetersizler
            : (yeterli ? new System.Collections.Generic.List<BinaTuru>() : new System.Collections.Generic.List<BinaTuru>(afet.hasarAlan));
        foreach (BinaTuru b in hasarAlanlar) HasarVer(gm, b);

        // 4) Can kaybı = Baz × (savunma yeterliyse 0.2) × (1 − Farkındalık × 0.006) × (1 − AKUT × 0.10)
        float baz = afet.bazKayip * (yeterli ? d.savunmaYeterliCarpani : 1f);
        int akutEtkiLevel = gm.akutHasarli ? 0 : gm.akutLevel;
        float koruma = (1f - gm.farkindalikYuzdesi * d.farkindalikEtkisi) * (1f - akutEtkiLevel * d.akutEtkisi);
        int vefat = Mathf.Min(gm.nufus, Mathf.RoundToInt(baz * Mathf.Clamp01(koruma)));
        int kurtarilan = Mathf.Max(0, Mathf.RoundToInt(baz) - vefat);
        gm.nufus -= vefat;

        // 5) Rapor ve istatistikler
        gm.sonAfetTuru = afet.afet;
        gm.sonSavunmaYeterli = yeterli;
        gm.sonHasarliBinaSayisi = hasarAlanlar.Count;
        gm.sonVefatSayisi = vefat;
        gm.sonKurtarilanSayisi = kurtarilan;
        gm.toplamVefat += vefat;
        gm.toplamKurtarilan += kurtarilan;
        gm.sonAfetMesaji = yeterli ? "Önlemler işe yaradı!" : "Savunma yetersiz kaldı.";
        gm.afetOzeti += AfetRaporu.AfetAdi(afet.afet) + ": " + (yeterli ? "Korundu" : "Hasar aldı") + " (" + vefat + " vefat)\n";

        if (MusicPlayer.Instance != null) MusicPlayer.Instance.NormalMuzigeDon();

        gm.afetOlduMu = true; // afet raporu açılır, Devam'a basınca oyun sürer
        afetDevamEdiyorMu = false;
    }

    bool ItfaiyeMi(Transform bina)
    {
        if (bina == itfaiyeObj) return true;
        var bv = bina.GetComponentInParent<BuildingVisuals>();
        if (bv == null) bv = bina.GetComponentInChildren<BuildingVisuals>();
        return bv != null && bv.binaTuru == BinaTuru.Itfaiye;
    }

    // Seviyesi 1'den büyükse bir düşer; zaten 1 ise "hasarlı" olur
    void HasarVer(GameManager gm, BinaTuru bina)
    {
        int level = gm.Level(bina);
        if (level > 1) gm.LevelAyarla(bina, level - 1);
        else gm.HasarliAyarla(bina, true);
    }

    void TozEfekti(bool hasarAlacak, Transform binaObj)
    {
        if (hasarAlacak && binaObj != null)
            StartCoroutine(BinaCokmeEfekti(binaObj, false)); // sadece toz; bina yerinden oynamaz
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
            yield return StartCoroutine(SuYukseltIndir(selSuYuksekligi, 6f, 7f, 3f));
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
                if (bina != null && !ItfaiyeMi(bina)) // itfaiye binası yangını söndüren taraf, kendisi yanmaz
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
            Vector3 orijinalScale = tsunamiDalgasi.localScale;
            float baslangicX = tsunamiDalgasi.position.x;

            tsunamiDalgasi.gameObject.SetActive(true);
            
            // Dalganın başlangıç ve bitiş noktaları
            Vector3 dalgaBaslangic = new Vector3(baslangicX, tsunamiDalgaY, tsunamiBaslangicZ);
            Vector3 dalgaBitis = new Vector3(baslangicX, tsunamiDalgaY, tsunamiBitisZ);

            float setinZPozisyonu = tsunamiSetZ;

            // Dalganın sete yüzde kaçlık ilerlemede çarpacağını otomatik hesaplıyoruz!
            float carpmaYuzdesi = Mathf.InverseLerp(dalgaBaslangic.z, dalgaBitis.z, setinZPozisyonu);

            float gecenSure = 0f;
            float dalgaSuresi = 15.0f; // Saniye uzadı
            
            while (gecenSure < dalgaSuresi)
            {
                float ilerleme = gecenSure / dalgaSuresi; 
                Vector3 mevcutPozisyon = Vector3.Lerp(dalgaBaslangic, dalgaBitis, ilerleme);

                if (ilerleme >= carpmaYuzdesi)
                {
                    if (setYeterliMi)
                    {
                        // EĞER SET YETERLİYSE:
                        // 1. Dalga seti delip geçemesin! İleri gitmeyi durdursun (Z'yi sete sabitliyoruz)
                        mevcutPozisyon.z = setinZPozisyonu; 

                        // 2. Çarptığı an duvarda parçalanıp hızla küçülerek yerin dibine girsin
                        float erimeOrani = Mathf.Clamp01((ilerleme - carpmaYuzdesi) * 5f); 
                        mevcutPozisyon.y = Mathf.Lerp(tsunamiDalgaY, -10f, erimeOrani);
                        float yeniScaleY = Mathf.Lerp(orijinalScale.y, 0.05f, erimeOrani);
                        tsunamiDalgasi.localScale = new Vector3(orijinalScale.x, yeniScaleY, orijinalScale.z);
                    }
                    else
                    {
                        // EĞER SET YETERSİZSE:
                        // Dalga şehirden geçerken ilerlemeye devam eder, ancak enerjisini kaybedip
                        // kademeli olarak küçülür (Y scale azalır) ve yavaşça yere gömülür (Y pozisyonu iner)
                        float sehirIlerleme = Mathf.InverseLerp(carpmaYuzdesi, 1f, ilerleme);
                        float gomulmeOrani = Mathf.SmoothStep(0f, 1f, sehirIlerleme);

                        mevcutPozisyon.y = Mathf.Lerp(tsunamiDalgaY, -10f, gomulmeOrani);
                        float yeniScaleY = Mathf.Lerp(orijinalScale.y, 0.05f, gomulmeOrani);
                        tsunamiDalgasi.localScale = new Vector3(orijinalScale.x, yeniScaleY, orijinalScale.z);
                    }
                }
                else
                {
                    tsunamiDalgasi.localScale = orijinalScale;
                }

                tsunamiDalgasi.position = mevcutPozisyon;
                gecenSure += Time.deltaTime;
                yield return null;
            }

            // Süreç sonunda dalgayı kapatmadan önce orijinal boyutunu geri yükle
            tsunamiDalgasi.localScale = orijinalScale;
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
        if (tsunamiSuyu == null) yield break;
        
        // 1. Suyu görünür yap
        tsunamiSuyu.gameObject.SetActive(true);

        // 2. Başlangıç yüksekliğini kesin olarak yerin dibi (-4f) yapalım
        Vector3 baslangic = tsunamiSuyu.position;
        //baslangic.y = 4f; 
        tsunamiSuyu.position = baslangic;

        Vector3 tepeNoktasi = new Vector3(baslangic.x, hedefYukseklik, baslangic.z);
        float gecenSure = 0;

        // Çıkış (Sular yükseliyor)
        while (gecenSure < cikmaSuresi) { 
            tsunamiSuyu.position = Vector3.Lerp(baslangic, tepeNoktasi, gecenSure / cikmaSuresi); 
            gecenSure += Time.deltaTime; 
            yield return null; 
        }
        tsunamiSuyu.position = tepeNoktasi;

        // Bekleme (Şehir sular altında)
        yield return new WaitForSeconds(beklemeSuresi);

        // İniş (Sular çekiliyor)
        gecenSure = 0;
        while (gecenSure < inmeSuresi) { 
            tsunamiSuyu.position = Vector3.Lerp(tepeNoktasi, baslangic, gecenSure / inmeSuresi); 
            gecenSure += Time.deltaTime; 
            yield return null; 
        }
        
        // Su yavaşça -4f noktasına (yerin dibine) indi ve orada duracak. 
        // Kapatma kodu sildiğimiz için aniden ekrandan silinmeyecek!
        tsunamiSuyu.position = baslangic;
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
