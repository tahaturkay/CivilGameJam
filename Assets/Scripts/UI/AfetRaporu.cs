using UnityEngine;
using TMPro;

// Ana Canvas'a eklenir. Afet bitince (GameManager.afetOlduMu) kısa bir sonuç paneli gösterir.
// Devam'a basınca oyun sürer; son afet ya da nüfus çok düştüyse oyun sonu ekranına geçilir.
public class AfetRaporu : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text baslikText;
    public TMP_Text savunmaText;
    public TMP_Text vefatText;
    public TMP_Text kurtarilanText;
    public TMP_Text hasarText;
    public TMP_Text nufusText;

    void Awake()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.afetOlduMu && !panel.activeSelf) Goster(gm);
    }

    void Goster(GameManager gm)
    {
        var afet = DengeAyarlari.Aktif.afetler[AfetIndex(gm.sonAfetTuru)];

        baslikText.text = AfetAdi(gm.sonAfetTuru);
        savunmaText.text = gm.sonSavunmaYeterli
            ? "Savunma: YETERLİ"
            : "Savunma: YETERSİZ  (" + YapiAdlari(afet.kontrolEdilen) + " en az Lv " + afet.gerekenLevel + " olmalıydı)";
        vefatText.text = "Hayatını kaybeden: " + gm.sonVefatSayisi;
        kurtarilanText.text = "Farkındalık ve AKUT sayesinde kurtarılan: " + gm.sonKurtarilanSayisi;
        hasarText.text = "Hasar alan yapı: " + gm.sonHasarliBinaSayisi;
        nufusText.text = "Kalan nüfus: " + gm.nufus;

        panel.SetActive(true);
    }

    public void Devam()
    {
        panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.AfetSonrasiDevam();
    }

    int AfetIndex(AfetTuru tur)
    {
        var afetler = DengeAyarlari.Aktif.afetler;
        for (int i = 0; i < afetler.Length; i++)
            if (afetler[i].afet == tur) return i;
        return 0;
    }

    public static string AfetAdi(AfetTuru tur)
    {
        switch (tur)
        {
            case AfetTuru.Yangin:  return "YANGIN";
            case AfetTuru.Sel:     return "SEL";
            case AfetTuru.Deprem:  return "DEPREM";
            default:               return "TSUNAMİ";
        }
    }

    public static string YapiAdi(BinaTuru b)
    {
        switch (b)
        {
            case BinaTuru.TemelBina: return "Konutlar";
            case BinaTuru.Okul:      return "Okul";
            case BinaTuru.Akut:      return "AKUT";
            case BinaTuru.Itfaiye:   return "İtfaiye";
            case BinaTuru.DenizSeti: return "Deniz Seti";
            default:                 return "Altyapı";
        }
    }

    static string YapiAdlari(BinaTuru[] yapilar)
    {
        var adlar = new string[yapilar.Length];
        for (int i = 0; i < yapilar.Length; i++) adlar[i] = YapiAdi(yapilar[i]);
        return string.Join(", ", adlar);
    }
}
