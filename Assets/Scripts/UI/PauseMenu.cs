using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Ana Canvas'a eklenir (pausePanel'in üstüne değil, panel kapanınca script de dururdu).
// Oyunu Time.timeScale ile durdurur; quiz'in kullandığı oyunDurduMu'ya dokunmaz, böylece ikisi karışmaz.
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public SettingsPanel ayarlarPaneli;
    public string anaMenuSahnesi = "MainMenu";

    public static bool DurdurulduMu { get; private set; }
    RectTransform pauseCard;

    void Start()
    {
        foreach(Transform child in pausePanel.transform)child.gameObject.SetActive(false);
        var overlay=pausePanel.GetComponent<Image>();
        if(overlay!=null){overlay.sprite=null;overlay.color=new Color32(10,18,28,190);}
        var box=CivicUI.Box("CivicPauseCard",pausePanel.transform,Color.white,true);
        box.sprite=CivicUI.GetBeveledCardSprite(CivicUI.Mint,CivicUI.MintShadow);
        box.type=Image.Type.Sliced;
        pauseCard=box.rectTransform;
        pauseCard.sizeDelta=new Vector2(560,390);

        TMP_FontAsset font=TurkishTMPFontSupport.PreferredFont;

        // Folded Blue Ribbon
        CivicUI.CreateRibbon(
            pauseCard,
            "OYUN DURAKLATILDI",
            CivicUI.RibbonBlue,CivicUI.RibbonBlueLip,CivicUI.RibbonBlueTail,
            font);

        // Subtitle pill badge
        var badge=CivicUI.Box("SubtitleBadge",pauseCard,Color.white,false);
        badge.sprite=CivicUI.GetPillBadgeSprite(CivicUI.Cream,CivicUI.CreamShadow);
        badge.type=Image.Type.Sliced;
        CivicUI.Place(badge.rectTransform,60,68,440,32);

        var subText=CivicUI.Text("SubText",badge.transform,"Zaman durduruldu · Şehrin güvende",15,CivicUI.Navy,font);
        subText.fontStyle=FontStyles.Bold;
        subText.alignment=TextAlignmentOptions.Center;
        CivicUI.Fill(subText.rectTransform,4);

        // 1) Devam Et Button
        var resumeBtn=CivicUI.CreateTwoToneButton(
            pauseCard,"ResumeBtn",font,"ŞEHRE DEVAM ET   →",
            CivicUI.Green,CivicUI.GreenLeft,CivicUI.GreenShadow,
            Color.white,CivicUI.IconType.Play,Devam);
        CivicUI.Place((RectTransform)resumeBtn.transform,32,122,496,56);

        // 2) Ayarlar & Kontroller Button
        var setBtn=CivicUI.CreateTwoToneButton(
            pauseCard,"SettingsBtn",font,"AYARLAR & KONTROLLER",
            CivicUI.Cyan,CivicUI.CyanLeft,CivicUI.CyanShadow,
            Color.white,CivicUI.IconType.Gear,AyarlariAc);
        CivicUI.Place((RectTransform)setBtn.transform,32,194,496,56);

        // 3) Ana Menü Button
        var menuBtn=CivicUI.CreateTwoToneButton(
            pauseCard,"MenuBtn",font,"ANA MENÜYE DÖN",
            CivicUI.Navy,CivicUI.NavyLeft,CivicUI.NavyShadow,
            Color.white,CivicUI.IconType.Home,AnaMenuyeDon);
        CivicUI.Place((RectTransform)menuBtn.transform,32,266,496,56);
    }
    void LateUpdate(){if(pausePanel.activeSelf&&pauseCard!=null)CivicUI.FitCard(pauseCard,new Vector2(560,350),32,1.6f);}

    void Awake()
    {
        DurdurulduMu = false;
        pausePanel.SetActive(false);
        if (ayarlarPaneli != null) ayarlarPaneli.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!EscBasildi()) return;

        // Ayarlar açıksa ESC önce ayarları kapatır
        if (ayarlarPaneli != null && ayarlarPaneli.AcikMi)
        {
            ayarlarPaneli.Kapat();
            return;
        }

        if (DurdurulduMu) Devam();
        else Durdur();
    }

    public void Durdur()
    {
        DurdurulduMu = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }

    public void Devam()
    {
        if (ayarlarPaneli != null && ayarlarPaneli.AcikMi) ayarlarPaneli.Kapat();
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        DurdurulduMu = false;
        GetComponent<IslandSimulationPause>()?.ApplyPause();
    }

    public void AyarlariAc()
    {
        if (ayarlarPaneli != null) ayarlarPaneli.Ac();
    }

    public void AnaMenuyeDon()
    {
        Time.timeScale = 1f;
        DurdurulduMu = false;
        SceneManager.LoadScene(anaMenuSahnesi);
    }

    void OnDestroy()
    {
        // Sahne kapanırken oyun duraklatılmış kaldıysa zamanı geri aç
        if (DurdurulduMu)
        {
            Time.timeScale = 1f;
            DurdurulduMu = false;
        }
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
