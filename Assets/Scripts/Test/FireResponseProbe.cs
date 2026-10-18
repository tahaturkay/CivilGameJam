#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

/// <summary>Real scene integration checks for contained fires, evacuation bands and playback keys.</summary>
public sealed class FireResponseProbe : MonoBehaviour
{
    public bool engineOnly;
    readonly StringBuilder log=new StringBuilder();int passed,failed,errors;
    GameManager gm;IslandCityLife city;DisasterController disaster;AfetRaporu report;IslandFireResponse response;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnDisable(){Application.logMessageReceived-=OnLog;Time.timeScale=1;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void Check(bool ok,string message){if(ok)passed++;else failed++;log.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText("Logs/FireResponseProgress.txt",log.ToString());}
    IEnumerator Start()
    {
        Application.runInBackground=true;yield return null;yield return null;yield return null;
        var steps=engineOnly?EngineRun():Run();
        while(true){object next;try{if(!steps.MoveNext())break;next=steps.Current;}catch(Exception error){Check(false,error.ToString());break;}yield return next;}
        Check(errors==0,"No runtime errors");log.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");File.WriteAllText("Logs/FireResponseTests.txt",log.ToString());UnityEditor.EditorApplication.isPlaying=false;
    }
    static object Field(object actor,string name)=>actor.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(actor);
    object[] Actors=>((IList)typeof(IslandCityLife).GetField("citizens",Private).GetValue(city)).Cast<object>().ToArray();
    void Policy(string name)=>typeof(IslandCityLife).GetMethod(name,Private).Invoke(city,null);
    void Begin(AfetTuru type)=>typeof(IslandCityLife).GetMethod("BeginIncident",Private).Invoke(city,new object[]{type});
    void DumpEngine(string label)
    {
        var dump=new StringBuilder(label+" engine="+response.Engine.position+" fire="+disaster.FireOrigin+" remaining="+response.DistanceRemaining+" routeValid="+response.RouteValid+" arrived="+response.Arrived+"\n");
        foreach(string key in new[]{"state","stationWorld","callDestination","waypointIndex"})dump.AppendLine(key+"="+Field(response,key));
        foreach(Vector3 p in (IList)Field(response,"route"))dump.AppendLine("waypoint="+p);
        var vehicles=(IList)typeof(IslandCityLife).GetField("vehicles",Private).GetValue(city);
        foreach(object car in vehicles){var t=(Transform)Field(car,"root");dump.AppendLine(t.name+" "+t.position+" yaw="+t.eulerAngles.y);}
        File.AppendAllText("Logs/FireEngineRoute.txt",dump.ToString());
    }
    IEnumerator EngineRun()
    {
        gm=GameManager.Instance;city=FindAnyObjectByType<IslandCityLife>();disaster=FindAnyObjectByType<DisasterController>();report=FindAnyObjectByType<AfetRaporu>();response=city.GetComponent<IslandFireResponse>();
        FindAnyObjectByType<EconomyAndTime>().enabled=false;gm.gun=1;gm.hizCarpani=1;gm.farkindalikYuzdesi=50;gm.kullaniciDurdurdu=gm.oyunDurduMu=false;
        disaster.afetSuresi=8;UnityEngine.Random.InitState(31415);disaster.AfetBaslat(0);yield return null;DumpEngine("Dispatch");
        for(int i=0;i<4;i++){yield return new WaitForSecondsRealtime(2.1f);DumpEngine("Time "+(i+1)*2.1f);}
        Check(response.Arrived&&Vector3.Distance(response.Engine.position,disaster.FireOrigin)<20,"Engine reaches the actual burning neighborhood in8seconds");
        report.Devam();gm.hizCarpani=3;yield return new WaitForSecondsRealtime(8);DumpEngine("Return");
        Check(!response.Dispatched,"Engine returns afterreport");
    }
    IEnumerator Run()
    {
        gm=GameManager.Instance;city=FindAnyObjectByType<IslandCityLife>();disaster=FindAnyObjectByType<DisasterController>();report=FindAnyObjectByType<AfetRaporu>();response=city.GetComponent<IslandFireResponse>();
        FindAnyObjectByType<EconomyAndTime>().enabled=false;disaster.enabled=false;gm.gun=1;gm.hizCarpani=1;gm.para=50000;gm.kullaniciDurdurdu=gm.oyunDurduMu=false;
        Check(response!=null&&response.Engine!=null,"Exactly one low-poly fire engine is installed beside its station");
        var settings=FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        foreach(int speed in new[]{1,2,3}){yield return Press(speed);Check(gm.hizCarpani==speed&&!gm.kullaniciDurdurdu,"Digit "+speed+" selects "+speed+"x");}
        gm.kullaniciDurdurdu=true;FindAnyObjectByType<IslandSimulationPause>().ApplyPause();yield return Press(2,true);
        Check(!gm.kullaniciDurdurdu&&gm.hizCarpani==2&&Time.timeScale>0,"Numpad speed key resumes the HUD pause");
        settings.Ac();yield return null;yield return Press(3);Check(gm.hizCarpani==2,"Settings ignores playback hotkeys");
        Check(settings.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="1 / 2 / 3"),"Settings lists 1 / 2 / 3 playback keys");
        yield return Capture("Logs/FireResponse_Settings.png");settings.Kapat();gm.hizCarpani=1;

        foreach(float awareness in new[]{0f,60f,60.01f,80f,80.01f,100f})
        {
            gm.farkindalikYuzdesi=awareness;
            foreach(var type in new[]{AfetTuru.Yangin,AfetTuru.Deprem,AfetTuru.Sel})
            {
                Begin(type);var actors=Actors;int band=awareness>80?2:awareness>60?1:0;
                bool low=band==0,water=type==AfetTuru.Sel;
                bool allSheltered=actors.All(a=>(IslandCityLife.CityLifeMode)Field(a,"mode")==IslandCityLife.CityLifeMode.Shelter&&!((GameObject)Field(a,"gameObject")).activeSelf);
                bool correct=city.AwarenessBandAtIncident==band&&(water ? (low?actors.All(a=>(IslandCityLife.CityLifeMode)Field(a,"mode")==IslandCityLife.CityLifeMode.Panic):allSheltered)
                    : low ? city.WrongResponseCount>0&&city.PanickedAssemblyCount>0
                    : actors.All(a=>(IslandCityLife.CityLifeMode)Field(a,"mode")==IslandCityLife.CityLifeMode.Assembly)&&city.PanickedAssemblyCount==(band==1?city.CitizenCount:0));
                Check(correct,type+" response at awareness "+awareness+" (band "+band+")");Policy("EndIncident");
            }
        }
        gm.farkindalikYuzdesi=0;gm.gun=IslandDisasterCalendar.TsunamiDay-1;yield return null;yield return null;
        var siren=FindAnyObjectByType<IslandSirenController>();Check(city.TsunamiShelterOrdered&&siren.TsunamiAdvanceWarning,"Tsunami siren sends everyone home a day early at zero awareness");
        gm.gun=IslandDisasterCalendar.TsunamiDay;yield return null;yield return null;
        Check(Actors.All(a=>!((GameObject)Field(a,"gameObject")).activeSelf),"No citizens or animals remain outdoors on tsunami day");
        gm.gun=1;yield return null;yield return null;

        gm.farkindalikYuzdesi=50;gm.LevelAyarla(BinaTuru.Itfaiye,1);disaster.enabled=true;disaster.afetSuresi=8;
        UnityEngine.Random.InitState(31415);disaster.AfetBaslat(0);yield return null;yield return null;
        Check(response.Dispatched,"Fire engine dispatches to an unprotected fire");
        DumpEngine("Full dispatch");
        yield return new WaitForSecondsRealtime(.6f);gm.kullaniciDurdurdu=true;FindAnyObjectByType<IslandSimulationPause>().ApplyPause();yield return null;
        Vector3 parked=response.Engine.position;int spread=disaster.BurningBuildingCount;
        yield return new WaitForSecondsRealtime(.25f);Check(response.Engine.position==parked&&disaster.BurningBuildingCount==spread,"HUD pause freezes engine and fire spread together");
        gm.kullaniciDurdurdu=false;FindAnyObjectByType<IslandSimulationPause>().ApplyPause();
        bool arrived=false,safe=true;float until=Time.realtimeSinceStartup+12;
        var carTransforms=((IList)typeof(IslandCityLife).GetField("vehicles",Private).GetValue(city)).Cast<object>().Select(a=>(Transform)Field(a,"root")).ToArray();
        while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<until)
        {
            arrived|=response.Arrived;
            foreach(var car in carTransforms)
            {
                var extents=car.GetComponent<MeshFilter>().sharedMesh.bounds.extents;
                safe&=response.IsVehiclePositionSafe(car.position,car.forward,extents.z,extents.x);
            }
            yield return null;
        }
        yield return null;yield return null;int weakLoss=gm.sonVefatSayisi,weakDamage=gm.sonHasarliBinaSayisi;
        Check((arrived||response.Arrived)&&Vector3.Distance(response.Engine.position,disaster.FireOrigin)<20,"Fire engine reaches the burning neighborhood before the report");
        Check(safe,"Fire engine does not overlap stopped city traffic");
        DumpEngine("Full report");
        Check(gm.afetOlduMu&&report.panel.activeSelf&&weakDamage>3,"Unprotected fire spreads locally and produces a report");
        Check(!disaster.FireAwaitingReport&&disaster.transform.Find("KatmanliYangin")==null,"Report extinguishes all active flames");
        Check(city.houses.Any(b=>b.IsDamaged)&&city.houses.Any(b=>!b.IsDamaged),"Only affected buildings retain damage");
        yield return Capture("Logs/FireResponse_Report.png");report.Devam();yield return null;RepairAll();yield return null;yield return null;
        Check(city.houses.All(b=>!b.IsDamaged),"Existing repairs clear local fire damage");
        // Let the engine return before the next real dispatch.
        until=Time.realtimeSinceStartup+12;gm.hizCarpani=3;
        while(response.Dispatched&&Time.realtimeSinceStartup<until)yield return null;
        gm.hizCarpani=1;Check(!response.Dispatched,"Engine returns to its station after the report");
        gm.LevelAyarla(BinaTuru.Itfaiye,2);UnityEngine.Random.InitState(31415);disaster.AfetBaslat(0);
        yield return new WaitForSecondsRealtime(3.5f);
        Check(disaster.SavunmaYeterli&&disaster.BurningBuildingCount>=2&&disaster.transform.Find("KatmanliYangin")!=null,"Strong fire defense still shows multiple visibly burning buildings");
        Check(response.Dispatched,"Protected neighborhoods also receive the fire engine");yield return Capture("Logs/FireResponse_ContainedFire.png");
        until=Time.realtimeSinceStartup+10;while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<until)yield return null;
        yield return null;yield return null;
        Check(gm.sonSavunmaYeterli&&gm.sonHasarliBinaSayisi>0&&gm.sonHasarliBinaSayisi<weakDamage,"Firefighters limit damaged buildings rather than eliminating the fire event");
        Check(gm.sonVefatSayisi<weakLoss,"Protected fire keeps the existing reduced casualty calculation ("+weakLoss+" → "+gm.sonVefatSayisi+")");
        Check(report.savunmaText.text.Contains("kontrol altına")&&!report.savunmaText.text.Contains("hasar almadan"),"Report describes containment and residual damage accurately");
        Check(city.houses.Where(b=>b.IsDamaged).Any(b=>b.transform.Find("KaliciHasarEfektleri")!=null),"Scorch and smoke persist after the active flames stop");
        yield return Press(3);Check(gm.hizCarpani==1,"Report ignores playback hotkeys");report.Devam();yield return null;RepairAll();
        // Use real flood water and current, not only response classification.
        foreach(float awareness in new[]{60f,60.01f})
        {
            gm.farkindalikYuzdesi=awareness;gm.LevelAyarla(BinaTuru.Altyapi,1);
            for(int i=0;i<3;i++)((Transform)Field(Actors[i],"root")).position=city.transform.TransformPoint(new Vector3(-30+i*2,city.landBounds.max.y+.34f,-14));
            disaster.enabled=true;disaster.afetSuresi=4;disaster.AfetBaslat(1);
            yield return new WaitForSecondsRealtime(1.9f);
            Check(awareness<=60 ? city.SweptCitizenCount>0 : Actors.All(a=>!((GameObject)Field(a,"gameObject")).activeSelf),
                "Real flood at "+awareness+" awareness: "+(awareness<=60?"outdoor people are carried by the current":"everyone remains sheltered"));
            float floodUntil=Time.realtimeSinceStartup+7;while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<floodUntil)yield return null;
            yield return null;report.Devam();yield return null;RepairAll();yield return null;
        }

        gm.LevelAyarla(BinaTuru.TemelBina,3);var popup=FindAnyObjectByType<BinaPopup>();popup.Ac(BinaTuru.TemelBina,city.houses[0].transform);yield return null;
        var hint=(TMP_Text)typeof(BinaPopup).GetField("actionHint",Private).GetValue(popup);
        hint.ForceMeshUpdate();Check(hint.text=="TAMAMEN GELİŞTİRİLDİ"&&hint.font==TurkishTMPFontSupport.DisplayFont&&!hint.isTextOverflowing&&!popup.aciklamaText.text.Contains("hazır"),"Fully upgraded popup uses one themed completion message without ready text");
        yield return Capture("Logs/FireResponse_MaxBuilding.png");
    }
    void RepairAll(){foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru)))if(gm.Hasarli(type))Ekonomi.Onar(type);}
    static IEnumerator Press(int speed,bool numpad=false)
    {
#if ENABLE_INPUT_SYSTEM
        Key key=numpad?(speed==1?Key.Numpad1:speed==2?Key.Numpad2:Key.Numpad3):(speed==1?Key.Digit1:speed==2?Key.Digit2:Key.Digit3);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
#else
        yield return null;
#endif
    }
    static IEnumerator Capture(string path){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}
}
#endif
