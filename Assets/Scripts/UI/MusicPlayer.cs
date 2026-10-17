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

    public void AfetMuzigiBaslat(AfetTuru afet)
    {
        AudioClip klip = null;
        switch (afet)
        {
            case AfetTuru.Deprem:  klip = depremMuzigi; break;
            case AfetTuru.Sel:     klip = selMuzigi; break;
            case AfetTuru.Yangin:  klip = yanginMuzigi; break;
            case AfetTuru.Tsunami: klip = tsunamiMuzigi; break;
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
