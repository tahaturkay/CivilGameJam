using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>The final scheduled tsunami is presented once through the two harbor visitors.</summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class IslandTsunamiFinale : MonoBehaviour
{
    static IslandTsunamiFinale active;
    public static bool BlocksInterface => active!=null&&
        (active.IsPlaying||(active.IsComplete&&!active.AccountingComplete));
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){active=null;}

    public string CurrentStage {get;private set;}="Dormant";
    public bool HasPlayed {get;private set;}
    public bool IsPlaying {get;private set;}
    public bool IsComplete {get;private set;}
    public bool AccountingComplete => gm!=null&&gm.oyunBittiMi;
    public bool WaveFillsScreen {get;private set;}
    public float FadeAmount => blackout!=null?blackout.color.a:0;
    public Vector3 SunsetDirection {get;private set;}
    public Transform Boat {get;private set;}
    public Transform VisitorA {get;private set;}
    public Transform VisitorB {get;private set;}
    public Transform CinematicWave {get;private set;}
    public float BoatApproachProgress {get;private set;}
    public float WaveApproachProgress {get;private set;}
    public float ImpactGap {get;private set;}=float.PositiveInfinity;
    public bool CanPlay => ResolveHarbor()&&Camera.main!=null&&controller!=null&&controller.tsunamiDalgasi!=null;

    Transform harbor,markers,mooring,approach,approachTurn,standA,standB,exitA,exitB,deckA,deckB,deckExit;
    DisasterController controller;
    GameManager gm;
    Camera view;
    IslandCameraController navigation;
    IslandWeather weather;
    IslandFinaleSunDisc sunsetDisc;
    IslandSirenAudio siren;
    GameObject visitorsRoot,overlayRoot,waveFace;
    Mesh waveFaceMesh;
    Material waveFaceMaterial;
    Canvas overlayCanvas;
    Image blackout,topBand,bottomBand;
    TMP_Text endingTitle,endingLine;
    Renderer[] waveRenderers,originalWaveRenderers,visitorARenderers;
    bool[] originalWaveVisibility;
    bool savedCamera,savedCursor,navigationEnabled,oldOrthographic,effectsDone;
    Vector3 oldCameraPosition,waveScale;
    Quaternion oldCameraRotation;
    int oldCullingMask;
    CameraClearFlags oldClearFlags;
    Color oldBackground;
    float oldSize,oldFov,oldNear,oldFar,desiredGap=105,storyElapsed,curtainAmount;
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
        mooring=markers.Find("BoatMooring");approach=markers.Find("BoatApproachOrigin");approachTurn=markers.Find("BoatApproachTurn");
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
        if(overlayCanvas!=null)overlayCanvas.enabled=!PauseMenu.DurdurulduMu;
        if(IsPlaying)Cursor.visible=PauseMenu.DurdurulduMu;
        else if(IsComplete)Cursor.visible=true;
        // Day 19 belongs to evacuation and normal city play. The arrival starts only
        // inside the existing day-20 disaster coroutine, never as a second tsunami.
    }
    bool SimulationPaused()=>IslandSimulationPause.IsPaused||(gm!=null&&gm.oyunDurduMu);
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
        VisitorA.SetPositionAndRotation(deckA.position,deckA.rotation);
        VisitorB.SetPositionAndRotation(deckB.position,deckB.rotation);
    }

    public IEnumerator Play(IEnumerator physicalTsunami)
    {
        if(HasPlayed||!CanPlay){yield return physicalTsunami;yield break;}
        active=this;IsPlaying=true;HasPlayed=true;gm=GameManager.Instance;
        Boat.SetPositionAndRotation(approach.position,approach.rotation);EnsureVisitors();
        SavePresentation();BuildOverlay();CreateWave();
        weather=FindAnyObjectByType<IslandWeather>();
        Vector3 towardFriend=Quaternion.AngleAxis(10,Vector3.up)*Vector3.ProjectOnPlane(standB.position-standA.position,Vector3.up).normalized;
        SunsetDirection=(towardFriend+Vector3.up*.075f).normalized;
        if(weather!=null)weather.SetCinematicSunset(true,SunsetDirection);
        sunsetDisc=new GameObject("IskeleGunBatimi").AddComponent<IslandFinaleSunDisc>();
        sunsetDisc.Configure(towardFriend,standA.position);
        EnsureSiren();
        bool completed=false;
        try
        {
            view.orthographic=false;view.fieldOfView=58;view.nearClipPlane=.06f;view.farClipPlane=Mathf.Max(oldFar,500);
            CinematicWave.gameObject.SetActive(true);SetWaveGap(105);
            Vector3 opening=standA.position+new Vector3(6,18,7);
            Quaternion openingLook=Quaternion.LookRotation(Vector3.Lerp(Boat.position,standA.position,.28f)+Vector3.up*2-opening);
            Vector3 dockCamera=standA.position+new Vector3(12,8,-14);
            Quaternion dockLook=Quaternion.LookRotation((standA.position+mooring.position)*.5f+Vector3.up-dockCamera);
            yield return Shot("BoatApproach",6f,u=>
            {
                float t=Mathf.SmoothStep(0,1,u);BoatApproachProgress=t;
                Vector3 control=approachTurn!=null?approachTurn.position:(approach.position+mooring.position)*.5f+Vector3.right*10;
                Boat.position=(1-t)*(1-t)*approach.position+2*(1-t)*t*control+t*t*mooring.position;
                Boat.rotation=Quaternion.Slerp(approach.rotation,mooring.rotation,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.95f,u)));
                PlaceOnDeck();SetWaveGap(Mathf.Lerp(105,45,t));
                float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.22f));
                float orbit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,1,u));
                Vector3 position=Vector3.Lerp(opening,dockCamera,orbit);
                Quaternion rotation=Quaternion.Slerp(openingLook,dockLook,orbit);
                view.transform.SetPositionAndRotation(Vector3.Lerp(oldCameraPosition,position,enter),Quaternion.Slerp(oldCameraRotation,rotation,enter));
            });
            Boat.SetPositionAndRotation(mooring.position,mooring.rotation);PlaceOnDeck();
            Vector3 startA=VisitorA.position,startB=VisitorB.position,boatExit=deckExit.position;
            Vector3 closeDock=dockCamera+new Vector3(-2,-1.5f,2);
            yield return Shot("Disembarking",3.2f,u=>
            {
                WalkOff(VisitorA,startA,boatExit,exitA.position,standA.position,Mathf.Clamp01(u/.82f));
                WalkOff(VisitorB,startB,boatExit,exitB.position,standB.position,Mathf.Clamp01((u-.14f)/.82f));
                SetWaveGap(Mathf.Lerp(45,34,Mathf.SmoothStep(0,1,u)));
                Vector3 p=Vector3.Lerp(dockCamera,closeDock,Mathf.SmoothStep(0,1,u));
                view.transform.SetPositionAndRotation(p,Quaternion.LookRotation((standA.position+mooring.position)*.5f+Vector3.up-p));
            });
            VisitorA.SetPositionAndRotation(standA.position,standA.rotation);VisitorB.SetPositionAndRotation(standB.position,standB.rotation);
            SetGait(VisitorA,0);SetGait(VisitorB,0);AddFaceFill();
            siren.PlayCue();effectsDone=false;StartCoroutine(PhysicalEffects(physicalTsunami));
            Vector3 eye=standA.position+Vector3.up*1.39f;
            Quaternion seaLook=Quaternion.LookRotation(new Vector3(.7f,.11f,-1));
            Vector3 warningPosition=view.transform.position;Quaternion warningLook=view.transform.rotation;
            yield return Shot("Warning",.9f,u=>
            {
                float t=Mathf.SmoothStep(0,1,u);SetWaveGap(Mathf.Lerp(34,30,t));
                view.transform.SetPositionAndRotation(Vector3.Lerp(warningPosition,eye,t),Quaternion.Slerp(warningLook,seaLook,t));
                view.fieldOfView=Mathf.Lerp(58,64,t);
                if(u>.55f)foreach(var r in visitorARenderers)if(r!=null)r.enabled=false;
            });
            yield return Shot("ViewWave",1.5f,u=>{SetWaveGap(Mathf.Lerp(30,22,Mathf.SmoothStep(0,1,u)));view.transform.SetPositionAndRotation(eye,seaLook);});
            Quaternion friendLook=Quaternion.LookRotation(VisitorB.position+Vector3.up*1.31f-eye);
            Quaternion friendStart=VisitorB.rotation;
            Quaternion mutualLook=Quaternion.LookRotation(Vector3.ProjectOnPlane(standA.position-standB.position,Vector3.up));
            yield return Shot("LookAtFriend",1.8f,u=>
            {
                SetWaveGap(Mathf.Lerp(22,10,Mathf.SmoothStep(0,1,u)));float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.52f));
                view.transform.SetPositionAndRotation(eye,Quaternion.Slerp(seaLook,friendLook,turn));
                VisitorB.rotation=Quaternion.Slerp(friendStart,mutualLook,turn);
            });
            CreateWaveFace();
            yield return Shot("FinalWave",1.8f,u=>
            {
                SetWaveGap(Mathf.Lerp(10,.70f,Mathf.SmoothStep(0,1,u)));float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.52f));
                view.transform.SetPositionAndRotation(eye,Quaternion.Slerp(friendLook,seaLook,turn));
                VisitorB.rotation=Quaternion.Slerp(mutualLook,standB.rotation,turn);
                CinematicWave.localScale=Vector3.Scale(waveScale,new Vector3(1,Mathf.Lerp(1,1.9f,Mathf.SmoothStep(0,1,u)),1));
                // The breaking face rolls across the lens, extending the same water surface
                // beyond every screen edge. No sky triangle, dock edge or letterbox remains.
                SetWaveFace(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,1,u)));
                float impactShade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1f,u));
                blackout.color=new Color(0f,0f,0f,impactShade);
            });
            SetWaveFace(1);endingTitle.gameObject.SetActive(true);endingLine.gameObject.SetActive(true);
            yield return Shot("Title",2.2f,u=>
            {
                endingTitle.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(u*3));
                endingLine.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.12f)*3));
            });
            var music=MusicPlayer.Instance!=null?MusicPlayer.Instance.GetComponent<AudioSource>():null;
            float musicVolume=music!=null?music.volume:0;
            float fadeStart=blackout.color.a;
            yield return Shot("FadeToBlack",3f,u=>
            {
                float fade=Mathf.Lerp(fadeStart,1f,Mathf.SmoothStep(0,1,u));blackout.color=new Color(0,0,0,fade);
                endingTitle.alpha=endingLine.alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.85f,u));
                if(music!=null)music.volume=musicVolume*(1-fade);
            });
            while(!effectsDone)yield return null;
            if(music!=null)music.Stop();
            siren.StopCue();blackout.color=Color.black;endingTitle.alpha=endingLine.alpha=0;
            if(overlayCanvas!=null){overlayCanvas.overrideSorting=true;overlayCanvas.sortingOrder=-1;}
            // The report is now on black; stop rendering the obscured city and
            // animating the close wave while keeping overlay UI and ESC available.
            view.cullingMask=0;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.black;
            if(CinematicWave!=null)CinematicWave.gameObject.SetActive(false);
            if(waveFace!=null)waveFace.SetActive(false);
            if(sunsetDisc!=null)sunsetDisc.gameObject.SetActive(false);
            IsComplete=true;CurrentStage="Complete";completed=true;
        }
        finally
        {
            if(siren!=null)siren.StopCue();IsPlaying=false;
            if(completed)
            {
                // Keep the black scene behind the normal city scorecard. The controller
                // records the tsunami outcome before AccountingComplete releases the UI.
                if(visitorsRoot!=null)visitorsRoot.SetActive(false);
            }
            else
            {
                RestorePresentation();if(active==this)active=null;
                if(overlayRoot!=null)overlayRoot.SetActive(false);
            }
        }
    }
    void AddFaceFill()
    {
        var fillObject=new GameObject("IskeleYumusakYuzIsigi");fillObject.transform.SetParent(visitorsRoot.transform,false);
        fillObject.transform.position=(standA.position+standB.position)*.5f+new Vector3(0,2.4f,.6f);
        var fill=fillObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=8;fill.intensity=3;
        fill.color=new Color(1,.85f,.69f);fill.shadows=LightShadows.None;
    }
    IEnumerator PhysicalEffects(IEnumerator effects){try{yield return effects;}finally{effectsDone=true;}}
    IEnumerator Shot(string stage,float duration,Action<float> animate)
    {
        CurrentStage=stage;float elapsed=0;
        while(elapsed<duration)
        {
            if(SimulationPaused()){yield return null;continue;}
            elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);storyElapsed+=Time.deltaTime;
            if(sunsetDisc!=null)
            {
                sunsetDisc.SetProgress(Mathf.Clamp01(storyElapsed/19));
                SunsetDirection=(sunsetDisc.transform.position-standA.position).normalized;
                if(weather!=null)weather.SetCinematicSunset(true,SunsetDirection);
            }
            animate(elapsed/duration);yield return null;
        }
        animate(1);
    }
    void SetWaveGap(float gap)
    {desiredGap=gap;WaveApproachProgress=Mathf.InverseLerp(105,.70f,gap);PositionWave();}
    void LateUpdate()
    {
        if(IsComplete&&view!=null)view.backgroundColor=Color.black;
        if(IsPlaying&&CinematicWave!=null&&CinematicWave.gameObject.activeSelf)PositionWave();
        if(waveFace!=null&&curtainAmount>0)FrameWaveFace();
    }
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
        var copy=Instantiate(source.gameObject);copy.name="FinalSahnesiDenizDalgasi";
        CinematicWave=copy.transform;CinematicWave.SetParent(transform,false);
        CinematicWave.SetPositionAndRotation(new Vector3(standA.position.x,-.575f,standA.position.z-110),source.rotation);waveScale=CinematicWave.localScale;
        waveRenderers=copy.GetComponentsInChildren<Renderer>(true);
        foreach(var r in waveRenderers)r.enabled=true;
        foreach(var c in copy.GetComponentsInChildren<Collider>(true))c.enabled=false;
        for(int i=0;i<originalWaveRenderers.Length;i++){originalWaveVisibility[i]=originalWaveRenderers[i].enabled;originalWaveRenderers[i].enabled=false;}
        copy.SetActive(false);
    }
    void CreateWaveFace()
    {
        waveFace=new GameObject("DalganinKamerayiSaranYuzu",typeof(MeshFilter),typeof(MeshRenderer));
        waveFace.transform.SetParent(view.transform,false);
        // An overscanned breaking-water face, sharing the visible wave's ocean material.
        // It is world geometry, so the final blue frame remains animated ocean water.
        waveFaceMesh=new Mesh{name="FinalDalgaYuzu"};
        waveFaceMesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
        waveFaceMesh.triangles=new[]{0,1,2,0,2,3};waveFaceMesh.RecalculateBounds();
        waveFace.GetComponent<MeshFilter>().sharedMesh=waveFaceMesh;
        var renderer=waveFace.GetComponent<MeshRenderer>();
        waveFaceMaterial=new Material(waveRenderers[0].sharedMaterial){name="FinalDenizDokusuYakinPlan"};
        // The lens is less than a metre from the sheet. Preserve the ocean palette and
        // animation while keeping its water detail readable at this close-up scale.
        if(waveFaceMaterial.HasProperty("_WaveScale"))waveFaceMaterial.SetFloat("_WaveScale",5.5f);
        renderer.sharedMaterial=waveFaceMaterial;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        waveFace.SetActive(false);
    }
    void SetWaveFace(float amount)
    {
        curtainAmount=amount;WaveFillsScreen=amount>=.999f;
        if(waveFace!=null){waveFace.SetActive(amount>0);FrameWaveFace();}
        Color bands=new Color(0,0,0,1-amount);topBand.color=bottomBand.color=bands;
    }
    void FrameWaveFace()
    {
        float depth=.6f,height=2*depth*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*1.12f;
        waveFace.transform.localPosition=new Vector3(0,-height*(1-curtainAmount),depth);
        waveFace.transform.localRotation=Quaternion.identity;waveFace.transform.localScale=new Vector3(height*view.aspect,height,1);
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
        oldCullingMask=view.cullingMask;oldClearFlags=view.clearFlags;oldBackground=view.backgroundColor;
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
        if(weather!=null)weather.SetCinematicSunset(false,Vector3.up);
        if(savedCamera&&view!=null)
        {
            view.transform.SetPositionAndRotation(oldCameraPosition,oldCameraRotation);view.orthographic=oldOrthographic;
            view.orthographicSize=oldSize;view.fieldOfView=oldFov;view.nearClipPlane=oldNear;view.farClipPlane=oldFar;
            view.cullingMask=oldCullingMask;view.clearFlags=oldClearFlags;view.backgroundColor=oldBackground;
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
        topBand=CivicUI.RawBox("UstSinemaBandi",overlayRoot.transform,Color.black,false);Band(topBand.rectTransform,true);
        bottomBand=CivicUI.RawBox("AltSinemaBandi",overlayRoot.transform,Color.black,false);Band(bottomBand.rectTransform,false);
        blackout=CivicUI.RawBox("DalgaSiyahaSolma",overlayRoot.transform,Color.clear,false);CivicUI.Fill(blackout.rectTransform);
        endingTitle=CivicUI.Text("OyunSonuBasligi",overlayRoot.transform,"OYUN BİTTİ",64,new Color32(247,236,211,255),TurkishTMPFontSupport.DisplayFont);
        endingTitle.alignment=TextAlignmentOptions.Center;endingTitle.enableAutoSizing=true;endingTitle.fontSizeMin=28;endingTitle.fontSizeMax=64;
        var rt=endingTitle.rectTransform;rt.anchorMin=new Vector2(.12f,.46f);rt.anchorMax=new Vector2(.88f,.62f);rt.offsetMin=rt.offsetMax=Vector2.zero;
        endingLine=CivicUI.Text("FinalMesaji",overlayRoot.transform,"Farkındalık Hayat Kurtarır",22,new Color32(199,175,130,255),TurkishTMPFontSupport.PreferredFont);
        endingLine.alignment=TextAlignmentOptions.Center;rt=endingLine.rectTransform;rt.anchorMin=new Vector2(.15f,.36f);rt.anchorMax=new Vector2(.85f,.44f);rt.offsetMin=rt.offsetMax=Vector2.zero;
        endingTitle.alpha=endingLine.alpha=0;endingTitle.gameObject.SetActive(false);endingLine.gameObject.SetActive(false);
    }
    static void Band(RectTransform rt,bool top)
    {rt.anchorMin=new Vector2(0,top?.93f:0);rt.anchorMax=new Vector2(1,top?1:.07f);rt.offsetMin=rt.offsetMax=Vector2.zero;}
    void OnDisable()
    {
        StopAllCoroutines();RestorePresentation();if(siren!=null)siren.StopCue();IsPlaying=IsComplete=false;if(active==this)active=null;
        if(overlayRoot!=null)overlayRoot.SetActive(false);if(CinematicWave!=null)Destroy(CinematicWave.gameObject);
        if(waveFace!=null)Destroy(waveFace);if(sunsetDisc!=null)Destroy(sunsetDisc.gameObject);
    }
    void OnDestroy()
    {
        if(visitorsRoot!=null)Destroy(visitorsRoot);if(overlayRoot!=null)Destroy(overlayRoot);
        if(waveFaceMesh!=null)Destroy(waveFaceMesh);
        if(waveFaceMaterial!=null)Destroy(waveFaceMaterial);
    }
}
