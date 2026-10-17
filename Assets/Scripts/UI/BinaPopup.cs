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

    float acilisZamani;
    const float tiklamaKorumasi = 0.2f; // açıldıktan sonra 0.2 sn butonlar tepki vermez

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    bool YeniAcildi()
    {
        return Time.unscaledTime - acilisZamani < tiklamaKorumasi;
    }

    public void Ac(BinaTuru bina)
    {
        aktifBina = bina;
        acilisZamani = Time.unscaledTime;
        panel.SetActive(true);
    }

    public void Kapat()
    {
        if (YeniAcildi()) return;
        panel.SetActive(false);
    }

    void Update()
    {
        // Para ve level sürekli değişebildiği için popup açıkken her karede yenile
        if (!panel.activeSelf || GameManager.Instance == null) return;

        int level = LevelGetir(aktifBina);
        int maliyet = Maliyet(level);

        baslikText.text = Ad(aktifBina);
        levelText.text = "Seviye: " + level;
        aciklamaText.text = Aciklama(aktifBina);
        yukseltButonText.text = "Yükselt (" + maliyet + "₺)";
        yukseltButonu.interactable = GameManager.Instance.para >= maliyet; // para yetmezse buton gri olur
        quizButonu.gameObject.SetActive(aktifBina == BinaTuru.Okul);
    }

    public void YukseltTiklandi()
    {
        if (YeniAcildi()) return;

        // Sahnede Nisa'nın EconomyAndTime'ı varsa yükseltmeyi o yapar (tek kaynak)
        var ekonomi = FindAnyObjectByType<EconomyAndTime>();
        if (ekonomi != null)
        {
            switch (aktifBina)
            {
                case BinaTuru.TemelBina: ekonomi.TemelBinaYukselt(); break;
                case BinaTuru.Okul:      ekonomi.OkulYukselt();      break;
                case BinaTuru.Akut:      ekonomi.AkutYukselt();      break;
                case BinaTuru.Itfaiye:   ekonomi.ItfaiyeYukselt();   break;
                case BinaTuru.DenizSeti: ekonomi.SetYukselt();       break;
                default:                 ekonomi.AltyapiYukselt();   break;
            }
            return;
        }

        // Yoksa (örn. sadece Scene_UI'da test ederken) aynı formülle burada yükselt
        var gm = GameManager.Instance;
        int level = LevelGetir(aktifBina);
        int maliyet = Maliyet(level);

        if (gm.para < maliyet) return;

        gm.para -= maliyet;
        LevelAyarla(aktifBina, level + 1);
    }

    public void QuizTiklandi()
    {
        if (YeniAcildi()) return;
        panel.SetActive(false);
        QuizManager.Instance.QuizAc();
    }

    int Maliyet(int level)
    {
        return level * 100; // 1. kişinin formülüyle aynı olmalı
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
            case BinaTuru.DenizSeti: return gm.setLevel;
            default:                 return gm.altyapiLevel;
        }
    }

    void LevelAyarla(BinaTuru b, int yeniLevel)
    {
        var gm = GameManager.Instance;
        switch (b)
        {
            case BinaTuru.TemelBina: gm.temelBinaLevel = yeniLevel; break;
            case BinaTuru.Okul:      gm.okulLevel = yeniLevel;      break;
            case BinaTuru.Akut:      gm.akutLevel = yeniLevel;      break;
            case BinaTuru.Itfaiye:   gm.itfaiyeLevel = yeniLevel;   break;
            case BinaTuru.DenizSeti: gm.setLevel = yeniLevel;       break;
            default:                 gm.altyapiLevel = yeniLevel;   break;
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
            case BinaTuru.DenizSeti: return "Deniz Seti";
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
            case BinaTuru.DenizSeti: return "Tsunamiye karşı adayı korur.";
            default:                 return "Sel hasarını önler.";
        }
    }

    [ContextMenu("TEST: Okulu Aç")]
    void TestOkul() { Ac(BinaTuru.Okul); }

    [ContextMenu("TEST: İtfaiyeyi Aç")]
    void TestItfaiye() { Ac(BinaTuru.Itfaiye); }
}