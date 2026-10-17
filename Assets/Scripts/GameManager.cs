using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Her yerden GameManager.Instance diyerek ulaşmak için
    public event Action<int> GunlukGelirEklendi;
    public event Action<BinaTuru> YapiIslemiTamamlandi;

    [Header("Genel Oyun Verileri")]
    public int gun = 1;
    public int para = 500;
    public int nufus = 1000;
    public float farkindalikYuzdesi = 10f; // 0 ile 100 arası (Okul quizi artırır)

    [Header("Bina Levelleri (En az 1 olur)")]
    public int temelBinaLevel = 1; // Ana gelir kaynağı ve deprem hedefi
    public int okulLevel = 1;      // Quiz açar, depremde hasar alabilir
    public int akutLevel = 1;      // Can kaybını azaltır
    public int itfaiyeLevel = 1;   // Yangını önler
    public int setLevel = 1;       // Tsunamiyi önler
    public int altyapiLevel = 1;   // Seli önler (Yol ve altyapı)

    [Header("Hasarlı Binalar (onarılana kadar çalışmaz)")]
    public bool temelBinaHasarli;
    public bool okulHasarli;
    public bool akutHasarli;
    public bool itfaiyeHasarli;
    public bool setHasarli;
    public bool altyapiHasarli;
    [SerializeField] private AfetTuru[] hasarTurleri = new AfetTuru[6];

    [Header("Zaman")]
    public int hizCarpani = 1;              // 1x, 2x, 3x
    public bool kullaniciDurdurdu = false;  // HUD'daki Duraklat butonu

    [Header("Panel ve Durum Kontrolleri")]
    public bool oyunDurduMu = false; // Popup, quiz veya afet raporu açıkken zamanı durdurmak için
    public bool afetSuruyor;

    [Header("Afet Özet Verileri (UI İçin)")]
    public bool afetOlduMu = false;       // true olunca afet raporu açılır, Devam'a basınca false olur
    public string sonAfetMesaji = "";
    public int sonVefatSayisi = 0;
    public AfetTuru sonAfetTuru;
    public bool sonSavunmaYeterli;
    public int sonHasarliBinaSayisi;
    public int sonKurtarilanSayisi;       // farkındalık ve AKUT sayesinde

    [Header("İstatistikler (Oyun Sonu Ekranı)")]
    public int toplamVefat;
    public int toplamKurtarilan;
    public int toplamKazanc;
    public int toplamHarcama;
    public int yukseltmeSayisi;
    public int dogruCevapSayisi;
    public float enYuksekFarkindalik;
    public string afetOzeti = "";         // "Yangın: Korundu" gibi satırlar (oyun sonu ekranı için)

    [Header("Oyun Sonu")]
    public bool oyunBittiMi = false;
    public bool kazandiMi = false;

    // Gün sayacı bu true iken ilerlemez
    public bool ZamanDurduMu
    {
        get { return oyunDurduMu || kullaniciDurdurdu || afetSuruyor || afetOlduMu || oyunBittiMi; }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Başlangıç değerleri tek yerden: DengeAyarlari
        var d = DengeAyarlari.Aktif;
        para = d.baslangicPara;
        nufus = d.baslangicNufus;
        farkindalikYuzdesi = d.baslangicFarkindalik;
        enYuksekFarkindalik = farkindalikYuzdesi;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Bina leveli düşürme yardımcı fonksiyonu (1'in altına düşmez)
    public int LeveliniBirDusur(int mevcutLevel)
    {
        if (mevcutLevel > 1) return mevcutLevel - 1;
        return 1;
    }

    // ---------- Bina türüne göre okuma/yazma (switch'leri her script'te tekrar yazmamak için) ----------

    public int Level(BinaTuru bina)
    {
        switch (bina)
        {
            case BinaTuru.TemelBina: return temelBinaLevel;
            case BinaTuru.Okul:      return okulLevel;
            case BinaTuru.Akut:      return akutLevel;
            case BinaTuru.Itfaiye:   return itfaiyeLevel;
            case BinaTuru.DenizSeti: return setLevel;
            default:                 return altyapiLevel;
        }
    }

    public void LevelAyarla(BinaTuru bina, int level)
    {
        level = Mathf.Clamp(level, 1, DengeAyarlari.Aktif.maxLevel);
        switch (bina)
        {
            case BinaTuru.TemelBina: temelBinaLevel = level; break;
            case BinaTuru.Okul:      okulLevel = level;      break;
            case BinaTuru.Akut:      akutLevel = level;      break;
            case BinaTuru.Itfaiye:   itfaiyeLevel = level;   break;
            case BinaTuru.DenizSeti: setLevel = level;       break;
            default:                 altyapiLevel = level;   break;
        }
    }

    public bool Hasarli(BinaTuru bina)
    {
        switch (bina)
        {
            case BinaTuru.TemelBina: return temelBinaHasarli;
            case BinaTuru.Okul:      return okulHasarli;
            case BinaTuru.Akut:      return akutHasarli;
            case BinaTuru.Itfaiye:   return itfaiyeHasarli;
            case BinaTuru.DenizSeti: return setHasarli;
            default:                 return altyapiHasarli;
        }
    }

    public void HasarliAyarla(BinaTuru bina, bool hasarli)
    {
        switch (bina)
        {
            case BinaTuru.TemelBina: temelBinaHasarli = hasarli; break;
            case BinaTuru.Okul:      okulHasarli = hasarli;      break;
            case BinaTuru.Akut:      akutHasarli = hasarli;      break;
            case BinaTuru.Itfaiye:   itfaiyeHasarli = hasarli;   break;
            case BinaTuru.DenizSeti: setHasarli = hasarli;       break;
            default:                 altyapiHasarli = hasarli;   break;
        }
    }

    public AfetTuru HasarTuru(BinaTuru bina)
    {
        return hasarTurleri != null && (int)bina < hasarTurleri.Length ? hasarTurleri[(int)bina] : AfetTuru.Yangin;
    }

    // Seviye kaybı ve onarım ihtiyacı ayrı durumlar: üst seviyeden düşen yapı da onarılır.
    public void HasarVer(BinaTuru bina, AfetTuru sebep)
    {
        if (hasarTurleri == null || hasarTurleri.Length != 6) Array.Resize(ref hasarTurleri, 6);
        hasarTurleri[(int)bina] = sebep;
        LevelAyarla(bina, Level(bina) - 1);
        HasarliAyarla(bina, true);
    }

    public void YapiIsleminiBildir(BinaTuru bina) { YapiIslemiTamamlandi?.Invoke(bina); }

    public void GelirKaydet(int miktar)
    {
        para += miktar;
        toplamKazanc += miktar;
        GunlukGelirEklendi?.Invoke(miktar);
    }

    // Afet raporundaki Devam butonu çağırır: oyun devam eder ya da biter
    public void AfetSonrasiDevam()
    {
        afetOlduMu = false;
        var d = DengeAyarlari.Aktif;

        if (nufus <= d.kaybetmeNufusu)
        {
            oyunBittiMi = true;
            kazandiMi = false;
        }
        else if (gun >= d.toplamGun)
        {
            oyunBittiMi = true;
            kazandiMi = true;
        }
    }

    // Toplam vefata göre 0-3 yıldız (DengeAyarlari.yildizEsikleri)
    public int Yildiz()
    {
        if (!kazandiMi) return 0;
        int[] esik = DengeAyarlari.Aktif.yildizEsikleri;
        for (int i = 0; i < esik.Length; i++)
            if (toplamVefat <= esik[i]) return 3 - i;
        return 0;
    }

    // Farkındalığı okul seviyesinin tavanını aşmadan artırır. Gerçekten artan miktarı döndürür.
    public float FarkindalikEkle(float miktar)
    {
        float tavan = DengeAyarlari.Aktif.FarkindalikTavani(okulLevel);
        float onceki = farkindalikYuzdesi;
        farkindalikYuzdesi = Mathf.Clamp(farkindalikYuzdesi + miktar, 0f, Mathf.Max(tavan, onceki));
        enYuksekFarkindalik = Mathf.Max(enYuksekFarkindalik, farkindalikYuzdesi);
        return farkindalikYuzdesi - onceki;
    }
}
