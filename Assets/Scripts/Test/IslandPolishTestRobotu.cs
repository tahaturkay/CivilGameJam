#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Focused Play Mode regression checks for the island polish features.</summary>
public sealed class IslandPolishTestRobotu : MonoBehaviour
{
    readonly StringBuilder reportText = new StringBuilder();
    GameManager gm;
    DisasterController disaster;
    IslandCityLife city;
    bool failed;

    IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return null;
        yield return null;

        gm = GameManager.Instance;
        disaster = FindAnyObjectByType<DisasterController>();
        city = FindAnyObjectByType<IslandCityLife>();
        if (gm == null || disaster == null || city == null)
        {
            Fail("MainScene is missing GameManager, DisasterController or IslandCityLife.");
        }
        else
        {
            gm.kullaniciDurdurdu = true;
            gm.oyunDurduMu = false;
            gm.afetOlduMu = false;
            gm.oyunBittiMi = false;
            gm.gun = 1;
            gm.nufus = 1000;
            gm.para = 50000;
            gm.farkindalikYuzdesi = 100f;
            gm.altyapiHasarli = false;
            Time.timeScale = 3f;

            IEnumerator run = RunChecks();
            while (true)
            {
                object next;
                try
                {
                    if (!run.MoveNext()) break;
                    next = run.Current;
                }
                catch (Exception ex)
                {
                    Fail(ex.ToString());
                    break;
                }
                yield return next;
            }
        }

        RestorePlayState();
        reportText.AppendLine(failed ? "FAIL: Island polish checks did not pass." : "PASS: All island polish checks passed.");
        WriteReport();
        UnityEditor.EditorApplication.isPlaying = false;
    }

    IEnumerator RunChecks()
    {
        Check(city.CitizenCount == 95 && city.AnimalCount == 5 && city.LivingActorCount == 100,
            "95 residents and 5 street animals are present");

        IslandCameraController cameraRig = Camera.main != null ? Camera.main.GetComponent<IslandCameraController>() : null;
        if (cameraRig != null && Camera.main != null)
        {
            cameraRig.FrameCity();
            Check(cameraRig.startOrthographicSize <= 60f && Camera.main.orthographicSize <= 60f,
                "The opening camera stays close enough to read the city");
        }
        else Check(false, "The main camera has IslandCameraController");

        IslandNightLighting night = FindAnyObjectByType<IslandNightLighting>();
        EconomyAndTime clock = FindAnyObjectByType<EconomyAndTime>();
        FieldInfo clockCounter = typeof(EconomyAndTime).GetField("gunSayaci", BindingFlags.Instance | BindingFlags.NonPublic);
        if (night != null && clock != null && clockCounter != null)
        {
            float oldCounter = (float)clockCounter.GetValue(clock);
            int oldSpeed = gm.hizCarpani;
            gm.hizCarpani = 1;
            float dayLength = DengeAyarlari.Aktif.birGunSuresi;
            clockCounter.SetValue(clock, dayLength * .3f);
            yield return null;
            yield return null;
            Check(clock.DayProgress > .28f && clock.DayProgress < .32f && night.NightAmount < .05f,
                "Daylight follows the day clock");
            yield return CaptureGameView("Logs/AdaPolish_Day.png");

            clockCounter.SetValue(clock, dayLength * .8f);
            yield return null;
            yield return null;
            float yawBefore = night.LighthouseYaw;
            yield return new WaitForSecondsRealtime(.2f);
            Check(night.NightAmount > .8f && night.StreetLampCount > 0 &&
                  night.HeadlightVehicleCount >= city.VehicleCount && night.RealLightBudget <= 9,
                "Night activates city and vehicle lights within the local-light budget");
            Check(Mathf.Abs(Mathf.DeltaAngle(yawBefore, night.LighthouseYaw)) > 1f,
                "The lighthouse beam continues rotating at night");
            Check(HasLitWindowMaterial(GameObject.Find("LowPolyAda")),
                "Building window materials emit light during the night");
            if (cameraRig != null)
            {
                cameraRig.FrameHome();
                yield return null;
                yield return CaptureGameView("Logs/AdaPolish_Night.png");
                cameraRig.FrameCity();
                yield return null;
            }
            else Check(false, "The lighthouse night view can be framed");
            clockCounter.SetValue(clock, oldCounter);
            gm.hizCarpani = oldSpeed;
        }
        else Check(false, "Night lighting and the day clock are configured");

        yield return CheckSchoolAndQuizLayout();
        yield return CheckOpaqueFlood();
        yield return CheckRoadRepairStopsAndRestartsTraffic();
        CheckConstructionSignsStayOutsideRoadLanes();

        yield return ProbeAwareness(0f, 0, 0, true, "0 awareness / fire");
        yield return ProbeAwareness(39.99f, 3, 0, true, "39.99 awareness / tsunami");
        yield return ProbeAwareness(60f, 1, 0, true, "60 awareness / flood");
        yield return ProbeAwareness(60.01f, 1, 1, false, "60.01 awareness / flood");
        yield return ProbeAwareness(79.99f, 0, 1, false, "79.99 awareness / fire");
        yield return ProbeAwareness(80f, 2, 1, false, "80 awareness / earthquake");
        yield return ProbeAwareness(80.01f, 2, 2, false, "80.01 awareness / earthquake");
        yield return ProbeAwareness(100f, 3, 2, false, "100 awareness / tsunami");
    }

    IEnumerator CheckSchoolAndQuizLayout()
    {
        BinaPopup popup = BinaPopup.Instance;
        QuizManager quiz = QuizManager.Instance;
        if (popup == null || quiz == null)
        {
            Check(false, "School popup and quiz UI are present");
            yield break;
        }

        popup.Ac(BinaTuru.Okul);
        yield return null;
        RectTransform schoolCard = popup.panel != null && popup.panel.transform.Find("Pencere") != null
            ? popup.panel.transform.Find("Pencere").GetComponent<RectTransform>() : null;
        Check(schoolCard != null && schoolCard.sizeDelta.x >= 700f && schoolCard.sizeDelta.y >= 460f,
            "The school popup has its expanded reading layout");
        Check(schoolCard != null && FitsSafeArea(schoolCard), "The school popup fits inside the current safe area");
        yield return CaptureGameView("Logs/AdaPolish_School.png");
        yield return new WaitForSecondsRealtime(.22f);
        popup.Kapat();

        float originalAwareness=gm.farkindalikYuzdesi;gm.farkindalikYuzdesi=10;
        quiz.QuizAc();
        yield return null;
        RectTransform quizCard = quiz.quizPanel != null && quiz.quizPanel.transform.Find("Pencere") != null
            ? quiz.quizPanel.transform.Find("Pencere").GetComponent<RectTransform>() : null;
        Check(quizCard != null && quizCard.sizeDelta.x >= 1000f && quizCard.sizeDelta.y >= 720f,
            "The quiz has expanded question and answer space");
        Check(quizCard != null && FitsSafeArea(quizCard), "The quiz fits inside the current safe area");
        yield return CaptureGameView("Logs/AdaPolish_Quiz.png");
        WriteLine("UI safe-area sample: " + Screen.width + "x" + Screen.height + ", rect=" + Screen.safeArea);
        quiz.QuizKapat();
        gm.farkindalikYuzdesi=originalAwareness;
    }

    IEnumerator CheckOpaqueFlood()
    {
        ResetEventState();
        disaster.AfetBaslat(1);
        yield return null;
        yield return null;
        Renderer water = disaster.tsunamiSuyu != null ? disaster.tsunamiSuyu.GetComponent<Renderer>() : null;
        Color waterColor = water != null && water.sharedMaterial != null && water.sharedMaterial.HasProperty("_BaseColor")
            ? water.sharedMaterial.GetColor("_BaseColor") : Color.clear;
        Check(water != null && disaster.tsunamiSuyu.gameObject.activeSelf && waterColor.a > .98f &&
              waterColor.b >= waterColor.r && waterColor.g >= waterColor.r,
            "Flood water is opaque and uses a dirty light-blue color");
        disaster.enabled = false;
        yield return null;
        disaster.enabled = true;
        yield return null;
        yield return null;
    }

    IEnumerator ProbeAwareness(float awareness, int incidentIndex, int expectedBand, bool lowAwareness, string label)
    {
        ResetEventState();
        gm.farkindalikYuzdesi = awareness;
        disaster.AfetBaslat(incidentIndex);
        yield return null;
        yield return null;

        Check(disaster.AfetDevamEdiyorMu && city.AwarenessBandAtIncident == expectedBand,
            label + ": awareness bracket captured at incident start");
        ActorModeCounts modes = ReadActorModes();
        if(incidentIndex==3||(incidentIndex==1&&!lowAwareness))
        {
            Check(city.WrongResponseCount==0&&modes.shelter==city.LivingActorCount,
                label+": all residents and animals shelter indoors");
        }
        else if(incidentIndex==1)
        {
            Check(modes.panic==city.LivingActorCount&&modes.assembly==0,
                label+": low-awareness flood response remains on the streets");
        }
        else if (lowAwareness)
        {
            int routedWrong = incidentIndex == 0 || incidentIndex == 2 ? modes.shelter : modes.panic;
            Check(city.WrongResponseCount > city.CitizenCount / 2 && city.WrongResponseCount < city.CitizenCount &&
                  routedWrong == city.WrongResponseCount && modes.assembly > 0,
                label + ": most, but not all, people choose an unsafe response");
        }
        else if (expectedBand == 1)
        {
            Check(city.WrongResponseCount == 0 && city.PanickedAssemblyCount == city.CitizenCount &&
                  modes.assembly == city.LivingActorCount,
                label + ": everyone gathers while residents remain panicked");
        }
        else
        {
            Check(city.WrongResponseCount == 0 && city.PanickedAssemblyCount == 0 &&
                  modes.assembly == city.LivingActorCount,
                label + ": everyone gathers calmly");
        }

        disaster.enabled = false;
        yield return null;
        disaster.enabled = true;
        yield return null;
        yield return null;
        gm.farkindalikYuzdesi = 100f;
    }

    void ResetEventState()
    {
        if (!disaster.enabled) disaster.enabled = true;
        gm.afetOlduMu = false;
        gm.afetSuruyor = false;
        gm.oyunDurduMu = false;
        gm.oyunBittiMi = false;
        gm.kullaniciDurdurdu = true;
        gm.gun = 1;
        gm.farkindalikYuzdesi = 100f;
    }

    IEnumerator CheckRoadRepairStopsAndRestartsTraffic()
    {
        Transform actors = city.transform.Find("SehirYasami");
        Transform car = actors != null ? actors.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("AdaAraci")) : null;
        RoadNetworkState roads = FindAnyObjectByType<RoadNetworkState>();
        if (car == null || roads == null)
        {
            Check(false, "Road state and a vehicle are present for traffic repair checks");
            yield break;
        }

        gm.altyapiHasarli = true;
        yield return null;
        Vector3 before = car.position;
        gm.kullaniciDurdurdu = false;
        yield return new WaitForSecondsRealtime(.25f);
        gm.kullaniciDurdurdu = true;
        Check(city.TrafficBlocked && Vector3.Distance(before, car.position) < .02f,
            "Cars stop while damaged roads await repair");

        gm.para = 50000;
        Check(Ekonomi.Onar(BinaTuru.Altyapi), "Existing repair action clears road damage");
        yield return null;
        before = car.position;
        gm.kullaniciDurdurdu = false;
        yield return new WaitForSecondsRealtime(.3f);
        gm.kullaniciDurdurdu = true;
        Check(!city.TrafficBlocked && Vector3.Distance(before, car.position) > .04f,
            "Cars resume once the roads have been repaired");
        gm.altyapiHasarli = false;
    }

    void CheckConstructionSignsStayOutsideRoadLanes()
    {
        RoadNetworkState roadState = FindAnyObjectByType<RoadNetworkState>();
        if (roadState == null)
        {
            Check(false, "Road network state is present");
            return;
        }

        Transform markerRoot = roadState.transform.Find("HasarliYolOnarimNoktalari");
        BuildingVisuals[] roadObjects = roadState.GetComponentsInChildren<BuildingVisuals>(true)
            .Where(b => b.GetComponent<BoxCollider>() != null).ToArray();
        Transform[] groups = markerRoot != null
            ? markerRoot.Cast<Transform>().Where(t => t.name.StartsWith("OnarimIsaretleri")).ToArray()
            : new Transform[0];
        if (groups.Length == 0 || groups.Length != roadObjects.Length * 3)
        {
            Check(false, "Each road section has three corresponding repair marker groups");
            return;
        }

        var signTypes = new HashSet<string>();
        bool allShoulder = true;
        for (int i = 0; i < groups.Length; i++)
        {
            BoxCollider road = roadObjects[i / 3].GetComponent<BoxCollider>();
            Transform[] markers = groups[i].GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "OnarimDubasi" || t.name == "YolCalismasi" ||
                            t.name == "DikkatBozukYol" || t.name == "Yavasla").ToArray();
            foreach (Transform marker in markers)
            {
                if (marker.name != "OnarimDubasi") signTypes.Add(marker.name);
                float localX = road.transform.InverseTransformPoint(marker.position).x;
                if (Mathf.Abs(localX - road.center.x) <= road.size.x * .5f + .08f)
                    allShoulder = false;
            }
        }
        Check(allShoulder, "Repair signs and cones sit outside their road lanes");
        Check(signTypes.Count >= 2, "Road repairs use more than one relevant traffic-sign type");
    }

    struct ActorModeCounts
    {
        public int shelter, panic, assembly;
    }

    ActorModeCounts ReadActorModes()
    {
        FieldInfo listField = typeof(IslandCityLife).GetField("citizens", BindingFlags.Instance | BindingFlags.NonPublic);
        IList actors = listField != null ? listField.GetValue(city) as IList : null;
        if (actors == null) throw new InvalidOperationException("IslandCityLife actor list is unavailable for incident behavior checks.");

        ActorModeCounts counts = new ActorModeCounts();
        for (int i = 0; i < actors.Count; i++)
        {
            object actor = actors[i];
            Type actorType = actor.GetType();
            bool animal = (bool)actorType.GetField("isAnimal").GetValue(actor);
            object mode = actorType.GetField("mode").GetValue(actor);
            string modeName = mode.ToString();
            if (modeName == "Shelter") counts.shelter++;
            else if (modeName == "Panic") counts.panic++;
            else if (modeName == "Assembly") counts.assembly++;
            if (!animal && modeName == "Normal")
                throw new InvalidOperationException("A resident remained in normal mode during an incident.");
        }
        return counts;
    }

    static bool FitsSafeArea(RectTransform rect)
    {
        if (!RectFitsSafeArea(rect)) return false;
        foreach (Button button in rect.GetComponentsInChildren<Button>(true))
            if (button.gameObject.activeInHierarchy && !RectFitsSafeArea(button.GetComponent<RectTransform>())) return false;
        return true;
    }

    static bool RectFitsSafeArea(RectTransform rect)
    {
        if (rect == null) return false;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Rect safe = Screen.safeArea;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            if (point.x < safe.xMin - 2f || point.x > safe.xMax + 2f ||
                point.y < safe.yMin - 2f || point.y > safe.yMax + 2f) return false;
        }
        return true;
    }

    static bool HasLitWindowMaterial(GameObject island)
    {
        if (island == null) return false;
        foreach (Renderer renderer in island.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.HasProperty("_EmissionColor") &&
                    material.GetColor("_EmissionColor").maxColorComponent > .05f &&
                    material.name.Contains("Gece Runtime")) return true;
        return false;
    }

    void Check(bool pass, string message)
    {
        string line = (pass ? "PASS: " : "FAIL: ") + message;
        reportText.AppendLine(line);
        if (!pass) failed = true;
        WriteReport();
    }

    IEnumerator CaptureGameView(string relativePath)
    {
        Directory.CreateDirectory("Logs");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(relativePath);
        // CaptureScreenshot completes asynchronously after this rendered frame.
        yield return null;
        yield return null;
    }

    void Fail(string message)
    {
        failed = true;
        reportText.AppendLine("FAIL: " + message);
        WriteReport();
    }

    void WriteLine(string message)
    {
        reportText.AppendLine(message);
        WriteReport();
    }

    void WriteReport()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/AdaPolishTesti.txt", reportText.ToString());
    }

    void RestorePlayState()
    {
        if (disaster != null && !disaster.enabled) disaster.enabled = true;
        if (gm != null)
        {
            gm.afetSuruyor = false;
            gm.afetOlduMu = false;
            gm.oyunDurduMu = false;
            gm.kullaniciDurdurdu = true;
            gm.farkindalikYuzdesi = 100f;
            gm.altyapiHasarli = false;
        }
        Time.timeScale = 1f;
    }
}
#endif
