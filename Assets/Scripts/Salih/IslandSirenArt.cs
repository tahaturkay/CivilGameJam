using UnityEngine;
using static IslandCivicModelParts;

/// <summary>Municipal warning mast asset. Scheduling, sound and rotation are owned by the caller.</summary>
public static class IslandSirenArt
{
    public sealed class Rig
    {
        public Transform Root;
        public Transform Rotor;
        public Light BeaconPoint;
        public Light[] RotatingSpots;
        public Renderer[] IndicatorRenderers;
    }

    /// <summary>Creates a ground-origin 8.6 m mast. The lights start disabled and cast no shadows.</summary>
    public static Rig Create(Transform parent)
    {
        var root=new GameObject("BelediyeSirenKulesi");root.transform.SetParent(parent,false);
        MeshObject(root.transform,"FacetedSirenMast",CachedMesh("Ada Belediye Siren Direği",BuildMast));
        var rotor=new GameObject("DönenKırmızıAlarm");rotor.transform.SetParent(root.transform,false);rotor.transform.localPosition=new Vector3(0,8.04f,0);
        MeshObject(rotor.transform,"AlarmRotorGövdesi",CachedMesh("Ada Siren Rotor Mekaniği",()=>
        {
            var b=new Builder();
            b.Cylinder(new Vector3(0,.08f,0),.48f,.48f,.16f,Dark,10);
            b.Cylinder(new Vector3(0,.34f,0),.36f,.31f,.44f,Cream,10);
            b.Cylinder(new Vector3(0,.61f,0),.40f,.16f,.14f,Metal,10);
            for(int side=-1;side<=1;side+=2)
            {
                b.Box(new Vector3(0,.35f,side*.30f),new Vector3(.36f,.32f,.11f),Dark);
                b.Box(new Vector3(0,.35f,side*.365f),new Vector3(.25f,.22f,.035f),Cream);
            }
            return b;
        }));
        var lens=MeshObject(rotor.transform,"KırmızıAlarmMercekleri",CachedMesh("Ada Siren Kırmızı Mercek",()=>
        {
            var b=new Builder();
            for(int side=-1;side<=1;side+=2)
            {
                Quaternion rotation=Quaternion.Euler(90,0,0);
                b.Cylinder(new Vector3(0,.35f,side*.39f),.105f,.105f,.025f,Signal,10,rotation);
                // Ribbed polycarbonate edges catch daylight while preserving the stylised silhouette.
                b.Box(new Vector3(side*.27f,.35f,0),new Vector3(.065f,.28f,.22f),Red);
            }
            return b;
        }));
        var point=NewLight(root.transform,"SirenHafifKırmızıIşık",new Vector3(0,8.34f,0),LightType.Point,13);
        var spots=new Light[2];
        for(int i=0;i<spots.Length;i++)
        {
            spots[i]=NewLight(rotor.transform,"SirenDönenIşık_"+i,new Vector3(0,.35f,i==0?.42f:-.42f),LightType.Spot,34);
            spots[i].transform.localRotation=Quaternion.Euler(9,i*180,0);
            spots[i].spotAngle=52;spots[i].innerSpotAngle=23;
        }
        return new Rig{Root=root.transform,Rotor=rotor.transform,BeaconPoint=point,RotatingSpots=spots,IndicatorRenderers=new[]{lens.GetComponent<Renderer>()}};
    }

    static Light NewLight(Transform parent,string name,Vector3 position,LightType type,float range)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var light=go.AddComponent<Light>();light.type=type;light.range=range;light.color=new Color(1,.045f,.02f);
        light.intensity=0;light.enabled=false;light.shadows=LightShadows.None;return light;
    }

    static Builder BuildMast()
    {
        var b=new Builder();
        // Stone plinth, chamfered machine base, visible fixing plates and four steel bolts.
        b.Cylinder(new Vector3(0,.12f,0),.91f,.86f,.24f,Stone,8);
        b.Box(new Vector3(0,.29f,0),new Vector3(1.03f,.14f,1.03f),Dark);
        b.Cylinder(new Vector3(0,3.56f,0),.25f,.15f,6.45f,Metal,8);
        b.Cylinder(new Vector3(0,.53f,0),.39f,.29f,.34f,Cream,8);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {
            b.Cylinder(new Vector3(x*.36f,.40f,z*.36f),.065f,.065f,.12f,Metal,6);
            b.Cylinder(new Vector3(x*.36f,.48f,z*.36f),.09f,.09f,.06f,Dark,6);
        }
        for(int band=0;band<3;band++)b.Cylinder(new Vector3(0,1.02f+band*.26f,0),.24f,.24f,.13f,band==1?Cream:Red,8);
        // Rear maintenance ladder and cable conduit add real volume without separate renderers.
        for(int side=-1;side<=1;side+=2)b.Beam(new Vector3(side*.21f,.65f,.38f),new Vector3(side*.21f,6.48f,.28f),.055f,Dark);
        for(int step=0;step<17;step++)b.Box(new Vector3(0,.85f+step*.32f,.36f-step*.005f),new Vector3(.47f,.045f,.055f),Metal);
        b.Beam(new Vector3(.28f,.50f,-.13f),new Vector3(.18f,6.70f,-.13f),.055f,Dark);
        // Utility cabinet, raised warning emblem and weather cap.
        b.Box(new Vector3(.63f,.95f,0),new Vector3(.69f,1.23f,.65f),Cream);
        b.Box(new Vector3(.63f,.95f,-.34f),new Vector3(.56f,1.08f,.055f),Metal);
        b.Box(new Vector3(.83f,.94f,-.38f),new Vector3(.045f,.21f,.035f),Dark);
        b.Box(new Vector3(.63f,1.60f,0),new Vector3(.79f,.10f,.77f),Dark);
        b.Plate(new Vector3(.63f,1.12f,-.385f),new[]{new Vector2(0,.17f),new Vector2(-.17f,-.12f),new Vector2(.17f,-.12f)},.02f,Amber);
        b.Box(new Vector3(.63f,1.13f,-.405f),new Vector3(.035f,.15f,.015f),Dark);
        b.Box(new Vector3(.63f,1.01f,-.405f),new Vector3(.035f,.035f,.015f),Dark);
        // Service platform and four radial acoustic horns make this read as a warning tower, not a street lamp.
        b.Cylinder(new Vector3(0,6.63f,0),.71f,.71f,.14f,Dark,8);
        b.Cylinder(new Vector3(0,7.14f,0),.23f,.23f,1.00f,Cream,8);
        for(int i=0;i<4;i++)
        {
            Quaternion yaw=Quaternion.Euler(0,i*90,0);
            Vector3 direction=yaw*Vector3.forward;
            Quaternion hornRotation=Quaternion.FromToRotation(Vector3.up,direction);
            b.Cylinder(new Vector3(0,7.13f,0)+direction*.62f,.19f,.46f,.79f,Cream,8,hornRotation);
            b.Cylinder(new Vector3(0,7.13f,0)+direction*1.025f,.47f,.47f,.10f,Metal,8,hornRotation);
            b.Cylinder(new Vector3(0,7.13f,0)+direction*1.085f,.36f,.36f,.025f,Dark,8,hornRotation);
            b.Cylinder(new Vector3(0,7.13f,0)+direction*1.108f,.10f,.10f,.025f,Metal,8,hornRotation);
            b.Beam(new Vector3(0,6.71f,0)+direction*.18f,new Vector3(0,6.88f,0)+direction*.68f,.075f,Metal);
        }
        b.Cylinder(new Vector3(0,7.75f,0),.40f,.30f,.18f,Red,8);
        b.Cylinder(new Vector3(0,7.95f,0),.28f,.28f,.22f,Metal,8);
        return b;
    }
}
