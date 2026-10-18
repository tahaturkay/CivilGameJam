using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class MenuPresentationValidation
{
    const string Request="Temp/menu_presentation_istegi",Key="MenuPresentation.Mode";
    static bool handlingRequest;
    static MenuPresentationValidation()
    {
        EditorApplication.update+=()=>
        {
            if(handlingRequest||!File.Exists(Request)||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            handlingRequest=true;
            try
            {
                AssetDatabase.Refresh();
                if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                string mode=File.ReadAllText(Request).Trim();
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(mode=="capture"?IslandSceneSetup.MainScenePath:"Assets/Scenes/MainMenu.unity");
                File.Delete(Request);SessionState.SetString(Key,mode);EditorApplication.isPlaying=true;
            }
            finally{handlingRequest=false;}
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            string mode=SessionState.GetString(Key,"");if(mode.Length==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                if(mode=="capture")new GameObject("IslandMenuClipCapture").AddComponent<IslandMenuClipCapture>();
                else new GameObject("MenuPresentationProbe").AddComponent<MenuPresentationProbe>().videoOnly=mode=="video";
            }
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseString(Key);AssetDatabase.Refresh();}
        };
    }
}

/// <summary>Editor-only repeatable capture, using the real scene, sea, citizens and cars.</summary>
public sealed class IslandMenuClipCapture : MonoBehaviour
{
    MediaEncoder encoder;RenderTexture target;Texture2D frame;Camera camera;
    int oldCaptureRate;float oldScale;bool captured;
    IEnumerator Start()
    {
        oldCaptureRate=Time.captureFramerate;oldScale=Time.timeScale;
        Application.runInBackground=true;yield return null;yield return null;
        var gm=GameManager.Instance;gm.gun=1;gm.hizCarpani=1;gm.kullaniciDurdurdu=false;
        FindAnyObjectByType<IslandSimulationPause>().ApplyPause();Time.timeScale=1;
        var clock=FindAnyObjectByType<EconomyAndTime>();clock.enabled=false;
        typeof(EconomyAndTime).GetField("gunSayaci",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(clock,DengeAyarlari.Aktif.birGunSuresi*.34f);
        FindAnyObjectByType<DisasterController>().enabled=false;
        camera=Camera.main;camera.GetComponent<IslandCameraController>().enabled=false;
        camera.orthographicSize=72;
        // Compose the city to the right of the menu, with the coast framing the shot.
        Vector3 focus=new Vector3(-14,2.5f,0);camera.transform.position=focus-camera.transform.forward*230;
        Vector3 origin=camera.transform.position;
        target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        frame=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        Directory.CreateDirectory("Assets/Resources/Video");
        try{encoder=new MediaEncoder("Assets/Resources/Video/IslandMenuLoop.mp4",new VideoTrackAttributes{frameRate=new MediaRational(24),width=1920,height=1080,includeAlpha=false,bitRateMode=VideoBitrateMode.High});}
        catch(Exception error){File.WriteAllText("Logs/MenuClipCapture.txt","FAIL: "+error);UnityEditor.EditorApplication.isPlaying=false;yield break;}
        Time.captureFramerate=24;
        for(int i=0;i<288;i++)
        {
            float phase=i/288f*Mathf.PI*2;
            camera.transform.position=origin+camera.transform.right*(Mathf.Sin(phase)*4)+camera.transform.up*(Mathf.Cos(phase)*1.2f);
            yield return new WaitForEndOfFrame();
            var previous=RenderTexture.active;RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,1920,1080),0,0,false);frame.Apply(false,false);RenderTexture.active=previous;
            encoder.AddFrame(frame);
            if(i==0)File.WriteAllBytes("Assets/Resources/Video/IslandMenuPoster.png",frame.EncodeToPNG());
            if(i%48==0)File.WriteAllText("Logs/MenuClipCapture.txt","Recording "+(i+1)+" / 288 frames");
        }
        encoder.Dispose();encoder=null;captured=true;
        File.WriteAllText("Logs/MenuClipCapture.txt","PASS: real MainScene island, 1920x1080, 24fps, 288 frames / 12 seconds, silent H.264 MP4. Camera/sea/cars/citizens animate; no HUD baked in. "+new FileInfo("Assets/Resources/Video/IslandMenuLoop.mp4").Length+" bytes.");
        UnityEditor.EditorApplication.isPlaying=false;
    }
    void OnDisable()
    {
        encoder?.Dispose();encoder=null;Time.captureFramerate=oldCaptureRate;Time.timeScale=oldScale>0?oldScale:1;
        if(camera!=null)camera.targetTexture=null;
        if(target!=null){target.Release();Destroy(target);}if(frame!=null)Destroy(frame);
        if(!captured)Debug.LogWarning("Ada menü kaydı tamamlanmadan durduruldu.");
    }
}
