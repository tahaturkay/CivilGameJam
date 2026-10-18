using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared dispatch and right-of-way for the fire engine and earthquake rescue fleet.</summary>
[DisallowMultipleComponent]
public sealed class IslandFireResponse : MonoBehaviour
{
    readonly List<IslandResponseVehicle> fleet=new List<IslandResponseVehicle>(4);
    readonly List<Vector3> sites=new List<Vector3>(3);
    IslandCityLife city;
    DisasterController disaster;
    GameManager gm;
    BuildingVisuals[] buildings;
    AfetTuru callType;
    bool callOpen;
    float waitForDamage;
    public Transform Engine => fleet.Count>0&&fleet[0]!=null?fleet[0].transform:null;
    public bool Dispatched => fleet.Count>0&&fleet[0]!=null&&fleet[0].Dispatched;
    public bool Arrived => fleet.Count>0&&fleet[0]!=null&&fleet[0].Arrived;
    public bool RouteValid => fleet.Count>0&&fleet[0]!=null&&fleet[0].RouteValid;
    public float DistanceRemaining => fleet.Count>0&&fleet[0]!=null?fleet[0].DistanceRemaining:0;
    public int RescueVehicleCount => Mathf.Max(0,fleet.Count-1);
    public int ArrivedVehicleCount {get{int count=0;foreach(var vehicle in fleet)if(vehicle!=null&&vehicle.Arrived)count++;return count;}}
    public IReadOnlyList<IslandResponseVehicle> Fleet => fleet;

    void Start(){city=GetComponent<IslandCityLife>();EnsureFleet();}
    void EnsureFleet()
    {
        if(city==null)city=GetComponent<IslandCityLife>();
        if(city==null)return;
        if(fleet.Count==4&&fleet[0]!=null&&fleet[3]!=null)return;
        disaster=city.disasterController!=null?city.disasterController:FindAnyObjectByType<DisasterController>();
        gm=GameManager.Instance;
        Transform root=city.transform.Find("SehirYasami");if(root==null||disaster==null)return;
        fleet.Clear();buildings=FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None);
        Vector3 fireLocal=disaster.itfaiyeObj!=null?city.transform.InverseTransformPoint(disaster.itfaiyeObj.position):new Vector3(15,0,27);
        Vector3 rescueLocal=disaster.akutObj!=null?city.transform.InverseTransformPoint(disaster.akutObj.position):new Vector3(-16,0,27);
        for(int i=0;i<4;i++)
        {
            GameObject model=i==0?IslandActorArt.CreateFireEngine(root):IslandActorArt.CreateAkutVehicle(root,i-1);
            model.name=i==0?"AdaAraci_Itfaiye":"AdaAraci_AKUT_"+i;
            Vector3 station=i==0?fireLocal+new Vector3(-1.6f,0,-4.5f):rescueLocal+new Vector3(-6+(i-1)*5.8f,0,-7.8f);
            station=city.transform.TransformPoint(station);station.y=city.landBounds.max.y+.34f;
            var responder=model.AddComponent<IslandResponseVehicle>();
            responder.Initialize(this,city,i,station);fleet.Add(responder);
        }
    }
    void Update()
    {
        EnsureFleet();if(disaster==null||gm==null||fleet.Count==0)return;
        bool running=disaster.AfetDevamEdiyorMu;
        AfetTuru type=running?disaster.AktifAfetTuru:gm.sonAfetTuru;
        bool supported=type==AfetTuru.Yangin||type==AfetTuru.Deprem;
        bool open=supported&&(running||gm.afetOlduMu||(type==AfetTuru.Yangin&&disaster.FireAwaitingReport));
        if(open&&(!callOpen||type!=callType))
        {callOpen=true;callType=type;waitForDamage=0;sites.Clear();}
        if(callOpen&&open&&sites.Count==0&&!Paused)
        {
            waitForDamage+=Time.deltaTime;
            if(type==AfetTuru.Yangin)
            {sites.Add(disaster.FireOrigin);fleet[0].Dispatch(sites[0],0);}
            else if(CollectQuakeSites()||waitForDamage>=disaster.afetSuresi*.38f)
            {
                if(sites.Count==0)CollectPrecautionSites();
                if(sites.Count>0)
                {
                    fleet[0].Dispatch(sites[0],0);
                    for(int i=1;i<fleet.Count;i++)fleet[i].Dispatch(sites[(i-1)%sites.Count],i==1?-6.8f:0,i*.6f);
                }
            }
        }
        if(callOpen&&!open)
        {callOpen=false;sites.Clear();for(int i=0;i<fleet.Count;i++)if(fleet[i]!=null&&fleet[i].Dispatched)fleet[i].ReturnToStation(i*.6f);}
    }
    bool CollectQuakeSites()
    {
        if(buildings==null)return false;
        foreach(var building in buildings)
            if(building!=null&&building.IsDamaged&&building.DamageCause==AfetTuru.Deprem&&
                building.binaTuru!=BinaTuru.DenizSeti&&building.binaTuru!=BinaTuru.Altyapi)
                AddDistinctSite(building.transform.position);
        return sites.Count>0;
    }
    void CollectPrecautionSites()
    {
        foreach(var building in buildings)
            if(building!=null&&building.binaTuru==BinaTuru.TemelBina)AddDistinctSite(building.transform.position);
    }
    void AddDistinctSite(Vector3 position)
    {
        if(sites.Count>=3)return;
        foreach(Vector3 existing in sites)if(HorizontalSquared(existing,position)<24*24)return;
        sites.Add(position);
    }
    public bool Paused => IslandSimulationPause.IsPaused||(gm!=null&&(gm.oyunDurduMu||gm.afetOlduMu||gm.oyunBittiMi))||
        (disaster!=null&&disaster.AfetDevamEdiyorMu&&(disaster.AktifAfetTuru==AfetTuru.Sel||disaster.AktifAfetTuru==AfetTuru.Tsunami));
    internal bool ParkedStepSafe(IslandResponseVehicle self,Vector3 position,Quaternion rotation)
    {
        foreach(var other in fleet)
            if(other!=null&&other!=self&&(!other.Dispatched||other.Arrived||other.WaitingForDispatch)&&other.gameObject.activeInHierarchy&&
                IslandResponseVehicle.Overlaps(position,rotation,2.57f,1.05f,other.transform.position,other.transform.rotation,2.57f,1.05f,.18f))return false;
        return true;
    }
    public bool IsVehiclePositionSafe(Vector3 position,Vector3 forward,float halfLength,float halfWidth)
    {
        Quaternion rotation=forward.sqrMagnitude>.001f?Quaternion.LookRotation(forward,Vector3.up):Quaternion.identity;
        foreach(var vehicle in fleet)
            if(vehicle!=null&&vehicle.Dispatched&&vehicle.gameObject.activeInHierarchy&&
                IslandResponseVehicle.Overlaps(position,rotation,halfLength,halfWidth,vehicle.transform.position,vehicle.transform.rotation,2.57f,1.05f,.18f))return false;
        return true;
    }
    internal bool IsResponseStepSafe(IslandResponseVehicle self,Vector3 position,Quaternion rotation)
    {
        if(city==null||!city.IsEmergencyVehiclePositionSafe(position,rotation*Vector3.forward,2.57f,1.05f))return false;
        foreach(var other in fleet)
            if(other!=null&&other!=self&&other.gameObject.activeInHierarchy&&
                IslandResponseVehicle.Overlaps(position,rotation,2.57f,1.05f,other.transform.position,other.transform.rotation,2.57f,1.05f,.18f))return false;
        return true;
    }
    /// <summary>Only traffic about to enter a response corridor waits. Cars already in it clear the crossing.</summary>
    public void TrafficPriority(Vector3 position,Vector3 forward,out bool hold,out bool clearCrossing)
    {
        hold=clearCrossing=false;
        foreach(var vehicle in fleet)
        {
            if(vehicle==null||!vehicle.Moving||HorizontalSquared(position,vehicle.transform.position)>18*18)continue;
            Vector3 start=vehicle.transform.position,end=vehicle.PriorityPoint;
            bool alreadyInside=DistanceToSegmentSquared(position,start,end)<3.3f*3.3f;
            if(alreadyInside){clearCrossing=true;continue;}
            if(DistanceToSegmentSquared(position+forward*7,start,end)<4.2f*4.2f)hold=true;
        }
        if(clearCrossing)hold=false;
    }
    static float DistanceToSegmentSquared(Vector3 p,Vector3 a,Vector3 b)
    {p.y=a.y=b.y=0;Vector3 delta=b-a;float u=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(p-a,delta)/delta.sqrMagnitude):0;return (p-a-delta*u).sqrMagnitude;}
    static float HorizontalSquared(Vector3 a,Vector3 b){a.y=b.y=0;return (a-b).sqrMagnitude;}
    void OnDestroy(){foreach(var vehicle in fleet)if(vehicle!=null)Destroy(vehicle.gameObject);fleet.Clear();}
}
