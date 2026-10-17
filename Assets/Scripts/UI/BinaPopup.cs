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
        var gm = GameManager.Instance;
        if (gm != null && (gm.afetOlduMu || gm.oyunBittiMi)) return;

        aktifBina = bina;
        acilisZamani = Time.unscaledTime;
        panel.SetActive(true);
        if (gm != null) gm.oyunDurduMu = true; // oyuncu rahatça okusun diye zaman durur
    }

    public void Kapat()
    {
        if (YeniAcildi()) return;
        PaneliKapat();
    }

    void PaneliKapat()
    {
        panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.oyunDurduMu = false;
    }

    void Update()
    {
        // Para ve level değişebildiği için popup açıkken her karede yenile
        var gm = GameManager.Instance;
        if (!panel.activeSelf || gm == null) return;

        int level = gm.Level(aktifBina);
        bool hasarli = gm.Hasarli(aktifBina);

        baslikText.text = Ad(aktifBina) + (hasarli ? "  (HASARLI)" : "");
        levelText.text = "Seviye: " + level + " / " + DengeAyarlari.Aktif.maxLevel;
        aciklamaText.text = hasarli ? "Bu yapı afette hasar aldı. Onarılana kadar görevini yapamaz." : Aciklama(aktifBina);

        if (hasarli)
        {
            yukseltButonText.text = "Onar (" + Ekonomi.OnarimMaliyeti(aktifBina) + "₺)";
            yukseltButonu.interactable = Ekonomi.OnarilabilirMi(aktifBina);
        }
        else
        {
            int maliyet = Ekonomi.YukseltmeMaliyeti(aktifBina);
            yukseltButonText.text = maliyet < 0 ? "MAKS SEVİYE" : "Yükselt (" + maliyet + "₺)";
            yukseltButonu.interactable = Ekonomi.YukseltilebilirMi(aktifBina); // para yetmezse ya da maksta gri
        }

        quizButonu.gameObject.SetActive(aktifBina == BinaTuru.Okul && !hasarli);
    }

    public void YukseltTiklandi()
    {
        if (YeniAcildi()) return;

        if (GameManager.Instance != null && GameManager.Instance.Hasarli(aktifBina))
            Ekonomi.Onar(aktifBina);
        else
            Ekonomi.Yukselt(aktifBina);
    }

    public void QuizTiklandi()
    {
        if (YeniAcildi()) return;
        PaneliKapat();
        QuizManager.Instance.QuizAc(); // quiz de zamanı durdurur
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
        var gm = GameManager.Instance;
        switch (b)
        {
            case BinaTuru.TemelBina:
                return "Nüfus kapasitesini belirler. Günlük gelir: " + Ekonomi.GunlukGelir() + "₺ · Kapasite: " + Ekonomi.NufusKapasitesi();
            case BinaTuru.Okul:
                return "Quiz çözerek halkı bilinçlendir. Farkındalık sınırı: %" + DengeAyarlari.Aktif.FarkindalikTavani(gm.okulLevel);
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
