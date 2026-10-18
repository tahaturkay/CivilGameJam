using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// Ana Canvas'a eklenir. GameManager.oyunBittiMi true olunca karne ekranını gösterir.
public class OyunSonu : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text baslikText;
    public TMP_Text dereceText;
    public TMP_Text istatistikText;
    public TMP_Text afetOzetiText;
    public string anaMenuSahnesi = "MainMenu";
    RectTransform resultCard;
    TMP_Text epilog;
    public string EpilogBasligi {get;private set;}

    private Image ribbonMain, ribbonLeft, ribbonRight;
    private TMP_Text ribbonTitleText;
    private readonly Image[] starPlaqueImages = new Image[3];

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
        if (gm != null && gm.oyunBittiMi && !panel.activeSelf) Goster(gm);
        if(panel.activeSelf&&resultCard!=null)CivicUI.FitCard(resultCard,new Vector2(900,740),30,1.8f);
    }
    void Start(){BuildEpilogueLayout();}

    void Goster(GameManager gm)
    {
        // Continue the cinematic's black ending behind the existing scorecard.
        var finale = FindAnyObjectByType<IslandTsunamiFinale>();
        var backdrop = panel.GetComponent<Image>();
        if (finale != null && finale.IsComplete && backdrop != null) backdrop.color = Color.black;
        int yildiz = gm.Yildiz();

        if (ribbonMain != null && ribbonLeft != null && ribbonRight != null)
        {
            if (gm.kazandiMi)
            {
                ribbonMain.sprite = CivicUI.GetRibbonMainSprite(CivicUI.RibbonGold, CivicUI.RibbonGoldLip);
                ribbonLeft.sprite = CivicUI.GetRibbonTailSprite(CivicUI.RibbonGoldTail, false);
                ribbonRight.sprite = CivicUI.GetRibbonTailSprite(CivicUI.RibbonGoldTail, true);
            }
            else
            {
                ribbonMain.sprite = CivicUI.GetRibbonMainSprite(CivicUI.RibbonRed, CivicUI.RibbonRedLip);
                ribbonLeft.sprite = CivicUI.GetRibbonTailSprite(CivicUI.RibbonRedTail, false);
                ribbonRight.sprite = CivicUI.GetRibbonTailSprite(CivicUI.RibbonRedTail, true);
            }
        }

        string title = gm.kazandiMi ? "ADA AYAKTA!" : "ADA KAYBEDİLDİ";
        if (ribbonTitleText != null) ribbonTitleText.text = title;
        if (baslikText != null) baslikText.text = title;

        UpdateStarPlaques(yildiz);

        if (dereceText != null)
        {
            dereceText.text = gm.kazandiMi
                ? DereceAdi(yildiz) + " (" + yildiz + "/3 YILDIZ)"
                : "Nüfus kritik seviyenin altına düştü.";
        }

        if (istatistikText != null)
        {
            istatistikText.text =
                "<b>Kurtarılan:</b> " + gm.toplamKurtarilan + " kişi\n" +
                "<b>Kayıp:</b> " + gm.toplamVefat + " kişi\n" +
                "<b>Kalan Nüfus:</b> " + gm.nufus + "\n" +
                "<b>Kazanılan:</b> " + gm.toplamKazanc + " coin\n" +
                "<b>Harcanan:</b> " + gm.toplamHarcama + " coin\n" +
                "<b>Yükseltmeler:</b> " + gm.yukseltmeSayisi + "\n" +
                "<b>Doğru Cevap:</b> " + gm.dogruCevapSayisi + "\n" +
                "<b>En Yüksek Farkındalık:</b> %" + Mathf.RoundToInt(gm.enYuksekFarkindalik);
        }

        if (afetOzetiText != null)
        {
            afetOzetiText.text = gm.afetOzeti +
                "\n\n<i>Afetleri tamamen engelleyemeyiz; ancak hazırlıkla can ve mal kaybını en aza indirebiliriz.</i>";
        }

        var data = AnlatiVerisi.Aktif;
        var controller = FindAnyObjectByType<DisasterController>();
        if (data != null && epilog != null)
        {
            int index = data.KartIndex(controller != null ? controller.DefendedIncidentCount : 0, gm.farkindalikYuzdesi, gm.para, gm.toplamVefat);
            var card = data.kartlar[index];
            EpilogBasligi = card.baslik;
            epilog.text = "<b>" + card.baslik + "</b>\n" + card.metin;
        }

        panel.SetActive(true);
    }

    void UpdateStarPlaques(int yildiz)
    {
        for (int i = 0; i < starPlaqueImages.Length; i++)
        {
            if (starPlaqueImages[i] != null)
                starPlaqueImages[i].sprite = CivicUI.GetStarPlaqueSprite(i < yildiz);
        }
    }

    void BuildEpilogueLayout()
    {
        Transform old = panel.transform.Find("Pencere");
        if (old == null) return;
        Transform retry = old.Find("TekrarOynaButonu"), menu = old.Find("AnaMenuButonu");

        TMP_FontAsset font = baslikText != null ? baslikText.font : null;
        font = TurkishTMPFontSupport.EnsureFont(font);

        var overlay = panel.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = new Color32(10, 18, 28, 190);
        }

        var box = CivicUI.Box("SehirKarnesi", panel.transform, Color.white, true);
        box.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Mint, CivicUI.MintShadow);
        box.type = Image.Type.Sliced;
        resultCard = box.rectTransform;
        resultCard.sizeDelta = new Vector2(900, 740);

        // Top Folded Ribbon
        CivicUI.CreateRibbon(
            resultCard,
            "ŞEHİR KARNESİ",
            CivicUI.RibbonGold, CivicUI.RibbonGoldLip, CivicUI.RibbonGoldTail,
            font,
            out ribbonMain, out ribbonLeft, out ribbonRight, out ribbonTitleText);
        if (ribbonTitleText != null)
        {
            ribbonTitleText.fontSize = 24f;
            ribbonTitleText.fontStyle = FontStyles.Bold;
        }

        // Hide obsolete text from parent
        Move(baslikText, 0, 0, 0, 0, 1, Color.clear);
        if (baslikText != null) baslikText.gameObject.SetActive(false);

        // 3 Golden Star Plaques Row
        var starsRow = CivicUI.Rect("StarRatingRow", resultCard);
        starsRow.anchorMin = new Vector2(0.5f, 1f);
        starsRow.anchorMax = new Vector2(0.5f, 1f);
        starsRow.pivot = new Vector2(0.5f, 1f);
        starsRow.anchoredPosition = new Vector2(0f, -68f);
        starsRow.sizeDelta = new Vector2(270f, 74f);

        for (int i = 0; i < 3; i++)
        {
            float posX = (i - 1) * (74f + 14f);
            var starObj = CivicUI.Box("Star_" + (i + 1), starsRow, Color.white, false);
            starPlaqueImages[i] = starObj;
            starObj.sprite = CivicUI.GetStarPlaqueSprite(false);
            starObj.type = Image.Type.Simple;
            var sRt = starObj.rectTransform;
            sRt.anchorMin = new Vector2(0.5f, 0.5f);
            sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(posX, 0f);
            sRt.sizeDelta = new Vector2(74f, 74f);
        }

        // Derece / Rating Pill Badge
        var ratingBadge = CivicUI.Box("RatingBadge", resultCard, Color.white, false);
        ratingBadge.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Amber, CivicUI.AmberShadow);
        ratingBadge.type = Image.Type.Sliced;
        CivicUI.Place(ratingBadge.rectTransform, 260, 150, 380, 42);

        Move(dereceText, 265, 150, 370, 42, 19, CivicUI.Navy, ratingBadge.transform);
        if (dereceText != null)
        {
            dereceText.alignment = TextAlignmentOptions.Center;
            dereceText.fontStyle = FontStyles.Bold;
        }

        // Left Column: Statistics Card
        var statsCard = CivicUI.Box("StatsCard", resultCard, Color.white, false);
        statsCard.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Cream, CivicUI.CreamShadow);
        statsCard.type = Image.Type.Sliced;
        CivicUI.Place(statsCard.rectTransform, 32, 204, 410, 290);

        var statsHeader = CivicUI.Box("StatsHeader", statsCard.transform, Color.white, false);
        statsHeader.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Mint, CivicUI.MintShadow);
        statsHeader.type = Image.Type.Sliced;
        CivicUI.Place(statsHeader.rectTransform, 14, 12, 382, 32);
        var statsTitle = CivicUI.Text("Title", statsHeader.transform, "ŞEHİR İSTATİSTİKLERİ", 16, CivicUI.Navy, font);
        statsTitle.fontStyle = FontStyles.Bold;
        statsTitle.alignment = TextAlignmentOptions.Center;
        CivicUI.Fill(statsTitle.rectTransform, 4);

        Move(istatistikText, 48, 258, 378, 226, 17, CivicUI.Navy, statsCard.transform);
        if (istatistikText != null)
        {
            istatistikText.rectTransform.offsetMin = new Vector2(16, 12);
            istatistikText.rectTransform.offsetMax = new Vector2(-16, -50);
            istatistikText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Right Column: Disaster Summary Card
        var disasterCard = CivicUI.Box("DisasterCard", resultCard, Color.white, false);
        disasterCard.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Cream, CivicUI.CreamShadow);
        disasterCard.type = Image.Type.Sliced;
        CivicUI.Place(disasterCard.rectTransform, 458, 204, 410, 290);

        var disHeader = CivicUI.Box("DisHeader", disasterCard.transform, Color.white, false);
        disHeader.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Cyan, CivicUI.CyanShadow);
        disHeader.type = Image.Type.Sliced;
        CivicUI.Place(disHeader.rectTransform, 14, 12, 382, 32);
        var disTitle = CivicUI.Text("Title", disHeader.transform, "AFET VE HAZIRLIK", 16, CivicUI.Navy, font);
        disTitle.fontStyle = FontStyles.Bold;
        disTitle.alignment = TextAlignmentOptions.Center;
        CivicUI.Fill(disTitle.rectTransform, 4);

        Move(afetOzetiText, 474, 258, 378, 226, 16, CivicUI.Navy, disasterCard.transform);
        if (afetOzetiText != null)
        {
            afetOzetiText.rectTransform.offsetMin = new Vector2(16, 12);
            afetOzetiText.rectTransform.offsetMax = new Vector2(-16, -50);
            afetOzetiText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Epilogue Box at bottom
        var epilogBox = CivicUI.Box("EpilogueCard", resultCard, Color.white, false);
        epilogBox.sprite = CivicUI.GetBeveledCardSprite(new Color32(236, 246, 243, 255), new Color32(205, 225, 218, 255));
        epilogBox.type = Image.Type.Sliced;
        CivicUI.Place(epilogBox.rectTransform, 32, 506, 836, 134);

        epilog = CivicUI.Text("Epilog", epilogBox.transform, "", 18, CivicUI.Navy, font);
        CivicUI.Fill(epilog.rectTransform, 14);
        epilog.alignment = TextAlignmentOptions.TopLeft;
        epilog.enableAutoSizing = true;
        epilog.fontSizeMin = 15;
        epilog.fontSizeMax = 20;

        // Action Buttons
        if (retry != null)
        {
            retry.SetParent(resultCard, false);
            var le = retry.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            CivicUI.Place((RectTransform)retry, 32, 654, 404, 58);

            var rImg = retry.GetComponent<Image>();
            if (rImg != null)
            {
                rImg.sprite = CivicUI.GetTwoToneButtonSprite(CivicUI.Green, CivicUI.GreenLeft, CivicUI.GreenShadow);
                rImg.type = Image.Type.Sliced;
                rImg.color = Color.white;
            }

            var rTxt = retry.GetComponentInChildren<TMP_Text>();
            if (rTxt != null)
            {
                rTxt.font = TurkishTMPFontSupport.EnsureFont(rTxt.font);
                rTxt.text = "TEKRAR OYNA";
                rTxt.fontSize = 22f;
                rTxt.fontStyle = FontStyles.Bold;
                rTxt.color = Color.white;
                rTxt.alignment = TextAlignmentOptions.Center;
                rTxt.rectTransform.offsetMin = new Vector2(46f, 4f);
                rTxt.rectTransform.offsetMax = Vector2.zero;
            }

            var icon = CivicUI.Box("RetryIcon", retry, Color.white, false);
            icon.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Refresh, Color.white);
            var iRt = icon.rectTransform;
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(25f, 2f);
            iRt.sizeDelta = new Vector2(22f, 22f);
        }

        if (menu != null)
        {
            menu.SetParent(resultCard, false);
            var le = menu.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
            CivicUI.Place((RectTransform)menu, 464, 654, 404, 58);

            var mImg = menu.GetComponent<Image>();
            if (mImg != null)
            {
                mImg.sprite = CivicUI.GetTwoToneButtonSprite(CivicUI.Navy, CivicUI.NavyLeft, CivicUI.NavyShadow);
                mImg.type = Image.Type.Sliced;
                mImg.color = Color.white;
            }

            var mTxt = menu.GetComponentInChildren<TMP_Text>();
            if (mTxt != null)
            {
                mTxt.font = TurkishTMPFontSupport.EnsureFont(mTxt.font);
                mTxt.text = "ANA MENÜ";
                mTxt.fontSize = 22f;
                mTxt.fontStyle = FontStyles.Bold;
                mTxt.color = Color.white;
                mTxt.alignment = TextAlignmentOptions.Center;
                mTxt.rectTransform.offsetMin = new Vector2(46f, 4f);
                mTxt.rectTransform.offsetMax = Vector2.zero;
            }

            var icon = CivicUI.Box("MenuIcon", menu, Color.white, false);
            icon.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Home, Color.white);
            var iRt = icon.rectTransform;
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(25f, 2f);
            iRt.sizeDelta = new Vector2(22f, 22f);
        }

        old.gameObject.SetActive(false);
    }

    void Move(TMP_Text text, float x, float y, float w, float h, float size, Color color, Transform newParent = null)
    {
        if (text == null) return;
        text.transform.SetParent(newParent != null ? newParent : resultCard, false);
        var le = text.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;
        text.font = TurkishTMPFontSupport.EnsureFont(text.font);
        if (newParent != null && newParent != resultCard)
        {
            CivicUI.Fill(text.rectTransform, 10);
        }
        else
        {
            CivicUI.Place(text.rectTransform, x, y, w, h);
        }
        text.color = color;
        text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12, size - 4);
        text.fontSizeMax = size;
    }

    string DereceAdi(int yildiz)
    {
        switch (yildiz)
        {
            case 3:  return "MÜKEMMEL HAZIRLIK";
            case 2:  return "BAŞARILI";
            case 1:  return "GELİŞTİRİLMELİ";
            default: return "KRİTİK";
        }
    }

    public void TekrarOyna()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        // Editörde Build listesinde olmayan test sahneleri de yeniden yüklenebilsin
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
    }

    public void AnaMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(anaMenuSahnesi);
    }
}
