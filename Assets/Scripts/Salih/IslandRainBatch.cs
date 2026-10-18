using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Same slanted storm rain, submitted in reusable mesh batches rather than streamed billboard vertices.</summary>
public sealed class IslandRainBatch : MonoBehaviour
{
    Matrix4x4[] matrices;
    Vector3[] seeds;
    Mesh mesh;Material material;RenderParams parameters;
    Vector3 area;
    float height,elapsed;
    int count;
    Camera view;
    public void Configure(Mesh quad,Material rainMaterial,Vector3 size,float fallHeight,float particlesPerSecond)
    {
        mesh=quad;material=rainMaterial;material.enableInstancing=true;area=size;height=fallHeight;
        count=Mathf.CeilToInt(particlesPerSecond*height/30f);matrices=new Matrix4x4[count];seeds=new Vector3[count];
        for(int i=0;i<count;i++)seeds[i]=new Vector3(Mathf.Repeat(i*.754877666f,1)-.5f,Mathf.Repeat(i*.569840296f,1),Mathf.Repeat(i*.43857902f,1)-.5f);
        var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.71f,.83f,.88f,.48f));
        parameters=new RenderParams(material){shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,matProps=tint,
            worldBounds=new Bounds(transform.position-Vector3.up*height*.5f,new Vector3(size.x+20,height+8,size.z+20))};
        view=Camera.main;
    }
    void OnEnable(){elapsed=0;}
    void LateUpdate()
    {
        if(matrices==null||mesh==null||material==null)return;
        elapsed+=Time.deltaTime;if(view==null)view=Camera.main;if(view==null)return;
        float lifetime=height/30f;int active=Mathf.Min(count,Mathf.CeilToInt(count*Mathf.Clamp01(elapsed/lifetime)));
        Quaternion facing=Quaternion.LookRotation(view.transform.forward,new Vector3(4.5f,30,-1.5f));
        for(int i=0;i<active;i++)
        {
            Vector3 seed=seeds[i];float age=Mathf.Repeat(elapsed+seed.y*lifetime,lifetime);
            Vector3 p=transform.position+new Vector3(seed.x*area.x-4.5f*age,-30f*age,seed.z*area.z+1.5f*age);
            float width=Mathf.Lerp(.07f,.13f,seed.y);
            matrices[i]=Matrix4x4.TRS(p,facing,new Vector3(width,1.8f+width*5,1));
        }
        for(int first=0;first<active;first+=511)
            Graphics.RenderMeshInstanced(in parameters,mesh,0,matrices,Mathf.Min(511,active-first),first);
    }
}
