using System.Collections;
using UnityEngine;

// Her sahneye bir tane konur (MainMenu ve MainScene'de ayrı müzikler çalar).
// Müzik dosyaları sonra Inspector'dan sürüklenir; boş bırakılan alan çalmaz.
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    [Header("Bu sahnenin müziği")]
    public AudioClip normalMuzik;

    [Header("Afet müzikleri (boşsa normal müzik devam eder)")]
    public AudioClip depremMuzigi;
    public AudioClip selMuzigi;
    public AudioClip yanginMuzigi;
    public AudioClip tsunamiMuzigi;

    [Header("Parçalar arası geçiş (saniye)")]
    public float gecisSuresi = 1f;

    AudioSource kaynak;

    void Awake()
    {
        Instance = this;
        kaynak = GetComponent<AudioSource>();
        kaynak.loop = true;
        kaynak.playOnAwake = false;

        // Müziği mixer'ın Muzik kanalına bağla ki ayarlardaki müzik slider'ı etkilesin
        if (Ayarlar.Mixer != null)
        {
            var gruplar = Ayarlar.Mixer.FindMatchingGroups(Ayarlar.MuzikGrubu);
            if (gruplar.Length > 0) kaynak.outputAudioMixerGroup = gruplar[0];
        }
    }

    void Start()
    {
        Ayarlar.SesleriUygula();
        Cal(normalMuzik);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // afetTipi DisasterController ile aynı: 0 Deprem, 1 Sel, 2 Yangın, 3 Tsunami
    public void AfetMuzigiBaslat(int afetTipi)
    {
        AudioClip klip = null;
        switch (afetTipi)
        {
            case 0: klip = depremMuzigi; break;
            case 1: klip = selMuzigi; break;
            case 2: klip = yanginMuzigi; break;
            case 3: klip = tsunamiMuzigi; break;
        }
        if (klip != null) Cal(klip);
    }

    public void NormalMuzigeDon()
    {
        Cal(normalMuzik);
    }

    public void Cal(AudioClip klip)
    {
        if (klip == null) return;
        if (kaynak.clip == klip && kaynak.isPlaying) return;

        StopAllCoroutines();
        StartCoroutine(Gecis(klip));
    }

    IEnumerator Gecis(AudioClip yeniKlip)
    {
        // ESC menüsünde timeScale 0 olsa da geçiş devam etsin diye unscaled zaman kullanılıyor
        float yarim = Mathf.Max(0.01f, gecisSuresi * 0.5f);

        if (kaynak.isPlaying)
        {
            float baslangic = kaynak.volume;
            for (float t = 0; t < yarim; t += Time.unscaledDeltaTime)
            {
                kaynak.volume = Mathf.Lerp(baslangic, 0f, t / yarim);
                yield return null;
            }
        }

        kaynak.clip = yeniKlip;
        kaynak.volume = 0f;
        kaynak.Play();

        for (float t = 0; t < yarim; t += Time.unscaledDeltaTime)
        {
            kaynak.volume = Mathf.Lerp(0f, 1f, t / yarim);
            yield return null;
        }
        kaynak.volume = 1f;
    }
}
