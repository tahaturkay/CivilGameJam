using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// Ana Canvas'a eklenir. GameManager.oyunBittiMi true olunca karne ekranını gösterir.
public class OyunSonu : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text baslikText;
    public TMP_Text dereceText;
    public TMP_Text istatistikText;
    public TMP_Text afetOzetiText;
    public string anaMenuSahnesi = "MainMenu";
    RectTransform resultCard;
    TMP_Text epilog;
    public string EpilogBasligi {get;private set;}

    void Awake()
    {
        panel.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.oyunBittiMi && !panel.activeSelf) Goster(gm);
        if(panel.activeSelf&&resultCard!=null)CivicUI.FitCard(resultCard,new Vector2(900,760),30,1.8f);
    }
    void Start(){BuildEpilogueLayout();}

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
        var data=AnlatiVerisi.Aktif;var controller=FindAnyObjectByType<DisasterController>();
        if(data!=null&&epilog!=null)
        {
            int index=data.KartIndex(controller!=null?controller.DefendedIncidentCount:0,gm.farkindalikYuzdesi,gm.para,gm.toplamVefat);
            var card=data.kartlar[index];EpilogBasligi=card.baslik;epilog.text="<b>"+card.baslik+"</b>\n"+card.metin;
        }

        panel.SetActive(true);
    }

    void BuildEpilogueLayout()
    {
        Transform old=panel.transform.Find("Pencere");if(old==null)return;
        Transform retry=old.Find("TekrarOynaButonu"),menu=old.Find("AnaMenuButonu");
        resultCard=CivicUI.Box("SehirKarnesi",panel.transform,CivicUI.Cream,true).rectTransform;
        Move(baslikText,28,22,844,50,32,CivicUI.Teal);
        Move(dereceText,28,85,844,45,22,CivicUI.Navy);
        Move(istatistikText,28,153,440,260,21,CivicUI.Navy);
        Move(afetOzetiText,510,153,362,260,20,CivicUI.Muted);
        epilog=CivicUI.Text("Epilog",resultCard,"",23,CivicUI.Navy,baslikText.font);
        CivicUI.Place(epilog.rectTransform,28,447,844,150);epilog.alignment=TextAlignmentOptions.TopLeft;
        epilog.enableAutoSizing=true;epilog.fontSizeMin=19;epilog.fontSizeMax=23;
        if(retry!=null){retry.SetParent(resultCard,false);CivicUI.Place((RectTransform)retry,28,650,408,65);CivicUI.ReskinButton(retry.GetComponent<Button>(),CivicUI.Teal,CivicUI.Cream);}
        if(menu!=null){menu.SetParent(resultCard,false);CivicUI.Place((RectTransform)menu,464,650,408,65);CivicUI.ReskinButton(menu.GetComponent<Button>(),CivicUI.Navy,CivicUI.Cream);}
        old.gameObject.SetActive(false);
    }
    void Move(TMP_Text text,float x,float y,float w,float h,float size,Color color)
    {
        if(text==null)return;text.transform.SetParent(resultCard,false);
        var le=text.GetComponent<LayoutElement>();if(le!=null)le.ignoreLayout=true;
        CivicUI.Place(text.rectTransform,x,y,w,h);text.color=color;text.fontSize=size;text.fontStyle=FontStyles.Normal;
        text.alignment=TextAlignmentOptions.TopLeft;text.textWrappingMode=TextWrappingModes.Normal;
        text.enableAutoSizing=true;text.fontSizeMin=size-3;text.fontSizeMax=size;
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
