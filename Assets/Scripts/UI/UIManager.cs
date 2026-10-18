using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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
        CloseX,
        Gear,
        Guide,
        Shield,
        Clock,
        Heart,
        Refresh,
        Home,
        MenuGrid,
        Volume,
        Checkmark,
        Exit
    }

    // --- COHESIVE ARCADE COLOR PALETTE ---
    public static readonly Color32 DarkInk         = CivicUI.DarkInk;
    public static readonly Color32 CreamBg         = CivicUI.Cream;
    public static readonly Color32 CreamShadow     = CivicUI.CreamShadow;
    public static readonly Color32 ModalCyanBg     = CivicUI.Mint;
    public static readonly Color32 ModalCyanShadow = CivicUI.MintShadow;
    public static readonly Color32 DarkSlotBg      = CivicUI.DarkSlotBg;
    public static readonly Color32 DarkSlotBorder  = CivicUI.DarkSlotBorder;

    public static readonly Color32 RibbonBlue      = CivicUI.RibbonBlue;
    public static readonly Color32 RibbonBlueDark  = CivicUI.RibbonBlueLip;
    public static readonly Color32 RibbonRed       = CivicUI.RibbonRed;
    public static readonly Color32 RibbonRedDark   = CivicUI.RibbonRedLip;

    public static readonly Color32 GreenBtn        = CivicUI.GreenBtn;
    public static readonly Color32 GreenBtnLeft    = CivicUI.GreenBtnLeft;
    public static readonly Color32 GreenBtnDark    = CivicUI.GreenBtnDark;

    public static readonly Color32 GoldBtn         = CivicUI.GoldBtn;
    public static readonly Color32 GoldBtnLeft     = CivicUI.GoldBtnLeft;
    public static readonly Color32 GoldBtnDark     = CivicUI.GoldBtnDark;
    public static readonly Color32 GoldStar        = CivicUI.GoldStar;

    public static readonly Color32 BlueBtn         = CivicUI.CyanBtn;
    public static readonly Color32 BlueBtnLeft     = CivicUI.CyanBtnLeft;
    public static readonly Color32 BlueBtnDark     = CivicUI.CyanBtnDark;

    public static readonly Color32 RedClose        = CivicUI.CoralBtn;
    public static readonly Color32 RedCloseDark    = CivicUI.CoralBtnDark;
    public static readonly Color32 MutedText       = CivicUI.Muted;

    [Header("Üst Bar Metinleri")]
    public TMP_Text gunText;
    public TMP_Text paraText;
    public TMP_Text nufusText;
    public TMP_Text farkindalikText;

    private TMP_FontAsset sharedFont;
    private Image awarenessFillBar;
    private TMP_Text gunSubLabelText;
    private TMP_Text farkindalikSubLabelText;
    private RectTransform disasterAlertBadgeRt;
    private Image disasterAlertBadgeImg;
    private TMP_Text disasterAlertBadgeText;

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
    private const float IkonOlcek = 1.0f;
    private const float IkonYuvasi = 48f;
    private const float IkonBoyu = 30f;
    private const float KartYuksekligi = 74f;
    private const float YaziSolBosluk = 68f;
    private const float EkGenislik = 0f;
    private const float HudKenarBoslugu = 20f;
    private const float HudGruplarArasiBosluk = 20f;
    private const float SolHudTasarimOlcegi = 1.5f;
    private const float SagHudGenisligi = 325f;
    private const float SagGunKartiYuksekligi = 128f;
    private const float SagUyariYuksekligi = 30f;
    private const float SagKontrolYuksekligi = 46f;
    private const float SagHudKartiAraligi = 6f;
    private const float SagUyariKontrolAraligi = 8f;
    private const float SagGunIcerikOlcegi = 1.3f;
    private const float UstPanelYeniStandartCarpani = 1.25f;
    private const string UstPanelOlcegiKey = "ui_ust_panel_olcegi_v2";
    private const string UstPanelDikeyBoslukKey = "ui_ust_panel_dikey_bosluk";
    private static System.Action ustPanelAyarDegisti;

    public static float UstPanelOlcegi
    {
        get { return Mathf.Clamp(PlayerPrefs.GetFloat(UstPanelOlcegiKey, 1f), 0.75f, 1.5f); }
    }

    public static float UstPanelDikeyBosluk
    {
        get { return Mathf.Clamp(PlayerPrefs.GetFloat(UstPanelDikeyBoslukKey, 0f), 0f, 80f); }
    }

    public static void UstPanelAyarlariniUygula(float olcek, float dikeyBosluk)
    {
        PlayerPrefs.SetFloat(UstPanelOlcegiKey, Mathf.Clamp(olcek, 0.75f, 1.5f));
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
        if(GetComponent<IslandSimulationPause>()==null)gameObject.AddComponent<IslandSimulationPause>();
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

    // SOL ÜST: 1) İnsan Sayısı (Nüfus) -> 2) Para -> 3) Farkındalık (Yan yana, Krem arka planlı, beveled arcade kartlar)
    private void BuildTopLeftHud(Transform ustBar)
    {
        GameObject leftGroup = new GameObject("TopLeftHudGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        leftGroup.transform.SetParent(ustBar, false);
        RectTransform lgRt = leftGroup.GetComponent<RectTransform>();
        lgRt.anchorMin = new Vector2(0f, 1f);
        lgRt.anchorMax = new Vector2(0f, 1f);
        lgRt.pivot = new Vector2(0f, 1f);
        lgRt.anchoredPosition = new Vector2(20f, -16f);
        float leftGroupWidth = 215f + EkGenislik + 245f + EkGenislik + 270f + EkGenislik + 24f;
        lgRt.sizeDelta = new Vector2(leftGroupWidth, KartYuksekligi + 4f);
        leftHudGroupRt = lgRt;

        HorizontalLayoutGroup hlg = leftGroup.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 1. İNSAN SAYISI (NÜFUS) - Basit İnsan Figürü İkonu
        TMP_Text nufusSub;
        CreateHudPillCard(
            leftGroup.transform, "NufusPill", 215f + EkGenislik, KartYuksekligi,
            IconType.Person, new Color32(56, 180, 242, 255),
            "NÜFUS", nufusText, out nufusSub, false, out _);

        // 2. PARA (BÜTÇE) - Altıgen Sikke İkonu
        TMP_Text paraSub;
        CreateHudPillCard(
            leftGroup.transform, "ParaPill", 245f + EkGenislik, KartYuksekligi,
            IconType.HexCoin, new Color32(255, 184, 46, 255),
            "BÜTÇE", paraText, out paraSub, false, out _);

        // 3. FARKINDALIK - Basic Açık Kitap İkonu + İlerleme Çubuğu
        CreateHudPillCard(
            leftGroup.transform, "FarkindalikPill", 270f + EkGenislik, KartYuksekligi,
            IconType.Book, new Color32(63, 212, 127, 255),
            "FARKINDALIK", farkindalikText, out farkindalikSubLabelText, true, out awarenessFillBar);
    }

    // SAĞ ÜST: En sağ üst köşede Gün Sayısı ve Dinamik Afet Uyarısı, hemen altında Durdur (II), 1X, 2X, 3X ve Hızlı Ayarlar
    private void BuildTopRightHud(Transform ustBar)
    {
        GameObject rightCol = new GameObject("TopRightHudColumn", typeof(RectTransform));
        rightCol.transform.SetParent(ustBar, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(1f, 1f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(1f, 1f);
        rcRt.anchoredPosition = new Vector2(-20f, -16f);
        float rightHudHeight = SagGunKartiYuksekligi + SagHudKartiAraligi + SagUyariYuksekligi +
            SagUyariKontrolAraligi + SagKontrolYuksekligi;
        rcRt.sizeDelta = new Vector2(SagHudGenisligi + EkGenislik, rightHudHeight);
        rightHudColumnRt = rcRt;

        // Üstte Gün Kutusu (Krem arka planlı)
        RectTransform dayPill = CreateHudPillCard(
            rightCol.transform, "GunPill", SagHudGenisligi + EkGenislik, SagGunKartiYuksekligi,
            IconType.Calendar, new Color32(240, 77, 90, 255),
            "GÜN SAYACI", gunText, out gunSubLabelText, false, out _, SagGunIcerikOlcegi);
        dayPill.anchorMin = new Vector2(1f, 1f);
        dayPill.anchorMax = new Vector2(1f, 1f);
        dayPill.pivot = new Vector2(1f, 1f);
        dayPill.anchoredPosition = Vector2.zero;

        // Afet uyarısı kendi satırını kullanır; gün kartının metnini ve hız kontrollerini örtmez.
        GameObject alertObj = new GameObject("DisasterAlertBadge", typeof(RectTransform), typeof(Image));
        alertObj.transform.SetParent(rightCol.transform, false);
        disasterAlertBadgeRt = alertObj.GetComponent<RectTransform>();
        disasterAlertBadgeRt.anchorMin = new Vector2(1f, 1f);
        disasterAlertBadgeRt.anchorMax = new Vector2(1f, 1f);
        disasterAlertBadgeRt.pivot = new Vector2(1f, 1f);
        disasterAlertBadgeRt.anchoredPosition = new Vector2(0f, -(SagGunKartiYuksekligi + SagHudKartiAraligi));
        disasterAlertBadgeRt.sizeDelta = new Vector2(SagHudGenisligi + EkGenislik, SagUyariYuksekligi);

        disasterAlertBadgeImg = alertObj.GetComponent<Image>();
        disasterAlertBadgeImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.CyanBtn, CivicUI.CyanBtnDark);
        disasterAlertBadgeImg.type = Image.Type.Sliced;
        disasterAlertBadgeImg.color = Color.white;
        disasterAlertBadgeImg.raycastTarget = false;

        disasterAlertBadgeText = CreateText(
            alertObj.transform, "AlertText", sharedFont, "AFETE HAZIRLIK",
            15f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        disasterAlertBadgeText.enableAutoSizing = true;
        disasterAlertBadgeText.fontSizeMin = 12f;
        disasterAlertBadgeText.fontSizeMax = 15f;
        disasterAlertBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
        disasterAlertBadgeText.overflowMode = TextOverflowModes.Ellipsis;
        CivicUI.Fill(disasterAlertBadgeText.rectTransform, 4f);

        // Altında Zaman Akışı Butonları + Hızlı Ayarlar Dişli Butonu
        GameObject speedRow = new GameObject("SpeedControlsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        speedRow.transform.SetParent(rightCol.transform, false);
        RectTransform srRt = speedRow.GetComponent<RectTransform>();
        srRt.anchorMin = new Vector2(1f, 1f);
        srRt.anchorMax = new Vector2(1f, 1f);
        srRt.pivot = new Vector2(1f, 1f);
        srRt.anchoredPosition = new Vector2(0f, -(SagGunKartiYuksekligi + SagHudKartiAraligi +
            SagUyariYuksekligi + SagUyariKontrolAraligi));
        srRt.sizeDelta = new Vector2(SagHudGenisligi + EkGenislik, SagKontrolYuksekligi);

        HorizontalLayoutGroup sHlg = speedRow.GetComponent<HorizontalLayoutGroup>();
        sHlg.spacing = 6f;
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
            btnImg.sprite = CivicUI.GetSquareButtonSprite(CreamBg, CreamShadow);
            btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => SetTimeSpeed(mode));

            TMP_Text txt = CreateText(btnObj.transform, "Text", sharedFont, labels[i], 19f, FontStyles.Bold, DarkInk, TextAlignmentOptions.Center);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(0f, 3f);
            txt.rectTransform.offsetMax = Vector2.zero;

            speedButtons[i] = btn;
            speedButtonImages[i] = btnImg;
            speedButtonLabels[i] = txt;
        }

        // Hızlı Ayarlar / Duraklat Butonu (Screenshot 2 floating square button)
        GameObject settingsBtnObj = new GameObject("QuickSettingsBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        settingsBtnObj.transform.SetParent(speedRow.transform, false);
        Image sImg = settingsBtnObj.GetComponent<Image>();
        sImg.sprite = CivicUI.GetSquareButtonSprite(CivicUI.CyanBtn, CivicUI.CyanBtnDark);
        sImg.type = Image.Type.Sliced;

        Button sBtn = settingsBtnObj.GetComponent<Button>();
        sBtn.targetGraphic = sImg;
        sBtn.onClick.AddListener(() =>
        {
            var pause = FindAnyObjectByType<PauseMenu>();
            if (pause != null)
            {
                if (PauseMenu.DurdurulduMu) pause.Devam();
                else pause.Durdur();
            }
            else
            {
                var settings = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
                if (settings != null)
                {
                    if (settings.AcikMi) settings.Kapat();
                    else settings.Ac();
                }
            }
        });

        GameObject sIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        sIcon.transform.SetParent(settingsBtnObj.transform, false);
        RectTransform sIconRt = sIcon.GetComponent<RectTransform>();
        CivicUI.Fill(sIconRt, 7f);
        sIconRt.anchoredPosition = new Vector2(0f, 2f);
        Image sIconImg = sIcon.GetComponent<Image>();
        sIconImg.sprite = CivicUI.GetColoredIconSprite(CivicUI.IconType.Gear, Color.white);
        sIconImg.preserveAspect = true;
        sIconImg.raycastTarget = false;

        // Eğer Ana Canvas üzerinde HizKontrol varsa bağlayalım
        HizKontrol hizKontrol = GetComponentInParent<HizKontrol>();
        if (hizKontrol == null) hizKontrol = FindAnyObjectByType<HizKontrol>();
        if (hizKontrol != null)
        {
            Transform oldHizPanel = transform.Find("HizPaneli");
            if (oldHizPanel != null) oldHizPanel.gameObject.SetActive(false);

            hizKontrol.duraklatButonu = speedButtons[0];
            hizKontrol.hizButonlari = new[] { speedButtons[1], speedButtons[2], speedButtons[3] };
        }
    }

    /// <summary>Called by HizKontrol before EventSystem processes Space as a UI Submit key.</summary>
    public bool TryHandleSpacePauseInput()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || SpacePauseIsBlocked(gm)) return false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SetTimeSpeed(0);
        return true;
    }

    /// <summary>Sets simulation speed from the 1, 2 and 3 keyboard shortcuts.</summary>
    public bool TryHandleSpeedHotkeyInput(int mode)
    {
        GameManager gm = GameManager.Instance;
        if (mode < 1 || mode > 3 || gm == null || IslandTsunamiFinale.BlocksInterface || SpacePauseIsBlocked(gm)) return false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SetTimeSpeed(mode);
        return true;
    }

    private static bool SpacePauseIsBlocked(GameManager gm)
    {
        if (gm.oyunDurduMu || gm.oyunBittiMi || gm.afetOlduMu || PauseMenu.DurdurulduMu)
            return true;

        // A report is shown when the hazard flag is still set, so inspect its visible panel
        // separately while allowing Space to pause/resume during the hazard itself.
        var settings = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settings != null && settings.AcikMi) return true;

        var report = FindAnyObjectByType<AfetRaporu>(FindObjectsInactive.Include);
        if (report != null && report.panel != null && report.panel.activeInHierarchy) return true;

        var gameOver = FindAnyObjectByType<OyunSonu>(FindObjectsInactive.Include);
        if (gameOver != null && gameOver.panel != null && gameOver.panel.activeInHierarchy) return true;

        if (FindAnyObjectByType<MainMenu>() != null) return true;

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null) return false;
        return selected.GetComponent<InputField>() != null || selected.GetComponent<TMP_InputField>() != null ||
               selected.GetComponentInParent<InputField>() != null || selected.GetComponentInParent<TMP_InputField>() != null;
    }

    public void SetTimeSpeed(int mode)
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (mode == 0)
            {
                gm.kullaniciDurdurdu = !gm.kullaniciDurdurdu;
                GetComponent<IslandSimulationPause>()?.ApplyPause();
            }
            else
            {
                gm.hizCarpani = Mathf.Clamp(mode, 1, 3);
                gm.kullaniciDurdurdu = false;
                GetComponent<IslandSimulationPause>()?.ApplyPause();
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
                speedButtonImages[i].sprite = CivicUI.GetSquareButtonSprite(GoldBtn, GoldBtnDark);
                speedButtonImages[i].color = Color.white;
            }
            else
            {
                speedButtonImages[i].sprite = CivicUI.GetSquareButtonSprite(CreamBg, CreamShadow);
                speedButtonImages[i].color = Color.white;
            }
            if (speedButtonLabels[i] != null)
                speedButtonLabels[i].color = DarkInk;
        }
    }

    private RectTransform CreateHudPillCard(
        Transform parent, string name, float width, float height,
        IconType iconType, Color32 iconColor, string subLabel,
        TMP_Text existingValueText, out TMP_Text outSubLabel, bool withProgressBar, out Image progressFill,
        float contentScale = 1f)
    {
        progressFill = null;
        float contentLeft = YaziSolBosluk * contentScale;
        float iconSlotSize = IkonYuvasi * contentScale;
        float iconSize = IkonBoyu * contentScale;
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = card.GetComponent<Image>();
        bg.sprite = CivicUI.GetBeveledCardSprite(CreamBg, CreamShadow);
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;

        // Sol İkon Yuvası (Koyu Yuvarlatılmış Çerçeveli Slot)
        GameObject iconSlot = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        iconSlot.transform.SetParent(card.transform, false);
        RectTransform isRt = iconSlot.GetComponent<RectTransform>();
        isRt.anchorMin = new Vector2(0f, 0.5f);
        isRt.anchorMax = new Vector2(0f, 0.5f);
        isRt.pivot = new Vector2(0f, 0.5f);
        isRt.anchoredPosition = new Vector2(10f * contentScale, 1f * contentScale);
        isRt.sizeDelta = new Vector2(iconSlotSize, iconSlotSize);

        Image slotImg = iconSlot.GetComponent<Image>();
        slotImg.sprite = CivicUI.GetDarkSlotSprite();
        slotImg.type = Image.Type.Sliced;
        slotImg.color = Color.white;

        GameObject iconGraphic = new GameObject("IconGraphic", typeof(RectTransform), typeof(Image));
        iconGraphic.transform.SetParent(iconSlot.transform, false);
        RectTransform igRt = iconGraphic.GetComponent<RectTransform>();
        igRt.anchorMin = new Vector2(0.5f, 0.5f);
        igRt.anchorMax = new Vector2(0.5f, 0.5f);
        igRt.sizeDelta = new Vector2(iconSize, iconSize);
        igRt.anchoredPosition = Vector2.zero;

        Image igImg = iconGraphic.GetComponent<Image>();
        igImg.sprite = CivicUI.GetColoredIconSprite((CivicUI.IconType)iconType, iconColor);
        igImg.preserveAspect = true;
        igImg.raycastTarget = false;

        // Küçük Üst Etiket
        outSubLabel = CreateText(card.transform, "SubLabel", sharedFont, subLabel,
            11f * contentScale, FontStyles.Bold, MutedText, TextAlignmentOptions.Left);
        RectTransform slRt = outSubLabel.rectTransform;
        slRt.anchorMin = new Vector2(0f, 1f);
        slRt.anchorMax = new Vector2(1f, 1f);
        slRt.pivot = new Vector2(0f, 1f);
        slRt.anchoredPosition = new Vector2(contentLeft, -10f * contentScale);
        slRt.sizeDelta = new Vector2(-(contentLeft + 8f * contentScale), 15f * contentScale);

        // Ana Değer Metni
        if (existingValueText != null)
        {
            existingValueText.transform.SetParent(card.transform, false);
            RectTransform vtRt = existingValueText.rectTransform;
            vtRt.anchorMin = new Vector2(0f, 0f);
            vtRt.anchorMax = new Vector2(1f, 1f);
            vtRt.offsetMin = new Vector2(contentLeft, (withProgressBar ? 18f : 8f) * contentScale);
            // The expanded day card reserves a distinct title band above its value.
            float valueTopPadding = !withProgressBar && contentScale > 1.05f
                ? 30f * contentScale
                : 14f * contentScale;
            vtRt.offsetMax = new Vector2(-10f * contentScale, -valueTopPadding);
            existingValueText.fontSize = (withProgressBar ? 19f : 20f) * contentScale;
            existingValueText.enableAutoSizing = true;
            existingValueText.fontSizeMin = 14f * contentScale;
            existingValueText.fontSizeMax = (withProgressBar ? 19f : 20f) * contentScale;
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
            bbRt.anchoredPosition = new Vector2(contentLeft, 10f * contentScale);
            bbRt.sizeDelta = new Vector2(-(contentLeft + 12f * contentScale), 8f * contentScale);

            Image bbImg = barBg.GetComponent<Image>();
            bbImg.color = DarkSlotBg;

            GameObject barFill = new GameObject("AwarenessBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform bfRt = barFill.GetComponent<RectTransform>();
            bfRt.anchorMin = new Vector2(0f, 0f);
            bfRt.anchorMax = new Vector2(1f, 1f);
            bfRt.offsetMin = new Vector2(1f, 1f);
            bfRt.offsetMax = new Vector2(-1f, -1f);

            progressFill = barFill.GetComponent<Image>();
            progressFill.sprite = GetSolidWhiteSprite();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.color = GreenBtn;
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
        bg.sprite = CivicUI.GetPillBadgeSprite(CreamBg, CreamShadow);
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
            gunSubLabelText.text = "GÜN SAYACI";
        }
        if (tavan != sonTavan && farkindalikSubLabelText != null)
            farkindalikSubLabelText.text = "FARKINDALIK (MAKS %" + tavan + ")";
        if (gm.gun != sonGun && gunText != null)
            gunText.text = "Gün: " + gm.gun + "/" + d.toplamGun;

        if (disasterAlertBadgeText != null && disasterAlertBadgeImg != null && (kalan != sonKalan || gm.gun != sonGun))
        {
            if (kalan > 3)
            {
                disasterAlertBadgeImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.CyanBtn, CivicUI.CyanBtnDark);
                disasterAlertBadgeText.text = "AFETE " + kalan + " GÜN";
                disasterAlertBadgeText.color = Color.white;
            }
            else if (kalan >= 1)
            {
                disasterAlertBadgeImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.CoralBtn, CivicUI.CoralBtnDark);
                disasterAlertBadgeText.text = "DİKKAT: " + kalan + " GÜN KALDI!";
                disasterAlertBadgeText.color = Color.white;
            }
            else if (kalan == 0)
            {
                disasterAlertBadgeImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.RibbonRed, CivicUI.RibbonRedLip);
                disasterAlertBadgeText.text = "AFET GÜNÜ!";
                disasterAlertBadgeText.color = Color.white;
            }
            else
            {
                disasterAlertBadgeImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.GreenBtn, CivicUI.GreenBtnDark);
                disasterAlertBadgeText.text = "ŞEHİR GÜVENDE";
                disasterAlertBadgeText.color = Color.white;
            }
        }
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
        // The former 125% setting is the new 100% baseline. A versioned key above
        // prevents legacy saved values from applying that multiplier twice.
        float userScale = UstPanelOlcegi * UstPanelYeniStandartCarpani;
        float leftScale = Mathf.Min(SolHudTasarimOlcegi,
            availableWidth / (leftHudGroupRt.sizeDelta.x * userScale)) * userScale;
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
        if (gm.oyunDurduMu || gm.afetOlduMu || gm.oyunBittiMi || IslandTsunamiFinale.BlocksInterface)
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
            case BinaTuru.Okul:      return "ODTÜ";
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
        return CivicUI.GetSolidWhiteSprite();
    }

    public static Sprite GetCreamBoxSprite(Color32 fillColor, Color32 bottomShade)
    {
        return CivicUI.GetBeveledCardSprite(fillColor, bottomShade);
    }

    public static Sprite GetSteppedModalSprite()
    {
        return CivicUI.GetBeveledCardSprite(ModalCyanBg, ModalCyanShadow);
    }

    public static Sprite GetDarkSlotSprite()
    {
        return CivicUI.GetDarkSlotSprite();
    }

    public static Sprite GetRibbonMainSprite(Color32 fill, Color32 bottomLip)
    {
        return CivicUI.GetRibbonMainSprite(fill, bottomLip);
    }

    public static Sprite GetRibbonTailSprite(bool leftSide)
    {
        return CivicUI.GetRibbonTailSprite(leftSide, RibbonBlue, RibbonBlueDark, RibbonBlueDark);
    }

    public static RectTransform CreateRibbonBanner(
        RectTransform modalCard, TMP_FontAsset font, string title,
        Color32 mainColor, Color32 lipColor, Color32 tailColor,
        out Image mainImg, out Image leftTailImg, out Image rightTailImg, out TMP_Text titleText)
    {
        RectTransform rt = CivicUI.CreateRibbon(modalCard, title, mainColor, lipColor, tailColor, font);
        mainImg = rt.GetComponent<Image>();
        leftTailImg = rt.Find("TailLeft").GetComponent<Image>();
        rightTailImg = rt.Find("TailRight").GetComponent<Image>();
        titleText = rt.Find("RibbonTitle").GetComponent<TMP_Text>();
        return rt;
    }

    public static Sprite GetTwoToneButtonSprite(Color32 mainFill, Color32 leftIconFill, Color32 shadowColor)
    {
        return CivicUI.GetTwoToneButtonSprite(mainFill, leftIconFill, shadowColor);
    }

    public static Button CreateTwoToneArcadeButton(
        Transform parent, string name, TMP_FontAsset font, string labelText,
        Color32 mainFill, Color32 leftFill, Color32 shadowCol,
        Color32 textColor, IconType iconType, out TMP_Text outLabel)
    {
        Button btn = CivicUI.CreateTwoToneButton(
            parent, name, labelText, mainFill, leftFill, shadowCol,
            (CivicUI.IconType)iconType, null, font);
        outLabel = btn.GetComponentInChildren<TMP_Text>(true);
        if (outLabel != null) outLabel.color = textColor;
        return btn;
    }

    public static Sprite GetColoredIconSprite(IconType type, Color32 mainColor)
    {
        return CivicUI.GetColoredIconSprite((CivicUI.IconType)type, mainColor);
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
