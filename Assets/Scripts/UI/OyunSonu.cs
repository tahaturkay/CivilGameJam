using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Ana Canvas'a eklenir. GameManager.oyunBittiMi true olunca karne ekranını gösterir.
public class OyunSonu : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text baslikText;
    public TMP_Text dereceText;
    public TMP_Text istatistikText;
    public TMP_Text afetOzetiText;
    public string anaMenuSahnesi = "MainMenu";

    void Awake()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.oyunBittiMi && !panel.activeSelf) Goster(gm);
    }

    void Goster(GameManager gm)
    {
        int yildiz = gm.Yildiz();

        baslikText.text = gm.kazandiMi ? "ADA AYAKTA!" : "ADA KAYBEDİLDİ";
        dereceText.text = gm.kazandiMi ? DereceAdi(yildiz) + "  (" + yildiz + "/3 yıldız)" : "Nüfus kritik seviyenin altına düştü.";

        istatistikText.text =
            "Hayatını kaybeden: " + gm.toplamVefat + "\n" +
            "Kurtarılan (farkındalık + AKUT): " + gm.toplamKurtarilan + "\n" +
            "Kalan nüfus: " + gm.nufus + "\n" +
            "Kazanılan para: " + gm.toplamKazanc + " coin   Harcanan: " + gm.toplamHarcama + " coin\n" +
            "Yapılan yükseltme: " + gm.yukseltmeSayisi + "\n" +
            "Doğru cevaplanan soru: " + gm.dogruCevapSayisi + "\n" +
            "En yüksek farkındalık: %" + Mathf.RoundToInt(gm.enYuksekFarkindalik);

        afetOzetiText.text = gm.afetOzeti +
            "\nAfetleri engelleyemeyiz, ama hazırlıkla etkilerini azaltabiliriz.";

        panel.SetActive(true);
    }

    string DereceAdi(int yildiz)
    {
        switch (yildiz)
        {
            case 3:  return "MÜKEMMEL HAZIRLIK";
            case 2:  return "BAŞARILI";
            case 1:  return "GELİŞTİRİLMELİ";
            default: return "KRİTİK";
        }
    }

    public void TekrarOyna()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        // Editörde Build listesinde olmayan test sahneleri de yeniden yüklenebilsin
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
    }

    public void AnaMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(anaMenuSahnesi);
    }
}
