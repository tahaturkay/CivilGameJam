#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEngine;

public sealed class JuryHazardProbe : MonoBehaviour
{
    public bool impactOnly;
    readonly StringBuilder log=new StringBuilder();
    int pass,fail,errors,bufferWarnings;
    ProfilerRecorder draws,batches;
    void OnEnable()
    {
        Application.logMessageReceived+=OnLog;
        draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
    }
    void OnDisable(){Application.logMessageReceived-=OnLog;draws.Dispose();batches.Dispose();Time.timeScale=1;}
    void OnLog(string message,string trace,LogType type)
    {if(message.Contains("Graphics Ring Buffer"))bufferWarnings++;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Time.timeScale=2;yield return null;yield return null;
        var gm=GameManager.Instance;var disaster=FindAnyObjectByType<DisasterController>();var city=FindAnyObjectByType<IslandCityLife>();var report=FindAnyObjectByType<AfetRaporu>();var data=AnlatiVerisi.Aktif;
        log.AppendLine(SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" / "+Screen.width+"x"+Screen.height);
        foreach(var h in data.hikayeler)
        {
            Check(data.HikayeMetni(h.afet,false,85,false).Contains(h.kotu),h.afet+" bad story follows failed defense");
            string expected=string.IsNullOrEmpty(h.yuksekFarkindalik)?h.iyi:h.yuksekFarkindalik;
            Check(data.HikayeMetni(h.afet,true,85,false).Contains(expected),h.afet+" good story follows successful defense");
        }
        var clock=FindAnyObjectByType<EconomyAndTime>();var timer=typeof(EconomyAndTime).GetField("gunSayaci",BindingFlags.Instance|BindingFlags.NonPublic);
        var cars=new List<Transform>();foreach(Transform t in city.transform.Find("SehirYasami"))if(t.name.StartsWith("AdaAraci_"))cars.Add(t);
        var clearance=typeof(JuryGameplayTestRobotu).GetMethod("VehicleClearance",BindingFlags.Static|BindingFlags.NonPublic);object[] args={cars.ToArray()};
        if(!impactOnly)
        {
            foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru))){gm.HasarliAyarla(type,false);gm.LevelAyarla(type,3);gm.YapiIsleminiBildir(type);}
            BuildingVisuals weakened=null;
            foreach(var b in FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None)){b.EnableLocalDamage();if(b.binaTuru==BinaTuru.TemelBina&&weakened==null)weakened=b;}
            gm.kullaniciDurdurdu=true;gm.gun=1;gm.nufus=1000;
            gm.HasarVer(BinaTuru.TemelBina,AfetTuru.Yangin);gm.LevelAyarla(BinaTuru.TemelBina,3);weakened.MarkLocalDamage(AfetTuru.Yangin);
            yield return null;disaster.AfetBaslat(2);gm.kullaniciDurdurdu=false;
            while(disaster.AfetDevamEdiyorMu)yield return null;
            bool healthyIntact=true;foreach(var b in FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None))
                if(b.binaTuru==BinaTuru.TemelBina&&b!=weakened)healthyIntact&=!b.IsDamaged;
            Check(healthyIntact&&disaster.DamagedInstanceCount==1,"Earthquake cannot damage healthy upgraded neighbors of a previously burned house");
            report.Devam();yield return null;
        }
        foreach(int index in impactOnly?new[]{3}:new[]{0,1,2,3})foreach(bool defended in impactOnly?new[]{true}:new[]{false,true})
        {
            foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru)))
            {gm.HasarliAyarla(type,false);gm.LevelAyarla(type,defended?3:1);gm.YapiIsleminiBildir(type);}
            if(index==0&&!defended){gm.LevelAyarla(BinaTuru.TemelBina,3);gm.YapiIsleminiBildir(BinaTuru.TemelBina);}
            gm.gun=1;gm.nufus=1000;gm.para=10000;gm.farkindalikYuzdesi=defended?85:10;gm.kullaniciDurdurdu=true;gm.afetOlduMu=false;gm.oyunBittiMi=false;gm.oyunDurduMu=false;
            timer.SetValue(clock,DengeAyarlari.Aktif.birGunSuresi*.3f);
            yield return null;yield return null;
            AfetTuru hazard=DengeAyarlari.Aktif.afetler[index].afet;
            disaster.AfetBaslat(index);gm.kullaniciDurdurdu=false;float deadline=Time.realtimeSinceStartup+15;
            bool captured=false,impactCaptured=false,impact=false,wet=false,swept=false;float minClearance=float.PositiveInfinity;long drawTotal=0,batchTotal=0;int frames=0;double frameMs=0;
            while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<deadline)
            {
                impact|=disaster.TsunamiImpactTriggered;wet|=disaster.FloodSurfaceY>disaster.kuruZeminY+.1f;swept|=city.SweptCitizenCount>0;
                minClearance=Mathf.Min(minClearance,(float)clearance.Invoke(null,args));drawTotal+=draws.LastValue;batchTotal+=batches.LastValue;frames++;frameMs+=Time.unscaledDeltaTime*1000;
                if(hazard==AfetTuru.Tsunami&&!impactCaptured&&disaster.TsunamiImpactTriggered&&disaster.IncidentProgress>.16f)
                {impactCaptured=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/Jury_TsunamiImpact"+(defended?"_Protected":"")+".png");}
                if(!captured&&disaster.IncidentProgress>.54f)
                {
                    captured=true;
                    if(!defended||hazard==AfetTuru.Tsunami)
                    {
                        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/Jury_"+hazard+(defended?"_Protected":"")+".png");
                    }
                }
                yield return null;
            }
            yield return null;yield return null;
            Check(gm.afetOlduMu&&!disaster.AfetDevamEdiyorMu,hazard+" completes and reports");
            Check(gm.sonSavunmaYeterli==defended,hazard+" actual defense branch matches setup");
            Check(minClearance>=-.001f,hazard+" no vehicle overlap; minimum padded separation="+minClearance.ToString("F3"));
            var story=Array.Find(data.hikayeler,h=>h.afet==hazard);
            string expected=defended?(!string.IsNullOrEmpty(story.yuksekFarkindalik)?story.yuksekFarkindalik:story.iyi):story.kotu;
            Check(report.hasarText.text.Contains(expected),hazard+" actual report shows "+(defended?"good":"bad")+" character story");
            if(defended)Check(disaster.DamagedInstanceCount==0,hazard+" protected structures remain intact");
            if(hazard==AfetTuru.Sel||hazard==AfetTuru.Tsunami)Check(wet!=defended,hazard+" water appears only when defense fails");
            if(hazard==AfetTuru.Tsunami){Check(impact,"Tsunami visibly impacts the seawall in both branches");if(!defended)Check(swept,"Tsunami carries exposed outdoor citizens");}
            if(hazard==AfetTuru.Deprem&&!defended)
            {
                int roofs=0;bool retained=true;
                foreach(var d in FindObjectsByType<BuildingDamageVisuals>(FindObjectsSortMode.None))
                {
                    var original=(Mesh)typeof(BuildingDamageVisuals).GetField("originalRoof",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(d);
                    var broken=(Mesh)typeof(BuildingDamageVisuals).GetField("brokenRoof",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(d);
                    if(original!=null&&broken!=null){roofs++;retained&=broken.triangles.Length==original.triangles.Length;}
                }
                Check(roofs>0&&retained,"Earthquake roofs keep their broken faces instead of deleting the roof");
                Check(disaster.DamagedInstanceCount>0&&disaster.DamagedInstanceCount<43,"Earthquake damages a subset of vulnerable buildings");
            }
            if(hazard==AfetTuru.Yangin&&!defended)Check(disaster.DamagedInstanceCount>0&&disaster.DamagedInstanceCount<=9,"Fire remains in a limited neighborhood");
            if(hazard==AfetTuru.Yangin&&!defended)
            {
                bool unchanged=gm.temelBinaLevel==3;
                foreach(var b in FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None))
                    if(b.binaTuru==BinaTuru.TemelBina)unchanged&=b.levelModelleri[2].activeSelf;
                Check(unchanged,"A local fire preserves purchased housing levels and untouched houses do not shrink");
            }
            log.AppendLine(hazard+" "+(defended?"protected":"exposed")+": frames="+frames+" draws="+(drawTotal/Math.Max(1,frames))+" batches="+(batchTotal/Math.Max(1,frames))+" frameMs="+(frameMs/Math.Max(1,frames)).ToString("F1"));
            if(hazard==AfetTuru.Deprem&&!defended)
            {yield return new WaitForSecondsRealtime(1.1f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Logs/Jury_Report.png");yield return null;}
            report.Devam();yield return null;
        }
        Check(errors==0,impactOnly?"No runtime errors during protected tsunami":"No runtime errors during all eight real disaster branches");
        Check(bufferWarnings==0,impactOnly?"No Graphics Ring Buffer warnings during protected tsunami":"No Graphics Ring Buffer warnings during all eight branches");
        log.AppendLine("RESULT: "+pass+" passed, "+fail+" failed; runtimeErrors="+errors+" bufferWarnings="+bufferWarnings);
        File.WriteAllText(impactOnly?"Logs/JuryImpactTest.txt":"Logs/JuryHazardTests.txt",log.ToString());UnityEditor.EditorApplication.isPlaying=false;
    }
    void Check(bool ok,string text){if(ok)pass++;else fail++;log.AppendLine((ok?"PASS ":"FAIL ")+text);File.WriteAllText("Logs/JuryHazardProgress.txt",log.ToString());}
}
#endif
