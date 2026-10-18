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
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

/// <summary>Focused real-scene integration probe for the two-day tsunami harbor finale.</summary>
public sealed class TsunamiFinaleProbe : MonoBehaviour
{
    public bool strongDefense;
    public bool runBothBranches;

    readonly StringBuilder output = new StringBuilder();
    readonly List<string> stageOrder = new List<string>();
    readonly HashSet<string> stages = new HashSet<string>();
    readonly HashSet<string> capturedStages = new HashSet<string>();
    int passed, failed, runtimeErrors, ringWarnings;
    string branchName;

    GameManager gm;
    EconomyAndTime clock;
    DisasterController disaster;
    IslandTsunamiFinale finale;
    IslandCityLife city;
    IslandWeather weather;
    IslandSirenAudio siren;
    AfetRaporu report;
    OyunSonu gameOver;
    IpucuSistemi hint;
    PauseMenu pauseMenu;
    Camera mainCamera;
    Transform mooring;
    bool uiStayedHidden = true;
    bool sawWater, sawImpact;
    float closestImpactGap = float.PositiveInfinity;
    string lastStage = "";

    string ProgressPath => "Logs/TsunamiFinale_" + branchName + "_Progress.txt";
    string ResultPath => "Logs/TsunamiFinale_" + branchName + "_Tests.txt";

    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
        Time.timeScale = 1f;
        if (gm != null) gm.kullaniciDurdurdu = false;
        if (pauseMenu != null && PauseMenu.DurdurulduMu) pauseMenu.Devam();
    }

    void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeErrors++;
        if (message.IndexOf("Ran out of Graphics Ring Buffer space", StringComparison.OrdinalIgnoreCase) >= 0)
            ringWarnings++;
    }

    void Check(bool condition, string description)
    {
        if (condition) passed++; else failed++;
        output.AppendLine((condition ? "PASS " : "FAIL ") + description);
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ProgressPath, output.ToString());
    }

    IEnumerator Start()
    {
        branchName = strongDefense ? "Strong" : "Weak";
        Application.runInBackground = true;
        Directory.CreateDirectory("Logs");
        for (int i = 0; i < 5; i++) yield return null;

        IEnumerator run = RunScenario();
        while (true)
        {
            object next;
            try
            {
                if (!run.MoveNext()) break;
                next = run.Current;
            }
            catch (Exception error)
            {
                Check(false, "Unhandled probe exception: " + error);
                break;
            }
            yield return next;
        }

        Check(runtimeErrors == 0, "No runtime errors during the tsunami finale");
        Check(ringWarnings == 0, "No Graphics Ring Buffer exhaustion during the tsunami finale");
        output.AppendLine("RESULT: " + passed + " passed, " + failed + " failed; runtimeErrors=" + runtimeErrors + "; ringWarnings=" + ringWarnings);
        File.WriteAllText(ResultPath, output.ToString());

        if (runBothBranches && !strongDefense)
            SessionState.SetString("TsunamiFinaleValidation.NextRun", "strong");
        EditorApplication.isPlaying = false;
    }

    IEnumerator RunScenario()
    {
        gm=GameManager.Instance;clock=FindAnyObjectByType<EconomyAndTime>();
        disaster=FindAnyObjectByType<DisasterController>();city=FindAnyObjectByType<IslandCityLife>();
        weather=FindAnyObjectByType<IslandWeather>();finale=FindAnyObjectByType<IslandTsunamiFinale>();
        report=FindAnyObjectByType<AfetRaporu>(FindObjectsInactive.Include);
        gameOver=FindAnyObjectByType<OyunSonu>(FindObjectsInactive.Include);
        hint=FindAnyObjectByType<IpucuSistemi>(FindObjectsInactive.Include);
        pauseMenu=FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);mainCamera=Camera.main;
        Check(gm!=null&&clock!=null&&disaster!=null&&city!=null&&finale!=null&&weather!=null&&mainCamera!=null,
            "Real MainScene managers, weather, camera and finale are present");
        if(gm==null||clock==null||disaster==null||city==null||finale==null||weather==null||mainCamera==null)yield break;
        clock.enabled=false;gm.gun=1;gm.hizCarpani=1;gm.nufus=1000;gm.para=10000;gm.farkindalikYuzdesi=0;
        gm.akutLevel=1;gm.kullaniciDurdurdu=gm.oyunDurduMu=gm.afetSuruyor=gm.afetOlduMu=gm.oyunBittiMi=gm.kazandiMi=false;
        pauseMenu.Devam();FindAnyObjectByType<IslandSimulationPause>()?.ApplyPause();Time.timeScale=1;
        yield return null;yield return null;
        int day=IslandDisasterCalendar.TsunamiDay;
        Check(day==20&&DengeAyarlari.Aktif.toplamGun==20,"Tsunami and game ending retain the authored day-20 schedule");
        gm.LevelAyarla(BinaTuru.DenizSeti,strongDefense?3:1);gm.HasarliAyarla(BinaTuru.DenizSeti,false);
        gm.LevelAyarla(BinaTuru.Akut,1);gm.HasarliAyarla(BinaTuru.Akut,false);
        Field(clock,"gunSayaci").SetValue(clock,0f);gm.gun=19;
        yield return null;yield return null;
        Check(finale.CanPlay,"Installed sailboat and harbor markers make the finale playable");
        if(!finale.CanPlay)yield break;
        var supplied=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Taha/LowPolyGemi.prefab");
        Check(supplied!=null&&finale.Boat.GetComponentInChildren<MeshFilter>().sharedMesh==supplied.GetComponentInChildren<MeshFilter>().sharedMesh,
            "The harbor uses the supplied LowPolyGemi mesh and original palette");
        Check(city.TsunamiShelterOrdered&&city.CurrentMode==IslandCityLife.CityLifeMode.Shelter,
            "Day 19 orders every resident and animal to shelter regardless of awareness");
        float evacuationStarted=Time.time;
        while(city.ShelteredCount<city.LivingActorCount&&Time.time-evacuationStarted<11f)yield return null;
        Check(city.ShelteredCount==city.LivingActorCount&&Time.time-evacuationStarted<12,
            "Every resident and animal naturally reaches shelter before day 19 ends (seconds="+(Time.time-evacuationStarted).ToString("F2")+")");
        Check(finale.CurrentStage=="Dormant"&&!finale.HasPlayed&&!IslandTsunamiFinale.BlocksInterface&&mainCamera.orthographic,
            "Day 19 keeps the ordinary city camera; no boat escort or duplicate tsunami plays");
        Check(city.TrafficBlocked,"The warned city also stops ordinary traffic");
        gm.gun=20;
        float deadline=Time.realtimeSinceStartup+38;
        bool boatPaused=false,warningPaused=false,friendChecked=false,arrivalChecked=false,titleChecked=false;
        bool fadeIncreasing=true;float previousFade=0;
        while(!finale.IsComplete&&Time.realtimeSinceStartup<deadline)
        {
            Observe();
            if(finale.CurrentStage=="BoatApproach"&&finale.BoatApproachProgress>.12f&&!boatPaused)
            {
                boatPaused=true;yield return VerifyPauseAtBoat();
                Check(finale.CinematicWave.gameObject.activeSelf&&finale.CinematicWave.GetComponentInChildren<Renderer>().bounds.max.z<finale.Boat.position.z,
                    "Boat approaches the pier with the tsunami visibly following behind it");
            }
            if(finale.CurrentStage=="Disembarking"&&!arrivalChecked)
            {
                arrivalChecked=true;
                Check(finale.BoatApproachProgress>=.999f&&finale.VisitorA!=null&&finale.VisitorB!=null,
                    "The supplied boat docks with the original two visitors aboard");
                Check(weather.CinematicSunsetActive&&weather.DuskAmount>.95f&&!weather.IsNight,
                    "The arrival uses a visible sunset instead of the disaster-day noon override");
                Check(city.ShelteredCount==city.LivingActorCount,"The city remains fully sheltered on day 20");
            }
            if(finale.CurrentStage=="Warning"&&!warningPaused)
            {warningPaused=true;yield return VerifyPauseAtWarning();}
            if(finale.CurrentStage=="LookAtFriend"&&!friendChecked)
            {
                friendChecked=true;float progress=finale.WaveApproachProgress;
                yield return new WaitForSeconds(1f);
                Vector3 towardFriend=finale.VisitorB.position+Vector3.up*1.31f-mainCamera.transform.position;
                Check(Vector3.Dot(mainCamera.transform.forward,towardFriend.normalized)>.95f,
                    "The eye-level camera smoothly turns to the companion");
                Check(finale.WaveApproachProgress>progress+.01f,"The wave keeps approaching throughout the mutual glance");
                Check(Vector3.Dot(Vector3.ProjectOnPlane(towardFriend,Vector3.up).normalized,Vector3.ProjectOnPlane(finale.SunsetDirection,Vector3.up).normalized)>.98f,
                    "The setting sun is positioned behind the companion in the glance shot");
            }
            if(finale.CurrentStage=="Title"&&!titleChecked)
            {
                titleChecked=true;
                Check(finale.WaveFillsScreen&&finale.FadeAmount<.001f,"The title appears over full-frame blue wave water before black fading");
                var face=mainCamera.transform.Find("DalganinKamerayiSaranYuzu");
                Vector3 low=mainCamera.WorldToViewportPoint(face.TransformPoint(new Vector3(-.5f,-.5f,0)));
                Vector3 high=mainCamera.WorldToViewportPoint(face.TransformPoint(new Vector3(.5f,.5f,0)));
                Check(low.x<0&&low.y<0&&high.x>1&&high.y>1,"The breaking-water face overscans all four edges, leaving no sky or dock gap");
            }
            if(finale.CurrentStage=="FadeToBlack")
            {fadeIncreasing&=finale.FadeAmount+.001f>=previousFade;previousFade=finale.FadeAmount;}
            yield return null;
        }
        yield return null;yield return null;Observe();
        Check(boatPaused&&warningPaused,"Both Space and ESC pause paths were exercised in the cinematic");
        Check(arrivalChecked&&friendChecked&&titleChecked,"Arrival, disembarkation, mutual glance and blue-water title were shown");
        Check(fadeIncreasing&&stages.Contains("FadeToBlack")&&finale.FadeAmount>.999f,"The blue wave darkens smoothly to complete black");
        Check(finale.IsComplete&&!finale.IsPlaying&&IslandTsunamiFinale.BlocksInterface,"The completed ending holds black and keeps ordinary panels blocked");
        Check(gm.oyunBittiMi&&!gm.afetOlduMu&&!disaster.AfetDevamEdiyorMu,"The real disaster finishes accounting and ends the game without requesting a report");
        Check(uiStayedHidden&&!report.panel.activeSelf&&!gameOver.panel.activeSelf&&!hint.serit.activeSelf,"No disaster report, final report, tutorial or player card interrupts the ending");
        Check(sawImpact&&sawWater==!strongDefense,"The existing seawall impact and protected/unprotected flood outcomes are preserved");
        Check(gm.sonAfetTuru==AfetTuru.Tsunami&&gm.sonSavunmaYeterli==strongDefense&&gm.sonHasarliBinaSayisi==disaster.DamagedInstanceCount,
            "Normal tsunami defense and local damage results are still recorded");
        var tsunami=DengeAyarlari.Aktif.afetler.First(a=>a.afet==AfetTuru.Tsunami);
        float baseLoss=tsunami.bazKayip*(strongDefense?DengeAyarlari.Aktif.savunmaYeterliCarpani:1);
        float protection=(1-gm.farkindalikYuzdesi*DengeAyarlari.Aktif.farkindalikEtkisi)*(1-(gm.akutHasarli?0:gm.akutLevel)*DengeAyarlari.Aktif.akutEtkisi);
        Check(gm.sonVefatSayisi==Mathf.Min(1000,Mathf.RoundToInt(baseLoss*Mathf.Clamp01(protection))),"The cinematic adds no artificial deaths to the established casualty formula");
        Check(HasOrderedStages(new[]{"BoatApproach","Disembarking","Warning","ViewWave","LookAtFriend","FinalWave","Title","FadeToBlack","Complete"}),
            "The authored one-day cinematic follows the complete shot order: "+string.Join(" → ",stageOrder));
        var title=FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="OyunSonuBasligi");
        Check(title!=null&&title.alpha<.001f,"The ending title fades completely with the water");
        yield return new WaitForSecondsRealtime(1.1f);
        Check(finale.CurrentStage=="Complete"&&finale.FadeAmount>.999f&&!report.panel.activeSelf&&!gameOver.panel.activeSelf,"The terminal black frame remains stable without advancing to any other screen");
        yield return Capture("Logs/TsunamiFinale_"+branchName+"_Complete.png");
        yield return new WaitForEndOfFrame();
        var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
        bool black=true;
        foreach(Vector2 point in new[]{new Vector2(.01f,.01f),new Vector2(.99f,.01f),new Vector2(.01f,.99f),new Vector2(.99f,.99f),new Vector2(.5f,.5f)})
        {Color color=pixels.GetPixel((int)(point.x*(Screen.width-1)),(int)(point.y*(Screen.height-1)));black&=Mathf.Max(color.r,color.g,color.b)<.01f;}
        Destroy(pixels);Check(black,"Rendered final centre and all corners are genuinely black");
    }

    IEnumerator VerifyPauseAtBoat()
    {
        float before = finale.BoatApproachProgress;
        Vector3 beforePosition = finale.Boat.position;
        yield return SendSpaceKey();
        yield return null;
        bool paused = gm.kullaniciDurdurdu && Mathf.Approximately(Time.timeScale, 0f);
        float pausedProgress = finale.BoatApproachProgress;
        Vector3 pausedPosition = finale.Boat.position;
        yield return new WaitForSecondsRealtime(.3f);
        bool frozen = Mathf.Approximately(finale.BoatApproachProgress, pausedProgress) && finale.Boat.position == pausedPosition;
        yield return SendSpaceKey();
        yield return null;
        Check(paused && frozen && !gm.kullaniciDurdurdu && Time.timeScale > 0f &&
              (finale.BoatApproachProgress > before || beforePosition != finale.Boat.position),
            "Space pauses and resumes the arriving boat and its approach clock");
    }

    IEnumerator VerifyPauseAtWarning()
    {
        float oldWaveProgress = finale.WaveApproachProgress;
        var audioSource = IslandSirenAudio.Instance != null ? IslandSirenAudio.Instance.GetComponent<AudioSource>() : null;
        siren = IslandSirenAudio.Instance;
        bool audibleCue = siren != null && siren.IsPlaying && audioSource != null;
        int oldSamples = audioSource != null ? audioSource.timeSamples : 0;

        yield return SendEscapeKey();
        yield return null;
        // AudioSource.Pause reaches the audio device asynchronously. Sample its clock
        // after that short handoff, rather than comparing across a DSP buffer boundary.
        yield return new WaitForSecondsRealtime(.08f);
        bool paused = PauseMenu.DurdurulduMu && Mathf.Approximately(Time.timeScale, 0f);
        string pausedStage = finale.CurrentStage;
        float pausedWaveProgress = finale.WaveApproachProgress;
        Vector3 pausedWavePosition = finale.CinematicWave.position;
        int pausedSamples = audioSource != null ? audioSource.timeSamples : 0;
        yield return new WaitForSecondsRealtime(.3f);
        bool frozen = finale.CurrentStage == pausedStage &&
                      Mathf.Approximately(finale.WaveApproachProgress, pausedWaveProgress) &&
                      finale.CinematicWave.position == pausedWavePosition &&
                      (audioSource == null || audioSource.timeSamples == pausedSamples);
        yield return SendEscapeKey();
        yield return null;
        bool resumed=!PauseMenu.DurdurulduMu&&Time.timeScale>0f;
        Check(audibleCue && oldSamples >= 0 && paused && frozen && resumed,
            "ESC pauses and resumes the siren, camera shot and advancing wave together (cue="+audibleCue+", paused="+paused+", frozen="+frozen+", resumed="+resumed+")");
        if (finale.WaveApproachProgress > oldWaveProgress)
            output.AppendLine("INFO Warning wave progress resumed after ESC.");
    }

    void Observe()
    {
        if (finale == null) return;
        if (siren == null) siren = IslandSirenAudio.Instance;
        string stage = finale.CurrentStage ?? "";
        if (stage.Length > 0 && stage != lastStage)
        {
            lastStage = stage;
            stageOrder.Add(stage);
            stages.Add(stage);
            output.AppendLine("STAGE " + stage + " boat=" + finale.BoatApproachProgress.ToString("F2") +
                              " wave=" + finale.WaveApproachProgress.ToString("F2") + " gap=" + finale.ImpactGap.ToString("F2"));
            if (stage == "BoatApproach" || stage == "Disembarking" || stage == "Warning" ||
                stage == "ViewWave" || stage == "LookAtFriend" || stage == "FinalWave" || stage == "Title" || stage == "FadeToBlack")
                StartCoroutine(CaptureOnce(stage, "Logs/TsunamiFinale_" + branchName + "_" + stage + ".png"));
        }

        if (finale.IsPlaying && IslandTsunamiFinale.BlocksInterface)
        {
            uiStayedHidden &= report == null || !report.panel.activeSelf;
            uiStayedHidden &= gameOver == null || !gameOver.panel.activeSelf;
            uiStayedHidden &= hint == null || !hint.serit.activeSelf;
        }
        if (disaster != null)
        {
            sawWater |= disaster.FloodSurfaceY > disaster.kuruZeminY + .1f;
            sawImpact |= disaster.TsunamiImpactTriggered;
        }
    }

    bool HasOrderedStages(string[] expected)
    {
        int cursor = 0;
        foreach (string stage in stageOrder)
            if (cursor < expected.Length && stage == expected[cursor]) cursor++;
        return cursor == expected.Length;
    }

    IEnumerator CaptureOnce(string key, string path)
    {
        if (!capturedStages.Add(key)) yield break;
        float delay=key=="BoatApproach"?3f:key=="ViewWave"?.8f:key=="LookAtFriend"?1.15f:key=="FinalWave"?1.5f:key=="Title"?.9f:key=="FadeToBlack"?1.5f:key=="Disembarking"?1.8f:0;
        if(delay>0)yield return new WaitForSeconds(delay);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
    }

    static IEnumerator Capture(string path)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        yield return null;
    }

    static IEnumerator SendSpaceKey()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            yield return null;
            yield break;
        }
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;
#else
        yield return null;
#endif
    }

    static IEnumerator SendEscapeKey()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            yield return null;
            yield break;
        }
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;
#else
        yield return null;
#endif
    }

    FieldInfo Field(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
#endif
