using System;
using UnityEngine;

public enum AfetTuru { Yangin, Sel, Deprem, Tsunami }

// Oyunun bütün sayıları burada: fiyatlar, nüfus, gelir, afetler, quiz, karne.
// Denge ayarı için kod değiştirmeye gerek yok: Assets/Resources/DengeAyarlari.asset'i seçip Inspector'dan değiştir.
// Her yerden DengeAyarlari.Aktif diye ulaşılır.
[CreateAssetMenu(fileName = "DengeAyarlari", menuName = "Oyun/Denge Ayarlari")]
public class DengeAyarlari : ScriptableObject
{
    [Serializable]
    public struct BinaFiyati
    {
        public BinaTuru bina;
        public int lv2Fiyat;   // Level 1 → 2
        public int lv3Fiyat;   // Level 2 → 3
        public int onarimFiyat;
    }

    [Serializable]
    public struct AfetAyari
    {
        public AfetTuru afet;
        public int gun;                    // hangi gün gelir
        public BinaTuru[] kontrolEdilen;   // savunma için bakılan yapılar
        public int gerekenLevel;           // bu yapıların hepsi en az bu seviyede olmalı
        public BinaTuru[] hasarAlan;       // savunma yetersizse seviyesi düşen yapılar (Deprem'de: yetersiz kalanlar)
        public int bazKayip;               // savunma yetersizse ölecek kişi (farkındalık/AKUT öncesi)
    }

    [Header("Zaman")]
    [Tooltip("1x hızda bir oyun gününün süresi (saniye). 2x'te yarısı, 3x'te üçte biri.")]
    public float birGunSuresi = 12f;
    public int toplamGun = 20;
    [Range(1,3)] public int baslangicHizi = 2;

    [Header("Başlangıç Değerleri")]
    public int baslangicPara = 500;
    public int baslangicNufus = 1000;
    public float baslangicFarkindalik = 10f;

    [Header("Nüfus ve Gelir")]
    [Tooltip("Konut Lv1, Lv2, Lv3 için nüfus kapasitesi")]
    public int[] konutKapasitesi = { 1000, 1300, 1600 };
    [Tooltip("Nüfus kapasitenin altındaysa her gün bu kadar artar (göç)")]
    public int gunlukGoc = 30;
    [Tooltip("Günlük gelir = nüfus × bu değer")]
    public float kisiBasiGunlukGelir = 0.1f;
    [Tooltip("Konut seviyesinin vergi ve yerel üretim geliri çarpanı")]
    public float[] konutGelirCarpanlari = { 1f, 1.35f, 1.75f };

    [Header("Binalar")]
    public int maxLevel = 3;
    public BinaFiyati[] binaFiyatlari =
    {
        new BinaFiyati { bina = BinaTuru.TemelBina, lv2Fiyat = 200, lv3Fiyat = 400, onarimFiyat = 100 },
        new BinaFiyati { bina = BinaTuru.Okul,      lv2Fiyat = 200, lv3Fiyat = 400, onarimFiyat = 100 },
        new BinaFiyati { bina = BinaTuru.Akut,      lv2Fiyat = 250, lv3Fiyat = 500, onarimFiyat = 125 },
        new BinaFiyati { bina = BinaTuru.Itfaiye,   lv2Fiyat = 250, lv3Fiyat = 500, onarimFiyat = 125 },
        new BinaFiyati { bina = BinaTuru.Altyapi,   lv2Fiyat = 300, lv3Fiyat = 550, onarimFiyat = 150 },
        new BinaFiyati { bina = BinaTuru.DenizSeti, lv2Fiyat = 350, lv3Fiyat = 650, onarimFiyat = 175 },
    };

    [Header("Afetler (sırayla)")]
    public AfetAyari[] afetler =
    {
        new AfetAyari
        {
            afet = AfetTuru.Yangin, gun = 5, gerekenLevel = 2, bazKayip = 120,
            kontrolEdilen = new[] { BinaTuru.Itfaiye },
            hasarAlan = new[] { BinaTuru.TemelBina, BinaTuru.Okul },
        },
        new AfetAyari
        {
            afet = AfetTuru.Sel, gun = 10, gerekenLevel = 2, bazKayip = 150,
            kontrolEdilen = new[] { BinaTuru.Altyapi },
            hasarAlan = new[] { BinaTuru.Altyapi, BinaTuru.TemelBina },
        },
        new AfetAyari
        {
            afet = AfetTuru.Deprem, gun = 15, gerekenLevel = 2, bazKayip = 200,
            kontrolEdilen = new[] { BinaTuru.TemelBina, BinaTuru.Okul, BinaTuru.Akut, BinaTuru.Itfaiye },
            hasarAlan = new BinaTuru[0], // depremde sadece seviyesi yetmeyenler hasar alır
        },
        new AfetAyari
        {
            afet = AfetTuru.Tsunami, gun = 20, gerekenLevel = 3, bazKayip = 250,
            kontrolEdilen = new[] { BinaTuru.DenizSeti },
            hasarAlan = new[] { BinaTuru.DenizSeti, BinaTuru.TemelBina },
        },
    };

    [Header("Can Kaybı")]
    [Tooltip("Savunma yeterliyse baz kayıp bu oranla çarpılır (0.2 = %80 azalır)")]
    public float savunmaYeterliCarpani = 0.2f;
    [Tooltip("Farkındalığın her %1'i can kaybını bu kadar azaltır (0.006 → %100'de %60)")]
    public float farkindalikEtkisi = 0.006f;
    [Tooltip("AKUT'un her seviyesi can kaybını bu kadar azaltır (0.10 → Lv3'te %30)")]
    public float akutEtkisi = 0.10f;

    [Header("Quiz")]
    public float dogruCevapOdulu = 25f;
    [Tooltip("Okul Lv1, Lv2, Lv3 iken ulaşılabilecek en yüksek farkındalık")]
    public float[] okulFarkindalikTavani = { 40f, 70f, 100f };

    [Header("Oyun Sonu")]
    [Tooltip("Nüfus bu sayıya ya da altına düşerse oyun kaybedilir")]
    public int kaybetmeNufusu = 200;
    [Tooltip("Toplam vefat bu sayılara eşit ya da altındaysa: 3 yıldız, 2 yıldız, 1 yıldız")]
    public int[] yildizEsikleri = { 80, 180, 320 };

    // ---------- Erişim ----------

    static DengeAyarlari aktif;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache(){aktif=null;}

    public static DengeAyarlari Aktif
    {
        get
        {
            if (aktif == null)
            {
                aktif = Resources.Load<DengeAyarlari>("DengeAyarlari");
                if (aktif == null)
                {
                    Debug.LogWarning("DengeAyarlari: Assets/Resources/DengeAyarlari.asset bulunamadı, varsayılan değerler kullanılıyor.");
                    aktif = CreateInstance<DengeAyarlari>();
                }
            }
            return aktif;
        }
    }

    // ---------- Yardımcılar ----------

    // Mevcut seviyeden bir üste çıkmanın fiyatı. Maks seviyedeyse -1.
    public int YukseltmeMaliyeti(BinaTuru bina, int mevcutLevel)
    {
        if (mevcutLevel >= maxLevel) return -1;
        BinaFiyati f = Fiyat(bina);
        return mevcutLevel <= 1 ? f.lv2Fiyat : f.lv3Fiyat;
    }

    public int OnarimMaliyeti(BinaTuru bina)
    {
        return Fiyat(bina).onarimFiyat;
    }

    public int KonutKapasitesi(int konutLevel)
    {
        int i = Mathf.Clamp(konutLevel - 1, 0, konutKapasitesi.Length - 1);
        return konutKapasitesi[i];
    }

    public float FarkindalikTavani(int okulLevel)
    {
        int i = Mathf.Clamp(okulLevel - 1, 0, okulFarkindalikTavani.Length - 1);
        return okulFarkindalikTavani[i];
    }

    // O gün bir afet varsa index'ini döndürür, yoksa -1
    public int BuGunkuAfetIndex(int gun)
    {
        for (int i = 0; i < afetler.Length; i++)
            if (afetler[i].gun == gun) return i;
        return -1;
    }

    // Bir sonraki afete kaç gün kaldı (afet kalmadıysa -1)
    public int SonrakiAfeteKalanGun(int gun)
    {
        foreach (AfetAyari a in afetler)
            if (a.gun >= gun) return a.gun - gun;
        return -1;
    }

    BinaFiyati Fiyat(BinaTuru bina)
    {
        foreach (BinaFiyati f in binaFiyatlari)
            if (f.bina == bina) return f;
        Debug.LogWarning("DengeAyarlari: " + bina + " için fiyat tanımlı değil.");
        return new BinaFiyati { bina = bina, lv2Fiyat = 9999, lv3Fiyat = 9999, onarimFiyat = 9999 };
    }
}
