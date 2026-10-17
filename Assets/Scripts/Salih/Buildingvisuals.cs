using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum BinaTuru { TemelBina, Okul, Akut, Itfaiye, DenizSeti, Altyapi }

/// <summary>
/// Bir binanin gorselini GameManager'daki level'a gore gunceller ve tiklamayi yakalar.
/// GameManager'a HICBIR sey yazmaz, sadece okur.
/// Her bina objesine (Collider ile birlikte) eklenir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BuildingVisuals : MonoBehaviour
{
    // 4. kisi (UI) bunu dinleyerek paneli acar:
    // BuildingVisuals.BinaTiklandi += (tur) => { ... };
    public static event Action<BinaTuru> BinaTiklandi;

    // Domain Reload kapalıyken eski Play'den kalan abonelikler silinsin
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StatikSifirla() { BinaTiklandi = null; }

    [Header("Bu obje hangi bina?")]
    public BinaTuru binaTuru;

    [Header("Level modelleri (0 = Lvl 1, 1 = Lvl 2, 2 = Lvl 3 ...)")]
    [Tooltip("Her level icin bir child obje surukle. Bos birakirsan asagidaki olceklemeye duser.")]
    public GameObject[] levelModelleri;

    [Tooltip("Aktif level modelinin sınırlarına göre kök BoxCollider'ı günceller.")]
    public bool colliderModeleUysun = true;

    [Header("Model yoksa: kup buyutme (ilk gun icin)")]
    public Transform olceklenecekGorsel; // bos ise bu obje
    public float levelBasinaBuyume = 0.4f; // her level boyu %40 uzatir

    [Header("Opsiyonel: level yazisi (TextMeshPro)")]
    public TMP_Text levelYazisi;

    private int gosterilenLevel = -1;
    private Vector3 baslangicOlcek;
    private GameManager dinlenenManager;
    private float vurguZamani = -1f;
    private Transform vurguModeli;
    private Vector3 vurguBaslangicOlcek;
    public float VurguBeyazligi { get; private set; }

    private void Start()
    {
        if (olceklenecekGorsel == null) olceklenecekGorsel = transform;
        baslangicOlcek = olceklenecekGorsel.localScale;
        if (levelYazisi != null) levelYazisi.gameObject.SetActive(false);
        Guncelle();
        ManagerDinle();
    }

    private void OnMouseEnter()
    {
        if (levelYazisi != null) levelYazisi.gameObject.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (levelYazisi != null) levelYazisi.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        ManagerDinle();
        if (MevcutLevel() != gosterilenLevel) Guncelle();
        if (vurguZamani >= 0)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - vurguZamani) / .2f);
            float punch = Mathf.Sin(t * Mathf.PI) * (1f - t * .3f);
            if (vurguModeli != null) vurguModeli.localScale = vurguBaslangicOlcek * (1f + .09f * punch);
            VurguBeyazligi = .55f * punch;
            if (t >= 1) VurguyuBitir();
        }
    }

    private void ManagerDinle()
    {
        if (dinlenenManager == GameManager.Instance) return;
        if (dinlenenManager != null) dinlenenManager.YapiIslemiTamamlandi -= YapiIslemi;
        dinlenenManager = GameManager.Instance;
        if (dinlenenManager != null) dinlenenManager.YapiIslemiTamamlandi += YapiIslemi;
    }

    private void YapiIslemi(BinaTuru tur)
    {
        if (tur != binaTuru) return;
        VurguyuBitir();
        Guncelle();
        vurguModeli = levelModelleri != null && levelModelleri.Length > 0
            ? levelModelleri[Mathf.Clamp(MevcutLevel() - 1, 0, levelModelleri.Length - 1)].transform : olceklenecekGorsel;
        if (vurguModeli != null) vurguBaslangicOlcek = vurguModeli.localScale;
        vurguZamani = Time.unscaledTime;
    }

    private void VurguyuBitir()
    {
        if (vurguModeli != null) vurguModeli.localScale = vurguBaslangicOlcek;
        vurguModeli = null;
        vurguZamani = -1;
        VurguBeyazligi = 0;
    }

    private void OnDisable() { VurguyuBitir(); }
    private void OnDestroy()
    {
        if (dinlenenManager != null) dinlenenManager.YapiIslemiTamamlandi -= YapiIslemi;
    }

    private int MevcutLevel()
    {
        var gm = GameManager.Instance;
        return gm != null ? gm.Level(binaTuru) : 1;
    }

    private void Guncelle()
    {
        if (GameManager.Instance == null) return;

        int level = Mathf.Max(1, MevcutLevel());
        gosterilenLevel = level;

        if (levelModelleri != null && levelModelleri.Length > 0)
        {
            // Level modelden fazlaysa son modelde kalir
            int aktifIndex = Mathf.Clamp(level - 1, 0, levelModelleri.Length - 1);
            for (int i = 0; i < levelModelleri.Length; i++)
            {
                if (levelModelleri[i] != null)
                    levelModelleri[i].SetActive(i == aktifIndex);
            }
            if (colliderModeleUysun && levelModelleri[aktifIndex] != null)
                ColliderGuncelle(levelModelleri[aktifIndex]);
        }
        else
        {
            // Model yoksa: kupu yukari dogru uzat
            float carpan = 1f + (level - 1) * levelBasinaBuyume;
            olceklenecekGorsel.localScale = new Vector3(
                baslangicOlcek.x,
                baslangicOlcek.y * carpan,
                baslangicOlcek.z);
        }

        if (levelYazisi != null)
            levelYazisi.text = "Lvl " + level;
    }

    // Kök sabit kalır; afet referansları ve tıklama hedefi model değişince kaybolmaz.
    private void ColliderGuncelle(GameObject model)
    {
        BoxCollider kutu = GetComponent<BoxCollider>();
        if (kutu == null) return;

        Bounds sinir = default;
        bool bulundu = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Bounds yerel = renderer.localBounds;
            Matrix4x4 matris = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int k = 0; k < 8; k++)
            {
                Vector3 kose = yerel.center + Vector3.Scale(yerel.extents,
                    new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                Vector3 nokta = matris.MultiplyPoint3x4(kose);
                if (!bulundu) { sinir = new Bounds(nokta, Vector3.zero); bulundu = true; }
                else sinir.Encapsulate(nokta);
            }
        }
        if (!bulundu) return;
        kutu.center = sinir.center;
        kutu.size = sinir.size + Vector3.one * .08f;
    }

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        var gm = GameManager.Instance;
        if (PauseMenu.DurdurulduMu || (gm != null && (gm.afetSuruyor || gm.afetOlduMu || gm.oyunBittiMi))) return;

        BinaTiklandi?.Invoke(binaTuru);
        if (BinaPopup.Instance == null) { Debug.LogError("3) BinaPopup sahnede yok!"); return; }

        BinaPopup.Instance.Ac(binaTuru);
    }
}
