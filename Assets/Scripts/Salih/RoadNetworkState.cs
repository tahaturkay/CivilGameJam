using System.Collections.Generic;
using UnityEngine;

/// <summary>Infrastructure controls every road surface and keeps repair markers visible until repaired.</summary>
[DisallowMultipleComponent]
public class RoadNetworkState : MonoBehaviour
{
    public GameObject asphaltMarkings;
    Transform markers;
    bool shown;
    public bool ConstructionVisible => markers != null && markers.gameObject.activeSelf;
    void Start()
    {
        markers = new GameObject("HasarliYolOnarimNoktalari").transform;
        markers.SetParent(transform, false);
        foreach (BuildingVisuals road in GetComponentsInChildren<BuildingVisuals>())
        {
            var col = road.GetComponent<BoxCollider>();
            if (col == null) continue;
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = road.transform.TransformPoint(new Vector3(0,.34f,(i-1)*col.size.z*.26f));
                var mark = IslandEffectsFactory.CreateConstructionMarkers(markers, p, col.size.x,i);
                mark.transform.rotation = road.transform.rotation;
            }
        }
        markers.gameObject.SetActive(false);
    }
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (asphaltMarkings != null) asphaltMarkings.SetActive(gm.altyapiLevel == 3 && !gm.altyapiHasarli);
        if (markers != null && shown != gm.altyapiHasarli)
        {
            shown = gm.altyapiHasarli; markers.gameObject.SetActive(shown);
        }
    }
}
