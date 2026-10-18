using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Two late arrivals outside the seawall; cinematic presentation does not replace disaster accounting.</summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class IslandTsunamiFinale : MonoBehaviour
{
    static IslandTsunamiFinale active;
    public static bool BlocksInterface => active!=null&&active.IsPlaying;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){active=null;}

    public string CurrentStage {get;private set;}="Dormant";
    public bool HasPlayed {get;private set;}
    public bool IsPlaying {get;private set;}
    public Transform Boat {get;private set;}
    public Transform VisitorA {get;private set;}
    public Transform VisitorB {get;private set;}
    public Transform CinematicWave {get;private set;}
    public float BoatApproachProgress {get;private set;}
    public float WaveApproachProgress {get;private set;}
    public float ImpactGap {get;private set;}=float.PositiveInfinity;
    public bool CanPlay => ResolveHarbor()&&Camera.main!=null&&controller!=null&&controller.tsunamiDalgasi!=null;

    Transform harbor,markers,mooring,approach,standA,standB,exitA,exitB,deckA,deckB,deckExit;
    DisasterController controller;
    GameManager gm;
    Camera view;
    IslandCameraController navigation;
    IslandSirenAudio siren;
    GameObject visitorsRoot,overlayRoot;
    Canvas overlayCanvas;
    Image blackout;
    TMP_Text endingTitle,endingLine;
    Renderer[] waveRenderers,originalWaveRenderers,visitorARenderers;
    bool[] originalWaveVisibility;
    bool savedCamera,savedCursor,navigationEnabled,oldOrthographic,effectsDone,approachStarted,approachShot;
    Vector3 oldCameraPosition;
    Quaternion oldCameraRotation;
    float oldSize,oldFov,oldNear,oldFar,approachElapsed,waveElapsed,desiredGap=60;
    const float ApproachDuration=6f,WaveDuration=5.35f;
    readonly List<HudState> hud=new List<HudState>();
    sealed class HudState {public CanvasGroup group;public float alpha;public bool interactable,raycasts;}

    void Awake(){controller=GetComponent<DisasterController>();}
    void Start(){ResolveHarbor();EnsureSiren();}
    void EnsureSiren()
    {
        siren=IslandSirenAudio.Instance;
        if(siren==null)siren=new GameObject("TsunamiFinaliSirenSesi").AddComponent<IslandSirenAudio>();
    }

    bool ResolveHarbor()
    {
        if(harbor!=null&&Boat!=null)return true;
        var city=FindAnyObjectByType<IslandCityLife>();
        if(city==null)return false;
        harbor=city.transform.Find("SabitAlanlar/Liman");if(harbor==null)return false;
        markers=harbor.Find("CinematicMarkers");Boat=harbor.Find("BalikciTeknesi");
        if(markers==null||Boat==null){harbor=null;Boat=null;return false;}
        mooring=markers.Find("BoatMooring");approach=markers.Find("BoatApproachOrigin");
        standA=markers.Find("CharacterStandA");standB=markers.Find("CharacterStandB");
        exitA=markers.Find("PierDisembarkA");exitB=markers.Find("PierDisembarkB");
        deckA=Boat.Find("DeckMarkers/PassengerA");deckB=Boat.Find("DeckMarkers/PassengerB");deckExit=Boat.Find("DeckMarkers/DeckExit");
        if(mooring==null||approach==null||standA==null||standB==null||exitA==null||exitB==null||deckA==null||deckB==null||deckExit==null)
        {harbor=null;Boat=null;return false;}
        Boat.SetPositionAndRotation(approach.position,approach.rotation);
        return true;
    }

    void Update()
    {
        if(gm==null)gm=GameManager.Instance;
        if(!ResolveHarbor()||gm==null)return;
        if(overlayCanvas!=null)overlayCanvas.enabled=!PauseMenu.DurdurulduMu;
        if(IsPlaying||approachShot)Cursor.visible=PauseMenu.DurdurulduMu;
        int day=IslandDisasterCalendar.TsunamiDay;
        if(!HasPlayed&&!IsPlaying&&gm.gun==day-1)
        {
            if(!approachStarted)
            {
                approachStarted=true;EnsureVisitors();CurrentStage="BoatApproach";
                if(Camera.main!=null){SavePresentation();approachShot=true;}
            }
            if(!SimulationPaused())
            {
                approachElapsed=Mathf.Min(ApproachDuration,approachElapsed+Time.deltaTime*Mathf.Clamp(gm.hizCarpani,1,3));
                BoatApproachProgress=approachElapsed/ApproachDuration;
                Boat.position=Vector3.Lerp(approach.position,mooring.position,Mathf.SmoothStep(0,1,BoatApproachProgress));
                Boat.rotation=mooring.rotation;
                if(approachShot)FrameBoatApproach();
            }
            if(BoatApproachProgress>=1)
            {
                CurrentStage="Moored";
                if(approachShot){RestorePresentation();approachShot=false;}
            }
        }
        else if(approachShot&&!IsPlaying){RestorePresentation();approachShot=false;}
        if(!IsPlaying&&visitorsRoot!=null&&!HasPlayed)PlaceOnDeck();
        if(CurrentStage=="AwaitReport"&&gm.afetOlduMu)
        {
            var report=FindAnyObjectByType<AfetRaporu>();
            if(report!=null&&report.panel!=null&&report.panel.activeSelf)
            {if(overlayRoot!=null)overlayRoot.SetActive(false);CurrentStage="Complete";}
        }
    }

    bool SimulationPaused()=>IslandSimulationPause.IsPaused||(gm!=null&&(gm.oyunDurduMu||gm.oyunBittiMi));

    void FrameBoatApproach()
    {
        // The harbor sits outside the normal city framing: briefly show the actual arrival,
        // then return smoothly so the remaining warning day stays playable.
        Vector3 focus=(approach.position+mooring.position)*.5f+Vector3.up;
        Vector3 position=focus+new Vector3(18,35,-28);
        Quaternion rotation=Quaternion.LookRotation(focus-position);
        float u=BoatApproachProgress;
        float blend=Mathf.SmoothStep(0,1,Mathf.Min(u/.17f,(1-u)/.17f));
        view.orthographic=true;view.orthographicSize=Mathf.Lerp(oldSize,26,blend);
        view.transform.SetPositionAndRotation(Vector3.Lerp(oldCameraPosition,position,blend),Quaternion.Slerp(oldCameraRotation,rotation,blend));
    }

    void EnsureVisitors()
    {
        if(visitorsRoot!=null)return;
        visitorsRoot=new GameObject("LimanaGelenIkiZiyaretci");visitorsRoot.transform.SetParent(harbor,false);
        VisitorA=CreateVisitor("Ziyaretci_A",new Color32(33,106,139,255),new Color32(223,177,135,255),1);
        VisitorB=CreateVisitor("Ziyaretci_B",new Color32(213,155,55,255),new Color32(174,123,87,255),4);
        visitorARenderers=VisitorA.GetComponentsInChildren<Renderer>();PlaceOnDeck();
    }
    Transform CreateVisitor(string name,Color shirt,Color skin,int variant)
    {
        var person=IslandEffectsFactory.CreateCitizen(visitorsRoot.transform,shirt,skin);person.name=name;
        IslandActorArt.EnhanceCitizen(person,variant);person.transform.localScale=Vector3.one*1.15f;return person.transform;
    }
    void PlaceOnDeck()
    {
        if(VisitorA==null)return;
        VisitorA.SetPositionAndRotation(deckA.position,deckA.rotation);
        VisitorB.SetPositionAndRotation(deckB.position,deckB.rotation);
    }

    public IEnumerator Play(IEnumerator physicalTsunami)
    {
        if(HasPlayed||!CanPlay){yield return physicalTsunami;yield break;}
        if(approachShot){RestorePresentation();approachShot=false;}
        active=this;IsPlaying=true;HasPlayed=true;gm=GameManager.Instance;
        EnsureVisitors();Boat.SetPositionAndRotation(mooring.position,mooring.rotation);BoatApproachProgress=1;PlaceOnDeck();
        SavePresentation();BuildOverlay();CreateWave();
        bool completed=false;
        try
        {
            Vector3 startA=VisitorA.position,startB=VisitorB.position,boatExit=deckExit.position;
            Vector3 harborFocus=(standA.position+Boat.position)*.5f;
            Vector3 establishing=harborFocus+new Vector3(11,9,-13);
            view.orthographic=false;view.fieldOfView=58;view.nearClipPlane=.06f;view.farClipPlane=Mathf.Max(oldFar,500);
            view.transform.SetPositionAndRotation(establishing,Quaternion.LookRotation(harborFocus+Vector3.up*.8f-establishing));
            yield return Shot("Disembarking",3f,u=>
            {
                WalkOff(VisitorA,startA,boatExit,exitA.position,standA.position,Mathf.Clamp01(u/ .82f));
                WalkOff(VisitorB,startB,boatExit,exitB.position,standB.position,Mathf.Clamp01((u-.14f)/.82f));
            });
            VisitorA.SetPositionAndRotation(standA.position,standA.rotation);VisitorB.SetPositionAndRotation(standB.position,standB.rotation);
            SetGait(VisitorA,0);SetGait(VisitorB,0);
            var fillObject=new GameObject("IskeleYumusakYuzIsigi");fillObject.transform.SetParent(visitorsRoot.transform,false);
            fillObject.transform.position=(standA.position+standB.position)*.5f+new Vector3(0,2.4f,.6f);
            var fill=fillObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=8;fill.intensity=4;
            fill.color=new Color(.88f,.94f,1);fill.shadows=UnityEngine.LightShadows.None;
            EnsureSiren();
            siren.PlayCue();CinematicWave.gameObject.SetActive(true);effectsDone=false;
            StartCoroutine(PhysicalEffects(physicalTsunami));
            waveElapsed=0;WaveApproachProgress=0;desiredGap=60;
            yield return Shot("Warning",.55f,u=>UpdateWaveTime());
            foreach(var r in visitorARenderers)if(r!=null)r.enabled=false;
            view.fieldOfView=64;
            Vector3 eye=standA.position+Vector3.up*1.39f;
            // Look past the starboard side: the moored cabin must not obscure the threat.
            Quaternion seaLook=Quaternion.LookRotation(new Vector3(.7f,.11f,-1));
            view.transform.SetPositionAndRotation(eye,seaLook);
            yield return Shot("ViewWave",1.9f,u=>{UpdateWaveTime();view.transform.SetPositionAndRotation(eye,seaLook);});
            Quaternion friendLook=Quaternion.LookRotation(VisitorB.position+Vector3.up*1.31f-eye);
            Quaternion friendStart=VisitorB.rotation;
            Quaternion mutualLook=Quaternion.LookRotation(Vector3.ProjectOnPlane(standA.position-standB.position,Vector3.up));
            yield return Shot("LookAtFriend",1.35f,u=>
            {
                UpdateWaveTime();float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.48f));
                view.transform.SetPositionAndRotation(eye,Quaternion.Slerp(seaLook,friendLook,turn));
                VisitorB.rotation=Quaternion.Slerp(friendStart,mutualLook,turn);
            });
            yield return Shot("FinalWave",1.55f,u=>
            {
                UpdateWaveTime();float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.43f));
                view.transform.SetPositionAndRotation(eye,Quaternion.Slerp(friendLook,seaLook,turn));
                VisitorB.rotation=Quaternion.Slerp(mutualLook,standB.rotation,turn);
            });
            WaveApproachProgress=1;desiredGap=.70f;PositionWave();
            CurrentStage="CutToBlack";blackout.color=Color.black;siren.StopCue();
            // A hard cut before contact leaves the outcome implied; no ragdolls or extra deaths are injected.
            yield return null;
            endingTitle.gameObject.SetActive(true);endingLine.gameObject.SetActive(true);
            yield return Shot("Title",1.6f,u=>
            {
                endingTitle.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(u*3));
                endingLine.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.15f)*3));
            });
            while(!effectsDone){if(SimulationPaused()){yield return null;continue;}yield return null;}
            completed=true;
        }
        finally
        {
            if(siren!=null)siren.StopCue();
            RestorePresentation();IsPlaying=false;if(active==this)active=null;
            if(CinematicWave!=null){Destroy(CinematicWave.gameObject);CinematicWave=null;}
            if(visitorsRoot!=null){visitorsRoot.SetActive(false);}
            if(completed)
            {
                CurrentStage="AwaitReport";
                // Keep a black backing until the real report has appeared on the following frame.
                if(endingTitle!=null)endingTitle.gameObject.SetActive(false);
                if(endingLine!=null)endingLine.gameObject.SetActive(false);
            }
            else if(overlayRoot!=null)overlayRoot.SetActive(false);
        }
    }

    IEnumerator PhysicalEffects(IEnumerator effects){try{yield return effects;}finally{effectsDone=true;}}
    IEnumerator Shot(string stage,float duration,Action<float> animate)
    {
        CurrentStage=stage;float elapsed=0;
        while(elapsed<duration)
        {
            if(SimulationPaused()){yield return null;continue;}
            elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);animate(elapsed/duration);yield return null;
        }
        animate(1);
    }
    void UpdateWaveTime()
    {
        waveElapsed=Mathf.Min(WaveDuration,waveElapsed+Time.deltaTime);
        WaveApproachProgress=waveElapsed/WaveDuration;
        desiredGap=Mathf.Lerp(60,.70f,Mathf.SmoothStep(0,1,WaveApproachProgress));
        PositionWave();
    }
    void LateUpdate(){if(IsPlaying&&CinematicWave!=null&&CinematicWave.gameObject.activeSelf)PositionWave();}
    void PositionWave()
    {
        if(waveRenderers==null||waveRenderers.Length==0||CinematicWave==null)return;
        Bounds bounds=waveRenderers[0].bounds;for(int i=1;i<waveRenderers.Length;i++)bounds.Encapsulate(waveRenderers[i].bounds);
        Vector3 p=CinematicWave.position;p.z+=standA.position.z-desiredGap-bounds.max.z;CinematicWave.position=p;
        ImpactGap=desiredGap;
    }
    void CreateWave()
    {
        var source=controller.tsunamiDalgasi;
        originalWaveRenderers=source.GetComponentsInChildren<Renderer>(true);
        originalWaveVisibility=new bool[originalWaveRenderers.Length];
        // Clone before hiding the normal wave. Both use the existing sea materials and mesh design.
        var copy=Instantiate(source.gameObject);copy.name="FinalSahnesiDenizDalgasi";
        CinematicWave=copy.transform;CinematicWave.SetParent(transform,false);
        CinematicWave.SetPositionAndRotation(new Vector3(standA.position.x,-.575f,standA.position.z-65),source.rotation);
        waveRenderers=copy.GetComponentsInChildren<Renderer>(true);
        foreach(var r in waveRenderers)r.enabled=true;
        foreach(var c in copy.GetComponentsInChildren<Collider>(true))c.enabled=false;
        for(int i=0;i<originalWaveRenderers.Length;i++){originalWaveVisibility[i]=originalWaveRenderers[i].enabled;originalWaveRenderers[i].enabled=false;}
        copy.SetActive(false);
    }
    static void WalkOff(Transform person,Vector3 start,Vector3 boatExit,Vector3 pier,Vector3 stand,float u)
    {
        Vector3 p,heading;
        if(u<.35f){float t=u/.35f;p=Vector3.Lerp(start,boatExit,t);heading=boatExit-start;}
        else if(u<.77f){float t=(u-.35f)/.42f;p=Vector3.Lerp(boatExit,pier,t);heading=pier-boatExit;}
        else{float t=(u-.77f)/.23f;p=Vector3.Lerp(pier,stand,t);heading=stand-pier;}
        person.position=p;if(heading.sqrMagnitude>.001f)person.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(heading,Vector3.up));
        SetGait(person,u>0&&u<1?Mathf.Sin(u*28)*27:0);
    }
    static void SetGait(Transform person,float swing)
    {
        Transform a=person.Find("SolKol"),b=person.Find("SagKol"),c=person.Find("SolBacak"),d=person.Find("SagBacak");
        if(a!=null)a.localRotation=Quaternion.Euler(swing,0,0);if(b!=null)b.localRotation=Quaternion.Euler(-swing,0,0);
        if(c!=null)c.localRotation=Quaternion.Euler(-swing*.8f,0,0);if(d!=null)d.localRotation=Quaternion.Euler(swing*.8f,0,0);
    }

    void SavePresentation()
    {
        view=Camera.main;navigation=view.GetComponent<IslandCameraController>();navigationEnabled=navigation!=null&&navigation.enabled;
        if(navigation!=null){navigation.StopShake();navigation.enabled=false;}
        oldCameraPosition=view.transform.position;oldCameraRotation=view.transform.rotation;oldOrthographic=view.orthographic;
        oldSize=view.orthographicSize;oldFov=view.fieldOfView;oldNear=view.nearClipPlane;oldFar=view.farClipPlane;savedCamera=true;
        savedCursor=Cursor.visible;Cursor.visible=false;
        var ui=FindAnyObjectByType<UIManager>();
        if(ui!=null)foreach(var t in ui.GetComponentsInChildren<RectTransform>(true))
            if(t.name=="TopLeftHudGroup"||t.name=="TopRightHudColumn")
            {
                var g=t.GetComponent<CanvasGroup>();if(g==null)g=t.gameObject.AddComponent<CanvasGroup>();
                hud.Add(new HudState{group=g,alpha=g.alpha,interactable=g.interactable,raycasts=g.blocksRaycasts});
                g.alpha=0;g.interactable=g.blocksRaycasts=false;
            }
    }
    void RestorePresentation()
    {
        if(savedCamera&&view!=null)
        {
            view.transform.SetPositionAndRotation(oldCameraPosition,oldCameraRotation);view.orthographic=oldOrthographic;
            view.orthographicSize=oldSize;view.fieldOfView=oldFov;view.nearClipPlane=oldNear;view.farClipPlane=oldFar;
            if(navigation!=null){navigation.StopShake();navigation.enabled=navigationEnabled;}
            Cursor.visible=savedCursor;savedCamera=false;
        }
        foreach(var s in hud)if(s.group!=null){s.group.alpha=s.alpha;s.group.interactable=s.interactable;s.group.blocksRaycasts=s.raycasts;}hud.Clear();
        if(originalWaveRenderers!=null)for(int i=0;i<originalWaveRenderers.Length;i++)if(originalWaveRenderers[i]!=null)originalWaveRenderers[i].enabled=originalWaveVisibility[i];
    }
    void BuildOverlay()
    {
        overlayRoot=new GameObject("TsunamiFinaliSunumu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        overlayCanvas=overlayRoot.GetComponent<Canvas>();overlayCanvas.renderMode=RenderMode.ScreenSpaceOverlay;overlayCanvas.sortingOrder=90;
        var scaler=overlayRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var top=CivicUI.Box("UstSinemaBandi",overlayRoot.transform,Color.black,false);Band(top.rectTransform,true);
        var bottom=CivicUI.Box("AltSinemaBandi",overlayRoot.transform,Color.black,false);Band(bottom.rectTransform,false);
        blackout=CivicUI.Box("TemasOncesiKesme",overlayRoot.transform,Color.clear,false);CivicUI.Fill(blackout.rectTransform);
        endingTitle=CivicUI.Text("OyunSonuBasligi",blackout.transform,"OYUN SONU",64,new Color32(247,236,211,255),TurkishTMPFontSupport.DisplayFont);
        endingTitle.alignment=TextAlignmentOptions.Center;endingTitle.enableAutoSizing=true;endingTitle.fontSizeMin=28;endingTitle.fontSizeMax=64;
        var rt=endingTitle.rectTransform;rt.anchorMin=new Vector2(.12f,.46f);rt.anchorMax=new Vector2(.88f,.62f);rt.offsetMin=rt.offsetMax=Vector2.zero;
        endingLine=CivicUI.Text("FinalMesaji",blackout.transform,"UYARILAR HAYAT KURTARIR",22,new Color32(199,175,130,255),TurkishTMPFontSupport.PreferredFont);
        endingLine.alignment=TextAlignmentOptions.Center;rt=endingLine.rectTransform;rt.anchorMin=new Vector2(.15f,.36f);rt.anchorMax=new Vector2(.85f,.44f);rt.offsetMin=rt.offsetMax=Vector2.zero;
        endingTitle.alpha=endingLine.alpha=0;
        endingTitle.gameObject.SetActive(false);endingLine.gameObject.SetActive(false);
    }
    static void Band(RectTransform rt,bool top)
    {rt.anchorMin=new Vector2(0,top?.93f:0);rt.anchorMax=new Vector2(1,top?1:.07f);rt.offsetMin=rt.offsetMax=Vector2.zero;}

    void OnDisable()
    {
        StopAllCoroutines();RestorePresentation();if(siren!=null)siren.StopCue();IsPlaying=false;if(active==this)active=null;
        if(overlayRoot!=null)overlayRoot.SetActive(false);if(CinematicWave!=null)Destroy(CinematicWave.gameObject);
    }
    void OnDestroy(){if(visitorsRoot!=null)Destroy(visitorsRoot);if(overlayRoot!=null)Destroy(overlayRoot);}
}
