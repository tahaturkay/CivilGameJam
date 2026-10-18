using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Small, shared-palette low-poly citizens, street animals and traffic models.</summary>
public static class IslandActorArt
{
    static readonly Dictionary<Color32,Material> materials=new Dictionary<Color32,Material>();
    static readonly List<Object> owned=new List<Object>();
    static readonly Dictionary<int,Model> animalModels=new Dictionary<int,Model>();
    static readonly Dictionary<int,Model> vehicleModels=new Dictionary<int,Model>();
    static Mesh cube,wheel;
    sealed class Model { public Mesh mesh; public Material[] materials; }
    static readonly Color32 Rubber=new Color32(49,55,56,255), Chrome=new Color32(177,187,183,255);
    static readonly Color32 Window=new Color32(78,126,139,255), Lamp=new Color32(244,218,151,255);
    static readonly Color32[] Paint={new Color32(161,67,49,255),new Color32(65,110,126,255),new Color32(216,201,165,255),
        new Color32(108,136,109,255),new Color32(185,151,77,255),new Color32(102,112,138,255)};

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        Application.quitting-=ClearRuntimeCache;
        ClearRuntimeCache();
        Application.quitting+=ClearRuntimeCache;
    }

    /// <summary>Call after retiring the old city actors; generated meshes and materials are owned here.</summary>
    public static void ClearRuntimeCache()
    {
        foreach(Object resource in owned)
        {
            if(resource==null) continue;
            if(Application.isPlaying) Object.Destroy(resource); else Object.DestroyImmediate(resource);
        }
        owned.Clear(); materials.Clear(); animalModels.Clear(); vehicleModels.Clear(); cube=wheel=null;
    }
    static T Keep<T>(T item) where T:Object
    { item.hideFlags=HideFlags.HideAndDontSave; owned.Add(item);return item; }
    static Material Material(Color32 color)
    {
        if(materials.TryGetValue(color,out Material result)&&result!=null) return result;
        Material template=Resources.Load<Material>("IslandEffects/CitizenInstancing");
        Shader shader=template!=null?template.shader:Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) shader=Shader.Find("Standard");
        result=Keep(new Material(shader){name="Ada Aktor / "+ColorUtility.ToHtmlStringRGB(color),color=color,enableInstancing=true});
        if(result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness",.16f);
        materials[color]=result;return result;
    }
    static Mesh CubeMesh()
    {
        if(cube!=null)return cube;
        cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if(cube!=null)return cube;
        // Readable fallback, preserving the 24-vertex cube convention used by the city instancer.
        var vertices=new List<Vector3>(24);var indices=new List<int>(36);
        Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        foreach(Vector3 normal in normals)
        {
            Vector3 right=Vector3.Cross(Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up,normal)*.5f;
            Vector3 up=Vector3.Cross(normal,right); int first=vertices.Count;Vector3 center=normal*.5f;
            vertices.Add(center-right-up);vertices.Add(center+right-up);vertices.Add(center+right+up);vertices.Add(center-right+up);
            indices.Add(first);indices.Add(first+1);indices.Add(first+2);indices.Add(first);indices.Add(first+2);indices.Add(first+3);
        }
        cube=Keep(new Mesh{name="Ada Aktor Ortak Kup"});cube.SetVertices(vertices);cube.SetTriangles(indices,0);cube.RecalculateNormals();cube.RecalculateBounds();return cube;
    }
    static GameObject Root(Transform parent,string name)
    {var root=new GameObject(name);root.transform.SetParent(parent,false);return root;}
    static Transform Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material,Mesh mesh=null)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.transform.localPosition=position;go.transform.localScale=scale;
        go.GetComponent<MeshFilter>().sharedMesh=mesh!=null?mesh:CubeMesh();go.GetComponent<MeshRenderer>().sharedMaterial=material;
        return go.transform;
    }
    static Transform Part(Transform parent,string name,Vector3 position,Vector3 scale,Color32 color,Mesh mesh=null)
    { return Part(parent,name,position,scale,Material(color),mesh); }

    /// <summary>Keep the original limb pivots and cube mesh; at most four extra cube parts (15 total).</summary>
    public static void EnhanceCitizen(GameObject root,int variant)
    {
        if(root==null||root.transform.Find("Bas/GozSol")!=null)return;
        Transform body=root.transform.Find("Govde"),head=root.transform.Find("Bas"),hair=root.transform.Find("Sac");
        if(body==null||head==null||hair==null)return;
        int choice=Mathf.Abs(variant)%6;
        Material dark=hair.GetComponent<MeshRenderer>().sharedMaterial;
        Material top=body.GetComponent<MeshRenderer>().sharedMaterial;
        Material skin=head.GetComponent<MeshRenderer>().sharedMaterial;
        Mesh shared=head.GetComponent<MeshFilter>().sharedMesh;
        // The walking rig faces +Z. Facial features share the hair material rather than adding a palette per person.
        for(int side=-1;side<=1;side+=2)
        {
            Transform eye=Part(root.transform,side<0?"GozSol":"GozSag",new Vector3(side*.061f,1.19f,.133f),new Vector3(.028f,.031f,.012f),dark,shared);
            eye.SetParent(head,true);
        }
        hair.localPosition+=new Vector3(choice==2?.025f:0,choice==1?.028f:0,choice==3?-.025f:0);
        hair.localScale=new Vector3(choice==2?.33f:.29f,choice==1?.14f:choice==4?.065f:.09f,choice==3?.33f:.28f);
        Part(root.transform,"Yaka",new Vector3(0,.997f,.143f),new Vector3(.18f,.065f,.025f),choice%2==0?skin:dark,shared);
        if(choice==1||choice==4)
            Part(root.transform,"SapkaSiperi",new Vector3(0,1.323f,.115f),new Vector3(.31f,.035f,.18f),top,shared);
        else if(choice==3)
            Part(root.transform,"OmuzCantasi",new Vector3(.225f,.72f,-.035f),new Vector3(.10f,.29f,.22f),dark,shared);
        else
            Part(root.transform,"CeketCebi",new Vector3(.11f,.825f,.141f),new Vector3(.09f,.11f,.024f),choice==5?skin:dark,shared);
        float width=choice==0?1.06f:choice==3?.94f:1f;
        body.localScale=Vector3.Scale(body.localScale,new Vector3(width,1,1));
    }

    /// <summary>Foot-origin animal, forward +Z. Three materials and a shared combined mesh per species/coat.</summary>
    public static GameObject CreateAnimal(Transform parent,bool isCat,int variant)
    {
        int coat=Mathf.Abs(variant)%3,key=(isCat?0:3)+coat;
        var root=Root(parent,isCat?"AdaKedisi":"AdaKopegi");
        if(!animalModels.TryGetValue(key,out Model model))
        {
            Color32[] coats={new Color32(175,132,86,255),new Color32(213,205,183,255),new Color32(101,109,111,255)};
            Color32 fur=coats[coat]; float size=isCat?1f:1.35f;
            Part(root.transform,"Govde",new Vector3(0,.35f,.0f)*size,new Vector3(.28f,.29f,.53f)*size,fur);
            Part(root.transform,"Gogus",new Vector3(0,.38f,.205f)*size,new Vector3(.23f,.22f,.17f)*size,Chrome);
            Part(root.transform,"Bas",new Vector3(0,.53f,.29f)*size,new Vector3(.27f,.25f,.24f)*size,fur);
            Part(root.transform,"Burun",new Vector3(0,.48f,.439f)*size,new Vector3(.12f,.10f,isCat?.06f:.16f)*size,Rubber);
            for(int side=-1;side<=1;side+=2)
            {
                Transform ear=Part(root.transform,"Kulak",new Vector3(side*.105f,isCat?.70f:.62f,.26f)*size,new Vector3(.10f,isCat?.17f:.21f,.10f)*size,fur);
                ear.localRotation=Quaternion.Euler(isCat?0:20,0,side*(isCat?-12:30));
                Part(root.transform,"Goz",new Vector3(side*.088f,.56f,.418f)*size,new Vector3(.035f,.04f,.015f)*size,Rubber);
                for(int end=-1;end<=1;end+=2)
                    Part(root.transform,"Pati",new Vector3(side*.105f,.13f,end*.19f)*size,new Vector3(.075f,.26f,.105f)*size,fur);
            }
            Transform tail=Part(root.transform,"Kuyruk",new Vector3(0,.47f,-.38f)*size,new Vector3(.065f,isCat?.38f:.30f,.07f)*size,fur);
            tail.localRotation=Quaternion.Euler(isCat?-20:-45,0,0);
            model=Combine(root.transform,"Ada_"+(isCat?"Kedi":"Kopek")+coat);animalModels[key]=model;
        }
        else Apply(root,model);
        return root;
    }

    /// <summary>Six body types, origin at ground and +Z front. FarSol/FarSag mark headlight beam origins.</summary>
    public static GameObject CreateVehicle(Transform parent,int variant)
    {
        int type=Mathf.Abs(variant)%6;
        var root=Root(parent,new[]{"SehirOtomobili","Sedan","SporAraba","Kamyon","Minibus","Taksi"}[type]);
        float length=type==3?4.9f:type==4?4.2f:3.45f,front=length*.5f;
        if(!vehicleModels.TryGetValue(type,out Model model))
        {
            Color32 paint=Paint[type];float width=type==3?1.84f:1.62f;
            Part(root.transform,"Sasi",new Vector3(0,.42f,0),new Vector3(width,.26f,length),Rubber);
            Part(root.transform,"Kaporta",new Vector3(0,.69f,0),new Vector3(width,.48f,length-.12f),paint);
            bool truck=type==3,van=type==4,sport=type==2;
            float cabinZ=truck?1.25f:van?-.12f:-.20f,cabinLength=truck?1.65f:van?3.1f:1.7f;
            float cabinY=truck?1.37f:van?1.38f:sport?1.05f:1.20f,cabinH=truck?.88f:van?.98f:sport?.48f:.73f;
            Part(root.transform,"CamliKabin",new Vector3(0,cabinY,cabinZ),new Vector3(width-.18f,cabinH,cabinLength),Window);
            Part(root.transform,"Tavan",new Vector3(0,cabinY+cabinH*.5f+.035f,cabinZ-.07f),new Vector3(width-.11f,.15f,cabinLength-.12f),paint);
            for(int side=-1;side<=1;side+=2)
            {
                for(int end=-1;end<=1;end+=2)
                    Part(root.transform,"KabinDiregi",new Vector3(side*(width*.5f-.1f),cabinY,cabinZ+end*(cabinLength*.5f-.07f)),new Vector3(.11f,cabinH+.12f,.12f),paint);
                Part(root.transform,"KapiDiregi",new Vector3(side*(width*.5f-.085f),cabinY,cabinZ),new Vector3(.10f,cabinH+.1f,.13f),paint);
                Part(root.transform,"KapiKolu",new Vector3(side*(width*.5f+.012f),.94f,cabinZ-.18f),new Vector3(.03f,.045f,.18f),Chrome);
                Part(root.transform,"DikizAynasi",new Vector3(side*(width*.5f+.12f),1.05f,cabinZ+cabinLength*.43f),new Vector3(.20f,.15f,.20f),Rubber);
                Part(root.transform,"OnFar",new Vector3(side*width*.32f,.76f,front-.025f),new Vector3(.34f,.19f,.05f),Lamp);
                Part(root.transform,"Stop",new Vector3(side*width*.32f,.76f,-front+.025f),new Vector3(.28f,.16f,.05f),new Color32(154,48,35,255));
                for(int end=-1;end<=1;end+=2)
                {
                    Vector3 position=new Vector3(side*width*.49f,.35f,end*(length*.5f-.68f));
                    Part(root.transform,"Lastik",position,new Vector3(.25f,.37f,.37f),Rubber,WheelMesh());
                    Part(root.transform,"Jant",position+Vector3.right*side*.14f,new Vector3(.035f,.18f,.18f),Chrome,WheelMesh());
                }
            }
            Part(root.transform,"OnTampon",new Vector3(0,.49f,front),new Vector3(width,.13f,.12f),Chrome);
            Part(root.transform,"ArkaTampon",new Vector3(0,.49f,-front),new Vector3(width,.13f,.12f),Chrome);
            Part(root.transform,"OnIzgara",new Vector3(0,.70f,front+.006f),new Vector3(.49f,.22f,.045f),Rubber);
            if(truck)
            {
                Part(root.transform,"YukKasasi",new Vector3(0,1.45f,-.90f),new Vector3(1.82f,1.30f,2.65f),Chrome);
                Part(root.transform,"KasaKapak",new Vector3(0,1.45f,-front+.08f),new Vector3(1.60f,1.12f,.055f),Paint[2]);
                for(int seam=-1;seam<=1;seam++) Part(root.transform,"KasaFitili",new Vector3(seam*.55f,1.45f,-front+.04f),new Vector3(.045f,1.20f,.04f),Rubber);
            }
            if(sport)
            {
                Part(root.transform,"SporKaput",new Vector3(0,.93f,.95f),new Vector3(1.36f,.12f,1.15f),paint);
                for(int side=-1;side<=1;side+=2)Part(root.transform,"SpoilerAyak",new Vector3(side*.49f,1.02f,-1.42f),new Vector3(.09f,.26f,.12f),Rubber);
                Part(root.transform,"Spoiler",new Vector3(0,1.16f,-1.43f),new Vector3(1.65f,.09f,.30f),paint);
            }
            if(type==5)
            {
                Part(root.transform,"TaksiTabelasi",new Vector3(0,1.71f,-.2f),new Vector3(.55f,.20f,.28f),Lamp);
                Part(root.transform,"TaksiIsareti",new Vector3(0,1.71f,-.045f),new Vector3(.34f,.075f,.025f),Rubber);
            }
            model=Combine(root.transform,"Ada_Arac_"+type);vehicleModels[type]=model;
        }
        else Apply(root,model);
        for(int side=-1;side<=1;side+=2)
        {
            Transform origin=Root(root.transform,side<0?"FarSol":"FarSag").transform;
            origin.localPosition=new Vector3(side*(type==3?.59f:.52f),.76f,front+.08f);
        }
        return root;
    }

    /// <summary>
    /// Shared low-poly rescue engine: ground origin, +Z front, 1.90 m body / 2.10 m mirrors,
    /// 5.14 m bumper-to-bumper, 2.47 m roof beacon height. Attachment points survive mesh combining.
    /// </summary>
    public static GameObject CreateFireEngine(Transform parent)
    {
        const int fireEngineKey=100; // Ordinary traffic variants retain their existing keys 0..5.
        var root=Root(parent,"ItfaiyeAraci");
        if(!vehicleModels.TryGetValue(fireEngineKey,out Model model))
        {
            Color32 red=new Color32(196,48,39,255),ivory=new Color32(234,232,215,255);
            Transform frame=root.transform;
            Part(frame,"TasiyiciSasi",new Vector3(0,.53f,0),new Vector3(1.76f,.25f,4.90f),Rubber);
            Part(frame,"KabinAltGovde",new Vector3(0,.99f,1.46f),new Vector3(1.90f,.78f,1.86f),red);
            Part(frame,"MurettebatKabini",new Vector3(0,1.69f,1.43f),new Vector3(1.80f,.78f,1.66f),Window);
            Part(frame,"BeyazKabinTavani",new Vector3(0,2.12f,1.37f),new Vector3(1.91f,.15f,1.82f),ivory);
            Part(frame,"OnCamOrtasi",new Vector3(0,1.70f,2.274f),new Vector3(.075f,.77f,.055f),red);
            Part(frame,"OnCamGunesligi",new Vector3(0,2.055f,2.285f),new Vector3(1.88f,.09f,.13f),red);
            Part(frame,"MotorIzgarasi",new Vector3(0,.98f,2.421f),new Vector3(.88f,.29f,.045f),Rubber);
            for(int slat=0;slat<3;slat++)
                Part(frame,"IzgaraCizgisi",new Vector3(0,.89f+slat*.085f,2.448f),new Vector3(.77f,.022f,.025f),Chrome);
            Part(frame,"OnTampon",new Vector3(0,.61f,2.50f),new Vector3(1.98f,.19f,.14f),Chrome);
            Part(frame,"ArkaTampon",new Vector3(0,.61f,-2.50f),new Vector3(1.98f,.19f,.14f),Chrome);
            Part(frame,"OnYansiticiSerit",new Vector3(0,1.28f,2.414f),new Vector3(1.88f,.12f,.035f),ivory);

            // Box-shaped tool lockers, inset aluminium shutters and two continuous reflective stripes.
            Part(frame,"EkipmanGovdesi",new Vector3(0,1.34f,-.98f),new Vector3(1.90f,1.39f,2.92f),red);
            Part(frame,"EkipmanTavanKenari",new Vector3(0,2.07f,-.98f),new Vector3(1.96f,.12f,2.98f),Chrome);
            for(int side=-1;side<=1;side+=2)
            {
                for(int end=-1;end<=1;end+=2)
                    Part(frame,"KabinKoseleri",new Vector3(side*.855f,1.69f,1.43f+end*.79f),new Vector3(.10f,.83f,.105f),red);
                Part(frame,"KabinKapiDiregi",new Vector3(side*.91f,1.66f,1.26f),new Vector3(.065f,.80f,.085f),red);
                Part(frame,"KabinKolu",new Vector3(side*.966f,1.25f,1.07f),new Vector3(.033f,.045f,.20f),Chrome);
                Part(frame,"DikizAynasiKolu",new Vector3(side*.958f,1.63f,2.05f),new Vector3(.13f,.045f,.055f),Chrome);
                Part(frame,"DikizAynasi",new Vector3(side*1.015f,1.64f,2.05f),new Vector3(.07f,.23f,.16f),Rubber);
                Part(frame,"KabinBeyazSerit",new Vector3(side*.957f,1.27f,1.44f),new Vector3(.025f,.13f,1.82f),ivory);
                Part(frame,"Basamak",new Vector3(side*.86f,.59f,.67f),new Vector3(.28f,.10f,.64f),Chrome);
                for(int locker=0;locker<3;locker++)
                {
                    float z=-1.99f+locker*.98f;
                    Part(frame,"EkipmanDolabi",new Vector3(side*.958f,1.40f,z),new Vector3(.035f,.90f,.85f),Chrome);
                    for(int seam=0;seam<4;seam++)
                        Part(frame,"DolapPanjuru",new Vector3(side*.979f,1.12f+seam*.19f,z),new Vector3(.016f,.018f,.81f),Rubber);
                    Part(frame,"DolapKilidi",new Vector3(side*.986f,1.03f,z),new Vector3(.02f,.04f,.20f),Rubber);
                }
                Part(frame,"ArkaYansiticiSerit",new Vector3(side*.975f,.83f,-.98f),new Vector3(.035f,.12f,2.88f),ivory);
                Part(frame,"OnFar",new Vector3(side*.70f,1.02f,2.431f),new Vector3(.31f,.20f,.055f),Lamp);
                Part(frame,"ArkaStop",new Vector3(side*.73f,1.07f,-2.458f),new Vector3(.24f,.28f,.05f),red);
                Part(frame,"ArkaGeriVitesLambasi",new Vector3(side*.73f,.91f,-2.461f),new Vector3(.24f,.07f,.055f),Lamp);
                for(int end=-1;end<=1;end+=2)
                {
                    Vector3 axle=new Vector3(side*.89f,.42f,end>0?1.54f:-1.59f);
                    Part(frame,"Lastik",axle,new Vector3(.28f,.42f,.42f),Rubber,WheelMesh());
                    Part(frame,"CelikJant",axle+Vector3.right*side*.151f,new Vector3(.035f,.23f,.23f),Chrome,WheelMesh());
                    Part(frame,"JantGobegi",axle+Vector3.right*side*.172f,new Vector3(.025f,.095f,.095f),Rubber,WheelMesh());
                    Part(frame,"CamurlukUstu",new Vector3(side*.94f,.91f,axle.z),new Vector3(.12f,.10f,1.04f),red);
                }
                Part(frame,"MerdivenRayi",new Vector3(side*.30f,2.22f,-.65f),new Vector3(.07f,.08f,3.30f),Chrome);
                for(int support=0;support<2;support++)
                    Part(frame,"MerdivenSabitleme",new Vector3(side*.30f,2.14f,-1.65f+support*2.05f),new Vector3(.11f,.18f,.13f),Rubber);
                Part(frame,"AlarmTabani",new Vector3(side*.61f,2.255f,1.54f),new Vector3(.30f,.09f,.30f),Rubber);
                Transform beacon=Part(frame,"KirmiziTepeLambasi",new Vector3(side*.61f,2.375f,1.54f),new Vector3(.15f,.12f,.12f),red,WheelMesh());
                beacon.localRotation=Quaternion.Euler(0,0,90);
                Part(frame,"TepeLambasiPariltisi",new Vector3(side*.61f,2.455f,1.54f),new Vector3(.14f,.025f,.14f),Lamp);
            }
            for(int rung=0;rung<10;rung++)
                Part(frame,"MerdivenBasamagi",new Vector3(0,2.22f,-2.15f+rung*.33f),new Vector3(.60f,.045f,.045f),Chrome);
            // Rear pump panel and folded hose give the silhouette a rescue role without textures or text meshes.
            Part(frame,"ArkaPompaPaneli",new Vector3(0,1.35f,-2.456f),new Vector3(.91f,.91f,.045f),Rubber);
            Part(frame,"PompaKaplini",new Vector3(-.24f,1.18f,-2.49f),new Vector3(.17f,.19f,.07f),Chrome);
            Part(frame,"PompaGostergesi",new Vector3(.21f,1.59f,-2.49f),new Vector3(.20f,.20f,.04f),ivory);
            for(int fold=0;fold<3;fold++)
                Part(frame,"KatliHortum",new Vector3(.20f,1.21f+fold*.10f,-2.49f),new Vector3(.43f,.065f,.07f),Lamp);
            model=Combine(frame,"Ada_ItfaiyeAraci");vehicleModels[fireEngineKey]=model;
        }
        else Apply(root,model);
        for(int side=-1;side<=1;side+=2)
        {
            Transform headlight=Root(root.transform,side<0?"FarSol":"FarSag").transform;
            headlight.localPosition=new Vector3(side*.70f,1.02f,2.48f);
            Transform alarm=Root(root.transform,side<0?"AlarmSol":"AlarmSag").transform;
            alarm.localPosition=new Vector3(side*.61f,2.46f,1.54f);
        }
        return root;
    }

    /// <summary>
    /// Three shared-mesh AKUT rescue van variants. Ground origin, +Z front, 2.10 m mirrors,
    /// 5.14 m bumper-to-bumper. FarSol/FarSag and AlarmSol/AlarmSag use the fire-engine contract.
    /// Each variant is combined once and then reuses the same mesh and palette for its fleet.
    /// </summary>
    public static GameObject CreateAkutVehicle(Transform parent,int variant=0)
    {
        int type=Mathf.Abs(variant%3),key=110+type;
        var root=Root(parent,"AKUT_Araci_"+(type+1));
        if(!vehicleModels.TryGetValue(key,out Model model))
        {
            Transform frame=root.transform;
            Color32 ivory=new Color32(234,232,215,255),red=new Color32(196,48,39,255);
            Part(frame,"TasiyiciSasi",new Vector3(0,.53f,0),new Vector3(1.76f,.25f,4.90f),Rubber);
            Part(frame,"KabinAltGovde",new Vector3(0,.99f,1.43f),new Vector3(1.90f,.78f,1.90f),ivory);
            Part(frame,"MurettebatKabini",new Vector3(0,1.69f,1.43f),new Vector3(1.80f,.78f,1.66f),Window);
            Part(frame,"KabinTavani",new Vector3(0,2.12f,1.37f),new Vector3(1.91f,.15f,1.82f),ivory);
            Part(frame,"OnCamOrtasi",new Vector3(0,1.70f,2.274f),new Vector3(.075f,.77f,.055f),ivory);
            Part(frame,"OnCamGunesligi",new Vector3(0,2.055f,2.285f),new Vector3(1.88f,.09f,.13f),red);
            Part(frame,"MotorIzgarasi",new Vector3(0,.98f,2.421f),new Vector3(.88f,.29f,.045f),Rubber);
            Part(frame,"OnTampon",new Vector3(0,.61f,2.50f),new Vector3(1.98f,.19f,.14f),Chrome);
            Part(frame,"ArkaTampon",new Vector3(0,.61f,-2.50f),new Vector3(1.98f,.19f,.14f),Chrome);
            Part(frame,"OnKirmiziSerit",new Vector3(0,1.28f,2.414f),new Vector3(1.88f,.12f,.035f),red);
            Part(frame,"KurtarmaBolmesi",new Vector3(0,1.39f,-.98f),new Vector3(1.90f,1.48f,2.92f),ivory);
            Part(frame,"EkipmanTavanKenari",new Vector3(0,2.15f,-.98f),new Vector3(1.96f,.10f,2.98f),Chrome);
            for(int side=-1;side<=1;side+=2)
            {
                for(int end=-1;end<=1;end+=2)
                    Part(frame,"KabinKoseleri",new Vector3(side*.855f,1.69f,1.43f+end*.79f),new Vector3(.10f,.83f,.105f),ivory);
                Part(frame,"KabinKapiDiregi",new Vector3(side*.91f,1.66f,1.26f),new Vector3(.065f,.80f,.085f),ivory);
                Part(frame,"KabinKolu",new Vector3(side*.966f,1.25f,1.07f),new Vector3(.033f,.045f,.20f),Rubber);
                Part(frame,"DikizAynasiKolu",new Vector3(side*.958f,1.63f,2.05f),new Vector3(.13f,.045f,.055f),Chrome);
                Part(frame,"DikizAynasi",new Vector3(side*1.015f,1.64f,2.05f),new Vector3(.07f,.23f,.16f),Rubber);
                Part(frame,"KabinKurtarmaSeridi",new Vector3(side*.957f,1.27f,1.44f),new Vector3(.025f,.13f,1.82f),red);
                Part(frame,"Basamak",new Vector3(side*.86f,.59f,.67f),new Vector3(.28f,.10f,.64f),Chrome);
                Part(frame,"KurtarmaSeridi",new Vector3(side*.968f,1.03f,-.98f),new Vector3(.035f,.24f,2.88f),red);
                Part(frame,"AltYansitici",new Vector3(side*.975f,.83f,-.98f),new Vector3(.035f,.08f,2.88f),Lamp);
                Part(frame,"OnFar",new Vector3(side*.70f,1.02f,2.431f),new Vector3(.31f,.20f,.055f),Lamp);
                Part(frame,"ArkaStop",new Vector3(side*.73f,1.07f,-2.458f),new Vector3(.24f,.28f,.05f),red);
                Part(frame,"ArkaGeriVitesLambasi",new Vector3(side*.73f,.91f,-2.461f),new Vector3(.24f,.07f,.055f),Lamp);
                for(int end=-1;end<=1;end+=2)
                {
                    Vector3 axle=new Vector3(side*.89f,.42f,end>0?1.54f:-1.59f);
                    Part(frame,"Lastik",axle,new Vector3(.28f,.42f,.42f),Rubber,WheelMesh());
                    Part(frame,"CelikJant",axle+Vector3.right*side*.151f,new Vector3(.035f,.23f,.23f),Chrome,WheelMesh());
                    Part(frame,"CamurlukUstu",new Vector3(side*.94f,.91f,axle.z),new Vector3(.12f,.10f,1.04f),Rubber);
                }
                // Geometric AKUT lettering stays crisp without a font atlas or a renderer per letter.
                Transform badge=Root(frame,"AKUT_YanKimlik").transform;
                badge.localPosition=new Vector3(side*.973f,1.63f,-.96f);
                badge.localRotation=Quaternion.Euler(0,side*90,0);
                AddAkutLettering(badge,red);
                Part(frame,"AlarmTabani",new Vector3(side*.61f,2.255f,1.54f),new Vector3(.30f,.09f,.30f),Rubber);
                Transform beacon=Part(frame,"KirmiziTepeLambasi",new Vector3(side*.61f,2.375f,1.54f),new Vector3(.15f,.12f,.12f),red,WheelMesh());
                beacon.localRotation=Quaternion.Euler(0,0,90);
                Part(frame,"TepeLambasiPariltisi",new Vector3(side*.61f,2.455f,1.54f),new Vector3(.14f,.025f,.14f),Lamp);
                Part(frame,"PortbagajRayi",new Vector3(side*.78f,2.27f,-1.06f),new Vector3(.06f,.11f,2.58f),Rubber);
            }
            for(int rail=0;rail<3;rail++)
                Part(frame,"PortbagajTravarsi",new Vector3(0,2.23f,-2.14f+rail*1.08f),new Vector3(1.62f,.065f,.07f),Chrome);
            Part(frame,"ArkaKapiFitili",new Vector3(0,1.43f,-2.462f),new Vector3(.035f,1.32f,.025f),Rubber);
            for(int side=-1;side<=1;side+=2)
            {
                Part(frame,"ArkaKapiCami",new Vector3(side*.43f,1.78f,-2.466f),new Vector3(.62f,.40f,.035f),Window);
                Part(frame,"ArkaKapiKolu",new Vector3(side*.12f,1.32f,-2.485f),new Vector3(.10f,.035f,.04f),Rubber);
            }
            if(type==0)
            {
                Part(frame,"KatliSedye",new Vector3(0,2.34f,-.98f),new Vector3(.72f,.12f,2.04f),Lamp);
                for(int side=-1;side<=1;side+=2)
                    Part(frame,"SedyeTutamagi",new Vector3(side*.40f,2.34f,-.98f),new Vector3(.045f,.065f,2.25f),Chrome);
            }
            else if(type==1)
            {
                for(int item=0;item<2;item++)
                {
                    Part(frame,"KurtarmaCantasi",new Vector3(-.36f+item*.72f,2.44f,-1.18f),new Vector3(.61f,.34f,1.24f),item==0?red:Rubber);
                    Part(frame,"CantaKemeri",new Vector3(-.36f+item*.72f,2.625f,-1.18f),new Vector3(.63f,.035f,.14f),Chrome);
                }
            }
            else
            {
                Part(frame,"HalatKutusu",new Vector3(-.38f,2.40f,-1.32f),new Vector3(.65f,.24f,1.45f),Lamp);
                for(int rung=0;rung<7;rung++)
                    Part(frame,"KatliMerdivenBasamagi",new Vector3(.37f,2.35f,-2.03f+rung*.25f),new Vector3(.47f,.045f,.045f),Chrome);
                for(int side=-1;side<=1;side+=2)
                    Part(frame,"KatliMerdivenRayi",new Vector3(.37f+side*.25f,2.35f,-1.28f),new Vector3(.05f,.08f,1.95f),Chrome);
            }
            model=Combine(frame,"Ada_AKUT_Araci_"+type);vehicleModels[key]=model;
        }
        else Apply(root,model);
        for(int side=-1;side<=1;side+=2)
        {
            Transform headlight=Root(root.transform,side<0?"FarSol":"FarSag").transform;
            headlight.localPosition=new Vector3(side*.70f,1.02f,2.48f);
            Transform alarm=Root(root.transform,side<0?"AlarmSol":"AlarmSag").transform;
            alarm.localPosition=new Vector3(side*.61f,2.46f,1.54f);
        }
        return root;
    }

    static void AddAkutLettering(Transform parent,Color32 red)
    {
        // Strokes in the label XY plane; its normal is the vehicle's outside wall.
        Vector2[][] strokes={
            new[]{new Vector2(-.65f,-.13f),new Vector2(-.54f,.13f),new Vector2(-.54f,.13f),new Vector2(-.43f,-.13f),new Vector2(-.61f,-.035f),new Vector2(-.47f,-.035f)},
            new[]{new Vector2(-.28f,-.13f),new Vector2(-.28f,.13f),new Vector2(-.28f,0),new Vector2(-.09f,.13f),new Vector2(-.28f,0),new Vector2(-.09f,-.13f)},
            new[]{new Vector2(.06f,.13f),new Vector2(.06f,-.13f),new Vector2(.06f,-.13f),new Vector2(.25f,-.13f),new Vector2(.25f,-.13f),new Vector2(.25f,.13f)},
            new[]{new Vector2(.40f,.13f),new Vector2(.65f,.13f),new Vector2(.525f,.13f),new Vector2(.525f,-.13f)}
        };
        foreach(Vector2[] letter in strokes)
            for(int i=0;i<letter.Length;i+=2)
            {
                Vector2 a=letter[i],b=letter[i+1],delta=b-a,mid=(a+b)*.5f;
                Transform stroke=Part(parent,"AKUT_HarfCizgisi",new Vector3(mid.x,mid.y,.006f),new Vector3(.042f,delta.magnitude+.025f,.025f),red);
                stroke.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            }
    }

    static Mesh WheelMesh()
    {
        if(wheel!=null)return wheel;
        var v=new List<Vector3>(96);var t=new List<int>(96);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
            Vector3 p=new Vector3(-.5f,Mathf.Sin(a),Mathf.Cos(a)),q=new Vector3(-.5f,Mathf.Sin(b),Mathf.Cos(b));
            AddTriangle(v,t,p,p+Vector3.right,q+Vector3.right);AddTriangle(v,t,p,q+Vector3.right,q);
            AddTriangle(v,t,Vector3.left*.5f,p,q);AddTriangle(v,t,Vector3.right*.5f,q+Vector3.right,p+Vector3.right);
        }
        wheel=Keep(new Mesh{name="Ada_OrtakSekizgenLastik"});wheel.SetVertices(v);wheel.SetTriangles(t,0);wheel.RecalculateNormals();wheel.RecalculateBounds();return wheel;
    }
    static void AddTriangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c)
    { int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2); }

    static Model Combine(Transform root,string name)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        MeshRenderer[] renderers=root.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer renderer in renderers)
        {
            Material material=renderer.sharedMaterial;
            if(!groups.TryGetValue(material,out List<CombineInstance> entries)) groups[material]=entries=new List<CombineInstance>();
            entries.Add(new CombineInstance{mesh=renderer.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*renderer.localToWorldMatrix});
        }
        var submeshes=new List<CombineInstance>();var palette=new List<Material>();var temporary=new List<Mesh>();
        foreach(var entry in groups)
        {
            var mesh=new Mesh();mesh.CombineMeshes(entry.Value.ToArray(),true,true);temporary.Add(mesh);
            palette.Add(entry.Key);submeshes.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
        }
        var combined=Keep(new Mesh{name=name});combined.CombineMeshes(submeshes.ToArray(),false,false);combined.RecalculateBounds();
        foreach(Mesh mesh in temporary) { if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh); }
        foreach(MeshRenderer renderer in renderers)
        {
            renderer.enabled=false;
            // Construction pieces are no longer actor parts. Detach before deferred destruction so
            // same-frame city instancing discovery sees only the finished combined model.
            renderer.transform.SetParent(null,false);
            if(Application.isPlaying)Object.Destroy(renderer.gameObject);else Object.DestroyImmediate(renderer.gameObject);
        }
        var model=new Model{mesh=combined,materials=palette.ToArray()};Apply(root.gameObject,model);return model;
    }
    static void Apply(GameObject root,Model model)
    {
        root.AddComponent<MeshFilter>().sharedMesh=model.mesh;
        var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=model.materials;
        renderer.shadowCastingMode=ShadowCastingMode.On;
    }
}
