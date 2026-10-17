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

    // ---------- 4) Hız butonları, afet raporu, oyun sonu (Ana Canvas prefab'ına) ----------

    [MenuItem("Araclar/UI Kurulum/4 - Ana Canvas'a Oyun Panellerini Ekle")]
    public static void OyunPanelleriniEkle()
    {
        GameObject kok = PrefabUtility.LoadPrefabContents(AnaCanvasYolu);
        try
        {
            // Yeni paneller ESC menüsünün ve ayarların altında kalsın
            Transform pause = kok.transform.Find("PausePanel");
            int sira = pause != null ? pause.GetSiblingIndex() : kok.transform.childCount;

            if (kok.GetComponent<HizKontrol>() == null)
            {
                GameObject hizPaneli = new GameObject("HizPaneli", typeof(RectTransform));
                hizPaneli.transform.SetParent(kok.transform, false);
                hizPaneli.transform.SetSiblingIndex(sira++);
                var rt = hizPaneli.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-20f, -100f);
                rt.sizeDelta = new Vector2(440f, 64f);
                var h = hizPaneli.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 10;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = true;

                Button duraklat = Buton("DuraklatButonu", hizPaneli.transform, "II");
                Button x1 = Buton("Hiz1xButonu", hizPaneli.transform, "1x");
                Button x2 = Buton("Hiz2xButonu", hizPaneli.transform, "2x");
                Button x3 = Buton("Hiz3xButonu", hizPaneli.transform, "3x");

                var hiz = kok.AddComponent<HizKontrol>();
                hiz.duraklatButonu = duraklat;
                hiz.hizButonlari = new[] { x1, x2, x3 };
                Bagla(duraklat, hiz.DuraklatDegistir);
                UnityEventTools.AddIntPersistentListener(x1.onClick, hiz.HizAyarla, 1);
                UnityEventTools.AddIntPersistentListener(x2.onClick, hiz.HizAyarla, 2);
                UnityEventTools.AddIntPersistentListener(x3.onClick, hiz.HizAyarla, 3);
            }

            if (kok.GetComponent<AfetRaporu>() == null)
            {
                GameObject panel = Panel("AfetRaporuPanel", kok.transform, Vector2.zero, OrtuRengi);
                panel.transform.SetSiblingIndex(sira++);
                GameObject pencere = Panel("Pencere", panel.transform, new Vector2(820, 560), PencereRengi);
                Dikey(pencere, 14);

                var rapor = kok.AddComponent<AfetRaporu>();
                rapor.panel = panel;
                rapor.baslikText = Yazi("BaslikText", pencere.transform, "AFET", 52);
                rapor.savunmaText = Yazi("SavunmaText", pencere.transform, "Savunma", 28);
                rapor.savunmaText.GetComponent<LayoutElement>().preferredHeight = 80;
                rapor.vefatText = Yazi("VefatText", pencere.transform, "Vefat", 30);
                rapor.kurtarilanText = Yazi("KurtarilanText", pencere.transform, "Kurtarılan", 28);
                rapor.hasarText = Yazi("HasarText", pencere.transform, "Hasar", 28);
                rapor.nufusText = Yazi("NufusText", pencere.transform, "Nüfus", 28);
                Bagla(Buton("DevamButonu", pencere.transform, "Devam"), rapor.Devam);
            }

            if (kok.GetComponent<OyunSonu>() == null)
            {
                GameObject panel = Panel("OyunSonuPanel", kok.transform, Vector2.zero, OrtuRengi);
                panel.transform.SetSiblingIndex(sira++);
                GameObject pencere = Panel("Pencere", panel.transform, new Vector2(900, 820), PencereRengi);
                Dikey(pencere, 12);

                var son = kok.AddComponent<OyunSonu>();
                son.panel = panel;
                son.baslikText = Yazi("BaslikText", pencere.transform, "OYUN BİTTİ", 56);
                son.dereceText = Yazi("DereceText", pencere.transform, "Derece", 34);
                son.istatistikText = Yazi("IstatistikText", pencere.transform, "İstatistikler", 26);
                son.istatistikText.alignment = TextAlignmentOptions.TopLeft;
                son.istatistikText.GetComponent<LayoutElement>().preferredHeight = 250;
                son.afetOzetiText = Yazi("AfetOzetiText", pencere.transform, "Afetler", 24);
                son.afetOzetiText.GetComponent<LayoutElement>().preferredHeight = 190;
                Bagla(Buton("TekrarOynaButonu", pencere.transform, "Tekrar Oyna"), son.TekrarOyna);
                Bagla(Buton("AnaMenuButonu", pencere.transform, "Ana Menü"), son.AnaMenu);
            }

            PrefabUtility.SaveAsPrefabAsset(kok, AnaCanvasYolu);
            Debug.Log("UI Kurulum: Hız butonları, afet raporu ve oyun sonu ekranı Ana Canvas'a eklendi.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(kok);
        }
    }

    // ---------- 5) Yönlendirici ipucu şeridi (Ana Canvas prefab'ına) ----------

    [MenuItem("Araclar/UI Kurulum/5 - Ana Canvas'a Ipucu Seridi Ekle")]
    public static void IpucuSeridiEkle()
    {
        GameObject kok = PrefabUtility.LoadPrefabContents(AnaCanvasYolu);
        try
        {
            if (kok.GetComponent<IpucuSistemi>() != null) return;

            Transform pause = kok.transform.Find("PausePanel");
            int sira = pause != null ? pause.GetSiblingIndex() : kok.transform.childCount;

            GameObject serit = Panel("IpucuSeridi", kok.transform, new Vector2(1200, 90), new Color(0f, 0f, 0f, 0.7f));
            serit.transform.SetSiblingIndex(sira);
            var rt = serit.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 30f);
            serit.GetComponent<Image>().raycastTarget = false; // arkadaki binalara tıklamayı engellemesin

            TMP_Text yazi = Yazi("IpucuText", serit.transform, "İpucu", 30);
            TamEkranYap(yazi.rectTransform);
            yazi.rectTransform.offsetMin = new Vector2(30, 0);
            yazi.rectTransform.offsetMax = new Vector2(-30, 0);

            var sistem = kok.AddComponent<IpucuSistemi>();
            sistem.serit = serit;
            sistem.ipucuText = yazi;

            PrefabUtility.SaveAsPrefabAsset(kok, AnaCanvasYolu);
            Debug.Log("UI Kurulum: İpucu şeridi Ana Canvas'a eklendi.");
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
