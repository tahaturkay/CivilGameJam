using System.Collections.Generic;
using UnityEngine;

/// <summary>Reads damage state only. Repairs restore the exact original renderer blocks and model hierarchy.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BuildingVisuals))]
public class BuildingDamageVisuals : MonoBehaviour
{
    static readonly int BaseColorId=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
    sealed class Surface
    {
        public Renderer renderer;public int index;public bool baseColor,color;
        public Color originalColor;
        public MaterialPropertyBlock original,working;
    }
    readonly List<Surface> surfaces=new List<Surface>();
    BuildingVisuals building;
    Transform activeModel,damageWrapper;
    GameObject damageEffects;
    Bounds modelBounds,rootBounds;
    float sootFrontZ;
    bool stateKnown,shownDamage;
    AfetTuru shownType;
    float shownFlash=-1;

    void Awake(){building=GetComponent<BuildingVisuals>();}
    void OnEnable(){stateKnown=false;shownFlash=-1;}

    Transform CurrentModel()
    {
        GameObject[] models=building.levelModelleri;
        if(models!=null&&models.Length>0)
        {
            for(int i=0;i<models.Length;i++)if(models[i]!=null&&models[i].activeSelf)return models[i].transform;
            return null;
        }
        return building.olceklenecekGorsel!=null&&building.olceklenecekGorsel!=transform?building.olceklenecekGorsel:transform;
    }

    void LateUpdate()
    {
        if(building==null||GameManager.Instance==null)return;
        Transform model=CurrentModel();
        if(model!=activeModel)
        {
            RestoreMaterials();ClearEffects();RestoreCollapse();activeModel=model;CacheSurfaces();stateKnown=false;
        }
        if(activeModel==null)return;
        var gm=GameManager.Instance;
        bool damaged=gm.Hasarli(building.binaTuru);
        AfetTuru type=damaged?gm.HasarTuru(building.binaTuru):default;
        bool changed=!stateKnown||damaged!=shownDamage||(damaged&&type!=shownType);
        float flash=building.VurguBeyazligi;
        if(changed)
        {
            ClearEffects();
            bool wasCollapsed=damageWrapper!=null;
            RestoreCollapse();
            // BuildingVisuals may have measured the collapsed mesh during the repair event; restore the stable click target.
            if(wasCollapsed)
            {
                BoxCollider clickTarget=GetComponent<BoxCollider>();
                if(clickTarget!=null&&building.colliderModeleUysun){clickTarget.center=rootBounds.center;clickTarget.size=rootBounds.size+Vector3.one*.08f;}
            }
            shownDamage=damaged;shownType=type;stateKnown=true;
            if(damaged)ShowDamage(type);
        }
        if(changed||Mathf.Abs(flash-shownFlash)>.002f)
        {
            ApplyTint(damaged,type,flash);shownFlash=flash;
        }
    }

    void CacheSurfaces()
    {
        surfaces.Clear();if(activeModel==null)return;
        Renderer[] renderers=activeModel.GetComponentsInChildren<Renderer>(false);
        bool first=true;modelBounds=default;rootBounds=default;
        foreach(Renderer renderer in renderers)
        {
            if(renderer is ParticleSystemRenderer||renderer.GetComponent<TextMesh>()!=null)continue;
            Bounds local=renderer.localBounds;
            for(int i=0;i<8;i++)
            {
                Vector3 p=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 world=renderer.transform.TransformPoint(p),mp=activeModel.InverseTransformPoint(world),rp=transform.InverseTransformPoint(world);
                if(first){modelBounds=new Bounds(mp,Vector3.zero);rootBounds=new Bounds(rp,Vector3.zero);first=false;}
                else{modelBounds.Encapsulate(mp);rootBounds.Encapsulate(rp);}
            }
            Material[] materials=renderer.sharedMaterials;
            for(int index=0;index<materials.Length;index++)
            {
                Material material=materials[index];if(material==null)continue;
                bool hasBase=material.HasProperty(BaseColorId),hasColor=material.HasProperty(ColorId);
                if(!hasBase&&!hasColor)continue;
                var original=new MaterialPropertyBlock();var working=new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original,index);renderer.GetPropertyBlock(working,index);
                int id=hasBase?BaseColorId:ColorId;
                Color baseColor=original.HasProperty(id)?original.GetColor(id):material.GetColor(id);
                surfaces.Add(new Surface{renderer=renderer,index=index,baseColor=hasBase,color=hasColor,originalColor=baseColor,original=original,working=working});
            }
        }
        // Find a real facade plane rather than putting soot in front of the campus courtyard/porch bounds.
        sootFrontZ=modelBounds.min.z;bool facadeFound=false;
        foreach(MeshFilter filter in activeModel.GetComponentsInChildren<MeshFilter>(false))
        {
            Mesh mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;
            Vector3[] vertices=mesh.vertices,normals=mesh.normals;if(normals.Length!=vertices.Length)continue;
            Matrix4x4 matrix=activeModel.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 normal=matrix.MultiplyVector(normals[i]).normalized;if(normal.z>-.75f)continue;
                Vector3 point=matrix.MultiplyPoint3x4(vertices[i]);
                if(point.y<modelBounds.min.y+modelBounds.size.y*.20f||point.y>modelBounds.min.y+modelBounds.size.y*.70f)continue;
                if(Mathf.Abs(point.x-modelBounds.center.x)>modelBounds.size.x*.35f)continue;
                if(!facadeFound||point.z<sootFrontZ){sootFrontZ=point.z;facadeFound=true;}
            }
        }
    }

    void ApplyTint(bool damaged,AfetTuru type,float flash)
    {
        Color multiplier=Color.white;
        if(damaged)
        {
            switch(type)
            {
                case AfetTuru.Yangin:multiplier=new Color(.70f,.67f,.62f,1);break;
                case AfetTuru.Deprem:multiplier=new Color(.82f,.79f,.73f,1);break;
                case AfetTuru.Sel:case AfetTuru.Tsunami:multiplier=new Color(.79f,.81f,.73f,1);break;
            }
        }
        for(int i=0;i<surfaces.Count;i++)
        {
            Surface surface=surfaces[i];if(surface.renderer==null)continue;
            if(!damaged&&flash<=.001f){surface.renderer.SetPropertyBlock(surface.original.isEmpty?null:surface.original,surface.index);continue;}
            Color tint=surface.originalColor*multiplier;
            tint=Color.Lerp(tint,Color.white,Mathf.Clamp01(flash));tint.a=surface.originalColor.a;
            if(surface.baseColor)surface.working.SetColor(BaseColorId,tint);
            if(surface.color)surface.working.SetColor(ColorId,tint);
            surface.renderer.SetPropertyBlock(surface.working,surface.index);
        }
    }

    void ShowDamage(AfetTuru type)
    {
        bool buildingCanCollapse=building.binaTuru!=BinaTuru.DenizSeti&&building.binaTuru!=BinaTuru.Altyapi;
        if(!buildingCanCollapse||(type!=AfetTuru.Yangin&&type!=AfetTuru.Deprem))return;
        damageEffects=new GameObject("KaliciHasarEfektleri");damageEffects.transform.SetParent(transform,false);
        float size=Mathf.Clamp(Mathf.Min(modelBounds.size.x,modelBounds.size.z)*.16f,.55f,1.6f);
        if(type==AfetTuru.Yangin&&buildingCanCollapse)
        {
            Vector3 chimney=activeModel.TransformPoint(new Vector3(modelBounds.center.x,modelBounds.max.y+.1f,modelBounds.center.z));
            IslandEffectsFactory.CreateSmoke(damageEffects.transform,chimney,size);
            Bounds sootBounds=modelBounds;sootBounds.SetMinMax(new Vector3(modelBounds.min.x,modelBounds.min.y,sootFrontZ),modelBounds.max);
            GameObject soot=IslandEffectsFactory.CreateSoot(activeModel,sootBounds);soot.transform.SetParent(damageEffects.transform,true);
        }
        else if(type==AfetTuru.Deprem&&buildingCanCollapse)
        {
            // The wrapper belongs to damage only. The original level root remains free for upgrade/repair punches.
            damageWrapper=new GameObject("DepremHasarKatmani").transform;damageWrapper.SetParent(activeModel,false);
            for(int i=activeModel.childCount-1;i>=0;i--)
            {
                Transform child=activeModel.GetChild(i);
                if(child==damageWrapper||child==damageEffects.transform)continue;
                child.SetParent(damageWrapper,false);
            }
            damageWrapper.localPosition=Vector3.down*Mathf.Clamp(modelBounds.size.y*.10f,.3f,1.2f);
            damageWrapper.localRotation=Quaternion.Euler(-3.5f,0,(GetEntityId().GetHashCode()&1)==0?5.5f:-5.5f);
            damageWrapper.localScale=new Vector3(1,.76f,1);
            IslandEffectsFactory.CreateRubble(damageEffects.transform,rootBounds);
            IslandEffectsFactory.CreateDust(damageEffects.transform,transform.TransformPoint(new Vector3(rootBounds.center.x,rootBounds.min.y+.2f,rootBounds.center.z)),size*1.4f);
        }
    }

    void RestoreMaterials()
    {
        for(int i=0;i<surfaces.Count;i++)
        {
            Surface surface=surfaces[i];if(surface.renderer!=null)surface.renderer.SetPropertyBlock(surface.original.isEmpty?null:surface.original,surface.index);
        }
    }
    void RestoreCollapse()
    {
        if(damageWrapper==null)return;
        damageWrapper.localPosition=Vector3.zero;damageWrapper.localRotation=Quaternion.identity;damageWrapper.localScale=Vector3.one;
        while(damageWrapper.childCount>0)damageWrapper.GetChild(0).SetParent(activeModel,false);
        Destroy(damageWrapper.gameObject);damageWrapper=null;
    }
    void ClearEffects()
    {
        if(damageEffects!=null){damageEffects.SetActive(false);Destroy(damageEffects);damageEffects=null;}
    }
    void OnDisable(){RestoreMaterials();ClearEffects();RestoreCollapse();stateKnown=false;shownFlash=-1;}
}
