using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Forecast, storm and fire atmosphere. All scene lighting/profile changes are restored on disable.</summary>
[DisallowMultipleComponent]
public class IslandWeather : MonoBehaviour
{
    [SerializeField] DisasterController disaster;
    [SerializeField] Bounds landBounds=new Bounds(new Vector3(0,2.4f,0),new Vector3(190,.1f,146));
    public GameObject RainObject {get;private set;}
    public bool RainActive => RainObject!=null&&RainObject.activeSelf;
    public float StormAmount => storm;
    public float NightAmount {get;private set;}
    public float DawnAmount {get;private set;}
    public float DuskAmount {get;private set;}
    public bool IsNight => NightAmount>=.5f;
    public bool DisasterDaylightActive {get;private set;}
    public bool CinematicSunsetActive {get;private set;}
    Vector3 cinematicSunDirection;
    float cinematicSunsetBlend;

    /// <summary>The harbor finale authors a sunset without changing other disaster palettes.</summary>
    public void SetCinematicSunset(bool active,Vector3 towardSun)
    {
        CinematicSunsetActive=active;
        if(towardSun.sqrMagnitude>.001f)cinematicSunDirection=towardSun.normalized;
    }
    EconomyAndTime dayClock;

    Light sun;
    Color sunColor,sky,equator,ground,ambientLight;
    float sunIntensity,ambientIntensity;
    Quaternion originalSunRotation;
    IslandSolarCycle.Sample solar;
    Volume weatherVolume;
    VolumeProfile runtimeProfile;
    ColorAdjustments colors;
    Color baseFilter;
    float baseExposure,baseSaturation,baseContrast;
    Transform cloudsRoot;
    Transform[] cloudTransforms;
    Renderer[] cloudRenderers;
    MaterialPropertyBlock cloudBlock;
    Camera viewCamera;
    Color originalCameraBackground;
    AfetTuru? manualDisaster;
    bool initialized;
    float storm,warmth,nextLightning,lightningRemaining;

    public void Configure(DisasterController controller,Bounds islandBounds)
    {
        disaster=controller;landBounds=islandBounds;
        if(Application.isPlaying&&initialized)
        {
            if(RainObject!=null)Destroy(RainObject);
            RainObject=IslandEffectsFactory.CreateRain(transform,landBounds);
        }
    }

    /// <summary>Optional explicit control; controller state is authoritative when a controller is configured.</summary>
    public void SetDisaster(AfetTuru? type,bool active){manualDisaster=active?type:null;}

    void Start(){Initialize();}

    void Initialize()
    {
        if(initialized||!Application.isPlaying)return;
        initialized=true;
        sun=RenderSettings.sun;
        if(sun==null)
        {
            Light[] lights=FindObjectsByType<Light>();
            for(int i=0;i<lights.Length;i++)if(lights[i].type==LightType.Directional&&lights[i].enabled){sun=lights[i];break;}
        }
        if(sun!=null){sunColor=sun.color;sunIntensity=sun.intensity;originalSunRotation=sun.transform.rotation;}
        sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
        ambientLight=RenderSettings.ambientLight;ambientIntensity=RenderSettings.ambientIntensity;
        Volume source=null;
        Volume[] volumes=FindObjectsByType<Volume>();
        for(int i=0;i<volumes.Length;i++)if(volumes[i].isGlobal&&volumes[i].sharedProfile!=null&&(source==null||volumes[i].priority>source.priority))source=volumes[i];
        // Profile Instantiate alone leaves VolumeComponent asset references shared: clone every component too.
        runtimeProfile=ScriptableObject.CreateInstance<VolumeProfile>();
        if(source!=null)
        {
            foreach(VolumeComponent component in source.sharedProfile.components)
            {
                if(component==null)continue;
                VolumeComponent copy=Instantiate(component);copy.hideFlags=HideFlags.DontSave;runtimeProfile.components.Add(copy);
            }
        }
        runtimeProfile.name="Ada Hava Durumu / Runtime Profil";runtimeProfile.hideFlags=HideFlags.DontSave;
        if(!runtimeProfile.TryGet(out colors))colors=runtimeProfile.Add<ColorAdjustments>(true);
        baseFilter=colors.colorFilter.value;baseExposure=colors.postExposure.value;baseSaturation=colors.saturation.value;baseContrast=colors.contrast.value;
        colors.colorFilter.overrideState=colors.postExposure.overrideState=colors.saturation.overrideState=colors.contrast.overrideState=true;
        var volumeObject=new GameObject("GeciciHavaDurumuVolume");volumeObject.transform.SetParent(transform,false);
        weatherVolume=volumeObject.AddComponent<Volume>();weatherVolume.isGlobal=true;weatherVolume.priority=source!=null?source.priority+10:10;
        weatherVolume.sharedProfile=runtimeProfile;weatherVolume.weight=0;
        RainObject=IslandEffectsFactory.CreateRain(transform,landBounds);
        dayClock=FindAnyObjectByType<EconomyAndTime>();
        viewCamera=Camera.main;if(viewCamera!=null)originalCameraBackground=viewCamera.backgroundColor;CreateClouds();nextLightning=Time.time+7;
    }

    void CreateClouds()
    {
        cloudsRoot=new GameObject("KameraArkasindakiYumusakBulutlar").transform;cloudsRoot.SetParent(transform,false);
        cloudTransforms=new Transform[8];cloudRenderers=new Renderer[8];cloudBlock=new MaterialPropertyBlock();
        for(int i=0;i<8;i++)
        {
            var cloud=new GameObject("FirtinaBulutu_"+i,typeof(MeshFilter),typeof(MeshRenderer));cloud.transform.SetParent(cloudsRoot,false);
            cloud.GetComponent<MeshFilter>().sharedMesh=IslandEffectsFactory.SoftQuadMesh;
            var renderer=cloud.GetComponent<MeshRenderer>();renderer.sharedMaterial=IslandEffectsFactory.SmokeMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            cloudTransforms[i]=cloud.transform;cloudRenderers[i]=renderer;
        }
    }

    float Forecast()
    {
        GameManager gm=GameManager.Instance;if(gm==null)return 0;
        var settings=DengeAyarlari.Aktif;if(settings==null||settings.afetler==null)return 0;
        for(int i=0;i<settings.afetler.Length;i++)
            if(settings.afetler[i].afet==AfetTuru.Sel&&gm.gun==settings.afetler[i].gun-1)return .5f;
        return 0;
    }

    void Update()
    {
        if(!initialized)Initialize();if(!initialized)return;
        AfetTuru? active=disaster!=null?(disaster.AfetDevamEdiyorMu?(AfetTuru?)disaster.AktifAfetTuru:null):manualDisaster;
        bool flood=active==AfetTuru.Sel,fire=active==AfetTuru.Yangin;
        float targetStorm=flood?1:Forecast(),targetWarmth=fire?1:0;
        float speed=1-Mathf.Exp(-Mathf.Min(Time.deltaTime,.1f)*.9f);
        storm=Mathf.Lerp(storm,targetStorm,speed);warmth=Mathf.Lerp(warmth,targetWarmth,speed);
        if(RainObject!=null&&RainObject.activeSelf!=flood)RainObject.SetActive(flood);
        if(flood&&Time.time>=nextLightning)
        {
            lightningRemaining=.21f;nextLightning=Time.time+7+Mathf.PerlinNoise(Time.time*.17f,9.1f)*7;
        }
        lightningRemaining=flood?Mathf.Max(0,lightningRemaining-Time.deltaTime):0;
        float phase=dayClock!=null?dayClock.DayProgress:0;
        solar=IslandSolarCycle.Evaluate(phase);
        var gm=GameManager.Instance;
        float daylight=active.HasValue?1:gm!=null?IslandDisasterCalendar.DaylightOverride(gm.gun,phase):0;
        DisasterDaylightActive=daylight>=.999f;
        solar=new IslandSolarCycle.Sample(solar.night*(1-daylight),solar.dawn*(1-daylight),solar.dusk*(1-daylight),
            Mathf.Lerp(solar.elevation,55,daylight),Mathf.LerpAngle(solar.azimuth,-25,daylight));
        cinematicSunsetBlend=Mathf.MoveTowards(cinematicSunsetBlend,CinematicSunsetActive?1:0,Time.deltaTime/1.8f);
        if(cinematicSunsetBlend>0)
            solar=new IslandSolarCycle.Sample(Mathf.Lerp(solar.night,0,cinematicSunsetBlend),
                solar.dawn*(1-cinematicSunsetBlend),Mathf.Lerp(solar.dusk,1,cinematicSunsetBlend),solar.elevation,solar.azimuth);
        NightAmount=solar.night;DawnAmount=solar.dawn;DuskAmount=solar.dusk;
        Shader.SetGlobalFloat("_IslandNight",NightAmount);
        Shader.SetGlobalFloat("_IslandStorm",storm);
        Shader.SetGlobalFloat("_IslandTwilight",solar.Twilight);
        Shader.SetGlobalColor("_IslandTwilightColor",Color.Lerp(new Color(1,.70f,.42f),new Color(.98f,.40f,.30f),DuskAmount));
        float flash=lightningRemaining>0?Mathf.Abs(Mathf.Sin(lightningRemaining*44))*.7f:0;
        ApplyAtmosphere(flash);
        UpdateClouds();
    }

    void ApplyAtmosphere(float flash)
    {
        Color cool=new Color(.67f,.76f,.82f),warm=new Color(1,.49f,.35f);
        float twilight=solar.Twilight;
        Color sunrise=new Color(1,.72f,.46f),sunset=new Color(1,.42f,.25f);
        Color sunTint=Color.Lerp(Color.Lerp(sunColor,new Color(.42f,.57f,.85f),NightAmount),sunrise,DawnAmount*.7f);
        sunTint=Color.Lerp(sunTint,sunset,DuskAmount*.72f);
        if(sun!=null)
        {
            sun.transform.rotation=Quaternion.Euler(solar.elevation,solar.azimuth,0);
            if(cinematicSunsetBlend>0)
                sun.transform.rotation=Quaternion.Slerp(sun.transform.rotation,Quaternion.LookRotation(-cinematicSunDirection),cinematicSunsetBlend);
            sun.color=Color.Lerp(Color.Lerp(sunTint,cool,storm*.55f),warm,warmth*.48f);
            sun.intensity=sunIntensity*Mathf.Lerp(1,.30f,NightAmount)*Mathf.Lerp(1,.78f,twilight)*(1-storm*.55f-warmth*.08f)+flash*.8f;
        }
        RenderSettings.ambientSkyColor=Color.Lerp(sky,sky*new Color(.65f,.73f,.83f),storm*.8f)+Color.white*(flash*.12f);
        RenderSettings.ambientEquatorColor=Color.Lerp(equator,equator*new Color(.70f,.76f,.80f),storm*.8f);
        RenderSettings.ambientGroundColor=Color.Lerp(ground,ground*.76f,storm*.7f);
        RenderSettings.ambientLight=Color.Lerp(ambientLight,ambientLight*cool,storm*.35f);
        RenderSettings.ambientIntensity=ambientIntensity*(1-storm*.15f);
        RenderSettings.ambientSkyColor=Color.Lerp(RenderSettings.ambientSkyColor,new Color(.16f,.21f,.31f),NightAmount);
        RenderSettings.ambientEquatorColor=Color.Lerp(RenderSettings.ambientEquatorColor,new Color(.10f,.13f,.19f),NightAmount);
        RenderSettings.ambientGroundColor=Color.Lerp(RenderSettings.ambientGroundColor,new Color(.08f,.11f,.13f),NightAmount);
        RenderSettings.ambientIntensity*=Mathf.Lerp(1,.85f,NightAmount);
        Color horizon=Color.Lerp(new Color(.73f,.50f,.34f),new Color(.56f,.27f,.31f),DuskAmount);
        RenderSettings.ambientSkyColor=Color.Lerp(RenderSettings.ambientSkyColor,horizon,twilight*.45f*(1-storm*.7f));
        RenderSettings.ambientEquatorColor=Color.Lerp(RenderSettings.ambientEquatorColor,new Color(.32f,.24f,.22f),twilight*.3f);
        if(viewCamera!=null)viewCamera.backgroundColor=Color.Lerp(Color.Lerp(new Color(.48f,.66f,.73f),new Color(.025f,.055f,.12f),NightAmount),horizon,twilight*.65f);
        if(colors!=null)
        {
            Color filter=Color.Lerp(baseFilter,new Color(.78f,.85f,.90f),storm*.65f);
            filter=Color.Lerp(filter,Color.Lerp(new Color(1,.90f,.76f),new Color(1,.77f,.68f),DuskAmount),twilight*.25f*(1-storm*.6f));
            colors.colorFilter.value=Color.Lerp(filter,new Color(1,.63f,.53f),warmth*.4f);
            colors.postExposure.value=baseExposure-storm*.20f-NightAmount*.12f-twilight*.06f+flash*.35f;
            colors.saturation.value=baseSaturation-storm*14-warmth*2;
            colors.contrast.value=baseContrast-storm*3+warmth*3;
        }
        if(weatherVolume!=null)weatherVolume.weight=Mathf.Max(twilight,Mathf.Max(NightAmount,Mathf.Max(storm,warmth)));
    }

    void UpdateClouds()
    {
        if(viewCamera==null)viewCamera=Camera.main;
        if(viewCamera==null||cloudTransforms==null)return;
        float depth=Mathf.Max(20,Vector3.Distance(viewCamera.transform.position,landBounds.center)-25);
        float viewportHeight=viewCamera.orthographic?viewCamera.orthographicSize*2:2*depth*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad);
        for(int i=0;i<cloudTransforms.Length;i++)
        {
            float x=Mathf.Repeat(i*.145f+Time.time*.0014f,1.2f)-.1f;
            // The cloud bank stays in the upper sky margin and above the ocean plane.
            float h=viewportHeight*(.08f+(i%3)*.012f),y=.96f+(i%2)*.025f;
            cloudTransforms[i].position=viewCamera.ViewportToWorldPoint(new Vector3(x,y,depth));
            cloudTransforms[i].rotation=viewCamera.transform.rotation;
            cloudTransforms[i].localScale=new Vector3(h*3.8f,h,1);
            Color tint=Color.Lerp(new Color(.55f,.58f,.61f),new Color(.16f,.19f,.22f),Mathf.Clamp01((storm-.45f)/.55f));
            tint.a=storm*(.65f+(i%3)*.06f);
            cloudBlock.SetColor("_BaseColor",tint);cloudBlock.SetColor("_Color",tint);cloudRenderers[i].SetPropertyBlock(cloudBlock);
        }
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_IslandNight",0);
        Shader.SetGlobalFloat("_IslandStorm",0);
        Shader.SetGlobalFloat("_IslandTwilight",0);
        NightAmount=0;
        if(viewCamera!=null)viewCamera.backgroundColor=originalCameraBackground;
        if(!initialized)return;
        if(sun!=null){sun.color=sunColor;sun.intensity=sunIntensity;sun.transform.rotation=originalSunRotation;}
        RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
        RenderSettings.ambientLight=ambientLight;RenderSettings.ambientIntensity=ambientIntensity;
        if(RainObject!=null){RainObject.SetActive(false);Destroy(RainObject);RainObject=null;}
        if(cloudsRoot!=null)Destroy(cloudsRoot.gameObject);
        if(weatherVolume!=null)Destroy(weatherVolume.gameObject);
        if(runtimeProfile!=null)
        {
            for(int i=0;i<runtimeProfile.components.Count;i++)if(runtimeProfile.components[i]!=null)Destroy(runtimeProfile.components[i]);
            Destroy(runtimeProfile);
        }
        initialized=false;storm=warmth=0;manualDisaster=null;CinematicSunsetActive=false;cinematicSunsetBlend=0;
    }
    void OnEnable(){if(Application.isPlaying&&initialized==false)storm=warmth=0;}
}
