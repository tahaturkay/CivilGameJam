using UnityEngine;

/// <summary>Compatibility for old scene/test references. The riverbed event and all its rewards are retired.</summary>
public sealed class DereYatagiKarari : MonoBehaviour
{
    public bool Soruldu=>false;
    public bool KabulEdildi=>false;
    public bool Cozuldu=>false;
    public bool Acik=>false;
    public string GeriBildirim=>"";
    public float GeriBildirimBitisi=>0;
    void Awake(){enabled=false;}
    public void KararVer(bool kabul){}
}
