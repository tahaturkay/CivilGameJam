using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    RectTransform reportCard;
    void Start(){BuildStoryLayout();}

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
        if(panel.activeSelf&&reportCard!=null)CivicUI.FitCard(reportCard,new Vector2(760,580),32,1.8f);
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
        var data=AnlatiVerisi.Aktif;var decision=GetComponent<DereYatagiKarari>();
        hasarText.text=data!=null?data.HikayeMetni(gm.sonAfetTuru,gm.sonSavunmaYeterli,gm.farkindalikYuzdesi,decision!=null&&decision.KabulEdildi):gm.sonAfetMesaji;
        if(gm.sonAfetTuru==AfetTuru.Sel&&decision!=null&&decision.KabulEdildi&&data!=null)
            hasarText.text+="\n<size=85%>"+data.selEkSatiri+"</size>";
        nufusText.text = "Kalan nüfus: " + gm.nufus+"   ·   Etkilenen yapı: "+gm.sonHasarliBinaSayisi;

        panel.SetActive(true);
    }

    void BuildStoryLayout()
    {
        Transform previous=panel.transform.Find("Pencere");if(previous==null)return;
        var button=previous.Find("DevamButonu");
        var image=CivicUI.Box("AnlatiliAfetRaporu",panel.transform,CivicUI.Cream,true);reportCard=image.rectTransform;
        Move(baslikText,28,25,704,48,32,CivicUI.Teal);
        Move(savunmaText,28,90,704,62,19,CivicUI.Navy);
        Move(vefatText,28,170,340,42,22,UIManager.RibbonRedDark);
        Move(kurtarilanText,390,170,342,60,19,UIManager.GreenBtnDark);
        Move(hasarText,28,245,704,165,20,CivicUI.Navy);
        Move(nufusText,28,428,704,32,18,CivicUI.Muted);
        if(button!=null)
        {
            button.SetParent(reportCard,false);var le=button.GetComponent<LayoutElement>();if(le!=null)le.ignoreLayout=true;
            CivicUI.Place((RectTransform)button,28,490,704,60);
            CivicUI.ReskinButton(button.GetComponent<Button>(),CivicUI.Teal,CivicUI.Cream);
        }
        previous.gameObject.SetActive(false);
    }
    void Move(TMP_Text text,float x,float y,float w,float h,float size,Color color)
    {
        if(text==null)return;text.transform.SetParent(reportCard,false);
        var le=text.GetComponent<LayoutElement>();if(le!=null)le.ignoreLayout=true;
        CivicUI.Place(text.rectTransform,x,y,w,h);text.fontSize=size;text.color=color;
        text.fontStyle=FontStyles.Normal;text.alignment=TextAlignmentOptions.TopLeft;
        text.textWrappingMode=TextWrappingModes.Normal;text.enableAutoSizing=true;text.fontSizeMin=size-3;text.fontSizeMax=size;
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
