using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class BinaPopup : MonoBehaviour
{
    public static BinaPopup Instance;

    public GameObject panel;
    public TMP_Text baslikText, levelText, aciklamaText, yukseltButonText;
    public Button yukseltButonu, quizButonu;

    BinaTuru aktifBina;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Ac(BinaTuru bina)
    {
        aktifBina = bina;
        panel.SetActive(true);
    }

    public void Kapat() { panel.SetActive(false); }

    void Update()
    {
        // Para ve level sürekli değişebildiği için popup açıkken her karede yenile
        if (!panel.activeSelf || GameManager.Instance == null) return;

        int level = LevelGetir(aktifBina);
        int maliyet = level * 100; // 1. kişinin formülüyle aynı olmalı

        baslikText.text = Ad(aktifBina);
        levelText.text = "Seviye: " + level;
        aciklamaText.text = Aciklama(aktifBina);
        yukseltButonText.text = "Yükselt (" + maliyet + "₺)";
        yukseltButonu.interactable = GameManager.Instance.para >= maliyet; // para yetmezse buton gri olur
        quizButonu.gameObject.SetActive(aktifBina == BinaTuru.Okul);
    }

    public void YukseltTiklandi()
    {
        // Birleştirmede 1. kişinin fonksiyonları buraya bağlanacak, örn:
        // switch (aktifBina) { case BinaTuru.Okul: ekonomi.OkulYukselt(); break; ... }
    }

    public void QuizTiklandi()
    {
        Kapat();
        QuizManager.Instance.QuizAc();
    }

    int LevelGetir(BinaTuru b)
    {
        var gm = GameManager.Instance;
        switch (b)
        {
            case BinaTuru.TemelBina: return gm.temelBinaLevel;
            case BinaTuru.Okul:      return gm.okulLevel;
            case BinaTuru.Akut:      return gm.akutLevel;
            case BinaTuru.Itfaiye:   return gm.itfaiyeLevel;
            case BinaTuru.DenizSeti:       return gm.setLevel;
            default:                 return gm.altyapiLevel;
        }
    }

    string Ad(BinaTuru b)
    {
        switch (b)
        {
            case BinaTuru.TemelBina: return "Konutlar";
            case BinaTuru.Okul:      return "Okul";
            case BinaTuru.Akut:      return "AKUT Merkezi";
            case BinaTuru.Itfaiye:   return "İtfaiye";
            case BinaTuru.DenizSeti:       return "Deniz Seti";
            default:                 return "Yol ve Altyapı";
        }
    }

    string Aciklama(BinaTuru b)
    {
        switch (b)
        {
            case BinaTuru.TemelBina: return "Ana gelir kaynağı. Seviyesi depreme dayanıklılığı artırır.";
            case BinaTuru.Okul:      return "Quiz çözerek halkın afet farkındalığını artır.";
            case BinaTuru.Akut:      return "Afetlerde can kaybını azaltır.";
            case BinaTuru.Itfaiye:   return "Yangın hasarını önler.";
            case BinaTuru.DenizSeti:       return "Tsunamiye karşı adayı korur.";
            default:                 return "Sel hasarını önler.";
        }
    }
}