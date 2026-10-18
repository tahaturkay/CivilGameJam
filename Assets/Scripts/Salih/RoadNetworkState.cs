using System.Collections.Generic;
using UnityEngine;

/// <summary>Infrastructure controls every road surface and keeps repair markers visible until repaired.</summary>
[DisallowMultipleComponent]
public class RoadNetworkState : MonoBehaviour
{
    public GameObject asphaltMarkings;
    Transform markers;
    bool shown;
    readonly Dictionary<BuildingVisuals,GameObject> roadMarkers=new Dictionary<BuildingVisuals,GameObject>();
    public bool ConstructionVisible => markers != null && markers.gameObject.activeSelf;
    void Start()
    {
        markers = new GameObject("HasarliYolOnarimNoktalari").transform;
        markers.SetParent(transform, false);
        foreach (BuildingVisuals road in GetComponentsInChildren<BuildingVisuals>())
        {
            var col = road.GetComponent<BoxCollider>();
            if (col == null) continue;
            var group=new GameObject(road.name+"_Onarim");group.transform.SetParent(markers,false);roadMarkers[road]=group;
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = road.transform.TransformPoint(new Vector3(0,.34f,(i-1)*col.size.z*.26f));
                var mark = IslandRoadBarrierArt.Create(group.transform, p, col.size.x * Mathf.Abs(road.transform.lossyScale.x),i);
                mark.transform.rotation = road.transform.rotation;
            }
        }
        markers.gameObject.SetActive(false);
    }
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (asphaltMarkings != null) asphaltMarkings.SetActive(gm.altyapiLevel == 3);
        bool any=false;
        foreach(var pair in roadMarkers)
        {
            bool damaged=pair.Key!=null&&pair.Key.IsDamaged;
            if(pair.Value.activeSelf!=damaged)pair.Value.SetActive(damaged);any|=damaged;
        }
        if(markers!=null&&shown!=any){shown=any;markers.gameObject.SetActive(shown);}
    }
}
