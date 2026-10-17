#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>Runs real island render paths and records native graphics-buffer warnings, not just C# exceptions.</summary>
public sealed class GraphicsBufferProbe : MonoBehaviour
{
    public string runLabel = "after";
    readonly StringBuilder report = new StringBuilder();
    ProfilerRecorder draws, batches;
    int bufferWarnings, errors;
    float originalDayLength = -1, originalTimeScale = 1;
    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
        draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
        batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
    }
    void OnDisable()
    {
        Application.logMessageReceived-=OnLog;draws.Dispose();batches.Dispose();
        if(originalDayLength>0 && DengeAyarlari.Aktif!=null)DengeAyarlari.Aktif.birGunSuresi=originalDayLength;
        Time.timeScale=originalTimeScale;
    }
    void OnLog(string message,string trace,LogType type)
    {
        if(message.Contains("Graphics Ring Buffer"))bufferWarnings++;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;
        originalDayLength=DengeAyarlari.Aktif.birGunSuresi;originalTimeScale=Time.timeScale;
        DengeAyarlari.Aktif.birGunSuresi=10000;Time.timeScale=1;
        yield return null;yield return null;
        var gm=GameManager.Instance;var disaster=FindAnyObjectByType<DisasterController>();
        gm.gun=1;gm.para=100000;gm.nufus=1000;gm.kullaniciDurdurdu=false;
        report.AppendLine("Graphics buffer probe "+runLabel+" · "+DateTime.Now);
        report.AppendLine(SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+" · "+Screen.width+"x"+Screen.height);
        var city=FindAnyObjectByType<IslandCityLife>();
        report.AppendLine("Citizens="+city.CitizenCount+" Vehicles="+city.VehicleCount+" Enabled city mesh renderers="+city.transform.Find("SehirYasami").GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled));
        report.AppendLine("Active renderers with property blocks="+FindObjectsByType<MeshRenderer>().Count(r=>r.enabled&&r.HasPropertyBlock()));
        yield return Sample("Normal",8);
        report.AppendLine("NPC instanced parts="+city.InstancedPartCount+" batches="+city.InstancedBatchCount);
        if(runLabel!="baseline")
        {
            Camera camera=Camera.main;Vector3 previous=camera.transform.position;float size=camera.orthographicSize;
            camera.transform.position=new Vector3(-26,4,0)-camera.transform.forward*230;camera.orthographicSize=32;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Logs/GraphicsBuffer-city.png");
            yield return null;yield return new WaitForEndOfFrame();
            camera.transform.position=previous;camera.orthographicSize=size;
        }
        BinaPopup.Instance.Ac(BinaTuru.TemelBina);yield return Sample("Popup",5);BinaPopup.Instance.Kapat();
        gm.para=100000;
        foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru)))Ekonomi.Yukselt(type);
        yield return Sample("All buildings upgraded and punched",5);
        if(runLabel=="baseline") { Finish();yield break; }
        foreach(int index in new[]{0,1,2,3})
        {
            foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru))) {gm.LevelAyarla(type,1);gm.HasarliAyarla(type,false);}
            gm.nufus=1000;gm.para=100000;
            disaster.AfetBaslat(index);
            yield return Sample("Disaster "+index,disaster.afetSuresi+1);
            FindAnyObjectByType<AfetRaporu>().Devam();
            yield return null;
        }
        Finish();
    }
    IEnumerator Sample(string label,float seconds)
    {
        float until=Time.realtimeSinceStartup+seconds;
        double dc=0,bt=0,ms=0;int n=0,start=bufferWarnings;
        while(Time.realtimeSinceStartup<until)
        {
            yield return null;dc+=draws.LastValue;bt+=batches.LastValue;ms+=Time.unscaledDeltaTime*1000;n++;
        }
        report.AppendLine(label+": frames="+n+" draws="+(dc/Math.Max(n,1)).ToString("F0")+" batches="+(bt/Math.Max(n,1)).ToString("F0")+" frameMs="+(ms/Math.Max(n,1)).ToString("F1")+" bufferWarnings="+(bufferWarnings-start));
        File.WriteAllText("Logs/GraphicsBuffer-"+runLabel+".txt",report.ToString());
    }
    void Finish()
    {
        report.AppendLine("TOTAL buffer warnings="+bufferWarnings+" errors="+errors);
        File.WriteAllText("Logs/GraphicsBuffer-"+runLabel+".txt",report.ToString());
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
