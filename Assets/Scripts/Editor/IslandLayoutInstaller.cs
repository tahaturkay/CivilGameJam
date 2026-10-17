using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Deterministic low-poly city art. Scene installation and runtime wiring live in IslandSceneSetup.</summary>
public static partial class IslandLayoutInstaller
{
    const string MaterialFolder = "Assets/Materials/LowPolyAda";
    const string MeshFolder = "Assets/Models/LowPolyAda";
    const string Palm = "Assets/Assets/Salih/Palmov Island/Low Poly Houses Free Pack/Prefabs/";
    const string Civic = "Assets/Assets/Salih/Studio Horizon/Simple Building Generic Free/Prefabs/";
    const string Vehicles = "Assets/Assets/Salih/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/";
    const int CoastCount = 72;
    const float CoastX = 92, CoastZ = 75, GroundY = 2.4f;
    static readonly Color Grass = new Color32(114, 151, 86, 255), GrassDark = new Color32(76, 119, 70, 255);
    static readonly Color Sand = new Color32(220, 199, 145, 255), Road = new Color32(68, 77, 79, 255);
    static readonly Color Cream = new Color32(229, 221, 193, 255), Stone = new Color32(142, 150, 140, 255);
    static readonly Color Roof = new Color32(181, 74, 43, 255), RoofDark = new Color32(117, 62, 47, 255);
    static readonly Color Timber = new Color32(112, 79, 49, 255), Glass = new Color32(78, 126, 139, 255);
    static readonly Color Pine = new Color32(43, 90, 61, 255), Sage = new Color32(92, 131, 73, 255);
    static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
    static Mesh coneMesh, rockMesh, roofMesh, taperedMesh;

    [MenuItem("Oyun/Ada Haritası/Install Low Poly Island")]
    public static void Install() { IslandSceneSetup.Install(); }

    public static GameObject BuildIsland(Scene scene)
    {
        var island = new GameObject("LowPolyAda");
        SceneManager.MoveGameObjectToScene(island, scene);
        BuildTerrain(Group(island.transform, "Arazi"));
        Transform roads = Group(island.transform, "Yollar", Vector3.up * GroundY);
        BuildRoadNetwork(roads);
        BuildLevelBuildings(Group(island.transform, "YukseltilebilirYapilar", Vector3.up * GroundY));
        BuildStaticPlaces(Group(island.transform, "SabitAlanlar", Vector3.up * GroundY));
        BuildWall(Group(island.transform, "DenizSeti", Vector3.up * GroundY));
        BuildLighthouse(island.transform,new Vector3(-70,GroundY,46));
        foreach (BuildingVisuals b in island.GetComponentsInChildren<BuildingVisuals>(true)) b.gameObject.AddComponent<BuildingDamageVisuals>();
        return island;
    }

    static Transform Group(Transform parent, string name, Vector3 position = default(Vector3))
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        return go.transform;
    }

    static Material Mat(Color color)
    {
        if (Materials.TryGetValue(color, out Material material) && material != null) return material;
        EnsureFolder(MaterialFolder); Color32 c = color;
        string path = MaterialFolder + "/Ada_" + c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2") + ".mat";
        material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
        EditorUtility.SetDirty(material); Materials[color] = material; return material;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next;
        }
    }

    static Mesh PersistMesh(Mesh mesh)
    {
        // Combining primitives with different vertex layouts can leave unused native attributes undefined.
        // Rebuild only the attributes used by the island materials before saving stable asset data.
        Mesh source = mesh;
        mesh = new Mesh { name = source.name, indexFormat = source.indexFormat };
        mesh.vertices = source.vertices; mesh.normals = source.normals;
        if (source.uv.Length == source.vertexCount) mesh.uv = source.uv;
        mesh.subMeshCount = source.subMeshCount;
        for (int i = 0; i < source.subMeshCount; i++) mesh.SetTriangles(source.GetTriangles(i), i);
        mesh.bounds = source.bounds; Object.DestroyImmediate(source);
        EnsureFolder(MeshFolder); string path = MeshFolder + "/" + mesh.name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        // Mesh native/GPU buffers must be updated explicitly; CopySerialized leaves stale geometry in this editor.
        existing.Clear(false); existing.indexFormat = mesh.indexFormat; existing.vertices = mesh.vertices;
        existing.normals = mesh.normals; existing.uv = mesh.uv;
        existing.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
        existing.bounds = mesh.bounds; EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh); return existing;
    }

    static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(color); Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }

    static GameObject Shape(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Color color)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = Mat(color); return go;
    }

    static void Triangle(List<Vector3> vertices, List<int> indices, Vector3 a, Vector3 b, Vector3 c)
    {
        int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
        indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
    }

    static Mesh FinishMesh(string name, List<Vector3> v, List<int> t)
    {
        var mesh = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.uv = new Vector2[v.Count]; mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return PersistMesh(mesh);
    }

    static GameObject MeshObject(Transform parent, string name, List<Vector3> v, List<int> t, Color color, bool collider = false)
    {
        var go = Shape(parent, name, FinishMesh(name + "_Mesh", v, t), Vector3.zero, Vector3.one, color);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        return go;
    }

    static Mesh ConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
            Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), q = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            Triangle(v, t, Vector3.up, q, p); Triangle(v, t, Vector3.zero, p, q);
        }
        return coneMesh = FinishMesh("Ada_OrtakSekizgenKoni", v, t);
    }

    static Mesh RockMesh()
    {
        if (rockMesh != null) return rockMesh;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2 / 7, b = (i + 1) * Mathf.PI * 2 / 7;
            Vector3 p = new Vector3(Mathf.Cos(a), -.25f, Mathf.Sin(a)), q = new Vector3(Mathf.Cos(b), -.25f, Mathf.Sin(b));
            Vector3 r = new Vector3(Mathf.Cos(a) * .72f + .13f, .65f + .13f * Mathf.Sin(i * 3), Mathf.Sin(a) * .72f);
            Vector3 s = new Vector3(Mathf.Cos(b) * .72f + .13f, .65f + .13f * Mathf.Sin((i + 1) * 3), Mathf.Sin(b) * .72f);
            Triangle(v, t, p, s, q); Triangle(v, t, p, r, s); Triangle(v, t, r, Vector3.up, s);
            Triangle(v, t, Vector3.down * .25f, q, p);
        }
        return rockMesh = FinishMesh("Ada_OrtakFasetliKaya", v, t);
    }

    static Mesh RoofMesh()
    {
        if (roofMesh != null) return roofMesh;
        var v = new List<Vector3>(); var t = new List<int>();
        Vector3 a = new Vector3(-.5f, 0, -.5f), b = new Vector3(.5f, 0, -.5f), c = new Vector3(0, 1, -.5f);
        Vector3 d = a + Vector3.forward, e = b + Vector3.forward, f = c + Vector3.forward;
        Triangle(v,t,a,c,b); Triangle(v,t,d,e,f); Triangle(v,t,a,d,f); Triangle(v,t,a,f,c); Triangle(v,t,c,f,e); Triangle(v,t,c,e,b);
        return roofMesh = FinishMesh("Ada_OrtakBesikCati", v, t);
    }

    static Mesh TaperMesh()
    {
        if (taperedMesh != null) return taperedMesh;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            float a = (45 + i * 90) * Mathf.Deg2Rad, b = (45 + (i + 1) * 90) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)) * .7071068f, q = new Vector3(Mathf.Cos(b),0,Mathf.Sin(b)) * .7071068f;
            Vector3 r = p * .67f + Vector3.up, s = q * .67f + Vector3.up;
            Triangle(v,t,p,s,q); Triangle(v,t,p,r,s); Triangle(v,t,Vector3.up,s,r); Triangle(v,t,Vector3.zero,q,p);
        }
        return taperedMesh = FinishMesh("Ada_OrtakEğimliTemel", v, t);
    }

    // Keep interaction roots separate, but collapse decorative geometry into one draw per material.
    static void CombineVisuals(Transform root, string assetName)
    {
        var groups = new Dictionary<Material,List<CombineInstance>>();
        var renderers = new List<MeshRenderer>();
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable || renderer.GetComponent<TextMesh>() != null) continue;
            if (!renderer.enabled || renderer.name=="KiremitCati") continue;
            Material[] mats = renderer.sharedMaterials;
            for (int sub = 0; sub < filter.sharedMesh.subMeshCount && sub < mats.Length; sub++)
            {
                if (mats[sub] == null) continue;
                if (!groups.TryGetValue(mats[sub], out var list)) groups[mats[sub]] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            }
            renderers.Add(renderer);
        }
        if (groups.Count > 0)
        {
            // One persistent mesh/renderer per logical level; each material gets its own submesh.
            var temporaryMeshes = new List<Mesh>();
            var submeshes = new List<CombineInstance>();
            var materials = new List<Material>();
            foreach (var pair in groups)
            {
                var materialMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                materialMesh.CombineMeshes(pair.Value.ToArray(), true, true);
                temporaryMeshes.Add(materialMesh);
                submeshes.Add(new CombineInstance { mesh = materialMesh, subMeshIndex = 0, transform = Matrix4x4.identity });
                materials.Add(pair.Key);
            }
            var mesh = new Mesh { name = assetName + "_Combined", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(submeshes.ToArray(), false, false);
            mesh.RecalculateBounds(); mesh = PersistMesh(mesh);
            foreach (Mesh temporary in temporaryMeshes) Object.DestroyImmediate(temporary);
            var batch = new GameObject("Birlesik_Gorsel", typeof(MeshFilter), typeof(MeshRenderer));
            batch.transform.SetParent(root, false);
            batch.GetComponent<MeshFilter>().sharedMesh = mesh;
            batch.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
        }
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer.transform.childCount == 0 && renderer.gameObject != root.gameObject && renderer.GetComponent<Collider>() == null) Object.DestroyImmediate(renderer.gameObject);
            else { Object.DestroyImmediate(renderer.GetComponent<MeshFilter>()); Object.DestroyImmediate(renderer); }
        }
    }

    static GameObject FitAsset(Transform parent, string name, string path, Vector3 position, float width, float depth, float maxHeight = 30)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("Ada varlığı bulunamadı: " + path); return null; }
        Transform wrapper = Group(parent, name, position); var instance = Object.Instantiate(prefab, wrapper, false); instance.name = "Model";
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (Rigidbody b in instance.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(b);
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { Object.DestroyImmediate(wrapper.gameObject); return null; }
        Bounds bounds = LocalBounds(wrapper, renderers);
        float factor = Mathf.Min(width / Mathf.Max(.001f,bounds.size.x), depth / Mathf.Max(.001f,bounds.size.z), maxHeight / Mathf.Max(.001f,bounds.size.y));
        instance.transform.localPosition -= new Vector3(bounds.center.x,bounds.min.y,bounds.center.z); wrapper.localScale = Vector3.one * factor;
        return wrapper.gameObject;
    }

    static Bounds LocalBounds(Transform root, Renderer[] renderers)
    {
        Bounds result = new Bounds(); bool first = true;
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 p = root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if (first) { result = new Bounds(p,Vector3.zero); first=false; } else result.Encapsulate(p);
            }
        }
        return result;
    }

    static void SetInitialCollider(GameObject root)
    {
        Bounds bounds = LocalBounds(root.transform,root.GetComponentsInChildren<Renderer>());
        BoxCollider box = root.GetComponent<BoxCollider>(); if (box == null) box = root.AddComponent<BoxCollider>();
        box.center=bounds.center; box.size=bounds.size+Vector3.one*.06f;
    }

    static void Label(Transform parent,string name,string text,Vector3 pos,float size,Color color)
    {
        Transform root=Group(parent,name,pos); var tm=root.gameObject.AddComponent<TextMesh>();
        tm.text=text; tm.fontSize=64; tm.characterSize=size; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=color;
        tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); root.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
        root.localRotation=Quaternion.Euler(18,0,0);
    }

    static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float thickness,Color color)
    {
        Cube(parent,name,(a+b)*.5f,new Vector3(thickness,(b-a).magnitude,thickness),color).transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
    }

    static void Window(Transform parent,Vector3 pos,float width,float height,float rotation=0)
    {
        Transform frame=Group(parent,"Pencere",pos); frame.localRotation=Quaternion.Euler(0,rotation,0);
        Cube(frame,"Cerceve",Vector3.zero,new Vector3(width+.18f,height+.18f,.13f),Cream);
        Cube(frame,"Cam",new Vector3(0,0,-.08f),new Vector3(width,height,.055f),Glass);
        Cube(frame,"DikeyKayit",new Vector3(0,0,-.12f),new Vector3(.065f,height,.045f),Cream);
        Cube(frame,"YatayKayit",new Vector3(0,0,-.13f),new Vector3(width,.065f,.04f),Cream);
        Cube(frame,"Denizlik",new Vector3(0,-height*.5f-.1f,-.07f),new Vector3(width+.35f,.12f,.27f),Cream);
    }

    static void House(Transform parent,int level,int variation)
    {
        float w=5.4f+level*.85f,d=5.2f+level*.65f,floorH=2.45f,h=(level+1)*floorH;
        Color[] facades={new Color32(217,186,132,255),new Color32(202,212,185,255),new Color32(163,187,188,255),new Color32(218,199,164,255),new Color32(203,170,139,255)};
        Color wall=facades[variation%facades.Length];
        Cube(parent,"TasTemel",new Vector3(0,.22f,0),new Vector3(w+.35f,.44f,d+.35f),Stone);
        Cube(parent,"SivaliCephe",new Vector3(0,h*.5f+.4f,0),new Vector3(w,h,d),wall);
        for(int floor=0;floor<=level;floor++)
        {
            float y=.4f+floor*floorH;
            Cube(parent,"KatSilmesi",new Vector3(0,y+.1f,0),new Vector3(w+.22f,.18f,d+.22f),Cream);
            for(int side=-1;side<=1;side+=2)
            {
                Window(parent,new Vector3(side*w*.29f,y+1.25f,-d*.5f-.04f),.88f,1.12f);
                Window(parent,new Vector3(side*w*.29f,y+1.25f,d*.5f+.04f),.88f,1.12f,180);
                for(int z=-1;z<=1;z+=2) Window(parent,new Vector3(side*(w*.5f+.04f),y+1.25f,z*d*.28f),.85f,1.12f,side<0?90:-90);
            }
            if(floor>0)
            {
                Cube(parent,"BalkonDoseme",new Vector3(0,y-.03f,-d*.5f-.65f),new Vector3(2.4f,.18f,1.25f),Cream);
                for(int rail=-3;rail<=3;rail++) Cube(parent,"BalkonKorkulugu",new Vector3(rail*.34f,y+.42f,-d*.5f-1.21f),new Vector3(.07f,.9f,.07f),Timber);
                Cube(parent,"BalkonTutamagi",new Vector3(0,y+.9f,-d*.5f-1.21f),new Vector3(2.45f,.09f,.1f),Timber);
                Window(parent,new Vector3(0,y+1.1f,-d*.5f-.05f),.8f,1.6f);
            }
        }
        for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) Cube(parent,"KoseTasi",new Vector3(x*(w*.5f+.025f),h*.5f+.4f,z*(d*.5f+.025f)),new Vector3(.22f,h,.22f),Cream);
        Cube(parent,"GirisKapi",new Vector3(0,1.25f,-d*.5f-.08f),new Vector3(.95f,1.9f,.18f),Timber);
        Cube(parent,"KapiCam",new Vector3(0,1.6f,-d*.5f-.19f),new Vector3(.52f,.65f,.03f),Glass);
        for(int step=0;step<3;step++) Cube(parent,"Merdiven",new Vector3(0,.08f+step*.1f,-d*.5f-1.0f+step*.25f),new Vector3(1.8f,.16f+step*.2f,1.2f-step*.25f),Cream);
        float roofY=h+.48f;
        Shape(parent,"KiremitCati",RoofMesh(),new Vector3(0,roofY,0),new Vector3(w+.95f,1.9f+level*.15f,d+.95f),variation%3==0?RoofDark:Roof);
        Cube(parent,"Mahya",new Vector3(0,roofY+1.94f+level*.15f,0),new Vector3(.22f,.16f,d+1.1f),RoofDark);
        Cube(parent,"OnSacak",new Vector3(0,roofY,-d*.5f-.48f),new Vector3(w+1,.16f,.18f),Cream);
        Cube(parent,"ArkaSacak",new Vector3(0,roofY,d*.5f+.48f),new Vector3(w+1,.16f,.18f),Cream);
        Cube(parent,"Baca",new Vector3(w*.27f,roofY+1.5f,d*.2f),new Vector3(.55f,1.5f,.6f),new Color32(164,115,84,255));
        Cube(parent,"BacaSapka",new Vector3(w*.27f,roofY+2.25f,d*.2f),new Vector3(.75f,.16f,.8f),Cream);
        if(level>0)
        {
            Cube(parent,"CatiPenceresi",new Vector3(0,roofY+.55f,-d*.5f-.02f),new Vector3(1.15f,.95f,.35f),wall);
            Window(parent,new Vector3(0,roofY+.55f,-d*.5f-.22f),.64f,.65f);
            Shape(parent,"CatiPenceresiUst",RoofMesh(),new Vector3(0,roofY+1.04f,-d*.5f),new Vector3(1.6f,.7f,.8f),RoofDark);
        }
        if(level==2) for(int side=-1;side<=1;side+=2)
        {
            Cube(parent,"Ciceklik",new Vector3(side*w*.29f,4.55f,-d*.5f-.25f),new Vector3(1.2f,.24f,.4f),Timber);
            for(int i=0;i<4;i++) Shape(parent,"Sardunya",RockMesh(),new Vector3(side*w*.29f-.4f+i*.27f,4.75f,-d*.5f-.25f),Vector3.one*.18f,new Color32(196,81,100,255));
        }
    }

    static void AddHouse(Transform parent,Transform decor,int number,Vector3 position)
    {
        Transform lot=Group(decor,"KonutBahcesi_"+number,position);
        Cube(lot,"Parsel",new Vector3(0,.045f,0),new Vector3(11.7f,.09f,10.0f),number%2==0?Grass:GrassDark);
        Cube(lot,"GirisYolu",new Vector3(0,.11f,-4.4f),new Vector3(1.25f,.06f,1.6f),Sand);
        for(int side=-1;side<=1;side+=2)
        {
            Cube(lot,"BahceDuvari",new Vector3(side*5.55f,.32f,0),new Vector3(.25f,.55f,9.8f),Stone);
            Cube(lot,"OnBahceDuvari",new Vector3(side*3.55f,.32f,-4.85f),new Vector3(4.05f,.55f,.25f),Stone);
            Tree(lot,"MeyveAgaci",new Vector3(side*4.7f,.1f,3.5f),2.8f,number%3);
        }
        FlowerBed(lot,new Vector3(4.7f,.1f,-3.2f),1.1f,3.0f,number);
        Transform root=Group(parent,"Konut_"+number.ToString("00"),position);
        root.gameObject.AddComponent<BoxCollider>(); var visuals=root.gameObject.AddComponent<BuildingVisuals>(); visuals.binaTuru=BinaTuru.TemelBina; visuals.levelModelleri=new GameObject[3];
        for(int level=0;level<3;level++)
        {
            Transform model=Group(root,"Level_"+(level+1)); House(model,level,number); DecorateHouse(model,level,number); CombineVisuals(model,"Konut_"+number.ToString("00")+"_L"+(level+1));
            visuals.levelModelleri[level]=model.gameObject; model.gameObject.SetActive(level==0);
        }
        SetInitialCollider(root.gameObject);
    }

    static void BuildLevelBuildings(Transform root)
    {
        Transform gardens=Group(root,"MahalleBahceleri"); int count=0;
        foreach(float x in new[]{-45f,45f}) foreach(float z in new[]{-28f,0f,27f})
        {
            Transform neighborhood=Group(root,(x<0?"Bati":"Dogu")+"_Mahalle_"+z);
            Cube(gardens,"MahalleYayaOmurgasi",new Vector3(x,.10f,z),new Vector3(1.2f,.10f,25),Sand);
            Cube(gardens,"BahceAraSokagi",new Vector3(x,.11f,z),new Vector3(26,.10f,.8f),Sand);
            foreach(float dx in new[]{-6.5f,6.5f}) foreach(float dz in new[]{-5.2f,5.2f}) AddHouse(neighborhood,gardens,++count,new Vector3(x+dx,.1f,z+dz));
        }
        foreach(float x in new[]{-74f,74f}) foreach(float z in new[]{-33.2f,-22.8f,-5.2f,5.2f,21.8f,32.2f})
            if(Mathf.Pow(x/CoastX,2)+Mathf.Pow(z/CoastZ,2)<.91f) AddHouse(root,gardens,++count,new Vector3(x,.1f,z));
        foreach(float x in new[]{-58f,-44f,44f,58f}) AddHouse(root,gardens,++count,new Vector3(x,.1f,49));
        CombineVisuals(gardens,"KonutBahceleri");
        AddCampus(root,"AKUT_Birimi",BinaTuru.Akut,new Vector3(-15,.1f,27));
        AddCampus(root,"Itfaiye",BinaTuru.Itfaiye,new Vector3(15,.1f,27));
        AddCampus(root,"Okul",BinaTuru.Okul,new Vector3(-15,.1f,-28));
        AddCampus(root,"Altyapi_PompaIstasyonu",BinaTuru.Altyapi,new Vector3(50,.1f,-51));
    }

    public static void FrameIsland(Camera camera)
    {
        if(camera==null)return; camera.orthographic=true; camera.orthographicSize=91; camera.nearClipPlane=.3f; camera.farClipPlane=650;
        camera.transform.position=new Vector3(107,140,-136); camera.transform.LookAt(new Vector3(0,5,4));
        camera.backgroundColor=new Color32(123,169,187,255); camera.clearFlags=CameraClearFlags.SolidColor;
    }
}
