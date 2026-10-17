using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

// Sahnede tek bir tane vardır. Herhangi bir binaya tıklanınca BinaPopup.Instance.Ac(binaTuru) çağrılır.
// Zaman (oyunDurduMu) Time.timeScale ile değil, kendi bayrağımızla durur; böylece animasyonlar çalışmaya devam eder.
public class BinaPopup : MonoBehaviour
{
    public static BinaPopup Instance;

    [Header("Panel ve Yazılar")]
    public GameObject panel;
    public TMP_Text baslikText;
    [FormerlySerializedAs("levelText")] public TMP_Text seviyeText; // eski adı levelText; prefab bağlantısı korunur
    public TMP_Text aciklamaText;

    [Header("Butonlar")]
    public Button yukseltButonu;
    public TMP_Text yukseltButonText;
    public Button quizButonu;      // Sadece Okul seçiliyken görünür
    public Button kapatButonu;

    [Header("Pencere ölçeği")]
    [Min(1f)] public float nominalPopupScale = 2f;

    private BinaTuru aktifBina;
    private Image[] starFillImages = new Image[3];
    private bool styleApplied;
    private RectTransform popupCardRt;
    private Rect popupLastSafeArea;
    private int popupLastScreenWidth = -1;
    private int popupLastScreenHeight = -1;

    // Popup bir binaya tıklanınca açılıyor; aynı tıklama alttaki butona (ör. X) denk gelmesin diye kısa koruma
    private float acilisZamani;
    private const float TiklamaKorumasi = 0.2f;
    private bool YeniAcildi { get { return Time.unscaledTime - acilisZamani < TiklamaKorumasi; } }

    private void Awake()
    {
        Instance = this;
        if (kapatButonu == null && panel != null)
        {
            Transform x = FindChild(panel.transform, "KapatButonu");
            if (x != null) kapatButonu = x.GetComponent<Button>();
        }
        ApplyPrototypePopupStyle();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf)
            FitPopupToSafeArea(false);
    }

    private void Start()
    {
        // Prefab'da Inspector'dan bağlı olan butonlara kodla ikinci kez bağlanırsa bir tık iki kez çalışır (çift yükseltme)
        BagliDegilseBagla(yukseltButonu, YukseltTiklandi);
        BagliDegilseBagla(quizButonu, QuizTiklandi);
        BagliDegilseBagla(kapatButonu, Kapat);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static void BagliDegilseBagla(Button btn, UnityEngine.Events.UnityAction fonksiyon)
    {
        if (btn != null && btn.onClick.GetPersistentEventCount() == 0) btn.onClick.AddListener(fonksiyon);
    }

    private static Transform FindChild(Transform kok, string ad)
    {
        foreach (Transform t in kok.GetComponentsInChildren<Transform>(true))
            if (t.name == ad) return t;
        return null;
    }

    private void ApplyPrototypePopupStyle()
    {
        if (styleApplied || panel == null) return;
        styleApplied = true;

        TMP_FontAsset font = baslikText != null ? baslikText.font : null;

        // Arka plan karartma örtüsü
        Image overlayImg = panel.GetComponent<Image>();
        if (overlayImg != null)
        {
            overlayImg.sprite = null;
            overlayImg.color = new Color32(10, 17, 28, 155);
        }
        UIManager.StretchFull(panel.GetComponent<RectTransform>());

        Transform pencereTr = panel.transform.Find("Pencere");
        if (pencereTr == null) pencereTr = PencereOlustur();

        // Eski otomatik layout ve eski çocukları temizleyip referans görseldeki kurdeleli + yıldızlı piksel-art modal kartı kuralım
        VerticalLayoutGroup oldVlg = pencereTr.GetComponent<VerticalLayoutGroup>();
        if (oldVlg != null) Destroy(oldVlg);

        RectTransform cardRt = pencereTr.GetComponent<RectTransform>();
        popupCardRt = cardRt;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(540f, 430f);
        cardRt.localScale = Vector3.one * nominalPopupScale;

        Image cardImg = pencereTr.GetComponent<Image>();
        if (cardImg != null)
        {
            cardImg.sprite = UIManager.GetSteppedModalSprite();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
        }

        // Mevcut referans objelerini kendi tasarımımıza dönüştür
        for (int i = pencereTr.childCount - 1; i >= 0; i--)
        {
            Transform ch = pencereTr.GetChild(i);
            if (ch.name == "AltButonlar") continue;
            ch.SetParent(pencereTr, false);
        }

        // 1) Üst Mavi Kurdele (Ribbon Banner)
        Image rMain, rLeft, rRight;
        TMP_Text ribbonTmp;
        RectTransform ribbonRt = UIManager.CreateRibbonBanner(
            cardRt, font, "BİNA BİLGİSİ",
            UIManager.RibbonBlue, UIManager.RibbonBlueDark, UIManager.RibbonBlueDark,
            out rMain, out rLeft, out rRight, out ribbonTmp);

        if (baslikText != null)
        {
            Destroy( ribbonTmp.gameObject );
            baslikText.transform.SetParent(ribbonRt, false);
            baslikText.fontSize = 26f;
            baslikText.fontStyle = FontStyles.Bold;
            baslikText.color = Color.white;
            baslikText.alignment = TextAlignmentOptions.Center;
            RectTransform btRt = baslikText.rectTransform;
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = new Vector2(16f, 6f);
            btRt.offsetMax = new Vector2(-16f, 0f);
        }

        // Sağ üst Kapat (X) butonu
        if (kapatButonu != null)
        {
            kapatButonu.transform.SetParent(cardRt, false);
            LayoutElement le = kapatButonu.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);

            RectTransform closeRt = kapatButonu.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-10f, -14f);
            closeRt.sizeDelta = new Vector2(44f, 44f);

            Image closeImg = kapatButonu.GetComponent<Image>();
            closeImg.sprite = UIManager.GetCreamBoxSprite(UIManager.RedClose, UIManager.RedCloseDark);
            closeImg.type = Image.Type.Sliced;
            closeImg.color = Color.white;

            TMP_Text closeTxt = kapatButonu.GetComponentInChildren<TMP_Text>();
            if (closeTxt != null)
            {
                closeTxt.text = "X";
                closeTxt.fontSize = 22f;
                closeTxt.fontStyle = FontStyles.Bold;
                closeTxt.color = Color.white;
                closeTxt.alignment = TextAlignmentOptions.Center;
                closeTxt.rectTransform.offsetMin = new Vector2(0f, 4f);
                closeTxt.rectTransform.offsetMax = Vector2.zero;
            }
        }

        // 2) 3'lü Yıldız Seviye Yuvası (Image 1 & 2'deki gibi)
        GameObject starsRow = new GameObject("StarsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        starsRow.transform.SetParent(cardRt, false);
        RectTransform srRt = starsRow.GetComponent<RectTransform>();
        srRt.anchorMin = new Vector2(0.5f, 1f);
        srRt.anchorMax = new Vector2(0.5f, 1f);
        srRt.pivot = new Vector2(0.5f, 1f);
        srRt.anchoredPosition = new Vector2(0f, -96f);
        srRt.sizeDelta = new Vector2(210f, 52f);

        HorizontalLayoutGroup sHlg = starsRow.GetComponent<HorizontalLayoutGroup>();
        sHlg.spacing = 14f;
        sHlg.childAlignment = TextAnchor.MiddleCenter;
        sHlg.childControlWidth = false;
        sHlg.childControlHeight = false;

        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject("StarSlot_" + (i + 1), typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(starsRow.transform, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(52f, 50f);
            Image slotBg = slot.GetComponent<Image>();
            slotBg.sprite = UIManager.GetDarkSlotSprite();
            slotBg.type = Image.Type.Sliced;

            GameObject starIcon = new GameObject("StarFill", typeof(RectTransform), typeof(Image));
            starIcon.transform.SetParent(slot.transform, false);
            RectTransform stRt = starIcon.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0.5f);
            stRt.anchorMax = new Vector2(0.5f, 0.5f);
            stRt.sizeDelta = new Vector2(36f, 36f);
            stRt.anchoredPosition = new Vector2(0f, 1f);

            Image stImg = starIcon.GetComponent<Image>();
            stImg.sprite = UIManager.GetColoredIconSprite(UIManager.IconType.Star, UIManager.GoldStar);
            starFillImages[i] = stImg;
        }

        // Seviye Etiketi
        if (seviyeText != null)
        {
            LayoutElement le = seviyeText.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);
            seviyeText.transform.SetParent(cardRt, false);
            RectTransform svRt = seviyeText.rectTransform;
            svRt.anchorMin = new Vector2(0f, 1f);
            svRt.anchorMax = new Vector2(1f, 1f);
            svRt.pivot = new Vector2(0.5f, 1f);
            svRt.anchoredPosition = new Vector2(0f, -152f);
            svRt.sizeDelta = new Vector2(-60f, 28f);
            seviyeText.fontSize = 18f;
            seviyeText.fontStyle = FontStyles.Bold;
            seviyeText.color = UIManager.MutedText;
            seviyeText.alignment = TextAlignmentOptions.Center;
        }

        // 3) Koyu Çerçeveli Açıklama Kutusu
        GameObject descBox = new GameObject("DescSlotBox", typeof(RectTransform), typeof(Image));
        descBox.transform.SetParent(cardRt, false);
        RectTransform dbRt = descBox.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(32f, 108f);
        dbRt.offsetMax = new Vector2(-32f, -184f);

        Image dbImg = descBox.GetComponent<Image>();
        dbImg.sprite = UIManager.GetDarkSlotSprite();
        dbImg.type = Image.Type.Sliced;

        if (aciklamaText != null)
        {
            LayoutElement le = aciklamaText.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);
            aciklamaText.transform.SetParent(descBox.transform, false);
            RectTransform acRt = aciklamaText.rectTransform;
            acRt.anchorMin = Vector2.zero;
            acRt.anchorMax = Vector2.one;
            acRt.offsetMin = new Vector2(18f, 14f);
            acRt.offsetMax = new Vector2(-18f, -14f);
            aciklamaText.fontSize = 20f;
            aciklamaText.fontStyle = FontStyles.Bold;
            aciklamaText.color = Color.white;
            aciklamaText.alignment = TextAlignmentOptions.Center;
        }

        // 4) Alt İki Tonlu (Two-Tone) Butonlar: YÜKSELT ve (Sadece Okulda) QUİZ ÇÖZ
        Transform altButonlar = pencereTr.Find("AltButonlar");
        if (altButonlar != null)
        {
            LayoutElement le = altButonlar.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);
            RectTransform abRt = altButonlar.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0f, 0f);
            abRt.anchorMax = new Vector2(1f, 0f);
            abRt.pivot = new Vector2(0.5f, 0f);
            abRt.anchoredPosition = new Vector2(0f, 26f);
            abRt.sizeDelta = new Vector2(-64f, 62f);

            HorizontalLayoutGroup hlg = altButonlar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = 14f;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
            }
        }

        StyleExistingButtonAsTwoTone(yukseltButonu, yukseltButonText, UIManager.GreenBtn, UIManager.GreenBtnLeft, UIManager.GreenBtnDark, UIManager.IconType.UpArrow);
        if (kapatButonu != null) kapatButonu.transform.SetAsLastSibling(); // kurdelenin ve diğer kutuların önünde dursun
        TMP_Text quizBtnText = quizButonu != null ? quizButonu.GetComponentInChildren<TMP_Text>() : null;
        if (quizBtnText != null) quizBtnText.text = "QUİZ ÇÖZ";
        StyleExistingButtonAsTwoTone(quizButonu, quizBtnText, UIManager.GoldBtn, UIManager.GoldBtnLeft, UIManager.GoldBtnDark, UIManager.IconType.Book);
    }

    // Prefab'da kart (Pencere) yoksa oluştur: panel ekranı kaplayan karartma olur, eski çocuklar kartın içine taşınır
    private Transform PencereOlustur()
    {
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        UIManager.StretchFull(panelRt);

        var eskiCocuklar = new System.Collections.Generic.List<Transform>();
        foreach (Transform ch in panel.transform) eskiCocuklar.Add(ch);

        GameObject pencere = new GameObject("Pencere", typeof(RectTransform), typeof(Image));
        pencere.transform.SetParent(panel.transform, false);
        foreach (Transform ch in eskiCocuklar) ch.SetParent(pencere.transform, false);

        GameObject alt = new GameObject("AltButonlar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        alt.transform.SetParent(pencere.transform, false);
        if (yukseltButonu != null) yukseltButonu.transform.SetParent(alt.transform, false);
        if (quizButonu != null) quizButonu.transform.SetParent(alt.transform, false);

        return pencere.transform;
    }

    private void StyleExistingButtonAsTwoTone(
        Button btn, TMP_Text label,
        Color32 mainFill, Color32 leftFill, Color32 shadowCol,
        UIManager.IconType iconType)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = UIManager.GetTwoToneButtonSprite(mainFill, leftFill, shadowCol);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        Transform oldIcon = btn.transform.Find("LeftIcon");
        if (oldIcon != null) Destroy(oldIcon.gameObject);

        GameObject iconObj = new GameObject("LeftIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btn.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(22f, 2f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = UIManager.GetColoredIconSprite(iconType, Color.white);
        iconImg.raycastTarget = false;

        if (label != null)
        {
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.color = UIManager.DarkInk;
            label.alignment = TextAlignmentOptions.Center;
            RectTransform lRt = label.rectTransform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(72f, 5f);
            lRt.offsetMax = new Vector2(-8f, 0f);
        }
    }

    public void Ac(BinaTuru bina)
    {
        // Afet ya da oyun sonu ekranı açıkken bina popup'ı açılmasın
        var gm = GameManager.Instance;
        if (gm != null && (gm.afetOlduMu || gm.oyunBittiMi)) return;

        aktifBina = bina;
        acilisZamani = Time.unscaledTime;
        if (gm != null) gm.oyunDurduMu = true;

        Guncelle();
        panel.SetActive(true);
        FitPopupToSafeArea(true);
    }

    private void FitPopupToSafeArea(bool force)
    {
        if (popupCardRt == null) return;
        Rect safeArea = Screen.safeArea;
        if (!force && popupLastScreenWidth == Screen.width && popupLastScreenHeight == Screen.height &&
            popupLastSafeArea == safeArea) return;

        RectTransform parentRect = popupCardRt.parent as RectTransform;
        if (parentRect == null) return;

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera
            : null;
        Vector2 safeMin;
        Vector2 safeMax;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, safeArea.min, uiCamera, out safeMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, safeArea.max, uiCamera, out safeMax))
        {
            safeMin = parentRect.rect.min;
            safeMax = parentRect.rect.max;
        }

        float availableWidth = Mathf.Max(0f, safeMax.x - safeMin.x);
        float availableHeight = Mathf.Max(0f, safeMax.y - safeMin.y);
        float fitScale = Mathf.Min(nominalPopupScale,
            Mathf.Min(availableWidth / popupCardRt.sizeDelta.x, availableHeight / popupCardRt.sizeDelta.y));
        fitScale = Mathf.Max(0.01f, fitScale);
        popupCardRt.localScale = Vector3.one * fitScale;
        popupCardRt.anchoredPosition = (safeMin + safeMax) * 0.5f;

        popupLastScreenWidth = Screen.width;
        popupLastScreenHeight = Screen.height;
        popupLastSafeArea = safeArea;
    }

    public void Kapat()
    {
        if (YeniAcildi) return;
        PaneliKapat();
    }

    private void PaneliKapat()
    {
        panel.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.oyunDurduMu = false;
    }

    private void Guncelle()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int level = gm.Level(aktifBina);
        bool hasarli = gm.Hasarli(aktifBina);
        int maxLevel = DengeAyarlari.Aktif.maxLevel;

        if (seviyeText != null) seviyeText.text = hasarli
            ? "Seviye " + level + "  (HASARLI)"
            : "Seviye " + level + " / " + maxLevel;
        BinaBilgileriniDoldur(aktifBina, level, hasarli);

        // Yıldızları güncelle
        for (int i = 0; i < starFillImages.Length; i++)
        {
            if (starFillImages[i] != null)
            {
                starFillImages[i].color = (i < level) ? Color.white : new Color(1f, 1f, 1f, 0.14f);
            }
        }

        if (hasarli)
        {
            int maliyet = Ekonomi.OnarimMaliyeti(aktifBina);
            yukseltButonText.text = "Onar (" + maliyet + " TL)";
            yukseltButonu.interactable = Ekonomi.OnarilabilirMi(aktifBina);
        }
        else if (level >= maxLevel)
        {
            yukseltButonText.text = "MAKS SEVİYE";
            yukseltButonu.interactable = false;
        }
        else
        {
            int maliyet = Ekonomi.YukseltmeMaliyeti(aktifBina);
            yukseltButonText.text = "Yükselt (" + maliyet + " TL)";
            yukseltButonu.interactable = Ekonomi.YukseltilebilirMi(aktifBina);
        }

        // Quiz Çöz butonu SADECE Okul popup'ında (ve okul hasarsızken) aktif olsun; diğer binalarda tamamen gizlensin
        bool okulMu = (aktifBina == BinaTuru.Okul) && !hasarli;
        if (quizButonu != null)
        {
            quizButonu.gameObject.SetActive(okulMu);
        }

        // Eğer Quiz butonu gizliyse Yükselt butonu tek başına ortada şık dursun
        Transform altButonlar = yukseltButonu != null ? yukseltButonu.transform.parent : null;
        if (altButonlar != null)
        {
            RectTransform abRt = altButonlar.GetComponent<RectTransform>();
            if (abRt != null)
            {
                abRt.sizeDelta = okulMu ? new Vector2(-64f, 62f) : new Vector2(-180f, 62f);
            }
        }
    }

    private void BinaBilgileriniDoldur(BinaTuru bina, int level, bool hasarli)
    {
        var d = DengeAyarlari.Aktif;
        string uyari = hasarli ? "\n\nHASARLI: Onarılana kadar görevini yapamaz!" : "";

        switch (bina)
        {
            case BinaTuru.TemelBina:
                baslikText.text = "KONUTLAR";
                aciklamaText.text = "Halkın yaşadığı evler. Seviye arttıkça nüfus kapasitesi büyür ve depreme dayanıklılık artar.\n"
                                  + "Kapasite: " + d.KonutKapasitesi(level) + " kişi" + uyari;
                break;

            case BinaTuru.Okul:
                baslikText.text = "OKUL";
                aciklamaText.text = "Halkı afetlere karşı eğitir. Quiz çözerek farkındalığı artırabilirsin.\n"
                                  + "Bu seviyede en yüksek farkındalık: %" + Mathf.RoundToInt(d.FarkindalikTavani(level)) + uyari;
                break;

            case BinaTuru.Akut:
                baslikText.text = "AKUT MERKEZİ";
                aciklamaText.text = "Arama-kurtarma ekibi. Her afette can kaybını azaltır.\n"
                                  + "Kurtarma etkisi: %" + Mathf.RoundToInt(level * d.akutEtkisi * 100f) + uyari;
                break;

            case BinaTuru.Itfaiye:
                baslikText.text = "İTFAİYE";
                aciklamaText.text = "Yangınlara müdahale eder. Yangın afetinde şehri korur (en az Seviye 2 gerekir)." + uyari;
                break;

            case BinaTuru.DenizSeti:
                baslikText.text = "DENİZ SETİ";
                aciklamaText.text = "Kıyıyı dev dalgalardan korur. Tsunamiye karşı tek savunmadır (Seviye 3 gerekir)." + uyari;
                break;

            case BinaTuru.Altyapi:
                baslikText.text = "ALTYAPI";
                aciklamaText.text = "Yollar ve yağmur suyu altyapısı. Toprak → çakıl → asfalt. Selden korunmak için en az Seviye 2 gerekir." + uyari;
                break;
        }
    }

    public void YukseltTiklandi()
    {
        var gm = GameManager.Instance;
        if (gm == null || YeniAcildi) return;

        bool degisti = gm.Hasarli(aktifBina) ? Ekonomi.Onar(aktifBina) : Ekonomi.Yukselt(aktifBina);
        if (degisti) Guncelle();
    }

    public void QuizTiklandi()
    {
        if (YeniAcildi) return;
        PaneliKapat();
        if (QuizManager.Instance != null)
            QuizManager.Instance.QuizAc();
    }
}
