using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenu : MonoBehaviour
{
    public string oyunSahnesi = "MainScene";
    public SettingsPanel ayarlarPaneli;
    public string oyunBasligi = "DİRENÇLİ\nŞEHİR";
    RectTransform menuCard;

    void Start()
    {
        Time.timeScale = 1f;
        BuildMenu();
        if (ayarlarPaneli != null) ayarlarPaneli.gameObject.SetActive(false);
    }
    void BuildMenu()
    {
        TMP_FontAsset font = null;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true)) if (t.font != null) { font=t.font; break; }
        if (ayarlarPaneli == null) ayarlarPaneli = GetComponentInChildren<SettingsPanel>(true);
        // Preserve the settings instance and its serialized references while replacing the old menu.
        if (ayarlarPaneli != null) ayarlarPaneli.transform.SetParent(transform, false);
        foreach (Transform child in transform)
            if (ayarlarPaneli == null || child != ayarlarPaneli.transform) child.gameObject.SetActive(false);
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; }
        var background = CivicUI.Box("CivicMenu", transform, CivicUI.Navy, true);
        CivicUI.Fill(background.rectTransform);
        menuCard = CivicUI.Rect("MenuContent", background.transform);
        menuCard.sizeDelta = new Vector2(1728,900);
        var art = CivicUI.Rect("LowPolyCity", menuCard); CivicUI.Place(art,720,5,1010,790);
        art.gameObject.AddComponent<CityMenuArt>();
        var eyebrow=CivicUI.Text("Eyebrow",menuCard,"BİRLİKTE HAZIRLAN, ŞEHRİNİ KORU",21,CivicUI.Amber,font);
        eyebrow.fontStyle=FontStyles.Bold; CivicUI.Place(eyebrow.rectTransform,20,70,680,42);
        var title=CivicUI.Text("Title",menuCard,oyunBasligi,94,CivicUI.Cream,font);
        title.fontStyle=FontStyles.Bold; title.lineSpacing=-7; CivicUI.Place(title.rectTransform,14,140,730,240);
        var desc=CivicUI.Text("Description",menuCard,"Bir şehrin yarını senin kararlarında.\nYapıları güçlendir, insanları bilinçlendir,\nafetlere birlikte hazırlan.",26,new Color32(186,207,205,255),font);
        CivicUI.Place(desc.rectTransform,20,420,680,132);
        var start=CivicUI.Button("Start",menuCard,"ŞEHRİNE BAŞLA   →",CivicUI.Amber,CivicUI.Navy,font,Basla);
        CivicUI.Place((RectTransform)start.transform,20,610,520,76);
        var settings=CivicUI.Button("Settings",menuCard,"AYARLAR & KONTROLLER",CivicUI.Teal,CivicUI.Cream,font,AyarlariAc);
        CivicUI.Place((RectTransform)settings.transform,20,704,520,70);
        var quit=CivicUI.Button("Quit",menuCard,"ÇIKIŞ",new Color32(39,61,72,255),CivicUI.Cream,font,Cikis);
        CivicUI.Place((RectTransform)quit.transform,20,792,230,60);
        var caption=CivicUI.Text("CityCaption",menuCard,"HER HAZIRLIK BİR HAYATI KORUR",20,CivicUI.Cream,font);
        caption.alignment=TextAlignmentOptions.Center; caption.characterSpacing=3;
        CivicUI.Place(caption.rectTransform,850,780,710,45);
        if (ayarlarPaneli != null) ayarlarPaneli.transform.SetAsLastSibling();
        CivicUI.FitCard(menuCard,new Vector2(1728,900),48);
    }
    void Update()
    {
        if (menuCard != null) CivicUI.FitCard(menuCard,new Vector2(1728,900),48);
        if (EscBasildi() && ayarlarPaneli != null && ayarlarPaneli.AcikMi) ayarlarPaneli.Kapat();
    }
    public void Basla() { Time.timeScale=1; SceneManager.LoadScene(oyunSahnesi); }
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
