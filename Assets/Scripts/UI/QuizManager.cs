using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Okul quizi. Doğru bilinen soru havuzdan çıkar; yanlış bilinen kalır ve doğru cevap gösterilir.
// Farkındalık okul seviyesinin tavanını geçemez (DengeAyarlari.okulFarkindalikTavani).
public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    public GameObject quizPanel;
    public TMP_Text soruText;
    public Button[] sikButonlari;       // 4 buton
    public QuestionSO[] sorular;

    [Header("Cevap sonrası")]
    public float sonucGosterimSuresi = 1.5f;
    public Color dogruRenk = new Color(0.3f, 0.8f, 0.4f);
    public Color yanlisRenk = new Color(0.9f, 0.35f, 0.3f);

    readonly List<QuestionSO> kalanSorular = new List<QuestionSO>();
    QuestionSO aktifSoru;
    bool cevapBekleniyor;

    public QuestionSO AktifSoru { get { return aktifSoru; } } // otomatik test için

    void Awake()
    {
        Instance = this;
        quizPanel.SetActive(false);
        kalanSorular.AddRange(sorular);
    }

    public void QuizAc()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.oyunDurduMu = true;
        quizPanel.SetActive(true);

        float tavan = DengeAyarlari.Aktif.FarkindalikTavani(gm.okulLevel);
        if (kalanSorular.Count == 0)
        {
            MesajGoster("Tebrikler! Bütün soruları doğru bildin.");
            return;
        }
        if (gm.farkindalikYuzdesi >= tavan)
        {
            MesajGoster(tavan >= 100f
                ? "Halkın farkındalığı en üst seviyede!"
                : "Farkındalık bu okulun sınırına (%" + tavan + ") ulaştı.\nDaha fazlası için okulu yükselt.");
            return;
        }

        aktifSoru = kalanSorular[Random.Range(0, kalanSorular.Count)];
        soruText.text = aktifSoru.soruMetni.Trim();
        cevapBekleniyor = true;

        for (int i = 0; i < sikButonlari.Length; i++)
        {
            bool varMi = i < aktifSoru.siklar.Length;
            sikButonlari[i].gameObject.SetActive(varMi);
            if (!varMi) continue;

            Boya(sikButonlari[i], Color.white);
            sikButonlari[i].interactable = true;
            sikButonlari[i].GetComponentInChildren<TMP_Text>().text = aktifSoru.siklar[i].Trim();
            int secilen = i; // lambda için kopya şart
            sikButonlari[i].onClick.RemoveAllListeners();
            sikButonlari[i].onClick.AddListener(() => CevapVer(secilen));
        }
    }

    void CevapVer(int secilen)
    {
        if (!cevapBekleniyor) return;
        cevapBekleniyor = false;

        var gm = GameManager.Instance;
        int dogru = aktifSoru.dogruSikIndex;
        bool bildi = secilen == dogru;

        if (bildi)
        {
            gm.FarkindalikEkle(DengeAyarlari.Aktif.dogruCevapOdulu);
            gm.dogruCevapSayisi++;
            kalanSorular.Remove(aktifSoru);
        }

        // Doğru şıkkı yeşil, yanlış seçileni kırmızı göster; kısa süre sonra kapat
        foreach (Button b in sikButonlari) b.interactable = false;
        Boya(sikButonlari[dogru], dogruRenk);
        if (!bildi) Boya(sikButonlari[secilen], yanlisRenk);
        soruText.text = bildi ? "Doğru! Farkındalık arttı." : "Yanlış. Doğru cevap: " + aktifSoru.siklar[dogru].Trim();

        StartCoroutine(BekleVeKapat());
    }

    IEnumerator BekleVeKapat()
    {
        yield return new WaitForSecondsRealtime(sonucGosterimSuresi);
        QuizKapat();
    }

    void MesajGoster(string mesaj)
    {
        soruText.text = mesaj;
        foreach (Button b in sikButonlari) b.gameObject.SetActive(false);
    }

    public void QuizKapat()
    {
        StopAllCoroutines();
        cevapBekleniyor = false;
        quizPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.oyunDurduMu = false;
    }

    void Boya(Button b, Color renk)
    {
        if (b.targetGraphic != null) b.targetGraphic.color = renk;
    }
}
