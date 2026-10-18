using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>One quiet, non-blocking hint at a time. Disaster guidance outranks general teaching.</summary>
public class IpucuSistemi : MonoBehaviour
{
    public GameObject serit;
    public TMP_Text ipucuText;
    public float enFazlaSure=6f;
    readonly HashSet<int> shown=new HashSet<int>();
    int active=-1,completed;
    float elapsed,nextAt;
    public int SiradakiIndex=>completed;
    public AfetTuru? CurrentDisasterHint {get;private set;}
    public string VisibleText=>serit!=null&&serit.activeSelf?ipucuText.text:"";
    void Awake(){if(serit!=null)serit.SetActive(false);}
    void Start()
    {
        if(serit==null||ipucuText==null)return;
        ipucuText.fontSize=20;ipucuText.fontSizeMax=20;ipucuText.fontSizeMin=16;ipucuText.enableAutoSizing=true;
        ipucuText.textWrappingMode=TextWrappingModes.Normal;ipucuText.fontStyle=FontStyles.Normal;
        ipucuText.alignment=TextAlignmentOptions.Center;ipucuText.richText=true;
        CivicUI.Fill(ipucuText.rectTransform,14);
    }
    void Update()
    {
        var gm=GameManager.Instance;var data=AnlatiVerisi.Aktif;
        if(gm==null||data==null||serit==null||ipucuText==null)return;
        bool blocked=gm.oyunDurduMu||gm.afetSuruyor||gm.afetOlduMu||gm.oyunBittiMi||PauseMenu.DurdurulduMu;
        if(blocked){serit.SetActive(false);return;}
        var decision=GetComponent<DereYatagiKarari>();
        if(decision!=null&&Time.unscaledTime<decision.GeriBildirimBitisi)
        {Show(decision.GeriBildirim);return;}
        if(active>=0&&active<10)
        {
            var disasters=DengeAyarlari.Aktif.afetler;
            for(int i=0;i<disasters.Length;i++)
            {
                int gap=disasters[i].gun-gm.gun,key=10+i;
                if(gap>=1&&gap<=3&&!shown.Contains(key)&&!Completed(gm,key))
                {active=key;elapsed=0;nextAt=0;break;}
            }
        }
        if(Time.unscaledTime<nextAt){serit.SetActive(false);return;}
        if(active<0)Select(gm,data);
        if(active<0){serit.SetActive(false);return;}
        if(Completed(gm,active)) {Finish();return;}
        if(active>=10)
        {
            var setting=DengeAyarlari.Aktif.afetler[active-10];
            if(gm.gun>=setting.gun){Finish();return;}
            CurrentDisasterHint=setting.afet;Show(data.AfetIpucu(setting.afet));
        }
        else {CurrentDisasterHint=null;Show(active==0?data.ilkIpucu:active==1?data.okulIpucu+"\n<i>"+data.okulDuygu+"</i>":data.onarimIpucu);}
        elapsed+=Time.unscaledDeltaTime;if(elapsed>=enFazlaSure)Finish();
    }
    void Select(GameManager gm,AnlatiVerisi data)
    {
        var disasters=DengeAyarlari.Aktif.afetler;
        for(int i=0;i<disasters.Length;i++)
        {
            int gap=disasters[i].gun-gm.gun,key=10+i;
            if(gap>=1&&gap<=3&&!shown.Contains(key)&&!Completed(gm,key)){active=key;return;}
        }
        if(!shown.Contains(0)&&gm.yukseltmeSayisi==0&&gm.gun<4){active=0;return;}
        if(!shown.Contains(1)&&gm.dogruCevapSayisi==0&&(gm.gun>=2||gm.yukseltmeSayisi>0)){active=1;return;}
        if(!shown.Contains(2)&&HasarliVarMi(gm)){active=2;return;}
    }
    bool Completed(GameManager gm,int key)
    {
        if(key==0)return gm.yukseltmeSayisi>0;
        if(key==1)return gm.dogruCevapSayisi>0;
        if(key==2)return !HasarliVarMi(gm);
        var a=DengeAyarlari.Aktif.afetler[key-10];
        foreach(var type in a.kontrolEdilen)if(gm.Level(type)<a.gerekenLevel||gm.Hasarli(type))return false;
        return true;
    }
    void Show(string text)
    {
        serit.SetActive(true);if(ipucuText.text!=text)ipucuText.text=text;
        var rt=(RectTransform)serit.transform;var parent=rt.parent as RectTransform;
        float scale=CivicUI.ReadableScale(this);rt.localScale=Vector3.one*scale;
        rt.sizeDelta=new Vector2(Mathf.Min(1100,parent!=null?(parent.rect.width-48)/scale:1100),96);
        rt.anchoredPosition=new Vector2(0,22);
    }
    void Finish(){shown.Add(active);completed++;active=-1;elapsed=0;nextAt=Time.unscaledTime+.7f;CurrentDisasterHint=null;serit.SetActive(false);}
    public static AfetTuru? HintDisasterForDay(int day)
    {
        foreach(var a in DengeAyarlari.Aktif.afetler)if(a.gun-day>=1&&a.gun-day<=3)return a.afet;
        return null;
    }
    static bool HasarliVarMi(GameManager gm)=>gm.temelBinaHasarli||gm.okulHasarli||gm.akutHasarli||gm.itfaiyeHasarli||gm.setHasarli||gm.altyapiHasarli;
}
