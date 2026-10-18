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
    private Image savunmaPillBg;
    private TMP_Text ribbonTitleText;
    void Start(){BuildStoryLayout();}

    void Awake()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        if (IslandTsunamiFinale.BlocksInterface)
        {
            if (panel.activeSelf) panel.SetActive(false);
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.afetOlduMu && !panel.activeSelf) Goster(gm);
        VefatSayaciniGuncelle();
        if(panel.activeSelf&&reportCard!=null)CivicUI.FitCard(reportCard,new Vector2(760,490),32,1.8f);
    }

    void Goster(GameManager gm)
    {
        var afet = DengeAyarlari.Aktif.afetler[AfetIndex(gm.sonAfetTuru)];

        string afetIsmi = AfetAdi(gm.sonAfetTuru);
        if (ribbonTitleText != null)
            ribbonTitleText.text = afetIsmi + " RAPORU";
        if (baslikText != null)
            baslikText.text = afetIsmi;

        if (gm.sonSavunmaYeterli)
        {
            if (savunmaPillBg != null)
                savunmaPillBg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Green, CivicUI.GreenShadow);
            if (savunmaText != null)
            {
                savunmaText.text = gm.sonAfetTuru == AfetTuru.Yangin
                    ? "SAVUNMA: YETERLİ  ·  İtfaiye yangını kontrol altına aldı; bazı binalarda hasar kaldı."
                    : "SAVUNMA: YETERLİ  ·  Şehir hasar almadan atlattı!";
                savunmaText.color = Color.white;
            }
        }
        else
        {
            if (savunmaPillBg != null)
                savunmaPillBg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Coral, CivicUI.CoralDark);
            if (savunmaText != null)
            {
                savunmaText.text = "SAVUNMA: YETERSİZ  (" + YapiAdlari(afet.kontrolEdilen) + " en az Lv " + afet.gerekenLevel + " olmalıydı)";
                savunmaText.color = Color.white;
            }
        }

        gosterilecekVefat = Mathf.Max(0, gm.sonVefatSayisi);
        vefatAnimasyonSuresi = 0f;
        vefatAnimasyonuAktif = gosterilecekVefat > 0;
        if (vefatText != null)
            vefatText.text = "Hayatını kaybeden: 0";
        if (kurtarilanText != null)
            kurtarilanText.text = "Kurtarılan: " + gm.sonKurtarilanSayisi + " kişi";
        var data = AnlatiVerisi.Aktif;
        if (hasarText != null)
            hasarText.text = data != null ? data.HikayeMetni(gm.sonAfetTuru, gm.sonSavunmaYeterli, gm.farkindalikYuzdesi, false) : gm.sonAfetMesaji;
        if (nufusText != null)
            nufusText.text = "Kalan nüfus: " + gm.nufus + "   ·   Etkilenen yapı: " + gm.sonHasarliBinaSayisi;

        panel.SetActive(true);
        if(gm.sonAfetTuru==AfetTuru.Yangin)
            FindAnyObjectByType<DisasterController>()?.ExtinguishReportedFire();
    }

    void BuildStoryLayout()
    {
        Transform previous = panel.transform.Find("Pencere");
        if (previous == null) return;
        var button = previous.Find("DevamButonu");

        TMP_FontAsset font = baslikText != null ? baslikText.font : null;
        font = TurkishTMPFontSupport.EnsureFont(font);

        var overlay = panel.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = new Color32(10, 18, 28, 185);
        }

        var image = CivicUI.Box("AnlatiliAfetRaporu", panel.transform, Color.white, true);
        image.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Mint, CivicUI.MintShadow);
        image.type = Image.Type.Sliced;
        reportCard = image.rectTransform;
        reportCard.sizeDelta = new Vector2(760, 490);

        // Folded pixel ribbon banner at top
        Image rMain, rLeft, rRight;
        CivicUI.CreateRibbon(
            reportCard,
            "AFET RAPORU",
            CivicUI.RibbonRed, CivicUI.RibbonRedLip, CivicUI.RibbonRedTail,
            font,
            out rMain, out rLeft, out rRight, out ribbonTitleText);
        if (ribbonTitleText != null)
        {
            ribbonTitleText.fontSize = 24f;
            ribbonTitleText.fontStyle = FontStyles.Bold;
        }

        // Inner narrative parchment card
        var narrativeBox = CivicUI.Box("NarrativeCard", reportCard, Color.white, false);
        narrativeBox.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Cream, CivicUI.CreamShadow);
        narrativeBox.type = Image.Type.Sliced;
        CivicUI.Place(narrativeBox.rectTransform, 32, 68, 696, 122);

        Move(hasarText, 48, 78, 664, 102, 18, CivicUI.Navy, narrativeBox.transform);

        // Savunma Status Pill Badge
        var defenseObj = CivicUI.Box("SavunmaBadge", reportCard, Color.white, false);
        savunmaPillBg = defenseObj;
        savunmaPillBg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Green, CivicUI.GreenShadow);
        savunmaPillBg.type = Image.Type.Sliced;
        CivicUI.Place(savunmaPillBg.rectTransform, 32, 202, 696, 42);

        Move(savunmaText, 44, 202, 672, 42, 17, Color.white, defenseObj.transform);
        if (savunmaText != null)
        {
            savunmaText.alignment = TextAlignmentOptions.Center;
            savunmaText.fontStyle = FontStyles.Bold;
        }

        // Impact & Rescue Row (Losses and Rescues)
        var vefatBox = CivicUI.Box("VefatCard", reportCard, Color.white, false);
        vefatBox.sprite = CivicUI.GetBeveledCardSprite(new Color32(255, 238, 238, 255), new Color32(235, 205, 205, 255));
        vefatBox.type = Image.Type.Sliced;
        CivicUI.Place(vefatBox.rectTransform, 32, 254, 338, 66);

        var heartIcon = CivicUI.Box("HeartIcon", vefatBox.transform, Color.white, false);
        heartIcon.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Heart, CivicUI.Coral);
        CivicUI.Place(heartIcon.rectTransform, 14, 15, 36, 36);

        Move(vefatText, 58, 254, 270, 66, 19, CivicUI.CoralDark, vefatBox.transform);
        if(vefatText!=null)CivicUI.Place(vefatText.rectTransform,58,0,266,66);
        if (vefatText != null) { vefatText.fontStyle = FontStyles.Bold; vefatText.alignment = TextAlignmentOptions.MidlineLeft; }

        var kurtarilanBox = CivicUI.Box("KurtarilanCard", reportCard, Color.white, false);
        kurtarilanBox.sprite = CivicUI.GetBeveledCardSprite(new Color32(238, 255, 244, 255), new Color32(205, 235, 215, 255));
        kurtarilanBox.type = Image.Type.Sliced;
        CivicUI.Place(kurtarilanBox.rectTransform, 390, 254, 338, 66);

        var shieldIcon = CivicUI.Box("ShieldIcon", kurtarilanBox.transform, Color.white, false);
        shieldIcon.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Shield, CivicUI.Green);
        CivicUI.Place(shieldIcon.rectTransform, 14, 15, 36, 36);

        Move(kurtarilanText, 416, 254, 270, 66, 17, CivicUI.GreenShadow, kurtarilanBox.transform);
        if(kurtarilanText!=null)CivicUI.Place(kurtarilanText.rectTransform,58,0,266,66);
        if (kurtarilanText != null) { kurtarilanText.fontStyle = FontStyles.Bold; kurtarilanText.alignment = TextAlignmentOptions.MidlineLeft; }

        // Population & Affected structures
        var nufusBadge = CivicUI.Box("NufusBadge", reportCard, Color.white, false);
        nufusBadge.sprite = CivicUI.GetPillBadgeSprite(new Color32(228, 235, 224, 255), new Color32(195, 205, 192, 255));
        nufusBadge.type = Image.Type.Sliced;
        CivicUI.Place(nufusBadge.rectTransform, 32, 330, 696, 36);

        Move(nufusText, 44, 330, 672, 36, 17, CivicUI.Navy, nufusBadge.transform);
        if (nufusText != null) { nufusText.alignment = TextAlignmentOptions.Center; nufusText.fontStyle = FontStyles.Bold; }

        // Devam Button
        if (button != null)
        {
            button.SetParent(reportCard, false);
            var le = button.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            CivicUI.Place((RectTransform)button, 32, 378, 696, 56);

            var bImg = button.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.sprite = CivicUI.GetTwoToneButtonSprite(CivicUI.Green, CivicUI.GreenLeft, CivicUI.GreenShadow);
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
            }

            var bTxt = button.GetComponentInChildren<TMP_Text>();
            if (bTxt != null)
            {
                bTxt.font = TurkishTMPFontSupport.EnsureFont(bTxt.font);
                bTxt.text = "ŞEHRE DÖN   →";
                bTxt.fontSize = 23f;
                bTxt.fontStyle = FontStyles.Bold;
                bTxt.color = Color.white;
                bTxt.alignment = TextAlignmentOptions.Center;
                bTxt.rectTransform.offsetMin = new Vector2(48f, 4f);
                bTxt.rectTransform.offsetMax = Vector2.zero;
            }

            // Left icon slot on button
            var playIcon = CivicUI.Box("ButtonIcon", button, Color.white, false);
            playIcon.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Play, Color.white);
            var pRt = playIcon.rectTransform;
            pRt.anchorMin = new Vector2(0f, 0.5f);
            pRt.anchorMax = new Vector2(0f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = new Vector2(25f, 2f);
            pRt.sizeDelta = new Vector2(22f, 22f);
        }

        previous.gameObject.SetActive(false);
    }
    void Move(TMP_Text text, float x, float y, float w, float h, float size, Color color, Transform newParent = null)
    {
        if (text == null) return;
        text.transform.SetParent(newParent != null ? newParent : reportCard, false);
        var le = text.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;
        text.font = TurkishTMPFontSupport.EnsureFont(text.font);
        if (newParent != null && newParent != reportCard)
        {
            CivicUI.Fill(text.rectTransform, 6);
        }
        else
        {
            CivicUI.Place(text.rectTransform, x, y, w, h);
        }
        text.fontSize = size;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12, size - 4);
        text.fontSizeMax = size;
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
        if (IslandTsunamiFinale.BlocksInterface) return;
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
            case BinaTuru.Okul:      return "ODTÜ";
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
