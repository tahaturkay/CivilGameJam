using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One civic warning mast; reads the existing calendar and active disaster directly.</summary>
[DisallowMultipleComponent]
public sealed class IslandSirenController : MonoBehaviour
{
    IslandSirenArt.Rig rig;
    DisasterController disaster;
    GameManager gm;
    Material glowMaterial;
    Mesh sweepMesh;
    Transform sweep;
    MaterialPropertyBlock lensBlock;
    float alarmTime;

    public bool AlarmActive {get;private set;}
    public bool TsunamiAdvanceWarning {get;private set;}
    public Transform Tower => rig!=null?rig.Root:null;
    public float RotorYaw => rig!=null?rig.Rotor.localEulerAngles.y:0;

    void Start()
    {
        gm=GameManager.Instance;disaster=FindAnyObjectByType<DisasterController>();
        Transform hall=transform.Find("SabitAlanlar/Belediye");
        if(hall==null){enabled=false;return;}
        rig=IslandSirenArt.Create(hall);
        rig.Root.localPosition=new Vector3(8.2f,.25f,-7.8f);
        lensBlock=new MaterialPropertyBlock();
        Material source=Resources.Load<Material>("IslandEffects/NightGlow");
        if(source!=null)
        {
            glowMaterial=new Material(source){name="Siren / Hafif Kırmızı Yansıma"};
            glowMaterial.SetColor("_BaseColor",new Color(1,.025f,.01f,.13f));
            sweepMesh=new Mesh{name="Siren / İki Yönlü Işık Yelpazesi",vertices=new[]{Vector3.zero,new Vector3(-.5f,0,1),new Vector3(.5f,0,1),Vector3.zero,new Vector3(.5f,0,-1),new Vector3(-.5f,0,-1)},
                uv=new[]{new Vector2(.5f,0),new Vector2(0,1),new Vector2(1,1),new Vector2(.5f,0),new Vector2(0,1),new Vector2(1,1)},triangles=new[]{0,1,2,3,4,5}};
            sweepMesh.RecalculateBounds();
            var obj=new GameObject("SirenKırmızıZeminTaraması",typeof(MeshFilter),typeof(MeshRenderer));sweep=obj.transform;
            sweep.SetParent(rig.Root,false);sweep.localPosition=Vector3.up*.05f;sweep.localScale=new Vector3(18,1,27);
            obj.GetComponent<MeshFilter>().sharedMesh=sweepMesh;
            var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=glowMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        SetLights(false,0);
    }

    void Update()
    {
        if(rig==null)return;
        if(gm==null)gm=GameManager.Instance;
        int tsunamiDay=IslandDisasterCalendar.TsunamiDay;
        TsunamiAdvanceWarning=gm!=null&&!gm.oyunBittiMi&&gm.gun==tsunamiDay-1;
        bool running=disaster!=null&&disaster.AfetDevamEdiyorMu;
        bool tsunamiStarting=gm!=null&&gm.gun==tsunamiDay&&!gm.afetOlduMu&&!gm.oyunBittiMi;
        AlarmActive=TsunamiAdvanceWarning||running||tsunamiStarting;
        bool paused=PauseMenu.DurdurulduMu||(gm!=null&&(gm.kullaniciDurdurdu||gm.oyunDurduMu||gm.afetOlduMu||gm.oyunBittiMi));
        if(AlarmActive&&!paused)
            alarmTime+=Time.deltaTime*(gm!=null?Mathf.Clamp(gm.hizCarpani,1,3):1);
        float pulse=.8f+.2f*Mathf.Sin(alarmTime*5);
        rig.Rotor.localRotation=Quaternion.Euler(0,alarmTime*115,0);
        if(sweep!=null){sweep.localRotation=rig.Rotor.localRotation;sweep.gameObject.SetActive(AlarmActive);}
        SetLights(AlarmActive,pulse);
    }

    void SetLights(bool active,float pulse)
    {
        if(rig==null)return;
        rig.BeaconPoint.enabled=active;rig.BeaconPoint.intensity=active?1.7f*pulse:0;
        foreach(var light in rig.RotatingSpots){light.enabled=active;light.intensity=active?5*pulse:0;}
        lensBlock.SetColor("_EmissionColor",active?new Color(3.4f,.035f,.015f)*pulse:Color.black);
        foreach(var renderer in rig.IndicatorRenderers)renderer.SetPropertyBlock(lensBlock);
        if(sweep!=null)sweep.gameObject.SetActive(active);
    }

    void OnDisable(){AlarmActive=TsunamiAdvanceWarning=false;if(lensBlock!=null)SetLights(false,0);}
    void OnDestroy()
    {
        if(rig!=null&&rig.Root!=null)Destroy(rig.Root.gameObject);
        if(glowMaterial!=null)Destroy(glowMaterial);if(sweepMesh!=null)Destroy(sweepMesh);
    }
}
