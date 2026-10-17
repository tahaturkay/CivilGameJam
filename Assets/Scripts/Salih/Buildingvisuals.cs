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

    [Header("Bu obje hangi bina?")]
    public BinaTuru binaTuru;

    [Header("Level modelleri (0 = Lvl 1, 1 = Lvl 2, 2 = Lvl 3 ...)")]
    [Tooltip("Her level icin bir child obje surukle. Bos birakirsan asagidaki olceklemeye duser.")]
    public GameObject[] levelModelleri;

    [Header("Model yoksa: kup buyutme (ilk gun icin)")]
    public Transform olceklenecekGorsel; // bos ise bu obje
    public float levelBasinaBuyume = 0.4f; // her level boyu %40 uzatir

    [Header("Opsiyonel: level yazisi (TextMeshPro)")]
    public TMP_Text levelYazisi;

    private int gosterilenLevel = -1;
    private Vector3 baslangicOlcek;

    private void Start()
    {
        if (olceklenecekGorsel == null) olceklenecekGorsel = transform;
        baslangicOlcek = olceklenecekGorsel.localScale;
        if (levelYazisi != null) levelYazisi.gameObject.SetActive(false);
        Guncelle();
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
        if (MevcutLevel() != gosterilenLevel) Guncelle();
    }

    private int MevcutLevel()
    {
        var gm = GameManager.Instance;
        switch (binaTuru)
        {
            case BinaTuru.TemelBina: return gm.temelBinaLevel;
            case BinaTuru.Okul: return gm.okulLevel;
            case BinaTuru.Akut: return gm.akutLevel;
            case BinaTuru.Itfaiye: return gm.itfaiyeLevel;
            case BinaTuru.DenizSeti: return gm.setLevel;
            case BinaTuru.Altyapi: return gm.altyapiLevel;
            default: return 1;
        }
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

    void OnMouseDown()
    {
        Debug.Log("1) Tıklandı: " + name);

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("2) UI tıklamayı engelledi");
            return;
        }

        BinaTiklandi?.Invoke(binaTuru);
        if (BinaPopup.Instance == null) { Debug.LogError("3) BinaPopup sahnede yok!"); return; }

        Debug.Log("4) Popup açılıyor: " + binaTuru);
        BinaPopup.Instance.Ac(binaTuru);
    }
}