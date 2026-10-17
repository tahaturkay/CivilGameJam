using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Emission and batched light pools; a small fixed light budget keeps the city affordable on Metal.</summary>
[DisallowMultipleComponent]
public sealed class IslandNightLighting : MonoBehaviour
{
    sealed class Replacement {public Renderer renderer;public Material[] original;}
    readonly List<Replacement> replacements=new List<Replacement>();
    readonly Dictionary<Material,Material> clones=new Dictionary<Material,Material>();
    readonly List<Transform> lamps=new List<Transform>(),cars=new List<Transform>();
    readonly List<Light> localLights=new List<Light>();
    readonly Matrix4x4[] pools=new Matrix4x4[256],headlights=new Matrix4x4[48];
    Material poolMaterial,beamMaterial,reflectionMaterial;
    Mesh fanMesh,coneMesh,wedgeMesh;
    GameObject runtimeRoot;
    Transform lighthouseHead,beam,reflection;
    Light lighthouseLight;
    IslandWeather weather;
    IslandCityLife city;
    int carsGeneration;
    float seaSurfaceY=-.575f;
    public float NightAmount=>weather!=null?weather.NightAmount:0;
    public int StreetLampCount=>lamps.Count;
    public int HeadlightVehicleCount=>cars.Count;
    public int RealLightBudget=>localLights.Count+(lighthouseLight!=null?1:0);
    public float LighthouseYaw=>lighthouseHead!=null?lighthouseHead.eulerAngles.y:0;

    void Start()
    {
        weather=FindAnyObjectByType<IslandWeather>();city=GetComponent<IslandCityLife>();
        Transform ocean=transform.Find("Arazi/Deniz");if(ocean!=null)seaSurfaceY=ocean.GetComponent<Renderer>().bounds.max.y+.025f;
        runtimeRoot=new GameObject("GeceAydinlatmasi");runtimeRoot.transform.SetParent(transform,false);
        Shader shader=Shader.Find("CivilIsland/Night Glow");
        poolMaterial=new Material(shader){name="Ada Sicak Isik Havuzlari",enableInstancing=true};
        beamMaterial=new Material(shader){name="Ada Deniz Feneri Huzmesi",enableInstancing=true};
        reflectionMaterial=new Material(beamMaterial);reflectionMaterial.SetFloat("_SeaOnly",1);
        fanMesh=CreateFan();coneMesh=CreateCone();wedgeMesh=CreateWedge();
        foreach(Transform t in GetComponentsInChildren<Transform>(true))if(t.name=="SokakFeneri")lamps.Add(t);
        ReplaceEmissiveSurfaces();
        Transform tower=transform.Find("DenizFeneri");
        if(tower!=null)
        {
            lighthouseHead=FindNamed(tower,"IsikKafasi");
            if(lighthouseHead!=null)
            {
                Transform source=FindNamed(lighthouseHead,"IsikKaynagi");if(source==null)source=lighthouseHead;
                beam=MeshObject("DonenIsikHuzmesi",source,coneMesh,beamMaterial).transform;
                beam.localScale=new Vector3(11,8,95);beam.localRotation=Quaternion.Euler(3,0,0);
                lighthouseLight=new GameObject("DenizFeneriSpot").AddComponent<Light>();lighthouseLight.transform.SetParent(source,false);
                lighthouseLight.type=LightType.Spot;lighthouseLight.range=130;lighthouseLight.spotAngle=22;lighthouseLight.color=new Color(1,.86f,.57f);lighthouseLight.shadows=LightShadows.None;
                reflection=MeshObject("DenizUzerindeIsikYansimasi",runtimeRoot.transform,wedgeMesh,reflectionMaterial).transform;
            }
        }
        for(int i=0;i<8;i++)
        {
            Light light=new GameObject(i<4?"YakinSokakIsigi":"YakinAracFari").AddComponent<Light>();light.transform.SetParent(runtimeRoot.transform,false);
            light.type=i<4?LightType.Point:LightType.Spot;light.range=i<4?11:17;light.spotAngle=48;
            light.color=new Color(1,.78f,.43f);light.shadows=LightShadows.None;light.enabled=false;localLights.Add(light);
        }
    }

    static Transform FindNamed(Transform parent,string name){foreach(Transform t in parent.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go;
    }

    void ReplaceEmissiveSurfaces()
    {
        foreach(Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if(r is ParticleSystemRenderer)continue;
            Material[] original=r.sharedMaterials,updated=(Material[])original.Clone();bool changed=false;
            for(int i=0;i<original.Length;i++)
            {
                Material m=original[i];if(m==null||!m.HasProperty("_BaseColor")||!m.HasProperty("_EmissionColor"))continue;
                if(m.name.EndsWith(" Gece Runtime"))continue;
                Color c=m.GetColor("_BaseColor");
                bool glass=Vector3.Distance(new Vector3(c.r,c.g,c.b),new Vector3(78/255f,126/255f,139/255f))<.015f;
                bool lamp=Vector3.Distance(new Vector3(c.r,c.g,c.b),new Vector3(244/255f,218/255f,151/255f))<.015f;
                if(!glass&&!lamp)continue;
                if(!clones.TryGetValue(m,out Material copy)){copy=new Material(m){name=m.name+" Gece Runtime"};copy.EnableKeyword("_EMISSION");copy.SetTexture("_EmissionMap",Texture2D.whiteTexture);clones[m]=copy;}
                updated[i]=copy;changed=true;
            }
            if(changed){replacements.Add(new Replacement{renderer=r,original=original});r.sharedMaterials=updated;}
        }
    }

    void LateUpdate()
    {
        if(runtimeRoot==null)return;
        float night=NightAmount;
        foreach(var pair in clones)
        {
            if(pair.Key==null||pair.Value==null)continue;
            pair.Value.SetColor("_BaseColor",Color.Lerp(pair.Key.GetColor("_BaseColor"),new Color(1,.77f,.39f),night*.9f));
            pair.Value.SetColor("_EmissionColor",new Color(1,.61f,.22f)*night*1.5f);
        }
        poolMaterial.SetColor("_BaseColor",new Color(1,.69f,.29f,night*.30f));
        beamMaterial.SetColor("_BaseColor",new Color(.9f,.84f,.57f,night*.12f));
        reflectionMaterial.SetColor("_BaseColor",new Color(.9f,.84f,.57f,night*.22f));
        Transform actors=transform.Find("SehirYasami");
        if(actors!=null&&carsGeneration!=actors.GetEntityId().GetHashCode())
        {
            carsGeneration=actors.GetEntityId().GetHashCode();cars.Clear();foreach(Transform t in actors)if(t.name.StartsWith("AdaAraci"))cars.Add(t);
            ReplaceEmissiveSurfaces();
        }
        if(night>.01f)
        {
            int count=Mathf.Min(lamps.Count,pools.Length);
            for(int i=0;i<count;i++)pools[i]=Matrix4x4.TRS(lamps[i].position+Vector3.up*.045f,Quaternion.Euler(0,0,0),new Vector3(8,1,8));
            Submit(fanMesh,poolMaterial,pools,count);
            int n=0;foreach(Transform car in cars)
            {
                if(car==null)continue;
                for(int side=-1;side<=1;side+=2)
                {
                    Transform socket=car.Find(side<0?"FarSol":"FarSag");
                    Vector3 p=socket!=null?socket.position:car.TransformPoint(new Vector3(side*.54f,.12f,1.6f));p.y=city!=null?city.landBounds.max.y+.40f:p.y;
                    headlights[n++]=Matrix4x4.TRS(p,Quaternion.Euler(0,car.eulerAngles.y,0),new Vector3(3,1,11));
                }
            }
            Submit(wedgeMesh,poolMaterial,headlights,n);
        }
        UpdateRealLights(night);
        if(lighthouseHead!=null)
        {
            lighthouseHead.Rotate(0,Time.deltaTime*26,0,Space.Self);
            beam.gameObject.SetActive(night>.01f);reflection.gameObject.SetActive(night>.01f);
            lighthouseLight.enabled=night>.01f;lighthouseLight.intensity=night*8;
            Vector3 p=lighthouseHead.position;float seaY=seaSurfaceY;
            // A broad, soft surface streak travels with the beam across the surrounding sea.
            Vector3 direction=lighthouseHead.forward;direction.y=0;direction.Normalize();
            reflection.position=new Vector3(p.x,seaY,p.z)+direction*12;
            reflection.rotation=Quaternion.LookRotation(direction,Vector3.up);reflection.localScale=new Vector3(14,1,105);
        }
    }

    void UpdateRealLights(float night)
    {
        Camera camera=Camera.main;Vector3 focus=camera!=null?camera.transform.position+camera.transform.forward*170:transform.position;
        for(int i=0;i<localLights.Count;i++)
        {
            List<Transform> candidates=i<4?lamps:cars;int slot=i%4;
            Transform chosen=null;float best=float.PositiveInfinity;
            foreach(Transform t in candidates)
            {
                if(t==null)continue;bool used=false;
                for(int j=i<4?0:4;j<i;j++)if((localLights[j].transform.position-(t.position+Vector3.up*(i<4?3.5f:.6f))).sqrMagnitude<.1f)used=true;
                if(used)continue;
                float d=(t.position-focus).sqrMagnitude;if(d<best){chosen=t;best=d;}
            }
            Light light=localLights[i];light.enabled=night>.01f&&chosen!=null;
            if(chosen==null)continue;
            light.transform.position=chosen.position+Vector3.up*(i<4?3.5f:.6f);
            if(i>=4)light.transform.rotation=chosen.rotation*Quaternion.Euler(10,0,0);
            light.intensity=night*(i<4?2.1f:3.0f);
        }
    }

    static void Submit(Mesh mesh,Material material,Matrix4x4[] matrices,int count)
    {
        if(count==0)return;
        if(SystemInfo.supportsInstancing){var rp=new RenderParams(material){shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false};Graphics.RenderMeshInstanced(in rp,mesh,0,matrices,count);}
        else for(int i=0;i<count;i++)Graphics.DrawMesh(mesh,matrices[i],material,0);
    }
    static Mesh CreateFan()
    {
        const int sides=24;var v=new Vector3[sides+2];var uv=new Vector2[v.Length];var t=new int[sides*3];
        uv[0]=new Vector2(.5f,0);
        for(int i=0;i<=sides;i++){float a=i/(float)sides*Mathf.PI*2;v[i+1]=new Vector3(Mathf.Sin(a)*.5f,0,Mathf.Cos(a)*.5f);uv[i+1]=new Vector2(.5f+Mathf.Sin(a)*.5f,1);if(i<sides){t[i*3]=0;t[i*3+1]=i+2;t[i*3+2]=i+1;}}
        var m=new Mesh{name="Ada Yumusk Isik Havuzu",vertices=v,uv=uv,triangles=t};m.RecalculateBounds();return m;
    }
    static Mesh CreateCone()
    {
        const int sides=16;var v=new Vector3[sides+2];var uv=new Vector2[v.Length];var t=new int[sides*3];uv[0]=new Vector2(.5f,0);
        for(int i=0;i<=sides;i++){float a=i/(float)sides*Mathf.PI*2;v[i+1]=new Vector3(Mathf.Sin(a),Mathf.Cos(a),1);uv[i+1]=new Vector2(i/(float)sides,1);if(i<sides){t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=i+2;}}
        var m=new Mesh{name="Ada Hacimsel Isik Konisi",vertices=v,uv=uv,triangles=t};m.RecalculateBounds();return m;
    }
    static Mesh CreateWedge()
    {
        var m=new Mesh{name="Ada Far Isik Yelpazesi",vertices=new[]{Vector3.zero,new Vector3(-.5f,0,1),new Vector3(.5f,0,1)},uv=new[]{new Vector2(.5f,0),new Vector2(0,1),new Vector2(1,1)},triangles=new[]{0,1,2}};m.RecalculateBounds();return m;
    }
    void OnDisable()
    {
        foreach(Material m in clones.Values)if(m!=null)m.SetColor("_EmissionColor",Color.black);
        foreach(Light light in localLights)if(light!=null)light.enabled=false;
        if(lighthouseLight!=null)lighthouseLight.enabled=false;
        if(beam!=null)beam.gameObject.SetActive(false);if(reflection!=null)reflection.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        foreach(var r in replacements)if(r.renderer!=null)r.renderer.sharedMaterials=r.original;
        foreach(Material m in clones.Values)if(m!=null)Destroy(m);
        if(poolMaterial!=null)Destroy(poolMaterial);if(beamMaterial!=null)Destroy(beamMaterial);if(reflectionMaterial!=null)Destroy(reflectionMaterial);
        if(fanMesh!=null)Destroy(fanMesh);if(coneMesh!=null)Destroy(coneMesh);if(wedgeMesh!=null)Destroy(wedgeMesh);
        if(runtimeRoot!=null)Destroy(runtimeRoot);
    }
}
