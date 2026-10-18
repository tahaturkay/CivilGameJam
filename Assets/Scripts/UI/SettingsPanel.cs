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
        if (hudScaleSlider != null) { hudScaleSlider.minValue=.75f; hudScaleSlider.maxValue=1.25f; hudScaleSlider.onValueChanged.AddListener(SetHudScale); }
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
    void Fit() { if(card != null) CivicUI.FitCard(card,new Vector2(1100,690),32,CivicUI.ReadableScale(this)); }
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
        overlay.sprite=null; overlay.color=new Color32(10,24,32,210); overlay.raycastTarget=true;
        CivicUI.Fill((RectTransform)transform);
        var box=CivicUI.Box("CivicSettingsCard",transform,CivicUI.Cream,true); card=box.rectTransform;
        var shadow=box.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.22f); shadow.effectDistance=new Vector2(7,-7);
        // Move live controls before hiding the previous prefab layout.
        if(muzikSlider != null) muzikSlider.transform.SetParent(card,false);
        if(efektSlider != null) efektSlider.transform.SetParent(card,false);
        if(tamEkranToggle != null) tamEkranToggle.transform.SetParent(card,false);
        foreach(Transform child in transform) if(child != card) child.gameObject.SetActive(false);
        var accent=CivicUI.Box("Accent",card,CivicUI.Teal); CivicUI.Place(accent.rectTransform,0,0,1100,8);
        var title=CivicUI.Text("Title",card,"AYARLAR & KONTROLLER",34,CivicUI.Navy,font);
        title.fontStyle=FontStyles.Bold; CivicUI.Place(title.rectTransform,40,34,970,50);
        var subtitle=CivicUI.Text("Subtitle",card,"Şehrine kendi hızında göz kulak ol.",20,CivicUI.Muted,font); CivicUI.Place(subtitle.rectTransform,40,88,960,32);
        var divider=CivicUI.Box("Divider",card,new Color32(210,215,199,255)); CivicUI.Place(divider.rectTransform,486,150,2,500);
        Label(card,"SES & GÖRÜNTÜ",40,148,400,30,20,CivicUI.Teal,font,true);
        Label(card,"Müzik",40,209,270,32,23,CivicUI.Navy,font);
        musicValue=Label(card,"",350,209,90,32,21,CivicUI.Muted,font);
        StyleSlider(muzikSlider,40,260,390);
        Label(card,"Efektler",40,330,270,32,23,CivicUI.Navy,font);
        effectsValue=Label(card,"",350,330,90,32,21,CivicUI.Muted,font);
        StyleSlider(efektSlider,40,382,390);
        Label(card,"Tam ekran",40,461,290,38,23,CivicUI.Navy,font);
        StyleToggle();
        Label(card,"ÜST PANEL",40,510,250,26,18,CivicUI.Teal,font,true);
        Label(card,"Panel boyutu",40,536,250,22,18,CivicUI.Navy,font);
        hudScaleValue=Label(card,"",350,536,90,22,17,CivicUI.Muted,font);
        hudScaleSlider=CreateSlider("TopHudScaleSlider",card);
        StyleSlider(hudScaleSlider,40,559,390,28);
        Label(card,"Üst kenar boşluğu",40,588,250,22,18,CivicUI.Navy,font);
        hudOffsetValue=Label(card,"",350,588,90,22,17,CivicUI.Muted,font);
        hudOffsetSlider=CreateSlider("TopHudOffsetSlider",card);
        StyleSlider(hudOffsetSlider,40,611,390,28);
        Label(card,"KAMERA & OYUN",530,148,500,30,20,CivicUI.Teal,font,true);
        Guide("W A S D / OK TUŞLARI","Haritada gezin",208,font);
        Guide("SAĞ / ORTA TUŞ","Basılı tutup sürükle",266,font);
        Guide("SPACE + SOL TUŞ","Basılı tutup sürükle",324,font);
        Guide("TEKERLEK / İKİ PARMAK ↑↓","Yakınlaş / uzaklaş",382,font);
        Guide("SHIFT + KAYDIRMA","Haritayı kaydır",440,font);
        Guide("Q / E   ·   HOME","Yakınlık   ·   Şehri ortala",498,font);
        Guide("ESC","Duraklat / devam et",556,font);
        var done=CivicUI.Button("Done",card,"TAMAM",CivicUI.Teal,CivicUI.Cream,font,Kapat); CivicUI.Place((RectTransform)done.transform,40,646,390,38);
        var reset=CivicUI.Button("ResetHud",card,"ÜST PANELİ SIFIRLA",CivicUI.Navy,CivicUI.Cream,font,ResetHudSettings); CivicUI.Place((RectTransform)reset.transform,530,646,510,38);
    }
    TMP_Text Label(Transform p,string value,float x,float y,float w,float h,float s,Color c,TMP_FontAsset f,bool bold=false)
    {
        var t=CivicUI.Text("Label",p,value,s,c,f); if(bold)t.fontStyle=FontStyles.Bold; CivicUI.Place(t.rectTransform,x,y,w,h); return t;
    }
    void Guide(string key,string action,float y,TMP_FontAsset font)
    {
        Label(card,key,530,y,525,25,18,CivicUI.Navy,font,true);
        Label(card,action,530,y+25,525,24,18,CivicUI.Muted,font);
    }
    void StyleSlider(Slider slider,float x,float y,float width,float height=40)
    {
        if(slider==null)return;
        slider.gameObject.SetActive(true);
        CivicUI.Place((RectTransform)slider.transform,x,y,width,height);
        var layout=slider.GetComponent<LayoutElement>(); if(layout!=null)layout.ignoreLayout=true;
        foreach(var image in slider.GetComponentsInChildren<Image>(true))
        {
            image.sprite=null;
            bool handle=image.transform==slider.handleRect;
            image.color=handle ? CivicUI.Navy : image.transform==slider.fillRect ? CivicUI.Teal : new Color32(214,219,202,255);
            image.raycastTarget=handle;
        }
        var background=slider.transform.Find("Background") as RectTransform;
        if(background!=null) { background.anchorMin=new Vector2(0,.4f);background.anchorMax=new Vector2(1,.6f);background.offsetMin=background.offsetMax=Vector2.zero; }
        if(slider.fillRect!=null)
        {
            var area=slider.fillRect.parent as RectTransform;
            if(area!=null){area.anchorMin=new Vector2(0,.4f);area.anchorMax=new Vector2(1,.6f);area.offsetMin=area.offsetMax=Vector2.zero;}
        }
        if(slider.handleRect!=null)
        {
            var area=slider.handleRect.parent as RectTransform;
            if(area!=null)CivicUI.Fill(area);
            bool compact=height<32;
            slider.handleRect.sizeDelta=new Vector2(20,compact?22:28);
            slider.handleRect.offsetMin=new Vector2(slider.handleRect.offsetMin.x,compact?3:6);
            slider.handleRect.offsetMax=new Vector2(slider.handleRect.offsetMax.x,compact?-3:-6);
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
    void StyleToggle()
    {
        if(tamEkranToggle==null)return;
        tamEkranToggle.gameObject.SetActive(true); CivicUI.Place((RectTransform)tamEkranToggle.transform,370,458,48,44);
        foreach(var text in tamEkranToggle.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);
        foreach(var text in tamEkranToggle.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
        if(tamEkranToggle.targetGraphic!=null)
        {
            var rt=tamEkranToggle.targetGraphic.rectTransform; CivicUI.Fill(rt,3);
            tamEkranToggle.targetGraphic.color=CivicUI.Navy;
            if(tamEkranToggle.targetGraphic is Image image)image.sprite=null;
        }
        if(tamEkranToggle.graphic!=null)
        {
            CivicUI.Fill(tamEkranToggle.graphic.rectTransform,8); tamEkranToggle.graphic.color=CivicUI.Amber;
            if(tamEkranToggle.graphic is Image image)image.sprite=null;
        }
    }
    public void Ac() { gameObject.SetActive(true); }
    public void Kapat() { Ayarlar.Kaydet(); PlayerPrefs.Save(); gameObject.SetActive(false); }
}
