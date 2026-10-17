using UnityEngine;
using UnityEngine.SceneManagement;
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
