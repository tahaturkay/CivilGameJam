using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Redesigned Main Menu matching the Screenshot 2 arcade pixel-art aesthetic.
/// Features an overlapping ribbon title, chunky two-tone arcade buttons,
/// floating corner quick-action buttons, and an interactive "Nasıl Oynanır?" player guide.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public string oyunSahnesi = "MainScene";
    public SettingsPanel ayarlarPaneli;
    public string oyunBasligi = "DİRENÇLİ ŞEHİR";

    private RectTransform menuCard;
    private GameObject rehberModal;

    void Start()
    {
        Time.timeScale = 1f;
        BuildMenu();
        if (ayarlarPaneli != null) ayarlarPaneli.gameObject.SetActive(false);
    }

    void BuildMenu()
    {
        TMP_FontAsset font = null;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.font != null) { font = t.font; break; }
        }
        font = TurkishTMPFontSupport.EnsureFont(font);

        if (ayarlarPaneli == null) ayarlarPaneli = GetComponentInChildren<SettingsPanel>(true);
        if (ayarlarPaneli != null) ayarlarPaneli.transform.SetParent(transform, false);

        foreach (Transform child in transform)
        {
            if (ayarlarPaneli == null || child != ayarlarPaneli.transform)
                child.gameObject.SetActive(false);
        }

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 1) Dark slate backdrop with city vignette
        var backdropObj = new GameObject("MenuBackdrop", typeof(RectTransform), typeof(Image));
        backdropObj.transform.SetParent(transform, false);
        var backdropRt = backdropObj.GetComponent<RectTransform>();
        CivicUI.Fill(backdropRt);
        var backdropImg = backdropObj.GetComponent<Image>();
        backdropImg.color = new Color32(14, 25, 36, 255); // Deep slate navy

        var islandMovie=CivicUI.Rect("IslandAnimation",backdropObj.transform);
        CivicUI.Fill(islandMovie);islandMovie.gameObject.AddComponent<RawImage>();
        islandMovie.gameObject.AddComponent<IslandMenuBackdrop>();
        var contrast=CivicUI.Box("MenuContrast",backdropObj.transform,new Color32(8,19,29,110),false);
        CivicUI.Fill(contrast.rectTransform);contrast.raycastTarget=false;

        // 2) Centered Arcade Main Menu Card (Screenshot 2 Style!)
        GameObject cardObj = new GameObject("ArcadeMenuCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(backdropObj.transform, false);
        menuCard = cardObj.GetComponent<RectTransform>();
        menuCard.sizeDelta = new Vector2(620f, 780f);

        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Mint, CivicUI.MintShadow);
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;

        // Add soft drop shadow behind the card
        var cardShadow = cardObj.AddComponent<Shadow>();
        cardShadow.effectColor = new Color32(8, 14, 22, 160);
        cardShadow.effectDistance = new Vector2(8f, -10f);

        // 3) Overlapping Ribbon Banner at Top ("DİRENÇLİ ŞEHİR")
        CivicUI.CreateRibbon(
            menuCard, oyunBasligi,
            CivicUI.RibbonBlue, CivicUI.RibbonBlueLip, CivicUI.RibbonBlueTail,
            font, 70f, 60f);

        // Subtitle / Eyebrow Motto
        var eyebrow = CivicUI.Text(
            "Eyebrow", menuCard, "AFETLERE KARŞI BİRLİKTE HAZIRLAN, ŞEHRİNİ KORU",
            15f, CivicUI.Teal, font);
        eyebrow.fontStyle = FontStyles.Bold;
        eyebrow.alignment = TextAlignmentOptions.Center;
        CivicUI.Place(eyebrow.rectTransform, 30f, 96f, 560f, 26f);

        // Narrative Description Card inside
        var descCard = CivicUI.Rect("DescCard", menuCard);
        CivicUI.Place(descCard, 34f, 132f, 552f, 100f);
        var descBg = descCard.gameObject.AddComponent<Image>();
        descBg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.Cream, CivicUI.CreamShadow);
        descBg.type = Image.Type.Sliced;
        descBg.color = Color.white;

        var descText = CivicUI.Text(
            "DescText", descCard,
            "Bir şehrin yarını senin kararlarında.\nYapıları güçlendir, insanları bilinçlendir,\nafetlere birlikte hazırlan.",
            19f, CivicUI.DarkInk, font);
        descText.alignment = TextAlignmentOptions.Center;
        descText.lineSpacing = -4f;
        CivicUI.Fill(descText.rectTransform, 10f);

        // 4) Two-Tone Arcade Buttons Stack (Image 2 style PLAY, MULTIPLAYER, SCORES, SHOP)
        float btnY = 252f;
        float btnHeight = 68f;
        float btnSpacing = 16f;

        // Button 1: OYUNA BAŞLA (Golden Arcade Button with Play Icon)
        var playBtn = CivicUI.CreateTwoToneButton(
            menuCard, "PlayBtn", "ŞEHRİNE BAŞLA   →",
            CivicUI.GoldBtn, CivicUI.GoldBtnLeft, CivicUI.GoldBtnDark,
            CivicUI.IconType.Play, Basla, font, btnHeight);
        CivicUI.Place((RectTransform)playBtn.transform, 34f, btnY, 552f, btnHeight);
        btnY += btnHeight + btnSpacing;

        // Button 2: NASIL OYNANIR? (Purple Arcade Button with Guide/Book Icon)
        var guideBtn = CivicUI.CreateTwoToneButton(
            menuCard, "GuideBtn", "NASIL OYNANIR? (REHBER)",
            CivicUI.PurpleBtn, CivicUI.PurpleBtnLeft, CivicUI.PurpleBtnDark,
            CivicUI.IconType.Guide, RehberiAc, font, btnHeight);
        CivicUI.Place((RectTransform)guideBtn.transform, 34f, btnY, 552f, btnHeight);
        btnY += btnHeight + btnSpacing;

        // Button 3: AYARLAR & SES (Mint/Cyan Arcade Button with Gear Icon)
        var settingsBtn = CivicUI.CreateTwoToneButton(
            menuCard, "SettingsBtn", "AYARLAR & KONTROLLER",
            CivicUI.CyanBtn, CivicUI.CyanBtnLeft, CivicUI.CyanBtnDark,
            CivicUI.IconType.Gear, AyarlariAc, font, btnHeight);
        CivicUI.Place((RectTransform)settingsBtn.transform, 34f, btnY, 552f, btnHeight);
        btnY += btnHeight + btnSpacing;

        // Button 4: ÇIKIŞ (Slate/Coral Arcade Button with Exit Icon)
        var quitBtn = CivicUI.CreateTwoToneButton(
            menuCard, "QuitBtn", "ÇIKIŞ",
            CivicUI.SlateBtn, CivicUI.SlateBtnLeft, CivicUI.SlateBtnDark,
            CivicUI.IconType.Exit, Cikis, font, btnHeight);
        CivicUI.Place((RectTransform)quitBtn.transform, 34f, btnY, 552f, btnHeight);
        btnY += btnHeight + btnSpacing;

        // Footer Caption
        var caption = CivicUI.Text(
            "FooterCaption", menuCard,
            "HER HAZIRLIK BİR HAYATI KORUR",
            14f, CivicUI.Muted, font);
        caption.fontStyle = FontStyles.Bold;
        caption.alignment = TextAlignmentOptions.Center;
        caption.characterSpacing = 3f;
        CivicUI.Place(caption.rectTransform, 34f, 696f, 552f, 30f);

        // 5) Floating Square Corner Buttons (Screenshot 2 bottom corners)
        var guideSquare = CivicUI.CreateSquareButton(
            backdropObj.transform, "CornerGuideBtn", CivicUI.IconType.Guide,
            CivicUI.PurpleBtn, CivicUI.PurpleBtnDark, RehberiAc, 56f);
        RectTransform gRt = (RectTransform)guideSquare.transform;
        gRt.anchorMin = new Vector2(0f, 0f);
        gRt.anchorMax = new Vector2(0f, 0f);
        gRt.pivot = new Vector2(0f, 0f);
        gRt.anchoredPosition = new Vector2(36f, 36f);

        var settingsSquare = CivicUI.CreateSquareButton(
            backdropObj.transform, "CornerSettingsBtn", CivicUI.IconType.Gear,
            CivicUI.CyanBtn, CivicUI.CyanBtnDark, AyarlariAc, 56f);
        RectTransform sRt = (RectTransform)settingsSquare.transform;
        sRt.anchorMin = new Vector2(1f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(1f, 0f);
        sRt.anchoredPosition = new Vector2(-36f, 36f);

        // 6) Build Interactive "Nasıl Oynanır?" Player Guide Modal
        BuildGuideModal(backdropObj.transform, font);

        if (ayarlarPaneli != null) ayarlarPaneli.transform.SetAsLastSibling();
        LayoutMenu();
    }

    // --- INTERACTIVE HOW TO PLAY TUTORIAL POPUP (PLAYER GUIDANCE) ---
    void BuildGuideModal(Transform parent, TMP_FontAsset font)
    {
        rehberModal = new GameObject("NasilOynanirModal", typeof(RectTransform), typeof(Image));
        rehberModal.transform.SetParent(parent, false);
        RectTransform modalRt = rehberModal.GetComponent<RectTransform>();
        CivicUI.Fill(modalRt);

        Image overlay = rehberModal.GetComponent<Image>();
        overlay.color = new Color32(10, 18, 28, 210);
        overlay.raycastTarget = true;

        GameObject card = new GameObject("GuideCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(rehberModal.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(740f, 660f);
        CivicUI.FitCard(cardRt, new Vector2(740f, 660f), 36f);

        Image cImg = card.GetComponent<Image>();
        cImg.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Mint, CivicUI.MintShadow);
        cImg.type = Image.Type.Sliced;
        cImg.color = Color.white;

        // Ribbon
        CivicUI.CreateRibbon(
            cardRt, "NASIL OYNANIR?",
            CivicUI.RibbonGreen, CivicUI.RibbonGreenLip, CivicUI.RibbonGreenTail,
            font, 66f, 48f);

        // Close Button (×)
        var closeBtn = CivicUI.CreateSquareButton(
            cardRt, "CloseBtn", CivicUI.IconType.CloseX,
            CivicUI.CoralBtn, CivicUI.CoralBtnDark, RehberiKapat, 40f);
        RectTransform cbRt = (RectTransform)closeBtn.transform;
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-16f, -16f);

        float rowY = 88f;
        float rowHeight = 135f;

        // Guide Step 1: Binaları Güçlendir
        CreateGuideStep(
            cardRt, rowY, CivicUI.IconType.UpArrow, new Color32(255, 184, 46, 255),
            "1. YAPILARI GÜÇLENDİR & GELİR KAZAN",
            "• Konutlar nüfus kapasitesi ve günlük coin geliri sağlar.\n• İtfaiye yangını, Deniz Seti tsunamiyi, Altyapı seli önler.\n• AKUT Merkezi tüm afetlerde arama-kurtarma yaparak can kaybını azaltır.",
            font);
        rowY += rowHeight + 12f;

        // Guide Step 2: Farkındalık Yarat
        CreateGuideStep(
            cardRt, rowY, CivicUI.IconType.Book, new Color32(63, 212, 127, 255),
            "2. FARKINDALIK YARAT & EĞİTİM VER",
            "• ODTÜ'ye tıkla ve farkındalık quizlerini doğru yanıtla.\n• Bilinçli ada halkı afet anında doğru refleks gösterir.\n• Yüksek farkındalık can kaybını doğrudan ve kalıcı olarak düşürür.",
            font);
        rowY += rowHeight + 12f;

        // Guide Step 3: Afetlere Hazırlıklı Ol
        CreateGuideStep(
            cardRt, rowY, CivicUI.IconType.Calendar, new Color32(240, 77, 90, 255),
            "3. AFET TAKVİMİNİ İZLE & HAZIRLAN",
            "• Sağ üstteki afet sayacını izle: Yaklaşan afet için kritik savunma seviyesine ulaş.\n• Afet vurduğunda sağlam binalar ada halkını korur.\n• 20 gün sonunda adayı ayakta tut, en yüksek yıldız derecesini kazan!",
            font);

        // Action Button: ANLADIM, BAŞLAYALIM!
        var understandBtn = CivicUI.CreateTwoToneButton(
            cardRt, "UnderstandBtn", "ANLADIM, ŞEHRE BAŞLAYALIM!   →",
            CivicUI.GreenBtn, CivicUI.GreenBtnLeft, CivicUI.GreenBtnDark,
            CivicUI.IconType.Play, () => { RehberiKapat(); Basla(); }, font, 64f);
        CivicUI.Place((RectTransform)understandBtn.transform, 34f, 560f, 672f, 64f);

        rehberModal.SetActive(false);
    }

    void CreateGuideStep(
        RectTransform parent, float y, CivicUI.IconType icon, Color32 iconColor,
        string title, string body, TMP_FontAsset font)
    {
        GameObject step = new GameObject("Step_" + title, typeof(RectTransform), typeof(Image));
        step.transform.SetParent(parent, false);
        RectTransform sRt = step.GetComponent<RectTransform>();
        CivicUI.Place(sRt, 34f, y, 672f, 135f);

        Image bg = step.GetComponent<Image>();
        bg.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Cream, CivicUI.CreamShadow);
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;

        // Icon slot
        GameObject slot = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(step.transform, false);
        RectTransform slotRt = slot.GetComponent<RectTransform>();
        CivicUI.Place(slotRt, 16f, 18f, 52f, 52f);
        Image slotImg = slot.GetComponent<Image>();
        slotImg.sprite = CivicUI.GetDarkSlotSprite();
        slotImg.type = Image.Type.Sliced;
        slotImg.color = Color.white;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slot.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        CivicUI.Fill(iconRt, 8f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = CivicUI.GetColoredIconSprite(icon, iconColor);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Title
        var titleTxt = CivicUI.Text("Title", step.transform, title, 18f, CivicUI.DarkInk, font);
        titleTxt.fontStyle = FontStyles.Bold;
        CivicUI.Place(titleTxt.rectTransform, 80f, 14f, 574f, 24f);

        // Body
        var bodyTxt = CivicUI.Text("Body", step.transform, body, 15f, new Color32(40, 55, 70, 255), font);
        bodyTxt.lineSpacing = -2f;
        CivicUI.Place(bodyTxt.rectTransform, 80f, 42f, 574f, 82f);
    }

    public void RehberiAc()
    {
        if (rehberModal != null)
        {
            rehberModal.SetActive(true);
            rehberModal.transform.SetAsLastSibling();
        }
    }

    public void RehberiKapat()
    {
        if (rehberModal != null) rehberModal.SetActive(false);
    }

    void Update()
    {
        if (menuCard != null) LayoutMenu();
        if (EscBasildi())
        {
            if (rehberModal != null && rehberModal.activeSelf)
            {
                RehberiKapat();
                return;
            }
            if (ayarlarPaneli != null && ayarlarPaneli.AcikMi)
            {
                ayarlarPaneli.Kapat();
            }
        }
    }

    public void Basla() { Time.timeScale = 1f; SceneManager.LoadScene(oyunSahnesi); }
    void LayoutMenu()
    {
        CivicUI.FitCard(menuCard,new Vector2(620,780),36);
        if(Screen.width<Screen.height*1.45f)return;
        var parent=(RectTransform)menuCard.parent;
        float room=Mathf.Max(0,(parent.rect.width-menuCard.sizeDelta.x*menuCard.localScale.x)*.5f-48);
        menuCard.anchoredPosition+=Vector2.left*Mathf.Min(parent.rect.width*.20f,room);
    }
    public void AyarlariAc() { if (ayarlarPaneli != null) ayarlarPaneli.Ac(); }
    public void Cikis()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    bool EscBasildi()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
