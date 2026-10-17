using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [Header("Sorular (Assets/Resources/Questions)")]
    public QuestionSO[] sorular;

    [Header("UI Elemanları")]
    public GameObject quizPanel;
    public TMP_Text soruText;
    public Button[] sikButonlari;
    public TMP_Text[] sikTextleri;
    public Button kapatButonu;

    // Doğru bilinen sorular havuzdan çıkar; yanlış bilinenler tekrar sorulabilir
    private readonly List<QuestionSO> kalanSorular = new List<QuestionSO>();
    private QuestionSO aktifSoru;
    private bool cevapVerildi;
    private bool styleApplied;

    private GameObject feedbackBox;
    private Image feedbackBoxImg;
    private TMP_Text feedbackText;
    private Button devamBtn;
    private RectTransform quizCardRt;
    private Rect quizLastSafeArea;
    private int quizLastScreenWidth = -1;
    private int quizLastScreenHeight = -1;
    [Min(1f)] public float nominalQuizScale = 1.35f;

    private static readonly Vector2 QuizCardSize = new Vector2(1000f, 720f);

    public QuestionSO AktifSoru { get { return aktifSoru; } } // otomatik test için

    private static readonly string[] SikHarfleri = { "A", "B", "C", "D" };

    private void Awake()
    {
        Instance = this;

        // Inspector'da atanmadıysa Resources/Questions klasöründen otomatik yükle
        if (sorular == null || sorular.Length == 0)
            sorular = Resources.LoadAll<QuestionSO>("Questions");

        kalanSorular.Clear();
        if (sorular != null) kalanSorular.AddRange(sorular);

        // Prefab'da şık yazıları ya da kapat butonu bağlanmadıysa butonların içinden bul
        if (sikButonlari == null) sikButonlari = new Button[0];
        if (sikTextleri == null || sikTextleri.Length < sikButonlari.Length)
        {
            sikTextleri = new TMP_Text[sikButonlari.Length];
            for (int i = 0; i < sikButonlari.Length; i++)
                if (sikButonlari[i] != null) sikTextleri[i] = sikButonlari[i].GetComponentInChildren<TMP_Text>(true);
        }
        if (kapatButonu == null && quizPanel != null)
        {
            foreach (Button b in quizPanel.GetComponentsInChildren<Button>(true))
                if (b.name == "QuizKapatButonu" || b.name == "KapatButonu") { kapatButonu = b; break; }
        }

        ApplyPrototypeQuizStyle();
        quizPanel.SetActive(false);
    }

    private void Start()
    {
        for (int i = 0; i < sikButonlari.Length; i++)
        {
            if (sikButonlari[i] == null) continue;
            int index = i; // Closure için yerel kopya şart
            sikButonlari[i].onClick.AddListener(() => SikSecildi(index));
        }

        // Inspector'da zaten bağlıysa tekrar bağlama
        if (kapatButonu != null && kapatButonu.onClick.GetPersistentEventCount() == 0)
            kapatButonu.onClick.AddListener(QuizKapat);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (quizPanel != null && quizPanel.activeSelf)
            FitQuizToSafeArea(false);
    }

    private void ApplyPrototypeQuizStyle()
    {
        if (styleApplied || quizPanel == null) return;
        styleApplied = true;

        TMP_FontAsset font = soruText != null ? soruText.font : null;

        if (quizPanel.GetComponent<RectTransform>() != null)
            UIManager.StretchFull(quizPanel.GetComponent<RectTransform>());

        Image overlayImg = quizPanel.GetComponent<Image>();
        if (overlayImg != null)
        {
            overlayImg.sprite = null;
            overlayImg.color = new Color32(10, 17, 28, 165);
        }

        Transform pencereTr = quizPanel.transform.Find("Pencere");
        if (pencereTr == null)
        {
            // Prefab'da kart (Pencere) yoksa oluştur ve eski çocukları içine taşı
            UIManager.StretchFull(quizPanel.GetComponent<RectTransform>());
            var eskiCocuklar = new List<Transform>();
            foreach (Transform ch in quizPanel.transform) eskiCocuklar.Add(ch);
            GameObject pencere = new GameObject("Pencere", typeof(RectTransform), typeof(Image));
            pencere.transform.SetParent(quizPanel.transform, false);
            foreach (Transform ch in eskiCocuklar) ch.SetParent(pencere.transform, false);
            pencereTr = pencere.transform;
        }

        VerticalLayoutGroup oldVlg = pencereTr.GetComponent<VerticalLayoutGroup>();
        if (oldVlg != null) Destroy(oldVlg);

        // Quiz kartı geniş tutulur; ekranda kalan gerçek güvenli alana göre aşağıda ölçeklenir.
        RectTransform cardRt = pencereTr.GetComponent<RectTransform>();
        quizCardRt = cardRt;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = QuizCardSize;

        Image cardImg = pencereTr.GetComponent<Image>();
        if (cardImg != null)
        {
            cardImg.sprite = UIManager.GetSteppedModalSprite();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
        }

        // Eski Baslik objesini gizle
        Transform oldBaslik = pencereTr.Find("Baslik");
        if (oldBaslik != null) oldBaslik.gameObject.SetActive(false);

        // 1) Üst Mavi Kurdele Başlığı
        Image rMain, rLeft, rRight;
        TMP_Text ribbonTitle;
        UIManager.CreateRibbonBanner(
            cardRt, font, "AFET BİLİNCİ SINAVI (+%10 FARKINDALIK)",
            UIManager.RibbonBlue, UIManager.RibbonBlueDark, UIManager.RibbonBlueDark,
            out rMain, out rLeft, out rRight, out ribbonTitle);
        ribbonTitle.fontSize = 25f;

        // Sağ Üst Kapat (X) Butonu
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
            closeRt.sizeDelta = new Vector2(46f, 46f);
            kapatButonu.transform.SetAsLastSibling(); // kurdelenin önünde dursun

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

        // 2) Büyük Koyu Soru Kutusu
        GameObject qBox = new GameObject("QuestionSlotBox", typeof(RectTransform), typeof(Image));
        qBox.transform.SetParent(cardRt, false);
        RectTransform qbRt = qBox.GetComponent<RectTransform>();
        qbRt.anchorMin = new Vector2(0f, 1f);
        qbRt.anchorMax = new Vector2(1f, 1f);
        qbRt.pivot = new Vector2(0.5f, 1f);
        qbRt.anchoredPosition = new Vector2(0f, -94f);
        qbRt.sizeDelta = new Vector2(-64f, 136f);

        Image qbImg = qBox.GetComponent<Image>();
        qbImg.sprite = UIManager.GetDarkSlotSprite();
        qbImg.type = Image.Type.Sliced;

        if (soruText != null)
        {
            LayoutElement le = soruText.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);
            soruText.transform.SetParent(qBox.transform, false);
            RectTransform stRt = soruText.rectTransform;
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = new Vector2(24f, 14f);
            stRt.offsetMax = new Vector2(-24f, -14f);
            soruText.fontSize = 25f;
            soruText.enableAutoSizing = true;
            soruText.fontSizeMin = 20f;
            soruText.fontSizeMax = 28f;
            soruText.fontStyle = FontStyles.Bold;
            soruText.color = Color.white;
            soruText.alignment = TextAlignmentOptions.Center;
            soruText.textWrappingMode = TextWrappingModes.Normal;
        }

        // 3) 4'lü Şık Listesi (Krem Arka Planlı İki Tonlu Blok Butonlar)
        GameObject choicesContainer = new GameObject("ChoicesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        choicesContainer.transform.SetParent(cardRt, false);
        RectTransform ccRt = choicesContainer.GetComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0f, 1f);
        ccRt.anchorMax = new Vector2(1f, 1f);
        ccRt.pivot = new Vector2(0.5f, 1f);
        ccRt.anchoredPosition = new Vector2(0f, -244f);
        ccRt.sizeDelta = new Vector2(-64f, 286f);

        VerticalLayoutGroup cVlg = choicesContainer.GetComponent<VerticalLayoutGroup>();
        cVlg.spacing = 12f;
        cVlg.childControlWidth = true;
        cVlg.childControlHeight = true;
        cVlg.childForceExpandWidth = true;
        cVlg.childForceExpandHeight = true;

        for (int i = 0; i < sikButonlari.Length; i++)
        {
            Button btn = sikButonlari[i];
            if (btn == null) continue;

            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);
            btn.transform.SetParent(choicesContainer.transform, false);

            // Sol bölmede A / B / C / D harfi
            Transform oldBadge = btn.transform.Find("LetterBadge");
            if (oldBadge != null) Destroy(oldBadge.gameObject);

            GameObject badgeObj = new GameObject("LetterBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObj.transform.SetParent(btn.transform, false);
            RectTransform bRt = badgeObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, 2f);
            bRt.sizeDelta = new Vector2(44f, 0f);

            TextMeshProUGUI bTxt = badgeObj.GetComponent<TextMeshProUGUI>();
            if (font != null) bTxt.font = font;
            bTxt.text = i < SikHarfleri.Length ? SikHarfleri[i] : (i + 1).ToString();
            bTxt.fontSize = 22f;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.color = Color.white;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.raycastTarget = false;

            if (i < sikTextleri.Length && sikTextleri[i] != null)
            {
                TMP_Text st = sikTextleri[i];
                st.fontSize = 21f;
                st.enableAutoSizing = true;
                st.fontSizeMin = 16f;
                st.fontSizeMax = 22f;
                st.fontStyle = FontStyles.Bold;
                st.color = UIManager.DarkInk;
                st.alignment = TextAlignmentOptions.MidlineLeft;
                RectTransform sRt = st.rectTransform;
                sRt.anchorMin = Vector2.zero;
                sRt.anchorMax = Vector2.one;
                sRt.offsetMin = new Vector2(58f, 6f);
                sRt.offsetMax = new Vector2(-18f, -2f);
            }
        }

        // 4) Alt Geri Bildirim & Devam Et Kutusu
        feedbackBox = new GameObject("FeedbackBox", typeof(RectTransform), typeof(Image));
        feedbackBox.transform.SetParent(cardRt, false);
        RectTransform fbRt = feedbackBox.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 0f);
        fbRt.anchorMax = new Vector2(1f, 0f);
        fbRt.pivot = new Vector2(0.5f, 0f);
        fbRt.anchoredPosition = new Vector2(0f, 22f);
        fbRt.sizeDelta = new Vector2(-64f, 84f);

        feedbackBoxImg = feedbackBox.GetComponent<Image>();
        feedbackBoxImg.sprite = UIManager.GetCreamBoxSprite(UIManager.CreamBg, UIManager.CreamShadow);
        feedbackBoxImg.type = Image.Type.Sliced;

        feedbackText = UIManager.CreateText(
            feedbackBox.transform, "FeedbackText", font, "", 18f, FontStyles.Bold,
            UIManager.DarkInk, TextAlignmentOptions.MidlineLeft);
        feedbackText.enableAutoSizing = true;
        feedbackText.fontSizeMin = 12f;
        feedbackText.fontSizeMax = 18f;
        RectTransform ftRt = feedbackText.rectTransform;
        ftRt.anchorMin = Vector2.zero;
        ftRt.anchorMax = Vector2.one;
        ftRt.offsetMin = new Vector2(18f, 10f);
        ftRt.offsetMax = new Vector2(-190f, -6f);

        TMP_Text devamLabel;
        devamBtn = UIManager.CreateTwoToneArcadeButton(
            feedbackBox.transform, "DevamBtn", font, "DEVAM ET",
            UIManager.GreenBtn, UIManager.GreenBtnLeft, UIManager.GreenBtnDark,
            UIManager.DarkInk, UIManager.IconType.Play, out devamLabel);
        RectTransform dvRt = devamBtn.GetComponent<RectTransform>();
        dvRt.anchorMin = new Vector2(1f, 0.5f);
        dvRt.anchorMax = new Vector2(1f, 0.5f);
        dvRt.pivot = new Vector2(1f, 0.5f);
        dvRt.anchoredPosition = new Vector2(-14f, 2f);
        dvRt.sizeDelta = new Vector2(166f, 52f);
        devamBtn.onClick.AddListener(OkulaDon); // okul popup'ına döner: oyuncu isterse yeniden Quiz Çöz der, isterse çıkar

        feedbackBox.SetActive(false);

        // Sonradan eklenen objeler kapat butonunun üstüne binmesin
        if (kapatButonu != null) kapatButonu.transform.SetAsLastSibling();
    }

    private void FitQuizToSafeArea(bool force)
    {
        if (quizCardRt == null) return;
        Rect safeArea = Screen.safeArea;
        if (!force && quizLastScreenWidth == Screen.width && quizLastScreenHeight == Screen.height &&
            quizLastSafeArea == safeArea) return;

        RectTransform parentRect = quizCardRt.parent as RectTransform;
        if (parentRect == null) return;

        Canvas canvas = quizPanel.GetComponentInParent<Canvas>();
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

        // Keep the close button and decorative card trim inside the usable screen area.
        float availableWidth = Mathf.Max(1f, safeMax.x - safeMin.x - 96f);
        float availableHeight = Mathf.Max(1f, safeMax.y - safeMin.y - 32f);
        float fitScale = Mathf.Min(nominalQuizScale,
            Mathf.Min(availableWidth / quizCardRt.sizeDelta.x, availableHeight / quizCardRt.sizeDelta.y));
        quizCardRt.localScale = Vector3.one * Mathf.Max(0.01f, fitScale);
        quizCardRt.anchoredPosition = (safeMin + safeMax) * 0.5f;

        quizLastScreenWidth = Screen.width;
        quizLastScreenHeight = Screen.height;
        quizLastSafeArea = safeArea;
    }

    private void ApplyChoiceVisualState(int index, Color32 mainFill, Color32 leftFill, Color32 shadowCol, Color32 textColor)
    {
        if (index < 0 || index >= sikButonlari.Length || sikButonlari[index] == null) return;
        Image img = sikButonlari[index].GetComponent<Image>();
        if (img != null)
        {
            img.sprite = UIManager.GetTwoToneButtonSprite(mainFill, leftFill, shadowCol);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        if (index < sikTextleri.Length && sikTextleri[index] != null)
        {
            sikTextleri[index].color = textColor;
        }
    }

    public void QuizAc()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        StopAllCoroutines();
        gm.oyunDurduMu = true;
        quizPanel.SetActive(true);
        FitQuizToSafeArea(true);
        if (feedbackBox != null) feedbackBox.SetActive(false);

        // 1) Okul seviyesinin izin verdiği farkındalık tavanına ulaşıldı mı?
        float tavan = DengeAyarlari.Aktif.FarkindalikTavani(gm.okulLevel);
        if (gm.farkindalikYuzdesi >= tavan)
        {
            SiklariGoster(false);
            soruText.text = gm.okulLevel < DengeAyarlari.Aktif.maxLevel
                ? "Bu okul seviyesinde farkındalık en fazla %" + Mathf.RoundToInt(tavan) + " olabilir.\n\nDaha fazla eğitim için okulu yükselt!"
                : "Halkın farkındalığı zaten %100! Tebrikler.";
            return;
        }

        // 2) Çözülmemiş soru kaldı mı?
        if (kalanSorular.Count == 0)
        {
            SiklariGoster(false);
            soruText.text = "Bütün soruları doğru cevapladın! Halk afetlere karşı bilinçlendi.";
            return;
        }

        cevapVerildi = false;
        SiklariGoster(true);
        aktifSoru = kalanSorular[Random.Range(0, kalanSorular.Count)];
        soruText.text = aktifSoru.soruMetni.Trim();

        for (int i = 0; i < sikButonlari.Length; i++)
        {
            if (sikButonlari[i] == null) continue;
            bool aktif = i < aktifSoru.siklar.Length;
            sikButonlari[i].gameObject.SetActive(aktif);
            if (!aktif) continue;

            if (sikTextleri[i] != null) sikTextleri[i].text = aktifSoru.siklar[i].Trim();
            sikButonlari[i].interactable = true;

            // Tüm şıkların başlangıç arka planı KREM (#FFF8E7), sol harf kutusu mavi
            ApplyChoiceVisualState(i, UIManager.CreamBg, UIManager.BlueBtnLeft, UIManager.CreamShadow, UIManager.DarkInk);
        }
    }

    private void SiklariGoster(bool goster)
    {
        for (int i = 0; i < sikButonlari.Length; i++)
            if (sikButonlari[i] != null)
                sikButonlari[i].gameObject.SetActive(goster);
    }

    private void SikSecildi(int secilenIndex)
    {
        if (cevapVerildi || aktifSoru == null) return;
        cevapVerildi = true;

        var gm = GameManager.Instance;

        for (int i = 0; i < sikButonlari.Length; i++)
            if (sikButonlari[i] != null) sikButonlari[i].interactable = false;

        // Doğru ve yanlış şıkların arka plan renklerini koru (Yeşil / Kırmızı)
        ApplyChoiceVisualState(
            aktifSoru.dogruSikIndex,
            UIManager.GreenBtn, UIManager.GreenBtnLeft, UIManager.GreenBtnDark, Color.white);

        string aciklamaStr = !string.IsNullOrEmpty(aktifSoru.aciklama)
            ? aktifSoru.aciklama
            : "Doğru cevap: " + aktifSoru.siklar[aktifSoru.dogruSikIndex].Trim();

        if (secilenIndex == aktifSoru.dogruSikIndex)
        {
            float eklenen = gm != null ? gm.FarkindalikEkle(DengeAyarlari.Aktif.dogruCevapOdulu) : 0f;
            if (gm != null) gm.dogruCevapSayisi++;
            kalanSorular.Remove(aktifSoru);

            soruText.text = "DOĞRU!  Farkındalık +%" + Mathf.RoundToInt(eklenen);

            if (feedbackBox != null)
            {
                feedbackBox.SetActive(true);
                feedbackBoxImg.sprite = UIManager.GetCreamBoxSprite(new Color32(212, 248, 224, 255), UIManager.GreenBtnDark);
                feedbackText.color = new Color32(21, 93, 49, 255);
                feedbackText.text = "DOĞRU! +%" + Mathf.RoundToInt(eklenen) + " Farkındalık. " + aciklamaStr;
            }
        }
        else
        {
            ApplyChoiceVisualState(
                secilenIndex,
                UIManager.RedClose, UIManager.RedCloseDark, UIManager.RedCloseDark, Color.white);

            soruText.text = "YANLIŞ!  Doğru cevap: " + aktifSoru.siklar[aktifSoru.dogruSikIndex].Trim();

            if (feedbackBox != null)
            {
                feedbackBox.SetActive(true);
                feedbackBoxImg.sprite = UIManager.GetCreamBoxSprite(new Color32(255, 222, 224, 255), UIManager.RedCloseDark);
                feedbackText.color = new Color32(140, 29, 36, 255);
                feedbackText.text = "YANLIŞ! " + aciklamaStr;
            }
        }

        // Kendiliğinden kapanmaz: oyuncu açıklamayı okuyup DEVAM ET ya da X ile kapatır
    }

    public void OkulaDon()
    {
        QuizKapat();
        if (BinaPopup.Instance != null) BinaPopup.Instance.Ac(BinaTuru.Okul);
    }

    public void QuizKapat()
    {
        StopAllCoroutines();
        quizPanel.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.oyunDurduMu = false;
    }
}
