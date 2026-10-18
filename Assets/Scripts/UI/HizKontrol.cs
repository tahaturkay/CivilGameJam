using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Ana Canvas'a eklenir. HUD'daki Duraklat / 1x / 2x / 3x butonları.
// Hız Time.timeScale'e dokunmaz; sadece gün süresini değiştirir (GameManager.hizCarpani).
[DefaultExecutionOrder(-200)]
public class HizKontrol : MonoBehaviour
{
    public Button duraklatButonu;
    public Button[] hizButonlari = new Button[3]; // 1x, 2x, 3x

    public Color normalRenk = Color.white;
    public Color seciliRenk = new Color(1f, 0.8f, 0.3f);

    void Start()
    {
        if(GameManager.Instance!=null)
            HizAyarla(DengeAyarlari.Aktif.baslangicHizi);
    }

    public void HizAyarla(int hiz)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.hizCarpani = Mathf.Clamp(hiz, 1, 3);
        gm.kullaniciDurdurdu = false;
        GetComponent<IslandSimulationPause>()?.ApplyPause();
    }

    public void DuraklatDegistir()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.kullaniciDurdurdu = !gm.kullaniciDurdurdu;
        GetComponent<IslandSimulationPause>()?.ApplyPause();
    }

    void Update()
    {
        // Handle Space before EventSystem can interpret it as Submit on a selected button.
        if (SpacePressed())
        {
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.TryHandleSpacePauseInput();
        }
        else if (!PlaybackModifierHeld() && SpeedKeyPressed(out int speed))
        {
            // Match the HUD buttons and Space shortcut, including modal/text-field guards.
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.TryHandleSpeedHotkeyInput(speed);
        }

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Seçili olan butonu renklendir
        Boya(duraklatButonu, gm.kullaniciDurdurdu);
        for (int i = 0; i < hizButonlari.Length; i++)
            Boya(hizButonlari[i], !gm.kullaniciDurdurdu && gm.hizCarpani == i + 1);
    }

    void Boya(Button buton, bool secili)
    {
        if (buton == null || buton.targetGraphic == null) return;
        buton.targetGraphic.color = secili ? seciliRenk : normalRenk;
    }

    static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    static bool SpeedKeyPressed(out int speed)
    {
        speed = 0;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) speed = 1;
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) speed = 2;
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) speed = 3;
            if (speed != 0) return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) speed = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) speed = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) speed = 3;
#endif
        return speed != 0;
    }

    static bool PlaybackModifierHeld()
    {
        bool held = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
            held |= keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed ||
                    keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ||
                    keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed ||
                    keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        held |= Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) ||
                Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#endif
        return held;
    }
}
