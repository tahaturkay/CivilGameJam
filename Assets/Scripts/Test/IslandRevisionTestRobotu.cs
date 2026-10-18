#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public sealed class IslandRevisionTestRobotu : MonoBehaviour
{
    public bool trafficOnly;
    readonly StringBuilder log=new StringBuilder();int passed,failed,errors;
    GameManager gm;DisasterController disaster;IslandCityLife city;EconomyAndTime clock;AfetRaporu report;IslandCameraController nav;IslandSimulationPause pause;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnDisable(){Application.logMessageReceived-=OnLog;Time.timeScale=1;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void Check(bool ok,string message){if(ok)passed++;else failed++;log.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText("Logs/IslandRevisionProgress.txt",log.ToString());}
    IEnumerator Start()
    {
        Application.runInBackground=true;yield return null;yield return null;
        var run=Run();
        while(true)
        {
            object value;try{if(!run.MoveNext())break;value=run.Current;}catch(Exception e){Check(false,e.ToString());break;}
            yield return value;
        }
        Check(errors==0,"No runtime errors");log.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");File.WriteAllText("Logs/IslandRevisionTests.txt",log.ToString());
        UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator Run()
    {
        gm=GameManager.Instance;disaster=FindAnyObjectByType<DisasterController>();city=FindAnyObjectByType<IslandCityLife>();clock=FindAnyObjectByType<EconomyAndTime>();
        report=FindAnyObjectByType<AfetRaporu>();nav=Camera.main.GetComponent<IslandCameraController>();pause=FindAnyObjectByType<IslandSimulationPause>();
        clock.enabled=false;disaster.enabled=false;gm.kullaniciDurdurdu=gm.oyunDurduMu=false;gm.gun=1;gm.nufus=1000;Time.timeScale=1;yield return null;
        Check(pause!=null,"HUD owns a shared scaled-time pause driver");
        var flags=city.transform.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ODTU_Bayragi").ToArray();
        Check(flags.Length==5,"All five school/municipality flags replaced with ODTÜ flags");
        Check(flags.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()).Select(f=>f.sharedMesh).Distinct().Count()==1,"All flags share the same detailed double-sided emblem mesh");
        var hall=city.transform.Find("SabitAlanlar/Belediye");var school=city.transform.Find("YukseltilebilirYapilar/Okul");
        Check(hall.Find("BelediyeTabela").GetComponent<TextMesh>().text=="KALKANLI MUHTARLIĞI","Municipality sign reads Kalkanlı Muhtarlığı");
        Check(Enumerable.Range(1,3).All(i=>school.Find("Level_"+i+"/OkulTabelasi").GetComponent<TextMesh>().text=="ODTÜ")&&UIManager.GetBuildingDisplayName(BinaTuru.Okul)=="ODTÜ"&&AfetRaporu.YapiAdi(BinaTuru.Okul)=="ODTÜ","Every school level, popup and report uses ODTÜ");
        Check(FindObjectsByType<DereYatagiKarari>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Riverbed decision component removed from gameplay scene");
        int money=gm.para;float awareness=gm.farkindalikYuzdesi;gm.gun=7;yield return new WaitForSecondsRealtime(.35f);
        Check(!gm.oyunDurduMu&&gm.para==money&&gm.farkindalikYuzdesi==awareness&&GameObject.Find("DereYatagiKarari")==null,"Day seven has no decision popup, reward or forced pause");gm.gun=1;

        gm.hizCarpani=3;
        Check(city.TrafficSignalCount==16&&FindObjectsByType<IslandTrafficSignal>(FindObjectsSortMode.None).Length==16,"Eight loop junctions have sixteen real signal heads");
        var cars=city.transform.Find("SehirYasami").Cast<Transform>().Where(t=>t.name.StartsWith("AdaAraci_")).ToArray();
        var previous=cars.Select(t=>t.position).ToArray();var travelled=new float[cars.Length];var stopped=new float[cars.Length];float longestStop=0,minGap=float.PositiveInfinity;
        var clearance=typeof(JuryGameplayTestRobotu).GetMethod("VehicleClearance",BindingFlags.Static|BindingFlags.NonPublic);
        var phases=new HashSet<string>[8];for(int i=0;i<phases.Length;i++)phases[i]=new HashSet<string>();
        bool opposingGreen=false;float simulated=0,nextDump=30;
        while(simulated<180)
        {
            yield return null;float dt=Time.deltaTime*gm.hizCarpani;simulated+=dt;
            for(int i=0;i<cars.Length;i++)
            {
                float step=Vector3.Distance(previous[i],cars[i].position);travelled[i]+=step;
                stopped[i]=step<.003f?stopped[i]+dt:0;longestStop=Mathf.Max(longestStop,stopped[i]);previous[i]=cars[i].position;
            }
            minGap=Mathf.Min(minGap,(float)clearance.Invoke(null,new object[]{cars}));
            for(int ring=0;ring<2;ring++)for(int corner=0;corner<4;corner++)
            {
                phases[ring*4+corner].Add(city.GetTrafficSignalPhase(ring,corner));
                opposingGreen|=city.GetTrafficSignalAspect(ring,corner,0)==IslandTrafficSignal.Aspect.Green&&city.GetTrafficSignalAspect(ring,corner,1)==IslandTrafficSignal.Aspect.Green;
            }
            if(simulated>=nextDump){DumpTraffic(simulated);nextDump+=30;}
            if(trafficOnly&&longestStop>50){DumpTraffic(simulated);break;}
        }
        Check(!opposingGreen,"Conflicting approaches never receive simultaneous green");
        Check(phases.All(set=>set.Contains("GreenA")&&set.Contains("AmberA")&&set.Contains("ClearA")&&set.Contains("GreenB")&&set.Contains("AmberB")&&set.Contains("ClearB")),"Every junction cycles both green, amber and all-red phases");
        Check(minGap>=-.001f,"No vehicle footprint overlap during 180 simulated seconds; minimum gap="+minGap.ToString("F2"));
        Check(city.TrafficCrossingCount>=24&&travelled.All(d=>d>100),"Every car makes progress through multiple junctions; crossings="+city.TrafficCrossingCount+" minimum travel="+travelled.Min().ToString("F1"));
        Check(longestStop<25&&city.TrafficStuckVehicleCount==0,"No persistent traffic deadlock; longest stationary period="+longestStop.ToString("F1")+" simulated seconds");
        yield return Capture("Logs/Revision_Traffic.png");
        gm.kullaniciDurdurdu=true;pause.ApplyPause();yield return null;
        string phaseBefore=city.GetTrafficSignalPhase(0,0);Vector3[] held=cars.Select(t=>t.position).ToArray();yield return new WaitForSecondsRealtime(.4f);
        Check(Time.timeScale==0&&phaseBefore==city.GetTrafficSignalPhase(0,0)&&cars.Select((t,i)=>(t.position-held[i]).sqrMagnitude).All(d=>d<.000001f),"HUD pause freezes traffic lights and all vehicles");gm.kullaniciDurdurdu=false;pause.ApplyPause();
        if(trafficOnly)yield break;

        disaster.enabled=true;gm.hizCarpani=2;
        for(int index=0;index<4;index++)
        {
            gm.gun=1;gm.nufus=1000;gm.oyunBittiMi=gm.afetOlduMu=gm.oyunDurduMu=gm.kullaniciDurdurdu=false;pause.ApplyPause();
            foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru))){gm.LevelAyarla(type,1);gm.HasarliAyarla(type,false);gm.YapiIsleminiBildir(type);}
            yield return null;disaster.afetSuresi=index==0?4:3;disaster.AfetBaslat(index);
            float desired=index==1?.43f:.17f;
            while(disaster.AfetDevamEdiyorMu&&disaster.IncidentProgress<desired)yield return null;
            var typeName=disaster.AktifAfetTuru.ToString();
            Check(!(bool)typeof(IslandCameraController).GetMethod("ControlsBlocked",Private).Invoke(nav,null),typeName+" permits live camera input");
            Vector3 cameraStart=Camera.main.transform.position;
            nav.ApplyPointerPan(new Vector2(Screen.width*.5f,Screen.height*.5f),new Vector2(Screen.width*.5f+80,Screen.height*.5f));
            for(int j=0;j<6;j++){nav.ApplyKeyboardPan(Vector2.right,.06f);yield return null;}
            Check(Vector3.Distance(cameraStart,Camera.main.transform.position)>1,typeName+" camera pans during the active effect");
            if(index==1)
            {
                var roadNetwork=city.transform.Find("Yollar");
                float roadTop=roadNetwork.GetComponentsInChildren<BuildingVisuals>().Where(b=>b.binaTuru==BinaTuru.Altyapi)
                    .SelectMany(b=>b.levelModelleri.Where(m=>m.activeSelf)).SelectMany(m=>m.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.name=="Birlesik_Gorsel").Max(r=>r.bounds.max.y);
                Check(disaster.FloodSurfaceY>roadTop+.12f,"Flood surface overtops actual raised road meshes (water="+disaster.FloodSurfaceY.ToString("F2")+", road="+roadTop.ToString("F2")+")");
                yield return Capture("Logs/Revision_Flood.png");
            }
            gm.kullaniciDurdurdu=true;pause.ApplyPause();yield return null;yield return null;
            int damage=disaster.DamagedInstanceCount;float progress=disaster.IncidentProgress;
            Vector3 water=disaster.tsunamiSuyu.position,wave=disaster.tsunamiDalgasi.position;
            var particles=disaster.GetComponentsInChildren<ParticleSystem>();var ages=particles.Select(ps=>ps.time).ToArray();
            yield return new WaitForSecondsRealtime(.55f);
            Check(Time.timeScale==0&&disaster.DamagedInstanceCount==damage&&Mathf.Abs(disaster.IncidentProgress-progress)<.00001f,typeName+" pause freezes progress and damage/spread");
            Check((water-disaster.tsunamiSuyu.position).sqrMagnitude<.000001f&&(wave-disaster.tsunamiDalgasi.position).sqrMagnitude<.000001f,typeName+" pause freezes water and wave transforms");
            Check(particles.Select((ps,i)=>ps==null||Mathf.Abs(ps.time-ages[i])<.0001f).All(v=>v),typeName+" pause freezes all live effect particle clocks");
            var before=Camera.main.transform.position;nav.ApplyPointerPan(new Vector2(Screen.width*.5f,Screen.height*.5f),new Vector2(Screen.width*.5f,Screen.height*.5f+90));
            nav.ApplyScrollDelta(new Vector2(0,1),new Vector2(Screen.width*.5f,Screen.height*.5f),false);
            float zoomBefore=Camera.main.orthographicSize;for(int j=0;j<5;j++)yield return null;
            Check(Vector3.Distance(before,Camera.main.transform.position)>.4f&&Camera.main.orthographicSize<zoomBefore,typeName+" camera remains movable/zoomable while simulation is paused");
            Vector3 moved=Camera.main.transform.position;
            gm.kullaniciDurdurdu=false;pause.ApplyPause();float until=Time.realtimeSinceStartup+8;
            while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<until)yield return null;
            Check(gm.afetOlduMu&&!disaster.AfetDevamEdiyorMu,typeName+" resumes and finishes after pause");
            Check(Vector3.Distance(cameraStart,Camera.main.transform.position)>1,typeName+" does not restore the old camera position on completion");
            report.Devam();yield return null;nav.FrameCity();
        }
        gm.kullaniciDurdurdu=false;pause.ApplyPause();
        yield return Capture("Logs/Revision_Identity.png");
        var flag=flags.First(t=>t.gameObject.activeInHierarchy);
        var camera=Camera.main;Vector3 originalPosition=camera.transform.position;Quaternion originalRotation=camera.transform.rotation;float originalSize=camera.orthographicSize;
        nav.enabled=false;camera.transform.position=flag.position+new Vector3(.4f,.25f,-6);camera.transform.LookAt(flag.position);camera.orthographicSize=1.5f;
        yield return Capture("Logs/Revision_ODTU_Flag.png");
        camera.transform.SetPositionAndRotation(originalPosition,originalRotation);camera.orthographicSize=originalSize;nav.enabled=true;
    }
    void DumpTraffic(float simulated)
    {
        var output=new StringBuilder("Simulated seconds: "+simulated+"\n");
        var actors=(IList)typeof(IslandCityLife).GetField("vehicles",Private).GetValue(city);
        foreach(var actor in actors)
        {
            var type=actor.GetType();var root=(Transform)type.GetField("root").GetValue(actor);
            output.Append(root.name+" pos="+root.position+" yaw="+root.eulerAngles.y);
            foreach(string name in new[]{"routeIndex","distance","speed","committedJunction","waitingAtSignal","stalledScaledSeconds"})output.Append(" "+name+"="+type.GetField(name).GetValue(actor));
            output.AppendLine();
        }
        var junctions=(IList)typeof(IslandCityLife).GetField("trafficJunctions",Private).GetValue(city);
        for(int i=0;i<junctions.Count;i++)
        {
            object junction=junctions[i];output.Append("Junction "+i);
            foreach(string name in new[]{"center","phase","remaining"})output.Append(" "+name+"="+junction.GetType().GetField(name).GetValue(junction));output.AppendLine();
        }
        File.WriteAllText("Logs/TrafficFlowSnapshot.txt",output.ToString());
    }
    static IEnumerator Capture(string path){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}
}
#endif
