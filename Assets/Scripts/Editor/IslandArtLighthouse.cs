using System.Collections.Generic;
using UnityEngine;

public static partial class IslandLayoutInstaller
{
    /// <summary>One coastal landmark. Position is local to the parent; rotating light is wired by the night controller.</summary>
    public static Transform BuildLighthouse(Transform parent, Vector3 localPosition)
    {
        Transform existing = parent.Find("DenizFeneri");
        if (existing != null) return existing;
        Transform root = Group(parent,"DenizFeneri",localPosition);
        Transform stonework = Group(root,"FenerMimarisi");
        Color white = new Color32(229,222,199,255), red = new Color32(170,68,49,255);
        Color iron = new Color32(64,86,91,255), lantern = new Color32(244,218,151,255);
        Mesh octagon = LighthouseOctagon();
        Shape(stonework,"SekizgenKayaTemel",octagon,Vector3.zero,new Vector3(4.4f,.55f,4.4f),Stone);
        Shape(stonework,"FenerTasKaidesi",octagon,new Vector3(0,.5f,0),new Vector3(3.5f,.85f,3.5f),Cream);
        Shape(stonework,"AltKuleGovdesi",octagon,new Vector3(0,1.2f,0),new Vector3(2.65f,4.4f,2.65f),white);
        Shape(stonework,"KirmiziKuleKusagi",octagon,new Vector3(0,5.6f,0),new Vector3(2.42f,1.45f,2.42f),red);
        Shape(stonework,"UstKuleGovdesi",octagon,new Vector3(0,7.05f,0),new Vector3(2.18f,3.0f,2.18f),white);
        for(int i=0;i<3;i++)
            Window(stonework,new Vector3(0,3.1f+i*2.3f,-(i<2?2.48f:2.04f)),.46f,.80f);
        Cube(stonework,"FenerKapisi",new Vector3(0,2.0f,-2.66f),new Vector3(.95f,1.65f,.14f),Timber);
        Cube(stonework,"KapiLentosu",new Vector3(0,2.87f,-2.66f),new Vector3(1.26f,.18f,.30f),white);
        for(int i=0;i<4;i++) Cube(stonework,"FenerGirisBasamagi",new Vector3(0,.20f+i*.25f,-4.2f+i*.35f),new Vector3(2.1f,.4f+i*.5f,1.2f-i*.12f),Stone);
        Shape(stonework,"SeyirBalkonu",octagon,new Vector3(0,9.75f,0),new Vector3(3.10f,.28f,3.10f),iron);
        Shape(stonework,"BalkonAltSilmesi",octagon,new Vector3(0,9.48f,0),new Vector3(2.57f,.22f,2.57f),white);
        for(int i=0;i<12;i++)
        {
            float a = i*Mathf.PI/6, b=(i+1)*Mathf.PI/6;
            Vector3 p=new Vector3(Mathf.Sin(a)*2.92f,10.05f,Mathf.Cos(a)*2.92f);
            Vector3 q=new Vector3(Mathf.Sin(b)*2.92f,10.05f,Mathf.Cos(b)*2.92f);
            Beam(stonework,"BalkonKorkulukDikmesi",p,p+Vector3.up*.83f,.075f,iron);
            Beam(stonework,"BalkonKorkulukKusagi",p+Vector3.up*.76f,q+Vector3.up*.76f,.085f,iron);
        }
        // Stationary glass lantern and copper-red cap enclose the rotating reflector.
        for(int i=0;i<8;i++)
        {
            float angle=i*45f;
            Vector3 radial=Quaternion.Euler(0,angle,0)*Vector3.forward;
            Transform pane=Group(stonework,"FenerCamCercevesi",radial*1.47f+Vector3.up*11.08f);
            pane.localRotation=Quaternion.Euler(0,angle,0);
            Cube(pane,"FenerCami",Vector3.zero,new Vector3(1.10f,1.8f,.065f),Glass);
            Cube(pane,"FenerKabinDiregi",new Vector3(.58f,0,0),new Vector3(.12f,1.9f,.12f),iron);
        }
        Shape(stonework,"FenerKabinCatisi",ConeMesh(),new Vector3(0,12.05f,0),new Vector3(2.2f,1.45f,2.2f),red);
        Beam(stonework,"Paratoner",new Vector3(0,13.4f,0),new Vector3(0,14.3f,0),.085f,iron);
        Shape(stonework,"FenerAydinlikCemberi",octagon,new Vector3(0,11.95f,0),new Vector3(1.73f,.18f,1.73f),iron);
        CombineVisuals(stonework,"DenizFeneriMimarisi");

        Transform head=Group(root,"IsikKafasi",new Vector3(0,11.08f,0));
        Cube(head,"DonerProjektor",Vector3.zero,new Vector3(.70f,.78f,.85f),iron);
        Cube(head,"FenerMercegi",new Vector3(0,0,.47f),new Vector3(.61f,.66f,.06f),lantern);
        // Keep the head and marker outside the static combine so the night system can rotate it.
        CombineVisuals(head,"DenizFeneriDonerProjektoru");
        Group(head,"IsikKaynagi",new Vector3(0,0,1.62f));
        return root;
    }

    static Mesh lighthouseOctagon;
    static Mesh LighthouseOctagon()
    {
        if(lighthouseOctagon!=null) return lighthouseOctagon;
        var vertices=new List<Vector3>(); var indices=new List<int>();
        for(int i=0;i<8;i++)
        {
            float a=(22.5f+i*45f)*Mathf.Deg2Rad,b=(22.5f+(i+1)*45f)*Mathf.Deg2Rad;
            Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
            // A slight inward taper gives the tower the silhouette of masonry rather than stacked boxes.
            Vector3 r=p*.94f+Vector3.up,s=q*.94f+Vector3.up;
            Triangle(vertices,indices,p,r,s); Triangle(vertices,indices,p,s,q);
            Triangle(vertices,indices,Vector3.zero,p,q); Triangle(vertices,indices,Vector3.up,s,r);
        }
        return lighthouseOctagon=FinishMesh("Ada_FenerSekizgenGovde",vertices,indices);
    }
}
