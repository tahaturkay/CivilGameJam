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
        gm = GameManager.Instance;
        clock = FindAnyObjectByType<EconomyAndTime>();
        disaster = FindAnyObjectByType<DisasterController>();
        city = FindAnyObjectByType<IslandCityLife>();
        weather = FindAnyObjectByType<IslandWeather>();
        finale = FindAnyObjectByType<IslandTsunamiFinale>();
        siren = IslandSirenAudio.Instance;
        report = FindAnyObjectByType<AfetRaporu>(FindObjectsInactive.Include);
        gameOver = FindAnyObjectByType<OyunSonu>(FindObjectsInactive.Include);
        hint = FindAnyObjectByType<IpucuSistemi>(FindObjectsInactive.Include);
        pauseMenu = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        mainCamera = Camera.main;

        Check(gm != null && clock != null && disaster != null && city != null && finale != null,
            "MainScene has the live game, city, disaster controller and finale");
        Check(report != null && gameOver != null && hint != null && pauseMenu != null,
            "The existing disaster report, game-over report, tutorial and pause menu are present");
        Check(mainCamera != null && weather != null && IslandSirenAudio.Instance != null,
            "Main camera, weather and runtime siren component are present");
        if (gm == null || clock == null || disaster == null || city == null || finale == null ||
            report == null || gameOver == null || hint == null || pauseMenu == null || mainCamera == null || weather == null)
            yield break;

        clock.enabled = false;
        gm.gun = 1;
        gm.hizCarpani = 1;
        gm.nufus = 1000;
        gm.para = 10000;
        gm.farkindalikYuzdesi = 0;
        gm.akutLevel = 1;
        gm.kullaniciDurdurdu = false;
        gm.oyunDurduMu = false;
        gm.afetSuruyor = false;
        gm.afetOlduMu = false;
        gm.oyunBittiMi = false;
        gm.kazandiMi = false;
        pauseMenu.Devam();
        FindAnyObjectByType<IslandSimulationPause>()?.ApplyPause();
        Time.timeScale = 1f;
        yield return null;
        yield return null;

        int tsunamiDay = IslandDisasterCalendar.TsunamiDay;
        Check(tsunamiDay > 1 && tsunamiDay == DengeAyarlari.Aktif.toplamGun,
            "The authored tsunami remains the final scheduled day");
        if (tsunamiDay <= 1) yield break;

        gm.LevelAyarla(BinaTuru.DenizSeti, strongDefense ? 3 : 1);
        gm.HasarliAyarla(BinaTuru.DenizSeti, false);
        gm.LevelAyarla(BinaTuru.Akut, 1);
        gm.HasarliAyarla(BinaTuru.Akut, false);

        FieldInfo dayCounter = Field(clock, "gunSayaci");
        Check(dayCounter != null, "The real day clock exposes its runtime time-of-day counter to the focused probe");
        if (dayCounter == null) yield break;
        dayCounter.SetValue(clock, DengeAyarlari.Aktif.birGunSuresi * .995f);
        gm.gun = tsunamiDay - 1;
        mooring = city.transform.Find("SabitAlanlar/Liman/CinematicMarkers/BoatMooring");
        Check(finale.CanPlay && mooring != null, "Installed harbor markers make the real finale playable");
        if (!finale.CanPlay || mooring == null) yield break;

        Check(!finale.HasPlayed && !finale.IsPlaying && !IslandTsunamiFinale.BlocksInterface,
            "Finale has not started and the ordinary HUD stays available on the warning day");
        float boatDeadline = Time.realtimeSinceStartup + 12f;
        bool checkedBoatPause = false;
        while (Time.realtimeSinceStartup < boatDeadline && finale.CurrentStage != "Moored")
        {
            Observe();
            if (!checkedBoatPause && finale.CurrentStage == "BoatApproach")
            {
                checkedBoatPause = true;
                yield return VerifyPauseAtBoat();
            }
            yield return null;
        }
        Observe();

        Check(stages.Contains("BoatApproach") && stages.Contains("Moored") && finale.BoatApproachProgress >= .999f,
            "The harbor boat moves in on day 19 and reaches its mooring");
        Check(finale.VisitorA != null && finale.VisitorB != null && finale.VisitorA.gameObject.activeInHierarchy && finale.VisitorB.gameObject.activeInHierarchy &&
              Vector3.Distance(finale.VisitorA.position, finale.Boat.position) < 14f && Vector3.Distance(finale.VisitorB.position, finale.Boat.position) < 14f,
            "Both named visitors arrive on the moored fishing boat");
        Check(city.TsunamiShelterOrdered && city.CurrentMode == IslandCityLife.CityLifeMode.Shelter,
            "The prior-day tsunami warning sends residents and animals to shelter");
        Check(weather.DisasterDaylightActive && !weather.IsNight,
            "The approach and warning day stay visibly in daylight");
        Check(mainCamera.orthographic && !finale.IsPlaying &&
              mainCamera.GetComponent<IslandCameraController>() != null && mainCamera.GetComponent<IslandCameraController>().enabled,
            "The city camera and navigation are restored after the day-19 harbor escort");
        yield return Capture("Logs/TsunamiFinale_" + branchName + "_Boat.png");

        if (!checkedBoatPause)
        {
            Check(false, "Space pause was exercised during the boat approach");
        }

        gm.gun = tsunamiDay;
        float startDeadline = Time.realtimeSinceStartup + 3f;
        while (!finale.IsPlaying && Time.realtimeSinceStartup < startDeadline)
        {
            Observe();
            yield return null;
        }
        Observe();
        Check(finale.HasPlayed && finale.IsPlaying && IslandTsunamiFinale.BlocksInterface,
            "The real tsunami day starts one blocking cinematic and cannot replay it");
        Check(!report.panel.activeSelf && !gameOver.panel.activeSelf && !hint.serit.activeSelf,
            "No report, game-over card or tutorial covers the opening shot");
        Check(city.ShelteredCount == city.LivingActorCount,
            "All residents and animals are inside safe buildings before the tsunami arrives");

        bool checkedWarningPause = false;
        bool checkedView = false;
        bool checkedFriend = false;
        bool checkedWaveRenderer = false;
        float previousLookProgress = -1f;
        float lookProgressAfterWait = -1f;
        float reportDeadline = Time.realtimeSinceStartup + 22f;
        while (Time.realtimeSinceStartup < reportDeadline && !report.panel.activeSelf)
        {
            Observe();

            if (finale.CurrentStage == "Warning" && !checkedWarningPause)
            {
                checkedWarningPause = true;
                yield return VerifyPauseAtWarning();
            }
            if (finale.CurrentStage == "ViewWave" && !checkedView)
            {
                checkedView = true;
                Vector3 toWave = finale.CinematicWave.position - mainCamera.transform.position;
                Check(!mainCamera.orthographic && Vector3.Dot(mainCamera.transform.forward, toWave.normalized) > .72f,
                    "First point-of-view shot looks directly toward the visible tsunami");
            }
            if (finale.CurrentStage == "LookAtFriend" && !checkedFriend)
            {
                checkedFriend = true;
                previousLookProgress = finale.WaveApproachProgress;
                yield return new WaitForSecondsRealtime(.65f);
                Vector3 toFriend = finale.VisitorB.position + Vector3.up * 1.3f - mainCamera.transform.position;
                Check(!mainCamera.orthographic && Vector3.Dot(mainCamera.transform.forward, toFriend.normalized) > .55f,
                    "The camera completes its turn from the wave to the companion");
                lookProgressAfterWait = finale.WaveApproachProgress;
                Check(lookProgressAfterWait > previousLookProgress + .02f,
                    "The tsunami keeps closing in while the visitors look at each other");
            }
            if ((finale.CurrentStage == "FinalWave" || finale.CurrentStage == "CutToBlack") && !checkedWaveRenderer)
            {
                checkedWaveRenderer = true;
                bool cinematicVisible = finale.CinematicWave != null && finale.CinematicWave.gameObject.activeInHierarchy &&
                                        finale.CinematicWave.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled);
                bool normalHidden = disaster.tsunamiDalgasi == null || !disaster.tsunamiDalgasi.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled);
                Check(cinematicVisible && normalHidden,
                    "The cinematic water is visible while the normal gameplay wave is hidden");
            }

            if (finale.CurrentStage == "CutToBlack")
            {
                closestImpactGap = Mathf.Min(closestImpactGap, finale.ImpactGap);
                Check(finale.ImpactGap > 0f && finale.ImpactGap <= 1.1f && finale.VisitorA != null && finale.VisitorB != null,
                    "A hard cut stops the approaching crest close to, but before, the two visitors");
            }
            if (finale.CurrentStage == "Title")
                yield return CaptureOnce("Title", "Logs/TsunamiFinale_" + branchName + "_Title.png");

            if (IslandTsunamiFinale.BlocksInterface)
                uiStayedHidden &= !report.panel.activeSelf && !gameOver.panel.activeSelf && !hint.serit.activeSelf;
            sawWater |= disaster.FloodSurfaceY > disaster.kuruZeminY + .1f;
            sawImpact |= disaster.TsunamiImpactTriggered;
            if (finale.ImpactGap > 0 && finale.ImpactGap < closestImpactGap) closestImpactGap = finale.ImpactGap;
            yield return null;
        }
        Observe();

        Check(checkedWarningPause, "ESC pauses and resumes the active siren shot without advancing it");
        Check(checkedView && checkedFriend && stages.Contains("FinalWave"),
            "The visitor scene follows the wave, companion glance, and final return to the wave");
        Check(checkedWaveRenderer && sawImpact,
            "The real tsunami still runs its existing seawall impact during the cutscene");
        Check(uiStayedHidden, "No report, game-over card or tutorial appears over any cinematic frame");
        Check(stages.Contains("Title"), "The ending title appears after the pre-impact cut");
        Check(report.panel.activeSelf && !gameOver.panel.activeSelf && !IslandTsunamiFinale.BlocksInterface,
            "The ordinary tsunami report appears only after the title and releases interface blocking");
        if (!report.panel.activeSelf) yield break;
        Check(sawWater == !strongDefense,
            "The existing seawall branch keeps its original flood-water outcome (protected=" + strongDefense + ")");

        yield return Capture("Logs/TsunamiFinale_" + branchName + "_TsunamiReport.png");
        Check(gm.sonAfetTuru == AfetTuru.Tsunami && gm.sonSavunmaYeterli == strongDefense &&
              gm.sonHasarliBinaSayisi == disaster.DamagedInstanceCount,
            "The existing tsunami report keeps the actual defense and structure-damage results");

        var tsunami = DengeAyarlari.Aktif.afetler.First(a => a.afet == AfetTuru.Tsunami);
        float baseLoss = tsunami.bazKayip * (strongDefense ? DengeAyarlari.Aktif.savunmaYeterliCarpani : 1f);
        float protection = (1f - gm.farkindalikYuzdesi * DengeAyarlari.Aktif.farkindalikEtkisi) *
                           (1f - (gm.akutHasarli ? 0 : gm.akutLevel) * DengeAyarlari.Aktif.akutEtkisi);
        int expectedLoss = Mathf.Min(1000, Mathf.RoundToInt(baseLoss * Mathf.Clamp01(protection)));
        Check(gm.sonVefatSayisi == expectedLoss,
            "The cutscene adds no deaths or changes to the existing casualty formula");

        report.Devam();
        yield return null;
        yield return null;
        Check(gm.oyunBittiMi && gameOver.panel.activeSelf && finale.CurrentStage == "Complete",
            "Continuing from the tsunami report opens the existing final game report in order");
        Check(!report.panel.activeSelf && !IslandTsunamiFinale.BlocksInterface,
            "The game-over report follows the tsunami report without a duplicate overlay");
        yield return Capture("Logs/TsunamiFinale_" + branchName + "_GameOver.png");

        Check(HasOrderedStages(new[] { "BoatApproach", "Moored", "Disembarking", "Warning", "ViewWave", "LookAtFriend", "FinalWave", "CutToBlack", "Title" }),
            "Recorded scene stages follow the complete authored order: " + string.Join(" → ", stageOrder));
        Check(Mathf.Abs(closestImpactGap - .70f) < .03f,
            "Closest recorded cinematic impact gap remains 0.70 world units");
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
        Check(audibleCue && oldSamples >= 0 && paused && frozen && !PauseMenu.DurdurulduMu && Time.timeScale > 0f,
            "ESC pauses and resumes the siren, camera shot and advancing wave together");
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
            if (stage == "BoatApproach" || stage == "Moored" || stage == "Disembarking" || stage == "Warning" ||
                stage == "ViewWave" || stage == "LookAtFriend" || stage == "FinalWave" || stage == "CutToBlack" || stage == "Title")
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
        float delay=key=="ViewWave"?.8f:key=="LookAtFriend"?.85f:key=="FinalWave"?1.25f:key=="Title"?.8f:key=="Disembarking"?1.8f:0;
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
