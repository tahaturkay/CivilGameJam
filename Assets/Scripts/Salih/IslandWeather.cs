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

    Light sun;
    Color sunColor,sky,equator,ground,ambientLight;
    float sunIntensity,ambientIntensity;
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
        if(sun!=null){sunColor=sun.color;sunIntensity=sun.intensity;}
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
        viewCamera=Camera.main;CreateClouds();nextLightning=Time.time+7;
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
        float speed=1-Mathf.Exp(-Mathf.Min(Time.unscaledDeltaTime,.1f)*.9f);
        storm=Mathf.Lerp(storm,targetStorm,speed);warmth=Mathf.Lerp(warmth,targetWarmth,speed);
        if(RainObject!=null&&RainObject.activeSelf!=flood)RainObject.SetActive(flood);
        if(flood&&Time.time>=nextLightning)
        {
            lightningRemaining=.21f;nextLightning=Time.time+7+Mathf.PerlinNoise(Time.time*.17f,9.1f)*7;
        }
        lightningRemaining=flood?Mathf.Max(0,lightningRemaining-Time.deltaTime):0;
        float flash=lightningRemaining>0?Mathf.Abs(Mathf.Sin(lightningRemaining*44))*.7f:0;
        ApplyAtmosphere(flash);
        UpdateClouds();
    }

    void ApplyAtmosphere(float flash)
    {
        Color cool=new Color(.67f,.76f,.82f),warm=new Color(1,.49f,.35f);
        if(sun!=null)
        {
            sun.color=Color.Lerp(Color.Lerp(sunColor,cool,storm*.55f),warm,warmth*.48f);
            sun.intensity=sunIntensity*(1-storm*.55f-warmth*.08f)+flash*.8f;
        }
        RenderSettings.ambientSkyColor=Color.Lerp(sky,sky*new Color(.65f,.73f,.83f),storm*.8f)+Color.white*(flash*.12f);
        RenderSettings.ambientEquatorColor=Color.Lerp(equator,equator*new Color(.70f,.76f,.80f),storm*.8f);
        RenderSettings.ambientGroundColor=Color.Lerp(ground,ground*.76f,storm*.7f);
        RenderSettings.ambientLight=Color.Lerp(ambientLight,ambientLight*cool,storm*.35f);
        RenderSettings.ambientIntensity=ambientIntensity*(1-storm*.15f);
        if(colors!=null)
        {
            Color filter=Color.Lerp(baseFilter,new Color(.78f,.85f,.90f),storm*.65f);
            colors.colorFilter.value=Color.Lerp(filter,new Color(1,.63f,.53f),warmth*.4f);
            colors.postExposure.value=baseExposure-storm*.20f+flash*.35f;
            colors.saturation.value=baseSaturation-storm*14-warmth*2;
            colors.contrast.value=baseContrast-storm*3+warmth*3;
        }
        if(weatherVolume!=null)weatherVolume.weight=Mathf.Max(storm,warmth);
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
        if(!initialized)return;
        if(sun!=null){sun.color=sunColor;sun.intensity=sunIntensity;}
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
        initialized=false;storm=warmth=0;manualDisaster=null;
    }
    void OnEnable(){if(Application.isPlaying&&initialized==false)storm=warmth=0;}
}
