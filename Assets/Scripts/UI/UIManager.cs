using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Üst Bar")]
    public TMP_Text gunText;
    public TMP_Text paraText;
    public TMP_Text farkindalikText;
    public TMP_Text nufusText;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var d = DengeAyarlari.Aktif;
        int kalan = d.SonrakiAfeteKalanGun(gm.gun);
        string afet = kalan == 0 ? " (AFET GÜNÜ!)" : kalan > 0 ? " (Afete " + kalan + " gün)" : "";
        gunText.text = "Gün: " + gm.gun + "/" + d.toplamGun + afet;
        paraText.text = "Para: " + gm.para + "₺ (+" + Ekonomi.GunlukGelir() + "/gün)";
        farkindalikText.text = "Farkındalık: %" + Mathf.RoundToInt(gm.farkindalikYuzdesi);
        nufusText.text = "Nüfus: " + gm.nufus + "/" + Ekonomi.NufusKapasitesi();
    }
}
