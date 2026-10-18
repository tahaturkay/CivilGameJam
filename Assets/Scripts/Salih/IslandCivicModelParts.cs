using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shared, owned low-poly geometry for civic props. No primitive colliders or per-part renderers.</summary>
internal static class IslandCivicModelParts
{
    internal const int Stone=0, Dark=1, Cream=2, Red=3, Amber=4, Metal=5, Signal=6;
    static readonly List<Object> owned=new List<Object>();
    static Material[] palette;
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Application.quitting-=Release;
        Release();
        Application.quitting+=Release;
    }
    static void Release()
    {
        foreach(Object item in owned)
            if(item!=null){if(Application.isPlaying)Object.Destroy(item);else Object.DestroyImmediate(item);}
        owned.Clear();meshes.Clear();palette=null;
    }
    internal static Material[] Materials
    {
        get
        {
            if(palette!=null)return palette;
            var template=Resources.Load<Material>("IslandEffects/CitizenInstancing");
            Shader shader=template!=null?template.shader:Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)shader=Shader.Find("Standard");
            Color[] colors={new Color32(146,145,129,255),new Color32(36,55,61,255),new Color32(242,231,199,255),
                new Color32(192,52,40,255),new Color32(243,160,47,255),new Color32(130,151,147,255),new Color(1,.055f,.025f)};
            palette=new Material[colors.Length];
            for(int i=0;i<palette.Length;i++)
            {
                var material=new Material(shader){name="Ada Belediye Detayı "+i,enableInstancing=true,hideFlags=HideFlags.HideAndDontSave};
                material.color=colors[i];
                if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",i==Metal?.28f:.15f);
                if(i==Signal&&material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",colors[i]*2.4f);
                }
                palette[i]=material;owned.Add(material);
            }
            return palette;
        }
    }
    internal static Mesh CachedMesh(string key,System.Func<Builder> create)
    {
        if(meshes.TryGetValue(key,out Mesh mesh)&&mesh!=null)return mesh;
        mesh=create().Build(key);mesh.hideFlags=HideFlags.HideAndDontSave;owned.Add(mesh);meshes[key]=mesh;return mesh;
    }
    internal static GameObject MeshObject(Transform parent,string name,Mesh mesh)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=Materials;
        renderer.shadowCastingMode=ShadowCastingMode.On;return go;
    }

    internal sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int>[] faces=new List<int>[7];
        internal Builder(){for(int i=0;i<faces.Length;i++)faces[i]=new List<int>();}
        void Triangle(Vector3 a,Vector3 b,Vector3 c,int material)
        {
            int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
            uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.up);
            faces[material].Add(first);faces[material].Add(first+1);faces[material].Add(first+2);
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
        {Triangle(a,b,c,material);Triangle(a,c,d,material);}
        internal void Box(Vector3 center,Vector3 size,int material,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            var p=new Vector3[8];
            for(int i=0;i<8;i++)p[i]=center+rotation*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size);
            Quad(p[0],p[4],p[6],p[2],material);Quad(p[1],p[3],p[7],p[5],material);
            Quad(p[0],p[1],p[5],p[4],material);Quad(p[2],p[6],p[7],p[3],material);
            Quad(p[0],p[2],p[3],p[1],material);Quad(p[4],p[5],p[7],p[6],material);
        }
        internal void Beam(Vector3 from,Vector3 to,float width,int material,float depth=-1)
        {
            Vector3 direction=to-from;
            Box((from+to)*.5f,new Vector3(width,direction.magnitude,depth<0?width:depth),material,Quaternion.FromToRotation(Vector3.up,direction));
        }
        internal void Cylinder(Vector3 center,float lowerRadius,float upperRadius,float height,int material,int sides=8,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            Vector3 bottom=center+rotation*(Vector3.down*height*.5f),top=center+rotation*(Vector3.up*height*.5f);
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 p=bottom+rotation*new Vector3(Mathf.Cos(a)*lowerRadius,0,Mathf.Sin(a)*lowerRadius);
                Vector3 q=bottom+rotation*new Vector3(Mathf.Cos(b)*lowerRadius,0,Mathf.Sin(b)*lowerRadius);
                Vector3 r=top+rotation*new Vector3(Mathf.Cos(b)*upperRadius,0,Mathf.Sin(b)*upperRadius);
                Vector3 s=top+rotation*new Vector3(Mathf.Cos(a)*upperRadius,0,Mathf.Sin(a)*upperRadius);
                Quad(p,s,r,q,material);Triangle(bottom,p,q,material);Triangle(top,r,s,material);
            }
        }
        internal void Plate(Vector3 center,Vector2[] outline,float thickness,int material,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            for(int i=0;i<outline.Length;i++)
            {
                Vector2 a=outline[i],b=outline[(i+1)%outline.Length];
                Vector3 p=center+rotation*new Vector3(a.x,a.y,-thickness*.5f),q=center+rotation*new Vector3(b.x,b.y,-thickness*.5f);
                Vector3 r=center+rotation*new Vector3(b.x,b.y,thickness*.5f),s=center+rotation*new Vector3(a.x,a.y,thickness*.5f);
                Triangle(center+rotation*new Vector3(0,0,-thickness*.5f),q,p,material);
                Triangle(center+rotation*new Vector3(0,0,thickness*.5f),s,r,material);
                Quad(p,q,r,s,material);
            }
        }
        internal void Append(Builder source,Matrix4x4 transform)
        {
            int offset=vertices.Count;
            foreach(Vector3 vertex in source.vertices)vertices.Add(transform.MultiplyPoint3x4(vertex));
            uv.AddRange(source.uv);
            for(int i=0;i<faces.Length;i++)foreach(int index in source.faces[i])faces[i].Add(index+offset);
        }
        internal Mesh Build(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=faces.Length;
            for(int i=0;i<faces.Length;i++)mesh.SetTriangles(faces[i],i,false);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
