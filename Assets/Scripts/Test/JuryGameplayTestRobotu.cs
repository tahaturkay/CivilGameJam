#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Focused jury regressions, or a natural 20-day timed playthrough when timedSession is enabled.</summary>
public sealed class JuryGameplayTestRobotu : MonoBehaviour
{
    public bool timedSession;

    const string FocusLogPath = "Logs/JuryGameplayTests.txt";
    const string TimingLogPath = "Logs/JurySessionTiming.txt";
    const float VehicleHalfLength = 1.75f;
    const float VehicleHalfWidth = .9f;
    const float VehicleMargin = .18f;
    readonly StringBuilder log = new StringBuilder();
    int passed, failed;
    float sessionStart;

    [Serializable] sealed class BalanceReport { public BalanceRun[] runs; }
    [Serializable] sealed class BalanceRun
    {
        public int cardIndex;
        public int cash;
        public float awareness;
        public int defendedIncidents;
        public int totalDeaths;
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        Directory.CreateDirectory("Logs");
        sessionStart = Time.realtimeSinceStartup;
        log.AppendLine("=== JURY GAMEPLAY TEST · " + DateTime.Now + " · " + SceneManager.GetActiveScene().name + " ===");
        yield return null;
        yield return null;

        yield return DriveNested(timedSession ? RunTimedSession() : RunFocusedTests());

        log.AppendLine("\nRESULT: " + passed + " passed, " + failed + " failed.");
        File.WriteAllText(FocusLogPath, log.ToString());
        if (timedSession)
        {
            float elapsed = Time.realtimeSinceStartup - sessionStart;
            string timing = "elapsedSeconds=" + elapsed.ToString("F1") + "\n" +
                            "targetSeconds=150-180\n" +
                            "naturalDayLengthSeconds=" + DengeAyarlari.Aktif.birGunSuresi.ToString("F1") + "\n" +
                            "gameSpeedMultiplier=" + (GameManager.Instance != null ? GameManager.Instance.hizCarpani : 0) + "\n" +
                            "daysCompleted=" + (GameManager.Instance != null ? GameManager.Instance.gun : 0) + "\n" +
                            "result=" + (failed == 0 ? "PASS" : "FAIL") + "\n";
            File.WriteAllText(TimingLogPath, timing);
            log.AppendLine("Timed session duration: " + elapsed.ToString("F1") + " seconds (target 150–180).");
        }
        UnityEditor.EditorApplication.isPlaying = false;
    }

    IEnumerator RunFocusedTests()
    {
        GameManager gm = GameManager.Instance;
        DisasterController disaster = FindAnyObjectByType<DisasterController>();
        AfetRaporu report = FindAnyObjectByType<AfetRaporu>();
        IslandCityLife city = FindAnyObjectByType<IslandCityLife>();
        DereYatagiKarari liveDecision = FindAnyObjectByType<DereYatagiKarari>();
        IpucuSistemi hints = FindAnyObjectByType<IpucuSistemi>();
        Check(gm != null && disaster != null && report != null, "GameManager, DisasterController, and AfetRaporu are present");
        Check(liveDecision != null, "Scheduled riverbed decision is installed on the main canvas");
        if (gm == null || disaster == null || report == null) yield break;

        gm.kullaniciDurdurdu = true;
        gm.oyunDurduMu = false;
        gm.afetSuruyor = false;
        gm.afetOlduMu = false;
        gm.oyunBittiMi = false;
        gm.gun = 1;
        gm.para = 500;
        gm.nufus = 1000;
        gm.farkindalikYuzdesi = 10f;
        gm.akutLevel = 1;
        gm.akutHasarli = false;
        gm.itfaiyeLevel = 1;
        gm.itfaiyeHasarli = false;
        gm.altyapiLevel = 1;
        gm.altyapiHasarli = false;
        Time.timeScale = 1f;
        yield return null;

        TestStoryAndCards();
        TestHintSchedule(hints);
        yield return TestDecisionSingleShot(gm);

        // The live scene traffic check includes rotated boxes and every stopped or displaced car.
        if (city != null)
        {
            gm.gun = 1;
            gm.kullaniciDurdurdu = false;
            gm.oyunDurduMu = false;
            gm.altyapiHasarli = false;
            yield return null;
            Transform[] cars = Vehicles(city);
            Check(cars.Length == city.VehicleCount && cars.Length > 0, "Traffic actors are available for oriented-clearance checks (" + cars.Length + ")");
            CheckVehicleClearance(cars, "initial spacing");

            Vector3[] initial = Positions(cars);
            yield return new WaitForSecondsRealtime(1.2f);
            bool moved = AnyVehicleMoved(cars, initial, .1f);
            Check(moved, "At least one car moves on the unobstructed normal route");
            CheckVehicleClearance(cars, "normal motion");

            gm.gun = 2;
            yield return null;
            if (hints != null)
            {
                bool visibleFireHint = hints.CurrentDisasterHint == AfetTuru.Yangin &&
                    !string.IsNullOrEmpty(hints.VisibleText) && hints.VisibleText.Contains("İtfaiye");
                Check(visibleFireHint, "Approaching fire hint reaches the live non-blocking hint card");
            }
        }
        else Check(false, "IslandCityLife is present for live traffic and water regressions");

        // Check that fire damage is localized to selected instances, not every building of a type.
        gm.kullaniciDurdurdu = true;
        gm.gun = 1;
        gm.itfaiyeLevel = 1;
        gm.itfaiyeHasarli = false;
        gm.akutLevel = 1;
        gm.akutHasarli = false;
        gm.farkindalikYuzdesi = 10f;
        int allBuildings = FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None).Length;
        Time.timeScale = 4f;
        disaster.AfetBaslat(0);
        yield return WaitForIncident(disaster, gm, 12f);
        yield return null;
        int localDamage = CountLocalDamage();
        Check(gm.afetOlduMu && disaster.DamagedInstanceCount > 0 && disaster.DamagedInstanceCount < allBuildings &&
              disaster.DamagedInstanceCount == localDamage && gm.sonHasarliBinaSayisi == localDamage,
            "Fire damages a localized set of building instances: " + localDamage + "/" + allBuildings);
        Check(report.nufusText != null && report.nufusText.text.Contains(localDamage.ToString()),
            "Fire report shows the count of locally damaged building instances");
        if (gm.afetOlduMu) report.Devam();
        yield return null;
        Time.timeScale = 1f;

        // Exercise both live decision branches through their public API and then use the real scheduled choice.
        if (liveDecision != null)
        {
            gm.gun = 7;
            gm.kullaniciDurdurdu = false;
            float decisionWait = Time.realtimeSinceStartup + 3f;
            while (!liveDecision.Acik && Time.realtimeSinceStartup < decisionWait) yield return null;
            Check(liveDecision.Acik && liveDecision.Soruldu, "Day-seven decision opens once at the real scheduled trigger");
            int beforeOffer = gm.para;
            if (liveDecision.Acik) liveDecision.KararVer(true);
            Check(liveDecision.Cozuldu && liveDecision.KabulEdildi && gm.para == beforeOffer + 150,
                "Accepted riverbed offer pays +150 exactly once");
            liveDecision.KararVer(true);
            Check(gm.para == beforeOffer + 150, "Repeating the accepted decision has no second effect");
        }
        gm.kullaniciDurdurdu = true;
        gm.gun = 1;
        gm.oyunDurduMu = false;
        gm.para = 10000;
        gm.nufus = 1000;
        gm.farkindalikYuzdesi = 10f;
        gm.altyapiLevel = 1;
        gm.altyapiHasarli = false;
        gm.akutLevel = 1;
        gm.akutHasarli = false;
        Time.timeScale = 4f;
        int populationBeforeFlood = gm.nufus;
        Vector3[] beforeFlood = city != null ? Positions(Vehicles(city)) : new Vector3[0];
        disaster.AfetBaslat(1);

        float floodDeadline = Time.realtimeSinceStartup + 12f;
        bool sawFloodWater = false, sawSweptCitizen = false, sawDisplacedCar = false;
        float floodMinimumGap = float.PositiveInfinity;
        while ((disaster.AfetDevamEdiyorMu || !gm.afetOlduMu) && Time.realtimeSinceStartup < floodDeadline)
        {
            sawFloodWater |= !float.IsNegativeInfinity(disaster.FloodSurfaceY);
            if (city != null)
            {
                sawSweptCitizen |= city.SweptCitizenCount > 0;
                sawDisplacedCar |= city.DisplacedVehicleCount > 0;
                float gap = VehicleClearance(Vehicles(city));
                floodMinimumGap = Mathf.Min(floodMinimumGap, gap);
            }
            yield return null;
        }
        Time.timeScale = 1f;
        yield return null;

        int expectedFloodDeaths = Mathf.RoundToInt(150f * 1.5f * (1f - gm.farkindalikYuzdesi * DengeAyarlari.Aktif.farkindalikEtkisi) *
                                                   (1f - gm.akutLevel * DengeAyarlari.Aktif.akutEtkisi));
        Check(gm.afetOlduMu && liveDecision != null && liveDecision.KabulEdildi && gm.sonAfetTuru == AfetTuru.Sel &&
              gm.sonVefatSayisi == expectedFloodDeaths,
            "Accepted riverbed decision applies the 1.5 flood casualty multiplier (" + gm.sonVefatSayisi + "/" + expectedFloodDeaths + ")");
        Check(gm.nufus == populationBeforeFlood - gm.sonVefatSayisi,
            "Only DisasterController's reported flood casualties change population");
        Check(sawFloodWater, "Flood surface becomes active during the incident");
        Check(sawSweptCitizen, "Exposed outdoor citizens are carried by flood water");
        Check(sawDisplacedCar, "Water-reached outdoor vehicles are displaced from the road");
        Check(float.IsPositiveInfinity(floodMinimumGap) || floodMinimumGap >= -.001f,
            "Rotated vehicle footprints never overlap during flood movement (minimum gap " + floodMinimumGap.ToString("F2") + ")");
        if (city != null && city.VehicleCount > 0)
        {
            Check(AnyVehicleMoved(Vehicles(city), beforeFlood, .1f), "At least one vehicle was moved by water during the flood");
        }

        if (gm.afetOlduMu) report.Devam();
        yield return null;
        if (city != null)
        {
            Check(gm.altyapiHasarli && city.TrafficBlocked, "Flood-damaged infrastructure keeps roads stopped after the report");
            Check(Ekonomi.Onar(BinaTuru.Altyapi), "Infrastructure repair can reopen the flood-damaged roads");
            Check(!city.TrafficBlocked, "Repair releases the global traffic stop");
            gm.kullaniciDurdurdu=false;

            bool clearAtEveryReturnStep = true;
            float returnDeadline = Time.realtimeSinceStartup + 8f;
            while (city.DisplacedVehicleCount > 0 && Time.realtimeSinceStartup < returnDeadline)
            {
                if (VehicleClearance(Vehicles(city)) < -.001f) clearAtEveryReturnStep = false;
                yield return null;
            }
            Check(clearAtEveryReturnStep && city.DisplacedVehicleCount == 0,
                "Displaced cars return to routes after repair without overlapping, including stopped cars");
            CheckVehicleClearance(Vehicles(city), "post-repair traffic");
        }
        gm.kullaniciDurdurdu = true;
        Time.timeScale = 1f;
    }

    IEnumerator TestDecisionSingleShot(GameManager gm)
    {
        gm.kullaniciDurdurdu = true;
        gm.oyunDurduMu = false;
        yield return ProbeDecision(gm, false);
        yield return ProbeDecision(gm, true);
    }

    IEnumerator ProbeDecision(GameManager gm, bool accept)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject host = new GameObject("DecisionProbe_" + (accept ? "Accept" : "Reject"));
        if (canvas != null) host.transform.SetParent(canvas.transform, false);
        DereYatagiKarari probe = host.AddComponent<DereYatagiKarari>();
        yield return null;
        FieldInfo panelField = typeof(DereYatagiKarari).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic);
        GameObject panel = panelField != null ? panelField.GetValue(probe) as GameObject : null;
        Check(panel != null, (accept ? "Accept" : "Reject") + " decision probe builds its public decision panel");
        if (panel != null)
        {
            gm.para = 500;
            gm.okulLevel = 2;
            gm.farkindalikYuzdesi = 10f;
            panel.SetActive(true);
            int beforeCash = gm.para;
            float beforeAwareness = gm.farkindalikYuzdesi;
            probe.KararVer(accept);
            probe.KararVer(accept);
            Check(probe.Cozuldu && probe.KabulEdildi == accept && !probe.Acik &&
                  gm.para == beforeCash + (accept ? 150 : 0) &&
                  Mathf.Approximately(gm.farkindalikYuzdesi, beforeAwareness + (accept ? 0 : 5)),
                (accept ? "Accept" : "Reject") + " decision is single-shot with the expected reward");
        }
        Destroy(host);
        yield return null;
    }

    void TestStoryAndCards()
    {
        AnlatiVerisi data = AnlatiVerisi.Aktif;
        Check(data != null, "AnlatiVerisi resource is loaded");
        if (data == null) return;

        string fireBad = data.HikayeMetni(AfetTuru.Yangin, false, 10f, false);
        string fireGood = data.HikayeMetni(AfetTuru.Yangin, true, 10f, false);
        string quakeEducated = data.HikayeMetni(AfetTuru.Deprem, true, 65f, false);
        string floodDecision = data.HikayeMetni(AfetTuru.Sel, false, 10f, true);
        Check(fireBad.Contains("kül oldu") && fireGood.Contains("iki çuval un"), "Fire story changes between an unsuccessful and successful response");
        Check(quakeEducated.Contains("Çök-Kapan-Tutun"), "High-awareness earthquake story uses the learned response variant");
        Check(floodDecision.Contains("bütün mahalleyi yuttu"), "Accepted riverbed story uses its flood-specific bad outcome");

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "JuryBalance.json"));
        Check(File.Exists(path), "Python balance report exists for legal-start card reachability checks");
        if (File.Exists(path))
        {
            BalanceReport balance = JsonUtility.FromJson<BalanceReport>(File.ReadAllText(path));
            bool[] reached = new bool[5];
            if (balance != null && balance.runs != null)
            {
                for (int i = 0; i < balance.runs.Length; i++)
                {
                    BalanceRun run = balance.runs[i];
                    if (run == null || run.cardIndex < 0 || run.cardIndex >= reached.Length) continue;
                    int actual = data.KartIndex(run.defendedIncidents, run.awareness, run.cash, run.totalDeaths);
                    if (actual == run.cardIndex) reached[actual] = true;
                }
            }
            bool all = true;
            for (int i = 0; i < reached.Length; i++) all &= reached[i];
            Check(all, "All five actual ending cards are reachable by policies starting with legal game resources");
        }
    }

    void TestHintSchedule(IpucuSistemi hints)
    {
        var settings = DengeAyarlari.Aktif.afetler;
        bool scheduleMatches = true;
        for (int day = 1; day <= DengeAyarlari.Aktif.toplamGun; day++)
        {
            AfetTuru? expected = null;
            for (int i = 0; i < settings.Length; i++)
            {
                int remaining = settings[i].gun - day;
                if (remaining >= 1 && remaining <= 3) { expected = settings[i].afet; break; }
            }
            if (IpucuSistemi.HintDisasterForDay(day) != expected) scheduleMatches = false;
        }
        Check(scheduleMatches, "Static disaster hint schedule matches all one-to-three-day warning windows");
        Check(hints != null && hints.VisibleText != null, "Hint system exposes current card text for live inspection");
    }

    IEnumerator WaitForIncident(DisasterController disaster, GameManager gm, float timeout)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!disaster.AfetDevamEdiyorMu && !gm.afetOlduMu && Time.realtimeSinceStartup < end) yield return null;
        while (!gm.afetOlduMu && Time.realtimeSinceStartup < end) yield return null;
    }

    IEnumerator RunTimedSession()
    {
        GameManager gm = GameManager.Instance;
        DisasterController disaster = FindAnyObjectByType<DisasterController>();
        AfetRaporu report = FindAnyObjectByType<AfetRaporu>();
        QuizManager quiz = QuizManager.Instance;
        BinaPopup popup = BinaPopup.Instance;
        DereYatagiKarari decision = FindAnyObjectByType<DereYatagiKarari>();
        Check(gm != null && disaster != null && report != null && quiz != null && popup != null && decision != null,
            "Timed run has all game, report, quiz, popup, and decision systems");
        if (gm == null || disaster == null || report == null || quiz == null || popup == null || decision == null) yield break;

        float originalDayLength = DengeAyarlari.Aktif.birGunSuresi;
        gm.hizCarpani = 2;
        gm.kullaniciDurdurdu = false;
        gm.oyunDurduMu = false;
        Time.timeScale = 1f;
        Check(gm.gun == 1 && Mathf.Approximately(originalDayLength, 12f), "Timed session starts at day 1 using the natural 12-second day length");
        Check(Mathf.Approximately(Time.timeScale, 1f), "Timed session does not override Unity time scale");

        HashSet<int> quizDays = new HashSet<int>();
        HashSet<int> contextDays = new HashSet<int>();
        int quizzesAnswered = 0;
        bool decisionHandled = false;
        int lastDay = gm.gun;
        float hardDeadline = sessionStart + 220f;
        while (!gm.oyunBittiMi && Time.realtimeSinceStartup < hardDeadline)
        {
            if (gm.afetOlduMu && report.panel.activeSelf)
            {
                Write("Day " + gm.gun + " " + gm.sonAfetTuru + " report: waiting 4 seconds.");
                yield return new WaitForSecondsRealtime(4f);
                report.Devam();
                continue;
            }
            if (decision.Acik && !decisionHandled)
            {
                Write("Day " + gm.gun + " riverbed decision: reject after 4 seconds.");
                yield return new WaitForSecondsRealtime(4f);
                decision.KararVer(false);
                decisionHandled = true;
                continue;
            }

            if (gm.gun != lastDay)
            {
                lastDay = gm.gun;
                Write("Reached natural day " + gm.gun + ".");
            }

            BuyTowards(gm, BinaTuru.Itfaiye, 2, 4);
            BuyTowards(gm, BinaTuru.TemelBina, 2, 4);
            BuyTowards(gm, BinaTuru.Okul, 2, 8);
            BuyTowards(gm, BinaTuru.Altyapi, 2, 9);
            BuyTowards(gm, BinaTuru.Akut, 2, 14);
            BuyTowards(gm, BinaTuru.DenizSeti, 3, 19);

            if (gm.okulLevel >= 2 && quizzesAnswered < 2 && !gm.okulHasarli && gm.gun >= 3 &&
                quizDays.Add(gm.gun) && !gm.ZamanDurduMu)
            {
                quiz.QuizAc();
                yield return null;
                if (quiz.AktifSoru != null && quiz.AktifSoru.dogruSikIndex < quiz.sikButonlari.Length)
                    quiz.sikButonlari[quiz.AktifSoru.dogruSikIndex].onClick.Invoke();
                Write("Day " + gm.gun + " quiz " + (quizzesAnswered + 1) + ": correct answer; reading for 4 seconds.");
                yield return new WaitForSecondsRealtime(4f);
                quiz.QuizKapat();
                quizzesAnswered++;
                continue;
            }

            if (popup != null && !gm.ZamanDurduMu && (gm.gun == 2 || gm.gun == 3) && contextDays.Add(gm.gun))
            {
                popup.Ac(gm.gun == 2 ? BinaTuru.TemelBina : BinaTuru.Okul);
                Write("Day " + gm.gun + " contextual building card: reading for 2 seconds.");
                yield return new WaitForSecondsRealtime(2f);
                popup.Kapat();
                continue;
            }

            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - sessionStart;
        Check(gm.gun == DengeAyarlari.Aktif.toplamGun && gm.oyunBittiMi && gm.kazandiMi,
            "Natural 1-to-20 day session ends in a win at day 20");
        Check(quizzesAnswered == 2, "Timed session answers exactly two quizzes");
        Check(disaster.DefendedIncidentCount==4,"Timed session contains exactly four defended scheduled incidents");
        Check(decisionHandled && decision.Soruldu && decision.Cozuldu && !decision.KabulEdildi,
            "Timed session rejects the scheduled flood decision after displaying it");
        Check(originalDayLength == DengeAyarlari.Aktif.birGunSuresi && Mathf.Approximately(Time.timeScale, 1f),
            "Timed session retains the configured day duration and Unity time scale");
        Check(elapsed >= 150f && elapsed <= 180f, "Natural timed run lasts " + elapsed.ToString("F1") + " seconds (target 150–180)");
        Write("Timed session finished in " + elapsed.ToString("F1") + " seconds.");
    }

    void BuyTowards(GameManager gm, BinaTuru building, int target, int latestDay)
    {
        if (gm.gun > latestDay || gm.Level(building) >= target || gm.Hasarli(building) || gm.afetSuruyor || gm.afetOlduMu) return;
        while (gm.Level(building) < target && Ekonomi.YukseltilebilirMi(building))
        {
            if (!Ekonomi.Yukselt(building)) break;
            Write("Day " + gm.gun + " upgrade " + building + " to level " + gm.Level(building) + ".");
        }
    }

    Transform[] Vehicles(IslandCityLife city)
    {
        if (city == null) return new Transform[0];
        Transform root = city.transform.Find("SehirYasami");
        if (root == null) return new Transform[0];
        List<Transform> cars = new List<Transform>(root.childCount);
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name.StartsWith("AdaAraci_")) cars.Add(child);
        }
        return cars.ToArray();
    }

    static Vector3[] Positions(Transform[] cars)
    {
        Vector3[] result = new Vector3[cars != null ? cars.Length : 0];
        for (int i = 0; i < result.Length; i++) result[i] = cars[i] != null ? cars[i].position : Vector3.zero;
        return result;
    }

    static bool AnyVehicleMoved(Transform[] cars, Vector3[] from, float threshold)
    {
        if (cars == null || from == null) return false;
        int count = Mathf.Min(cars.Length, from.Length);
        for (int i = 0; i < count; i++)
            if (cars[i] != null && Vector3.Distance(cars[i].position, from[i]) >= threshold) return true;
        return false;
    }

    void CheckVehicleClearance(Transform[] cars, string phase)
    {
        float gap = VehicleClearance(cars);
        Check(float.IsPositiveInfinity(gap) || gap >= -.001f,
            "No oriented car-footprint overlap during " + phase + " (minimum clearance " + gap.ToString("F2") + ")");
    }

    static float VehicleClearance(Transform[] cars)
    {
        if (cars == null || cars.Length < 2) return float.PositiveInfinity;
        float minimum = float.PositiveInfinity;
        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] == null) continue;
            for (int j = i + 1; j < cars.Length; j++)
            {
                if (cars[j] == null) continue;
                minimum = Mathf.Min(minimum, OrientedVehicleGap(cars[i], cars[j]));
            }
        }
        return minimum;
    }

    static float OrientedVehicleGap(Transform a, Transform b)
    {
        Vector3 forwardA = Vector3.ProjectOnPlane(a.forward, Vector3.up).normalized;
        Vector3 forwardB = Vector3.ProjectOnPlane(b.forward, Vector3.up).normalized;
        if (forwardA.sqrMagnitude < .001f) forwardA = Vector3.forward;
        if (forwardB.sqrMagnitude < .001f) forwardB = Vector3.forward;
        Vector3 rightA = Vector3.Cross(Vector3.up, forwardA).normalized;
        Vector3 rightB = Vector3.Cross(Vector3.up, forwardB).normalized;
        Vector3 delta = b.position - a.position;
        float separatingGap = float.NegativeInfinity;
        separatingGap = Mathf.Max(separatingGap, AxisGap(delta, forwardA, forwardA, rightA, forwardB, rightB));
        separatingGap = Mathf.Max(separatingGap, AxisGap(delta, rightA, forwardA, rightA, forwardB, rightB));
        separatingGap = Mathf.Max(separatingGap, AxisGap(delta, forwardB, forwardA, rightA, forwardB, rightB));
        separatingGap = Mathf.Max(separatingGap, AxisGap(delta, rightB, forwardA, rightA, forwardB, rightB));
        return separatingGap;
    }

    static float AxisGap(Vector3 delta, Vector3 axis, Vector3 forwardA, Vector3 rightA, Vector3 forwardB, Vector3 rightB)
    {
        float radiusA = Mathf.Abs(Vector3.Dot(forwardA, axis)) * (VehicleHalfLength + VehicleMargin) +
                        Mathf.Abs(Vector3.Dot(rightA, axis)) * (VehicleHalfWidth + VehicleMargin);
        float radiusB = Mathf.Abs(Vector3.Dot(forwardB, axis)) * (VehicleHalfLength + VehicleMargin) +
                        Mathf.Abs(Vector3.Dot(rightB, axis)) * (VehicleHalfWidth + VehicleMargin);
        return Mathf.Abs(Vector3.Dot(delta, axis)) - radiusA - radiusB;
    }

    int CountLocalDamage()
    {
        int count = 0;
        BuildingVisuals[] buildings = FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None);
        for (int i = 0; i < buildings.Length; i++)
            if (buildings[i] != null && buildings[i].LocalDamageManaged && buildings[i].IsDamaged) count++;
        return count;
    }

    void Check(bool condition, string message)
    {
        if (condition) passed++; else failed++;
        Write((condition ? "PASS " : "FAIL ") + message);
    }

    void Write(string message)
    {
        log.AppendLine(message);
        Debug.Log("[JuryGameplayTest] " + message);
    }

    IEnumerator DriveNested(IEnumerator root)
    {
        Stack<IEnumerator> routines = new Stack<IEnumerator>();
        routines.Push(root);
        while (routines.Count > 0)
        {
            IEnumerator currentRoutine = routines.Peek();
            object current = null;
            bool moved = false, threw = false;
            try
            {
                moved = currentRoutine.MoveNext();
                if (moved) current = currentRoutine.Current;
            }
            catch (Exception exception)
            {
                Check(false, "Unhandled nested test exception: " + exception);
                threw = true;
            }
            if (threw) yield break;
            if (!moved) { routines.Pop(); continue; }
            IEnumerator nested = current as IEnumerator;
            if (nested != null) routines.Push(nested);
            else yield return current;
        }
    }
}
#endif
