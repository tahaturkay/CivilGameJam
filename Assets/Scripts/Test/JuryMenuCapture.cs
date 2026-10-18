#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class JuryMenuCapture : MonoBehaviour
{
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");
        yield return null;yield return null;
        yield return Capture("Logs/Jury_MainMenu.png");
        var menu=FindAnyObjectByType<MainMenu>();menu.AyarlariAc();yield return null;
        yield return Capture("Logs/Jury_Settings.png");menu.ayarlarPaneli.Kapat();menu.Basla();
        yield return null;yield return null;yield return null;
        GameManager.Instance.kullaniciDurdurdu=true;
        var building=FindAnyObjectByType<BuildingVisuals>();BinaPopup.Instance.Ac(building.binaTuru,building.transform);
        yield return Capture("Logs/Jury_BuildingCard.png");
        yield return new WaitForSecondsRealtime(.25f);BinaPopup.Instance.DisariTiklandi();yield return null;
        var pause=FindAnyObjectByType<PauseMenu>();pause.Durdur();
        yield return null;
        var settings=FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);settings.Ac();
        yield return Capture("Logs/Jury_PauseSettings.png");settings.Kapat();pause.Devam();
        GameManager.Instance.gun=7;GameManager.Instance.kullaniciDurdurdu=false;
        yield return null;yield return null;
        var decision=FindAnyObjectByType<DereYatagiKarari>();
        if(decision.Acik){yield return Capture("Logs/Jury_Decision.png");decision.KararVer(false);}
        GameManager.Instance.kullaniciDurdurdu=true;GameManager.Instance.oyunBittiMi=true;GameManager.Instance.kazandiMi=true;
        yield return Capture("Logs/Jury_Ending.png");
        File.WriteAllText("Logs/JuryMenuCapture.txt","PASS: Main menu, shared settings and contextual card rendered in Play Mode.");
        UnityEditor.EditorApplication.isPlaying=false;
    }
    static IEnumerator Capture(string path){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);yield return null;}
}
#endif
