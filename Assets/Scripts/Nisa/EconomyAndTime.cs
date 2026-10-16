using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EconomyAndTime : MonoBehaviour
{
    [Header("Zaman")]
    [Min(0.1f)] public float birGunSuresi = 10f;

    private float gunSayaci;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.oyunDurduMu || gm.afetOlduMu) return;

        if (DebugSpaceBasildi())
        {
            gunSayaci = 0f;
            BirGunIlerlet();
            return;
        }

        gunSayaci += Time.deltaTime;
        float sure = Mathf.Max(0.1f, birGunSuresi);

        if (gunSayaci >= sure)
        {
            // Her karede en fazla bir gun ilerletilir; afet sistemi gunu gorebilir.
            gunSayaci -= sure;
            BirGunIlerlet();
        }
    }

    private bool DebugSpaceBasildi()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private void BirGunIlerlet()
    {
        GameManager gm = GameManager.Instance;
        int kazanc = GunlukGeliriHesapla();
        gm.para += kazanc;
        gm.gun++;
        Debug.Log($"Gun: {gm.gun} | Gunluk kazanc: {kazanc} | Toplam para: {gm.para}", this);
    }

    public int GunlukGeliriHesapla()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return 0;

        return (gm.temelBinaLevel * 50)
             + (gm.okulLevel * 20)
             + (gm.akutLevel * 15)
             + (gm.itfaiyeLevel * 15);
    }

    public void TemelBinaYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.temelBinaLevel);
    }

    public void OkulYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.okulLevel);
    }

    public void AkutYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.akutLevel);
    }

    public void ItfaiyeYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.itfaiyeLevel);
    }

    public void SetYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.setLevel);
    }

    public void AltyapiYukselt()
    {
        if (GameManager.Instance == null) return;
        Yukselt(ref GameManager.Instance.altyapiLevel);
    }

    private void Yukselt(ref int level)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.oyunDurduMu || gm.afetOlduMu) return;

        int ucret = level * 100;
        if (gm.para < ucret) return;

        gm.para -= ucret;
        level++;
    }
}
