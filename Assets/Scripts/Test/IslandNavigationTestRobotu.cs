#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Play Mode checks for the solar cycle and bounded island camera controls.</summary>
public sealed class IslandNavigationTestRobotu : MonoBehaviour
{
    readonly StringBuilder report = new StringBuilder();
    bool failed;
    int checks;
    GameManager gm;
    EconomyAndTime clock;
    IslandWeather weather;
    IslandCameraController cameraRig;
    DisasterController disaster;
    FieldInfo clockCounter;
    float dayLength;
    float originalCounter;
    int originalSpeed;
    float originalTimeScale;
    bool originalUserPause, originalGamePause, originalIncident, originalGameOver;
    int originalMoney, originalDay;
    float originalAwareness;
    bool originalDisasterReported, originalWeatherEnabled, originalEventSystemEnabled;
    EventSystem eventSystem;

    IEnumerator Start()
    {
        Application.runInBackground = true;
        Directory.CreateDirectory("Logs");
        yield return null;
        yield return null;

        bool ready = false;
        try { Prepare(); ready = true; }
        catch (Exception ex) { RecordFailure("Test setup exception: " + ex); }

        if (ready)
        {
            IEnumerator checksRun = DriveNested(RunChecks());
            while (true)
            {
                object next;
                bool moved;
                try
                {
                    moved = checksRun.MoveNext();
                    next = moved ? checksRun.Current : null;
                }
                catch (Exception ex)
                {
                    RecordFailure("Unhandled test exception: " + ex);
                    break;
                }
                if (!moved) break;
                yield return next;
            }
        }

        RestoreState();
        report.AppendLine(failed ? "FAIL: Island navigation checks did not pass." : "PASS: Island navigation checks passed.");
        WriteReport("Logs/AdaDonguKameraTesti.txt");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    void Prepare()
    {
        gm = GameManager.Instance;
        clock = FindAnyObjectByType<EconomyAndTime>();
        weather = FindAnyObjectByType<IslandWeather>();
        disaster = FindAnyObjectByType<DisasterController>();
        cameraRig = Camera.main != null ? Camera.main.GetComponent<IslandCameraController>() : null;
        clockCounter = typeof(EconomyAndTime).GetField("gunSayaci", BindingFlags.Instance | BindingFlags.NonPublic);
        dayLength = DengeAyarlari.Aktif != null ? DengeAyarlari.Aktif.birGunSuresi : 0f;

        if (gm == null || clock == null || weather == null || disaster == null || cameraRig == null ||
            clockCounter == null || dayLength <= .1f)
            throw new InvalidOperationException("MainScene is missing the game clock, weather, camera, disaster controller, or day-length setting.");

        originalCounter = (float)clockCounter.GetValue(clock);
        originalSpeed = gm.hizCarpani;
        originalTimeScale = Time.timeScale;
        originalUserPause = gm.kullaniciDurdurdu;
        originalGamePause = gm.oyunDurduMu;
        originalIncident = gm.afetSuruyor;
        originalDisasterReported = gm.afetOlduMu;
        originalGameOver = gm.oyunBittiMi;
        originalMoney = gm.para;
        originalAwareness = gm.farkindalikYuzdesi;
        originalDay = gm.gun;
        originalWeatherEnabled = weather.enabled;
        eventSystem = EventSystem.current;
        originalEventSystemEnabled = eventSystem != null && eventSystem.enabled;

        gm.kullaniciDurdurdu = true;
        gm.oyunDurduMu = false;
        gm.afetSuruyor = false;
        gm.afetOlduMu = false;
        gm.oyunBittiMi = false;
        gm.para = 500;
        gm.farkindalikYuzdesi = 10;
        gm.gun = 1;
        gm.hizCarpani = 1;
        Time.timeScale = 3f;
    }

    IEnumerator RunChecks()
    {
        Check(cameraRig != null && Camera.main.orthographic, "Main camera is wired for orthographic island navigation");
        Check(weather.enabled && weather.transform.Find("KameraArkasindakiYumusakBulutlar") != null,
            "Weather initializes one persistent sky layer without rebuilding island art");

        float frozenProgress = clock.DayProgress;
        foreach (int speed in new[] { 1, 3, 2 })
        {
            gm.hizCarpani = speed;
            yield return null;
            yield return null;
        }
        Check(gm.kullaniciDurdurdu && Mathf.Abs(clock.DayProgress - frozenProgress) < .00001f,
            "Changing 1x, 3x and 2x speed does not move the paused day clock");

        float maximumCurveStep = 0f;
        IslandSolarCycle.Sample previousSample = IslandSolarCycle.Evaluate(0f);
        for (int i = 1; i <= 1000; i++)
        {
            IslandSolarCycle.Sample sample = IslandSolarCycle.Evaluate(i / 1000f);
            maximumCurveStep = Mathf.Max(maximumCurveStep, Mathf.Abs(sample.night - previousSample.night));
            previousSample = sample;
        }
        IslandSolarCycle.Sample atThirty = IslandSolarCycle.Evaluate(.3f);
        IslandSolarCycle.Sample atEighty = IslandSolarCycle.Evaluate(.8f);
        Check(atThirty.night < .001f && atEighty.night > .999f && maximumCurveStep < .006f,
            "Solar darkness changes smoothly through daylight and reaches the requested .3/.8 endpoints (max step " + maximumCurveStep.ToString("F4") + ")");
        float seamNightGap = Mathf.Abs(IslandSolarCycle.Evaluate(.99999f).night - IslandSolarCycle.Evaluate(.00001f).night);
        Check(seamNightGap < .001f, "Solar darkness and dawn are continuous across midnight");

        yield return TestCameraNavigation();
        yield return TestSolarWeatherAndCaptures();
        yield return TestClickOwnership();
        yield return TestBlockedInput();

        IslandCursor cursor = Camera.main != null ? Camera.main.GetComponent<IslandCursor>() : null;
        Check(cursor != null && cursor.pointer != null && cursor.pointer.width == 64 && cursor.pointer.height == 64 &&
              cursor.hotspot == new Vector2(6, 5) && cursor.Applied,
            "The 64×64 lighthouse cursor is applied with hotspot (6, 5)");
    }

    IEnumerator TestCameraNavigation()
    {
        Camera camera = Camera.main;
        Vector3 start = camera.transform.position;
        float height = start.y;
        for (int i = 0; i < 24; i++) cameraRig.ApplyKeyboardPan(Vector2.right, 1f / 60f);
        Vector3 moving = camera.transform.position;
        for (int i = 0; i < 120; i++) cameraRig.ApplyKeyboardPan(Vector2.zero, 1f / 60f);
        Vector3 stopped = camera.transform.position;
        for (int i = 0; i < 60; i++) cameraRig.ApplyKeyboardPan(Vector2.zero, 1f / 60f);
        Check((new Vector2(moving.x - start.x, moving.z - start.z)).magnitude > .1f &&
              Mathf.Abs(camera.transform.position.y - height) < .001f &&
              Vector3.Distance(stopped, camera.transform.position) < .01f,
            "Keyboard pan moves smoothly on X/Z and settles without height drift or residual overshoot");

        Vector2 oldExtents = cameraRig.boundsHalfExtents;
        cameraRig.boundsHalfExtents = new Vector2(5f, 5f);
        cameraRig.FrameCity();
        for (int i = 0; i < 480; i++) cameraRig.ApplyKeyboardPan(new Vector2(1f, 1f), 1f / 60f);
        Vector3 focus = CameraFocus(camera, cameraRig.boundsCenter.y);
        bool inBounds = Mathf.Abs(focus.x - cameraRig.boundsCenter.x) <= 5.2f &&
                        Mathf.Abs(focus.z - cameraRig.boundsCenter.z) <= 5.2f;
        cameraRig.boundsHalfExtents = oldExtents;
        cameraRig.FrameCity();
        Check(inBounds, "Camera focus remains inside configured island bounds");

        Vector3 panStart = camera.transform.position;
        Vector2 screenCenter = new Vector2(camera.pixelWidth * .5f, camera.pixelHeight * .5f);
        cameraRig.ApplyPointerPan(screenCenter, screenCenter + new Vector2(55f, 34f));
        Vector3 panEnd = camera.transform.position;
        Check(Vector3.Distance(panStart, panEnd) > .1f && Mathf.Abs(panStart.y - panEnd.y) < .001f &&
              IslandCameraController.SuppressWorldClick,
            "Pointer drag pans the map and claims the pointer for that gesture");

        float initialZoom = camera.orthographicSize;
        Vector2 cursorPoint = new Vector2(camera.pixelWidth * .69f, camera.pixelHeight * .43f);
        Vector3 anchoredBefore = CameraGroundPoint(camera, cursorPoint, cameraRig.boundsCenter.y);
        cameraRig.ApplyScrollDelta(new Vector2(0f, 1f), cursorPoint, false);
        Check(Mathf.Abs(camera.orthographicSize - initialZoom) < .001f,
            "Scroll zoom animates instead of changing the view in one step");
        for (int i = 0; i < 50; i++) yield return null;
        Vector3 anchoredAfter = CameraGroundPoint(camera, cursorPoint, cameraRig.boundsCenter.y);
        Check(camera.orthographicSize < initialZoom && Vector3.Distance(anchoredBefore, anchoredAfter) < .15f,
            "Smooth cursor zoom preserves the world point under the pointer");

        for (int i = 0; i < 18; i++)
        {
            cameraRig.ApplyScrollDelta(new Vector2(0f, 3f), screenCenter, false);
            yield return null;
        }
        Check(camera.orthographicSize >= cameraRig.minOrthographicSize - .001f,
            "Extreme positive scroll remains clamped at minimum zoom");
        for (int i = 0; i < 22; i++)
        {
            cameraRig.ApplyScrollDelta(new Vector2(0f, -3f), screenCenter, false);
            yield return null;
        }
        Check(camera.orthographicSize <= cameraRig.maxOrthographicSize + .001f,
            "Extreme negative scroll remains clamped at maximum zoom");

        Vector3 beforeTrackpad = camera.transform.position;
        cameraRig.ApplyScrollDelta(new Vector2(1f, 1f), screenCenter, true);
        Check(Vector3.Distance(beforeTrackpad, camera.transform.position) > .1f,
            "Shift+two-axis trackpad scroll pans the scene");
        cameraRig.FrameCity();
    }

    IEnumerator TestSolarWeatherAndCaptures()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Check(false, "A directional sun is assigned for solar-cycle lighting");
            yield break;
        }

        Transform skyRoot = weather.transform.Find("KameraArkasindakiYumusakBulutlar");
        int skyInstanceId = skyRoot != null ? skyRoot.GetEntityId().GetHashCode() : 0;
        float highestNightStep = 0f;
        float highestIntensityStep = 0f;
        SetPhase(.98f);
        yield return null;
        yield return null;
        float priorNight = weather.NightAmount;
        float priorIntensity = sun.intensity;
        for (int i = 0; i <= 120; i++)
        {
            float phase = Mathf.Repeat(.98f + i * (.04f / 120f), 1f);
            SetPhase(phase);
            yield return null;
            highestNightStep = Mathf.Max(highestNightStep, Mathf.Abs(weather.NightAmount - priorNight));
            highestIntensityStep = Mathf.Max(highestIntensityStep, Mathf.Abs(sun.intensity - priorIntensity));
            priorNight = weather.NightAmount;
            priorIntensity = sun.intensity;
        }
        WriteLine("Live midnight seam maxima: night=" + highestNightStep.ToString("F4") + ", sun intensity=" + highestIntensityStep.ToString("F4"));
        Check(highestNightStep < .06f && highestIntensityStep < .12f,
            "Live weather darkness and sun intensity cross midnight without a visible flash");
        Check(weather.transform.Find("KameraArkasindakiYumusakBulutlar") != null &&
              weather.transform.Find("KameraArkasindakiYumusakBulutlar").GetEntityId().GetHashCode() == skyInstanceId,
            "Weather phase sampling reuses the existing sky objects");

        SetPhase(.005f);
        yield return null; yield return null;
        Check(weather.DawnAmount > .9f && sun.color.r > sun.color.b,
            "Sunrise uses a warm light tint");
        Capture("Logs/DayCycle_Sunrise.png");
        yield return new WaitForEndOfFrame(); yield return null;

        SetPhase(.30f);
        yield return null; yield return null;
        Check(weather.NightAmount < .01f, "Day phase remains bright");
        Capture("Logs/DayCycle_Day.png");
        yield return new WaitForEndOfFrame(); yield return null;

        SetPhase(.60f);
        yield return null; yield return null;
        Check(weather.DuskAmount > .9f && sun.color.r > sun.color.b,
            "Sunset uses a warm light tint");
        Capture("Logs/DayCycle_Sunset.png");
        yield return new WaitForEndOfFrame(); yield return null;

        SetPhase(.80f);
        yield return null; yield return null;
        Check(weather.NightAmount > .99f && sun.intensity < .4f,
            "Night phase dims the sun and activates the blue-hour atmosphere");
        Capture("Logs/DayCycle_Night.png");
        yield return new WaitForEndOfFrame(); yield return null;

        FieldInfo originalRotationField = typeof(IslandWeather).GetField("originalSunRotation", BindingFlags.Instance | BindingFlags.NonPublic);
        Quaternion authoredRotation = originalRotationField != null ? (Quaternion)originalRotationField.GetValue(weather) : sun.transform.rotation;
        weather.enabled = false;
        yield return null;
        Check(Quaternion.Angle(sun.transform.rotation, authoredRotation) < .1f && weather.NightAmount == 0f,
            "Disabling weather restores the authored sun direction");
        weather.enabled = true;
        yield return null; yield return null;
        SetPhase(originalCounter / dayLength);
        yield return null;
    }

    IEnumerator TestClickOwnership()
    {
        BinaPopup popup = BinaPopup.Instance;
        BuildingVisuals building = FindAnyObjectByType<BuildingVisuals>();
        if (popup == null || popup.panel == null || building == null)
        {
            Check(false, "Building click and popup exist for gesture ownership check");
            yield break;
        }

        if (eventSystem != null) eventSystem.enabled = false;
        if (popup.panel.activeSelf)
        {
            yield return new WaitForSecondsRealtime(.22f);
            popup.Kapat();
        }
        Vector2 center = new Vector2(Camera.main.pixelWidth * .5f, Camera.main.pixelHeight * .5f);
        cameraRig.ApplyPointerPan(center, center + Vector2.right * 8f);
        building.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
        Check(IslandCameraController.SuppressWorldClick && !popup.panel.activeSelf,
            "A building click is suppressed while a pan gesture owns the pointer");
        yield return null;
        building.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
        Check(popup.panel.activeSelf, "An ordinary building click still opens its popup");
        if (eventSystem != null) eventSystem.enabled = originalEventSystemEnabled;
        yield return new WaitForSecondsRealtime(.22f);
        popup.Kapat();
        cameraRig.FrameCity();
    }

    IEnumerator TestBlockedInput()
    {
        Camera camera = Camera.main;
        Vector2 center = new Vector2(camera.pixelWidth * .5f, camera.pixelHeight * .5f);
        float zoomBefore = camera.orthographicSize;
        cameraRig.ApplyScrollDelta(new Vector2(0f, 1f), center, false);
        gm.oyunDurduMu = true;
        for (int i = 0; i < 4; i++) yield return null;
        Check(Mathf.Abs(camera.orthographicSize - zoomBefore) < .01f,
            "A modal prevents a queued scroll zoom from changing the camera");
        gm.oyunDurduMu = false;

        zoomBefore = camera.orthographicSize;
        disaster.AfetBaslat(2);
        cameraRig.ApplyScrollDelta(new Vector2(0f, 1f), center, false);
        for (int i = 0; i < 4; i++) yield return null;
        Check(disaster.AfetDevamEdiyorMu && Mathf.Abs(camera.orthographicSize - zoomBefore) < .01f,
            "Earthquake mode blocks queued camera input");
        disaster.enabled = false;
        yield return null;
        disaster.enabled = true;
        gm.afetSuruyor = gm.afetOlduMu = false;
        for (int i = 0; i < 3; i++) yield return null;
        Vector3 settled = camera.transform.position;
        for (int i = 0; i < 30; i++) yield return null;
        Check(Vector3.Distance(settled, camera.transform.position) < .02f,
            "Cancelling the earthquake leaves no stored camera momentum");
        cameraRig.FrameCity();
    }

    void SetPhase(float phase)
    {
        clockCounter.SetValue(clock, Mathf.Repeat(phase, 1f) * dayLength);
    }

    static Vector3 CameraFocus(Camera camera, float planeHeight)
    {
        Vector3 point;
        return CameraGroundPoint(camera, new Vector2(camera.pixelWidth * .5f, camera.pixelHeight * .5f), planeHeight, out point)
            ? point : camera.transform.position;
    }

    static Vector3 CameraGroundPoint(Camera camera, Vector2 screen, float planeHeight)
    {
        Vector3 point;
        return CameraGroundPoint(camera, screen, planeHeight, out point) ? point : Vector3.zero;
    }

    static bool CameraGroundPoint(Camera camera, Vector2 screen, float planeHeight, out Vector3 point)
    {
        Ray ray = camera.ScreenPointToRay(screen);
        float distance;
        if (new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f)).Raycast(ray, out distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    void Capture(string path)
    {
        Directory.CreateDirectory("Logs");
        ScreenCapture.CaptureScreenshot(path);
        WriteLine("Requested Game View screenshot: " + path);
    }

    void Check(bool pass, string message)
    {
        checks++;
        string line = (pass ? "PASS: " : "FAIL: ") + message;
        report.AppendLine(line);
        if (!pass) failed = true;
        WriteReport("Logs/AdaDonguKameraTestiIlerleme.txt");
    }

    void WriteLine(string message)
    {
        report.AppendLine(message);
        WriteReport("Logs/AdaDonguKameraTestiIlerleme.txt");
    }

    void RecordFailure(string message)
    {
        failed = true;
        report.AppendLine("FAIL: " + message);
        WriteReport("Logs/AdaDonguKameraTestiIlerleme.txt");
    }

    void WriteReport(string path)
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(path, report.ToString());
    }

    void RestoreState()
    {
        if (disaster != null && !disaster.enabled) disaster.enabled = true;
        if (weather != null && weather.enabled != originalWeatherEnabled) weather.enabled = originalWeatherEnabled;
        if (eventSystem != null && eventSystem.enabled != originalEventSystemEnabled)
            eventSystem.enabled = originalEventSystemEnabled;
        if (gm != null)
        {
            gm.kullaniciDurdurdu = originalUserPause;
            gm.oyunDurduMu = originalGamePause;
            gm.afetSuruyor = originalIncident;
            gm.afetOlduMu = originalDisasterReported;
            gm.oyunBittiMi = originalGameOver;
            gm.para = originalMoney;
            gm.farkindalikYuzdesi = originalAwareness;
            gm.gun = originalDay;
            gm.hizCarpani = originalSpeed;
        }
        if (clockCounter != null && clock != null) clockCounter.SetValue(clock, originalCounter);
        if (cameraRig != null) cameraRig.FrameCity();
        Time.timeScale = originalTimeScale;
    }

    IEnumerator DriveNested(IEnumerator root)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            IEnumerator currentRoutine = stack.Peek();
            object currentYield;
            bool moved;
            try
            {
                moved = currentRoutine.MoveNext();
                currentYield = moved ? currentRoutine.Current : null;
            }
            catch (Exception ex)
            {
                RecordFailure("Nested test exception: " + ex);
                (currentRoutine as IDisposable)?.Dispose();
                stack.Pop();
                continue;
            }

            if (!moved)
            {
                (currentRoutine as IDisposable)?.Dispose();
                stack.Pop();
                continue;
            }

            IEnumerator nested = currentYield as IEnumerator;
            if (nested != null) stack.Push(nested);
            else yield return currentYield;
        }
    }
}
#endif
