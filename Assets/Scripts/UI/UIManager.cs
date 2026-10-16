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

        int kalan = (7 - gm.gun % 7) % 7;
        gunText.text = "Gün: " + gm.gun + (kalan == 0 ? " (AFET GÜNÜ!)" : " (Afete " + kalan + " gün)");
        paraText.text = "Para: " + gm.para + "₺";
        farkindalikText.text = "Farkındalık: %" + Mathf.RoundToInt(gm.farkindalikYuzdesi);
        nufusText.text = "Nüfus: " + gm.nufus;
    }
}