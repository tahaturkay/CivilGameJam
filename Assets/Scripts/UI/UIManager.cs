using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Üst Bar")]
    public TMP_Text gunText, paraText, farkindalikText, nufusText;

    [Header("Bina Satırları")]
    public TMP_Text temelBinaText, okulText, akutText, itfaiyeText, setText, altyapiText;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int kalan = (7 - gm.gun % 7) % 7;
        gunText.text = "Gün: " + gm.gun + (kalan == 0 ? " (AFET GÜNÜ!)" : " (Afete " + kalan + " gün)");
        paraText.text = "Para: " + gm.para + "₺";
        farkindalikText.text = "Farkındalık: %" + Mathf.RoundToInt(gm.farkindalikYuzdesi);
        nufusText.text = "Nüfus: " + gm.nufus;

        temelBinaText.text = BinaYazisi("Temel Bina", gm.temelBinaLevel);
        okulText.text      = BinaYazisi("Okul", gm.okulLevel);
        akutText.text      = BinaYazisi("AKUT", gm.akutLevel);
        itfaiyeText.text   = BinaYazisi("İtfaiye", gm.itfaiyeLevel);
        setText.text       = BinaYazisi("Deniz Seti", gm.setLevel);
        altyapiText.text   = BinaYazisi("Altyapı", gm.altyapiLevel);
    }

    string BinaYazisi(string ad, int level)
    {
        return ad + "  Lv " + level + "  (Yükselt: " + (level * 100) + "₺)";
    }
}