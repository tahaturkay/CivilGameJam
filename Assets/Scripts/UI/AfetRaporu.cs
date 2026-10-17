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

    private const float SayacAnimasyonSuresi = 1f;
    private int gosterilecekVefat;
    private float vefatAnimasyonSuresi;
    private bool vefatAnimasyonuAktif;

    void Awake()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.afetOlduMu && !panel.activeSelf) Goster(gm);
        VefatSayaciniGuncelle();
    }

    void Goster(GameManager gm)
    {
        var afet = DengeAyarlari.Aktif.afetler[AfetIndex(gm.sonAfetTuru)];

        baslikText.text = AfetAdi(gm.sonAfetTuru);
        savunmaText.text = gm.sonSavunmaYeterli
            ? "Savunma: YETERLİ"
            : "Savunma: YETERSİZ  (" + YapiAdlari(afet.kontrolEdilen) + " en az Lv " + afet.gerekenLevel + " olmalıydı)";
        gosterilecekVefat = Mathf.Max(0, gm.sonVefatSayisi);
        vefatAnimasyonSuresi = 0f;
        vefatAnimasyonuAktif = gosterilecekVefat > 0;
        if (vefatText != null)
            vefatText.text = "Hayatını kaybeden: 0";
        kurtarilanText.text = "Farkındalık ve AKUT sayesinde kurtarılan: " + gm.sonKurtarilanSayisi;
        hasarText.text = "Hasar alan yapı: " + gm.sonHasarliBinaSayisi;
        nufusText.text = "Kalan nüfus: " + gm.nufus;

        panel.SetActive(true);
    }

    private void VefatSayaciniGuncelle()
    {
        if (!vefatAnimasyonuAktif || vefatText == null) return;

        vefatAnimasyonSuresi += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(vefatAnimasyonSuresi / SayacAnimasyonSuresi);
        float easeOut = 1f - Mathf.Pow(1f - t, 3f);
        int sayac = t >= 1f ? gosterilecekVefat : Mathf.RoundToInt(gosterilecekVefat * easeOut);
        vefatText.text = "Hayatını kaybeden: " + sayac;
        if (t >= 1f) vefatAnimasyonuAktif = false;
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
