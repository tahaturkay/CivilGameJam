using UnityEngine;

/// <summary>The chosen, hand-drawn lighthouse pointer. Scene references retain the texture in player builds.</summary>
[DisallowMultipleComponent]
public sealed class IslandCursor : MonoBehaviour
{
    public Texture2D pointer;
    public Vector2 hotspot=new Vector2(3,3);
    bool focused=true;
    public bool Applied {get;private set;}

    void OnEnable(){Apply();}
    void OnApplicationFocus(bool hasFocus){focused=hasFocus;if(hasFocus)Apply();else ResetCursor();}
    void Apply()
    {
        if(!Application.isPlaying||!focused||pointer==null)return;
        Cursor.SetCursor(pointer,hotspot,CursorMode.Auto);Applied=true;
    }
    void ResetCursor(){if(Applied)Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);Applied=false;}
    void OnDisable(){ResetCursor();}
}
