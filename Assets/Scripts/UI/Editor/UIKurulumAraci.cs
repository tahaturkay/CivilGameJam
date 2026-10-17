using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Menü: Araclar > UI Kurulum
// Ana menü sahnesini, ayarlar prefab'ını ve ESC menüsünü düz (tasarımsız) halde kurar ve butonları bağlar.
// Tasarımcı sonra objelerin görünümünü değiştirebilir; objeleri silmemesi / yeniden adlandırmaması yeterli.
public static class UIKurulumAraci
{
    const string SettingsPrefabYolu = "Assets/Prefabs/UI/SettingsPanel.prefab";
    const string AnaCanvasYolu = "Assets/Prefabs/UI/Ana Canvas.prefab";
    const string MenuSahneYolu = "Assets/Scenes/MainMenu.unity";

    static readonly Color OrtuRengi = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color PencereRengi = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    static readonly Color ArkaplanRengi = new Color(0.08f, 0.16f, 0.24f, 1f);

    // ---------- 1) Ayarlar prefab'ı ----------

    [MenuItem("Araclar/UI Kurulum/1 - SettingsPanel Prefab Olustur")]
    public static void SettingsPrefabOlustur()
    {
        if (File.Exists(SettingsPrefabYolu) &&
            !EditorUtility.DisplayDialog("SettingsPanel", "SettingsPanel.prefab zaten var. Üzerine yazılsın mı?", "Evet", "İptal"))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPrefabYolu));

        // Kök: ekranı kaplayan yarı saydam örtü (arkadaki butonlara tıklanmasın)
        GameObject kok = Panel("SettingsPanel", null, Vector2.zero, OrtuRengi);
        GameObject pencere = Panel("Pencere", kok.transform, new Vector2(760, 480), PencereRengi);
        Dikey(pencere, 20);

        Yazi("Baslik", pencere.transform, "AYARLAR", 44);
        Slider muzik = SliderSatiri("MuzikSatiri", pencere.transform, "Müzik");
        Slider efekt = SliderSatiri("EfektSatiri", pencere.transform, "Efekt");
        Toggle tamEkran = ToggleSatiri("TamEkranSatiri", pencere.transform, "Tam Ekran");
        Button kapat = Buton("KapatButonu", pencere.transform, "Kapat");

        var panel = kok.AddComponent<SettingsPanel>();
        panel.muzikSlider = muzik;
        panel.efektSlider = efekt;
        panel.tamEkranToggle = tamEkran;
        Bagla(kapat, panel.Kapat);

        PrefabUtility.SaveAsPrefabAsset(kok, SettingsPrefabYolu);
        Object.DestroyImmediate(kok);
        Debug.Log("UI Kurulum: " + SettingsPrefabYolu + " oluşturuldu.");
    }

    // ---------- 2) Ana menü sahnesi ----------

    [MenuItem("Araclar/UI Kurulum/2 - MainMenu Sahnesi Olustur")]
    public static void MenuSahnesiOlustur()
    {
        var settingsPrefab = SettingsPrefabiAl();
        if (settingsPrefab == null) return;

        if (File.Exists(MenuSahneYolu) &&
            !EditorUtility.DisplayDialog("MainMenu", "MainMenu.unity zaten var. Üzerine yazılsın mı?", "Evet", "İptal"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sahne = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject canvas = CanvasOlustur("MenuCanvas");
        EventSystemOlustur();

        Panel("Arkaplan", canvas.transform, Vector2.zero, ArkaplanRengi);

        GameObject anaPanel = Panel("AnaPanel", canvas.transform, new Vector2(520, 520), PencereRengi);
        Dikey(anaPanel, 24);
        Yazi("OyunAdi", anaPanel.transform, "OYUN ADI", 56);
        Button basla = Buton("BaslaButonu", anaPanel.transform, "Başla");
        Button ayarlar = Buton("AyarlarButonu", anaPanel.transform, "Ayarlar");
        Button cikis = Buton("CikisButonu", anaPanel.transform, "Çıkış");

        var ayarPaneli = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, canvas.transform);
        TamEkranYap(ayarPaneli.GetComponent<RectTransform>());

        var menu = canvas.AddComponent<MainMenu>();
        menu.ayarlarPaneli = ayarPaneli.GetComponent<SettingsPanel>();
        Bagla(basla, menu.Basla);
        Bagla(ayarlar, menu.AyarlariAc);
        Bagla(cikis, menu.Cikis);

        new GameObject("MusicPlayer", typeof(AudioSource), typeof(MusicPlayer));

        EditorSceneManager.SaveScene(sahne, MenuSahneYolu);
        Debug.Log("UI Kurulum: " + MenuSahneYolu + " oluşturuldu. Build Profiles'ta en üste (0. sıra) eklenmeli.");
    }

    // ---------- 3) ESC menüsü (Ana Canvas prefab'ına) ----------

    [MenuItem("Araclar/UI Kurulum/3 - Ana Canvas'a ESC Menusu Ekle")]
    public static void PauseMenusuEkle()
    {
        var settingsPrefab = SettingsPrefabiAl();
        if (settingsPrefab == null) return;

        GameObject kok = PrefabUtility.LoadPrefabContents(AnaCanvasYolu);
        try
        {
            if (kok.GetComponent<PauseMenu>() != null)
            {
                EditorUtility.DisplayDialog("ESC Menüsü", "Ana Canvas'ta zaten bir PauseMenu var.", "Tamam");
                return;
            }

            GameObject pausePanel = Panel("PausePanel", kok.transform, Vector2.zero, OrtuRengi);
            GameObject pencere = Panel("Pencere", pausePanel.transform, new Vector2(520, 480), PencereRengi);
            Dikey(pencere, 24);
            Yazi("Baslik", pencere.transform, "DURAKLATILDI", 44);
            Button devam = Buton("DevamButonu", pencere.transform, "Devam");
            Button ayarlar = Buton("AyarlarButonu", pencere.transform, "Ayarlar");
            Button anaMenu = Buton("AnaMenuButonu", pencere.transform, "Ana Menü");

            // Ayarlar en sonda: ESC menüsünün de üstünde görünsün
            var ayarPaneli = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, kok.transform);
            TamEkranYap(ayarPaneli.GetComponent<RectTransform>());

            var pause = kok.AddComponent<PauseMenu>();
            pause.pausePanel = pausePanel;
            pause.ayarlarPaneli = ayarPaneli.GetComponent<SettingsPanel>();
            Bagla(devam, pause.Devam);
            Bagla(ayarlar, pause.AyarlariAc);
            Bagla(anaMenu, pause.AnaMenuyeDon);

            // Oyun müziği de Ana Canvas ile gelsin ki MainScene'e ayrıca bir şey eklemek gerekmesin
            if (kok.GetComponentInChildren<MusicPlayer>(true) == null)
                new GameObject("MusicPlayer", typeof(AudioSource), typeof(MusicPlayer)).transform.SetParent(kok.transform, false);

            PrefabUtility.SaveAsPrefabAsset(kok, AnaCanvasYolu);
            Debug.Log("UI Kurulum: Ana Canvas'a ESC menüsü ve MusicPlayer eklendi.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(kok);
        }
    }

    // ---------- Yardımcılar ----------

    static GameObject SettingsPrefabiAl()
    {
        if (!File.Exists(SettingsPrefabYolu)) SettingsPrefabOlustur();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabYolu);
        if (prefab == null) Debug.LogError("UI Kurulum: " + SettingsPrefabYolu + " bulunamadı.");
        return prefab;
    }

    static GameObject CanvasOlustur(string ad)
    {
        var go = new GameObject(ad, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    static void EventSystemOlustur()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static DefaultControls.Resources UiKaynaklari()
    {
        var r = new DefaultControls.Resources();
        r.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        r.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        r.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        r.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        r.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        r.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        r.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return r;
    }

    static TMP_DefaultControls.Resources TmpKaynaklari()
    {
        var u = UiKaynaklari();
        var r = new TMP_DefaultControls.Resources();
        r.standard = u.standard;
        r.background = u.background;
        r.inputField = u.inputField;
        r.knob = u.knob;
        r.checkmark = u.checkmark;
        r.dropdown = u.dropdown;
        r.mask = u.mask;
        return r;
    }

    static void TamEkranYap(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // boyut sıfırsa ekranı kaplar, değilse ortada o boyutta durur
    static GameObject Panel(string ad, Transform ebeveyn, Vector2 boyut, Color renk)
    {
        GameObject go = DefaultControls.CreatePanel(UiKaynaklari());
        go.name = ad;
        if (ebeveyn != null) go.transform.SetParent(ebeveyn, false);

        var rt = go.GetComponent<RectTransform>();
        if (boyut == Vector2.zero)
        {
            TamEkranYap(rt);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = boyut;
            rt.anchoredPosition = Vector2.zero;
        }
        go.GetComponent<Image>().color = renk;
        return go;
    }

    static void Dikey(GameObject go, float bosluk)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = bosluk;
        v.padding = new RectOffset(40, 40, 30, 30);
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
    }

    static TMP_Text Yazi(string ad, Transform ebeveyn, string metin, float boyut)
    {
        GameObject go = TMP_DefaultControls.CreateText(TmpKaynaklari());
        go.name = ad;
        go.transform.SetParent(ebeveyn, false);
        var t = go.GetComponent<TMP_Text>();
        t.text = metin;
        t.fontSize = boyut;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = boyut * 1.5f;
        return t;
    }

    static Button Buton(string ad, Transform ebeveyn, string metin)
    {
        GameObject go = TMP_DefaultControls.CreateButton(TmpKaynaklari());
        go.name = ad;
        go.transform.SetParent(ebeveyn, false);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = metin;
        t.fontSize = 30;
        go.AddComponent<LayoutElement>().preferredHeight = 64;
        return go.GetComponent<Button>();
    }

    // Satır: solda etiket, sağda kontrol
    static Transform Satir(string ad, Transform ebeveyn, string etiket)
    {
        var satir = new GameObject(ad, typeof(RectTransform));
        satir.transform.SetParent(ebeveyn, false);
        var h = satir.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 20;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        satir.AddComponent<LayoutElement>().preferredHeight = 50;

        TMP_Text yazi = Yazi("Etiket", satir.transform, etiket, 28);
        yazi.alignment = TextAlignmentOptions.MidlineLeft;
        var le = yazi.GetComponent<LayoutElement>();
        le.preferredWidth = 240;
        le.preferredHeight = 50;
        return satir.transform;
    }

    static void KontrolBoyutu(GameObject go, float yukseklik)
    {
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = yukseklik;
    }

    static Slider SliderSatiri(string ad, Transform ebeveyn, string etiket)
    {
        Transform satir = Satir(ad, ebeveyn, etiket);
        GameObject go = DefaultControls.CreateSlider(UiKaynaklari());
        go.name = "Slider";
        go.transform.SetParent(satir, false);
        KontrolBoyutu(go, 30);
        var s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.8f;
        return s;
    }

    static Toggle ToggleSatiri(string ad, Transform ebeveyn, string etiket)
    {
        Transform satir = Satir(ad, ebeveyn, etiket);
        GameObject go = DefaultControls.CreateToggle(UiKaynaklari());
        go.name = "Toggle";
        go.transform.SetParent(satir, false);
        KontrolBoyutu(go, 30);
        var etiketYazisi = go.GetComponentInChildren<Text>();
        if (etiketYazisi != null) etiketYazisi.text = ""; // satırın kendi etiketi var
        return go.GetComponent<Toggle>();
    }

    // Inspector'daki On Click'e kalıcı bağlantı ekler (elle sürüklemek gibi)
    static void Bagla(Button buton, UnityAction fonksiyon)
    {
        UnityEventTools.AddPersistentListener(buton.onClick, fonksiyon);
    }
}
