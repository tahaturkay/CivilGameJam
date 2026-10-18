using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>A silent recorded island vignette; menus never run a second city simulation.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class IslandMenuBackdrop : MonoBehaviour
{
    RawImage image;
    VideoPlayer player;
    RenderTexture target;
    Texture poster;
    Vector2 lastSize;
    public bool Playing => player!=null&&player.isPlaying;
    public long Frame => player!=null?player.frame:-1;

    void Awake()
    {
        image=GetComponent<RawImage>();image.raycastTarget=false;
        poster=Resources.Load<Texture2D>("Video/IslandMenuPoster");image.texture=poster;
        image.color=poster!=null?Color.white:Color.clear;
        var clip=Resources.Load<VideoClip>("Video/IslandMenuLoop");
        if(clip==null){Debug.LogWarning("Menü ada videosu bulunamadı; kapak görseli kullanılıyor.");return;}
        target=new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32){name="MenuIslandVideo",antiAliasing=1};target.Create();
        player=gameObject.AddComponent<VideoPlayer>();player.playOnAwake=false;
        player.source=VideoSource.VideoClip;player.clip=clip;player.isLooping=true;
        player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=target;
        player.audioOutputMode=VideoAudioOutputMode.None;player.skipOnDrop=true;
        player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
        player.waitForFirstFrame=true;player.sendFrameReadyEvents=true;
        player.prepareCompleted+=Prepared;player.frameReady+=FirstFrameReady;player.errorReceived+=VideoError;player.Prepare();
    }
    void Prepared(VideoPlayer video){if(isActiveAndEnabled)video.Play();}
    void FirstFrameReady(VideoPlayer video,long frame)
    {image.texture=target;image.color=Color.white;video.sendFrameReadyEvents=false;video.frameReady-=FirstFrameReady;}
    void VideoError(VideoPlayer video,string message){image.texture=poster;image.color=poster!=null?Color.white:Color.clear;Debug.LogWarning("Menü videosu: "+message);}
    void OnEnable(){if(player!=null&&player.isPrepared)player.Play();}
    void OnDisable(){if(player!=null)player.Pause();}
    void LateUpdate()
    {
        Vector2 size=image.rectTransform.rect.size;if(size==lastSize||size.y<=0)return;lastSize=size;
        float view=size.x/size.y,source=1920f/1080;
        image.uvRect=view>source?new Rect(0,(1-source/view)*.5f,1,source/view):new Rect((1-view/source)*.5f,0,view/source,1);
    }
    void OnDestroy()
    {
        if(player!=null){player.prepareCompleted-=Prepared;player.frameReady-=FirstFrameReady;player.errorReceived-=VideoError;player.Stop();player.targetTexture=null;}
        if(target!=null){target.Release();Destroy(target);}
    }
}
