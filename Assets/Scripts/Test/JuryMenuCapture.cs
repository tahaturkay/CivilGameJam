#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class JuryMenuCapture : MonoBehaviour
{
    readonly StringBuilder results=new StringBuilder();int passed,failed,errors;
    void OnEnable(){Application.logMessageReceived+=OnLog;}
    void OnDisable(){Application.logMessageReceived-=OnLog;}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void Check(bool ok,string message){if(ok)passed++;else failed++;results.AppendLine((ok?"PASS ":"FAIL ")+message);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");
        yield return null;yield return null;
        yield return Capture("Logs/Jury_MainMenu.png");
        var menu=FindAnyObjectByType<MainMenu>();
        Check(menu!=null&&menu.transform.Find("MenuBackdrop/ArcadeMenuCard").gameObject.activeInHierarchy,"New main menu opens in Play Mode");
        menu.RehberiAc();yield return Capture("Logs/Jury_Guide.png");
        Check(menu.transform.Find("MenuBackdrop/NasilOynanirModal").gameObject.activeInHierarchy,"How-to-play guide opens");menu.RehberiKapat();
        menu.AyarlariAc();yield return null;
        Check(menu.ayarlarPaneli.AcikMi,"Main menu settings open");
        var audioParent=menu.ayarlarPaneli.muzikSlider.transform.parent;
        Check(audioParent.name=="LeftCard"&&menu.ayarlarPaneli.efektSlider.transform.parent==audioParent,"Both audio sliders align with the audio card");
        float music=Ayarlar.MuzikSesi,effects=Ayarlar.EfektSesi;
        menu.ayarlarPaneli.muzikSlider.value=.35f;menu.ayarlarPaneli.efektSlider.value=.45f;
        Check(Mathf.Approximately(Ayarlar.MuzikSesi,.35f)&&Mathf.Approximately(Ayarlar.EfektSesi,.45f),"Audio sliders apply settings");
        menu.ayarlarPaneli.muzikSlider.value=music;menu.ayarlarPaneli.efektSlider.value=effects;
        yield return Capture("Logs/Jury_Settings.png");menu.ayarlarPaneli.Kapat();menu.Basla();
        while (GameManager.Instance == null || BinaPopup.Instance == null || FindAnyObjectByType<BuildingVisuals>() == null) yield return null;
        yield return null;
        Check(GameManager.Instance!=null,"Start button loads MainScene");
        GameManager.Instance.kullaniciDurdurdu=true;
        var building=FindAnyObjectByType<BuildingVisuals>();BinaPopup.Instance.Ac(building.binaTuru,building.transform);
        yield return Capture("Logs/Jury_BuildingCard.png");
        yield return new WaitForSecondsRealtime(.25f);BinaPopup.Instance.DisariTiklandi();yield return null;
        var pause=FindAnyObjectByType<PauseMenu>();pause.Durdur();
        yield return Capture("Logs/Jury_Pause.png");
        Check(PauseMenu.DurdurulduMu&&Time.timeScale==0&&pause.pausePanel.activeSelf,"Pause menu opens and freezes simulation");
        var settings=FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);settings.Ac();
        yield return Capture("Logs/Jury_PauseSettings.png");
        Check(settings.AcikMi,"Shared settings open from pause menu");
        var hudSlider=settings.transform.Find("CivicSettingsCard/LeftCard/TopHudScaleSlider").GetComponent<Slider>();
        float scale=UIManager.UstPanelOlcegi;hudSlider.value=1.1f;
        Check(Mathf.Approximately(UIManager.UstPanelOlcegi,1.1f),"HUD size control applies to the live city");hudSlider.value=scale;
        settings.Kapat();pause.Devam();
        Check(!PauseMenu.DurdurulduMu&&Time.timeScale==0,"Closing ESC pause preserves an existing HUD pause");
        GameManager.Instance.gun=7;GameManager.Instance.kullaniciDurdurdu=false;
        yield return null;yield return null;
        var decision=FindAnyObjectByType<DereYatagiKarari>();
        if(decision!=null&&decision.Acik){yield return Capture("Logs/Jury_Decision.png");decision.KararVer(false);}
        FindAnyObjectByType<EconomyAndTime>().enabled=false;GameManager.Instance.gun=1;
        FindAnyObjectByType<IslandSimulationPause>().ApplyPause();
        var disaster=FindAnyObjectByType<DisasterController>();disaster.afetSuresi=.75f;disaster.AfetBaslat(0);
        float until=Time.realtimeSinceStartup+10;
        while(disaster.AfetDevamEdiyorMu&&Time.realtimeSinceStartup<until)yield return null;
        var report=FindAnyObjectByType<AfetRaporu>();yield return new WaitForSecondsRealtime(1.1f);
        Check(report.panel.activeSelf&&GameManager.Instance.afetOlduMu,"New report opens after a real fire");
        yield return Capture("Logs/Jury_Report.png");report.Devam();
        Check(!report.panel.activeSelf&&!GameManager.Instance.afetOlduMu,"Report continue returns to the city");
        GameManager.Instance.kullaniciDurdurdu=true;GameManager.Instance.oyunBittiMi=true;GameManager.Instance.kazandiMi=true;
        yield return Capture("Logs/Jury_Ending.png");
        Check(FindAnyObjectByType<OyunSonu>().panel.activeSelf,"New ending card opens");Check(errors==0,"No runtime errors");
        results.AppendLine("RESULT: "+passed+" passed, "+failed+" failed.");File.WriteAllText("Logs/JuryMenuCapture.txt",results.ToString());
        UnityEditor.EditorApplication.isPlaying=false;
    }
    static IEnumerator Capture(string path){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}
}
#endif
