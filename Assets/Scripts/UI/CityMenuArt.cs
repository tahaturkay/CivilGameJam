using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Native UI Images with polygon sprite geometry keep every edge sharp at any resolution.
// Using Image also gives the generated illustration the same renderer lifecycle as the menus.
[RequireComponent(typeof(RectTransform))]
public class CityMenuArt : MonoBehaviour
{
    readonly List<Sprite> ownedSprites = new List<Sprite>();
    bool built;
    void OnEnable()
    {
        if (built) return;
        built = true;
        BuildArt();
    }
    void BuildArt()
    {
        Poly( new Color32(58, 128, 127, 255), new Vector2(.02f,.24f),new Vector2(.44f,.04f),new Vector2(.98f,.31f),new Vector2(.57f,.56f));
        Poly( new Color32(80, 151, 143, 255), new Vector2(.02f,.24f),new Vector2(.57f,.56f),new Vector2(.24f,.55f));
        Poly( new Color32(139, 172, 149, 255), new Vector2(.11f,.32f),new Vector2(.46f,.15f),new Vector2(.87f,.34f),new Vector2(.53f,.51f));
        Poly( new Color32(222, 211, 174, 255), new Vector2(.25f,.28f),new Vector2(.29f,.26f),new Vector2(.74f,.47f),new Vector2(.70f,.49f));
        Poly( new Color32(222, 211, 174, 255), new Vector2(.30f,.44f),new Vector2(.34f,.46f),new Vector2(.71f,.28f),new Vector2(.67f,.26f));
        House( .20f,.39f,.17f,.14f, new Color32(223,171,95,255));
        House( .41f,.51f,.20f,.22f, new Color32(63,111,126,255));
        House( .63f,.41f,.16f,.14f, new Color32(182,103,78,255));
        House( .42f,.27f,.15f,.12f, new Color32(73,131,126,255));
        Tree(.17f,.30f); Tree(.73f,.36f); Tree(.64f,.23f); Tree(.34f,.51f);
    }
    void House(float x,float y,float w,float h,Color roof)
    {
        Color front = CivicUI.Cream, side = new Color32(203,207,184,255);
        Poly(front,new Vector2(x,y),new Vector2(x+w*.5f,y-w*.24f),new Vector2(x+w*.5f,y-w*.24f+h),new Vector2(x,y+h));
        Poly(side,new Vector2(x+w*.5f,y-w*.24f),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x+w*.5f,y-w*.24f+h));
        Poly(roof,new Vector2(x-.015f,y+h),new Vector2(x+w*.5f,y+h+.09f),new Vector2(x+w+.015f,y+h),new Vector2(x+w*.5f,y+h-w*.24f));
        Poly(new Color32(34,75,88,255),new Vector2(x+w*.12f,y+h*.28f),new Vector2(x+w*.27f,y+h*.22f),new Vector2(x+w*.27f,y+h*.58f),new Vector2(x+w*.12f,y+h*.64f));
    }
    void Tree(float x,float y)
    {
        Poly(new Color32(190,161,116,255),new Vector2(x-.006f,y),new Vector2(x+.006f,y),new Vector2(x+.006f,y+.07f),new Vector2(x-.006f,y+.07f));
        Poly(new Color32(36,95,83,255),new Vector2(x-.035f,y+.035f),new Vector2(x,y+.15f),new Vector2(x+.04f,y+.035f));
        Poly(new Color32(73,141,109,255),new Vector2(x-.035f,y+.035f),new Vector2(x,y+.15f),new Vector2(x,y+.035f));
    }
    void Poly(Color c,params Vector2[] points)
    {
        // A native Image is responsible for material, opaque vertex colors and canvas rebuilds.
        // Its Sprite contains vector vertices rather than a rasterized illustration.
        Vector2 min=points[0], max=points[0];
        foreach(Vector2 point in points) { min=Vector2.Min(min,point); max=Vector2.Max(max,point); }
        Vector2 size=max-min;
        if(size.x<=0 || size.y<=0 || points.Length<3)return;
        var vertices=new Vector2[points.Length];
        for(int i=0;i<points.Length;i++)vertices[i]=new Vector2((points[i].x-min.x)/size.x,(points[i].y-min.y)/size.y);
        float area=0;
        for(int i=0;i<points.Length;i++) { Vector2 a=vertices[i], b=vertices[(i+1)%points.Length]; area+=a.x*b.y-b.x*a.y; }
        var triangles=new ushort[(points.Length-2)*3];
        for(int i=1;i<points.Length-1;i++)
        {
            int offset=(i-1)*3; triangles[offset]=0;
            triangles[offset+1]=(ushort)(area>0?i+1:i);
            triangles[offset+2]=(ushort)(area>0?i:i+1);
        }
        var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.zero,1f,0,SpriteMeshType.FullRect);
        sprite.name="CityPolygon_"+ownedSprites.Count;
        sprite.OverrideGeometry(vertices,triangles); ownedSprites.Add(sprite);
        var go=new GameObject(sprite.name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rt=(RectTransform)go.transform; rt.SetParent(transform,false);
        rt.anchorMin=min; rt.anchorMax=max; rt.offsetMin=rt.offsetMax=Vector2.zero;
        rt.localScale=Vector3.one;
        var image=go.GetComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Simple;
        image.useSpriteMesh=true; image.preserveAspect=false; image.color=new Color(c.r,c.g,c.b,1f);
        image.raycastTarget=false;
        image.canvasRenderer.SetColor(Color.white);
        image.SetAllDirty();
    }
    void OnDestroy()
    {
        foreach(Sprite sprite in ownedSprites)if(sprite!=null)Destroy(sprite);
        ownedSprites.Clear();
    }
}
