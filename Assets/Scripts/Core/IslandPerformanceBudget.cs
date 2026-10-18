using UnityEngine;

/// <summary>Limit idle GPU work instead of rendering the same small city at hundreds of frames per second.</summary>
public sealed class IslandPerformanceBudget : MonoBehaviour
{
    int previousRate,previousVsync;
    void OnEnable(){previousRate=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;}
    void OnDisable(){Application.targetFrameRate=previousRate;QualitySettings.vSyncCount=previousVsync;}
}
