using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    public GameObject quizPanel;
    public TMP_Text soruText;
    public Button[] sikButonlari;       // 4 buton
    public QuestionSO[] sorular;
    public float dogruCevapOdulu = 10f;

    int aktifSoru = -1;

    void Awake()
    {
        Instance = this;
        quizPanel.SetActive(false);
    }

    public void QuizAc()
    {
        if (sorular.Length == 0) return;
        GameManager.Instance.oyunDurduMu = true;

        aktifSoru = Random.Range(0, sorular.Length);
        QuestionSO soru = sorular[aktifSoru];
        soruText.text = soru.soruMetni;

        for (int i = 0; i < sikButonlari.Length; i++)
        {
            bool varMi = i < soru.siklar.Length;
            sikButonlari[i].gameObject.SetActive(varMi);
            if (!varMi) continue;

            sikButonlari[i].GetComponentInChildren<TMP_Text>().text = soru.siklar[i];
            int secilen = i; // lambda için kopya şart
            sikButonlari[i].onClick.RemoveAllListeners();
            sikButonlari[i].onClick.AddListener(() => CevapVer(secilen));
        }
        quizPanel.SetActive(true);
    }

    void CevapVer(int secilen)
    {
        var gm = GameManager.Instance;
        if (secilen == sorular[aktifSoru].dogruSikIndex)
            gm.farkindalikYuzdesi = Mathf.Min(100f, gm.farkindalikYuzdesi + dogruCevapOdulu);
        QuizKapat();
    }

    public void QuizKapat()
    {
        quizPanel.SetActive(false);
        GameManager.Instance.oyunDurduMu = false;
    }
}