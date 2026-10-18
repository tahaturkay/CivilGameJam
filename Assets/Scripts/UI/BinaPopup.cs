using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;

// Contextual building card. Its pointer shield consumes dismissal clicks; only the quiz pauses play.
public class BinaPopup : MonoBehaviour
{
    public static BinaPopup Instance;
    public GameObject panel;
    public TMP_Text baslikText;
    [FormerlySerializedAs("levelText")] public TMP_Text seviyeText;
    public TMP_Text aciklamaText;
    public Button yukseltButonu;
    public TMP_Text yukseltButonText;
    public Button quizButonu;
    public Button kapatButonu;
    [Header("Kart ölçeği (en fazla 1)")]
    [Range(.7f,1f)] public float nominalPopupScale = 1f;
    BinaTuru aktifBina;
    Transform worldAnchor;
    BuildingVisuals selectedVisual;
    RectTransform card;
    TMP_Text actionHint, quizLabel;
    float openedAt, refreshAt;
    bool styled;
    bool JustOpened { get { return Time.unscaledTime-openedAt<.2f; } }
    public bool AcikMi { get { return panel!=null && panel.activeSelf; } }
    // World click handlers can also check this when their input executes after the UI click.
    public static bool DunyaTiklamasiniEngelle { get { return blockedFrame == Time.frameCount || (Instance!=null && Instance.AcikMi); } }
    static int blockedFrame=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeStatics(){Instance=null;blockedFrame=-1;}

    void Awake()
    {
        Instance=this;
        BuildCard();
        if(panel!=null)panel.SetActive(false);
    }
    void Start()
    {
        Bind(yukseltButonu,YukseltTiklandi); Bind(quizButonu,QuizTiklandi); Bind(kapatButonu,Kapat);
    }
    void OnDestroy() { if(Instance==this)Instance=null; }
    static void Bind(Button button,UnityEngine.Events.UnityAction action)
    {
        if(button!=null && button.onClick.GetPersistentEventCount()==0)button.onClick.AddListener(action);
    }
    void Update()
    {
        if(!AcikMi)return;
        var gm=GameManager.Instance;
        if(gm==null || gm.afetSuruyor || gm.afetOlduMu || gm.oyunBittiMi || gm.oyunDurduMu || PauseMenu.DurdurulduMu) { CloseImmediately(); return; }
        if(Time.unscaledTime>=refreshAt) { Guncelle(); refreshAt=Time.unscaledTime+.2f; }
    }
    void LateUpdate() { if(AcikMi)PositionCard(); }
    void BuildCard()
    {
        if(styled || panel==null)return; styled=true;
        TMP_FontAsset font=baslikText!=null?baslikText.font:null;
        var overlay=panel.GetComponent<Image>(); if(overlay==null)overlay=panel.AddComponent<Image>();
        overlay.sprite=null; overlay.color=Color.clear; overlay.raycastTarget=true;
        CivicUI.Fill((RectTransform)panel.transform);
        var surface=panel.GetComponent<BuildingCardDismissSurface>();
        if(surface==null)surface=panel.AddComponent<BuildingCardDismissSurface>(); surface.owner=this;
        var box=CivicUI.Box("ContextBuildingCard",panel.transform,CivicUI.Cream,true); card=box.rectTransform;
        var shadow=box.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.22f); shadow.effectDistance=new Vector2(7,-7);
        card.sizeDelta=new Vector2(430,400);
        // Keep all inspector-backed controls and event bindings, replace only their visual layout.
        if(baslikText!=null)baslikText.transform.SetParent(card,false);
        if(seviyeText!=null)seviyeText.transform.SetParent(card,false);
        if(aciklamaText!=null)aciklamaText.transform.SetParent(card,false);
        if(yukseltButonu!=null)yukseltButonu.transform.SetParent(card,false);
        if(quizButonu!=null)quizButonu.transform.SetParent(card,false);
        if(kapatButonu!=null)kapatButonu.transform.SetParent(card,false);
        foreach(Transform child in panel.transform)if(child!=card)child.gameObject.SetActive(false);
        var stripe=CivicUI.Box("Accent",card,CivicUI.Teal); CivicUI.Place(stripe.rectTransform,0,0,430,6);
        if(baslikText==null)baslikText=CivicUI.Text("Title",card,"",26,CivicUI.Navy,font);
        if(seviyeText==null)seviyeText=CivicUI.Text("Level",card,"",18,CivicUI.Teal,font);
        if(aciklamaText==null)aciklamaText=CivicUI.Text("Description",card,"",20,CivicUI.Navy,font);
        StyleText(baslikText,26,CivicUI.Navy,true); CivicUI.Place(baslikText.rectTransform,24,22,330,40);
        StyleText(seviyeText,18,CivicUI.Teal,true); CivicUI.Place(seviyeText.rectTransform,24,66,382,30);
        StyleText(aciklamaText,20,CivicUI.Navy,false); aciklamaText.alignment=TextAlignmentOptions.TopLeft;
        CivicUI.Place(aciklamaText.rectTransform,24,112,382,167);
        actionHint=CivicUI.Text("ActionHint",card,"",16,CivicUI.Muted,font); CivicUI.Place(actionHint.rectTransform,24,278,382,34);
        if(yukseltButonu==null)yukseltButonu=CivicUI.Button("Upgrade",card,"",CivicUI.Teal,CivicUI.Cream,font,null);
        if(yukseltButonText==null)yukseltButonText=yukseltButonu.GetComponentInChildren<TMP_Text>(true);
        StyleButton(yukseltButonu,yukseltButonText,CivicUI.Teal,CivicUI.Cream,24,322,382,54);
        if(quizButonu==null)quizButonu=CivicUI.Button("Quiz",card,"BİRLİKTE ÖĞREN · QUIZ",CivicUI.Amber,CivicUI.Navy,font,null);
        quizLabel=quizButonu.GetComponentInChildren<TMP_Text>(true);
        StyleButton(quizButonu,quizLabel,CivicUI.Amber,CivicUI.Navy,24,388,382,48);
        if(quizLabel!=null)quizLabel.text="FARKINDALIK QUIZİ";
        if(kapatButonu==null)kapatButonu=CivicUI.Button("Close",card,"×",CivicUI.Navy,CivicUI.Cream,font,null);
        StyleButton(kapatButonu,kapatButonu.GetComponentInChildren<TMP_Text>(true),CivicUI.Navy,CivicUI.Cream,370,20,38,38);
        var closeLabel=kapatButonu.GetComponentInChildren<TMP_Text>(true); if(closeLabel!=null)closeLabel.text="×";
    }
    static void StyleText(TMP_Text t,float size,Color color,bool bold)
    {
        var le=t.GetComponent<LayoutElement>(); if(le!=null)le.ignoreLayout=true;
        t.fontSize=size; t.enableAutoSizing=false; t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
        t.color=color; t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Overflow;
        t.alignment=TextAlignmentOptions.MidlineLeft; t.raycastTarget=false;
    }
    static void StyleButton(Button button,TMP_Text label,Color fill,Color ink,float x,float y,float w,float h)
    {
        var le=button.GetComponent<LayoutElement>(); if(le!=null)le.ignoreLayout=true;
        CivicUI.Place((RectTransform)button.transform,x,y,w,h);
        var img=button.GetComponent<Image>(); if(img!=null){img.sprite=null;img.color=fill;button.targetGraphic=img;}
        foreach(Transform child in button.transform)if(label==null || child!=label.transform)child.gameObject.SetActive(false);
        if(label!=null){StyleText(label,20,ink,true);label.alignment=TextAlignmentOptions.Center;CivicUI.Fill(label.rectTransform,8);}
    }
    public void Ac(BinaTuru bina) { Ac(bina,null); }
    public void Ac(BinaTuru bina,Transform anchor)
    {
        var gm=GameManager.Instance;
        if(gm==null || gm.afetSuruyor || gm.afetOlduMu || gm.oyunBittiMi || gm.oyunDurduMu || PauseMenu.DurdurulduMu)return;
        BuildCard(); if(panel==null)return;
        aktifBina=bina; worldAnchor=anchor; selectedVisual=anchor!=null?anchor.GetComponentInParent<BuildingVisuals>():null; openedAt=Time.unscaledTime; blockedFrame=Time.frameCount;
        Guncelle(); panel.SetActive(true); panel.transform.SetAsLastSibling(); PositionCard();
    }
    void PositionCard()
    {
        if(card==null)return;
        var parent=card.parent as RectTransform; if(parent==null)return;
        Canvas canvas=panel.GetComponentInParent<Canvas>();
        Camera uiCamera=canvas!=null && canvas.rootCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.rootCanvas.worldCamera:null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Screen.safeArea.min,uiCamera,out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Screen.safeArea.max,uiCamera,out Vector2 max);
        float scale=Mathf.Min(Mathf.Clamp(nominalPopupScale,.7f,1f)*CivicUI.ReadableScale(this),Mathf.Min((max.x-min.x-40)/card.sizeDelta.x,(max.y-min.y-160)/card.sizeDelta.y));
        scale=Mathf.Max(.1f,scale); card.localScale=Vector3.one*scale;
        card.anchorMin=card.anchorMax=card.pivot=Vector2.one*.5f;
        Vector2 half=card.sizeDelta*scale*.5f;
        Vector2 position=new Vector2(max.x-half.x-24,(min.y+max.y)*.5f);
        if(worldAnchor!=null && Camera.main!=null)
        {
            Vector3 screen=Camera.main.WorldToScreenPoint(worldAnchor.position);
            if(screen.z<=0){CloseImmediately();return;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,uiCamera,out Vector2 local);
            // Place beside the structure, leaving the actual upgrade animation unobscured.
            bool right=local.x+half.x*2+66<max.x;
            position=local+new Vector2(right?half.x+66:-half.x-66,half.y*.18f);
        }
        position.x=Mathf.Clamp(position.x,min.x+half.x+20,max.x-half.x-20);
        position.y=Mathf.Clamp(position.y,min.y+half.y+30,max.y-half.y-110);
        card.anchoredPosition=position;
    }
    public void Kapat() { if(!JustOpened)CloseImmediately(); }
    public void DisariTiklandi() { if(!JustOpened){blockedFrame=Time.frameCount;CloseImmediately();} }
    void CloseImmediately()
    {
        if(panel!=null)panel.SetActive(false);
        worldAnchor=null; selectedVisual=null;
        // This card does not own the game's pause flag.
    }
    void Guncelle()
    {
        var gm=GameManager.Instance; if(gm==null)return;
        var d=DengeAyarlari.Aktif; int level=gm.Level(aktifBina); bool damaged=gm.Hasarli(aktifBina);
        bool school=aktifBina==BinaTuru.Okul && !damaged;
        card.sizeDelta=new Vector2(430,school?460:400);
        bool selectedDamaged=selectedVisual!=null?selectedVisual.IsDamaged:damaged;
        string condition=selectedDamaged?(selectedVisual!=null?AfetAdi(selectedVisual.DamageCause)+" HASARI":"HASARLI"):"BU YAPI SAĞLAM";
        seviyeText.text="SEVİYE "+level+" / "+d.maxLevel+"   ·   "+TurkishTMPFontSupport.TurkceBuyukHarf(condition);
        seviyeText.color=selectedDamaged?new Color32(165,68,48,255):CivicUI.Teal;
        BinaBilgileriniDoldur(aktifBina,level,damaged);
        int cost=damaged?Ekonomi.OnarimMaliyeti(aktifBina):Ekonomi.YukseltmeMaliyeti(aktifBina);
        bool max=!damaged && level>=d.maxLevel;
        yukseltButonText.text=max?"EN YÜKSEK SEVİYE":(damaged?"ONAR":"YÜKSELT")+"  ·  "+cost+" COIN";
        yukseltButonu.interactable=damaged?Ekonomi.OnarilabilirMi(aktifBina):Ekonomi.YukseltilebilirMi(aktifBina);
        actionHint.text=max?"Bu yapı tamamen geliştirildi.":gm.para<cost?(cost-gm.para)+" coin daha gerekli. Günlük geliri bekleyebilirsin.":damaged?"Onar ve yapıyı yeniden göreve döndür.":"Bir sonraki seviye için hazır.";
        if(damaged && gm.para>=cost)actionHint.text=selectedDamaged?"Onarım, bu gruptaki hasarlı yapıları iyileştirir.":"Bu yapı sağlam. Onarım gruptaki hasarlı yapılar içindir.";
        if(quizButonu!=null)quizButonu.gameObject.SetActive(school);
    }
    void BinaBilgileriniDoldur(BinaTuru bina,int level,bool damaged)
    {
        var gm=GameManager.Instance; var d=DengeAyarlari.Aktif; string text;
        switch(bina)
        {
            case BinaTuru.TemelBina:
                baslikText.text="KONUTLAR";
                text="Şehrin evi ve gelir kaynağı.\nNüfus: "+gm.nufus+" / "+Ekonomi.NufusKapasitesi()+" kişi\nGünlük gelir: +"+Ekonomi.GunlukGelir()+" coin\n";
                text+=level<d.maxLevel?"Sonraki: "+d.KonutKapasitesi(level+1)+" kişi · gelir ×"+HousingMultiplier(level+1).ToString("0.##"):"Kapasite tamamen geliştirildi.";
                text+=damaged?"\nGruptaki hasar geliri yarıya düşürüyor.":"\n"+Defense(AfetTuru.Deprem,level,damaged); break;
            case BinaTuru.Okul:
                baslikText.text="OKUL";
                text="Bilgi paylaş, afetlere birlikte hazırlan.\nFarkındalık: %"+Mathf.RoundToInt(gm.farkindalikYuzdesi)+" / %"+Mathf.RoundToInt(d.FarkindalikTavani(level))+"\n";
                text+=damaged?"Quiz için önce okulu onar.":"Quiz ile farkındalığı artır.\n"+Defense(AfetTuru.Deprem,level,damaged); break;
            case BinaTuru.Akut:
                baslikText.text="AKUT MERKEZİ";
                text="Arama ve kurtarma ekibini güçlendir.\nCan kaybını azaltma: %"+Mathf.RoundToInt((damaged?0:level)*d.akutEtkisi*100)+"\nTüm afetlerde katkı sağlar.\n"+(damaged?"Ekibi göreve döndürmek için onar.":Defense(AfetTuru.Deprem,level,damaged)); break;
            case BinaTuru.Itfaiye:
                baslikText.text="İTFAİYE"; text="Yangına hazırlık mahallede başlar.\n"+Defense(AfetTuru.Yangin,level,damaged)+"\n"+DefenseBenefit(AfetTuru.Yangin,level,damaged); break;
            case BinaTuru.DenizSeti:
                baslikText.text="DENİZ SETİ"; text="Kıyıdaki yaşamı dalgalara karşı koru.\n"+Defense(AfetTuru.Tsunami,level,damaged)+"\n"+DefenseBenefit(AfetTuru.Tsunami,level,damaged); break;
            default:
                baslikText.text="ALTYAPI"; text="Yağmur suyuna güvenli bir yol aç.\n"+Defense(AfetTuru.Sel,level,damaged)+"\n"+DefenseBenefit(AfetTuru.Sel,level,damaged); break;
        }
        aciklamaText.text=text;
    }
    static string AfetAdi(AfetTuru type)
    {
        switch(type)
        {
            case AfetTuru.Yangin:return "Yangın";
            case AfetTuru.Sel:return "Sel";
            case AfetTuru.Deprem:return "Deprem";
            default:return "Tsunami";
        }
    }
    float HousingMultiplier(int level)
    {
        var values=DengeAyarlari.Aktif.konutGelirCarpanlari;
        return values!=null && values.Length>0?values[Mathf.Clamp(level-1,0,values.Length-1)]:1;
    }
    int Required(AfetTuru type)
    {
        foreach(var a in DengeAyarlari.Aktif.afetler)if(a.afet==type)return a.gerekenLevel;
        return DengeAyarlari.Aktif.maxLevel;
    }
    string Defense(AfetTuru type,int level,bool damaged)
    {
        int required=Required(type);
        return AfetAdi(type)+" eşiği: Seviye "+required+"\nBu yapı: "+(damaged?"onarım bekliyor":level>=required?"hazır":"güçlendirme gerekli");
    }
    string DefenseBenefit(AfetTuru type,int level,bool damaged)
    {
        if(damaged)return "Savunma için önce hasarı onar.";
        int reduction=Mathf.RoundToInt((1-DengeAyarlari.Aktif.savunmaYeterliCarpani)*100);
        return level>=Required(type)?"Savunma hazır: baz can kaybı %"+reduction+" azalır.":"Eşiğe ulaşınca baz can kaybı %"+reduction+" azalır.";
    }
    public void YukseltTiklandi()
    {
        var gm=GameManager.Instance; if(gm==null || JustOpened)return;
        if(gm.Hasarli(aktifBina)?Ekonomi.Onar(aktifBina):Ekonomi.Yukselt(aktifBina))Guncelle();
    }
    public void QuizTiklandi()
    {
        if(JustOpened)return;
        CloseImmediately(); if(QuizManager.Instance!=null)QuizManager.Instance.QuizAc();
    }
}
