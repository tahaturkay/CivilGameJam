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
    RectTransform infoCard;
    TMP_Text actionHint, quizLabel;
    string measuredDescription, measuredActionHint;
    bool measuredSchool, hasMeasuredLayout;
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
    RectTransform ribbonRt;
    TMP_Text ribbonTitleText;
    Image statusPillImg;
    Image yukseltIconImg;
    Image quizIconImg;

    void BuildCard()
    {
        if(styled || panel==null)return; styled=true;
        TMP_FontAsset font=baslikText!=null?baslikText.font:null;
        var overlay=panel.GetComponent<Image>(); if(overlay==null)overlay=panel.AddComponent<Image>();
        overlay.sprite=null; overlay.color=new Color32(10, 18, 28, 140); overlay.raycastTarget=true;
        CivicUI.Fill((RectTransform)panel.transform);
        var surface=panel.GetComponent<BuildingCardDismissSurface>();
        if(surface==null)surface=panel.AddComponent<BuildingCardDismissSurface>(); surface.owner=this;

        // Arcade Beveled Mint Card
        var box=CivicUI.Box("ContextBuildingCard",panel.transform,CivicUI.Mint,true); card=box.rectTransform;
        var boxImg=box.GetComponent<Image>();
        boxImg.sprite=CivicUI.GetBeveledCardSprite(CivicUI.Mint, CivicUI.MintShadow);
        boxImg.type=Image.Type.Sliced;
        boxImg.color=Color.white;

        var shadow=box.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.26f); shadow.effectDistance=new Vector2(8,-8);
        card.sizeDelta=new Vector2(460,440);

        // Overlapping Ribbon Banner
        ribbonRt = CivicUI.CreateRibbon(card, "BİNA DETAYI", CivicUI.RibbonBlue, CivicUI.RibbonBlueLip, CivicUI.RibbonBlueTail, font, 60f, 36f);
        ribbonTitleText = ribbonRt.GetComponentInChildren<TMP_Text>();

        // Keep all inspector-backed controls and event bindings, replace only their visual layout.
        if(baslikText!=null)baslikText.transform.SetParent(card,false);
        if(seviyeText!=null)seviyeText.transform.SetParent(card,false);
        if(aciklamaText!=null)aciklamaText.transform.SetParent(card,false);
        if(yukseltButonu!=null)yukseltButonu.transform.SetParent(card,false);
        if(quizButonu!=null)quizButonu.transform.SetParent(card,false);
        if(kapatButonu!=null)kapatButonu.transform.SetParent(card,false);
        foreach(Transform child in panel.transform)if(child!=card)child.gameObject.SetActive(false);

        if(baslikText!=null)baslikText.gameObject.SetActive(false); // Title is now displayed on the gorgeous top ribbon!

        // Status Capsule Pill Badge
        var pillObj = new GameObject("StatusPillBadge", typeof(RectTransform), typeof(Image));
        pillObj.transform.SetParent(card, false);
        var pillRt = pillObj.GetComponent<RectTransform>();
        CivicUI.Place(pillRt, 24f, 74f, 412f, 32f);
        statusPillImg = pillObj.GetComponent<Image>();
        statusPillImg.sprite = CivicUI.GetPillBadgeSprite(CivicUI.GreenBtn, CivicUI.GreenBtnDark);
        statusPillImg.type = Image.Type.Sliced;
        statusPillImg.color = Color.white;

        if(seviyeText==null)seviyeText=CivicUI.Text("Level",pillRt,"",16,Color.white,font);
        else seviyeText.transform.SetParent(pillRt,false);
        StyleText(seviyeText,16,Color.white,true);
        seviyeText.alignment=TextAlignmentOptions.Center;
        CivicUI.Fill(seviyeText.rectTransform, 4f);

        // Inner Cream Info Card
        var innerCard = new GameObject("InnerInfoCard", typeof(RectTransform), typeof(Image));
        innerCard.transform.SetParent(card, false);
        var icRt = innerCard.GetComponent<RectTransform>();
        CivicUI.Place(icRt, 24f, 114f, 412f, 168f);
        infoCard = icRt;
        var icImg = innerCard.GetComponent<Image>();
        icImg.sprite = CivicUI.GetBeveledCardSprite(CivicUI.Cream, CivicUI.CreamShadow);
        icImg.type = Image.Type.Sliced;
        icImg.color = Color.white;

        if(aciklamaText==null)aciklamaText=CivicUI.Text("Description",innerCard.transform,"",18,CivicUI.DarkInk,font);
        else aciklamaText.transform.SetParent(innerCard.transform,false);
        StyleText(aciklamaText,18,CivicUI.DarkInk,false);
        aciklamaText.alignment=TextAlignmentOptions.TopLeft;
        CivicUI.Place(aciklamaText.rectTransform, 14f, 12f, 384f, 110f);

        actionHint=CivicUI.Text("ActionHint",innerCard.transform,"",15,CivicUI.Muted,font);
        actionHint.fontStyle=FontStyles.Bold;
        CivicUI.Place(actionHint.rectTransform, 14f, 126f, 384f, 34f);

        // Buttons
        if(yukseltButonu==null)yukseltButonu=CivicUI.Button("Upgrade",card,"",CivicUI.GoldBtn,CivicUI.DarkInk,font,null);
        if(yukseltButonText==null)yukseltButonText=yukseltButonu.GetComponentInChildren<TMP_Text>(true);
        StyleArcadeButton(yukseltButonu, yukseltButonText, CivicUI.GoldBtn, CivicUI.GoldBtnLeft, CivicUI.GoldBtnDark, CivicUI.IconType.UpArrow, 24f, 294f, 412f, 58f, out yukseltIconImg);

        if(quizButonu==null)quizButonu=CivicUI.Button("Quiz",card,"BİRLİKTE ÖĞREN · QUIZ",CivicUI.PurpleBtn,Color.white,font,null);
        quizLabel=quizButonu.GetComponentInChildren<TMP_Text>(true);
        StyleArcadeButton(quizButonu, quizLabel, CivicUI.PurpleBtn, CivicUI.PurpleBtnLeft, CivicUI.PurpleBtnDark, CivicUI.IconType.Book, 24f, 360f, 412f, 54f, out quizIconImg);
        if(quizLabel!=null)quizLabel.text="FARKINDALIK QUIZİ";

        if(kapatButonu==null)kapatButonu=CivicUI.Button("Close",card,"×",CivicUI.CoralBtn,Color.white,font,null);
        StyleCloseButton(kapatButonu, 404f, 12f, 42f, 42f);
    }
    static void StyleText(TMP_Text t,float size,Color color,bool bold)
    {
        var le=t.GetComponent<LayoutElement>(); if(le!=null)le.ignoreLayout=true;
        t.fontSize=size; t.enableAutoSizing=false; t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
        t.color=color; t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Overflow;
        t.alignment=TextAlignmentOptions.MidlineLeft; t.raycastTarget=false;
    }
    static void StyleArcadeButton(Button button, TMP_Text label, Color32 fill, Color32 leftFill, Color32 shadow, CivicUI.IconType icon, float x, float y, float w, float h, out Image iconImg)
    {
        var le=button.GetComponent<LayoutElement>(); if(le!=null)le.ignoreLayout=true;
        CivicUI.Place((RectTransform)button.transform, x, y, w, h);
        var img=button.GetComponent<Image>();
        if(img!=null)
        {
            img.sprite=CivicUI.GetTwoToneButtonSprite(fill, leftFill, shadow);
            img.type=Image.Type.Sliced;
            img.color=Color.white;
            button.targetGraphic=img;
        }

        GameObject iconObj = button.transform.Find("LeftIcon")?.gameObject;
        if(iconObj==null)
        {
            iconObj = new GameObject("LeftIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(button.transform, false);
        }
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(26f, 2f);
        iconRt.sizeDelta = new Vector2(32f, 32f);

        iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = CivicUI.GetColoredIconSprite(icon, Color.white);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        foreach(Transform child in button.transform)
            if(child!=iconObj.transform && (label==null || child!=label.transform)) child.gameObject.SetActive(false);

        if(label!=null)
        {
            StyleText(label, 20f, Color.white, true);
            label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(56f, 5f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
        }
    }
    static void StyleCloseButton(Button button, float x, float y, float w, float h)
    {
        var le=button.GetComponent<LayoutElement>(); if(le!=null)le.ignoreLayout=true;
        CivicUI.Place((RectTransform)button.transform, x, y, w, h);
        var img=button.GetComponent<Image>();
        if(img!=null)
        {
            img.sprite=CivicUI.GetSquareButtonSprite(CivicUI.CoralBtn, CivicUI.CoralBtnDark);
            img.type=Image.Type.Sliced;
            img.color=Color.white;
            button.targetGraphic=img;
        }
        var label=button.GetComponentInChildren<TMP_Text>(true);
        if(label!=null)
        {
            StyleText(label, 22f, Color.white, true);
            label.alignment=TextAlignmentOptions.Center;
            CivicUI.Fill(label.rectTransform, 4f);
            label.text="×";
        }
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
        bool selectedDamaged=selectedVisual!=null?selectedVisual.IsDamaged:damaged;
        string condition=selectedDamaged?(selectedVisual!=null?AfetAdi(selectedVisual.DamageCause)+" HASARI":"HASARLI"):"BU YAPI SAĞLAM";
        seviyeText.text="SEVİYE "+level+" / "+d.maxLevel+"   ·   "+TurkishTMPFontSupport.TurkceBuyukHarf(condition);
        if(statusPillImg!=null)
        {
            statusPillImg.sprite=selectedDamaged
                ? CivicUI.GetPillBadgeSprite(CivicUI.CoralBtn, CivicUI.CoralBtnDark)
                : CivicUI.GetPillBadgeSprite(CivicUI.GreenBtn, CivicUI.GreenBtnDark);
        }
        BinaBilgileriniDoldur(aktifBina,level,damaged);
        if(ribbonTitleText!=null && baslikText!=null)
            ribbonTitleText.text=baslikText.text;

        int cost=damaged?Ekonomi.OnarimMaliyeti(aktifBina):Ekonomi.YukseltmeMaliyeti(aktifBina);
        bool max=!damaged && level>=d.maxLevel;
        yukseltButonText.text=max?"EN YÜKSEK SEVİYE":(damaged?"ONAR":"YÜKSELT")+"  ·  "+cost+" COIN";
        yukseltButonu.interactable=damaged?Ekonomi.OnarilabilirMi(aktifBina):Ekonomi.YukseltilebilirMi(aktifBina);

        // Update Button Sprite according to status
        Image yImg = yukseltButonu.GetComponent<Image>();
        if(yImg!=null)
        {
            if(damaged) yImg.sprite = CivicUI.GetTwoToneButtonSprite(CivicUI.CoralBtn, CivicUI.CoralBtnLeft, CivicUI.CoralBtnDark);
            else if(max) yImg.sprite = CivicUI.GetSquareButtonSprite(CivicUI.SlateBtn, CivicUI.SlateBtnDark);
            else yImg.sprite = CivicUI.GetTwoToneButtonSprite(CivicUI.GoldBtn, CivicUI.GoldBtnLeft, CivicUI.GoldBtnDark);
        }

        actionHint.text=max?"TAMAMEN GELİŞTİRİLDİ":gm.para<cost?(cost-gm.para)+" coin daha gerekli. Günlük geliri bekleyebilirsin.":damaged?"Onar ve yapıyı yeniden göreve döndür.":"Bir sonraki seviye için hazır.";
        if(max)
        {
            actionHint.font=TurkishTMPFontSupport.DisplayFont;
            actionHint.fontSize=15f;
            actionHint.enableAutoSizing=true;
            actionHint.fontSizeMin=12f;
            actionHint.fontSizeMax=15f;
            actionHint.color=CivicUI.GreenBtnDark;
            actionHint.alignment=TextAlignmentOptions.Center;
        }
        else
        {
            actionHint.font=TurkishTMPFontSupport.PreferredFont;
            actionHint.enableAutoSizing=false;
            actionHint.fontSize=15f;
            actionHint.color=CivicUI.Muted;
            actionHint.alignment=TextAlignmentOptions.MidlineLeft;
        }
        if(damaged && gm.para>=cost)actionHint.text=selectedDamaged?"Onarım, bu gruptaki hasarlı yapıları iyileştirir.":"Bu yapı sağlam. Onarım gruptaki hasarlı yapılar içindir.";
        FitDescriptionAndActions(school);
        if(quizButonu!=null)quizButonu.gameObject.SetActive(school);
    }

    void FitDescriptionAndActions(bool school)
    {
        if(infoCard==null||aciklamaText==null||actionHint==null)return;
        if(hasMeasuredLayout&&measuredSchool==school&&measuredDescription==aciklamaText.text&&measuredActionHint==actionHint.text)return;

        const float textWidth=384f;
        const float innerTop=10f;
        const float gap=6f;
        const float innerBottom=8f;
        const float infoTop=114f;
        const float buttonGap=12f;

        aciklamaText.textWrappingMode=TextWrappingModes.Normal;
        aciklamaText.overflowMode=TextOverflowModes.Overflow;
        aciklamaText.enableAutoSizing=true;
        aciklamaText.fontSizeMin=14f;
        aciklamaText.fontSizeMax=18f;
        aciklamaText.rectTransform.sizeDelta=new Vector2(textWidth,400f);
        aciklamaText.ForceMeshUpdate();
        float descriptionHeight=Mathf.Clamp(aciklamaText.GetPreferredValues(aciklamaText.text,textWidth,0f).y+4f,110f,184f);

        actionHint.textWrappingMode=TextWrappingModes.Normal;
        actionHint.overflowMode=TextOverflowModes.Overflow;
        actionHint.enableAutoSizing=true;
        actionHint.fontSizeMin=11f;
        actionHint.fontSizeMax=14f;
        actionHint.rectTransform.sizeDelta=new Vector2(textWidth,120f);
        actionHint.ForceMeshUpdate();
        float hintHeight=Mathf.Clamp(actionHint.GetPreferredValues(actionHint.text,textWidth,0f).y+4f,34f,76f);

        float innerHeight=Mathf.Max(168f,innerTop+descriptionHeight+gap+hintHeight+innerBottom);
        CivicUI.Place(infoCard,24f,infoTop,412f,innerHeight);
        CivicUI.Place(aciklamaText.rectTransform,14f,innerTop,textWidth,descriptionHeight);
        CivicUI.Place(actionHint.rectTransform,14f,innerTop+descriptionHeight+gap,textWidth,hintHeight);

        float upgradeY=infoTop+innerHeight+buttonGap;
        CivicUI.Place((RectTransform)yukseltButonu.transform,24f,upgradeY,412f,58f);
        float quizY=upgradeY+58f+8f;
        CivicUI.Place((RectTransform)quizButonu.transform,24f,quizY,412f,54f);
        float contentBottom=school?quizY+54f+20f:upgradeY+58f+20f;
        card.sizeDelta=new Vector2(460f,Mathf.Max(school?490f:425f,contentBottom));
        measuredSchool=school;
        measuredDescription=aciklamaText.text;
        measuredActionHint=actionHint.text;
        hasMeasuredLayout=true;
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
                baslikText.text="ODTÜ";
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
        return AfetAdi(type)+" eşiği: Seviye "+required+"\nSavunma seviyesi: "+(damaged?"onarım bekliyor":level>=required?"yeterli":"güçlendirme gerekli");
    }
    string DefenseBenefit(AfetTuru type,int level,bool damaged)
    {
        if(damaged)return "Savunma için önce hasarı onar.";
        int reduction=Mathf.RoundToInt((1-DengeAyarlari.Aktif.savunmaYeterliCarpani)*100);
        return level>=Required(type)?"Bu seviye baz can kaybını %"+reduction+" azaltır.":"Eşiğe ulaşınca baz can kaybı %"+reduction+" azalır.";
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
