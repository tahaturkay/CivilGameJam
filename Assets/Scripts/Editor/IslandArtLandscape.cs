using System.Collections.Generic;
using UnityEngine;

public static partial class IslandLayoutInstaller
{
    static Vector2 CoastPoint(int i,int count,float rx,float rz)
    {
        float a=i/(float)count*Mathf.PI*2;
        float wobble=1+.045f*Mathf.Sin(a*3+.4f)+.028f*Mathf.Sin(a*7+1.2f)+.014f*Mathf.Sin(a*13);
        float bay=Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(a*Mathf.Rad2Deg,258)/18,2))*.12f;
        return new Vector2(Mathf.Cos(a)*rx*(wobble-bay),Mathf.Sin(a)*rz*(wobble-bay));
    }
    static Vector3 Coast(int i,float fraction,float y)
    {
        Vector2 p=CoastPoint(i,CoastCount,CoastX,CoastZ)*fraction; return new Vector3(p.x,y,p.y);
    }
    static void BuildTerrain(Transform root)
    {
        Cube(root,"Deniz",new Vector3(0,-1.0f,0),new Vector3(430,.8f,370),new Color32(37,115,150,255));
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<CoastCount;i++)Triangle(v,t,new Vector3(0,GroundY,0),Coast(i+1,1,GroundY),Coast(i,1,GroundY));
        MeshObject(root,"Ada_OtlakYuzey",v,t,Grass,true);
        Transform cliffs=Group(root,"KayalikKiyilar");
        for(int band=0;band<3;band++)
        {
            v.Clear();t.Clear();
            float top=GroundY-band*2.0f,bottom=top-2.05f;
            for(int i=0;i<CoastCount;i++)
            {
                Vector3 a=Coast(i,1-band*.012f,top),b=Coast(i+1,1-band*.012f,top);
                Vector3 c=Coast(i,1.015f-band*.007f,bottom),d=Coast(i+1,1.015f-band*.007f,bottom);
                c.x+=Mathf.Sin(i*2.31f)*.7f; d.x+=Mathf.Sin((i+1)*2.31f)*.7f;
                Triangle(v,t,a,b,c);Triangle(v,t,b,d,c);
            }
            MeshObject(cliffs,"KiyiJeolojikKatman_"+band,v,t,band==0?new Color32(158,153,120,255):band==1?new Color32(124,134,126,255):new Color32(92,111,112,255));
        }
        Transform scatter=Group(root,"KiyiKayalariVeKopuk");
        for(int i=0;i<CoastCount*3;i++)
        {
            float a=i/(float)(CoastCount*3)*Mathf.PI*2;
            Vector2 p=CoastPoint(i,CoastCount*3,CoastX,CoastZ)*(1.015f+.024f*Mathf.Sin(i*2.3f));
            float s=1.2f+(i%5)*.36f;
            Shape(scatter,"DalgakiranKayasi",RockMesh(),new Vector3(p.x,-.3f,p.y),new Vector3(s,1.5f+(i%3)*.7f,s*.8f),i%3==0?Stone:new Color32(107,125,120,255)).transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            if(i%2==0)
            {
                p*=1.035f;
                Shape(scatter,"KiyiKopugu",RockMesh(),new Vector3(p.x,-.49f,p.y),new Vector3(2.7f,.06f,1.3f),i%4==0?new Color32(174,227,217,255):new Color32(91,182,183,255));
            }
        }
        CombineVisuals(scatter,"KiyiKayalariKopuk");
        BuildCoastalPatch(root,"KiyiKumGecisi",0,CoastCount,.956f,GroundY+.015f);
    }
    static void BuildCoastalPatch(Transform parent,string name,int start,int end,float inner,float y)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=start;i<end;i++)
        {
            Vector3 a=Coast(i,inner,y),b=Coast(i+1,inner,y),c=Coast(i,1,y),d=Coast(i+1,1,y);
            Triangle(v,t,a,b,d);Triangle(v,t,a,d,c);
        }
        MeshObject(parent,name,v,t,Sand);
    }
    static void Tree(Transform parent,string name,Vector3 pos,float height,int variant=0)
    {
        Transform tree=Group(parent,name,pos);tree.localRotation=Quaternion.Euler(0,(pos.x*23+pos.z*17)%360,0);
        Cube(tree,"Govde",new Vector3(0,height*.28f,0),new Vector3(height*.085f,height*.56f,height*.085f),Timber);
        if(variant%3==0)
        {
            for(int layer=0;layer<3;layer++)Shape(tree,"IbreliTac",ConeMesh(),new Vector3(0,height*(.22f+layer*.19f),0),new Vector3(height*(.36f-layer*.075f),height*.46f,height*(.36f-layer*.075f)),layer%2==0?Pine:Sage);
        }
        else
        {
            Shape(tree,"GenisTac",RockMesh(),new Vector3(0,height*.56f,0),new Vector3(height*.32f,height*.43f,height*.31f),variant%3==1?new Color32(63,115,66,255):Sage);
            Shape(tree,"YanTac",RockMesh(),new Vector3(height*.17f,height*.47f,-height*.12f),new Vector3(height*.23f,height*.28f,height*.26f),GrassDark);
        }
    }
    static void FlowerBed(Transform parent,Vector3 pos,float width,float depth,int variant)
    {
        Transform bed=Group(parent,"CicekTarhi",pos);
        Cube(bed,"Bordur",new Vector3(0,.10f,0),new Vector3(width+.2f,.20f,depth+.2f),Stone);
        Cube(bed,"Toprak",new Vector3(0,.21f,0),new Vector3(width,.05f,depth),Timber);
        Color[] colors={new Color32(207,101,96,255),new Color32(238,194,83,255),new Color32(170,140,189,255)};
        int n=Mathf.Max(2,Mathf.CeilToInt(depth/.55f));
        for(int i=0;i<n;i++)
        {
            Vector3 p=new Vector3(Mathf.Sin(i*2.1f)*width*.25f,.35f,-depth*.4f+i*depth*.8f/(n-1));
            Shape(bed,"Yaprak",RockMesh(),p,new Vector3(width*.38f,.17f,.35f),Pine);
            Shape(bed,"Cicek",RockMesh(),p+Vector3.up*.15f,new Vector3(.25f,.17f,.25f),colors[variant%3]);
        }
    }
    static void RoadLink(Transform root,string name,Vector2 from,Vector2 to,float width=4.8f)
    {
        Vector2 delta=to-from,center=(from+to)*.5f;Quaternion rotation=Quaternion.Euler(0,Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg,0);
        // Parallel surfaces at junctions need deterministic depth: depress the
        // north/south bed slightly below the east/west bed, including dirt details.
        float bedY=Mathf.Abs(delta.y)>Mathf.Abs(delta.x)?-.025f:0;
        Transform road=Group(root,name,new Vector3(center.x,bedY,center.y));road.localRotation=rotation;
        road.gameObject.AddComponent<BoxCollider>();
        var visual=road.gameObject.AddComponent<BuildingVisuals>();visual.binaTuru=BinaTuru.Altyapi;
        visual.colliderModeleUysun=false;visual.levelModelleri=new GameObject[3];
        var collider=road.GetComponent<Collider>() as BoxCollider;
        if(collider==null)collider=road.gameObject.AddComponent<BoxCollider>();
        collider.center=new Vector3(0,.23f,0);collider.size=new Vector3(width,.46f,delta.magnitude+width);
        Color[] surfaces={new Color32(151,115,76,255),new Color32(139,143,132,255),Road};
        string[] labels={"Toprak","Cakil","Asfalt"};
        for(int level=0;level<3;level++)
        {
            Transform model=Group(road,"Level"+(level+1)+"_"+labels[level]);visual.levelModelleri[level]=model.gameObject;
            Cube(model,"YolZemini",new Vector3(0,.22f,0),new Vector3(width,.18f,delta.magnitude+width),surfaces[level]);
            if(level>0)
            {
                for(int side=-1;side<=1;side+=2)
                    Cube(model,"YolBorduru",new Vector3(side*(width*.5f+.45f),.12f,0),new Vector3(.9f,.24f,delta.magnitude+width),Cream);
            }
            if(level<2)
            {
                int count=Mathf.CeilToInt(delta.magnitude/(level==0?3.8f:2.1f));
                for(int k=0;k<count;k++)
                {
                    float z=-delta.magnitude*.48f+(k+.5f)*delta.magnitude/count;
                    float x=Mathf.Sin(k*12.7f)*width*.38f;
                    Shape(model,level==0?"ToprakIzi":"CakilTasi",RockMesh(),new Vector3(x,.318f,z),new Vector3(level==0?.7f:.25f,.018f,level==0?1.3f:.3f),level==0?new Color32(131,99,68,255):Stone);
                }
            }
            else for(int k=1;k<delta.magnitude/3.4f;k++)
                Cube(model,"KesikliSerit",new Vector3(0,.318f,-delta.magnitude*.5f+k*3.4f),new Vector3(.13f,.018f,1.4f),Cream);
            CombineVisuals(model,name+"_"+labels[level]);model.gameObject.SetActive(level==0);
        }
        Transform decoration=root.Find("YolDekorasyonu");
        int lampCount=Mathf.FloorToInt(delta.magnitude/16);
        for(int i=1;i<=lampCount;i++)
        {
            Vector2 p=Vector2.Lerp(from,to,i/(float)(lampCount+1));Vector2 side=new Vector2(delta.normalized.y,-delta.normalized.x);
            for(int s=-1;s<=1;s+=2)
            {
                Vector2 q=p+side*(width*.5f+.65f)*s;
                Lamp(decoration,new Vector3(q.x,.25f,q.y));
                q+=side*1.9f*s;
                if(i%2==0)Tree(decoration,"BulvarAgaci",new Vector3(q.x,.05f,q.y),3.2f,1);
            }
        }
    }
    static void Lamp(Transform parent,Vector3 pos)
    {
        Transform lamp=Group(parent,"SokakFeneri",pos);
        Cube(lamp,"Kaide",new Vector3(0,.15f,0),new Vector3(.45f,.3f,.45f),Stone);
        Cube(lamp,"Direk",new Vector3(0,1.8f,0),new Vector3(.13f,3.4f,.13f),new Color32(62,80,77,255));
        Cube(lamp,"Fener",new Vector3(0,3.65f,0),new Vector3(.48f,.60f,.48f),new Color32(244,218,151,255));
        Shape(lamp,"Sapka",ConeMesh(),new Vector3(0,3.97f,0),new Vector3(.45f,.35f,.45f),Pine);
    }
    static void Crosswalk(Transform root,float x,float z,bool horizontal)
    {
        for(int i=0;i<6;i++)
        {
            Vector3 p=horizontal?new Vector3(x,.324f,z-2+i*.8f):new Vector3(x-2+i*.8f,.324f,z);
            Cube(root,"YayaGecidi",p,horizontal?new Vector3(1.5f,.02f,.40f):new Vector3(.40f,.02f,1.5f),Cream);
        }
    }
    static void BuildRoadNetwork(Transform root)
    {
        Transform decoration=Group(root,"YolDekorasyonu");
        foreach(float z in new[]{-42f,-14f,14f,40f})RoadLink(root,"DoguBatiBulvari_"+z,new Vector2(z==40?-72:z==-42?-71:-79,z),new Vector2(z==40?72:z==-42?71:79,z));
        foreach(float x in new[]{-60f,-30f,30f,60f})RoadLink(root,"KuzeyGuneyBulvari_"+x,new Vector2(x,-44),new Vector2(x,42));
        RoadLink(root,"MerkezGuney",new Vector2(0,-42),new Vector2(0,-14));
        RoadLink(root,"MerkezKuzey",new Vector2(0,14),new Vector2(0,40));
        RoadLink(root,"SahilBaglantisi",new Vector2(0,-42),new Vector2(0,-59),3.4f);
        RoadLink(root,"LimanYolu",new Vector2(60,-42),new Vector2(66,-45),3.4f);
        Transform markings=Group(root,"AsfaltYayaGecitleri");
        foreach(float x in new[]{-60f,-30f,0f,30f,60f})foreach(float z in new[]{-42f,-14f,14f,40f})
        {
            AddJunctionMarkings(markings,x,z);
        }
        CombineVisuals(markings,"AsfaltGecitIsaretleri");markings.gameObject.SetActive(false);
        root.gameObject.AddComponent<RoadNetworkState>().asphaltMarkings=markings.gameObject;
        for(int i=0;i<4;i++)Cube(decoration,"KuzeyMahalleGiris",new Vector3(new[]{-58f,-44f,44f,58f}[i],.10f,45.3f),new Vector3(1.4f,.1f,7.0f),Sand);
        CombineVisuals(decoration,"YolFenerleriVeAgaclar");
    }
    static void AddJunctionMarkings(Transform markings,float x,float z)
    {
        Crosswalk(markings,x-4.7f,z,true);Crosswalk(markings,x+4.7f,z,true);
        bool south=z>-42&&!(x==0&&z==14)||(x==0&&z==-42);
        bool north=z<40&&!(x==0&&z==-14);
        if(south)Crosswalk(markings,x,z-4.7f,false);
        else Cube(markings,"T_Kavsak_GuneySiniri",new Vector3(x,.324f,z-3.1f),new Vector3(5.8f,.02f,.22f),Cream);
        if(north)Crosswalk(markings,x,z+4.7f,false);
        else Cube(markings,"T_Kavsak_KuzeySiniri",new Vector3(x,.324f,z+3.1f),new Vector3(5.8f,.02f,.22f),Cream);
    }
    public static void RefreshJunctionMarkings(GameObject island)
    {
        Transform roads=island.transform.Find("Yollar"),markings=roads.Find("AsfaltYayaGecitleri");
        for(int i=markings.childCount-1;i>=0;i--)Object.DestroyImmediate(markings.GetChild(i).gameObject);
        foreach(float x in new[]{-60f,-30f,0f,30f,60f})foreach(float z in new[]{-42f,-14f,14f,40f})AddJunctionMarkings(markings,x,z);
        CombineVisuals(markings,"AsfaltGecitIsaretleri");
    }
    static void Mountain(Transform parent,string name,Vector3 center,float rx,float rz,float height,int seed)
    {
        Transform mountain=Group(parent,name,center);var v=new List<Vector3>();var t=new List<int>();const int n=13;
        for(int band=0;band<3;band++)
        {
            v.Clear();t.Clear();
            float lower=1-band*.28f,upper=1-(band+1)*.28f;
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                float jag=1+.13f*Mathf.Sin(i*3.17f+seed),jag2=1+.13f*Mathf.Sin((i+1)*3.17f+seed);
                Vector3 p=new Vector3(Mathf.Cos(a)*rx*lower*jag,height*band*.29f,Mathf.Sin(a)*rz*lower*jag);
                Vector3 q=new Vector3(Mathf.Cos(b)*rx*lower*jag2,height*band*.29f,Mathf.Sin(b)*rz*lower*jag2);
                Vector3 r=new Vector3(Mathf.Cos(a)*rx*upper*jag,height*(band+1)*.29f+Mathf.Sin(i*4.2f+seed)*.9f,Mathf.Sin(a)*rz*upper*jag);
                Vector3 s=new Vector3(Mathf.Cos(b)*rx*upper*jag2,height*(band+1)*.29f+Mathf.Sin((i+1)*4.2f+seed)*.9f,Mathf.Sin(b)*rz*upper*jag2);
                Triangle(v,t,p,s,q);Triangle(v,t,p,r,s);
                if(band==2)Triangle(v,t,r,new Vector3(rx*.06f,height,0),s);
            }
            MeshObject(mountain,name+"_KayaKatmani_"+band,v,t,band%2==0?new Color32(139,145,120,255):new Color32(108,125,116,255));
            v.Clear();t.Clear();
            float f=1-(band+1)*.28f;
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                Vector3 p=new Vector3(Mathf.Cos(a)*rx*f,height*(band+1)*.29f+.05f,Mathf.Sin(a)*rz*f);
                Vector3 q=new Vector3(Mathf.Cos(b)*rx*f,height*(band+1)*.29f+.05f,Mathf.Sin(b)*rz*f);
                Triangle(v,t,new Vector3(0,height*(band+1)*.29f+.05f,0),q,p);
            }
            MeshObject(mountain,name+"_YesilTeras_"+band,v,t,band==2?Sage:GrassDark);
        }
        for(int tier=0;tier<3;tier++)
        {
            int trees=tier==0?11:tier==1?7:3;
            float radius=tier==0?.735f:tier==1?.455f:.17f;
            for(int i=0;i<trees;i++)
            {
                float angle=(i/(float)trees)*Mathf.PI*2+seed*.27f;
                Tree(mountain,"DagYamaciCamligi",new Vector3(Mathf.Cos(angle)*rx*radius,height*(tier+1)*.29f+.2f,Mathf.Sin(angle)*rz*radius),2.6f+(i%3)*.4f,i%4==0?1:0);
            }
        }
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI*2/12;
            Shape(mountain,"DagEtegiKayasi",RockMesh(),new Vector3(Mathf.Cos(a)*rx*.9f,1.2f,Mathf.Sin(a)*rz*.9f),new Vector3(2.3f,4+i%3,2),i%2==0?Stone:new Color32(113,127,114,255));
        }
    }
    static void BuildNaturalAreas(Transform parent)
    {
        Transform hill=Group(parent,"Tepe");
        Mountain(hill,"AnaDag",new Vector3(-7,0,57),20,13,19,2);
        Mountain(hill,"DoguYamac",new Vector3(12,0,60),12,11,13,8);
        Mountain(hill,"BatiKayaligi",new Vector3(-26,0,56),10,9,10,5);
        for(int i=0;i<8;i++)Tree(hill,"DagTerasiAgaci",new Vector3(-15+i*3.0f,6.0f,56+Mathf.Sin(i)*3),3.2f,0);
        CombineVisuals(hill,"ArkaKayalikDaglar");
        Transform forest=Group(parent,"Orman");
        for(int i=0;i<160;i++)
        {
            float x=-72+(i%20)*7.4f+Mathf.Sin(i*2.3f)*1.8f,z=54+(i/20)*2.1f+Mathf.Cos(i*1.73f)*1.2f;
            if(Mathf.Pow(x/CoastX,2)+Mathf.Pow(z/CoastZ,2)>.84f)continue;
            if(x>-36&&x<30)continue;
            if(z<55&&Mathf.Abs(x)>34&&Mathf.Abs(x)<65)continue;
            Tree(forest,"OrmanAgaci_"+i,new Vector3(x,0,z),4.1f+i%5*.65f,i%3);
        }
        for(int i=0;i<18;i++)
        {
            float x=-78+(i%3)*6.5f,z=-30+(i/3)*12;
            if(Mathf.Pow(x/CoastX,2)+Mathf.Pow(z/CoastZ,2)>.84f)continue;
            if(Mathf.Abs(x+74)<4)continue;
            Tree(forest,"KiyiAgaci",new Vector3(x,.1f,z),4.1f,i%3);
        }
        for(int side=-1;side<=1;side+=2)for(int i=0;i<13;i++)
            Tree(forest,"KiyiKorulugu",new Vector3(side*82,.1f,-24+i*4),3.6f+(i%3)*.5f,i%3);
        for(int i=0;i<20;i++)
        {
            float x=-25+i*2.7f,z=-48+Mathf.Sin(i*1.6f)*1.1f;
            if(Mathf.Abs(x)<3)continue;
            FlowerBed(forest,new Vector3(x,.1f,z),1.0f,1.3f,i);
        }
        CombineVisuals(forest,"OrmanYesilDoku");
        Transform stream=Group(parent,"SelaleVeDere");Color water=new Color32(65,163,170,255);
        Cube(stream,"Selale",new Vector3(20,5.8f,54.7f),new Vector3(3.7f,11.4f,.3f),new Color32(174,220,207,255));
        for(int i=0;i<5;i++)Cube(stream,"SelaleAkinti",new Vector3(18.5f+i*.75f,5.8f,54.45f),new Vector3(.18f,11.2f,.06f),new Color32(225,241,224,255));
        Shape(stream,"SelaleHavuzu",RockMesh(),new Vector3(20,.08f,51),new Vector3(7,.08f,4.5f),water);
        Vector3[] p={new Vector3(20,.11f,51),new Vector3(27,.11f,54),new Vector3(34,.11f,59),new Vector3(44,.11f,61),new Vector3(51,.11f,63)};
        for(int i=0;i<p.Length-1;i++)
        {
            Vector3 d=p[i+1]-p[i];var river=Cube(stream,"DereYatagi",(p[i+1]+p[i])*.5f,new Vector3(3.0f,.09f,d.magnitude+1.0f),water);
            river.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,d.normalized);
            for(int k=0;k<3;k++)Shape(stream,"DereKayasi",RockMesh(),Vector3.Lerp(p[i],p[i+1],k/2f)+new Vector3(1.8f,.2f,1.2f),new Vector3(.9f,.7f,1),Stone);
        }
        Cube(stream,"KopruDoseme",new Vector3(29,.6f,55),new Vector3(6,.30f,2.3f),Timber).transform.localRotation=Quaternion.Euler(0,-35,0);
        CombineVisuals(stream,"SelaleDereKopru");
    }
}
