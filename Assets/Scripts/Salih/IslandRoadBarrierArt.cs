using UnityEngine;
using static IslandCivicModelParts;

/// <summary>Paired roadside work stations, leaving the carriageway and displaced-car envelope clear.</summary>
public static class IslandRoadBarrierArt
{
    public static GameObject Create(Transform parent,Vector3 worldPosition,float roadWidth,int variant=0)
    {
        int style=((variant%3)+3)%3;
        float width=Mathf.Round(Mathf.Max(roadWidth,4)*100f)*.01f;
        Mesh mesh=CachedMesh("Ada Yol Güvenliği "+Mathf.RoundToInt(width*100)+" / "+style,()=>
        {
            var pair=new Builder();
            for(int side=-1;side<=1;side+=2)
            {
                // The old markers sat at the curb. Keep substantial props outside a displaced vehicle's body.
                Vector3 position=new Vector3(side*(width*.5f+1.65f),0,0);
                pair.Append(BuildStation(style),Matrix4x4.TRS(position,Quaternion.Euler(0,side*90,0),Vector3.one));
            }
            return pair;
        });
        GameObject root=MeshObject(parent,"YolOnarımı_"+(style==0?"Çalışma":style==1?"BozukYol":"Yönlendirme"),mesh);
        root.transform.position=worldPosition;
        return root;
    }

    static Builder BuildStation(int variant)
    {
        var b=new Builder();
        for(int side=-1;side<=1;side+=2)
        {
            b.Box(new Vector3(side*.72f,.09f,0),new Vector3(.48f,.18f,.94f),Dark);
            b.Box(new Vector3(side*.72f,.20f,0),new Vector3(.25f,.08f,.55f),Stone);
            for(int leg=-1;leg<=1;leg+=2)
                b.Beam(new Vector3(side*.72f,.18f,leg*.37f),new Vector3(side*.72f,1.45f,0),.10f,Metal);
            b.Cylinder(new Vector3(side*.72f,1.48f,0),.14f,.14f,.08f,Dark);
            b.Cylinder(new Vector3(side*.72f,1.61f,0),.105f,.075f,.20f,Amber);
            b.Cylinder(new Vector3(side*.72f,1.73f,0),.11f,.11f,.035f,Cream);
        }
        b.Box(new Vector3(0,1.14f,0),new Vector3(1.92f,.60f,.17f),Dark);
        b.Box(new Vector3(0,1.14f,0),new Vector3(1.80f,.49f,.21f),Cream);
        b.Box(new Vector3(0,.55f,0),new Vector3(1.75f,.16f,.12f),Metal);
        // Solid, raised strips stay crisp at gameplay distance and on both faces of the barrier.
        for(int face=-1;face<=1;face+=2)
        for(int stripe=-2;stripe<=2;stripe++)
            b.Box(new Vector3(stripe*.34f,1.14f,face*.119f),new Vector3(.16f,.43f,.025f),Red,Quaternion.Euler(0,0,-25));
        for(int side=-1;side<=1;side+=2)
        for(int face=-1;face<=1;face+=2)
            b.Box(new Vector3(side*.81f,1.14f,face*.14f),new Vector3(.07f,.23f,.025f),Amber);

        // A complementary sign sits behind the A-frame; no large sign protrudes into a traffic lane.
        b.Box(new Vector3(1.35f,.09f,.05f),new Vector3(.58f,.18f,.65f),Dark);
        b.Cylinder(new Vector3(1.35f,1.13f,.05f),.055f,.055f,2.05f,Metal);
        if(variant<2)
        {
            Vector2[] triangle={new Vector2(0,.47f),new Vector2(-.52f,-.37f),new Vector2(.52f,-.37f)};
            Vector2[] inset={new Vector2(0,.34f),new Vector2(-.38f,-.28f),new Vector2(.38f,-.28f)};
            b.Plate(new Vector3(1.35f,2.08f,.05f),triangle,.10f,Red);
            b.Plate(new Vector3(1.35f,2.08f,.05f),inset,.13f,Amber);
            for(int face=-1;face<=1;face+=2)
            {
                float z=.05f+face*.077f;
                if(variant==0)
                {
                    // Road worker: faceted head, bent body, shovel and soil pile.
                    b.Cylinder(new Vector3(1.36f,2.24f,z),.055f,.055f,.025f,Dark,8,Quaternion.Euler(90,0,0));
                    b.Beam(new Vector3(1.36f,2.16f,z),new Vector3(1.25f,2.01f,z),.065f,Dark,.025f);
                    b.Beam(new Vector3(1.27f,2.02f,z),new Vector3(1.17f,1.87f,z),.045f,Dark,.025f);
                    b.Beam(new Vector3(1.27f,2.02f,z),new Vector3(1.39f,1.88f,z),.045f,Dark,.025f);
                    b.Beam(new Vector3(1.30f,2.11f,z),new Vector3(1.48f,2.03f,z),.04f,Dark,.025f);
                    b.Beam(new Vector3(1.43f,2.17f,z),new Vector3(1.54f,1.89f,z),.025f,Dark,.025f);
                    b.Box(new Vector3(1.53f,1.91f,z),new Vector3(.12f,.06f,.025f),Dark);
                }
                else
                {
                    // Uneven road rather than a text label: readable independently of language/font.
                    Vector3[] p={new Vector3(1.08f,1.92f,z),new Vector3(1.23f,2.06f,z),new Vector3(1.35f,1.95f,z),new Vector3(1.50f,2.10f,z),new Vector3(1.62f,1.96f,z)};
                    for(int i=0;i<p.Length-1;i++)b.Beam(p[i],p[i+1],.065f,Dark,.025f);
                }
            }
        }
        else
        {
            b.Box(new Vector3(1.35f,2.06f,.05f),new Vector3(.91f,.69f,.10f),Dark);
            b.Box(new Vector3(1.35f,2.06f,.05f),new Vector3(.80f,.58f,.13f),Amber);
            for(int face=-1;face<=1;face+=2)
            for(int chevron=-1;chevron<=1;chevron++)
            {
                float x=1.35f+chevron*.23f,z=.05f+face*.08f;
                b.Beam(new Vector3(x-.06f,2.23f,z),new Vector3(x+.075f,2.06f,z),.065f,Dark,.025f);
                b.Beam(new Vector3(x+.075f,2.06f,z),new Vector3(x-.06f,1.89f,z),.065f,Dark,.025f);
            }
        }
        // A small six-sided cone, contrasting white collar and weighted rubber foot.
        b.Box(new Vector3(-1.25f,.055f,.12f),new Vector3(.46f,.11f,.46f),Dark);
        b.Cylinder(new Vector3(-1.25f,.39f,.12f),.18f,.045f,.62f,Amber,6);
        b.Cylinder(new Vector3(-1.25f,.42f,.12f),.126f,.10f,.13f,Cream,6);
        return b;
    }
}
