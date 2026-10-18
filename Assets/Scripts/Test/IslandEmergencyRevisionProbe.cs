#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class IslandEmergencyRevisionProbe : MonoBehaviour
{
    readonly StringBuilder output=new StringBuilder();int passed,failed,errors,rings;
    IslandCityLife city;IslandFireResponse response;DisasterController disaster;GameManager gm;AfetRaporu report;
    bool safe=true;
    void OnEnable(){Application.logMessageReceived+=Log;}
    void OnDisable(){Application.logMessageReceived-=Log;Time.timeScale=1;if(gm!=null)gm.kullaniciDurdurdu=false;}
    void Log(string text,string trace,LogType type)
    {
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;
        if(text.Contains("Ran out of Graphics Ring Buffer")){rings++;output.AppendLine("INFO Graphics buffer warning at t="+Time.time.ToString("F2")+" incident="+(disaster!=null?disaster.AktifAfetTuru.ToString():"startup")+" drawCalls="+UnityStats.drawCalls+" triangles="+UnityStats.triangles);}
    }
    void Check(bool condition,string message)
    {
        if(condition)passed++;else failed++;output.AppendLine((condition?"PASS ":"FAIL ")+message);
        if(!condition&&response!=null&&response.Engine!=null)
        {
            foreach(var vehicle in response.Fleet)
            {
                var type=typeof(IslandResponseVehicle);var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                output.AppendLine("  "+vehicle.name+" at="+vehicle.transform.position+" yaw="+vehicle.transform.eulerAngles.y+" waypoint="+type.GetField("waypoint",flags).GetValue(vehicle)+" detour="+type.GetField("avoiding",flags).GetValue(vehicle)+" remaining="+vehicle.DistanceRemaining);
                var route=(IList)type.GetField("route",flags).GetValue(vehicle);var text=new StringBuilder("  route:");foreach(var point in route)text.Append(" "+point);output.AppendLine(text.ToString());
            }
            foreach(Transform child in city.transform.Find("SehirYasami"))if(child.name.StartsWith("AdaAraci_"))output.AppendLine("  traffic "+child.name+" "+child.position+" yaw="+child.eulerAngles.y);
        }
        File.WriteAllText("Logs/EmergencyRevisionProgress.txt",output.ToString());
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;Directory.CreateDirectory("Logs");for(int i=0;i<6;i++)yield return null;
        var run=Scenario();while(true){object next;try{if(!run.MoveNext())break;next=run.Current;}catch(Exception error){Check(false,error.ToString());break;}yield return next;}
        Check(errors==0&&rings==0,"No runtime errors or graphics ring buffer warnings");
        output.AppendLine("RESULT: "+passed+" passed, "+failed+" failed; errors="+errors+" rings="+rings);File.WriteAllText("Logs/EmergencyRevisionTests.txt",output.ToString());EditorApplication.isPlaying=false;
    }
    IEnumerator Scenario()
    {
        gm=GameManager.Instance;city=FindAnyObjectByType<IslandCityLife>();disaster=FindAnyObjectByType<DisasterController>();response=city.GetComponent<IslandFireResponse>();report=FindAnyObjectByType<AfetRaporu>();
        FindAnyObjectByType<EconomyAndTime>().enabled=false;gm.gun=1;gm.hizCarpani=1;gm.para=50000;gm.farkindalikYuzdesi=0;
        gm.kullaniciDurdurdu=gm.oyunDurduMu=gm.oyunBittiMi=gm.afetOlduMu=false;
        Check(response!=null&&response.Engine!=null&&response.RescueVehicleCount==3,"One fire engine and three shared-mesh AKUT vehicle variants are installed");
        var flags=city.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ODTU_Bayragi").ToArray();
        Check(flags.Length==5&&flags.All(t=>t.GetComponentInChildren<MeshRenderer>().sharedMaterials[0].color.r>.97f&&t.GetComponentInChildren<MeshRenderer>().sharedMaterials[0].color.g>.97f),"All five flags use white cloth with their red ODTÜ emblem");
        bool roadBeds=true;
        foreach(var building in city.transform.Find("Yollar").GetComponentsInChildren<BuildingVisuals>(true))
        {Vector3 f=building.transform.localRotation*Vector3.forward;float y=Mathf.Abs(f.z)>Mathf.Abs(f.x)?-.025f:0;roadBeds&=Mathf.Abs(building.transform.localPosition.y-y)<.001f;}
        Check(roadBeds,"North/south and east/west road decks no longer share a coplanar surface");
        gm.hizCarpani=3;
        foreach(Vector3 destination in new[]{new Vector3(-52,2.4f,-26),new Vector3(48,2.4f,-28),new Vector3(-14,2.4f,27),new Vector3(47,2.4f,27)})
        {
            response.Fleet[0].Dispatch(destination,0);float until=Time.realtimeSinceStartup+5;
            while(!response.Arrived&&Time.realtimeSinceStartup<until){Observe();yield return null;}
            Check(response.Arrived&&Vector3.Distance(response.Engine.position,destination)<22,"Engine escapes station and reaches neighborhood "+destination+" (remaining="+response.DistanceRemaining.ToString("F1")+")");
            response.Fleet[0].ReturnToStation();until=Time.realtimeSinceStartup+7;
            while(response.Dispatched&&Time.realtimeSinceStartup<until){Observe();yield return null;}
            Check(!response.Dispatched,"Engine returns safely through live traffic");
        }
        gm.hizCarpani=1;gm.LevelAyarla(BinaTuru.Itfaiye,1);UnityEngine.Random.InitState(31415);
        disaster.AfetBaslat(0);yield return null;yield return null;
        Check(!city.TrafficBlocked&&response.Dispatched,"A fire dispatches the engine while ordinary traffic keeps running");
        float travelled=city.TrafficTotalTravelDistance;
        yield return new WaitForSeconds(.7f);
        Check(city.TrafficTotalTravelDistance>travelled+.5f,"Civilian cars visibly move during the local fire");
        gm.kullaniciDurdurdu=true;FindAnyObjectByType<IslandSimulationPause>().ApplyPause();yield return null;
        Vector3 stopped=response.Engine.position;yield return new WaitForSecondsRealtime(.25f);
        Check(response.Engine.position==stopped,"Pausing freezes the response engine as well as fire spread");
        gm.kullaniciDurdurdu=false;FindAnyObjectByType<IslandSimulationPause>().ApplyPause();
        float deadline=Time.realtimeSinceStartup+10;bool arrived=false;
        while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<deadline){arrived|=response.Arrived;Observe();yield return null;}
        Check(arrived||response.Arrived,"The fire engine arrives at the burning district before the report");
        yield return Capture("Fire");report.Devam();gm.hizCarpani=3;deadline=Time.realtimeSinceStartup+9;
        while(response.Dispatched&&Time.realtimeSinceStartup<deadline){Observe();yield return null;}
        Check(!response.Dispatched,"The fire call returns to station after the report");
        gm.hizCarpani=2;gm.LevelAyarla(BinaTuru.TemelBina,1);gm.LevelAyarla(BinaTuru.Okul,1);gm.LevelAyarla(BinaTuru.Akut,1);
        disaster.AfetBaslat(2);yield return null;yield return null;
        travelled=city.TrafficTotalTravelDistance;deadline=Time.realtimeSinceStartup+10;int maximumArrivals=0;
        while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<deadline){maximumArrivals=Mathf.Max(maximumArrivals,response.ArrivedVehicleCount);Observe();yield return null;}
        Check(city.TrafficBlocked&&Mathf.Abs(city.TrafficTotalTravelDistance-travelled)<.01f,"Earthquake stops ordinary road traffic");
        Check(response.Fleet.All(v=>v.Dispatched),"Earthquake dispatches the fire engine and all three AKUT vehicles");
        Check(maximumArrivals==4||response.ArrivedVehicleCount==4,"All four responders reach their earthquake sites (arrived="+response.ArrivedVehicleCount+")");
        var damaged=FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None).Where(b=>b.IsDamaged&&b.DamageCause==AfetTuru.Deprem&&b.binaTuru!=BinaTuru.Altyapi&&b.binaTuru!=BinaTuru.DenizSeti).ToArray();
        Check(damaged.Length>0&&response.Fleet.All(v=>damaged.Any(b=>Vector3.Distance(b.transform.position,v.transform.position)<25)),"Rescue vehicles park near actual earthquake-damaged structures");
        Check(safe,"All observed normal/emergency and emergency/emergency vehicle bounds remain separated");
        yield return Capture("Earthquake");report.Devam();gm.hizCarpani=3;deadline=Time.realtimeSinceStartup+12;
        while(response.Fleet.Any(v=>v.Dispatched)&&Time.realtimeSinceStartup<deadline){Observe();yield return null;}
        Check(response.Fleet.All(v=>!v.Dispatched),"All responders return to their stations after the earthquake report");
        gm.hizCarpani=1;disaster.AfetBaslat(1);yield return null;yield return null;
        Check(city.TrafficBlocked&&response.Fleet.All(v=>!v.Dispatched),"Flood stops traffic and does not dispatch fire or AKUT vehicles");
        yield return new WaitForSeconds(3.5f);yield return Capture("Flood");
        deadline=Time.realtimeSinceStartup+10;while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<deadline)yield return null;
    }
    void Observe()
    {
        var list=(IList)typeof(IslandCityLife).GetField("vehicles",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(city);
        foreach(object actor in list)
        {
            Transform car=(Transform)actor.GetType().GetField("root").GetValue(actor);
            safe&=response.IsVehiclePositionSafe(car.position,car.forward,1.75f,.9f);
        }
        for(int a=0;a<response.Fleet.Count;a++)for(int b=a+1;b<response.Fleet.Count;b++)
            safe&=!IslandResponseVehicle.Overlaps(response.Fleet[a].transform.position,response.Fleet[a].transform.rotation,2.57f,1.05f,response.Fleet[b].transform.position,response.Fleet[b].transform.rotation,2.57f,1.05f,.05f);
    }
    static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/EmergencyRevision_"+name+".png");}
}
#endif
