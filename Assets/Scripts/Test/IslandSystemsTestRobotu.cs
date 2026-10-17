#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>Integration checks against the actual installed island, UI, state and disaster coroutines.</summary>
public class IslandSystemsTestRobotu : MonoBehaviour
{
    readonly StringBuilder log = new StringBuilder();
    int checks;
    GameManager gm;
    DisasterController disaster;
    IslandCityLife city;
    AfetRaporu report;
    IslandWeather weather;
    IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return null; yield return null;
        gm = GameManager.Instance; disaster = FindAnyObjectByType<DisasterController>();
        city = FindAnyObjectByType<IslandCityLife>(); report = FindAnyObjectByType<AfetRaporu>();
        weather = FindAnyObjectByType<IslandWeather>();
        gm.kullaniciDurdurdu = true; gm.para = 50000; gm.farkindalikYuzdesi = 100f;
        Time.timeScale = 3;
        var test = Run();
        while (true)
        {
            object next;
            try { if (!test.MoveNext()) break; next = test.Current; }
            catch (Exception ex) { log.AppendLine("FAIL: " + ex); break; }
            yield return next;
        }
        Time.timeScale = 1;
        log.AppendLine("Completed checks: " + checks);
        File.WriteAllText("Logs/AdaSistemTesti.txt", log.ToString());
        UnityEditor.EditorApplication.isPlaying = false;
    }
    void Check(bool ok, string text)
    {
        if (!ok) throw new Exception(text);
        checks++; log.AppendLine("PASS: " + text); File.WriteAllText("Logs/AdaSistemTestiIlerleme.txt", log.ToString());
    }
    IEnumerator Run()
    {
        Check(city.CitizenCount == 95 && city.AnimalCount == 5 && city.LivingActorCount == 100 && city.VehicleCount == 12,
            "95 pedestrians, 5 street animals and 12 moving cars spawned");
        Transform actors = city.transform.Find("SehirYasami");
        if(SystemInfo.supportsInstancing)
        {
            Check(city.InstancedPartCount>=1400 && city.InstancedBatchCount<=50, "100 living actors share instanced mesh groups");
            city.enabled=false;yield return null;
            Check(city.InstancedPartCount==0 && actors.GetChild(0).GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled), "Disabling city simulation restores ordinary NPC rendering");
            city.enabled=true;yield return null;yield return null;
            Check(city.InstancedPartCount>=1400 && actors.GetChild(0).GetComponentsInChildren<MeshRenderer>().All(r=>!r.enabled), "Re-enabling restores instancing without duplicate renderers");
        }
        Transform pedestrian = actors.GetChild(0), car = actors.Cast<Transform>().First(t => t.name.StartsWith("AdaAraci"));
        Vector3 p = pedestrian.position, c = car.position;
        gm.kullaniciDurdurdu = false;
        yield return new WaitForSeconds(.7f);
        gm.kullaniciDurdurdu = true;
        Check(Vector3.Distance(p,pedestrian.position) > .2f && Vector3.Distance(c,car.position) > .2f, "Citizens walk and vehicles drive on live routes");
        var road = GameObject.Find("LowPolyAda").transform.Find("Yollar").GetComponentsInChildren<BuildingVisuals>().First();
        Check(road.levelModelleri.Select(m=>m.name).SequenceEqual(new[]{"Level1_Toprak","Level2_Cakil","Level3_Asfalt"}), "Road stages are dirt, gravel and asphalt");
        Check(Ekonomi.Yukselt(BinaTuru.Altyapi), "Road can upgrade through existing economy");
        yield return new WaitForSecondsRealtime(.06f);
        Check(road.VurguBeyazligi > .05f && road.levelModelleri[1].transform.localScale.x > 1, "Upgrade has scale punch and white flash");
        yield return new WaitForSecondsRealtime(.25f);
        Check(road.VurguBeyazligi == 0 && road.levelModelleri[1].transform.localScale == Vector3.one, "Punch returns exactly to original scale after 0.2 seconds");
        var hud = FindAnyObjectByType<UIManager>();
        int before = gm.para; gm.GelirKaydet(123);
        yield return new WaitForSecondsRealtime(.1f);
        Check(!hud.paraText.text.StartsWith((before+123)+" coin"), "Income display counts up instead of jumping");
        yield return new WaitForSecondsRealtime(1.1f);
        Check(hud.paraText.text.StartsWith(gm.para+" coin"), "Income counter reaches exact balance in one second");
        BuildingVisuals house = city.houses[0];
        gm.LevelAyarla(BinaTuru.TemelBina,3); yield return null;
        gm.HasarVer(BinaTuru.TemelBina,AfetTuru.Yangin);
        yield return null; yield return null;
        Check(gm.temelBinaLevel == 2 && gm.temelBinaHasarli, "Upper level damage both downgrades and requires repair");
        Check(house.GetComponentsInChildren<ParticleSystem>().Length > 0 && house.transform.Find("KaliciHasarEfektleri") != null, "Fire damage keeps smoke after flames end");
        Check(Ekonomi.Onar(BinaTuru.TemelBina), "Fire damage can be repaired");
        yield return new WaitForSecondsRealtime(.3f);
        Check(house.transform.Find("KaliciHasarEfektleri") == null, "Repair removes persistent smoke and soot");
        gm.HasarVer(BinaTuru.TemelBina,AfetTuru.Deprem);
        yield return null; yield return null;
        Check(house.levelModelleri[0].transform.Find("DepremHasarKatmani") != null, "Earthquake leaves a visibly collapsed rubble state");
        Check(Ekonomi.Onar(BinaTuru.TemelBina), "Earthquake damage can be repaired");
        yield return new WaitForSecondsRealtime(.3f);
        Check(house.levelModelleri[0].transform.Find("DepremHasarKatmani") == null, "Repair restores the earthquake model hierarchy");
        gm.gun = 13;
        yield return new WaitForSecondsRealtime(3);
        Check(weather.StormAmount > .35f && !weather.RainActive, "Day before flood darkens atmosphere without rain");
        Check(weather.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name.StartsWith("FirtinaBulutu") && r.bounds.center.y>city.landBounds.max.y+15), "Forecast clouds stay above the ocean and clear of city silhouettes");
        gm.gun = 1;
        // Insufficient fire protection: assembly, flames, persistent fire damage.
        ResetBuildings(); disaster.AfetBaslat(0);
        yield return new WaitForSeconds(2);
        Check(gm.afetSuruyor && gm.ZamanDurduMu && city.CurrentMode == IslandCityLife.CityLifeMode.Assembly, "Fire freezes day clock and evacuates people to assembly areas");
        Check(!Ekonomi.Yukselt(BinaTuru.TemelBina), "Building transactions are blocked during an active disaster");
        Check(disaster.GetComponentsInChildren<ParticleSystem>().Length > 0, "Layered fire particles are active");
        yield return new WaitForSeconds(4);
        Check(disaster.GetComponentsInChildren<ParticleSystem>().Sum(ps=>ps.particleCount)>200, "Fire effects emit live particles above the rooftops");
        Capture("Logs/Ada_Yangin.png");
        yield return null; // Let the native graphics queue consume this capture before submitting another.
        CaptureDetail("Logs/Ada_YanginYakin.png", new Vector3(-34,4,8));
        // Per-building fire waits resume on frame boundaries; synchronous captures can delay them.
        // Observe the real completion state with a bound instead of guessing its finishing frame.
        float fireDeadline=Time.realtimeSinceStartup+disaster.afetSuresi*2;
        while(disaster.AfetDevamEdiyorMu && Time.realtimeSinceStartup<fireDeadline)yield return null;
        log.AppendLine("Fire completion: report="+gm.afetOlduMu+" active="+gm.afetSuruyor+" damage="+gm.temelBinaHasarli+" arrivals="+city.GatheredCount+"/"+city.CitizenCount);
        Check(gm.afetOlduMu && !gm.afetSuruyor && gm.temelBinaHasarli && city.GatheredCount == city.LivingActorCount, "Fire ends with repairable damage and assembled citizens");
        yield return new WaitForSecondsRealtime(1.1f);
        Check(report.vefatText.text.EndsWith(gm.sonVefatSayisi.ToString()), "Disaster death counter reaches its exact final value");
        Capture("Logs/Ada_YanginHasari.png");
        CloseReport(); yield return null;
        ResetBuildings();
        // Low infrastructure floods, high infrastructure remains dry.
        disaster.AfetBaslat(1); yield return new WaitForSeconds(7);
        Check(weather.RainActive && weather.StormAmount > .75f, "Flood enables rain and dark storm lighting");
        Check(disaster.tsunamiSuyu.gameObject.activeSelf && Mathf.Abs(disaster.tsunamiSuyu.position.y-disaster.selSuYuksekligi)<.03f, "Insufficient infrastructure produces shallow flood water");
        Check(city.CurrentMode == IslandCityLife.CityLifeMode.Assembly, "Flood evacuates the full city to gathering areas at high awareness");
        Renderer water = disaster.tsunamiSuyu.GetComponent<Renderer>();
        Check(water.sharedMaterial.GetColor("_BaseColor").a > .98f && water.bounds.size.x < city.landBounds.size.x, "Flood water is opaque and stays inside the seawall");
        Capture("Logs/Ada_Sel.png");
        yield return new WaitForSeconds(disaster.afetSuresi);
        Check(gm.altyapiHasarli && FindAnyObjectByType<RoadNetworkState>().ConstructionVisible, "Flood leaves damaged roads and construction markers");
        CloseReport(); yield return null;
        Transform trafficCar=actors.Cast<Transform>().First(t=>t.name.StartsWith("AdaAraci"));
        Vector3 blockedPosition=trafficCar.position;gm.kullaniciDurdurdu=false;
        yield return new WaitForSecondsRealtime(.3f);gm.kullaniciDurdurdu=true;
        Check(city.TrafficBlocked && Vector3.Distance(blockedPosition,trafficCar.position)<.02f,
            "Cars stay stopped while flood-damaged roads await repair");
        Check(Ekonomi.Onar(BinaTuru.Altyapi), "Damaged roads use the existing repair action");
        gm.kullaniciDurdurdu=false;yield return new WaitForSecondsRealtime(.35f);gm.kullaniciDurdurdu=true;
        Check(!FindAnyObjectByType<RoadNetworkState>().ConstructionVisible, "Road repair clears construction markers");
        Check(!city.TrafficBlocked && Vector3.Distance(blockedPosition,trafficCar.position)>.05f,
            "Traffic resumes after road repair");
        ResetBuildings(); gm.altyapiLevel = 2;
        disaster.AfetBaslat(1); yield return new WaitForSeconds(7);
        Check(disaster.SavunmaYeterli && !disaster.tsunamiSuyu.gameObject.activeSelf, "Level 2 intact infrastructure keeps the city dry during flood");
        yield return new WaitForSeconds(disaster.afetSuresi);
        Check(!gm.altyapiHasarli && gm.sonSavunmaYeterli, "Protected flood causes no road damage");
        CloseReport(); yield return null;
        // Tsunami inherits the sea material and carries a water sheet behind its front.
        ResetBuildings(); disaster.AfetBaslat(3); yield return new WaitForSeconds(6);
        Material sea = GameObject.Find("LowPolyAda").transform.Find("Arazi/Deniz").GetComponent<Renderer>().sharedMaterial;
        Check(disaster.tsunamiDalgasi.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m==sea)), "Tsunami uses exactly the ocean material");
        Check(disaster.tsunamiSuyu.gameObject.activeSelf && disaster.tsunamiSuyu.position.y > disaster.kuruZeminY &&
            disaster.tsunamiSuyu.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").a > .98f,
            "Weak seawall allows opaque flood water to follow the wave");
        Capture("Logs/Ada_Tsunami.png");
        yield return new WaitForSeconds(disaster.afetSuresi);
        Check(gm.setHasarli && !disaster.tsunamiDalgasi.gameObject.activeSelf, "Unprotected tsunami exits view and leaves seawall damage");
        CloseReport(); yield return null;
        ResetBuildings(); gm.setLevel = 3;
        disaster.AfetBaslat(3); yield return new WaitForSeconds(6);
        Check(disaster.SavunmaYeterli && !disaster.tsunamiSuyu.gameObject.activeSelf && !disaster.tsunamiDalgasi.gameObject.activeSelf, "Level 3 seawall dissolves tsunami at impact without flooding city");
        yield return new WaitForSeconds(disaster.afetSuresi);
        CloseReport(); yield return null;
        Check(city.CurrentMode == IslandCityLife.CityLifeMode.Normal && actors.Cast<Transform>().Count(t=>
            t.gameObject.activeSelf && (t.name.StartsWith("Yaya") || t.name.StartsWith("SokakHayvani")))==city.LivingActorCount,
            "All sheltered residents and animals return after the report");
        ResetBuildings(); Vector3 cameraBefore=Camera.main.transform.position;
        disaster.AfetBaslat(2); yield return new WaitForSecondsRealtime(.25f);
        IslandGroundQuake groundQuake=FindAnyObjectByType<IslandGroundQuake>();
        Check(groundQuake!=null&&groundQuake.GroundShaking&&groundQuake.CracksVisible,
            "Earthquake shakes gravel and reveals ground cracks");
        yield return new WaitForSeconds(disaster.afetSuresi+1);
        Check(gm.temelBinaHasarli && gm.okulHasarli && gm.akutHasarli && gm.itfaiyeHasarli, "Earthquake controller damages every underdeveloped service type");
        bool brokenRoof=false;
        foreach(BuildingVisuals houseVisual in city.houses)
        {
            if(houseVisual==null)continue;
            bool broken=houseVisual.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&f.sharedMesh.name=="KirilmisKiremitCati");
            Transform effects=houseVisual.transform.Find("KaliciHasarEfektleri");
            if(broken&&effects!=null&&effects.Find("KirikCatiKiremitleri")!=null){brokenRoof=true;break;}
        }
        Check(brokenRoof,"Earthquake damage leaves a broken tile roof and scattered roof debris");
        Check(Vector3.Distance(cameraBefore,Camera.main.transform.position)<.05f, "Earthquake camera shake restores the original camera position");
        CaptureDetail("Logs/Ada_DepremHasari.png",new Vector3(-34,4,8));
        CloseReport(); yield return null;
        var popup = BinaPopup.Instance; popup.Ac(BinaTuru.Altyapi);
        yield return null;
        Check(popup.nominalPopupScale == 2 && popup.panel.activeSelf, "Building popup uses doubled nominal size and fits the screen");
        ScreenCapture.CaptureScreenshot("Logs/Ada_BuyukPopup.png");
        yield return new WaitForSecondsRealtime(.2f);
        popup.Kapat();
        city.Configure(disaster,city.landBounds,city.houses,city.gatheringAreas);
        yield return null;yield return null;
        Check(city.CitizenCount==95 && city.AnimalCount==5 && city.LivingActorCount==100 && city.VehicleCount==12 &&
            city.transform.Cast<Transform>().Count(t=>t.name=="SehirYasami")==1,
            "Runtime reconfiguration replaces the full population and shared instance caches cleanly");
        log.AppendLine("PASS: ALL ISLAND SYSTEM INTEGRATION CHECKS");
    }
    void ResetBuildings()
    {
        gm.gun=1; gm.nufus=1000; gm.oyunBittiMi=false; gm.para=50000;gm.farkindalikYuzdesi=100f;
        foreach(BinaTuru type in Enum.GetValues(typeof(BinaTuru))) {gm.LevelAyarla(type,1);gm.HasarliAyarla(type,false);}
    }
    void CloseReport(){ report.Devam(); gm.kullaniciDurdurdu=true; }
    void CaptureDetail(string path, Vector3 center)
    {
        Camera camera=Camera.main; Vector3 previous=camera.transform.position; float size=camera.orthographicSize;
        camera.transform.position=center-camera.transform.forward*230; camera.orthographicSize=32; Capture(path);
        camera.transform.position=previous; camera.orthographicSize=size;
    }
    void Capture(string path)
    {
        Camera camera=Camera.main; RenderTexture rt=RenderTexture.GetTemporary(1600,1000,24);
        RenderTexture previous=camera.targetTexture, active=RenderTexture.active;
        camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
        Texture2D image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=previous; RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt); Destroy(image);
    }
}
#endif
