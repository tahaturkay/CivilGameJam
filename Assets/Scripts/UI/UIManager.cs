using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIManager : MonoBehaviour
{
    public enum IconType
    {
        Person,
        HexCoin,
        Book,
        Calendar,
        Star,
        UpArrow,
        Play,
        CloseX
    }

    // --- ONAYLANAN PROTOTİP RENK PALETİ ---
    public static readonly Color32 DarkInk         = new Color32(20, 29, 43, 255);    // #141D2B
    public static readonly Color32 CreamBg         = new Color32(255, 248, 231, 255); // #FFF8E7 (Krem)
    public static readonly Color32 CreamShadow     = new Color32(226, 212, 181, 255); // #E2D4B5
    public static readonly Color32 ModalCyanBg     = new Color32(228, 244, 243, 255); // #E4F4F3
    public static readonly Color32 ModalCyanShadow = new Color32(180, 212, 210, 255); // #B4D4D2
    public static readonly Color32 DarkSlotBg      = new Color32(27, 42, 56, 255);    // #1B2A38
    public static readonly Color32 DarkSlotBorder  = new Color32(15, 23, 34, 255);    // #0F1722

    public static readonly Color32 RibbonBlue      = new Color32(43, 148, 229, 255);  // #2B94E5
    public static readonly Color32 RibbonBlueDark  = new Color32(26, 98, 160, 255);   // #1A62A0
    public static readonly Color32 RibbonRed       = new Color32(232, 74, 85, 255);   // #E84A55
    public static readonly Color32 RibbonRedDark   = new Color32(158, 39, 48, 255);   // #9E2730

    public static readonly Color32 GreenBtn        = new Color32(72, 209, 108, 255);  // #48D16C
    public static readonly Color32 GreenBtnLeft    = new Color32(47, 179, 84, 255);   // #2FB354
    public static readonly Color32 GreenBtnDark    = new Color32(29, 122, 55, 255);   // #1D7A37

    public static readonly Color32 GoldBtn         = new Color32(255, 200, 59, 255);  // #FFC83B
    public static readonly Color32 GoldBtnLeft     = new Color32(235, 166, 28, 255);  // #EBA61C
    public static readonly Color32 GoldBtnDark     = new Color32(184, 123, 16, 255);  // #B87B10
    public static readonly Color32 GoldStar        = new Color32(255, 212, 59, 255);  // #FFD43B

    public static readonly Color32 BlueBtn         = new Color32(74, 171, 247, 255);  // #4AABF7
    public static readonly Color32 BlueBtnLeft     = new Color32(43, 148, 229, 255);  // #2B94E5
    public static readonly Color32 BlueBtnDark     = new Color32(27, 105, 168, 255);  // #1B69A8

    public static readonly Color32 RedClose        = new Color32(239, 68, 77, 255);   // #EF444D
    public static readonly Color32 RedCloseDark    = new Color32(163, 36, 43, 255);   // #A3242B
    public static readonly Color32 MutedText       = new Color32(92, 107, 115, 255);  // #5C6B73

    [Header("Üst Bar Metinleri")]
    public TMP_Text gunText;
    public TMP_Text paraText;
    public TMP_Text nufusText;
    public TMP_Text farkindalikText;

    private TMP_FontAsset sharedFont;
    private Image awarenessFillBar;
    private TMP_Text gunSubLabelText;
    private TMP_Text farkindalikSubLabelText;

    // Zaman Hızı Butonları (0: Durdur, 1: 1X, 2: 2X, 3: 3X)
    private readonly Button[] speedButtons = new Button[4];
    private readonly Image[] speedButtonImages = new Image[4];
    private readonly TMP_Text[] speedButtonLabels = new TMP_Text[4];

    // Bina Hover (Üstüne Gelince Gözüken İsim & LVL Etiketi)
    private RectTransform hoverTooltipRt;
    private TMP_Text hoverTooltipText;
    private Camera mainCam;

    private bool initialized;

    // HUD ölçüleri tek bir ölçekte tutulur; mevcut canvas sabit piksel kullandığı için
    // yalnızca ekran kenar boşlukları ve dar ekranlarda grup ölçeği uyarlanır.
    private const float IkonOlcek = 1.8f;
    private const float IkonYuvasi = 44f * IkonOlcek;
    private const float IkonBoyu = 32f * IkonOlcek;
    private const float KartYuksekligi = 112f;
    private const float YaziSolBosluk = 10f + IkonYuvasi + 10f;
    private const float EkGenislik = YaziSolBosluk - 64f;
    private const float HudKenarBoslugu = 20f;
    private const float HudGruplarArasiBosluk = 32f;
    private const string UstPanelOlcegiKey = "ui_ust_panel_olcegi";
    private const string UstPanelDikeyBoslukKey = "ui_ust_panel_dikey_bosluk";
    private static System.Action ustPanelAyarDegisti;

    public static float UstPanelOlcegi
    {
        get { return Mathf.Clamp(PlayerPrefs.GetFloat(UstPanelOlcegiKey, 1f), 0.75f, 1.25f); }
    }

    public static float UstPanelDikeyBosluk
    {
        get { return Mathf.Clamp(PlayerPrefs.GetFloat(UstPanelDikeyBoslukKey, 0f), 0f, 80f); }
    }

    public static void UstPanelAyarlariniUygula(float olcek, float dikeyBosluk)
    {
        PlayerPrefs.SetFloat(UstPanelOlcegiKey, Mathf.Clamp(olcek, 0.75f, 1.25f));
        PlayerPrefs.SetFloat(UstPanelDikeyBoslukKey, Mathf.Clamp(dikeyBosluk, 0f, 80f));
        if (ustPanelAyarDegisti != null) ustPanelAyarDegisti();
    }

    public static void UstPanelVarsayilanaSifirla()
    {
        UstPanelAyarlariniUygula(1f, 0f);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void SifirlaStatikDurum()
    {
        ustPanelAyarDegisti = null;
    }

    // Update'te her karede yazı/sprite üretmemek için son gösterilen değerler
    private int sonGun = -1, sonPara = int.MinValue, sonGelir = -1, sonNufus = -1, sonKapasite = -1, sonFark = -1, sonKalan = int.MinValue, sonTavan = -1, sonHizModu = -1;
    private Vector2 sonFarePos = new Vector2(-1f, -1f);
    private BuildingVisuals sonHoverBina;

    private RectTransform leftHudGroupRt;
    private RectTransform rightHudColumnRt;
    private RectTransform hudCanvasRt;
    private RectTransform hudBarRt;
    private float lastHudCanvasWidth = -1f;
    private Rect hudLastSafeArea;
    private int hudLastScreenWidth = -1;
    private int hudLastScreenHeight = -1;

    private GameManager incomeEventManager;
    private bool paraGosterimiHazir;
    private bool bakiyeAnimasyonuAktif;
    private bool gelirAnimasyonuAktif;
    private int gosterilenBakiye;
    private int bakiyeAnimasyonBaslangici;
    private int bakiyeAnimasyonHedefi;
    private int gelirAnimasyonHedefi;
    private int gosterilenGelir;
    private int sonGosterilenBakiye = int.MinValue;
    private int sonGosterilenGelir = int.MinValue;
    private float bakiyeAnimasyonSuresi;
    private float gelirAnimasyonSuresi;
    private const float ParaAnimasyonSuresi = 1f;

    private void Awake()
    {
        sharedFont = gunText != null ? gunText.font : TMP_Settings.defaultFontAsset;
        EnsureTurkishFontSupport(sharedFont);
        ApplyPrototypeHudAndModals();
    }

    private void OnEnable()
    {
        ustPanelAyarDegisti -= UstPanelAyarDegisti;
        ustPanelAyarDegisti += UstPanelAyarDegisti;
        GelirEtkinligineBaglan(GameManager.Instance);
        if (initialized) UpdateHudLayout(true);
    }

    private void OnDisable()
    {
        ustPanelAyarDegisti -= UstPanelAyarDegisti;
        GelirEtkinligindenAyril();
    }

    private void UstPanelAyarDegisti()
    {
        UpdateHudLayout(true);
    }

    public static void EnsureTurkishFontSupport(TMP_FontAsset fontAsset)
    {
        TurkishTMPFontSupport.EnsureFont(fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset);
    }

    private void ApplyPrototypeHudAndModals()
    {
        if (initialized) return;
        initialized = true;

        Transform ustBar = gunText != null ? gunText.transform.parent : transform.Find("UstBar");
        if (ustBar != null)
        {
            // Eski tam genişlik siyah bar görüntüsünü kapatıp şeffaf konteyner yapalım
            Image oldBarBg = ustBar.GetComponent<Image>();
            if (oldBarBg != null) oldBarBg.enabled = false;

            HorizontalLayoutGroup oldHlg = ustBar.GetComponent<HorizontalLayoutGroup>();
            if (oldHlg != null) Destroy(oldHlg);

            RectTransform ustRt = ustBar.GetComponent<RectTransform>();
            hudBarRt = ustRt;
            ustRt.anchorMin = new Vector2(0f, 1f);
            ustRt.anchorMax = new Vector2(1f, 1f);
            ustRt.pivot = new Vector2(0.5f, 1f);
            ustRt.anchoredPosition = Vector2.zero;
            ustRt.sizeDelta = new Vector2(0f, 200f);

            BuildTopLeftHud(ustBar);
            BuildTopRightHud(ustBar);
            Canvas canvas = ustBar.GetComponentInParent<Canvas>();
            if (canvas != null) hudCanvasRt = canvas.rootCanvas.GetComponent<RectTransform>();
            UpdateHudLayout(true);
        }

        BuildHoverTooltip();
        StyleExistingExternalPanels();
    }

    // SOL ÜST: 1) İnsan Sayısı (Nüfus) -> 2) Para -> 3) Farkındalık (Yan yana, Krem arka planlı)
    private void BuildTopLeftHud(Transform ustBar)
    {
        GameObject leftGroup = new GameObject("TopLeftHudGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        leftGroup.transform.SetParent(ustBar, false);
        RectTransform lgRt = leftGroup.GetComponent<RectTransform>();
        lgRt.anchorMin = new Vector2(0f, 1f);
        lgRt.anchorMax = new Vector2(0f, 1f);
        lgRt.pivot = new Vector2(0f, 1f);
        lgRt.anchoredPosition = new Vector2(20f, -18f);
        float leftGroupWidth = 230f + EkGenislik + 265f + EkGenislik + 275f + EkGenislik + 28f;
        lgRt.sizeDelta = new Vector2(leftGroupWidth, KartYuksekligi + 4f);
        leftHudGroupRt = lgRt;

        HorizontalLayoutGroup hlg = leftGroup.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 1. İNSAN SAYISI (NÜFUS) - Basit İnsan Figürü İkonu
        TMP_Text nufusSub;
        CreateHudPillCard(
            leftGroup.transform, "NufusPill", 230f + EkGenislik, KartYuksekligi,
            IconType.Person, new Color32(76, 153, 145, 255),
            "NÜFUS", nufusText, out nufusSub, false, out _);

        // 2. PARA (BÜTÇE) - Altıgen Sikke İkonu
        TMP_Text paraSub;
        CreateHudPillCard(
            leftGroup.transform, "ParaPill", 265f + EkGenislik, KartYuksekligi,
            IconType.HexCoin, new Color32(218, 163, 65, 255),
            "COIN", paraText, out paraSub, false, out _);

        // 3. FARKINDALIK - Basic Açık Kitap İkonu + İlerleme Çubuğu
        CreateHudPillCard(
            leftGroup.transform, "FarkindalikPill", 275f + EkGenislik, KartYuksekligi,
            IconType.Book, new Color32(67, 139, 147, 255),
            "FARKINDALIK", farkindalikText, out farkindalikSubLabelText, true, out awarenessFillBar);
    }

    // SAĞ ÜST: En sağ üst köşede Gün Sayısı, hemen altında Durdur (II), 1X, 2X, 3X butonları
    private void BuildTopRightHud(Transform ustBar)
    {
        GameObject rightCol = new GameObject("TopRightHudColumn", typeof(RectTransform));
        rightCol.transform.SetParent(ustBar, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(1f, 1f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(1f, 1f);
        rcRt.anchoredPosition = new Vector2(-20f, -18f);
        rcRt.sizeDelta = new Vector2(260f + EkGenislik, KartYuksekligi + 60f);
        rightHudColumnRt = rcRt;

        // Üstte Gün Kutusu (Krem arka planlı)
        RectTransform dayPill = CreateHudPillCard(
            rightCol.transform, "GunPill", 248f + EkGenislik, KartYuksekligi,
            IconType.Calendar, new Color32(196, 104, 78, 255),
            "GÜN SAYACI", gunText, out gunSubLabelText, false, out _);
        dayPill.anchorMin = new Vector2(1f, 1f);
        dayPill.anchorMax = new Vector2(1f, 1f);
        dayPill.pivot = new Vector2(1f, 1f);
        dayPill.anchoredPosition = Vector2.zero;

        // Altında Zaman Akışı Butonları: Durdur (II), 1X, 2X, 3X
        GameObject speedRow = new GameObject("SpeedControlsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        speedRow.transform.SetParent(rightCol.transform, false);
        RectTransform srRt = speedRow.GetComponent<RectTransform>();
        srRt.anchorMin = new Vector2(1f, 1f);
        srRt.anchorMax = new Vector2(1f, 1f);
        srRt.pivot = new Vector2(1f, 1f);
        srRt.anchoredPosition = new Vector2(0f, -(KartYuksekligi + 8f));
        srRt.sizeDelta = new Vector2(248f + EkGenislik, 42f);

        HorizontalLayoutGroup sHlg = speedRow.GetComponent<HorizontalLayoutGroup>();
        sHlg.spacing = 8f;
        sHlg.childAlignment = TextAnchor.MiddleRight;
        sHlg.childControlWidth = true;
        sHlg.childControlHeight = true;
        sHlg.childForceExpandWidth = true;
        sHlg.childForceExpandHeight = true;

        string[] labels = { "II", "1X", "2X", "3X" };
        for (int i = 0; i < 4; i++)
        {
            int mode = i;
            GameObject btnObj = new GameObject("SpeedBtn_" + labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(speedRow.transform, false);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.sprite = GetCreamBoxSprite(CreamBg, CreamShadow);
            btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => SetTimeSpeed(mode));

            TMP_Text txt = CreateText(btnObj.transform, "Text", sharedFont, labels[i], 18f, FontStyles.Bold, DarkInk, TextAlignmentOptions.Center);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(0f, 4f);
            txt.rectTransform.offsetMax = Vector2.zero;

            speedButtons[i] = btn;
            speedButtonImages[i] = btnImg;
            speedButtonLabels[i] = txt;
        }

        // Eğer Ana Canvas üzerinde HizKontrol varsa, eski HizPaneli'ni gizleyip bizim butonlarımızı bağlayalım
        HizKontrol hizKontrol = GetComponentInParent<HizKontrol>();
        if (hizKontrol == null) hizKontrol = FindAnyObjectByType<HizKontrol>();
        if (hizKontrol != null)
        {
            Transform oldHizPanel = transform.Find("HizPaneli");
            if (oldHizPanel != null) oldHizPanel.gameObject.SetActive(false);

            hizKontrol.duraklatButonu = speedButtons[0];
            hizKontrol.hizButonlari = new[] { speedButtons[1], speedButtons[2], speedButtons[3] };
            hizKontrol.enabled = false; // Renklendirmeyi UIManager kendi piksel-art sprite'larıyla yapıyor
        }
    }

    public void SetTimeSpeed(int mode)
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (mode == 0)
            {
                gm.kullaniciDurdurdu = !gm.kullaniciDurdurdu;
            }
            else
            {
                gm.hizCarpani = Mathf.Clamp(mode, 1, 3);
                gm.kullaniciDurdurdu = false;
            }
        }
        RefreshSpeedButtonVisuals();
    }

    private void RefreshSpeedButtonVisuals()
    {
        var gm = GameManager.Instance;
        int activeMode = 1;
        if (gm != null)
        {
            activeMode = gm.kullaniciDurdurdu ? 0 : Mathf.Clamp(gm.hizCarpani, 1, 3);
        }
        if (activeMode == sonHizModu) return;
        sonHizModu = activeMode;

        for (int i = 0; i < 4; i++)
        {
            if (speedButtonImages[i] == null) continue;
            bool isActive = (i == activeMode);
            if (isActive)
            {
                speedButtonImages[i].sprite = GetCreamBoxSprite(GoldBtn, GoldBtnDark);
                speedButtonImages[i].color = Color.white;
            }
            else
            {
                speedButtonImages[i].sprite = GetCreamBoxSprite(CreamBg, CreamShadow);
                speedButtonImages[i].color = Color.white;
            }
            if (speedButtonLabels[i] != null)
                speedButtonLabels[i].color = DarkInk;
        }
    }

    private RectTransform CreateHudPillCard(
        Transform parent, string name, float width, float height,
        IconType iconType, Color32 iconColor, string subLabel,
        TMP_Text existingValueText, out TMP_Text outSubLabel, bool withProgressBar, out Image progressFill)
    {
        progressFill = null;
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = card.GetComponent<Image>();
        bg.sprite = GetCreamBoxSprite(CreamBg, CreamShadow);
        bg.type = Image.Type.Sliced;

        // Küçük şehir renk şeridi kartı ikon rengiyle eşler, içerik alanını daraltmaz.
        GameObject accentObj = new GameObject("CityColorAccent", typeof(RectTransform), typeof(Image));
        accentObj.transform.SetParent(card.transform, false);
        RectTransform accentRt = accentObj.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.offsetMin = new Vector2(YaziSolBosluk, -14f);
        accentRt.offsetMax = new Vector2(-12f, -10f);
        Image accentImg = accentObj.GetComponent<Image>();
        accentImg.color = iconColor;
        accentImg.raycastTarget = false;

        // Sol İkon Yuvası (Koyu Çerçeveli)
        GameObject iconSlot = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        iconSlot.transform.SetParent(card.transform, false);
        RectTransform isRt = iconSlot.GetComponent<RectTransform>();
        isRt.anchorMin = new Vector2(0f, 0.5f);
        isRt.anchorMax = new Vector2(0f, 0.5f);
        isRt.pivot = new Vector2(0f, 0.5f);
        isRt.anchoredPosition = new Vector2(10f, 0f);
        isRt.sizeDelta = new Vector2(IkonYuvasi, IkonYuvasi);

        Image slotImg = iconSlot.GetComponent<Image>();
        slotImg.sprite = GetDarkSlotSprite();
        slotImg.type = Image.Type.Sliced;

        GameObject iconGraphic = new GameObject("IconGraphic", typeof(RectTransform), typeof(Image));
        iconGraphic.transform.SetParent(iconSlot.transform, false);
        RectTransform igRt = iconGraphic.GetComponent<RectTransform>();
        igRt.anchorMin = new Vector2(0.5f, 0.5f);
        igRt.anchorMax = new Vector2(0.5f, 0.5f);
        igRt.sizeDelta = new Vector2(IkonBoyu, IkonBoyu);
        igRt.anchoredPosition = Vector2.zero;

        Image igImg = iconGraphic.GetComponent<Image>();
        igImg.sprite = GetColoredIconSprite(iconType, iconColor);
        igImg.preserveAspect = true;

        // Küçük Üst Etiket
        outSubLabel = CreateText(card.transform, "SubLabel", sharedFont, subLabel, 12f, FontStyles.Bold, MutedText, TextAlignmentOptions.Left);
        RectTransform slRt = outSubLabel.rectTransform;
        slRt.anchorMin = new Vector2(0f, 1f);
        slRt.anchorMax = new Vector2(1f, 1f);
        slRt.pivot = new Vector2(0f, 1f);
        slRt.anchoredPosition = new Vector2(YaziSolBosluk, -20f);
        slRt.sizeDelta = new Vector2(-(YaziSolBosluk + 10f), 16f);

        // Ana Değer Metni (Mevcut TMP_Text referansını koruyarak içine taşıyoruz)
        if (existingValueText != null)
        {
            existingValueText.transform.SetParent(card.transform, false);
            RectTransform vtRt = existingValueText.rectTransform;
            vtRt.anchorMin = new Vector2(0f, 0f);
            vtRt.anchorMax = new Vector2(1f, 1f);
            vtRt.offsetMin = new Vector2(YaziSolBosluk, withProgressBar ? 20f : 10f);
            vtRt.offsetMax = new Vector2(-12f, -22f);
            existingValueText.fontSize = withProgressBar ? 20f : 21f;
            existingValueText.enableAutoSizing = true;
            existingValueText.fontSizeMin = 15f;
            existingValueText.fontSizeMax = withProgressBar ? 20f : 21f;
            existingValueText.fontStyle = FontStyles.Bold;
            existingValueText.color = DarkInk;
            existingValueText.alignment = TextAlignmentOptions.MidlineLeft;
            existingValueText.textWrappingMode = TextWrappingModes.NoWrap;
            existingValueText.overflowMode = TextOverflowModes.Overflow;
        }

        if (withProgressBar)
        {
            GameObject barBg = new GameObject("AwarenessBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(card.transform, false);
            RectTransform bbRt = barBg.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0f, 0f);
            bbRt.anchoredPosition = new Vector2(YaziSolBosluk, 12f);
            bbRt.sizeDelta = new Vector2(-(YaziSolBosluk + 14f), 10f);

            Image bbImg = barBg.GetComponent<Image>();
            bbImg.color = DarkInk;

            GameObject barFill = new GameObject("AwarenessBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform bfRt = barFill.GetComponent<RectTransform>();
            bfRt.anchorMin = new Vector2(0f, 0f);
            bfRt.anchorMax = new Vector2(1f, 1f);
            bfRt.offsetMin = new Vector2(2f, 2f);
            bfRt.offsetMax = new Vector2(-2f, -2f);

            progressFill = barFill.GetComponent<Image>();
            progressFill.sprite = GetSolidWhiteSprite();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.color = new Color32(72, 209, 108, 255);
            progressFill.fillAmount = 0.2f;
        }

        return rt;
    }

    // BİNALARIN ÜSTÜNE GELİNCE GÖZÜKEN İSİM & LVL ETİKETİ
    private void BuildHoverTooltip()
    {
        GameObject tooltipObj = new GameObject("BuildingHoverTooltip", typeof(RectTransform), typeof(Image));
        tooltipObj.transform.SetParent(transform, false);
        // Etiket fareyle her karede hareket ediyor; kendi alt Canvas'ı olunca sadece o yeniden çizilir, bütün Ana Canvas değil
        tooltipObj.AddComponent<Canvas>();
        hoverTooltipRt = tooltipObj.GetComponent<RectTransform>();
        hoverTooltipRt.anchorMin = new Vector2(0.5f, 0.5f);
        hoverTooltipRt.anchorMax = new Vector2(0.5f, 0.5f);
        hoverTooltipRt.pivot = new Vector2(0.5f, 0f);
        hoverTooltipRt.sizeDelta = new Vector2(230f, 44f);

        Image bg = tooltipObj.GetComponent<Image>();
        bg.sprite = GetCreamBoxSprite(CreamBg, CreamShadow);
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        hoverTooltipText = CreateText(
            tooltipObj.transform, "TooltipLabel", sharedFont,
            "KONUTLAR • LVL 1", 17f, FontStyles.Bold, DarkInk, TextAlignmentOptions.Center);
        StretchFull(hoverTooltipText.rectTransform);
        hoverTooltipText.rectTransform.offsetMin = new Vector2(10f, 5f);
        hoverTooltipText.rectTransform.offsetMax = new Vector2(-10f, -2f);

        tooltipObj.SetActive(false);
    }

    // Ana Canvas üzerindeki AfetRaporu, OyunSonu ve IpucuSistemi panellerini piksel-art prototip tasarımımıza dönüştürür
    private void StyleExistingExternalPanels()
    {
        // 1) İpucu Şeridi (Krem kutu + Koyu çerçeve)
        IpucuSistemi ipucu = GetComponentInParent<IpucuSistemi>();
        if (ipucu == null) ipucu = FindAnyObjectByType<IpucuSistemi>();
        if (ipucu != null && ipucu.serit != null)
        {
            Image seritImg = ipucu.serit.GetComponent<Image>();
            if (seritImg != null)
            {
                seritImg.sprite = GetCreamBoxSprite(CreamBg, CreamShadow);
                seritImg.type = Image.Type.Sliced;
                seritImg.color = Color.white;
                seritImg.raycastTarget = false;
            }
            RectTransform sRt = ipucu.serit.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.sizeDelta = new Vector2(980f, 68f);
                sRt.anchoredPosition = new Vector2(0f, 22f);
            }
            if (ipucu.ipucuText != null)
            {
                ipucu.ipucuText.color = DarkInk;
                ipucu.ipucuText.fontSize = 21f;
                ipucu.ipucuText.fontStyle = FontStyles.Bold;
            }
        }

        // 2) Afet Raporu Paneli
        AfetRaporu rapor = GetComponentInParent<AfetRaporu>();
        if (rapor == null) rapor = FindAnyObjectByType<AfetRaporu>();
        if (rapor != null && rapor.panel != null)
        {
            Transform pencere = rapor.panel.transform.Find("Pencere");
            if (pencere != null)
            {
                Image pImg = pencere.GetComponent<Image>();
                if (pImg != null)
                {
                    pImg.sprite = GetSteppedModalSprite();
                    pImg.type = Image.Type.Sliced;
                    pImg.color = Color.white;
                }
                if (rapor.baslikText != null) rapor.baslikText.color = RibbonRed;
                if (rapor.savunmaText != null) rapor.savunmaText.color = DarkInk;
                if (rapor.vefatText != null) rapor.vefatText.color = RibbonRedDark;
                if (rapor.kurtarilanText != null) rapor.kurtarilanText.color = GreenBtnDark;
                if (rapor.hasarText != null) rapor.hasarText.color = DarkInk;
                if (rapor.nufusText != null) rapor.nufusText.color = DarkInk;

                Transform devamBtnTr = pencere.Find("DevamButonu");
                if (devamBtnTr != null)
                {
                    Button dBtn = devamBtnTr.GetComponent<Button>();
                    TMP_Text dTxt = devamBtnTr.GetComponentInChildren<TMP_Text>();
                    if (dBtn != null && dTxt != null)
                    {
                        Image dImg = dBtn.GetComponent<Image>();
                        dImg.sprite = GetTwoToneButtonSprite(GreenBtn, GreenBtnLeft, GreenBtnDark);
                        dImg.type = Image.Type.Sliced;
                        dImg.color = Color.white;
                        dTxt.color = DarkInk;
                        dTxt.fontStyle = FontStyles.Bold;
                    }
                }
            }
        }

        // 3) Oyun Sonu Paneli
        OyunSonu oyunSonu = GetComponentInParent<OyunSonu>();
        if (oyunSonu == null) oyunSonu = FindAnyObjectByType<OyunSonu>();
        if (oyunSonu != null && oyunSonu.panel != null)
        {
            Transform pencere = oyunSonu.panel.transform.Find("Pencere");
            if (pencere != null)
            {
                Image pImg = pencere.GetComponent<Image>();
                if (pImg != null)
                {
                    pImg.sprite = GetSteppedModalSprite();
                    pImg.type = Image.Type.Sliced;
                    pImg.color = Color.white;
                }
                if (oyunSonu.baslikText != null) oyunSonu.baslikText.color = RibbonBlueDark;
                if (oyunSonu.dereceText != null) oyunSonu.dereceText.color = GoldBtnDark;
                if (oyunSonu.istatistikText != null) oyunSonu.istatistikText.color = DarkInk;
                if (oyunSonu.afetOzetiText != null) oyunSonu.afetOzetiText.color = MutedText;

                Transform tekrarTr = pencere.Find("TekrarOynaButonu");
                if (tekrarTr != null)
                {
                    Image tImg = tekrarTr.GetComponent<Image>();
                    TMP_Text tTxt = tekrarTr.GetComponentInChildren<TMP_Text>();
                    if (tImg != null)
                    {
                        tImg.sprite = GetTwoToneButtonSprite(GreenBtn, GreenBtnLeft, GreenBtnDark);
                        tImg.type = Image.Type.Sliced;
                        tImg.color = Color.white;
                    }
                    if (tTxt != null)
                    {
                        tTxt.color = DarkInk;
                        tTxt.fontStyle = FontStyles.Bold;
                    }
                }

                Transform menuTr = pencere.Find("AnaMenuButonu");
                if (menuTr != null)
                {
                    Image mImg = menuTr.GetComponent<Image>();
                    TMP_Text mTxt = menuTr.GetComponentInChildren<TMP_Text>();
                    if (mImg != null)
                    {
                        mImg.sprite = GetTwoToneButtonSprite(GoldBtn, GoldBtnLeft, GoldBtnDark);
                        mImg.type = Image.Type.Sliced;
                        mImg.color = Color.white;
                    }
                    if (mTxt != null)
                    {
                        mTxt.color = DarkInk;
                        mTxt.fontStyle = FontStyles.Bold;
                    }
                }
            }
        }
    }

    private void Update()
    {
        UpdateHudLayout(false);
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        GelirEtkinligineBaglan(gm);

        var d = DengeAyarlari.Aktif;
        int kalan = d.SonrakiAfeteKalanGun(gm.gun);
        int tavan = Mathf.RoundToInt(d.FarkindalikTavani(gm.okulLevel));
        int gelir = Ekonomi.GunlukGelir();
        int kapasite = Ekonomi.NufusKapasitesi();
        int fark = Mathf.RoundToInt(gm.farkindalikYuzdesi);

        if (kalan != sonKalan && gunSubLabelText != null)
        {
            gunSubLabelText.text = kalan > 0
                ? "AFETE " + kalan + " GÜN"
                : (kalan == 0 ? "AFET GÜNÜ!" : "GÜN SAYACI");
        }
        if (tavan != sonTavan && farkindalikSubLabelText != null)
            farkindalikSubLabelText.text = "FARKINDALIK (MAKS %" + tavan + ")";
        if (gm.gun != sonGun && gunText != null)
            gunText.text = "Gün: " + gm.gun + "/" + d.toplamGun;
        UpdateParaAnimasyonlari(gm, gelir);
        int gelirGosterimi = gelirAnimasyonuAktif ? gosterilenGelir : gelir;
        if (paraText != null &&
            (gosterilenBakiye != sonGosterilenBakiye || gelirGosterimi != sonGosterilenGelir))
        {
            paraText.text = gosterilenBakiye + " coin (+" + gelirGosterimi + " /g)";
            sonGosterilenBakiye = gosterilenBakiye;
            sonGosterilenGelir = gelirGosterimi;
        }
        if ((gm.nufus != sonNufus || kapasite != sonKapasite) && nufusText != null)
            nufusText.text = gm.nufus + "/" + kapasite;
        if (fark != sonFark)
        {
            if (farkindalikText != null) farkindalikText.text = "%" + fark;
            if (awarenessFillBar != null) awarenessFillBar.fillAmount = Mathf.Clamp01(gm.farkindalikYuzdesi / 100f);
        }
        sonKalan = kalan; sonTavan = tavan; sonGun = gm.gun; sonPara = gm.para; sonGelir = gelir;
        sonNufus = gm.nufus; sonKapasite = kapasite; sonFark = fark;

        RefreshSpeedButtonVisuals();
        UpdateBuildingHoverLabel(gm);
    }

    private void GelirEtkinligineBaglan(GameManager manager)
    {
        if (manager == incomeEventManager) return;
        GelirEtkinligindenAyril();
        if (manager == null || !isActiveAndEnabled) return;

        incomeEventManager = manager;
        incomeEventManager.GunlukGelirEklendi += GunlukGelirEklendi;
    }

    private void GelirEtkinligindenAyril()
    {
        if (incomeEventManager == null) return;
        incomeEventManager.GunlukGelirEklendi -= GunlukGelirEklendi;
        incomeEventManager = null;
    }

    private void GunlukGelirEklendi(int miktar)
    {
        GameManager manager = incomeEventManager;
        if (manager == null) return;

        if (!paraGosterimiHazir)
        {
            // GelirKaydet, bakiye arttıktan sonra çağrılır; önceki değer buradan güvenle bulunur.
            gosterilenBakiye = manager.para - miktar;
            paraGosterimiHazir = true;
        }

        bakiyeAnimasyonBaslangici = gosterilenBakiye;
        bakiyeAnimasyonHedefi = manager.para;
        bakiyeAnimasyonSuresi = 0f;
        bakiyeAnimasyonuAktif = miktar != 0;
        gosterilenGelir = 0;
        gelirAnimasyonHedefi = Mathf.Max(0, miktar);
        gelirAnimasyonSuresi = 0f;
        gelirAnimasyonuAktif = miktar > 0;
        sonPara = manager.para;
    }

    private void UpdateParaAnimasyonlari(GameManager manager, int gunlukGelir)
    {
        if (!paraGosterimiHazir)
        {
            gosterilenBakiye = manager.para;
            paraGosterimiHazir = true;
        }
        else if (manager.para != sonPara)
        {
            // Harcama veya başka bir değişiklik olduğunda bakiye gerçek değere anında geçer.
            gosterilenBakiye = manager.para;
            bakiyeAnimasyonuAktif = false;
        }

        if (bakiyeAnimasyonuAktif)
        {
            bakiyeAnimasyonSuresi += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(bakiyeAnimasyonSuresi / ParaAnimasyonSuresi);
            gosterilenBakiye = Mathf.RoundToInt(Mathf.Lerp(
                bakiyeAnimasyonBaslangici, bakiyeAnimasyonHedefi, EaseOutCubic(t)));
            if (t >= 1f)
            {
                gosterilenBakiye = bakiyeAnimasyonHedefi;
                bakiyeAnimasyonuAktif = false;
            }
        }

        if (gelirAnimasyonuAktif)
        {
            gelirAnimasyonSuresi += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gelirAnimasyonSuresi / ParaAnimasyonSuresi);
            gosterilenGelir = Mathf.RoundToInt(Mathf.Lerp(0f, gelirAnimasyonHedefi, EaseOutCubic(t)));
            if (t >= 1f)
            {
                gosterilenGelir = gelirAnimasyonHedefi;
                gelirAnimasyonuAktif = false;
            }
        }

        sonPara = manager.para;
        sonGelir = gunlukGelir;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private void UpdateHudLayout(bool force)
    {
        if (leftHudGroupRt == null || rightHudColumnRt == null) return;
        if (hudCanvasRt == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) hudCanvasRt = canvas.rootCanvas.GetComponent<RectTransform>();
        }
        if (hudCanvasRt == null) return;

        if (hudBarRt == null) hudBarRt = leftHudGroupRt.parent as RectTransform;
        if (hudBarRt == null) return;

        Rect safeArea = Screen.safeArea;
        bool safeChanged = safeArea != hudLastSafeArea || Screen.width != hudLastScreenWidth || Screen.height != hudLastScreenHeight;
        float canvasWidth = hudBarRt.rect.width;
        if (canvasWidth <= 0f || (!force && !safeChanged && Mathf.Abs(canvasWidth - lastHudCanvasWidth) < 0.5f)) return;
        lastHudCanvasWidth = canvasWidth;
        hudLastSafeArea = safeArea;
        hudLastScreenWidth = Screen.width;
        hudLastScreenHeight = Screen.height;

        Canvas hudCanvas = hudBarRt.GetComponentInParent<Canvas>();
        Camera uiCamera = hudCanvas != null && hudCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? hudCanvas.rootCanvas.worldCamera
            : null;
        Vector2 safeMin = hudBarRt.rect.min;
        Vector2 safeMax = hudBarRt.rect.max;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hudBarRt, safeArea.min, uiCamera, out safeMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(hudBarRt, safeArea.max, uiCamera, out safeMax))
        {
            safeMin = hudBarRt.rect.min;
            safeMax = hudBarRt.rect.max;
        }

        float leftMargin = Mathf.Max(HudKenarBoslugu, safeMin.x - hudBarRt.rect.xMin + HudKenarBoslugu);
        float rightMargin = Mathf.Max(HudKenarBoslugu, hudBarRt.rect.xMax - safeMax.x + HudKenarBoslugu);
        float availableWidth = Mathf.Max(1f, safeMax.x - safeMin.x - HudKenarBoslugu * 2f);
        float userScale = UstPanelOlcegi;
        float leftScale = Mathf.Min(1f, availableWidth / (leftHudGroupRt.sizeDelta.x * userScale)) * userScale;
        float rightScale = Mathf.Min(1f, availableWidth / (rightHudColumnRt.sizeDelta.x * userScale)) * userScale;
        bool placeControlsBelow = leftHudGroupRt.sizeDelta.x * leftScale +
                                  rightHudColumnRt.sizeDelta.x * rightScale + HudGruplarArasiBosluk > availableWidth;

        float requiredHeight = placeControlsBelow
            ? leftHudGroupRt.sizeDelta.y * leftScale + 12f + rightHudColumnRt.sizeDelta.y * rightScale
            : Mathf.Max(leftHudGroupRt.sizeDelta.y * leftScale, rightHudColumnRt.sizeDelta.y * rightScale);
        float safeHeight = Mathf.Max(1f, safeMax.y - safeMin.y);
        float safeTopInset = Mathf.Max(18f, -safeMax.y + 18f);
        float verticalRoom = Mathf.Max(1f, safeHeight - safeTopInset - 18f);
        float verticalFit = Mathf.Min(1f, verticalRoom / Mathf.Max(1f, requiredHeight));
        leftScale *= verticalFit;
        rightScale *= verticalFit;
        leftHudGroupRt.localScale = Vector3.one * leftScale;
        rightHudColumnRt.localScale = Vector3.one * rightScale;

        requiredHeight = placeControlsBelow
            ? leftHudGroupRt.sizeDelta.y * leftScale + 12f + rightHudColumnRt.sizeDelta.y * rightScale
            : Mathf.Max(leftHudGroupRt.sizeDelta.y * leftScale, rightHudColumnRt.sizeDelta.y * rightScale);
        float maxExtraInset = Mathf.Max(0f, safeHeight - safeTopInset - requiredHeight - 18f);
        float topOffset = safeTopInset + Mathf.Min(UstPanelDikeyBosluk, maxExtraInset);

        leftHudGroupRt.anchoredPosition = new Vector2(leftMargin, -topOffset);
        rightHudColumnRt.anchoredPosition = placeControlsBelow
            ? new Vector2(-rightMargin, -(topOffset + leftHudGroupRt.sizeDelta.y * leftScale + 12f))
            : new Vector2(-rightMargin, -topOffset);
    }

    private void UpdateBuildingHoverLabel(GameManager gm)
    {
        if (hoverTooltipRt == null) return;

        // Modal açıkken hover etiketi gizlensin
        if (gm.oyunDurduMu || gm.afetOlduMu || gm.oyunBittiMi)
        {
            if (hoverTooltipRt.gameObject.activeSelf)
                hoverTooltipRt.gameObject.SetActive(false);
            return;
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            mousePos = Mouse.current.position.ReadValue();
#else
        mousePos = Input.mousePosition;
#endif

        // Fare kıpırdamadıysa aynı binayı tekrar aramaya gerek yok (oyun 3D; 2D fizik sorgusu yapılmıyor)
        BuildingVisuals bv = sonHoverBina;
        if ((mousePos - sonFarePos).sqrMagnitude > 0.25f)
        {
            sonFarePos = mousePos;
            bv = null;
            Ray ray = mainCam.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit3D, 500f))
                bv = hit3D.collider.GetComponentInParent<BuildingVisuals>();
            sonHoverBina = bv;
        }

        if (bv != null)
        {
            string bName = GetBuildingDisplayName(bv.binaTuru);
            int lvl = gm.Level(bv.binaTuru);
            bool hasarli = gm.Hasarli(bv.binaTuru);
            string yeni = hasarli
                ? bName + " • LVL " + lvl + " (HASARLI)"
                : bName + " • LVL " + lvl;
            if (hoverTooltipText.text != yeni) hoverTooltipText.text = yeni;

            if (!hoverTooltipRt.gameObject.activeSelf)
                hoverTooltipRt.gameObject.SetActive(true);

            RectTransform canvasRt = transform as RectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, mousePos + new Vector2(0f, 26f), null, out localPoint))
            {
                hoverTooltipRt.anchoredPosition = localPoint;
            }
            return;
        }

        if (hoverTooltipRt.gameObject.activeSelf)
            hoverTooltipRt.gameObject.SetActive(false);
    }

    public static string GetBuildingDisplayName(BinaTuru bina)
    {
        switch (bina)
        {
            case BinaTuru.TemelBina: return "KONUTLAR";
            case BinaTuru.Okul:      return "OKUL";
            case BinaTuru.Akut:      return "AKUT MERKEZİ";
            case BinaTuru.Itfaiye:   return "İTFAİYE";
            case BinaTuru.DenizSeti: return "DENİZ SETİ";
            case BinaTuru.Altyapi:   return "ALTYAPI";
            default:                 return "BİNA";
        }
    }

    // =========================================================================
    // PROSEDÜREL PİKSEL-ART / İZOMETRİK UI SPRITE VE BİLEŞEN ÜRETİCİLERİ
    // =========================================================================

    private static Sprite solidWhiteSprite;
    private static Sprite steppedModalSprite;
    private static Sprite darkSlotSprite;
    private static readonly Dictionary<ulong, Sprite> creamBoxCache = new Dictionary<ulong, Sprite>();
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // Domain Reload kapalıyken Play'den çıkınca oluşturulan sprite'lar yok olur; önbelleği her Play başında temizle
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnbellegiSifirla()
    {
        creamBoxCache.Clear();
        spriteCache.Clear();
        solidWhiteSprite = null; steppedModalSprite = null; darkSlotSprite = null;
    }

    private static ulong RenkAnahtari(Color32 a, Color32 b)
    {
        return ((ulong)a.r << 56) | ((ulong)a.g << 48) | ((ulong)a.b << 40) | ((ulong)a.a << 32)
             | ((ulong)b.r << 24) | ((ulong)b.g << 16) | ((ulong)b.b << 8) | b.a;
    }

    private static bool OnbellekteVar(string anahtar, out Sprite sprite)
    {
        return spriteCache.TryGetValue(anahtar, out sprite) && sprite != null;
    }

    public static Sprite GetSolidWhiteSprite()
    {
        if (solidWhiteSprite != null) return solidWhiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
        tex.Apply();
        solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return solidWhiteSprite;
    }

    // 1) KREM KUTU SPRITE: Kalın koyu çerçeve (#141D2B) + Alt blok gölge + Yuvarlatılmış/Stepped köşe
    public static Sprite GetCreamBoxSprite(Color32 fillColor, Color32 bottomShade)
    {
        ulong key = RenkAnahtari(fillColor, bottomShade);
        if (creamBoxCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int w = 48, h = 48;
        int border = 3;
        int shadow = 5;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool cutOuter = (x < 3 && y < 3) || (x >= w - 3 && y < 3) || (x < 3 && y >= h - 3) || (x >= w - 3 && y >= h - 3);
                if (cutOuter)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isBorder = x < border || x >= w - border || y < border + 2 || y >= h - border;
                bool isCornerStep = (x < 5 && y < 5) || (x >= w - 5 && y < 5) || (x < 5 && y >= h - 5) || (x >= w - 5 && y >= h - 5);

                if (y < 2)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (isBorder || isCornerStep)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y < border + shadow + 1)
                {
                    tex.SetPixel(x, y, bottomShade);
                }
                else
                {
                    tex.SetPixel(x, y, fillColor);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 12, 10, 10));
        creamBoxCache[key] = sp;
        return sp;
    }

    // 2) STEPPED PIXEL-ART MODAL KARTI (#E4F4F3 açık buz mavisi gövde + basamaklı köşe + kalın çerçeve)
    public static Sprite GetSteppedModalSprite()
    {
        if (steppedModalSprite != null) return steppedModalSprite;

        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                if ((dx < 4 && dy < 8) || (dx < 8 && dy < 4))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isOuterBorder = dx < 4 || dy < 4 || (dx < 8 && dy < 8) || (dx < 11 && dy < 6) || (dx < 6 && dy < 11);
                if (isOuterBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y < 11)
                {
                    tex.SetPixel(x, y, ModalCyanShadow);
                }
                else if (y >= h - 7)
                {
                    tex.SetPixel(x, y, new Color32(244, 252, 251, 255));
                }
                else
                {
                    tex.SetPixel(x, y, ModalCyanBg);
                }
            }
        }

        tex.Apply();
        steppedModalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        return steppedModalSprite;
    }

    // 3) KOYU İÇ YUVA (DARK SLOT) SPRITE (#1B2A38)
    public static Sprite GetDarkSlotSprite()
    {
        if (darkSlotSprite != null) return darkSlotSprite;

        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                if (dx < 3 && dy < 3)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dx < 3 || dy < 3 || (dx < 5 && dy < 5))
                {
                    tex.SetPixel(x, y, DarkSlotBorder);
                }
                else
                {
                    tex.SetPixel(x, y, DarkSlotBg);
                }
            }
        }

        tex.Apply();
        darkSlotSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        return darkSlotSprite;
    }

    // 4) ÜST KURDELE (RIBBON BANNER) OLUŞTURUCU
    public static Sprite GetRibbonMainSprite(Color32 fill, Color32 bottomLip)
    {
        string anahtar = "ribbon" + RenkAnahtari(fill, bottomLip);
        if (OnbellekteVar(anahtar, out Sprite onceki)) return onceki;
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < h; y++)
        {
            Color32 c = y < 5 ? bottomLip : fill;
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return spriteCache[anahtar] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 6, 4, 4));
    }

    public static Sprite GetRibbonTailSprite(bool leftSide)
    {
        string anahtar = leftSide ? "tailL" : "tailR";
        if (OnbellekteVar(anahtar, out Sprite onceki)) return onceki;
        int w = 32, h = 40;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / h;
            float notchDepth = (0.5f - Mathf.Abs(ny - 0.5f)) * 0.7f;
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                bool inside = leftSide ? (nx >= notchDepth) : (nx <= 1f - notchDepth);
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return spriteCache[anahtar] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public static RectTransform CreateRibbonBanner(
        RectTransform modalCard, TMP_FontAsset font, string title,
        Color32 mainColor, Color32 lipColor, Color32 tailColor,
        out Image mainImg, out Image leftTailImg, out Image rightTailImg, out TMP_Text titleText)
    {
        GameObject ribbonObj = new GameObject("RibbonBanner", typeof(RectTransform), typeof(Image));
        ribbonObj.transform.SetParent(modalCard, false);
        RectTransform ribbonRt = ribbonObj.GetComponent<RectTransform>();
        ribbonRt.anchorMin = new Vector2(0f, 1f);
        ribbonRt.anchorMax = new Vector2(1f, 1f);
        ribbonRt.pivot = new Vector2(0.5f, 1f);
        ribbonRt.anchoredPosition = new Vector2(0f, -18f);
        ribbonRt.sizeDelta = new Vector2(44f, 68f);

        GameObject tailL = new GameObject("TailLeft", typeof(RectTransform), typeof(Image));
        tailL.transform.SetParent(ribbonRt, false);
        RectTransform tlRt = tailL.GetComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0f, 0f);
        tlRt.anchorMax = new Vector2(0f, 0f);
        tlRt.pivot = new Vector2(0f, 0f);
        tlRt.anchoredPosition = new Vector2(-20f, -12f);
        tlRt.sizeDelta = new Vector2(36f, 52f);
        leftTailImg = tailL.GetComponent<Image>();
        leftTailImg.sprite = GetRibbonTailSprite(true);
        leftTailImg.color = tailColor;

        GameObject tailR = new GameObject("TailRight", typeof(RectTransform), typeof(Image));
        tailR.transform.SetParent(ribbonRt, false);
        RectTransform trRt = tailR.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(1f, 0f);
        trRt.anchorMax = new Vector2(1f, 0f);
        trRt.pivot = new Vector2(1f, 0f);
        trRt.anchoredPosition = new Vector2(20f, -12f);
        trRt.sizeDelta = new Vector2(36f, 52f);
        rightTailImg = tailR.GetComponent<Image>();
        rightTailImg.sprite = GetRibbonTailSprite(false);
        rightTailImg.color = tailColor;

        mainImg = ribbonObj.GetComponent<Image>();
        mainImg.sprite = GetRibbonMainSprite(mainColor, lipColor);
        mainImg.type = Image.Type.Sliced;

        titleText = CreateText(ribbonRt, "RibbonTitle", font, title, 28f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = Vector2.zero;
        titleText.rectTransform.anchorMax = Vector2.one;
        titleText.rectTransform.offsetMin = new Vector2(16f, 6f);
        titleText.rectTransform.offsetMax = new Vector2(-16f, 0f);

        return ribbonRt;
    }

    // 5) İKİ TONLU (TWO-TONE) ARCADE BUTON SPRITE & YARDIMCISI (IMAGE 2 STİLİ)
    public static Sprite GetTwoToneButtonSprite(Color32 mainFill, Color32 leftIconFill, Color32 shadowColor)
    {
        string anahtar = "btn" + RenkAnahtari(mainFill, leftIconFill) + "_" + RenkAnahtari(shadowColor, shadowColor);
        if (OnbellekteVar(anahtar, out Sprite onceki)) return onceki;
        int w = 128, h = 48;
        int leftWidth = 44;
        int border = 3;
        int shadow = 5;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool cornerCut = (x < 3 && y < 3) || (x >= w - 3 && y < 3) || (x < 3 && y >= h - 3) || (x >= w - 3 && y >= h - 3);
                if (cornerCut) { tex.SetPixel(x, y, Color.clear); continue; }

                if (y < shadow)
                {
                    tex.SetPixel(x, y, shadowColor);
                }
                else if (x < border || x >= w - border || y < shadow + border || y >= h - border || (x >= leftWidth - border && x < leftWidth))
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (x < leftWidth)
                {
                    tex.SetPixel(x, y, leftIconFill);
                }
                else
                {
                    tex.SetPixel(x, y, mainFill);
                }
            }
        }
        tex.Apply();
        return spriteCache[anahtar] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(46, 10, 8, 6));
    }

    public static Button CreateTwoToneArcadeButton(
        Transform parent, string name, TMP_FontAsset font, string labelText,
        Color32 mainFill, Color32 leftFill, Color32 shadowCol,
        Color32 textColor, IconType iconType, out TMP_Text outLabel)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = GetTwoToneButtonSprite(mainFill, leftFill, shadowCol);
        btnImg.type = Image.Type.Sliced;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject iconObj = new GameObject("LeftIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(22f, 2f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = GetColoredIconSprite(iconType, Color.white);
        iconImg.raycastTarget = false;

        outLabel = CreateText(btnObj.transform, "Label", font, labelText, 20f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
        outLabel.rectTransform.anchorMin = Vector2.zero;
        outLabel.rectTransform.anchorMax = Vector2.one;
        outLabel.rectTransform.offsetMin = new Vector2(72f, 5f);
        outLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);

        return btn;
    }

    // 6) İKON ÜRETİCİ (BASİT İNSAN FİGÜRÜ, ALTIGEN SİKKE, BASİT KİTAP, TAKVİM, YILDIZ)
    public static Sprite GetColoredIconSprite(IconType type, Color32 mainColor)
    {
        string anahtar = "icon" + (int)type + "_" + RenkAnahtari(mainColor, mainColor);
        if (OnbellekteVar(anahtar, out Sprite onceki)) return onceki;
        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                tex.SetPixel(x, y, EvaluateIconPixel(type, nx, ny, mainColor));
            }
        }
        tex.Apply();
        return spriteCache[anahtar] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Color EvaluateIconPixel(IconType type, float x, float y, Color32 mainColor)
    {
        switch (type)
        {
            case IconType.Person:
                // Basit İnsan Figürü (Kafa çemberi + Omuz/Gövde yarım daire + Koyu dış kontur)
                float dHead = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.70f));
                float dBody = Vector2.Distance(new Vector2(x, (y - 0.14f) * 1.15f + 0.14f), new Vector2(0.5f, 0.14f));
                bool inHeadOuter = dHead <= 0.21f;
                bool inHeadInner = dHead <= 0.14f;
                bool inBodyOuter = y >= 0.14f && y <= 0.46f && dBody <= 0.36f;
                bool inBodyInner = y >= 0.20f && y <= 0.45f && dBody <= 0.29f;
                if (inHeadInner || inBodyInner) return mainColor;
                if (inHeadOuter || inBodyOuter) return DarkInk;
                return Color.clear;

            case IconType.HexCoin:
                // Altıgen Para Sikkesi
                float dx = Mathf.Abs(x - 0.5f);
                float dy = Mathf.Abs(y - 0.5f);
                float hexDist = Mathf.Max(dx * 0.866f + dy * 0.5f, dy);
                if (hexDist > 0.42f) return Color.clear;
                if (hexDist > 0.34f) return DarkInk;
                if (hexDist > 0.26f) return new Color32(229, 169, 28, 255);
                return mainColor;

            case IconType.Book:
                // Basic Açık Kitap İkonu
                bool inBookBox = x >= 0.10f && x <= 0.90f && y >= 0.18f && y <= 0.82f;
                if (!inBookBox) return Color.clear;
                bool border = x < 0.17f || x > 0.83f || y < 0.25f || y > 0.75f || Mathf.Abs(x - 0.5f) < 0.045f;
                if (border) return DarkInk;
                bool pageLines = (Mathf.Abs(y - 0.58f) < 0.035f || Mathf.Abs(y - 0.42f) < 0.035f)
                              && ((x >= 0.24f && x <= 0.42f) || (x >= 0.58f && x <= 0.76f));
                if (pageLines) return mainColor;
                return Color.white;

            case IconType.Calendar:
                // Takvim İkonu
                if (x < 0.12f || x > 0.88f || y < 0.12f || y > 0.86f) return Color.clear;
                if (x < 0.19f || x > 0.81f || y < 0.19f || y > 0.79f || (y > 0.58f && y < 0.65f)) return DarkInk;
                if (y >= 0.65f) return mainColor;
                return Color.white;

            case IconType.Star:
                // 5 köşeli yıldız: koyu dış çizgi + dolgu (diğer ikonlarla aynı piksel stil)
                if (YildizIcinde(x, y, 0.49f, 0.21f)) return YildizIcinde(x, y, 0.38f, 0.16f) ? (Color)mainColor : (Color)DarkInk;
                return Color.clear;

            case IconType.UpArrow:
                bool head = y >= 0.50f && y <= 0.86f && Mathf.Abs(x - 0.5f) <= (0.86f - y) * 0.95f;
                bool stem = y >= 0.16f && y <= 0.52f && Mathf.Abs(x - 0.5f) <= 0.13f;
                return (head || stem) ? (Color)mainColor : Color.clear;

            case IconType.Play:
                bool tri = x >= 0.26f && x <= 0.80f && Mathf.Abs(y - 0.5f) <= (0.80f - x) * 0.65f;
                return tri ? (Color)mainColor : Color.clear;

            case IconType.CloseX:
                float d1 = Mathf.Abs((x - 0.5f) - (y - 0.5f));
                float d2 = Mathf.Abs((x - 0.5f) + (y - 0.5f));
                bool inX = x >= 0.20f && x <= 0.80f && y >= 0.20f && y <= 0.80f && (d1 <= 0.11f || d2 <= 0.11f);
                return inX ? (Color)mainColor : Color.clear;
        }
        return mainColor;
    }

    // Merkezi (0.5, 0.48), sivri ucu yukarıda olan 5 köşeli yıldızın içinde mi?
    private static bool YildizIcinde(float x, float y, float disYaricap, float icYaricap)
    {
        const int kose = 10;
        bool icinde = false;
        float oncekiX = 0f, oncekiY = 0f;
        for (int i = 0; i <= kose; i++)
        {
            float aci = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? disYaricap : icYaricap;
            float px = 0.5f + Mathf.Cos(aci) * r, py = 0.48f + Mathf.Sin(aci) * r;
            if (i > 0 && ((py > y) != (oncekiY > y)) && (x < (oncekiX - px) * (y - py) / (oncekiY - py) + px))
                icinde = !icinde;
            oncekiX = px; oncekiY = py;
        }
        return icinde;
    }

    public static TMP_Text CreateText(
        Transform parent, string name, TMP_FontAsset font, string content,
        float fontSize, FontStyles style, Color32 color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = TurkishTMPFontSupport.EnsureFont(font != null ? font : TMP_Settings.defaultFontAsset);
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
