using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Readable spindrift above a tall wall, supplementing the small particle droplets below it.</summary>
public sealed class IslandSeaImpactSplash : MonoBehaviour
{
    Transform[] fans;
    Renderer[] renderers;
    Vector3[] origins;
    MaterialPropertyBlock block;
    float age,strength;
    public void Initialize(Material material,float impactStrength)
    {
        strength=impactStrength;fans=new Transform[12];renderers=new Renderer[12];origins=new Vector3[12];block=new MaterialPropertyBlock();
        for(int i=0;i<fans.Length;i++)
        {
            var go=new GameObject("SetUstuSuYelpazesi",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=IslandEffectsFactory.SoftQuadMesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            origins[i]=transform.position+new Vector3((i-5.5f)*6.4f,0,Mathf.Sin(i*1.7f));
            fans[i]=go.transform;renderers[i]=renderer;
        }
    }
    void LateUpdate()
    {
        if(fans==null)return;age+=Time.deltaTime;float phase=Mathf.Clamp01(age/1.8f);
        float alpha=Mathf.SmoothStep(0,1,phase/.10f)*(1-Mathf.SmoothStep(.32f,1,phase));
        var camera=Camera.main;block.SetColor("_BaseColor",new Color(.84f,.96f,1,alpha*.66f));
        for(int i=0;i<fans.Length;i++)
        {
            fans[i].position=origins[i]+new Vector3(Mathf.Sin(i)*phase*1.2f,phase*(3.4f+i%3)*strength,0);
            if(camera!=null)fans[i].rotation=camera.transform.rotation*Quaternion.Euler(0,0,(i%3-1)*17);
            fans[i].localScale=new Vector3(2.4f+phase*3.0f,4.6f*strength*(1-phase*.55f),1);
            renderers[i].SetPropertyBlock(block);
        }
        if(phase>=1){enabled=false;foreach(var r in renderers)r.enabled=false;}
    }
}
