using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The same responsive panel is used by the main menu and the pause menu.
public class SettingsPanel : MonoBehaviour
{
    public Slider muzikSlider;
    public Slider efektSlider;
    public Toggle tamEkranToggle;
    public bool AcikMi { get { return gameObject.activeSelf; } }
    RectTransform card;
    TMP_Text musicValue, effectsValue;
    TMP_Text hudScaleValue, hudOffsetValue;
    Slider hudScaleSlider, hudOffsetSlider;
    bool built;
    void Awake()
    {
        Build();
        if (muzikSlider != null) { muzikSlider.minValue=0; muzikSlider.maxValue=1; muzikSlider.onValueChanged.AddListener(SetMusic); }
        if (efektSlider != null) { efektSlider.minValue=0; efektSlider.maxValue=1; efektSlider.onValueChanged.AddListener(SetEffects); }
        if (tamEkranToggle != null) tamEkranToggle.onValueChanged.AddListener(v => Ayarlar.TamEkran=v);
        if (hudScaleSlider != null) { hudScaleSlider.minValue=.75f; hudScaleSlider.maxValue=1.5f; hudScaleSlider.onValueChanged.AddListener(SetHudScale); }
        if (hudOffsetSlider != null) { hudOffsetSlider.minValue=0f; hudOffsetSlider.maxValue=80f; hudOffsetSlider.wholeNumbers=true; hudOffsetSlider.onValueChanged.AddListener(SetHudOffset); }
    }
    void OnEnable()
    {
        Build();
        if (muzikSlider != null) muzikSlider.SetValueWithoutNotify(Ayarlar.MuzikSesi);
        if (efektSlider != null) efektSlider.SetValueWithoutNotify(Ayarlar.EfektSesi);
        if (tamEkranToggle != null) tamEkranToggle.SetIsOnWithoutNotify(Ayarlar.TamEkran);
        if (hudScaleSlider != null) hudScaleSlider.SetValueWithoutNotify(UIManager.UstPanelOlcegi);
        if (hudOffsetSlider != null) hudOffsetSlider.SetValueWithoutNotify(UIManager.UstPanelDikeyBosluk);
        RefreshValues();
        transform.SetAsLastSibling();
        Fit();
    }
    void LateUpdate() { Fit(); }
    void Fit() { if(card != null) CivicUI.FitCard(card,new Vector2(1100,710),32,CivicUI.ReadableScale(this)); }
    void SetMusic(float value) { Ayarlar.MuzikSesi=value; RefreshValues(); }
    void SetEffects(float value) { Ayarlar.EfektSesi=value; RefreshValues(); }
    void SetHudScale(float value) { UIManager.UstPanelAyarlariniUygula(value,UIManager.UstPanelDikeyBosluk); RefreshValues(); }
    void SetHudOffset(float value) { UIManager.UstPanelAyarlariniUygula(UIManager.UstPanelOlcegi,value); RefreshValues(); }
    void ResetHudSettings()
    {
        UIManager.UstPanelVarsayilanaSifirla();
        if(hudScaleSlider!=null)hudScaleSlider.SetValueWithoutNotify(1f);
        if(hudOffsetSlider!=null)hudOffsetSlider.SetValueWithoutNotify(0f);
        RefreshValues();
    }
    void RefreshValues()
    {
        if(musicValue != null) musicValue.text=Mathf.RoundToInt(Ayarlar.MuzikSesi*100)+"%";
        if(effectsValue != null) effectsValue.text=Mathf.RoundToInt(Ayarlar.EfektSesi*100)+"%";
        if(hudScaleValue != null) hudScaleValue.text=Mathf.RoundToInt(UIManager.UstPanelOlcegi*100)+"%";
        if(hudOffsetValue != null) hudOffsetValue.text=Mathf.RoundToInt(UIManager.UstPanelDikeyBosluk)+" px";
    }
    void Build()
    {
        if(built) return; built=true;
        TMP_FontAsset font=null;
        foreach(var t in GetComponentsInChildren<TMP_Text>(true)) if(t.font != null) { font=t.font; break; }
        font=TurkishTMPFontSupport.EnsureFont(font);
        var overlay=GetComponent<Image>(); if(overlay == null) overlay=gameObject.AddComponent<Image>();
        overlay.sprite=null; overlay.color=new Color32(10,18,28,190); overlay.raycastTarget=true;
        CivicUI.Fill((RectTransform)transform);
        var box=CivicUI.Box("CivicSettingsCard",transform,Color.white,true);
        box.sprite=CivicUI.GetBeveledCardSprite(CivicUI.Mint,CivicUI.MintShadow);
        box.type=Image.Type.Sliced;
        card=box.rectTransform;
        card.sizeDelta=new Vector2(1100,710);

        // Folded Cyan Ribbon Banner
        CivicUI.CreateRibbon(
            card,
            "AYARLAR & KONTROLLER",
            CivicUI.RibbonCyan,CivicUI.RibbonCyanLip,CivicUI.RibbonCyanTail,
            font);

        // Move live controls before hiding the previous prefab layout.
        if(muzikSlider != null) muzikSlider.transform.SetParent(card,false);
        if(efektSlider != null) efektSlider.transform.SetParent(card,false);
        if(tamEkranToggle != null) tamEkranToggle.transform.SetParent(card,false);
        foreach(Transform child in transform) if(child != card) child.gameObject.SetActive(false);

        // Left Container: Audio & HUD
        var leftCard=CivicUI.Box("LeftCard",card,Color.white,false);
        leftCard.sprite=CivicUI.GetBeveledCardSprite(CivicUI.Cream,CivicUI.CreamShadow);
        leftCard.type=Image.Type.Sliced;
        CivicUI.Place(leftCard.rectTransform,36,70,494,544);
        if(muzikSlider!=null)muzikSlider.transform.SetParent(leftCard.transform,false);
        if(efektSlider!=null)efektSlider.transform.SetParent(leftCard.transform,false);

        // Left Header: SES & GÖRÜNTÜ
        var sesBadge=CivicUI.Box("SesBadge",leftCard.transform,Color.white,false);
        sesBadge.sprite=CivicUI.GetPillBadgeSprite(CivicUI.Teal,CivicUI.TealShadow);
        sesBadge.type=Image.Type.Sliced;
        CivicUI.Place(sesBadge.rectTransform,16,14,462,32);
        var sesTitle=CivicUI.Text("Title",sesBadge.transform,"SES & GÖRÜNTÜ",16,Color.white,font);
        sesTitle.fontStyle=FontStyles.Bold;sesTitle.alignment=TextAlignmentOptions.Center;CivicUI.Fill(sesTitle.rectTransform,4);

        Label(leftCard.transform,"Müzik",24,56,260,28,20,CivicUI.Navy,font,true);
        musicValue=Label(leftCard.transform,"",360,56,100,28,19,CivicUI.Teal,font,true);
        musicValue.alignment=TextAlignmentOptions.MidlineRight;
        StyleSlider(muzikSlider,20,88,454,34);

        Label(leftCard.transform,"Efektler",24,128,260,28,20,CivicUI.Navy,font,true);
        effectsValue=Label(leftCard.transform,"",360,128,100,28,19,CivicUI.Teal,font,true);
        effectsValue.alignment=TextAlignmentOptions.MidlineRight;
        StyleSlider(efektSlider,20,160,454,34);

        Label(leftCard.transform,"Tam ekran",24,204,260,32,20,CivicUI.Navy,font,true);
        StyleToggle(leftCard.transform);

        // Left Subheader: ÜST PANEL DÜZENİ
        var hudBadge=CivicUI.Box("HudBadge",leftCard.transform,Color.white,false);
        hudBadge.sprite=CivicUI.GetPillBadgeSprite(CivicUI.Cyan,CivicUI.CyanShadow);
        hudBadge.type=Image.Type.Sliced;
        CivicUI.Place(hudBadge.rectTransform,16,252,462,32);
        var hudTitle=CivicUI.Text("Title",hudBadge.transform,"ÜST PANEL DÜZENİ",16,Color.white,font);
        hudTitle.fontStyle=FontStyles.Bold;hudTitle.alignment=TextAlignmentOptions.Center;CivicUI.Fill(hudTitle.rectTransform,4);

        Label(leftCard.transform,"Panel boyutu",24,294,260,26,18,CivicUI.Navy,font,true);
        hudScaleValue=Label(leftCard.transform,"",360,294,100,26,18,CivicUI.Cyan,font,true);
        hudScaleValue.alignment=TextAlignmentOptions.MidlineRight;
        hudScaleSlider=CreateSlider("TopHudScaleSlider",leftCard.transform);
        StyleSlider(hudScaleSlider,20,324,454,30);

        Label(leftCard.transform,"Üst kenar boşluğu",24,364,260,26,18,CivicUI.Navy,font,true);
        hudOffsetValue=Label(leftCard.transform,"",360,364,100,26,18,CivicUI.Cyan,font,true);
        hudOffsetValue.alignment=TextAlignmentOptions.MidlineRight;
        hudOffsetSlider=CreateSlider("TopHudOffsetSlider",leftCard.transform);
        StyleSlider(hudOffsetSlider,20,394,454,30);

        // Right Container: Controls Guide
        var rightCard=CivicUI.Box("RightCard",card,Color.white,false);
        rightCard.sprite=CivicUI.GetBeveledCardSprite(CivicUI.Cream,CivicUI.CreamShadow);
        rightCard.type=Image.Type.Sliced;
        CivicUI.Place(rightCard.rectTransform,554,70,510,544);

        var guideBadge=CivicUI.Box("GuideBadge",rightCard.transform,Color.white,false);
        guideBadge.sprite=CivicUI.GetPillBadgeSprite(CivicUI.Amber,CivicUI.AmberShadow);
        guideBadge.type=Image.Type.Sliced;
        CivicUI.Place(guideBadge.rectTransform,16,14,478,32);
        var guideTitle=CivicUI.Text("Title",guideBadge.transform,"KAMERA & KONTROLLER",16,CivicUI.Navy,font);
        guideTitle.fontStyle=FontStyles.Bold;guideTitle.alignment=TextAlignmentOptions.Center;CivicUI.Fill(guideTitle.rectTransform,4);

        Guide(rightCard.transform,"W A S D / OK TUŞLARI","Haritada gezin",58,font);
        Guide(rightCard.transform,"SAĞ / ORTA TUŞ","Basılı tutup sürükle",122,font);
        Guide(rightCard.transform,"SPACE","Oyunu duraklat / devam ettir",186,font);
        Guide(rightCard.transform,"TEKERLEK / İKİ PARMAK ↑↓","Yakınlaş / uzaklaş",250,font);
        Guide(rightCard.transform,"SHIFT + KAYDIRMA","Haritayı kaydır",314,font);
        Guide(rightCard.transform,"Q / E   ·   HOME","Yakınlık   ·   Şehri ortala",378,font);
        Guide(rightCard.transform,"ESC","Duraklatma menüsü / ayarları kapat",442,font);

        // Bottom Action Buttons
        var reset=CivicUI.CreateTwoToneButton(card,"ResetHud",font,"ÜST PANELİ SIFIRLA",
            CivicUI.Navy,CivicUI.NavyLeft,CivicUI.NavyShadow,
            Color.white,CivicUI.IconType.Refresh,ResetHudSettings);
        CivicUI.Place((RectTransform)reset.transform,36,630,494,54);

        var done=CivicUI.CreateTwoToneButton(card,"Done",font,"TAMAM",
            CivicUI.Green,CivicUI.GreenLeft,CivicUI.GreenShadow,
            Color.white,CivicUI.IconType.Play,Kapat);
        CivicUI.Place((RectTransform)done.transform,554,630,510,54);
    }
    TMP_Text Label(Transform p,string value,float x,float y,float w,float h,float s,Color c,TMP_FontAsset f,bool bold=false)
    {
        var t=CivicUI.Text("Label",p,value,s,c,f); if(bold)t.fontStyle=FontStyles.Bold; CivicUI.Place(t.rectTransform,x,y,w,h); return t;
    }
    void Guide(Transform parent,string key,string action,float y,TMP_FontAsset font)
    {
        Label(parent,key,26,y,460,25,18,CivicUI.Navy,font,true);
        Label(parent,action,26,y+24,460,24,16,CivicUI.Muted,font);
    }
    void StyleSlider(Slider slider,float x,float y,float width,float height=40)
    {
        if(slider==null)return;
        slider.gameObject.SetActive(true);
        CivicUI.Place((RectTransform)slider.transform,x,y,width,height);
        var layout=slider.GetComponent<LayoutElement>(); if(layout!=null)layout.ignoreLayout=true;
        foreach(var image in slider.GetComponentsInChildren<Image>(true))
        {
            bool handle=image.transform==slider.handleRect;
            bool fill=image.transform==slider.fillRect;
            if(handle)
            {
                image.sprite=CivicUI.GetSquareButtonSprite(CivicUI.Teal,CivicUI.TealShadow);
                image.type=Image.Type.Sliced;
                image.color=Color.white;
            }
            else if(fill)
            {
                image.sprite=CivicUI.GetPillBadgeSprite(CivicUI.Teal,CivicUI.TealShadow);
                image.type=Image.Type.Sliced;
                image.color=Color.white;
            }
            else
            {
                image.sprite=CivicUI.GetPillBadgeSprite(new Color32(212,218,206,255),new Color32(185,192,178,255));
                image.type=Image.Type.Sliced;
                image.color=Color.white;
            }
            image.raycastTarget=handle;
        }
        var background=slider.transform.Find("Background") as RectTransform;
        if(background!=null) { background.anchorMin=new Vector2(0,.35f);background.anchorMax=new Vector2(1,.65f);background.offsetMin=background.offsetMax=Vector2.zero; }
        if(slider.fillRect!=null)
        {
            var area=slider.fillRect.parent as RectTransform;
            if(area!=null){area.anchorMin=new Vector2(0,.35f);area.anchorMax=new Vector2(1,.65f);area.offsetMin=area.offsetMax=Vector2.zero;}
        }
        if(slider.handleRect!=null)
        {
            var area=slider.handleRect.parent as RectTransform;
            if(area!=null)CivicUI.Fill(area);
            bool compact=height<32;
            slider.handleRect.sizeDelta=new Vector2(24,compact?24:30);
            slider.handleRect.offsetMin=new Vector2(slider.handleRect.offsetMin.x,compact?1:3);
            slider.handleRect.offsetMax=new Vector2(slider.handleRect.offsetMax.x,compact?-1:-3);
        }
    }
    Slider CreateSlider(string objectName,Transform parent)
    {
        var root=new GameObject(objectName,typeof(RectTransform));
        root.transform.SetParent(parent,false);
        var slider=root.AddComponent<Slider>();
        var background=CivicUI.Box("Background",root.transform,new Color32(214,219,202,255),false);
        CivicUI.Fill(background.rectTransform);
        var fillArea=CivicUI.Rect("Fill Area",root.transform);
        fillArea.anchorMin=new Vector2(0,.4f); fillArea.anchorMax=new Vector2(1,.6f);
        fillArea.offsetMin=new Vector2(10,0); fillArea.offsetMax=new Vector2(-10,0);
        var fill=CivicUI.Box("Fill",fillArea,new Color32(43,119,116,255),false);
        CivicUI.Fill(fill.rectTransform);
        var handleArea=CivicUI.Rect("Handle Slide Area",root.transform);
        CivicUI.Fill(handleArea,10);
        var handle=CivicUI.Box("Handle",handleArea,new Color32(24,43,55,255),true);
        handle.rectTransform.sizeDelta=new Vector2(20,28);
        slider.fillRect=fill.rectTransform;
        slider.handleRect=handle.rectTransform;
        slider.targetGraphic=handle;
        slider.direction=Slider.Direction.LeftToRight;
        return slider;
    }
    void StyleToggle(Transform parent = null)
    {
        if(tamEkranToggle==null)return;
        if(parent != null) tamEkranToggle.transform.SetParent(parent, false);
        tamEkranToggle.gameObject.SetActive(true);
        CivicUI.Place((RectTransform)tamEkranToggle.transform,414,198,48,42);
        foreach(var text in tamEkranToggle.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);
        foreach(var text in tamEkranToggle.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
        if(tamEkranToggle.targetGraphic!=null)
        {
            var rt=tamEkranToggle.targetGraphic.rectTransform;
            CivicUI.Fill(rt,2);
            if(tamEkranToggle.targetGraphic is Image image)
            {
                image.sprite=CivicUI.GetSquareButtonSprite(CivicUI.Cream,CivicUI.CreamShadow);
                image.type=Image.Type.Sliced;
                image.color=Color.white;
            }
        }
        if(tamEkranToggle.graphic!=null)
        {
            CivicUI.Fill(tamEkranToggle.graphic.rectTransform,6);
            if(tamEkranToggle.graphic is Image image)
            {
                image.sprite=CivicUI.GetSquareButtonSprite(CivicUI.Amber,CivicUI.AmberShadow);
                image.type=Image.Type.Sliced;
                image.color=Color.white;
            }
        }
    }
    public void Ac() { gameObject.SetActive(true); }
    public void Kapat() { Ayarlar.Kaydet(); PlayerPrefs.Save(); gameObject.SetActive(false); }
}
