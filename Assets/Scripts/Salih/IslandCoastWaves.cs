using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A single contour mesh follows the actual seawall; incoming foam is animated on the GPU.</summary>
[DisallowMultipleComponent]
public sealed class IslandCoastWaves : MonoBehaviour
{
    Mesh mesh;
    GameObject foam;
    void Start()
    {
        Transform walls=transform.Find("DenizSeti"),sea=transform.Find("Arazi/Deniz");
        Material material=Resources.Load<Material>("IslandEffects/CoastFoam");
        if(walls==null||sea==null||material==null)return;
        var points=new List<Vector3>();
        foreach(Transform segment in walls)if(segment.GetComponent<BuildingVisuals>()!=null)points.Add(segment.position);
        points.Sort((a,b)=>Mathf.Atan2(a.z-transform.position.z,a.x-transform.position.x).CompareTo(Mathf.Atan2(b.z-transform.position.z,b.x-transform.position.x)));
        if(points.Count<3)return;
        float y=sea.GetComponent<Renderer>().bounds.max.y+.17f;
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
        for(int i=0;i<=points.Count;i++)
        {
            Vector3 p=points[i%points.Count],outward=p-transform.position;outward.y=0;outward.Normalize();
            Vector3 inner=p+outward*2.5f,outer=p+outward*12;inner.y=outer.y=y;
            vertices.Add(transform.InverseTransformPoint(inner));vertices.Add(transform.InverseTransformPoint(outer));
            uvs.Add(new Vector2(i/(float)points.Count,1));uvs.Add(new Vector2(i/(float)points.Count,0));
            if(i==0)continue;int a=(i-1)*2,b=i*2;
            triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
        }
        mesh=new Mesh{name="SetCizgisiniIzleyenKopuk"};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        foam=new GameObject("KiyiyaVuranDalgalar",typeof(MeshFilter),typeof(MeshRenderer));foam.transform.SetParent(transform,false);
        foam.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=foam.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
