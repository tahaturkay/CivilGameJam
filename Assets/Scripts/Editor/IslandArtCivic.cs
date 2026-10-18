using UnityEngine;

public static partial class IslandLayoutInstaller
{
    // Stable palettes and details repeat across levels so each home keeps its identity when upgraded.
    public static void DecorateHouse(Transform parent, int level, int variation)
    {
        Color32[] facades = {
            new Color32(223,191,145,255), new Color32(178,198,175,255), new Color32(156,185,194,255),
            new Color32(225,211,179,255), new Color32(209,163,138,255), new Color32(186,178,199,255),
            new Color32(207,205,162,255), new Color32(181,199,197,255) };
        Color32[] shutters = { new Color32(67,107,103,255), new Color32(101,126,148,255),
            new Color32(124,80,59,255), new Color32(103,117,73,255) };
        Color32[] roofs = { new Color32(174,82,56,255), new Color32(126,78,63,255),
            new Color32(183,115,73,255), new Color32(88,104,110,255) };
        int seed = Mathf.Abs(variation);
        Color wall = facades[(seed * 5 + seed / 7) % facades.Length];
        Color trim = shutters[(seed / 2) % shutters.Length];
        foreach (MeshRenderer renderer in parent.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name == "SivaliCephe" || renderer.name == "CatiPenceresi") renderer.sharedMaterial = Mat(wall);
            if (renderer.name == "KiremitCati") renderer.sharedMaterial = Mat(roofs[(seed + seed / 3) % roofs.Length]);
        }
        float w = 5.4f + level * .85f, d = 5.2f + level * .65f, h = (level + 1) * 2.45f;
        int style = seed % 4;
        if (style == 0 || style == 2)
        {
            // Painted timber shutters make a small Mediterranean frontage, with restrained shared colours.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int edge = -1; edge <= 1; edge += 2)
                    Cube(parent,"AhşapPanjur",new Vector3(side*w*.29f+edge*.64f,1.65f,-d*.5f-.18f),new Vector3(.28f,1.18f,.12f),trim);
                Cube(parent,"PanjurKusagi",new Vector3(side*w*.29f,2.36f,-d*.5f-.12f),new Vector3(1.55f,.13f,.12f),trim);
            }
        }
        if (style == 1)
        {
            Cube(parent,"GirisTentesi",new Vector3(0,2.44f,-d*.5f-.64f),new Vector3(2.05f,.16f,1.32f),trim);
            for (int side=-1; side<=1; side+=2)
                Cube(parent,"TenteKonsolu",new Vector3(side*.84f,2.17f,-d*.5f-.43f),new Vector3(.12f,.50f,.16f),Timber);
            Cube(parent,"CepheAltKaplama",new Vector3(0,.65f,-d*.5f-.06f),new Vector3(w-.5f,.40f,.10f),Stone);
        }
        if (style == 2)
        {
            for (int i=0;i<3;i++) Cube(parent,"OnCepheTasSirasi",new Vector3(-w*.39f,.88f+i*.32f,-d*.5f-.08f),new Vector3(.42f,.18f,.14f),Cream);
        }
        if (style == 3)
        {
            for (int side=-1;side<=1;side+=2)
                Cube(parent,"AhşapCepheDikmesi",new Vector3(side*w*.43f,h*.5f+.4f,-d*.5f-.11f),new Vector3(.16f,h,.15f),trim);
            Cube(parent,"AhşapCepheKusagi",new Vector3(0,2.48f,-d*.5f-.10f),new Vector3(w,.16f,.14f),trim);
            Cube(parent,"KapiUstuSacak",new Vector3(0,2.32f,-d*.5f-.37f),new Vector3(1.6f,.14f,.65f),trim);
        }
        // A slim door plate and stone lintel read well without introducing a text renderer per house.
        Cube(parent,"KapiNumaraPlakasi",new Vector3(.74f,1.93f,-d*.5f-.16f),new Vector3(.20f,.24f,.045f),trim);
        Cube(parent,"KapiLentosu",new Vector3(0,2.25f,-d*.5f-.15f),new Vector3(1.20f,.14f,.20f),Cream);
    }

    static void AddCampus(Transform parent,string name,BinaTuru type,Vector3 pos)
    {
        Transform root=Group(parent,name,pos); root.gameObject.AddComponent<BoxCollider>();
        var visuals=root.gameObject.AddComponent<BuildingVisuals>(); visuals.binaTuru=type;visuals.levelModelleri=new GameObject[3];
        for(int level=0;level<3;level++)
        {
            Transform model=Group(root,"Level_"+(level+1));
            float width=type==BinaTuru.Altyapi?13:22,depth=type==BinaTuru.Altyapi?12:19;
            Cube(model,"KampusDosemesi",new Vector3(0,.08f,0),new Vector3(width,.16f,depth),Cream);
            if(type==BinaTuru.Akut)BuildRescueCampus(model,level);
            else if(type==BinaTuru.Itfaiye)
            {
                BuildFireStationBuilding(model,level);
                Watchtower(model,new Vector3(7,.15f,5),5+level*2.3f,true);
                Label(model,"ItfaiyeTabelasi","İTFAİYE",new Vector3(0,3.1f,-6.18f),.22f,Cream);
                GameObject truck=FitAsset(model,"ItfaiyeAraci","Assets/Assets/Salih/FireEngine/FireEngine.fbx",new Vector3(-5,.25f,-7.8f),2.1f,4.8f,2.8f);
                if(truck!=null)truck.transform.localRotation=Quaternion.Euler(0,90,0);
                for(int i=0;i<3;i++)
                {
                    Cube(model,"GarajOnuCizgi",new Vector3(-5+i*4,.20f,-8),new Vector3(.15f,.02f,2.4f),Roof);
                    Cube(model,"HortumKutusu",new Vector3(9,.9f,-3+i*2.0f),new Vector3(.65f,1.4f,.7f),Roof);
                }
            }
            else if(type==BinaTuru.Okul)
            {
                BuildSchoolBuilding(model,level);
                Cube(model,"SporSahasi",new Vector3(0,.18f,-5.5f),new Vector3(12,.08f,5.4f),new Color32(163,100,71,255));
                for(int i=-1;i<=1;i+=2)
                {
                    Cube(model,"SahaYanCizgi",new Vector3(i*5.6f,.23f,-5.5f),new Vector3(.10f,.025f,4.8f),Cream);
                    Cube(model,"PotaDiregi",new Vector3(i*5.25f,1.4f,-5.5f),new Vector3(.12f,2.8f,.12f),Stone);
                    Cube(model,"Pota",new Vector3(i*5.25f,2.8f,-5.5f),new Vector3(.16f,1.0f,1.15f),Cream);
                }
                Cube(model,"SahaOrtaCizgi",new Vector3(0,.23f,-5.5f),new Vector3(.1f,.025f,4.8f),Cream);
                Flag(model,new Vector3(-9,.2f,-3),5.5f);
                Label(model,"OkulTabelasi","ODTÜ",new Vector3(0,2.55f,-.86f),.25f,RoofDark);
                for(int i=0;i<3;i++)Tree(model,"OkulAgaci",new Vector3(8.7f,.17f,-6+i*3),2.9f,1);
            }
            else BuildInfrastructure(model,level);
            if(type!=BinaTuru.Altyapi)
            {
                for(int i=0;i<4;i++)Cube(model,"KampusBorduru",new Vector3(-8+i*5.3f,.22f,9.1f),new Vector3(4.7f,.4f,.35f),Stone);
                FlowerBed(model,new Vector3(-9.5f,.17f,4),1.1f,5,level);
            }
            CombineVisuals(model,name+"_L"+(level+1));visuals.levelModelleri[level]=model.gameObject;model.gameObject.SetActive(level==0);
        }
        SetInitialCollider(root.gameObject);
    }
    static void BuildSchoolBuilding(Transform parent,int level)
    {
        Transform school=Group(parent,"OkulDerslikKanatlari");
        float w=14.0f+level*1.2f,d=7.5f,h=3.0f+level*2.8f,z=3.0f;
        Color plaster=new Color32(221,191,128,255);
        Cube(school,"OkulTasTemel",new Vector3(0,.3f,z),new Vector3(w+.4f,.6f,d+.4f),Stone);
        Cube(school,"DerslikKatlari",new Vector3(0,h*.5f+.4f,z),new Vector3(w,h,d),plaster);
        for(int floor=0;floor<=level;floor++)
        {
            float y=.4f+floor*2.8f;
            Cube(school,"OkulKatSilmesi",new Vector3(0,y+.1f,z),new Vector3(w+.30f,.2f,d+.3f),Cream);
            for(int i=-2;i<=2;i++)
            {
                if(floor>0||i!=0)Window(school,new Vector3(i*w*.18f,y+1.45f,z-d*.5f-.04f),1.45f,1.5f);
                Window(school,new Vector3(i*w*.18f,y+1.45f,z+d*.5f+.04f),1.45f,1.5f,180);
            }
            for(int side=-1;side<=1;side+=2)for(int i=-1;i<=1;i+=2)
                Window(school,new Vector3(side*(w*.5f+.04f),y+1.45f,z+i*d*.27f),1.25f,1.5f,side<0?90:-90);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Cube(school,"OnKosePilaster",new Vector3(side*(w*.5f-.2f),h*.5f+.4f,z-d*.5f-.11f),new Vector3(.45f,h,.28f),Cream);
            Cube(school,"ArkaKosePilaster",new Vector3(side*(w*.5f-.2f),h*.5f+.4f,z+d*.5f+.11f),new Vector3(.45f,h,.28f),Cream);
        }
        Cube(school,"CiftKanatliAnaKapi",new Vector3(0,1.35f,z-d*.5f-.12f),new Vector3(1.8f,2.1f,.18f),Timber);
        Cube(school,"KapıCamı",new Vector3(0,1.60f,z-d*.5f-.23f),new Vector3(1.3f,.9f,.035f),Glass);
        for(int step=0;step<3;step++)Cube(school,"OkulGirisBasamagi",new Vector3(0,.12f+step*.13f,-2.1f+step*.3f),new Vector3(3.0f,.24f+step*.26f,1.0f-step*.2f),Cream);
        Shape(school,"OkulKiremitCatisi",RoofMesh(),new Vector3(0,h+.5f,z),new Vector3(w+1.1f,2.1f+level*.15f,d+1.1f),Roof);
        Cube(school,"OkulMahyasi",new Vector3(0,h+2.65f+level*.15f,z),new Vector3(.25f,.20f,d+1.3f),RoofDark);
        Cube(school,"OnSacak",new Vector3(0,h+.5f,z-d*.5f-.55f),new Vector3(w+1.2f,.18f,.20f),Cream);
        // A clock pediment and paired entrance columns make the school recognisable from every level.
        Cube(school,"SaatAlinligi",new Vector3(0,h+.85f,z-d*.5f-.30f),new Vector3(2.3f,1.3f,.5f),Cream);
        Cube(school,"SaatKadrani",new Vector3(0,h+.92f,z-d*.5f-.58f),new Vector3(.82f,.82f,.04f),new Color32(244,235,199,255));
        Beam(school,"SaatAkrep",new Vector3(0,h+.92f,z-d*.5f-.62f),new Vector3(-.22f,h+1.09f,z-d*.5f-.62f),.045f,RoofDark);
        Beam(school,"SaatYelkovan",new Vector3(0,h+.92f,z-d*.5f-.63f),new Vector3(0,h+1.25f,z-d*.5f-.63f),.035f,RoofDark);
        Shape(school,"SaatCatisi",RoofMesh(),new Vector3(0,h+1.51f,z-d*.5f-.15f),new Vector3(2.8f,.85f,1.3f),RoofDark);
    }
    static void BuildFireStationBuilding(Transform parent,int level)
    {
        Transform station=Group(parent,"ItfaiyeGarajVeIdareBinasi");
        float w=14.5f+level*.9f,d=9.0f,z=-1.5f,h=3.5f+level*2.35f;
        Color brick=new Color32(185,115,78,255);
        Cube(station,"GarajTemeli",new Vector3(0,.23f,z),new Vector3(w+.4f,.46f,d+.4f),Stone);
        Cube(station,"ItfaiyeAnaGovde",new Vector3(0,h*.5f+.35f,z),new Vector3(w,h,d),brick);
        for(int i=0;i<4;i++)
        {
            float x=(i-1.5f)*w*.235f;
            Cube(station,"GarajTasCercevesi",new Vector3(x,1.65f,z-d*.5f-.10f),new Vector3(w*.21f,2.85f,.26f),Cream);
            Cube(station,"KirmiziGarajKapisi",new Vector3(x,1.55f,z-d*.5f-.26f),new Vector3(w*.18f,2.45f,.10f),new Color32(148,57,43,255));
            for(int seam=0;seam<6;seam++)Cube(station,"SarmalKapiDerzi",new Vector3(x,.55f+seam*.40f,z-d*.5f-.33f),new Vector3(w*.18f,.045f,.04f),RoofDark);
            Cube(station,"GarajUstCam",new Vector3(x,2.2f,z-d*.5f-.35f),new Vector3(w*.145f,.36f,.025f),Glass);
        }
        for(int floor=0;floor<level;floor++)
        {
            float y=4.25f+floor*2.35f;
            Cube(station,"IdareKatiSilmesi",new Vector3(0,y-.6f,z),new Vector3(w+.25f,.20f,d+.25f),Cream);
            for(int i=0;i<5;i++)
            {
                Window(station,new Vector3(-w*.38f+i*w*.19f,y+.35f,z-d*.5f-.04f),1.5f,1.2f);
                Window(station,new Vector3(-w*.38f+i*w*.19f,y+.35f,z+d*.5f+.04f),1.5f,1.2f,180);
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            Cube(station,"ItfaiyeKoseTasi",new Vector3(side*(w*.5f-.12f),h*.5f+.35f,z-d*.5f-.08f),new Vector3(.35f,h,.25f),Cream);
            for(int i=-1;i<=1;i++)Window(station,new Vector3(side*(w*.5f+.04f),2.0f,z+i*2.5f),1.2f,1.1f,side<0?90:-90);
        }
        Shape(station,"ItfaiyeKiremitCatisi",RoofMesh(),new Vector3(0,h+.45f,z),new Vector3(w+1.0f,1.7f,d+1.0f),Roof);
        Cube(station,"ItfaiyeMahya",new Vector3(0,h+2.2f,z),new Vector3(.24f,.16f,d+1.2f),RoofDark);
        Cube(station,"GarajUstTabelaKusagi",new Vector3(0,3.1f,z-d*.5f-.28f),new Vector3(w*.65f,.50f,.16f),new Color32(147,49,36,255));
    }
    static void BuildRescueCampus(Transform parent,int level)
    {
        float h=3.2f+level*2.15f,w=11+level,d=7.3f;
        Cube(parent,"KurtarmaBinası",new Vector3(-1,h*.5f+.22f,-2),new Vector3(w,h,d),new Color32(214,209,183,255));
        Cube(parent,"AKUTKirmiziKusak",new Vector3(-1,1.3f,-2),new Vector3(w+.08f,.65f,d+.1f),new Color32(173,65,47,255));
        Cube(parent,"DuzCati",new Vector3(-1,h+.28f,-2),new Vector3(w+.5f,.28f,d+.5f),Stone);
        for(int floor=0;floor<=level;floor++)for(int i=0;i<4;i++)Window(parent,new Vector3(-5+i*2.7f,1.8f+floor*2.1f,-5.7f),1.3f,1.0f);
        Cube(parent,"AcilGiris",new Vector3(-1,1.0f,-5.9f),new Vector3(1.4f,1.9f,.2f),RoofDark);
        Label(parent,"AkutTabela","AKUT",new Vector3(-1,h-.35f,-5.86f),.34f,new Color32(166,48,38,255));
        Cube(parent,"HelikopterPisti",new Vector3(-3,.19f,5.3f),new Vector3(8,.07f,6.4f),new Color32(99,124,112,255));
        for(int side=-1;side<=1;side+=2)Cube(parent,"HPistDikCizgi",new Vector3(-3+side*1.1f,.235f,5.3f),new Vector3(.25f,.025f,2.8f),Cream);
        Cube(parent,"HPistYatayCizgi",new Vector3(-3,.235f,5.3f),new Vector3(2.4f,.025f,.25f),Cream);
        Watchtower(parent,new Vector3(7,.18f,5.3f),4.6f+level*2.5f,false);
        for(int i=0;i<3;i++)
        {
            Cube(parent,"KurtarmaEkipmanKutusu",new Vector3(8.3f,.6f,-5+i*2.2f),new Vector3(1.4f,.8f,1.4f),i%2==0?new Color32(208,139,49,255):Roof);
            Cube(parent,"KutuKayisi",new Vector3(8.3f,1.03f,-5+i*2.2f),new Vector3(.17f,.05f,1.45f),Timber);
        }
        Beam(parent,"Anten",new Vector3(-4,h+.3f,-1),new Vector3(-4,h+2.6f,-1),.10f,Stone);
        Beam(parent,"AntenYatay",new Vector3(-5,h+2.1f,-1),new Vector3(-3,h+2.1f,-1),.08f,Stone);
    }
    static void Watchtower(Transform parent,Vector3 pos,float h,bool fire)
    {
        Transform tower=Group(parent,"GozlemVeHaberlesmeKulesi",pos);
        Cube(tower,"KuleGovdesi",new Vector3(0,h*.5f,0),new Vector3(2.15f,h,2.15f),fire?new Color32(197,130,89,255):Cream);
        for(int floor=0;floor<h/2;floor++)Window(tower,new Vector3(0,1.1f+floor*1.8f,-1.09f),.65f,.75f);
        Cube(tower,"KuleBalkonu",new Vector3(0,h-.4f,0),new Vector3(3.3f,.25f,3.3f),Stone);
        Cube(tower,"GozelemeCamliKabin",new Vector3(0,h+.5f,0),new Vector3(2.65f,1.4f,2.65f),Glass);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Cube(tower,"KabinDiregi",new Vector3(x*1.27f,h+.5f,z*1.27f),new Vector3(.16f,1.45f,.16f),Cream);
        Shape(tower,"KuleCatisi",RoofMesh(),new Vector3(0,h+1.25f,0),new Vector3(3.5f,1.2f,3.5f),fire?Roof:RoofDark);
    }
    static void BuildInfrastructure(Transform parent,int level)
    {
        FitAsset(parent,"PompaKontrolBinasi",Civic+"Workshop.prefab",new Vector3(-1,.18f,1.2f),7.5f+level,6,3.8f+level*1.5f);
        for(int i=0;i<2+level;i++)
        {
            Cube(parent,"SuFiltresi",new Vector3(-4+i*2.3f,1.0f,-3.8f),new Vector3(1.5f,1.6f,1.8f),new Color32(110,154,161,255));
            Cube(parent,"FiltreBasligi",new Vector3(-4+i*2.3f,1.9f,-3.8f),new Vector3(1.65f,.16f,1.95f),Cream);
        }
        Beam(parent,"AnaSuHatti",new Vector3(-5,.5f,-5),new Vector3(5,.5f,-5),.36f,Stone);
        Label(parent,"AltyapiTabelasi","SU / ALTYAPI",new Vector3(0,1.6f,-5.3f),.15f,Cream);
    }
    static void Flag(Transform parent,Vector3 pos,float h)
    {
        Beam(parent,"BayrakDiregi",pos,pos+Vector3.up*h,.09f,Stone);
        AddOdtuFlagCloth(parent,pos+new Vector3(.75f,h-.55f,0));
    }
    static void Bench(Transform parent,Vector3 pos,float yaw=0)
    {
        Transform bench=Group(parent,"Bank",pos);bench.localRotation=Quaternion.Euler(0,yaw,0);
        for(int i=0;i<3;i++)Cube(bench,"OturmaTahtasi",new Vector3(0,.48f,-.30f+i*.3f),new Vector3(2.1f,.10f,.25f),Timber);
        for(int s=-1;s<=1;s+=2)Cube(bench,"Ayak",new Vector3(s*.75f,.25f,0),new Vector3(.13f,.5f,.75f),Stone);
        Cube(bench,"Sirtlik",new Vector3(0,.90f,.4f),new Vector3(2.1f,.62f,.10f),Timber);
    }
    static void BuildStaticPlaces(Transform parent)
    {
        Transform hall=Group(parent,"Belediye");
        Cube(hall,"BelediyeMeydani",new Vector3(0,.12f,0),new Vector3(18,.24f,20),Cream);
        FitAsset(hall,"BelediyeBinasi",Palm+"Houses/city hall.prefab",new Vector3(0,.25f,2.2f),15,12,13);
        FitAsset(hall,"MeydanHavuzu",Palm+"Environment/fountain.prefab",new Vector3(0,.25f,-7.0f),3.6f,3.6f,3.0f);
        for(int i=-1;i<=1;i+=2)
        {
            Flag(hall,new Vector3(i*6.6f,.2f,-4.5f),5.5f);
            FlowerBed(hall,new Vector3(i*7.9f,.2f,2),1.4f,11,i+1);
            Bench(hall,new Vector3(i*5,.24f,-7),i<0?90:-90);
        }
        Label(hall,"BelediyeTabela","KALKANLI MUHTARLIĞI",new Vector3(0,3.2f,-4.05f),.15f,RoofDark);
        CombineVisuals(hall,"BelediyeMeydanDetay");
        Transform park=Group(parent,"Park",new Vector3(-17,.1f,0));
        Cube(park,"ParkCim",Vector3.zero,new Vector3(14,.12f,20),GrassDark);
        Cube(park,"UzunPatika",new Vector3(0,.1f,0),new Vector3(1.8f,.08f,20),Sand);
        Cube(park,"EninePatika",new Vector3(0,.11f,0),new Vector3(14,.08f,1.7f),Sand);
        for(int i=0;i<8;i++)
        {
            Vector3 p=new Vector3(i%2==0?-4.8f:4.8f,.15f,-7.8f+(i/2)*5.2f);
            Tree(park,"ParkAgaci",p,3.8f,i%3);Bench(park,new Vector3(p.x*.5f,.15f,p.z),p.x<0?90:-90);
            FlowerBed(park,p+new Vector3(0,0,1.5f),1.4f,1.6f,i);
        }
        FitAsset(park,"ParkFiskiyesi",Palm+"Environment/fountain.prefab",new Vector3(0,.18f,0),3.2f,3.2f,2.3f);
        CombineVisuals(park,"ParkPeyzaj");
        BuildMarket(Group(parent,"TicaretVePazar",new Vector3(17,.1f,0)));
        BuildParking(Group(parent,"Otopark",new Vector3(15,.1f,-28)));
        BuildBeach(Group(parent,"Kumsal"));
        BuildFair(Group(parent,"Lunapark",new Vector3(29,.1f,-54)));
        BuildHarbor(Group(parent,"Liman"));
        BuildNaturalAreas(parent);
    }
    static void BuildMarket(Transform root)
    {
        Cube(root,"PazarMeydani",Vector3.zero,new Vector3(14,.16f,20),Sand);
        for(int row=0;row<2;row++)for(int i=0;i<3;i++)
        {
            Transform stall=Group(root,"PazarTezgahi",new Vector3(-4.5f+i*4.5f,.1f,-6+row*11));
            Cube(stall,"Tezgah",new Vector3(0,.7f,0),new Vector3(3,1.0f,2.2f),Timber);
            for(int x=-1;x<=1;x+=2)Cube(stall,"TenteDiregi",new Vector3(x*1.45f,1.7f,0),new Vector3(.12f,2.6f,.12f),Timber);
            for(int strip=0;strip<6;strip++)Cube(stall,"CizgiliTente",new Vector3(-1.5f+strip*.6f,3.0f,0),new Vector3(.6f,.15f,3.1f),strip%2==0?Cream:row==0?Roof:new Color32(79,138,134,255));
            for(int k=0;k<5;k++)Shape(stall,"MeyveKasasi",RockMesh(),new Vector3(-1.1f+k*.5f,1.35f,0),new Vector3(.35f,.3f,.5f),k%2==0?new Color32(214,165,64,255):new Color32(172,75,50,255));
        }
        for(int i=0;i<4;i++)Bench(root,new Vector3(-5+i*3.3f,.1f,0));
        CombineVisuals(root,"PazarTezgahlari");
    }
    static void BuildParking(Transform root)
    {
        Cube(root,"OtoparkAsfalti",Vector3.zero,new Vector3(23,.2f,20),Road);
        for(int row=0;row<2;row++)for(int i=0;i<8;i++)
        {
            float x=-9.5f+i*2.7f,z=row==0?-5.9f:5.9f;
            Cube(root,"ParkCizgisi",new Vector3(x,.12f,z),new Vector3(.09f,.025f,5),Cream);
            if((i+row)%3!=0)
            {
                var car=FitAsset(root,"ParkAraci",Vehicles+"Vehicle_Car_color0"+(i%3+1)+".prefab",new Vector3(x+1.2f,.15f,z),1.8f,3.6f,1.9f);
                if(car!=null&&row==1)car.transform.localRotation=Quaternion.Euler(0,180,0);
            }
        }
        for(int i=-1;i<=1;i+=2)
        {
            Tree(root,"OtoparkGolgelik",new Vector3(i*10.2f,.14f,0),3.7f,1);
            Cube(root,"OtoparkBariyer",new Vector3(i*9,.75f,-9.1f),new Vector3(3.8f,.15f,.15f),Roof);
        }
        Label(root,"OtoparkTabelasi","P",new Vector3(9,2.4f,-9),.4f,Cream);
        CombineVisuals(root,"OtoparkDetaylari");
    }
    static void BuildBeach(Transform root)
    {
        BuildCoastalPatch(root,"GuneyKoyu",48,59,.81f,.055f);
        for(int i=0;i<10;i++)
        {
            float x=-23+i*4.0f,z=-60-Mathf.Cos(i*.35f)*1.5f;
            Transform umbrella=Group(root,"PlajSemsiye",new Vector3(x,.12f,z));
            Beam(umbrella,"SemsiyeDiregi",Vector3.zero,Vector3.up*2,.07f,Timber);
            Shape(umbrella,"RenkliSemsiye",ConeMesh(),new Vector3(0,1.8f,0),new Vector3(1.45f,.65f,1.45f),i%2==0?new Color32(206,111,83,255):Cream);
            for(int s=-1;s<=1;s+=2)Cube(umbrella,"Sezlong",new Vector3(s*.7f,.25f,-1.5f),new Vector3(.65f,.20f,1.8f),Cream);
        }
        Cube(root,"SahilYuruyusTahtasi",new Vector3(-5,.16f,-57),new Vector3(38,.2f,1.8f),Timber);
        for(int i=0;i<20;i++)Cube(root,"TahtaAraligi",new Vector3(-23+i*1.85f,.27f,-57),new Vector3(.045f,.015f,1.8f),RoofDark);
        for(int i=0;i<5;i++)Tree(root,"SahilAgaci",new Vector3(-22+i*8,.1f,-55),3.5f,2);
        CombineVisuals(root,"KumsalSosyalAlan");
    }
    static void BuildFair(Transform root)
    {
        Cube(root,"LunaparkMeydani",Vector3.zero,new Vector3(23,.2f,17),Sand);
        FitAsset(root,"DonmeDolap",Palm+"Lunapark/ferris wheel.prefab",new Vector3(4,.18f,1),11,6.5f,16);
        for(int i=0;i<4;i++)
        {
            Transform stall=Group(root,"EglenceStandı",new Vector3(-8,.12f,-6+i*4));
            Cube(stall,"StandGovdesi",new Vector3(0,1,0),new Vector3(3,2,2.7f),i%2==0?new Color32(187,93,74,255):new Color32(78,135,146,255));
            Shape(stall,"StandCatisi",RoofMesh(),new Vector3(0,2.05f,0),new Vector3(3.8f,1.6f,3.5f),i%2==0?Cream:Roof);
            Cube(stall,"OyunPenceresi",new Vector3(0,1.25f,-1.4f),new Vector3(2.2f,1.1f,.1f),Glass);
        }
        for(int i=0;i<6;i++)Lamp(root,new Vector3(-10+i*4,.15f,-8));
        CombineVisuals(root,"LunaparkSergi");
    }
    static void BuildHarbor(Transform root)
    {
        // Boat stays independent so the ending can animate this original vessel.
        Transform infrastructure=Group(root,"LimanAltyapisi");
        BuildHarborInfrastructure(infrastructure);
        Transform boat=BuildHarborBoat(root);
        BuildHarborCinematicMarkers(root,boat);
    }

    static void BuildHarborInfrastructure(Transform root)
    {
        Vector3 a=new Vector3(66,.2f,-45),b=new Vector3(77,7.0f,-59),c=new Vector3(90,7.0f,-72);
        for(int i=0;i<30;i++)
        {
            Vector3 p=Vector3.Lerp(a,b,i/29f);Cube(root,"LimanKopruBasamagi",p,new Vector3(4,.30f,1.0f),Timber).transform.localRotation=Quaternion.Euler(0,-38,0);
            if(i%5==0)for(int s=-1;s<=1;s+=2)Beam(root,"KopruAyagi",p+new Vector3(s*1.4f,-p.y-4,0),p+new Vector3(s*1.4f,0,0),.26f,Timber);
        }
        Vector3 d=c-b;
        Cube(root,"YuksekLimanIskelesi",(b+c)*.5f,new Vector3(4,.4f,d.magnitude+2),Timber).transform.localRotation=Quaternion.FromToRotation(Vector3.forward,d.normalized);
        for(int i=0;i<8;i++)
        {
            Vector3 p=Vector3.Lerp(b,c,i/7f);
            for(int s=-1;s<=1;s+=2)Beam(root,"IskeleKazigi",new Vector3(p.x+s*1.6f,-4,p.z),new Vector3(p.x+s*1.6f,7.9f,p.z),.28f,Timber);
        }
        Beam(root,"IskeleKorkuluk",b+new Vector3(-1.7f,1,0),c+new Vector3(-1.7f,1,0),.13f,Cream);
        Vector3 lower=new Vector3(96,-2.55f,-81);
        for(int step=0;step<40;step++)
        {
            Vector3 p=Vector3.Lerp(c,lower,step/39f);
            Cube(root,"AltRıhtımMerdiveni",p,new Vector3(2.4f,.22f,.50f),Timber).transform.localRotation=Quaternion.Euler(0,-34,0);
        }
        Cube(root,"AlcakRıhtım",lower+new Vector3(0,-.08f,-2),new Vector3(11,.30f,5),Timber);
        for(int i=0;i<5;i++)Beam(root,"RıhtımBabası",lower+new Vector3(-4+i*2,-1.2f,-3.6f),lower+new Vector3(-4+i*2,.7f,-3.6f),.30f,Stone);
        // Between the two bollards: narrow gangplank is clear of the cabin and crates.
        Vector3 gangplankStart=new Vector3(99,-2.325f,-87.9f),gangplankEnd=new Vector3(99,-2.48f,-84.4f);
        Vector3 gangplankDirection=gangplankEnd-gangplankStart;
        var gangplank=Cube(root,"TekneInisTahtasi",(gangplankStart+gangplankEnd)*.5f-Vector3.up*.08f,
            new Vector3(.85f,.16f,gangplankDirection.magnitude),Timber);
        gangplank.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,gangplankDirection.normalized);
        CombineVisuals(root,"LimanIskeleAltyapisi");
    }

    static Transform BuildHarborBoat(Transform root)
    {
        Transform boat=Group(root,"BalikciTeknesi",new Vector3(96,-2.5f,-90));
        boat.localRotation=Quaternion.Euler(0,-90,0);
        Shape(boat,"TekneGovdesi",TaperMesh(),new Vector3(0,0,0),new Vector3(5,2.2f,12),new Color32(53,94,107,255)).transform.localRotation=Quaternion.Euler(180,0,0);
        Cube(boat,"TekneGuvertesi",new Vector3(0,.05f,0),new Vector3(4.6f,.25f,10.8f),Timber);
        Cube(boat,"TekneKamarasi",new Vector3(0,1.2f,1.5f),new Vector3(3.0f,2.1f,3.5f),Cream);
        Cube(boat,"KamaraPenceresi",new Vector3(0,1.65f,-.3f),new Vector3(2.5f,.7f,.08f),Glass);
        Cube(boat,"KamaraCatisi",new Vector3(0,2.35f,1.5f),new Vector3(3.6f,.2f,4),Roof);
        Beam(boat,"TekneDiregi",new Vector3(0,.1f,-2.4f),new Vector3(0,5.3f,-2.4f),.12f,Timber);
        for(int i=0;i<4;i++)Cube(boat,"BalikKasasi",new Vector3(-1.4f+i*.9f,.45f,-3.7f),new Vector3(.75f,.6f,1.0f),RoofDark);
        CombineVisuals(boat,"LimanBalikciTeknesi");
        return boat;
    }

    static Transform HarborMarker(Transform parent,string name,Vector3 point,float yaw=0)
    {
        Transform marker=Group(parent,name,point);marker.localRotation=Quaternion.Euler(0,yaw,0);return marker;
    }

    static void BuildHarborCinematicMarkers(Transform root,Transform boat)
    {
        // All these poses are relative to Liman; SabitAlanlar adds GroundY (2.4m).
        Transform markers=Group(root,"CinematicMarkers");
        HarborMarker(markers,"BoatMooring",boat.localPosition,-90);
        HarborMarker(markers,"BoatApproachOrigin",new Vector3(151,-2.5f,-90),-90);
        HarborMarker(markers,"PierDisembarkA",new Vector3(99,-2.45f,-84.1f));
        HarborMarker(markers,"PierDisembarkB",new Vector3(98.15f,-2.45f,-83.9f));
        HarborMarker(markers,"CharacterStandA",new Vector3(97.25f,-2.45f,-83.1f),180);
        HarborMarker(markers,"CharacterStandB",new Vector3(98.65f,-2.45f,-82.55f),180);
        HarborMarker(markers,"LookToSea",new Vector3(98,-.8f,-134));
        HarborMarker(markers,"LandExit",new Vector3(66,.37f,-45),-38);
        Transform path=Group(markers,"Footpath");
        HarborMarker(path,"PierStart",new Vector3(96,-2.45f,-81));
        // Ordered from the lower pier uphill; feet sit on the top of each retained stair.
        for(int step=0;step<40;step++)
        {
            Vector3 p=Vector3.Lerp(new Vector3(96,-2.55f,-81),new Vector3(90,7,-72),step/39f);
            HarborMarker(path,"Step_"+step.ToString("00"),p+Vector3.up*.13f,-34);
        }
        HarborMarker(path,"UpperPier",new Vector3(77,7.23f,-59),-38);
        HarborMarker(path,"LandExit",new Vector3(66,.37f,-45),-38);
        Transform deck=Group(boat,"DeckMarkers");
        HarborMarker(deck,"PassengerA",new Vector3(.7f,.175f,-1.6f),90);
        HarborMarker(deck,"PassengerB",new Vector3(-.6f,.175f,-1.6f),90);
        HarborMarker(deck,"DeckExit",new Vector3(2.1f,.175f,-3),90);
    }

    /// <summary>Refresh only the existing harbor subtree, preserving its root identity and all other city edits.</summary>
    public static void RefreshHarborForCinema(GameObject island)
    {
        if(island==null)throw new System.ArgumentNullException(nameof(island));
        Transform root=island.transform.Find("SabitAlanlar/Liman");
        if(root==null)throw new System.InvalidOperationException("SabitAlanlar/Liman is missing; city was not rebuilt.");
        for(int i=root.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        BuildHarbor(root);
    }
}
