using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;

// Otomatik oyun testi: Play modunda oyunu kendi kendine oynar, sonuçları Logs/OyunTesti.txt'ye yazar.
// Araclar > Otomatik Oyun Testi menüsünden (ya da Temp/otomatik_test_baslat dosyasıyla) başlatılır.
// Gerçek oyunda hiçbir sahnede bulunmaz.
public class OyunTestRobotu : MonoBehaviour
{
    public const string LogYolu = "Logs/OyunTesti.txt";

    readonly StringBuilder log = new StringBuilder();
    int gecen, kalan;
    float eskiGunSuresi;

    void Start()
    {
        Application.runInBackground = true;
        StartCoroutine(Calistir());
    }

    IEnumerator Calistir()
    {
        Yaz("=== OYUN TESTİ " + DateTime.Now + " · " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " ===");
        yield return null;
        yield return null;

        var gm = GameManager.Instance;
        var d = DengeAyarlari.Aktif;
        eskiGunSuresi = d.birGunSuresi;

        IEnumerator adim = null;
        try { adim = Testler(gm, d); }
        catch (Exception e) { Yaz("HATA: " + e); }

        // Testler sırasında hata çıkarsa da log yazılıp Play'den çıkılsın
        while (adim != null)
        {
            object sonraki;
            try
            {
                if (!adim.MoveNext()) break;
                sonraki = adim.Current;
            }
            catch (Exception e)
            {
                Yaz("HATA (test yarıda kaldı): " + e.Message + "\n" + e.StackTrace);
                break;
            }
            yield return sonraki;
        }

        d.birGunSuresi = eskiGunSuresi; // ScriptableObject Play'den sonra da kalıcı olduğu için geri al
        Time.timeScale = 1f;
        Yaz("\nSONUÇ: " + gecen + " geçti, " + kalan + " kaldı.");
        File.WriteAllText(LogYolu, log.ToString());

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator Testler(GameManager gm, DengeAyarlari d)
    {
        // ---------- 1. Başlangıç ----------
        Yaz("\n[1] Başlangıç değerleri");
        Kontrol(gm != null, "GameManager sahnede var");
        Kontrol(gm.gun == 1 && gm.para == d.baslangicPara && gm.nufus == d.baslangicNufus, "gun=1 para=" + gm.para + " nufus=" + gm.nufus);
        Kontrol(Mathf.Approximately(gm.farkindalikYuzdesi, d.baslangicFarkindalik), "farkındalık=%" + gm.farkindalikYuzdesi);
        var hud = FindAnyObjectByType<UIManager>();
        Kontrol(hud != null, "UIManager (üst bar) var");
        yield return null;
        if (hud != null)
        {
            Yaz("   Üst bar: " + hud.gunText.text + " | " + hud.paraText.text + " | " + hud.nufusText.text + " | " + hud.farkindalikText.text);
            Kontrol(hud.gunText.text.Contains("1/" + d.toplamGun), "Gün yazısı doğru");
        }

        var ipucu = FindAnyObjectByType<IpucuSistemi>();
        Kontrol(ipucu != null, "IpucuSistemi var");
        if (ipucu != null)
        {
            Kontrol(ipucu.serit.activeSelf && ipucu.ipucuText.text.Contains("Binalara"), "İlk ipucu görünüyor: " + ipucu.ipucuText.text);
        }

        // ---------- 2. Hız ve duraklatma ----------
        Yaz("\n[2] Hız butonları");
        var hiz = FindAnyObjectByType<HizKontrol>();
        Kontrol(hiz != null, "HizKontrol var");
        Kontrol(FindAnyObjectByType<EconomyAndTime>() != null, "EconomyAndTime sahnede var");
        hiz.HizAyarla(3);
        int g0 = gm.gun;
        float t0 = Time.time;
        float sinir = Time.realtimeSinceStartup + 10f;
        while (gm.gun == g0 && Time.realtimeSinceStartup < sinir) yield return null;
        float gunSuresi = Time.time - t0;
        Kontrol(gm.gun == g0 + 1, "3x hızda gün ilerledi");
        Kontrol(gunSuresi <= d.birGunSuresi / 3f + 0.5f, "3x'te bir gün ≈ " + gunSuresi.ToString("0.0") + " sn (beklenen ≤ " + (d.birGunSuresi / 3f).ToString("0.0") + ")");

        hiz.DuraklatDegistir();
        g0 = gm.gun;
        yield return new WaitForSecondsRealtime(d.birGunSuresi / 3f + 1f);
        Kontrol(gm.gun == g0, "Duraklat'ta gün durdu");
        hiz.DuraklatDegistir();
        Kontrol(!gm.kullaniciDurdurdu, "Duraklat tekrar basınca devam");

        // Testin geri kalanı hızlı geçsin
        d.birGunSuresi = 0.6f;

        // ---------- 3. Bina popup'ı ve yükseltme ----------
        Yaz("\n[3] Bina popup'ı ve yükseltme");
        var popup = BinaPopup.Instance;
        Kontrol(popup != null, "BinaPopup var");
        gm.para = 1000;
        popup.Ac(BinaTuru.TemelBina);
        g0 = gm.gun;
        yield return new WaitForSecondsRealtime(1.5f);
        Kontrol(popup.panel.activeSelf, "Popup açıldı");
        var kart = popup.panel.transform.Find("Pencere");
        var kartImg = kart != null ? kart.GetComponent<UnityEngine.UI.Image>() : null;
        Kontrol(kartImg != null && kartImg.sprite != null && kartImg.enabled, "Bina popup'ının kart arka planı var");
        Kontrol(gm.oyunDurduMu && gm.gun == g0, "Popup açıkken zaman durdu");
        Yaz("   Buton yazısı: " + popup.yukseltButonText.text);
        Kontrol(popup.yukseltButonText.text.Contains("200"), "Konut Lv1→2 fiyatı 200₺");
        int paraOnce = gm.para;
        popup.YukseltTiklandi();
        yield return null;
        Kontrol(gm.temelBinaLevel == 2 && gm.para == paraOnce - 200, "Konut Lv2 oldu, 200₺ düştü");
        Kontrol(Ekonomi.NufusKapasitesi() == 1300, "Kapasite 1300 oldu");
        popup.YukseltTiklandi();
        yield return null;
        Kontrol(gm.temelBinaLevel == 3, "Konut Lv3 oldu");
        yield return null;
        Yaz("   Buton yazısı: " + popup.yukseltButonText.text);
        Kontrol(popup.yukseltButonText.text.Contains("MAKS") && !popup.yukseltButonu.interactable, "Lv3'te MAKS ve buton kapalı");
        Kontrol(!Ekonomi.Yukselt(BinaTuru.TemelBina) && gm.temelBinaLevel == 3, "Lv3'ün üstüne çıkmıyor");
        popup.Kapat();
        Kontrol(!popup.panel.activeSelf && !gm.oyunDurduMu, "Popup kapandı, zaman devam");
        yield return null;
        if (ipucu != null) Kontrol(ipucu.SiradakiIndex >= 1, "Popup açılınca ilk ipucu geçildi (sıradaki: " + ipucu.SiradakiIndex + ", görünen: " + (ipucu.serit.activeSelf ? ipucu.ipucuText.text : "-") + ")");

        int nufusOnce = gm.nufus;
        int paraOnce2 = gm.para;
        int gelir = Ekonomi.GunlukGelir();
        g0 = gm.gun;
        while (gm.gun == g0) yield return null;
        Kontrol(gm.nufus == Mathf.Min(1600, nufusOnce + d.gunlukGoc), "Nüfus göçle arttı: " + nufusOnce + " → " + gm.nufus);
        Kontrol(gm.para == paraOnce2 + gelir, "Günlük gelir eklendi: +" + gelir);

        // Buradan sonra günler kendi kendine akmasın (yoksa 7. günde yangın araya girer); afetleri elle başlatacağız
        gm.kullaniciDurdurdu = true;

        // ---------- 4. Quiz ----------
        Yaz("\n[4] Quiz");
        var quiz = QuizManager.Instance;
        Kontrol(quiz != null && quiz.sorular.Length > 0, "QuizManager var, soru sayısı: " + (quiz != null ? quiz.sorular.Length : 0));
        gm.okulLevel = 1;
        gm.farkindalikYuzdesi = 10f;
        quiz.QuizAc();
        yield return null;
        Kontrol(quiz.quizPanel.activeSelf && gm.oyunDurduMu, "Quiz açıldı, zaman durdu");
        var qKart = quiz.quizPanel.transform.Find("Pencere");
        var qImg = qKart != null ? qKart.GetComponent<UnityEngine.UI.Image>() : null;
        Kontrol(qImg != null && qImg.sprite != null, "Quiz kartının arka planı var");
        int dogru = quiz.AktifSoru.dogruSikIndex;
        quiz.sikButonlari[dogru].onClick.Invoke();
        Kontrol(Mathf.Approximately(gm.farkindalikYuzdesi, 20f), "Doğru cevap: farkındalık %10 → %" + gm.farkindalikYuzdesi);
        yield return new WaitForSecondsRealtime(2.5f);
        Kontrol(quiz.quizPanel.activeSelf, "Cevaptan sonra quiz açık kalıyor (açıklama okunabilir)");
        quiz.QuizKapat();
        Kontrol(!quiz.quizPanel.activeSelf && !gm.oyunDurduMu, "Quiz kapatılınca zaman devam ediyor");

        quiz.QuizAc();
        yield return null;
        int yanlis = (quiz.AktifSoru.dogruSikIndex + 1) % quiz.AktifSoru.siklar.Length;
        quiz.sikButonlari[yanlis].onClick.Invoke();
        Kontrol(Mathf.Approximately(gm.farkindalikYuzdesi, 20f), "Yanlış cevap: farkındalık değişmedi");
        Yaz("   Yanlış cevap mesajı: " + quiz.soruText.text);
        quiz.QuizKapat();

        gm.farkindalikYuzdesi = 40f;
        quiz.QuizAc();
        yield return null;
        Yaz("   Tavan mesajı: " + quiz.soruText.text.Replace("\n", " "));
        Kontrol(!quiz.sikButonlari[0].gameObject.activeSelf, "Okul Lv1'de %40 tavanında soru sorulmadı");
        quiz.QuizKapat();
        Kontrol(Mathf.Approximately(gm.FarkindalikEkle(10f), 0f), "FarkindalikEkle tavanı aşmıyor");

        // ---------- 5. Afet ve rapor ----------
        Yaz("\n[5] Yangın (savunma yetersiz) ve rapor");
        var afet = FindAnyObjectByType<DisasterController>();
        var rapor = FindAnyObjectByType<AfetRaporu>();
        Kontrol(afet != null && rapor != null, "DisasterController ve AfetRaporu var");
        gm.itfaiyeLevel = 1; gm.temelBinaLevel = 2; gm.okulLevel = 1; gm.akutLevel = 1;
        gm.farkindalikYuzdesi = 40f;
        int nufus0 = gm.nufus;
        int beklenenVefat = Mathf.RoundToInt(120f * (1f - 40f * d.farkindalikEtkisi) * (1f - 1 * d.akutEtkisi));
        Time.timeScale = 6f;
        afet.AfetBaslat(0);
        sinir = Time.realtimeSinceStartup + 30f;
        int itfaiyedeAtes = 0, toplamAtes = 0;
        Transform itf = null;
        foreach (var bv in FindObjectsByType<BuildingVisuals>())
            if (bv.binaTuru == BinaTuru.Itfaiye) { itf = bv.transform; break; }
        while (!gm.afetOlduMu && Time.realtimeSinceStartup < sinir)
        {
            foreach (Transform fx in afet.GetComponentsInChildren<Transform>())
            {
                if (fx.name != "KatmanliYangin") continue;
                toplamAtes++;
                if (itf != null && Vector2.Distance(new Vector2(fx.position.x, fx.position.z), new Vector2(itf.position.x, itf.position.z)) < 1.5f) itfaiyedeAtes++;
            }
            yield return null;
        }
        Kontrol(toplamAtes > 0 && (itf == null || itfaiyedeAtes == 0), "İtfaiye binasında ateş yok (görülen ateş karesi: " + toplamAtes + ", itfaiye " + (itf != null ? "bulundu" : "sahnede yok") + ")");
        Time.timeScale = 1f;
        Kontrol(gm.afetOlduMu, "Afet bitti, rapor bayrağı kalktı");
        yield return null;
        Kontrol(rapor.panel.activeSelf, "Afet raporu açıldı");
        Kontrol(!gm.sonSavunmaYeterli && gm.sonAfetTuru == AfetTuru.Yangin, "Rapor: Yangın, savunma yetersiz");
        Kontrol(gm.sonVefatSayisi == beklenenVefat, "Vefat " + gm.sonVefatSayisi + " (beklenen " + beklenenVefat + "), kurtarılan " + gm.sonKurtarilanSayisi);
        Kontrol(gm.nufus == nufus0 - gm.sonVefatSayisi, "Nüfus düştü");
        Kontrol(gm.temelBinaLevel == 1 && gm.okulHasarli, "Konut 2→1, okul (Lv1) hasarlı oldu");
        Yaz("   Rapor: " + rapor.baslikText.text + " | " + rapor.savunmaText.text + " | " + rapor.vefatText.text);
        g0 = gm.gun;
        yield return new WaitForSecondsRealtime(1f);
        Kontrol(gm.gun == g0, "Rapor açıkken zaman durdu");
        rapor.Devam();
        yield return null;
        Kontrol(!gm.afetOlduMu && !rapor.panel.activeSelf && !gm.oyunBittiMi, "Devam: oyun sürüyor");

        // ---------- 6. Hasarlı bina ve onarım ----------
        Yaz("\n[6] Hasarlı bina ve onarım");
        gm.para = 1000;
        popup.Ac(BinaTuru.Okul);
        yield return new WaitForSecondsRealtime(0.3f);
        Yaz("   Buton yazısı: " + popup.yukseltButonText.text);
        Kontrol(popup.yukseltButonText.text.Contains("Onar") && !popup.quizButonu.gameObject.activeSelf, "Hasarlı okulda Onar butonu, quiz kapalı");
        Kontrol(!Ekonomi.Yukselt(BinaTuru.Okul), "Hasarlıyken yükseltilemiyor");
        paraOnce = gm.para;
        popup.YukseltTiklandi();
        Kontrol(!gm.okulHasarli && gm.para == paraOnce - d.OnarimMaliyeti(BinaTuru.Okul), "Onarıldı, " + d.OnarimMaliyeti(BinaTuru.Okul) + "₺ düştü");
        popup.Kapat();

        // ---------- 7. Savunma yeterliyken ----------
        Yaz("\n[7] Sel (savunma yeterli)");
        gm.altyapiLevel = 2;
        Time.timeScale = 6f;
        afet.AfetBaslat(1);
        sinir = Time.realtimeSinceStartup + 30f;
        while (!gm.afetOlduMu && Time.realtimeSinceStartup < sinir) yield return null;
        Time.timeScale = 1f;
        int beklenen2 = Mathf.RoundToInt(150f * d.savunmaYeterliCarpani * (1f - gm.farkindalikYuzdesi * d.farkindalikEtkisi) * (1f - gm.akutLevel * d.akutEtkisi));
        Kontrol(gm.sonSavunmaYeterli && gm.sonHasarliBinaSayisi == 0, "Sel: savunma yeterli, hasar yok");
        Kontrol(gm.sonVefatSayisi == beklenen2, "Vefat " + gm.sonVefatSayisi + " (beklenen " + beklenen2 + ")");
        yield return null;
        rapor.Devam();

        // ---------- 8. Son gün ve oyun sonu ----------
        Yaz("\n[8] 28. gün: tsunami ve oyun sonu");
        var son = FindAnyObjectByType<OyunSonu>();
        Kontrol(son != null, "OyunSonu var");
        gm.gun = d.toplamGun - 1;
        gm.kullaniciDurdurdu = false;
        Time.timeScale = 6f;
        sinir = Time.realtimeSinceStartup + 40f;
        while (!gm.afetOlduMu && Time.realtimeSinceStartup < sinir) yield return null;
        Time.timeScale = 1f;
        Kontrol(gm.gun == d.toplamGun && gm.sonAfetTuru == AfetTuru.Tsunami, "28. gün tsunami kendiliğinden geldi");
        yield return null;
        rapor.Devam();
        yield return null;
        yield return null;
        Kontrol(gm.oyunBittiMi && gm.kazandiMi, "Oyun bitti, kazanıldı");
        Kontrol(son.panel.activeSelf, "Oyun sonu ekranı açıldı");
        Yaz("   " + son.baslikText.text + " | " + son.dereceText.text);
        Yaz("   " + son.istatistikText.text.Replace("\n", " | "));
        Yaz("   " + son.afetOzetiText.text.Replace("\n", " | "));
        int g28 = gm.gun;
        yield return new WaitForSecondsRealtime(1f);
        Kontrol(gm.gun == g28, "Oyun bitince gün ilerlemiyor");
    }

    void Kontrol(bool sart, string aciklama)
    {
        if (sart) gecen++; else kalan++;
        Yaz((sart ? "  OK   " : "  HATA ") + aciklama);
    }

    void Yaz(string satir)
    {
        log.AppendLine(satir);
        Debug.Log("[OyunTesti] " + satir);
    }
}
