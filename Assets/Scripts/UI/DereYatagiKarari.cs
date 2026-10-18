using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One scheduled decision, owned by the existing game canvas. No independent event/economy state.</summary>
public sealed class DereYatagiKarari : MonoBehaviour
{
    public bool Soruldu {get;private set;}
    public bool KabulEdildi {get;private set;}
    public bool Cozuldu {get;private set;}
    public bool Acik=>panel!=null&&panel.activeSelf;
    public string GeriBildirim {get;private set;}
    GameObject panel;
    RectTransform card;
    float shownAt;
    public float GeriBildirimBitisi {get;private set;}
    void Start(){Build();}
    void Update()
    {
        var gm=GameManager.Instance;var data=AnlatiVerisi.Aktif;
        if(gm==null||data==null)return;
        if(!Soruldu&&gm.gun>=data.kararGunu&&!gm.ZamanDurduMu&&!PauseMenu.DurdurulduMu)
        {Soruldu=true;gm.oyunDurduMu=true;panel.SetActive(true);panel.transform.SetAsLastSibling();shownAt=Time.unscaledTime;}
        if(Acik&&card!=null)
            card.localScale=Vector3.one*Mathf.Min(1.4f,Mathf.Min(Screen.safeArea.width/720f,Screen.safeArea.height/460f));
    }
    public void KararVer(bool kabul)
    {
        var gm=GameManager.Instance;var data=AnlatiVerisi.Aktif;
        if(gm==null||data==null||Cozuldu||!Acik)return;
        KabulEdildi=kabul;Cozuldu=true;
        if(kabul)gm.GelirKaydet(data.kararKazanci);else gm.FarkindalikEkle(data.kararFarkindalikOdulu);
        GeriBildirim=kabul?data.kabulGeriBildirimi:data.retGeriBildirimi;GeriBildirimBitisi=Time.unscaledTime+4;
        panel.SetActive(false);gm.oyunDurduMu=false;
    }
    void Build()
    {
        var data=AnlatiVerisi.Aktif;if(data==null)return;
        panel=new GameObject("DereYatagiKarari",typeof(RectTransform),typeof(Image));panel.transform.SetParent(transform,false);
        UIManager.StretchFull((RectTransform)panel.transform);panel.GetComponent<Image>().color=new Color(.03f,.07f,.1f,.6f);
        var go=new GameObject("KararKarti",typeof(RectTransform),typeof(Image));go.transform.SetParent(panel.transform,false);
        card=(RectTransform)go.transform;card.sizeDelta=new Vector2(650,370);
        var image=go.GetComponent<Image>();image.sprite=UIManager.GetCreamBoxSprite(UIManager.CreamBg,UIManager.CreamShadow);image.type=Image.Type.Sliced;
        Text(go.transform,"Baslik",data.kararBasligi,29,new Vector2(0,125),new Vector2(580,55),true);
        Text(go.transform,"Metin",data.kararMetni,23,new Vector2(0,25),new Vector2(560,130),false);
        Button(go.transform,data.retMetni,new Vector2(-154,-113),false);
        Button(go.transform,data.kabulMetni,new Vector2(154,-113),true);
        panel.SetActive(false);
    }
    void Button(Transform parent,string text,Vector2 pos,bool kabul)
    {
        var go=new GameObject(kabul?"Kabul":"Reddet",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        var rt=(RectTransform)go.transform;rt.anchoredPosition=pos;rt.sizeDelta=new Vector2(280,62);
        var image=go.GetComponent<Image>();image.sprite=UIManager.GetCreamBoxSprite(kabul?UIManager.GoldBtn:UIManager.GreenBtn,kabul?UIManager.GoldBtnDark:UIManager.GreenBtnDark);image.type=Image.Type.Sliced;
        go.GetComponent<Button>().onClick.AddListener(()=>{if(Time.unscaledTime-shownAt>.15f)KararVer(kabul);});
        Text(go.transform,"Etiket",text,18,Vector2.zero,new Vector2(260,56),true);
    }
    static void Text(Transform parent,string name,string value,float size,Vector2 pos,Vector2 bounds,bool bold)
    {
        var t=UIManager.CreateText(parent,name,TMP_Settings.defaultFontAsset,value,size,bold?FontStyles.Bold:FontStyles.Normal,UIManager.DarkInk,TextAlignmentOptions.Center);
        t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=bounds;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=size-3;t.fontSizeMax=size;
    }
}
