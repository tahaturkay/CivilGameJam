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
        var overlay=pausePanel.GetComponent<Image>();if(overlay!=null){overlay.sprite=null;overlay.color=new Color(.04f,.08f,.11f,.65f);}
        pauseCard=CivicUI.Box("CivicPauseCard",pausePanel.transform,CivicUI.Cream,true).rectTransform;
        var title=CivicUI.Text("Title",pauseCard,"KISA BİR MOLA",32,CivicUI.Navy);title.fontStyle=FontStyles.Bold;CivicUI.Place(title.rectTransform,32,24,500,56);
        var subtitle=CivicUI.Text("Subtitle",pauseCard,"Şehir seni bekliyor.",21,CivicUI.Muted);CivicUI.Place(subtitle.rectTransform,32,86,500,40);
        var resume=CivicUI.Button("Resume",pauseCard,"ŞEHRE DÖN",CivicUI.Teal,CivicUI.Cream,null,Devam);CivicUI.Place((RectTransform)resume.transform,32,152,500,62);
        var settings=CivicUI.Button("Settings",pauseCard,"AYARLAR & KONTROLLER",CivicUI.Amber,CivicUI.Navy,null,AyarlariAc);CivicUI.Place((RectTransform)settings.transform,32,232,500,62);
        var menu=CivicUI.Button("Menu",pauseCard,"ANA MENÜ",CivicUI.Navy,CivicUI.Cream,null,AnaMenuyeDon);CivicUI.Place((RectTransform)menu.transform,32,312,500,62);
    }
    void LateUpdate(){if(pausePanel.activeSelf&&pauseCard!=null)CivicUI.FitCard(pauseCard,new Vector2(564,410),32,1.6f);}

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
