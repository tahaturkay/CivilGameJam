using UnityEngine;
using UnityEngine.Rendering;

/// <summary>The original fifteen rubble pieces, submitted together instead of fifteen scene renderers.</summary>
public sealed class IslandRubbleBatch : MonoBehaviour
{
    readonly Matrix4x4[] local=new Matrix4x4[15],world=new Matrix4x4[15];
    Mesh mesh;Material material;Bounds localBounds;RenderParams parameters;bool ready;
    public void Initialize(Mesh sharedMesh,Material sharedMaterial,Bounds bounds,float width,float depth)
    {
        mesh=sharedMesh;material=sharedMaterial;
        for(int i=0;i<local.Length;i++)
        {
            float a=i*2.399963f,r=.25f+Mathf.Sqrt(i/15f)*width*.18f;
            Vector3 p=new Vector3(bounds.center.x+width*.22f+Mathf.Cos(a)*r,bounds.min.y+.03f,bounds.center.z-depth*.30f+Mathf.Sin(a)*r*.65f);
            local[i]=Matrix4x4.TRS(p,Quaternion.Euler(0,i*47,0),new Vector3(.3f+(i%4)*.19f,.3f+(i%3)*.22f,.4f+(i%5)*.11f));
        }
        localBounds=new Bounds(bounds.center,new Vector3(width+5,Mathf.Max(4,bounds.size.y),depth+5));
        parameters=new RenderParams(material){layer=gameObject.layer,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};
        UpdateMatrices();ready=true;
    }
    void UpdateMatrices()
    {
        Matrix4x4 matrix=transform.localToWorldMatrix;
        for(int i=0;i<local.Length;i++)world[i]=matrix*local[i];
        parameters.worldBounds=new Bounds(transform.TransformPoint(localBounds.center),Vector3.Scale(localBounds.size,transform.lossyScale));
        transform.hasChanged=false;
    }
    void LateUpdate()
    {
        if(!ready||mesh==null||material==null)return;
        if(transform.hasChanged)UpdateMatrices();
        Graphics.RenderMeshInstanced(in parameters,mesh,0,world,world.Length);
    }
}
