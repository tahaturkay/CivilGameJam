#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Focused real-scene checks for the popup layout cache and day flip counter.</summary>
public sealed class IslandHudRevisionProbe : MonoBehaviour
{
    const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly StringBuilder output=new StringBuilder();
    int passed,failed,errors;
    GameManager gm;
    UIManager ui;
    BinaPopup popup;

    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnDisable(){Application.logMessageReceived-=OnLog;Time.timeScale=1f;}
    void OnLog(string message,string trace,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}

    void Check(bool condition,string description)
    {
        if(condition)passed++;else failed++;
        output.AppendLine((condition?"PASS ":"FAIL ")+description);
    }

    IEnumerator Start()
    {
        Application.runInBackground=true;
        Directory.CreateDirectory("Logs");
        yield return null;yield return null;
        gm=GameManager.Instance;
        ui=FindAnyObjectByType<UIManager>();
        popup=FindAnyObjectByType<BinaPopup>(FindObjectsInactive.Include);
        var clock=FindAnyObjectByType<EconomyAndTime>();
        var pause=FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if(clock!=null)clock.enabled=false;
        if(pause!=null)pause.Devam();
        FindAnyObjectByType<IslandSimulationPause>()?.ApplyPause();
        Time.timeScale=1f;

        Check(gm!=null&&ui!=null&&popup!=null&&popup.panel!=null,"MainScene has its HUD, building popup and game state");
        if(gm==null||ui==null||popup==null||popup.panel==null)
        {Finish();yield break;}

        gm.gun=2;gm.para=50000;gm.nufus=1000;gm.farkindalikYuzdesi=50f;
        gm.kullaniciDurdurdu=false;gm.oyunDurduMu=false;gm.afetSuruyor=false;gm.afetOlduMu=false;gm.oyunBittiMi=false;
        yield return null;yield return null;
        yield return new WaitForSecondsRealtime(.6f);
        var totalText=ui.gunText!=null&&ui.gunText.transform.parent!=null
            ?ui.gunText.transform.parent.Find("GunToplam")?.GetComponent<TMP_Text>():null;
        Check(ui.gunText!=null&&ui.gunText.text=="2"&&totalText!=null&&totalText.text=="/ 20",
            "Day HUD keeps the day number and total in separate stable fields");

        gm.gun=3;
        float flipDeadline=Time.realtimeSinceStartup+1f;
        while(!ui.GunDayFlipActive&&Time.realtimeSinceStartup<flipDeadline)yield return null;
        bool flipStarted=ui.GunDayFlipActive;
        while(ui.GunDayFlipActive&&Time.realtimeSinceStartup<flipDeadline)yield return null;
        Check(flipStarted,"Day 2 to day 3 starts the short flip animation");
        Check(!ui.GunDayFlipActive&&ui.gunText.text=="3"&&totalText!=null&&totalText.text=="/ 20",
            "Flip settles on day 3 while the total stays fixed");

        foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
        {
            gm.LevelAyarla(type,1);gm.HasarliAyarla(type,false);
        }
        gm.gun=3;
        foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
        {
            yield return CheckPopup(type,"normal-L1",false,false);

            gm.LevelAyarla(type,2);
            gm.HasarVer(type,Cause(type));
            yield return CheckPopup(type,"damaged",true,type==BinaTuru.TemelBina);

            gm.HasarliAyarla(type,false);gm.LevelAyarla(type,3);
            yield return CheckPopup(type,"level3",false,false);
        }

        Check(errors==0,"No runtime errors during HUD and popup validation ("+errors+")");
        Finish();
    }

    IEnumerator CheckPopup(BinaTuru type,string state,bool damaged,bool capture)
    {
        gm.HasarliAyarla(type,damaged);
        if(damaged)
        {
            // HasarVer lowers one level to represent an actual repairable incident.
            if(gm.Level(type)<1)gm.LevelAyarla(type,1);
        }
        else if(state=="normal-L1")gm.LevelAyarla(type,1);

        popup.Ac(type);
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        var action=(TMP_Text)typeof(BinaPopup).GetField("actionHint",PrivateInstance).GetValue(popup);
        var info=(RectTransform)typeof(BinaPopup).GetField("infoCard",PrivateInstance).GetValue(popup);
        var card=(RectTransform)typeof(BinaPopup).GetField("card",PrivateInstance).GetValue(popup);
        TMP_Text description=popup.aciklamaText;
        if(description!=null)description.ForceMeshUpdate();
        if(action!=null)action.ForceMeshUpdate();

        bool available=popup.AcikMi&&description!=null&&action!=null&&info!=null&&card!=null&&popup.yukseltButonu!=null;
        Check(available,"Popup opens for "+type+" "+state);
        if(available)
        {
            Rect descRect=RectIn(description.rectTransform,card);
            Rect hintRect=RectIn(action.rectTransform,card);
            Rect upgradeRect=RectIn((RectTransform)popup.yukseltButonu.transform,card);
            Rect descGlyphs=RenderedRectIn(description,card);
            Rect hintGlyphs=RenderedRectIn(action,card);
            bool textRegions=Separated(descRect,hintRect)&&Separated(hintRect,upgradeRect);
            bool renderedRegions=Separated(descGlyphs,hintGlyphs)&&Separated(hintGlyphs,upgradeRect);
            Check(textRegions&&renderedRegions&&!description.isTextOverflowing&&!action.isTextOverflowing,
                type+" "+state+" description, action hint and upgrade button have separated rendered bounds");

            bool quizExpected=type==BinaTuru.Okul&&!damaged;
            Check(popup.quizButonu!=null&&popup.quizButonu.gameObject.activeSelf==quizExpected,
                type+" "+state+" keeps its expected quiz control visibility");
            if(capture)yield return Capture("Logs/HudRevision_DamagedLong.png");
            if(type==BinaTuru.Okul&&state=="normal-L1")yield return Capture("Logs/HudRevision_School.png");
        }
        popup.panel.SetActive(false);
    }

    static AfetTuru Cause(BinaTuru type)
    {
        switch(type)
        {
            case BinaTuru.DenizSeti:return AfetTuru.Tsunami;
            case BinaTuru.Altyapi:return AfetTuru.Sel;
            case BinaTuru.Itfaiye:return AfetTuru.Yangin;
            default:return AfetTuru.Deprem;
        }
    }

    static Rect RectIn(RectTransform source,RectTransform target)
    {
        Vector3[] corners=new Vector3[4];source.GetWorldCorners(corners);
        Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        Vector3 max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        for(int i=0;i<corners.Length;i++)
        {
            Vector3 p=target.InverseTransformPoint(corners[i]);
            min=Vector3.Min(min,p);max=Vector3.Max(max,p);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    static Rect RenderedRectIn(TMP_Text text,RectTransform target)
    {
        Bounds b=text.textBounds;
        Vector3 e=b.extents,c=b.center;
        Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        Vector3 max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 world=text.transform.TransformPoint(c+Vector3.Scale(e,new Vector3(x,y,z)));
            Vector3 p=target.InverseTransformPoint(world);
            min=Vector3.Min(min,p);max=Vector3.Max(max,p);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    static bool Separated(Rect upper,Rect lower)
    {return upper.yMin>=lower.yMax+1f||lower.yMin>=upper.yMax+1f;}

    static IEnumerator Capture(string path)
    {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}

    void Finish()
    {
        output.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");
        File.WriteAllText("Logs/HudRevisionTests.txt",output.ToString());
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
