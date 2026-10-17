using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// MainMenu sahnesindeki MenuCanvas'a eklenir.
public class MainMenu : MonoBehaviour
{
    public string oyunSahnesi = "MainScene";
    public SettingsPanel ayarlarPaneli;

    void Start()
    {
        Time.timeScale = 1f; // ESC menüsünden dönüldüyse oyun donuk kalmasın
        if (ayarlarPaneli != null) ayarlarPaneli.gameObject.SetActive(false);
    }

    void Update()
    {
        if (EscBasildi() && ayarlarPaneli != null && ayarlarPaneli.AcikMi)
            ayarlarPaneli.Kapat();
    }

    public void Basla()
    {
        SceneManager.LoadScene(oyunSahnesi);
    }

    public void AyarlariAc()
    {
        if (ayarlarPaneli != null) ayarlarPaneli.Ac();
    }

    public void Cikis()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // editörde Quit çalışmadığı için Play'i durdur
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
