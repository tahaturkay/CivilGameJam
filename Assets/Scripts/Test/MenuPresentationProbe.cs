#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

public sealed class MenuPresentationProbe : MonoBehaviour
{
    public bool videoOnly;
    readonly StringBuilder log=new StringBuilder();int passed,failed,errors;
    float savedScale,savedOffset;bool saved;
    UnityEditor.EditorWindow gameView;PropertyInfo sizeProperty;object sizeGroup;MethodInfo removeSize;
    int originalSize=-1,builtinCount;readonly System.Collections.Generic.List<int> addedSizes=new System.Collections.Generic.List<int>();
    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void OnDisable()
    {
        Application.logMessageReceived-=OnLog;
        if(saved)UIManager.UstPanelAyarlariniUygula(savedScale,savedOffset);
        if(gameView!=null&&originalSize>=0)sizeProperty.SetValue(gameView,originalSize);
        for(int i=addedSizes.Count-1;i>=0;i--)removeSize?.Invoke(sizeGroup,new object[]{addedSizes[i]-builtinCount});
    }
    void Check(bool ok,string message){if(ok)passed++;else failed++;log.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText("Logs/MenuPresentationProgress.txt",log.ToString());}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;var steps=Run();
        while(true){object value;try{if(!steps.MoveNext())break;value=steps.Current;}catch(Exception error){Check(false,error.ToString());break;}yield return value;}
        Check(errors==0,"No runtime errors");log.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");File.WriteAllText("Logs/MenuPresentationTests.txt",log.ToString());EditorApplication.isPlaying=false;
    }
    IEnumerator Run()
    {
        yield return null;yield return null;
        var body=TurkishTMPFontSupport.PreferredFont;var display=TurkishTMPFontSupport.DisplayFont;
        bool Turkish(TMP_FontAsset font){foreach(char c in "İıŞşĞğÜüÖöÇç")if(!font.characterLookupTable.ContainsKey(c))return false;return true;}
        Check(body.name=="LowPolyTheme Body SDF"&&display.name=="LowPolyTheme Display SDF"&&body.atlasPopulationMode==AtlasPopulationMode.Static&&display.atlasPopulationMode==AtlasPopulationMode.Static,"Shipped static Nunito/Bungee theme atlases load");
        Check(Turkish(body)&&Turkish(display),"Both typefaces contain every Turkish character directly");
        var menu=FindAnyObjectByType<MainMenu>();var backdrop=FindAnyObjectByType<IslandMenuBackdrop>();
        var clip=Resources.Load<VideoClip>("Video/IslandMenuLoop");Check(clip!=null&&clip.width==1920&&clip.height==1080&&clip.length>11,"Actual island clip shipped in 1080p");
        float deadline=Time.realtimeSinceStartup+12;
        while((!backdrop.Playing||backdrop.Frame<0)&&Time.realtimeSinceStartup<deadline)yield return null;
        var player=backdrop.GetComponent<VideoPlayer>();
        long first=backdrop.Frame;double timeBefore=player.time;deadline=Time.realtimeSinceStartup+3;
        while(backdrop.Frame==first&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(backdrop.Playing&&backdrop.Frame!=first,"Main-menu video decodes and advances (prepared="+player.isPrepared+", playing="+player.isPlaying+", frame="+first+"→"+backdrop.Frame+", time="+timeBefore+"→"+player.time+", active="+player.isActiveAndEnabled+", speed="+player.playbackSpeed+")");
        Check(menu.transform.Find("MenuBackdrop/ArcadeMenuCard/RibbonBanner/RibbonTitle").GetComponent<TMP_Text>().font==display,"Main-menu title uses themed display font");
        yield return Capture("Logs/MenuPresentation_Menu.png");
        if(videoOnly)yield break;
        menu.AyarlariAc();yield return Capture("Logs/MenuPresentation_Settings.png");menu.ayarlarPaneli.Kapat();menu.Basla();
        while(GameManager.Instance==null||FindAnyObjectByType<UIManager>()==null)yield return null;
        yield return null;yield return null;
        var gm=GameManager.Instance;gm.gun=4;gm.kullaniciDurdurdu=true;
        FindAnyObjectByType<EconomyAndTime>().enabled=false;FindAnyObjectByType<DisasterController>().enabled=false;
        var hud=FindAnyObjectByType<UIManager>();
        var fields=BindingFlags.NonPublic|BindingFlags.Instance;
        var left=(RectTransform)typeof(UIManager).GetField("leftHudGroupRt",fields).GetValue(hud);
        var right=(RectTransform)typeof(UIManager).GetField("rightHudColumnRt",fields).GetValue(hud);
        var alert=(RectTransform)typeof(UIManager).GetField("disasterAlertBadgeRt",fields).GetValue(hud);
        savedScale=UIManager.UstPanelOlcegi;savedOffset=UIManager.UstPanelDikeyBosluk;saved=true;
        UIManager.UstPanelAyarlariniUygula(1,0);yield return null;
        Check(Mathf.Abs(left.localScale.x-1.875f)<.01f,"Previous125% HUD is the new100% baseline (left1.875x original)");
        Check(Mathf.Abs(right.sizeDelta.x-325)<.1f&&Mathf.Abs(right.sizeDelta.y-218)<.1f,"Right HUD is 1.30x wider and approximately1.75x taller");
        Check(hud.gunText.font==body&&hud.paraText.font==body,"Game HUD uses the new readable body font");
        foreach(Vector2Int size in new[]{new Vector2Int(3840,2160),new Vector2Int(1920,1080),new Vector2Int(1366,768)})
        {
            Check(SetGameSize(size.x,size.y),"Select actual Game view "+size.x+"x"+size.y);for(int i=0;i<5;i++)yield return null;
            foreach(float scale in new[]{.75f,1f,1.5f})
            {
                UIManager.UstPanelAyarlariniUygula(scale,80);yield return null;Canvas.ForceUpdateCanvases();
                var day=(RectTransform)right.Find("GunPill");var controls=(RectTransform)right.Find("SpeedControlsRow");
                Rect a=ScreenRect(alert),d=ScreenRect(day),c=ScreenRect(controls),l=ScreenRect(left),r=ScreenRect(right);
                Check(!a.Overlaps(d)&&!a.Overlaps(c)&&!l.Overlaps(r),size+" / "+scale+": alert, day, controls and HUD groups do not overlap");
                Check(Inside(l)&&Inside(r),size+" / "+scale+": enlarged HUD stays inside safe area at80px offset");
            }
            UIManager.UstPanelAyarlariniUygula(1,0);yield return null;
            yield return Capture("Logs/MenuPresentation_HUD_"+size.x+".png");
        }
        foreach(int day in new[]{1,4,5}){gm.gun=day;yield return null;var text=alert.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Warning text fits for day "+day);}
        gm.gun=1;gm.hizCarpani=2;gm.kullaniciDurdurdu=false;
        var pause=FindAnyObjectByType<IslandSimulationPause>();pause.ApplyPause();
        var controlsRow=right.Find("SpeedControlsRow").GetComponentsInChildren<Button>();
        EventSystem.current.SetSelectedGameObject(controlsRow[3].gameObject);
        yield return PressSpace();
        Check(gm.kullaniciDurdurdu&&Time.timeScale==0&&gm.hizCarpani==2,"Space pauses without submitting the previously selected speed button");
        yield return PressSpace();Check(!gm.kullaniciDurdurdu&&Time.timeScale>0,"Second Space press resumes");
        var settings=FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);settings.Ac();yield return null;
        var slider=settings.transform.Find("CivicSettingsCard/LeftCard/TopHudScaleSlider").GetComponent<Slider>();
        Check(Mathf.Approximately(slider.maxValue,1.5f),"Settings permit150% of the enlarged baseline");
        yield return PressSpace();Check(!gm.kullaniciDurdurdu,"Space does not change game pause while settings are open");settings.Kapat();
        var disaster=FindAnyObjectByType<DisasterController>();disaster.enabled=true;disaster.afetSuresi=1.5f;disaster.AfetBaslat(0);
        yield return null;yield return PressSpace();float progress=disaster.IncidentProgress;int damage=disaster.DamagedInstanceCount;
        yield return new WaitForSecondsRealtime(.25f);
        Check(gm.kullaniciDurdurdu&&Time.timeScale==0&&disaster.AfetDevamEdiyorMu&&disaster.IncidentProgress==progress&&disaster.DamagedInstanceCount==damage,"Space freezes a real active fire and its spread");
        yield return PressSpace();float until=Time.realtimeSinceStartup+5;
        while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<until)yield return null;
        Check(!disaster.AfetDevamEdiyorMu&&gm.afetOlduMu,"Space resumes the fire through completion");
        yield return PressSpace();Check(!gm.kullaniciDurdurdu,"Report modal ignores the Space shortcut");FindAnyObjectByType<AfetRaporu>().Devam();
        UIManager.UstPanelAyarlariniUygula(savedScale,savedOffset);
    }
    static Rect ScreenRect(RectTransform transform)
    {
        var canvas=transform.GetComponentInParent<Canvas>().rootCanvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var corners=new Vector3[4];transform.GetWorldCorners(corners);Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=min;
        foreach(var corner in corners){Vector2 p=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static bool Inside(Rect rect){var area=Screen.safeArea;return rect.xMin>=area.xMin-.5f&&rect.xMax<=area.xMax+.5f&&rect.yMin>=area.yMin-.5f&&rect.yMax<=area.yMax+.5f;}
    static IEnumerator PressSpace()
    {
#if ENABLE_INPUT_SYSTEM
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Space));yield return null;yield return null;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
#else
        yield return null;
#endif
    }
    bool SetGameSize(int width,int height)
    {
        try
        {
            var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);object sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var groupEnum=assembly.GetType("UnityEditor.GameViewSizeGroupType");sizeGroup=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupEnum,"Standalone")});
            builtinCount=(int)sizeGroup.GetType().GetMethod("GetBuiltinCount").Invoke(sizeGroup,null);
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var ctor=sizeType.GetConstructor(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{kind,typeof(int),typeof(int),typeof(string)},null);
            object item=ctor.Invoke(new[]{Enum.Parse(kind,"FixedResolution"),(object)width,height,"MenuHUD QA"});sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{item});
            int index=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1;addedSizes.Add(index);removeSize=sizeGroup.GetType().GetMethod("RemoveCustomSize");
            gameView=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));sizeProperty=gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if(originalSize<0)originalSize=(int)sizeProperty.GetValue(gameView);sizeProperty.SetValue(gameView,index);return true;
        }
        catch(Exception error){log.AppendLine(error.Message);return false;}
    }
    static IEnumerator Capture(string path){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}
}
#endif
