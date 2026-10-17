using UnityEngine;

public static partial class IslandLayoutInstaller
{
    static void BuildWall(Transform parent)
    {
        for(int i=0;i<CoastCount;i++)
        {
            Vector3 a=Coast(i,1,.02f),b=Coast(i+1,1,.02f),delta=b-a;
            Transform segment=Group(parent,"DenizSeti_"+(i+1).ToString("00"),(a+b)*.5f);
            segment.localRotation=Quaternion.FromToRotation(Vector3.right,delta.normalized);
            segment.gameObject.AddComponent<BoxCollider>();
            var visuals=segment.gameObject.AddComponent<BuildingVisuals>();visuals.binaTuru=BinaTuru.DenizSeti;visuals.levelModelleri=new GameObject[3];
            for(int level=0;level<3;level++)
            {
                Transform model=Group(segment,"Level_"+(level+1));float length=delta.magnitude,width=1.5f+level*.70f,h=2.1f+level*1.6f;
                Color wall=level==0?new Color32(154,158,140,255):level==1?new Color32(142,151,139,255):new Color32(128,143,137,255);
                Shape(model,"EgimliTasTahkimat",TaperMesh(),new Vector3(0,-1.0f,0),new Vector3(length+5,1.75f,width*2.6f),new Color32(112,128,124,255));
                Cube(model,"KesintisizSetDuvari",new Vector3(0,h*.5f+.35f,0),new Vector3(length+width*.75f,h,width),wall);
                // A recessed central ashlar panel and offset joints give the fortification a readable scale.
                Cube(model,"DisCepheGommePanel",new Vector3(0,h*.50f+.4f,-width*.5f-.025f),new Vector3(length*.74f,h*.60f,.07f),new Color32(119,134,126,255));
                int courses=2+level*2;
                for(int row=0;row<courses;row++)
                {
                    float y=.55f+(row+1)*(h-.25f)/(courses+1);
                    Cube(model,"YatayTasDerzi",new Vector3(0,y,-width*.5f-.075f),new Vector3(length+.25f,.045f,.035f),new Color32(96,115,111,255));
                    for(int joint=0;joint<3;joint++)
                    {
                        float x=-length*.34f+joint*length*.32f+(row%2)*length*.13f;
                        Cube(model,"DuseyTasDerzi",new Vector3(x,y-(h-.25f)/(courses+1)*.5f,-width*.5f-.08f),new Vector3(.045f,(h-.25f)/(courses+1),.035f),new Color32(96,115,111,255));
                    }
                }
                Cube(model,"UstYuruyusYolu",new Vector3(0,h+.45f,0),new Vector3(length+width*.8f,.25f,width+.6f),Cream);
                Cube(model,"DenizTarafiKorkuluk",new Vector3(0,h+.9f,-width*.5f-.12f),new Vector3(length+width*.7f,.7f,.24f),Stone);
                if(level>0)Cube(model,"KaraTarafiKorkuluk",new Vector3(0,h+.8f,width*.5f+.12f),new Vector3(length+width*.7f,.5f,.20f),Stone);
                for(int s=-1;s<=1;s+=2)
                {
                    Shape(model,"EgimliPayanda",TaperMesh(),new Vector3(s*length*.36f,-.1f,.25f),new Vector3(.85f+level*.15f,h+.55f,width+1.8f),Stone);
                    Cube(model,"PayandaSapka",new Vector3(s*length*.36f,h+.5f,.25f),new Vector3(.85f+level*.15f,.16f,width+1.0f),Cream);
                }
                if(i%6==0)
                {
                    Transform bastion=Group(model,"KoseBurcu",new Vector3(-length*.5f,0,0));
                    float towerW=3.0f+level*.6f,towerH=h+1.0f;
                    Shape(bastion,"BurcTemeli",TaperMesh(),new Vector3(0,-1.0f,0),new Vector3(towerW*1.45f,2.0f,towerW*1.45f),Stone);
                    Cube(bastion,"BurcGovdesi",new Vector3(0,towerH*.5f,0),new Vector3(towerW,towerH,towerW),wall);
                    Cube(bastion,"BurcKemerKusagi",new Vector3(0,towerH-.8f,0),new Vector3(towerW+.3f,.22f,towerW+.3f),Cream);
                    Cube(bastion,"BurcUstPlatform",new Vector3(0,towerH+.10f,0),new Vector3(towerW+.6f,.25f,towerW+.6f),Cream);
                    for(int side=-1;side<=1;side+=2)for(int k=-1;k<=1;k++)
                    {
                        Cube(bastion,"Merlon",new Vector3(k*towerW*.38f,towerH+.6f,side*towerW*.48f),new Vector3(.65f,.9f,.55f),Stone);
                        if(k!=0)Cube(bastion,"Merlon",new Vector3(side*towerW*.48f,towerH+.6f,k*towerW*.38f),new Vector3(.55f,.9f,.65f),Stone);
                    }
                    if(i%18==0)FortressBeacon(bastion,towerH+.25f,level);
                    else
                    {
                        Cube(bastion,"OkcuMazgali",new Vector3(0,towerH*.55f,-towerW*.5f-.02f),new Vector3(.28f,1.0f,.05f),RoofDark);
                        Cube(bastion,"OkcuMazgaliYatay",new Vector3(0,towerH*.55f,-towerW*.5f-.04f),new Vector3(.7f,.22f,.05f),RoofDark);
                    }
                }
                CombineVisuals(model,"Tahkimat_"+i.ToString("00")+"_L"+(level+1));
                visuals.levelModelleri[level]=model.gameObject;model.gameObject.SetActive(level==0);
            }
            SetInitialCollider(segment.gameObject);
        }
    }
    static void FortressBeacon(Transform parent,float baseY,int level)
    {
        float h=2.3f+level*.5f;
        Cube(parent,"DenizFeneriGovdesi",new Vector3(0,baseY+h*.5f,0),new Vector3(1.7f,h,1.7f),Cream);
        Cube(parent,"FenerKirmiziKusagi",new Vector3(0,baseY+h*.55f,0),new Vector3(1.78f,.45f,1.78f),Roof);
        Cube(parent,"FenerBalkonu",new Vector3(0,baseY+h,0),new Vector3(2.5f,.20f,2.5f),Stone);
        Cube(parent,"FenerCamOdasi",new Vector3(0,baseY+h+.65f,0),new Vector3(1.9f,1.1f,1.9f),Glass);
        Cube(parent,"FenerIsigi",new Vector3(0,baseY+h+.65f,-.97f),new Vector3(1.3f,.65f,.03f),new Color32(244,216,144,255));
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Cube(parent,"FenerKabinDiregi",new Vector3(x*.88f,baseY+h+.65f,z*.88f),new Vector3(.14f,1.2f,.14f),Cream);
        Shape(parent,"FenerKubbesi",ConeMesh(),new Vector3(0,baseY+h+1.25f,0),new Vector3(1.6f,1.2f,1.6f),Roof);
        Beam(parent,"Paratoner",new Vector3(0,baseY+h+2.3f,0),new Vector3(0,baseY+h+3.1f,0),.08f,Stone);
    }
}
