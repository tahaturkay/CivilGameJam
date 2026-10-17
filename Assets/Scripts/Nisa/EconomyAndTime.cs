using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Gün sayacı: her gün sonunda gelir eklenir ve nüfus kapasiteye doğru büyür.
// Gün süresi = DengeAyarlari.birGunSuresi / hız (1x, 2x, 3x).
// Para kuralları (yükseltme, onarım, gelir) Ekonomi.cs'de.
public class EconomyAndTime : MonoBehaviour
{
    private float gunSayaci;
    // Counter uses 1x game seconds. Changing speed changes its rate, never the current time of day.
    public float DayProgress => Mathf.Clamp01(gunSayaci / Mathf.Max(.1f,DengeAyarlari.Aktif.birGunSuresi));

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ZamanDurduMu) return;

        // Son gün (tsunami günü) gelince gün ilerlemez; oyun afet raporundan sonra biter
        if (gm.gun >= DengeAyarlari.Aktif.toplamGun) return;

#if UNITY_EDITOR
        if (DebugSpaceBasildi())
        {
            gunSayaci = 0f;
            BirGunIlerlet();
            return;
        }
#endif

        gunSayaci += Time.deltaTime * Mathf.Max(1,gm.hizCarpani);
        float sure = Mathf.Max(.1f,DengeAyarlari.Aktif.birGunSuresi);

        if (gunSayaci >= sure)
        {
            // Her karede en fazla bir gün ilerletilir; afet sistemi günü görebilir.
            gunSayaci -= sure;
            BirGunIlerlet();
        }
    }

#if UNITY_EDITOR
    // Editor shortcut avoids colliding with Space + drag camera navigation.
    private bool DebugSpaceBasildi()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.ctrlKey.isPressed && Keyboard.current.shiftKey.isPressed && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }
#endif

    private void BirGunIlerlet()
    {
        GameManager gm = GameManager.Instance;
        var d = DengeAyarlari.Aktif;

        int kazanc = Ekonomi.GunlukGelir();
        gm.GelirKaydet(kazanc);

        // Nüfus kapasitenin altındaysa göçle büyür (hasarlı konut yeni gelen almaz)
        int kapasite = Ekonomi.NufusKapasitesi();
        if (!gm.temelBinaHasarli && gm.nufus < kapasite)
            gm.nufus = Mathf.Min(kapasite, gm.nufus + d.gunlukGoc);

        gm.gun++;
    }

    // Eski çağrılar bozulmasın diye (yeni kodda Ekonomi.Yukselt kullanılıyor)
    public void TemelBinaYukselt() { Ekonomi.Yukselt(BinaTuru.TemelBina); }
    public void OkulYukselt()      { Ekonomi.Yukselt(BinaTuru.Okul); }
    public void AkutYukselt()      { Ekonomi.Yukselt(BinaTuru.Akut); }
    public void ItfaiyeYukselt()   { Ekonomi.Yukselt(BinaTuru.Itfaiye); }
    public void SetYukselt()       { Ekonomi.Yukselt(BinaTuru.DenizSeti); }
    public void AltyapiYukselt()   { Ekonomi.Yukselt(BinaTuru.Altyapi); }
}
