using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class IslandLayoutInstaller
{
    // The supplied construction uses equal circles separated by D/5. On the white
    // field the emblem is red: the left crescent and the right semicircle.
    // Geometry, rather than a bitmap, keeps the small insignia crisp at any zoom.
    static void AddOdtuFlagCloth(Transform parent, Vector3 centre)
    {
        Transform flag = Group(parent, "ODTU_Bayragi", centre);
        Cube(flag, "ODTU_BeyazKumas", Vector3.zero, new Vector3(1.5f,.9f,.045f), Color.white);
        var vertices = new List<Vector3>(); var indices = new List<int>();
        const float radius = .33f, separation = radius * 2f / 5f;
        const int strips = 64;
        for (int i=0; i<strips; i++)
        {
            float y0 = Mathf.Lerp(-radius,radius,i/(float)strips);
            float y1 = Mathf.Lerp(-radius,radius,(i+1)/(float)strips);
            float a = Mathf.Sqrt(Mathf.Max(0,radius*radius-y0*y0));
            float b = Mathf.Sqrt(Mathf.Max(0,radius*radius-y1*y1));
            for (int face=-1; face<=1; face+=2)
            {
                float z = face*.026f;
                // Disjoint strips avoid overlapping coplanar emblem triangles.
                AddEmblemStrip(vertices,indices,-a,Mathf.Min(0,separation-a),-b,Mathf.Min(0,separation-b),y0,y1,z,face);
                AddEmblemStrip(vertices,indices,0,a,0,b,y0,y1,z,face);
            }
        }
        Shape(flag,"ODTU_KirmiziAmblem",FinishMesh("ODTU_Amblem_CiftYuz",vertices,indices),Vector3.zero,Vector3.one,new Color32(227,6,19,255));
        // Five flags share one two-material mesh; no extra textures or lights.
        CombineVisuals(flag,"ODTU_Bayrak");
    }

    static void AddEmblemStrip(List<Vector3> v,List<int> t,float l0,float r0,float l1,float r1,float y0,float y1,float z,int face)
    {
        Vector3 a=new Vector3(l0,y0,z),b=new Vector3(r0,y0,z),c=new Vector3(r1,y1,z),d=new Vector3(l1,y1,z);
        if(face>0) { Triangle(v,t,a,b,c);Triangle(v,t,a,c,d); }
        else { Triangle(v,t,a,c,b);Triangle(v,t,a,d,c); }
    }

    /// <summary>Targeted and repeatable update: existing civic architecture, poles,
    /// colliders, levels and interaction paths remain untouched.</summary>
    public static string RefreshOdtuCivicIdentity(GameObject island)
    {
        if(island==null) throw new ArgumentNullException(nameof(island));
        Transform hall=island.transform.Find("SabitAlanlar/Belediye");
        Transform school=island.transform.Find("YukseltilebilirYapilar/Okul");
        if(hall==null||school==null) throw new InvalidOperationException("Civic roots were not found; no identity update applied.");
        var sites=new List<Transform>{hall};
        for(int level=1;level<=3;level++)
        {
            Transform model=school.Find("Level_"+level);
            if(model==null) throw new InvalidOperationException("Missing school level "+level);
            sites.Add(model);
        }
        int removed=0;
        for(int i=0;i<sites.Count;i++)
        {
            Transform site=sites[i];
            var positions=i==0 ? new[]{new Vector3(-6.6f,.2f,-4.5f),new Vector3(6.6f,.2f,-4.5f)} : new[]{new Vector3(-9,.2f,-3)};
            // Existing separate ODTU children are replaced, not accumulated.
            for(int child=site.childCount-1;child>=0;child--)
                if(site.GetChild(child).name=="ODTU_Bayragi") UnityEngine.Object.DestroyImmediate(site.GetChild(child).gameObject);
            removed+=RemoveBakedFlagFaces(site,positions);
            foreach(Vector3 p in positions) AddOdtuFlagCloth(site,p+new Vector3(.75f,4.95f,0));
            Transform label=site.Find(i==0 ? "BelediyeTabela" : "OkulTabelasi");
            if(label!=null && label.TryGetComponent(out TextMesh text))
            {
                text.text=i==0 ? "KALKANLI MUHTARLIĞI" : "ODTÜ";
                text.characterSize=i==0 ? .15f : .25f;
                EditorUtility.SetDirty(text);
            }
        }
        return "ODTÜ identity: 5 flags, 4 signs; removed "+removed+" old flag triangles, civic geometry preserved.";
    }

    /// <summary>Rebuilds only separate cloth children; no civic mesh, label, pole or interaction is changed.</summary>
    public static int RefreshOdtuFlagCloth(GameObject island)
    {
        if(island==null)throw new ArgumentNullException(nameof(island));
        var flags=new List<Transform>();
        foreach(Transform child in island.GetComponentsInChildren<Transform>(true))
            if(child.name=="ODTU_Bayragi")flags.Add(child);
        if(flags.Count!=5)throw new InvalidOperationException("Expected five separate ODTÜ flag cloths, found "+flags.Count);
        foreach(Transform oldFlag in flags)
        {
            Transform parent=oldFlag.parent;
            Vector3 position=oldFlag.localPosition,scale=oldFlag.localScale;
            Quaternion rotation=oldFlag.localRotation;
            int index=oldFlag.GetSiblingIndex();
            UnityEngine.Object.DestroyImmediate(oldFlag.gameObject);
            AddOdtuFlagCloth(parent,position);
            Transform replacement=parent.GetChild(parent.childCount-1);
            replacement.localRotation=rotation;replacement.localScale=scale;replacement.SetSiblingIndex(index);
        }
        return flags.Count;
    }

    // Combined decorative meshes have no source object names. Only triangles
    // wholly inside the former cloth/square bounds are stripped; every other
    // vertex and index remains as authored, including the original flagpoles.
    static int RemoveBakedFlagFaces(Transform site,Vector3[] polePositions)
    {
        Transform batch=site.Find("Birlesik_Gorsel");
        if(batch==null || !batch.TryGetComponent(out MeshFilter filter) || filter.sharedMesh==null) return 0;
        Mesh source=filter.sharedMesh;
        if(!source.isReadable) throw new InvalidOperationException("Civic mesh must be readable: "+source.name);
        var bounds=new List<Bounds>();
        foreach(Vector3 p in polePositions)
            bounds.Add(new Bounds(p+new Vector3(.75f,4.95f,-.015f),new Vector3(1.502f,.902f,.15f)));
        Vector3[] vertices=source.vertices;
        Matrix4x4 toSite=site.worldToLocalMatrix*batch.localToWorldMatrix;
        var triangles=new List<int[]>(); int removed=0;
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            int[] old=source.GetTriangles(sub); var kept=new List<int>(old.Length);
            for(int j=0;j<old.Length;j+=3)
            {
                bool flag=false;
                foreach(Bounds area in bounds)
                    if(area.Contains(toSite.MultiplyPoint3x4(vertices[old[j]]))&&area.Contains(toSite.MultiplyPoint3x4(vertices[old[j+1]]))&&area.Contains(toSite.MultiplyPoint3x4(vertices[old[j+2]]))) {flag=true;break;}
                if(flag) removed++;
                else {kept.Add(old[j]);kept.Add(old[j+1]);kept.Add(old[j+2]);}
            }
            triangles.Add(kept.ToArray());
        }
        if(removed==0) return 0;
        Mesh replacement=UnityEngine.Object.Instantiate(source); replacement.name=source.name;
        for(int sub=0;sub<triangles.Count;sub++) replacement.SetTriangles(triangles[sub],sub,false);
        filter.sharedMesh=PersistMesh(replacement);
        return removed;
    }
}
