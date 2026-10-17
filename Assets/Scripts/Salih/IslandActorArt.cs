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
