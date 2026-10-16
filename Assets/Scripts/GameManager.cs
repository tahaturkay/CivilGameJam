using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Her yerden GameManager.Instance diyerek ulaşmak için

    [Header("Genel Oyun Verileri")]
    public int gun = 1;
    public int para = 500;
    public int nufus = 1000;
    public float farkindalikYuzdesi = 10f; // 0 ile 100 arası (Okul quizi artırır)

    [Header("Bina Levelleri (En az 1 olur)")]
    public int temelBinaLevel = 1; // Ana gelir kaynağı ve deprem hedefi
    public int okulLevel = 1;      // Quiz açar, depremde hasar alabilir
    public int akutLevel = 1;      // Can kaybını azaltır
    public int itfaiyeLevel = 1;   // Yangını önler
    public int setLevel = 1;       // Tsunamiyi önler
    public int altyapiLevel = 1;   // Seli önler (Yol ve altyapı)

    [Header("Panel ve Durum Kontrolleri")]
    public bool oyunDurduMu = false; // Quiz veya Afet ekranı açıkken zamanı durdurmak için

    [Header("Afet Özet Verileri (UI İçin)")]
    public bool afetOlduMu = false;       // 4. Kişi bu true olunca Afet Paneli'ni açacak
    public string sonAfetMesaji = "";     // "Şiddetli Deprem Oldu! 2 Bina Hasar Aldı." gibi bir metin
    public int sonVefatSayisi = 0;        // O afette ölen kişi sayısı

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Bina leveli düşürme yardımcı fonksiyonu (1'in altına düşmez)
    public int LeveliniBirDusur(int mevcutLevel)
    {
        if (mevcutLevel > 1) return mevcutLevel - 1;
        return 1;
    }
}