using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Temel Bağlantılar")]
    public Transform kamera;
    public Transform tsunamiSuyu;
    [Header("Ada ve dalga")]
    public Transform tsunamiDalgasi;
    public float tsunamiSetZ = 10f, tsunamiBaslangicZ = -45f, tsunamiBitisZ = 102f;
    public float tsunamiDalgaY = 1f, tsunamiSuYuksekligi = 3.02f, selSuYuksekligi = 2.92f;
    public float kuruZeminY = 2.4f;
    [Min(1)] public float afetSuresi = 8f;
    [Tooltip("Yalnız editörde Ctrl+Shift+1..4 ile elle afet denemesi. Normal oynanışta kapalı.")]
    public bool editorDebugAfetleri;
    public GameObject yanginEfekti, yagmurEfekti, yanginPrefab, tozBulutuPrefab;
    public Transform[] yanacakBinalar, temelBinalar;
    public Transform okulObj, akutObj, itfaiyeObj;
    public bool AfetDevamEdiyorMu { get; private set; }
    public AfetTuru AktifAfetTuru { get; private set; }
    public bool SavunmaYeterli { get; private set; }
    public float IncidentProgress {get;private set;}
    public int DefendedIncidentCount {get;private set;}
    public int DamagedInstanceCount=>incidentDamage.Count;
    public bool TsunamiImpactTriggered {get;private set;}
    public Vector3 FireOrigin {get;private set;}
    public int BurningBuildingCount {get;private set;}
    public bool FireAwaitingReport {get;private set;}
    public float FloodSurfaceY=>tsunamiSuyu!=null&&tsunamiSuyu.gameObject.activeSelf?tsunamiSuyu.position.y:float.NegativeInfinity;
    float incidentStarted,revealLimit=10000;
    BuildingVisuals[] islandBuildings;
    readonly HashSet<BuildingVisuals> incidentDamage=new HashSet<BuildingVisuals>();
    readonly HashSet<BinaTuru> damagedTypes=new HashSet<BinaTuru>();
    Vector3[] waterVertices;
    int[] waterTriangles;

    int sonKontrolEdilenGun;
    readonly HashSet<int> gerceklesenAfetler = new HashSet<int>();
    readonly List<GameObject> geciciEfektler = new List<GameObject>();
    MaterialPropertyBlock suBlock;
    Renderer suRenderer;
    Vector3 kameraIlk, dalgaOlcek = Vector3.one;
    bool kameraSallaniyor;

    void Start()
    {
        if(GetComponent<IslandTsunamiFinale>()==null)gameObject.AddComponent<IslandTsunamiFinale>();
    }

    void Update()
    {
        if(AfetDevamEdiyorMu)IncidentProgress=Mathf.Clamp01((Time.time-incidentStarted)/Mathf.Max(.1f,afetSuresi));
        var gm = GameManager.Instance;
        // A hidden/removed report must not leave a previous incident burning forever.
        if(FireAwaitingReport && (gm==null || !gm.afetOlduMu)) ExtinguishReportedFire();
        if (AfetDevamEdiyorMu || gm == null || gm.afetOlduMu || gm.oyunBittiMi || gm.oyunDurduMu || gm.kullaniciDurdurdu) return;
        if (gm.gun != sonKontrolEdilenGun)
        {
            sonKontrolEdilenGun = gm.gun;
            int index = DengeAyarlari.Aktif.BuGunkuAfetIndex(gm.gun);
            if (index >= 0 && !gerceklesenAfetler.Contains(index)) AfetBaslat(index);
        }
#if UNITY_EDITOR
        if(editorDebugAfetleri&&Input.GetKey(KeyCode.LeftControl)&&Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) AfetBaslat(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) AfetBaslat(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) AfetBaslat(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) AfetBaslat(3);
        }
#endif
    }

    public void AfetBaslat(int index)
    {
        var gm = GameManager.Instance;
        var afetler = DengeAyarlari.Aktif.afetler;
        if (AfetDevamEdiyorMu || gm == null || gm.oyunBittiMi || gm.afetOlduMu || index < 0 || index >= afetler.Length) return;
        gerceklesenAfetler.Add(index);
        StartCoroutine(AfetSureciYonet(afetler[index]));
    }

    IEnumerator AfetSureciYonet(DengeAyarlari.AfetAyari afet)
    {
        var gm = GameManager.Instance;
        var d = DengeAyarlari.Aktif;
        AfetDevamEdiyorMu = gm.afetSuruyor = true;
        ExtinguishReportedFire();
        incidentStarted=Time.time;IncidentProgress=0;TsunamiImpactTriggered=false;BurningBuildingCount=0;
        incidentDamage.Clear();damagedTypes.Clear();
        if(islandBuildings==null)islandBuildings=FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None);
        foreach(var b in islandBuildings)if(b!=null)b.EnableLocalDamage();
        CalibrateFloodWaterline();
        AktifAfetTuru = afet.afet;
        var yetersizler = new List<BinaTuru>();
        foreach (BinaTuru b in afet.kontrolEdilen)
            if (gm.Level(b) < afet.gerekenLevel || gm.Hasarli(b)) yetersizler.Add(b);
        SavunmaYeterli = yetersizler.Count == 0;
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.AfetMuzigiBaslat(afet.afet);
        try
        {
            switch (afet.afet)
            {
                case AfetTuru.Deprem:
                    StartCoroutine(KameraSalla(afetSuresi, .4f));
                    yield return new WaitForSeconds(afetSuresi / 3);
                    SelectEarthquakeDamage(yetersizler,afet.gerekenLevel);
                    yield return new WaitForSeconds(afetSuresi * 2 / 3);
                    break;
                case AfetTuru.Yangin:
                    yield return YanginEfektiOynat(!SavunmaYeterli);
                    break;
                case AfetTuru.Sel:
                    if (yagmurEfekti != null) yagmurEfekti.SetActive(true);
                    if (!SavunmaYeterli) yield return SuYukseltIndir(selSuYuksekligi, afetSuresi);
                    else yield return new WaitForSeconds(afetSuresi);
                    break;
                case AfetTuru.Tsunami:
                    var finale=GetComponent<IslandTsunamiFinale>();
                    if(finale!=null&&finale.CanPlay&&!finale.HasPlayed)
                        yield return finale.Play(TsunamiEfekti(SavunmaYeterli));
                    else yield return TsunamiEfekti(SavunmaYeterli);
                    break;
            }
            foreach (BinaTuru b in damagedTypes)
            {
                int purchasedLevel=gm.Level(b);
                gm.HasarVer(b,afet.afet);
                // Damage is local. Reducing the shared category level would also shrink every untouched building.
                gm.LevelAyarla(b,purchasedLevel);
            }
            float baz = afet.bazKayip * (SavunmaYeterli ? d.savunmaYeterliCarpani : 1f);
            int akutEtkiLevel = gm.akutHasarli ? 0 : gm.akutLevel;
            float koruma = (1f - gm.farkindalikYuzdesi * d.farkindalikEtkisi) * (1f - akutEtkiLevel * d.akutEtkisi);
            int vefat = Mathf.Min(gm.nufus, Mathf.RoundToInt(baz * Mathf.Clamp01(koruma)));
            int kurtarilan = Mathf.Max(0, Mathf.RoundToInt(baz) - vefat);
            gm.nufus -= vefat;
            gm.sonAfetTuru = afet.afet;
            gm.sonSavunmaYeterli = SavunmaYeterli;
            gm.sonHasarliBinaSayisi = incidentDamage.Count;
            if(SavunmaYeterli)DefendedIncidentCount++;
            gm.sonVefatSayisi = vefat;
            gm.sonKurtarilanSayisi = kurtarilan;
            gm.toplamVefat += vefat;
            gm.toplamKurtarilan += kurtarilan;
            gm.sonAfetMesaji = afet.afet==AfetTuru.Yangin&&SavunmaYeterli
                ? "İtfaiye yangını mahalleye yayılmadan kontrol altına aldı. Etkilenen yapılar onarım bekliyor."
                : SavunmaYeterli ? "Önlemler işe yaradı!" : "Savunma yetersiz kaldı.";
            gm.afetOzeti += AfetRaporu.AfetAdi(afet.afet) + ": " + (SavunmaYeterli
                ? afet.afet==AfetTuru.Yangin?"Kontrol altına alındı":"Korundu" : "Hasar aldı") + " (" + vefat + " vefat)\n";
            gm.afetOlduMu = true;
            var ending=GetComponent<IslandTsunamiFinale>();
            if(afet.afet==AfetTuru.Tsunami&&ending!=null&&ending.IsComplete)
                gm.AfetSonrasiDevam();
        }
        finally
        {
            // All preparations still allow a visible local fire. Extinguish once the
            // report actually appears; soot, local damage and repair costs remain.
            if(afet.afet==AfetTuru.Yangin&&gm.afetOlduMu)FireAwaitingReport=true;
            else EfektleriTemizle();
            AfetDevamEdiyorMu = gm.afetSuruyor = false;
            if (MusicPlayer.Instance != null&&!gm.oyunBittiMi) MusicPlayer.Instance.NormalMuzigeDon();
        }
    }

    void CalibrateFloodWaterline()
    {
        float roadTop=kuruZeminY+.34f;
        foreach(var building in islandBuildings)
        {
            if(building==null||building.binaTuru!=BinaTuru.Altyapi)continue;
            foreach(var model in building.levelModelleri)
            {
                if(model==null||!model.activeSelf)continue;
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.bounds.max.y<kuruZeminY+1)roadTop=Mathf.Max(roadTop,renderer.bounds.max.y);
            }
        }
        // Roads are raised above the grass. A grass-relative plane at +0.30 m left them dry.
        selSuYuksekligi=Mathf.Max(selSuYuksekligi,roadTop+.18f);
        tsunamiSuYuksekligi=Mathf.Max(tsunamiSuYuksekligi,selSuYuksekligi+.10f);
    }

    void TozEfekti(Transform bina)
    {
        if (bina == null) return;
        geciciEfektler.Add(IslandEffectsFactory.CreateDust(transform, bina.position + Vector3.up * .4f, 1.5f));
    }

    void Damage(BuildingVisuals building,AfetTuru cause)
    {
        if(building==null||!incidentDamage.Add(building))return;
        building.MarkLocalDamage(cause);damagedTypes.Add(building.binaTuru);
    }

    void SelectEarthquakeDamage(List<BinaTuru> vulnerable,int requiredLevel)
    {
        if(SavunmaYeterli)return;
        BuildingVisuals fallback=null;
        foreach(var b in islandBuildings)
        {
            if(b==null||!vulnerable.Contains(b.binaTuru))continue;
            if(GameManager.Instance.Level(b.binaTuru)>=requiredLevel&&!b.IsDamaged)continue;
            fallback=b;
            // Weak homes fail in scattered clusters; strong, undamaged categories do not collapse.
            if(Random.value>(b.binaTuru==BinaTuru.TemelBina?.48f:.65f))continue;
            Damage(b,AfetTuru.Deprem);TozEfekti(b.transform);
        }
        if(incidentDamage.Count==0&&fallback!=null){Damage(fallback,AfetTuru.Deprem);TozEfekti(fallback.transform);}
    }

    void SelectWaterDamage(AfetTuru cause)
    {
        if(SavunmaYeterli)return;
        foreach(var b in islandBuildings)
        {
            if(b==null||incidentDamage.Contains(b))continue;
            if(b.binaTuru!=BinaTuru.TemelBina&&b.binaTuru!=BinaTuru.Altyapi&&
                !(cause==AfetTuru.Tsunami&&b.binaTuru==BinaTuru.DenizSeti))continue;
            if(!WaterReaches(b.transform.position))continue;
            // Selection is stable within an incident. Flood damage stays patchy rather than erasing every road.
            float sample=Mathf.Repeat(Mathf.Sin(b.transform.position.x*12.9898f+b.transform.position.z*78.233f+sonKontrolEdilenGun)*43758.5453f,1);
            float chance=b.binaTuru==BinaTuru.Altyapi?.42f:b.binaTuru==BinaTuru.DenizSeti?.30f:.22f;
            if(sample<chance)Damage(b,cause);
        }
    }

    public bool WaterReaches(Vector3 position)
    {
        if(!AfetDevamEdiyorMu||SavunmaYeterli||FloodSurfaceY<kuruZeminY+.10f||
            position.z>revealLimit||position.y>FloodSurfaceY+.65f)return false;
        if(waterVertices==null&&tsunamiSuyu!=null)
        {
            var f=tsunamiSuyu.GetComponent<MeshFilter>();
            if(f!=null&&f.sharedMesh!=null&&f.sharedMesh.isReadable){waterVertices=f.sharedMesh.vertices;waterTriangles=f.sharedMesh.triangles;}
        }
        if(waterVertices==null)return false;
        Vector3 p=tsunamiSuyu.InverseTransformPoint(position);
        for(int i=0;i<waterTriangles.Length;i+=3)
        {
            Vector3 a=waterVertices[waterTriangles[i]],b=waterVertices[waterTriangles[i+1]],c=waterVertices[waterTriangles[i+2]];
            if(Mathf.Abs((b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x))<.00001f)continue;
            float x=(p.x-b.x)*(a.z-b.z)-(a.x-b.x)*(p.z-b.z);
            float y=(p.x-c.x)*(b.z-c.z)-(b.x-c.x)*(p.z-c.z);
            float z=(p.x-a.x)*(c.z-a.z)-(c.x-a.x)*(p.z-a.z);
            if(!((x<0||y<0||z<0)&&(x>0||y>0||z>0)))return true;
        }
        return false;
    }
    public Vector3 WaterCurrentAt(Vector3 position)
    {
        if(!WaterReaches(position))return Vector3.zero;
        return AktifAfetTuru==AfetTuru.Tsunami?new Vector3(Mathf.Sin(position.x*.08f)*1.2f,0,19f)
            :new Vector3(1.4f+Mathf.Sin(position.z*.07f)*.4f,0,2.0f);
    }

    IEnumerator KameraSalla(float sure, float siddet)
    {
        if (kamera == null) yield break;
        var navigation=kamera.GetComponent<IslandCameraController>();
        if(navigation!=null){navigation.Shake(sure,siddet);yield break;}
        kameraIlk = kamera.localPosition; kameraSallaniyor = true;
        for (float t = 0; t < sure; t += Time.deltaTime)
        {
            if(IslandSimulationPause.IsPaused){yield return null;continue;}
            kamera.localPosition = kameraIlk + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * siddet * (1 - t / sure);
            yield return null;
        }
        kamera.localPosition = kameraIlk; kameraSallaniyor = false;
    }

    IEnumerator YanginEfektiOynat(bool hasar)
    {
        var candidates=new List<Transform>();
        if(yanacakBinalar!=null)foreach(var b in yanacakBinalar)if(b!=null&&b!=itfaiyeObj)candidates.Add(b);
        if(candidates.Count==0){yield return new WaitForSeconds(afetSuresi);yield break;}
        Vector3 origin=candidates[Random.Range(0,candidates.Count)].position;
        FireOrigin=origin;
        candidates.Sort((a,b)=>(a.position-origin).sqrMagnitude.CompareTo((b.position-origin).sqrMagnitude));
        int count=Mathf.Min(candidates.Count,hasar?9:3),next=0;
        for(float t=0;t<afetSuresi;t+=Time.deltaTime)
        {
            if(IslandSimulationPause.IsPaused){yield return null;continue;}
            if(next<count&&t>=next*afetSuresi*.60f/Mathf.Max(1,count-1))
            {
                Transform bina=candidates[next++];
                if((bina.position-origin).sqrMagnitude>32*32){count=next;continue;}
                Collider col = bina.GetComponent<Collider>();
                Vector3 p = bina.position; float size = 1;
                if (col != null) { p.y = col.bounds.max.y - .15f; size = Mathf.Clamp(col.bounds.size.x * .16f, .7f, 1.8f); }
                geciciEfektler.Add(IslandEffectsFactory.CreateFire(transform, p, size));
                BurningBuildingCount++;
                Damage(bina.GetComponent<BuildingVisuals>(),AfetTuru.Yangin);
            }
            yield return null;
        }
    }

    void SuAyarla(float y, float revealZ = 10000)
    {
        if (tsunamiSuyu == null) return;
        revealLimit=revealZ;
        tsunamiSuyu.gameObject.SetActive(true);
        Vector3 p = tsunamiSuyu.position; p.y = y; tsunamiSuyu.position = p;
        if (suRenderer == null) suRenderer = tsunamiSuyu.GetComponent<Renderer>();
        if (suRenderer != null) { if (suBlock == null) suBlock = new MaterialPropertyBlock(); suBlock.SetFloat("_RevealZ", revealZ); suRenderer.SetPropertyBlock(suBlock); }
    }

    IEnumerator SuYukseltIndir(float target, float sure)
    {
        for (float t = 0; t < sure; t += Time.deltaTime)
        {
            if(IslandSimulationPause.IsPaused){yield return null;continue;}
            float u = t / sure;
            float height = u < .32f ? Mathf.SmoothStep(kuruZeminY - .3f, target, u / .32f) :
                u > .78f ? Mathf.SmoothStep(target, kuruZeminY - .3f, (u - .78f) / .22f) : target;
            SuAyarla(height);
            if(u>.35f&&u<.72f)SelectWaterDamage(AfetTuru.Sel);
            yield return null;
        }
    }

    IEnumerator TsunamiEfekti(bool korundu)
    {
        if (tsunamiDalgasi != null) { dalgaOlcek = tsunamiDalgasi.localScale; tsunamiDalgasi.gameObject.SetActive(true); }
        float travelTime = afetSuresi * .72f;
        float impactTime = travelTime * Mathf.InverseLerp(tsunamiBaslangicZ, tsunamiBitisZ, tsunamiSetZ);
        for (float t = 0; t < afetSuresi; t += Time.deltaTime)
        {
            if(IslandSimulationPause.IsPaused){yield return null;continue;}
            float z = Mathf.Lerp(tsunamiBaslangicZ, tsunamiBitisZ, t / travelTime);
            float waveHeight = 1, y = tsunamiDalgaY;
            if(!TsunamiImpactTriggered&&t>=impactTime)
            {
                TsunamiImpactTriggered=true;
                if(!korundu)foreach(var b in islandBuildings)
                    if(b!=null&&b.binaTuru==BinaTuru.DenizSeti&&b.transform.position.z<tsunamiSetZ+15&&Random.value<.6f)Damage(b,AfetTuru.Tsunami);
                float impactY=kuruZeminY+(korundu?5.0f:1.8f);
                geciciEfektler.Add(IslandEffectsFactory.CreateSeaImpact(transform,new Vector3(0,impactY,tsunamiSetZ-.8f),korundu?1.2f:.85f));
                StartCoroutine(KameraSalla(afetSuresi*.14f,korundu?.25f:.38f));
            }
            if (korundu && t >= impactTime)
            {
                float melt = Mathf.SmoothStep(0, 1, (t - impactTime) / (afetSuresi*.28f));
                z = tsunamiSetZ - melt * 9; y -= melt * 3;
                waveHeight = 1 - melt;
            }
            else if (!korundu)
            {
                // Dirty blue water advances behind the crest, clipped to the island's interior mesh.
                float rise = Mathf.SmoothStep(0, 1, (t - impactTime) / (afetSuresi*.12f));
                float recede = Mathf.SmoothStep(0, 1, (t - travelTime) / Mathf.Max(.1f, afetSuresi - travelTime));
                SuAyarla(Mathf.Lerp(kuruZeminY - .3f, tsunamiSuYuksekligi, rise * (1 - recede)), z - 3);
                SelectWaterDamage(AfetTuru.Tsunami);
            }
            if (tsunamiDalgasi != null)
            {
                tsunamiDalgasi.position = new Vector3(tsunamiDalgasi.position.x, y, z);
                tsunamiDalgasi.localScale = Vector3.Scale(dalgaOlcek, new Vector3(1, Mathf.Max(.001f, waveHeight), 1));
                if (t >= travelTime || waveHeight <= .001f) tsunamiDalgasi.gameObject.SetActive(false);
            }
            yield return null;
        }
    }

    void EfektleriTemizle()
    {
        FireAwaitingReport=false;
        foreach (GameObject go in geciciEfektler) if (go != null) { go.SetActive(false); Destroy(go); }
        geciciEfektler.Clear();
        if (tsunamiSuyu != null) tsunamiSuyu.gameObject.SetActive(false);
        if (tsunamiDalgasi != null) { tsunamiDalgasi.gameObject.SetActive(false); tsunamiDalgasi.localScale = dalgaOlcek; }
        if (yagmurEfekti != null) yagmurEfekti.SetActive(false);
        if (kameraSallaniyor && kamera != null) kamera.localPosition = kameraIlk;
        if(kamera!=null)kamera.GetComponent<IslandCameraController>()?.StopShake();
        kameraSallaniyor = false;
    }

    public void ExtinguishReportedFire()
    {
        if(FireAwaitingReport)EfektleriTemizle();
    }

    void OnDisable()
    {
        StopAllCoroutines(); EfektleriTemizle(); AfetDevamEdiyorMu = false;
        if (GameManager.Instance != null) GameManager.Instance.afetSuruyor = false;
    }
}
