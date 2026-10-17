using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Ground cracks and hopping gravel: visual-only, no rigidbodies or new physics workload.</summary>
[DisallowMultipleComponent]
public sealed class IslandGroundQuake : MonoBehaviour
{
    public DisasterController disaster;
    public Bounds land;
    GameObject cracks;
    Material soil,gravel;
    Mesh crackMesh,stoneMesh;
    readonly Matrix4x4[] stones=new Matrix4x4[96];
    readonly Vector3[] origins=new Vector3[96];
    float movement,fade;
    bool initialized;
    bool wasActive;
    public bool GroundShaking=>movement>.05f;
    public bool CracksVisible=>cracks!=null&&cracks.activeSelf;

    void Start()
    {
        if(disaster==null)disaster=GetComponent<DisasterController>();
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        soil=new Material(shader){name="Ada Toprak Catlagi",color=new Color(.29f,.24f,.17f)};
        gravel=new Material(shader){name="Ada Titreyen Cakil",color=new Color(.51f,.46f,.35f),enableInstancing=true};
        stoneMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        cracks=new GameObject("DepremZeminCatlaklari",typeof(MeshFilter),typeof(MeshRenderer));cracks.transform.SetParent(transform,false);
        var v=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<origins.Length;i++)
        {
            // Grass strips between blocks, away from the live traffic lanes.
            float x=(i%2==0?-1:1)*(9+(i%5)*15);float z=-55+(i/2%12)*9.3f;
            origins[i]=new Vector3(land.center.x+x,land.max.y+.07f,land.center.z+z);
            if(i%3!=0)continue;
            Vector3 p=origins[i];
            for(int k=0;k<5;k++)
            {
                Vector3 q=p+new Vector3(Mathf.Sin(i*1.7f+k*2.1f)*1.7f,0,1.1f);
                Vector3 side=Vector3.Cross(Vector3.up,(q-p).normalized)*(.07f+(k%2)*.035f);
                int n=v.Count;v.Add(p-side);v.Add(p+side);v.Add(q+side*.4f);v.Add(q-side*.4f);
                tris.Add(n);tris.Add(n+2);tris.Add(n+1);tris.Add(n);tris.Add(n+3);tris.Add(n+2);p=q;
            }
        }
        // Geometry is in world space; the disaster host may be translated.
        for(int i=0;i<v.Count;i++)v[i]=transform.InverseTransformPoint(v[i]);
        crackMesh=new Mesh{name="Ada Kademeli Zemin Yarilmasi"};crackMesh.SetVertices(v);crackMesh.SetTriangles(tris,0);crackMesh.RecalculateNormals();crackMesh.RecalculateBounds();
        cracks.GetComponent<MeshFilter>().sharedMesh=crackMesh;cracks.GetComponent<MeshRenderer>().sharedMaterial=soil;
        cracks.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;cracks.SetActive(false);initialized=true;
    }
    void LateUpdate()
    {
        if(!initialized)return;
        bool active=disaster!=null&&disaster.AfetDevamEdiyorMu&&disaster.AktifAfetTuru==AfetTuru.Deprem;
        if(active&&!wasActive)for(int i=0;i<8;i++)IslandEffectsFactory.CreateDust(transform,origins[i*11],.65f);
        wasActive=active;
        var gm=GameManager.Instance;
        bool damage=gm!=null&&gm.temelBinaHasarli&&gm.HasarTuru(BinaTuru.TemelBina)==AfetTuru.Deprem;
        movement=Mathf.MoveTowards(movement,active?1:0,Time.deltaTime*2);
        fade=Mathf.MoveTowards(fade,active||damage?1:0,Time.deltaTime*.8f);
        cracks.SetActive(fade>.03f);soil.color=Color.Lerp(new Color(.43f,.46f,.28f),new Color(.23f,.19f,.13f),fade);
        if(movement<.01f)return;
        for(int i=0;i<stones.Length;i++)
        {
            float hop=Mathf.Abs(Mathf.Sin(Time.time*18+i*2.1f))*movement;
            Vector3 p=origins[i]+new Vector3(Mathf.Sin(Time.time*31+i)*.06f,hop*.30f,Mathf.Cos(Time.time*23+i)*.05f);
            stones[i]=Matrix4x4.TRS(p,Quaternion.Euler(hop*32,i*47,hop*23),new Vector3(.13f+(i%3)*.04f,.1f,.18f));
        }
        if(SystemInfo.supportsInstancing){var rp=new RenderParams(gravel){shadowCastingMode=ShadowCastingMode.Off};Graphics.RenderMeshInstanced(in rp,stoneMesh,0,stones,stones.Length);}
        else for(int i=0;i<stones.Length;i++)Graphics.DrawMesh(stoneMesh,stones[i],gravel,0);
    }
    void OnDestroy(){if(cracks!=null)Destroy(cracks);if(crackMesh!=null)Destroy(crackMesh);if(soil!=null)Destroy(soil);if(gravel!=null)Destroy(gravel);}
}
