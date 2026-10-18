using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A small horizon sun, built with the island's existing unlit particle shader.</summary>
public sealed class IslandFinaleSunDisc : MonoBehaviour
{
    Mesh mesh;
    Material material;
    GameObject sky;
    Mesh skyMesh;
    Camera view;
    Vector3 horizon,origin;
    public void Configure(Vector3 direction,Vector3 anchor)
    {
        horizon=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;origin=anchor;
        var filter=gameObject.AddComponent<MeshFilter>();var renderer=gameObject.AddComponent<MeshRenderer>();
        material=new Material(IslandEffectsFactory.SmokeMaterial){name="FinalGunBatimi"};
        material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetColor("_BaseColor",Color.white);renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        const int segments=32,rings=3;
        var vertices=new Vector3[1+segments*rings];var colors=new Color[vertices.Length];var indices=new int[segments*3+(rings-1)*segments*6];
        colors[0]=new Color(1,.82f,.44f,1);float[] radii={.30f,.39f,1};
        Color[] tints={new Color(1,.72f,.30f,1),new Color(1,.46f,.18f,.22f),new Color(1,.30f,.12f,0)};
        for(int ring=0;ring<rings;ring++)for(int s=0;s<segments;s++)
        {float a=s*Mathf.PI*2/segments;int i=1+ring*segments+s;vertices[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radii[ring];colors[i]=tints[ring];}
        int k=0;
        for(int s=0;s<segments;s++){indices[k++]=0;indices[k++]=1+s;indices[k++]=1+(s+1)%segments;}
        for(int ring=0;ring<rings-1;ring++)for(int s=0;s<segments;s++)
        {
            int a=1+ring*segments+s,b=1+ring*segments+(s+1)%segments,c=a+segments,d=b+segments;
            indices[k++]=a;indices[k++]=c;indices[k++]=b;indices[k++]=b;indices[k++]=c;indices[k++]=d;
        }
        mesh=new Mesh{name="YumusakUfukGunesi"};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=indices;mesh.RecalculateBounds();filter.sharedMesh=mesh;
        transform.rotation=Quaternion.LookRotation(-horizon,Vector3.up);transform.localScale=Vector3.one*20;SetProgress(0);
        view=Camera.main;
        if(view!=null)
        {
            sky=new GameObject("FinalGunBatimiUfku",typeof(MeshFilter),typeof(MeshRenderer));sky.transform.SetParent(view.transform,false);
            skyMesh=new Mesh{name="FinalUfukRenkGecisi"};
            skyMesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
            skyMesh.colors=new[]{new Color(1,.52f,.25f,1),new Color(.35f,.43f,.57f,1),new Color(.35f,.43f,.57f,1),new Color(1,.52f,.25f,1)};
            skyMesh.triangles=new[]{0,1,2,0,2,3};skyMesh.RecalculateBounds();sky.GetComponent<MeshFilter>().sharedMesh=skyMesh;
            var backdrop=sky.GetComponent<MeshRenderer>();backdrop.sharedMaterial=material;backdrop.shadowCastingMode=ShadowCastingMode.Off;backdrop.receiveShadows=false;
        }
    }
    public void SetProgress(float progress)
    {transform.position=origin+horizon*220+Vector3.up*Mathf.Lerp(16.5f,-4f,Mathf.SmoothStep(0,1,progress));}
    void LateUpdate()
    {
        if(view==null||sky==null)return;
        const float depth=400;
        float height=2*depth*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*1.05f;
        sky.transform.localPosition=Vector3.forward*depth;sky.transform.localRotation=Quaternion.identity;
        sky.transform.localScale=new Vector3(height*view.aspect,height,1);
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(sky!=null)Destroy(sky);if(skyMesh!=null)Destroy(skyMesh);if(material!=null)Destroy(material);}
}
