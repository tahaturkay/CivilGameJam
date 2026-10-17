using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisasterController : MonoBehaviour
{
    [Header("Temel Bağlantılar")]
    public Transform kamera;
    public Transform tsunamiSuyu;
    [Header("Ada ve dalga")]
    public Transform tsunamiDalgasi;
    public float tsunamiSetZ = 10f, tsunamiBaslangicZ = -45f, tsunamiBitisZ = 102f;
    public float tsunamiDalgaY = 1f, tsunamiSuYuksekligi = 3.2f, selSuYuksekligi = 2.82f;
    public float kuruZeminY = 2.4f;
    [Min(1)] public float afetSuresi = 15f;
    public GameObject yanginEfekti, yagmurEfekti, yanginPrefab, tozBulutuPrefab;
    public Transform[] yanacakBinalar, temelBinalar;
    public Transform okulObj, akutObj, itfaiyeObj;
    public bool AfetDevamEdiyorMu { get; private set; }
    public AfetTuru AktifAfetTuru { get; private set; }
    public bool SavunmaYeterli { get; private set; }

    int sonKontrolEdilenGun;
    readonly HashSet<int> gerceklesenAfetler = new HashSet<int>();
    readonly List<GameObject> geciciEfektler = new List<GameObject>();
    MaterialPropertyBlock suBlock;
    Renderer suRenderer;
    Vector3 kameraIlk, dalgaOlcek = Vector3.one;
    bool kameraSallaniyor;

    void Update()
    {
        var gm = GameManager.Instance;
        if (AfetDevamEdiyorMu || gm == null || gm.afetOlduMu || gm.oyunBittiMi || gm.oyunDurduMu || gm.kullaniciDurdurdu) return;
        if (gm.gun != sonKontrolEdilenGun)
        {
            sonKontrolEdilenGun = gm.gun;
            int index = DengeAyarlari.Aktif.BuGunkuAfetIndex(gm.gun);
            if (index >= 0 && !gerceklesenAfetler.Contains(index)) AfetBaslat(index);
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1)) AfetBaslat(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AfetBaslat(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AfetBaslat(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) AfetBaslat(3);
#endif
    }

    public void AfetBaslat(int index)
    {
        var gm = GameManager.Instance;
        var afetler = DengeAyarlari.Aktif.afetler;
        if (AfetDevamEdiyorMu || gm == null || gm.oyunBittiMi || gm.afetOlduMu || index < 0 || index >= afetler.Length) return;
        gerceklesenAfetler.Add(index);
        StartCoroutine(AfetSureciYonet(afetler[index]));
    }

    IEnumerator AfetSureciYonet(DengeAyarlari.AfetAyari afet)
    {
        var gm = GameManager.Instance;
        var d = DengeAyarlari.Aktif;
        AfetDevamEdiyorMu = gm.afetSuruyor = true;
        AktifAfetTuru = afet.afet;
        var yetersizler = new List<BinaTuru>();
        foreach (BinaTuru b in afet.kontrolEdilen)
            if (gm.Level(b) < afet.gerekenLevel || gm.Hasarli(b)) yetersizler.Add(b);
        SavunmaYeterli = yetersizler.Count == 0;
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.AfetMuzigiBaslat(afet.afet);
        try
        {
            switch (afet.afet)
            {
                case AfetTuru.Deprem:
                    StartCoroutine(KameraSalla(afetSuresi, .4f));
                    yield return new WaitForSeconds(afetSuresi / 3);
                    if (yetersizler.Contains(BinaTuru.TemelBina) && temelBinalar != null)
                        foreach (Transform b in temelBinalar) TozEfekti(b);
                    if (yetersizler.Contains(BinaTuru.Okul)) TozEfekti(okulObj);
                    if (yetersizler.Contains(BinaTuru.Akut)) TozEfekti(akutObj);
                    if (yetersizler.Contains(BinaTuru.Itfaiye)) TozEfekti(itfaiyeObj);
                    yield return new WaitForSeconds(afetSuresi * 2 / 3);
                    break;
                case AfetTuru.Yangin:
                    yield return YanginEfektiOynat(!SavunmaYeterli);
                    break;
                case AfetTuru.Sel:
                    if (yagmurEfekti != null) yagmurEfekti.SetActive(true);
                    if (!SavunmaYeterli) yield return SuYukseltIndir(selSuYuksekligi, afetSuresi);
                    else yield return new WaitForSeconds(afetSuresi);
                    break;
                case AfetTuru.Tsunami:
                    yield return TsunamiEfekti(SavunmaYeterli);
                    break;
            }
            var hasarAlanlar = afet.afet == AfetTuru.Deprem ? yetersizler :
                (SavunmaYeterli ? new List<BinaTuru>() : new List<BinaTuru>(afet.hasarAlan));
            foreach (BinaTuru b in hasarAlanlar) gm.HasarVer(b, afet.afet);
            float baz = afet.bazKayip * (SavunmaYeterli ? d.savunmaYeterliCarpani : 1f);
            int akutEtkiLevel = gm.akutHasarli ? 0 : gm.akutLevel;
            float koruma = (1f - gm.farkindalikYuzdesi * d.farkindalikEtkisi) * (1f - akutEtkiLevel * d.akutEtkisi);
            int vefat = Mathf.Min(gm.nufus, Mathf.RoundToInt(baz * Mathf.Clamp01(koruma)));
            int kurtarilan = Mathf.Max(0, Mathf.RoundToInt(baz) - vefat);
            gm.nufus -= vefat;
            gm.sonAfetTuru = afet.afet;
            gm.sonSavunmaYeterli = SavunmaYeterli;
            gm.sonHasarliBinaSayisi = hasarAlanlar.Count;
            gm.sonVefatSayisi = vefat;
            gm.sonKurtarilanSayisi = kurtarilan;
            gm.toplamVefat += vefat;
            gm.toplamKurtarilan += kurtarilan;
            gm.sonAfetMesaji = SavunmaYeterli ? "Önlemler işe yaradı!" : "Savunma yetersiz kaldı.";
            gm.afetOzeti += AfetRaporu.AfetAdi(afet.afet) + ": " + (SavunmaYeterli ? "Korundu" : "Hasar aldı") + " (" + vefat + " vefat)\n";
            gm.afetOlduMu = true;
        }
        finally
        {
            EfektleriTemizle();
            AfetDevamEdiyorMu = gm.afetSuruyor = false;
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.NormalMuzigeDon();
        }
    }

    void TozEfekti(Transform bina)
    {
        if (bina == null) return;
        geciciEfektler.Add(IslandEffectsFactory.CreateDust(transform, bina.position + Vector3.up * .4f, 1.5f));
    }

    IEnumerator KameraSalla(float sure, float siddet)
    {
        if (kamera == null) yield break;
        kameraIlk = kamera.localPosition; kameraSallaniyor = true;
        for (float t = 0; t < sure; t += Time.deltaTime)
        {
            kamera.localPosition = kameraIlk + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * siddet * (1 - t / sure);
            yield return null;
        }
        kamera.localPosition = kameraIlk; kameraSallaniyor = false;
    }

    IEnumerator YanginEfektiOynat(bool hasar)
    {
        int count = yanacakBinalar == null ? 0 : yanacakBinalar.Length;
        float elapsed = 0;
        for (int i = 0; i < count; i++)
        {
            Transform bina = yanacakBinalar[i];
            if (bina != null && bina != itfaiyeObj)
            {
                Collider col = bina.GetComponent<Collider>();
                Vector3 p = bina.position; float size = 1;
                if (col != null) { p.y = col.bounds.max.y - .15f; size = Mathf.Clamp(col.bounds.size.x * .16f, .7f, 1.8f); }
                geciciEfektler.Add(IslandEffectsFactory.CreateFire(transform, p, size));
            }
            float interval = afetSuresi / 3 / Mathf.Max(1, count);
            yield return new WaitForSeconds(interval); elapsed += interval;
        }
        yield return new WaitForSeconds(Mathf.Max(0, afetSuresi * 2 / 3 - elapsed));
        if (!hasar && geciciEfektler.Count > 0)
        {
            float interval = afetSuresi / 3 / geciciEfektler.Count;
            for (int i = 0; i < geciciEfektler.Count; i++)
            {
                if (geciciEfektler[i] != null) geciciEfektler[i].SetActive(false);
                yield return new WaitForSeconds(interval);
            }
        }
        else yield return new WaitForSeconds(afetSuresi / 3);
    }

    void SuAyarla(float y, float revealZ = 10000)
    {
        if (tsunamiSuyu == null) return;
        tsunamiSuyu.gameObject.SetActive(true);
        Vector3 p = tsunamiSuyu.position; p.y = y; tsunamiSuyu.position = p;
        if (suRenderer == null) suRenderer = tsunamiSuyu.GetComponent<Renderer>();
        if (suRenderer != null) { if (suBlock == null) suBlock = new MaterialPropertyBlock(); suBlock.SetFloat("_RevealZ", revealZ); suRenderer.SetPropertyBlock(suBlock); }
    }

    IEnumerator SuYukseltIndir(float target, float sure)
    {
        for (float t = 0; t < sure; t += Time.deltaTime)
        {
            float u = t / sure;
            float height = u < .32f ? Mathf.SmoothStep(kuruZeminY - .3f, target, u / .32f) :
                u > .78f ? Mathf.SmoothStep(target, kuruZeminY - .3f, (u - .78f) / .22f) : target;
            SuAyarla(height);
            yield return null;
        }
    }

    IEnumerator TsunamiEfekti(bool korundu)
    {
        if (tsunamiDalgasi != null) { dalgaOlcek = tsunamiDalgasi.localScale; tsunamiDalgasi.gameObject.SetActive(true); }
        float travelTime = afetSuresi * .72f;
        float impactTime = travelTime * Mathf.InverseLerp(tsunamiBaslangicZ, tsunamiBitisZ, tsunamiSetZ);
        for (float t = 0; t < afetSuresi; t += Time.deltaTime)
        {
            float z = Mathf.Lerp(tsunamiBaslangicZ, tsunamiBitisZ, t / travelTime);
            float waveHeight = 1, y = tsunamiDalgaY;
            if (korundu && t >= impactTime)
            {
                float melt = Mathf.SmoothStep(0, 1, (t - impactTime) / 2.5f);
                z = tsunamiSetZ - melt * 9; y -= melt * 3;
                waveHeight = 1 - melt;
            }
            else if (!korundu)
            {
                // An advancing, transparent water sheet follows the wave inside the seawall polygon.
                float rise = Mathf.SmoothStep(0, 1, (t - impactTime) / 1.8f);
                float recede = Mathf.SmoothStep(0, 1, (t - travelTime) / Mathf.Max(.1f, afetSuresi - travelTime));
                SuAyarla(Mathf.Lerp(kuruZeminY - .3f, tsunamiSuYuksekligi, rise * (1 - recede)), z - 3);
            }
            if (tsunamiDalgasi != null)
            {
                tsunamiDalgasi.position = new Vector3(tsunamiDalgasi.position.x, y, z);
                tsunamiDalgasi.localScale = Vector3.Scale(dalgaOlcek, new Vector3(1, Mathf.Max(.001f, waveHeight), 1));
                if (t >= travelTime || waveHeight <= .001f) tsunamiDalgasi.gameObject.SetActive(false);
            }
            yield return null;
        }
    }

    void EfektleriTemizle()
    {
        foreach (GameObject go in geciciEfektler) if (go != null) { go.SetActive(false); Destroy(go); }
        geciciEfektler.Clear();
        if (tsunamiSuyu != null) tsunamiSuyu.gameObject.SetActive(false);
        if (tsunamiDalgasi != null) { tsunamiDalgasi.gameObject.SetActive(false); tsunamiDalgasi.localScale = dalgaOlcek; }
        if (yagmurEfekti != null) yagmurEfekti.SetActive(false);
        if (kameraSallaniyor && kamera != null) kamera.localPosition = kameraIlk;
        kameraSallaniyor = false;
    }

    void OnDisable()
    {
        StopAllCoroutines(); EfektleriTemizle(); AfetDevamEdiyorMu = false;
        if (GameManager.Instance != null) GameManager.Instance.afetSuruyor = false;
    }
}
