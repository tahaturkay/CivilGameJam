#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real scene checks for Turkish glyphs, city time, warning/evacuation, weather and HUD settings.</summary>
public sealed class CityFeedbackTestRobotu : MonoBehaviour
{
    readonly StringBuilder log=new StringBuilder();int passed,failed,errors;
    float oldScale,oldOffset;
    void OnEnable(){Application.logMessageReceived+=OnLog;oldScale=UIManager.UstPanelOlcegi;oldOffset=UIManager.UstPanelDikeyBosluk;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void OnDisable(){Application.logMessageReceived-=OnLog;UIManager.UstPanelAyarlariniUygula(oldScale,oldOffset);Ayarlar.Kaydet();Time.timeScale=1;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Time.timeScale=1;yield return null;yield return null;
        var run=Run();
        while(true)
        {
            object step;
            try{if(!run.MoveNext())break;step=run.Current;}
            catch(Exception error){Check(false,error.ToString());break;}
            yield return step;
        }
        Check(errors==0,"No runtime errors during integration checks");
        log.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");
        File.WriteAllText("Logs/CityFeedbackTests.txt",log.ToString());UnityEditor.EditorApplication.isPlaying=false;
    }
    void Check(bool ok,string text)
    {if(ok)passed++;else failed++;log.AppendLine((ok?"PASS ":"FAIL ")+text);File.WriteAllText("Logs/CityFeedbackProgress.txt",log.ToString());}
    static FieldInfo Field(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
    static T Get<T>(object obj,string name)=>(T)Field(obj,name).GetValue(obj);
    IEnumerator Run()
    {
        var gm=GameManager.Instance;var city=FindAnyObjectByType<IslandCityLife>();var clock=FindAnyObjectByType<EconomyAndTime>();
        var disaster=FindAnyObjectByType<DisasterController>();var weather=FindAnyObjectByType<IslandWeather>();var siren=FindAnyObjectByType<IslandSirenController>();
        var report=FindAnyObjectByType<AfetRaporu>();var decision=FindAnyObjectByType<DereYatagiKarari>();
        if(decision!=null)decision.enabled=false;
        clock.enabled=false;disaster.enabled=false;gm.kullaniciDurdurdu=gm.oyunDurduMu=false;gm.hizCarpani=1;gm.gun=1;
        Check(city!=null&&siren!=null&&siren.Tower!=null,"Shared island creates exactly one civic siren beside municipality");
        Check(city.transform.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="BelediyeSirenKulesi")==1,"No duplicate siren mast");

        const string letters="İıŞşĞğÜüÖöÇç";var font=TurkishTMPFontSupport.PreferredFont;
        Check(font!=null&&font.atlasPopulationMode==AtlasPopulationMode.Static&&font.HasCharacters(letters),"Shipped static atlas contains every Turkish upper/lowercase letter");
        int textCount=0;bool covered=true;
        foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {textCount++;foreach(char letter in letters)covered&=text.font!=null&&text.font.HasCharacter(letter,true,false);}
        Check(covered&&textCount>30,"Every active/inactive scene TMP font resolves Turkish glyphs ("+textCount+" texts)");

        var settings=FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);settings.Ac();yield return null;
        var sliders=settings.GetComponentsInChildren<Slider>();
        var scale=sliders.FirstOrDefault(s=>s.name=="TopHudScaleSlider");var offset=sliders.FirstOrDefault(s=>s.name=="TopHudOffsetSlider");
        Check(scale!=null&&offset!=null,"Settings exposes upper-panel size and vertical position controls");
        scale.value=1.2f;offset.value=40;yield return null;
        Check(Mathf.Abs(UIManager.UstPanelOlcegi-1.2f)<.01f&&UIManager.UstPanelDikeyBosluk==40,"HUD slider changes apply and persist immediately");
        var hud=FindAnyObjectByType<UIManager>();var left=Get<RectTransform>(hud,"leftHudGroupRt");var right=Get<RectTransform>(hud,"rightHudColumnRt");
        Check(left.localScale.x>0&&right.localScale.x>0&&left.position.x<right.position.x,"HUD groups remain ordered at customized size");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/CityFeedback_Settings.png");yield return null;
        UIManager.UstPanelVarsayilanaSifirla();yield return null;
        Check(UIManager.UstPanelOlcegi==1&&UIManager.UstPanelDikeyBosluk==0,"Upper-panel reset restores both defaults");settings.Kapat();

        var citizenList=(IList)Field(city,"citizens").GetValue(city);object citizen=citizenList[0];
        var root=Get<Transform>(citizen,"root");var start=root.position;
        // Put one pedestrian on a long unobstructed leg; changing only the speed removes random waits from the measurement.
        void ResetCitizen(){root.position=start;Field(citizen,"mode").SetValue(citizen,IslandCityLife.CityLifeMode.Normal);Field(citizen,"waitRemaining").SetValue(citizen,0f);Field(citizen,"hasInitialLeg").SetValue(citizen,true);Field(citizen,"resumeAtNode").SetValue(citizen,false);Field(citizen,"initialLegTarget").SetValue(citizen,start+Vector3.right*20);}
        ResetCitizen();gm.hizCarpani=1;float started=Time.time;yield return new WaitForSecondsRealtime(.45f);float d1=Vector3.Distance(start,root.position)/(Time.time-started);
        ResetCitizen();gm.hizCarpani=2;started=Time.time;yield return new WaitForSecondsRealtime(.45f);float d2=Vector3.Distance(start,root.position)/(Time.time-started);
        ResetCitizen();gm.hizCarpani=3;started=Time.time;yield return new WaitForSecondsRealtime(.45f);float d3=Vector3.Distance(start,root.position)/(Time.time-started);
        Check(d1>.3f&&Mathf.Abs(d2/d1-2)<.20f&&Mathf.Abs(d3/d1-3)<.25f,"Pedestrian metres per game second follow 1x/2x/3x: "+d1.ToString("F2")+" / "+d2.ToString("F2")+" / "+d3.ToString("F2"));
        var vehicleList=(IList)Field(city,"vehicles").GetValue(city);object car=vehicleList[0];
        gm.hizCarpani=1;float v=Get<float>(car,"distance");started=Time.time;yield return new WaitForSecondsRealtime(.35f);float v1=(Get<float>(car,"distance")-v)/(Time.time-started);
        gm.hizCarpani=3;v=Get<float>(car,"distance");started=Time.time;yield return new WaitForSecondsRealtime(.35f);float v3=(Get<float>(car,"distance")-v)/(Time.time-started);
        Check(v1>.1f&&Mathf.Abs(v3/v1-3)<.4f,"Traffic route distance follows selected speed: "+v1.ToString("F2")+" / "+v3.ToString("F2"));
        gm.kullaniciDurdurdu=true;yield return null;
        var actors=city.transform.Find("SehirYasami").Cast<Transform>().ToArray();var positions=actors.Select(t=>t.position).ToArray();
        yield return new WaitForSecondsRealtime(.3f);
        Check(actors.Select((t,i)=>(t.position-positions[i]).sqrMagnitude).All(d=>d<.000001f),"HUD pause stops people, animals and vehicles");gm.kullaniciDurdurdu=false;

        Field(clock,"gunSayaci").SetValue(clock,DengeAyarlari.Aktif.birGunSuresi*.76f);gm.gun=1;yield return null;yield return null;
        Check(weather.NightAmount>.7f,"Ordinary days retain the smooth night cycle");
        foreach(var incident in DengeAyarlari.Aktif.afetler)
        {gm.gun=incident.gun;yield return null;yield return null;Check(weather.NightAmount<.001f&&!weather.IsNight&&weather.DisasterDaylightActive,incident.afet+" day removes only the night layer");}

        gm.gun=18;gm.farkindalikYuzdesi=0;yield return null;yield return null;
        Check(!siren.AlarmActive&&!city.TsunamiShelterOrdered,"No tsunami alarm two days early");
        gm.gun=IslandDisasterCalendar.TsunamiDay-1;gm.hizCarpani=3;yield return null;yield return null;
        Check(siren.AlarmActive&&siren.TsunamiAdvanceWarning&&city.TsunamiShelterOrdered&&city.CurrentMode==IslandCityLife.CityLifeMode.Shelter,"Day-before tsunami siren orders shelter even at zero awareness");
        float yaw=siren.RotorYaw;yield return new WaitForSecondsRealtime(.3f);
        Check(Mathf.Abs(Mathf.DeltaAngle(yaw,siren.RotorYaw))>20,"Red beacon physically rotates while warning");
        float deadline=Time.realtimeSinceStartup+5;
        while(city.ShelteredCount<city.LivingActorCount&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(city.ShelteredCount==city.LivingActorCount,"All citizens and street animals reach houses during warning ("+city.ShelteredCount+")");
        Check(siren.Tower.GetComponentsInChildren<Light>().All(l=>l.enabled&&l.shadows==LightShadows.None),"Active siren casts local red light without shadow maps");
        yield return CaptureTower(siren.Tower);
        gm.gun=IslandDisasterCalendar.TsunamiDay;yield return null;yield return null;
        Check(city.ShelteredCount==city.LivingActorCount&&city.SweptCitizenCount==0,"Residents remain indoors on the tsunami day");
        gm.kullaniciDurdurdu=true;yield return null;yaw=siren.RotorYaw;yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Mathf.DeltaAngle(yaw,siren.RotorYaw))<.01f,"Pausing also holds the siren rotor");gm.kullaniciDurdurdu=false;
        float originalDuration=disaster.afetSuresi;disaster.afetSuresi=2;disaster.enabled=true;disaster.AfetBaslat(3);
        bool indoorsThroughWave=true,alarmThroughWave=true,daylightThroughWave=true;
        while(disaster.AfetDevamEdiyorMu)
        {
            yield return null;
            if(!disaster.AfetDevamEdiyorMu)break;
            indoorsThroughWave&=city.ShelteredCount==city.LivingActorCount&&city.SweptCitizenCount==0;
            alarmThroughWave&=siren.AlarmActive;
            daylightThroughWave&=weather.NightAmount<.001f;
        }
        Check(indoorsThroughWave&&gm.afetOlduMu,"All warned residents remain indoors throughout the real tsunami wave");
        Check(alarmThroughWave&&daylightThroughWave,"Tsunami keeps its alarm and daylight for the full active event");
        gm.gun=1;report.Devam();disaster.afetSuresi=originalDuration;disaster.enabled=false;gm.hizCarpani=2;yield return null;yield return null;
        Check(!city.TsunamiShelterOrdered&&city.ShelteredCount==0&&!siren.AlarmActive,"Leaving the warning window releases shelter without stale state");

        // Inspect all remaining siren schedules using real disaster coroutines, with shortened effects only in this test.
        disaster.enabled=true;float duration=disaster.afetSuresi;disaster.afetSuresi=2;
        foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru))){gm.HasarliAyarla(type,false);gm.LevelAyarla(type,3);gm.YapiIsleminiBildir(type);}
        foreach(int i in new[]{0,1,2})
        {
            var incident=DengeAyarlari.Aktif.afetler[i];disaster.enabled=false;gm.gun=incident.gun-1;yield return null;yield return null;
            Check(!siren.AlarmActive,incident.afet+" has no advance siren");
            gm.gun=1;disaster.enabled=true;disaster.AfetBaslat(i);yield return null;yield return null;
            Check(siren.AlarmActive&&weather.NightAmount<.001f,incident.afet+" activates siren and daylight during the actual event");
            if(incident.afet==AfetTuru.Sel)Check(weather.RainActive,"Flood retains its specific rain weather");
            gm.kullaniciDurdurdu=true;yield return null;var held=root.position;yield return new WaitForSecondsRealtime(.15f);
            Check((held-root.position).sqrMagnitude<.000001f,"HUD pause also stops citizens during "+incident.afet);gm.kullaniciDurdurdu=false;
            while(disaster.AfetDevamEdiyorMu)yield return null;
            report.Devam();yield return null;yield return null;
            Check(!siren.AlarmActive,incident.afet+" alarm stops after the event");
        }
        disaster.afetSuresi=duration;disaster.enabled=false;

        var road=FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None).First(b=>b.binaTuru==BinaTuru.Altyapi);
        road.EnableLocalDamage();road.MarkLocalDamage(AfetTuru.Sel);gm.HasarliAyarla(BinaTuru.Altyapi,true);yield return null;yield return null;
        var network=FindAnyObjectByType<RoadNetworkState>();
        Check(network.ConstructionVisible&&network.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("Ada Yol Güvenliği")),"Damaged roads show the new shared striped low-poly barriers");
        Check(!network.GetComponentsInChildren<Collider>().Any(c=>c.name.StartsWith("YolOnarımı")),"Decorative repair barriers add no blocking physics colliders");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/CityFeedback_Barriers.png");yield return null;
        Field(clock,"gunSayaci").SetValue(clock,DengeAyarlari.Aktif.birGunSuresi*.32f);yield return null;yield return null;
        var marker=network.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith("Ada Yol Güvenliği")).transform;
        yield return CaptureTower(marker,"Logs/CityFeedback_BarrierDetail.png",9);
    }
    IEnumerator CaptureTower(Transform tower,string path="Logs/CityFeedback_Siren.png",float framing=12)
    {
        Camera camera=Camera.main;var nav=camera.GetComponent<IslandCameraController>();bool enabled=nav.enabled;nav.enabled=false;
        Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float size=camera.orthographicSize;
        camera.transform.position=tower.position+new Vector3(-17,15,-20);camera.transform.LookAt(tower.position+Vector3.up*(framing==12?4:1));camera.orthographicSize=framing;
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;
        camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;nav.enabled=enabled;
    }
}
#endif
