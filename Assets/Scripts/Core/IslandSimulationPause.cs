using UnityEngine;

/// <summary>HUD/escape pause owns scaled simulation time; speed buttons keep their existing calendar/actor multiplier.</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class IslandSimulationPause : MonoBehaviour
{
    bool ownsPause;
    float resumeScale=1;
    public static bool IsPaused=>PauseMenu.DurdurulduMu||(GameManager.Instance!=null&&GameManager.Instance.kullaniciDurdurdu);

    void Update(){ApplyPause();}
    public void ApplyPause()
    {
        if(IsPaused)
        {
            if(!ownsPause){if(Time.timeScale>0)resumeScale=Time.timeScale;ownsPause=true;}
            Time.timeScale=0;
        }
        else if(ownsPause){Time.timeScale=resumeScale;ownsPause=false;}
        else if(Time.timeScale>0)resumeScale=Time.timeScale;
    }
    void OnDisable(){if(ownsPause&&!PauseMenu.DurdurulduMu)Time.timeScale=resumeScale;ownsPause=false;}
}
