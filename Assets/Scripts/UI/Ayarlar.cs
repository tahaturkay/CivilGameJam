using UnityEngine;
using UnityEngine.Audio;

// Oyun ayarlarını kaydeder (PlayerPrefs), yükler ve uygular. Her sahneden Ayarlar.MuzikSesi gibi ulaşılır.
// Ses için mixer şurada olmalı: Assets/Resources/AnaMixer.mixer
// Mixer'da "Muzik" ve "Efekt" grupları, bunların Volume'ü de "MuzikSesi" ve "EfektSesi" adıyla expose edilmiş olmalı.
public static class Ayarlar
{
    public const string MuzikParametresi = "MuzikSesi";
    public const string EfektParametresi = "EfektSesi";
    public const string MuzikGrubu = "Muzik";
    public const string EfektGrubu = "Efekt";

    const string MuzikKey = "ayar_muzik";
    const string EfektKey = "ayar_efekt";
    const string TamEkranKey = "ayar_tamekran";

    static AudioMixer mixer;
    static bool mixerArandi;

    public static AudioMixer Mixer
    {
        get
        {
            if (!mixerArandi)
            {
                mixerArandi = true;
                mixer = Resources.Load<AudioMixer>("AnaMixer");
                if (mixer == null)
                    Debug.LogWarning("Ayarlar: Assets/Resources/AnaMixer.mixer bulunamadı, ses ayarları uygulanmayacak.");
            }
            return mixer;
        }
    }

    // 0 ile 1 arası
    public static float MuzikSesi
    {
        get { return PlayerPrefs.GetFloat(MuzikKey, 0.8f); }
        set { PlayerPrefs.SetFloat(MuzikKey, value); SesUygula(MuzikParametresi, value); }
    }

    // 0 ile 1 arası
    public static float EfektSesi
    {
        get { return PlayerPrefs.GetFloat(EfektKey, 0.8f); }
        set { PlayerPrefs.SetFloat(EfektKey, value); SesUygula(EfektParametresi, value); }
    }

    public static bool TamEkran
    {
        get { return PlayerPrefs.GetInt(TamEkranKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(TamEkranKey, value ? 1 : 0);
            TamEkranUygula(value);
        }
    }

    // Sadece modu değiştirmek yetmiyor: pencere ekran boyutunda kalırsa fark görünmez.
    // Bu yüzden tam ekranda monitör boyutuna, pencerede ekranın %75'i kadar 16:9 bir boyuta geçiyoruz.
    // Not: Editörde etkisi yoktur, build alınmış oyunda çalışır.
    static void TamEkranUygula(bool tamEkran)
    {
        int ekranGenislik = Display.main.systemWidth;
        int ekranYukseklik = Display.main.systemHeight;

        if (tamEkran)
        {
            Screen.SetResolution(ekranGenislik, ekranYukseklik, FullScreenMode.FullScreenWindow);
        }
        else
        {
            int genislik = Mathf.RoundToInt(ekranGenislik * 0.75f);
            int yukseklik = Mathf.RoundToInt(genislik * 9f / 16f);
            if (yukseklik > ekranYukseklik * 0.85f)
            {
                yukseklik = Mathf.RoundToInt(ekranYukseklik * 0.85f);
                genislik = Mathf.RoundToInt(yukseklik * 16f / 9f);
            }
            Screen.SetResolution(genislik, yukseklik, FullScreenMode.Windowed);
        }
    }

    public static void Kaydet()
    {
        PlayerPrefs.Save();
    }

    // Oyun açılınca ilk sahne yüklendiğinde kendiliğinden çalışır
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OyunAcilinca()
    {
        SesleriUygula();
        TamEkranUygula(TamEkran);
    }

    public static void SesleriUygula()
    {
        SesUygula(MuzikParametresi, MuzikSesi);
        SesUygula(EfektParametresi, EfektSesi);
    }

    // Mixer desibel ile çalışır: 1 = 0 dB, 0 = -80 dB (sessiz)
    static void SesUygula(string parametre, float deger)
    {
        if (Mixer == null) return;
        float db = deger <= 0.0001f ? -80f : Mathf.Log10(deger) * 20f;
        Mixer.SetFloat(parametre, db);
    }
}
