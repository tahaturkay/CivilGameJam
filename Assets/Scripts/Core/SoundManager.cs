using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// CivilGameJam merkezi ses efektleri (SFX) ve çevre ambiyansı yöneticisi.
/// Tüm sesler Ayarlar.Mixer'ın "Efekt" kanalına bağlıdır.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("SFX Klipleri (Resources/Audio/SFX)")]
    public AudioClip quizDogruClip;
    public AudioClip quizYanlisClip;
    public AudioClip afetRaporuClip;
    public AudioClip oyunKazanmaClip;
    public AudioClip atesClip;
    public AudioClip dalgaClip;
    public AudioClip sirenClip;
    public AudioClip upgradeClip;
    public AudioClip simsekClip;
    public AudioClip yagmurVeGokGurultusuClip;

    private AudioSource sfxSource;
    private AudioSource dalgaSource;
    private AudioSource sirenSource;
    private AudioSource atesSource;
    private AudioSource yagmurSource;

    private AudioMixerGroup efektGrubu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SoundManager");
            go.AddComponent<SoundManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BaglaMixer();
        YukleKlipler();
        OlusturKaynaklar();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void Start()
    {
        SahneyeGoreAmbiyansAyarla(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SahneyeGoreAmbiyansAyarla(scene.name);
    }

    private void BaglaMixer()
    {
        if (Ayarlar.Mixer != null)
        {
            var gruplar = Ayarlar.Mixer.FindMatchingGroups(Ayarlar.EfektGrubu);
            if (gruplar != null && gruplar.Length > 0)
                efektGrubu = gruplar[0];
        }
    }

    private void YukleKlipler()
    {
        if (quizDogruClip == null) quizDogruClip = Resources.Load<AudioClip>("Audio/SFX/quiz_dogru_sesi");
        if (quizYanlisClip == null) quizYanlisClip = Resources.Load<AudioClip>("Audio/SFX/quiz_yanlis_sesi");
        if (afetRaporuClip == null) afetRaporuClip = Resources.Load<AudioClip>("Audio/SFX/afet_raporu_bildirim");
        if (oyunKazanmaClip == null) oyunKazanmaClip = Resources.Load<AudioClip>("Audio/SFX/oyun_kazanma_sesi");
        if (atesClip == null) atesClip = Resources.Load<AudioClip>("Audio/SFX/ates");
        if (dalgaClip == null) dalgaClip = Resources.Load<AudioClip>("Audio/SFX/dalga");
        if (sirenClip == null) sirenClip = Resources.Load<AudioClip>("Audio/SFX/siren");
        if (upgradeClip == null) upgradeClip = Resources.Load<AudioClip>("Audio/SFX/upgrade");
        if (simsekClip == null) simsekClip = Resources.Load<AudioClip>("Audio/SFX/simsek");
        if (yagmurVeGokGurultusuClip == null) yagmurVeGokGurultusuClip = Resources.Load<AudioClip>("Audio/SFX/yagmurvegokgurultusu");
    }

    private AudioSource KaynakEkle(string isim, bool loop, float sesSeviyesi)
    {
        GameObject child = new GameObject("AudioSource_" + isim);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.loop = loop;
        src.volume = sesSeviyesi;
        src.playOnAwake = false;
        if (efektGrubu != null) src.outputAudioMixerGroup = efektGrubu;
        return src;
    }

    private void OlusturKaynaklar()
    {
        sfxSource = KaynakEkle("SFX_OneShot", false, 1.0f);
        dalgaSource = KaynakEkle("Dalga_Ambiyans", true, 0.22f); // Hafif arka plan dalgası
        sirenSource = KaynakEkle("Siren", true, 0.75f);
        atesSource = KaynakEkle("Ates_Afeti", true, 0.70f);
        yagmurSource = KaynakEkle("Yagmur_GokGurultusu", true, 0.75f);
    }

    private void SahneyeGoreAmbiyansAyarla(string sahneAdi)
    {
        // Ana menüdeyken ada dalgası ve afet seslerini durdur, oyun sahnesinde dalgayı başlat
        if (sahneAdi == "MainMenu")
        {
            DalgaDurdur();
            TumAfetSesleriniDurdur();
        }
        else
        {
            DalgaBaslat();
        }
    }

    // ---------- Genel SFX Çalma ----------

    public void CalOneShot(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ---------- Özel Olay Sesleri ----------

    /// <summary>Oyun boyunca sürekli arkadan hafif düzeyde gelen dalga sesi</summary>
    public void DalgaBaslat()
    {
        if (dalgaSource == null || dalgaClip == null) return;
        if (!dalgaSource.isPlaying)
        {
            dalgaSource.clip = dalgaClip;
            dalgaSource.Play();
        }
    }

    public void DalgaDurdur()
    {
        if (dalgaSource != null && dalgaSource.isPlaying)
            dalgaSource.Stop();
    }

    /// <summary>Her yükseltme satın alımı yapıldığında çalar</summary>
    public void UpgradeCal()
    {
        CalOneShot(upgradeClip, 0.85f);
    }

    /// <summary>Quiz doğru cevap verildiğinde çalar</summary>
    public void QuizDogruCal()
    {
        CalOneShot(quizDogruClip, 0.90f);
    }

    /// <summary>Quiz yanlış cevap verildiğinde çalar</summary>
    public void QuizYanlisCal()
    {
        CalOneShot(quizYanlisClip, 0.85f);
    }

    /// <summary>Afetlerden sonraki rapor popup'ı açılınca çalar</summary>
    public void AfetRaporuCal()
    {
        CalOneShot(afetRaporuClip, 0.90f);
    }

    /// <summary>Oyun kazanıldığında çalar</summary>
    public void OyunKazanmaCal()
    {
        CalOneShot(oyunKazanmaClip, 0.95f);
    }

    /// <summary>Sel anında yağmur yağarken anlık beyaz ışık geldiğinde çalar</summary>
    public void SimsekCal()
    {
        CalOneShot(simsekClip, 1.0f);
    }

    // ---------- Afet Sesleri (Smooth Fade Desteğiyle) ----------

    private System.Collections.IEnumerator FadeVolume(AudioSource src, float hedefVol, float sure)
    {
        if (src == null) yield break;
        float baslangic = src.volume;
        for (float t = 0; t < sure; t += Time.unscaledDeltaTime)
        {
            if (src == null) yield break;
            src.volume = Mathf.Lerp(baslangic, hedefVol, t / sure);
            yield return null;
        }
        if (src != null) src.volume = hedefVol;
    }

    private System.Collections.IEnumerator FadeAndStop(AudioSource src, float sure)
    {
        if (src == null || !src.isPlaying) yield break;
        float baslangic = src.volume;
        for (float t = 0; t < sure; t += Time.unscaledDeltaTime)
        {
            if (src == null) yield break;
            src.volume = Mathf.Lerp(baslangic, 0f, t / sure);
            yield return null;
        }
        if (src != null)
        {
            src.volume = 0f;
            src.Stop();
        }
    }

    /// <summary>Tsunami hariç her afetin olduğu gün başlar, veya Tsunami öncesi gün 19'da tetiklenir</summary>
    public void SirenCal(bool cal, float fadeSure = 0.5f, float hedefVol = 0.75f)
    {
        if (sirenSource == null || sirenClip == null) return;
        if (cal)
        {
            if (!sirenSource.isPlaying)
            {
                sirenSource.clip = sirenClip;
                sirenSource.volume = 0f;
                sirenSource.Play();
            }
            StartCoroutine(FadeVolume(sirenSource, hedefVol, fadeSure));
        }
        else
        {
            if (sirenSource.isPlaying)
                StartCoroutine(FadeAndStop(sirenSource, fadeSure));
        }
    }

    /// <summary>Sireni fade ile durdurur</summary>
    public void SirenDurdur(float fadeSure = 0.5f)
    {
        SirenCal(false, fadeSure);
    }

    /// <summary>Siren çalarken ses seviyesini dinamik olarak kısmak / artırmak için (örn: Gün 20'de kısık çalması)</summary>
    public void SirenSesSeviyesiAyarla(float hedefVol, float sure = 0.8f)
    {
        if (sirenSource == null) return;
        if (!sirenSource.isPlaying)
        {
            SirenCal(true, sure, hedefVol);
            return;
        }
        StartCoroutine(FadeVolume(sirenSource, hedefVol, sure));
    }

    /// <summary>Yangın afeti başladığında çalar, afet bitince durur</summary>
    public void AtesCal(bool cal, float fadeSure = 0.5f)
    {
        if (atesSource == null || atesClip == null) return;
        if (cal)
        {
            if (!atesSource.isPlaying)
            {
                atesSource.clip = atesClip;
                atesSource.volume = 0f;
                atesSource.Play();
            }
            StartCoroutine(FadeVolume(atesSource, 0.70f, fadeSure));
        }
        else
        {
            if (atesSource.isPlaying)
                StartCoroutine(FadeAndStop(atesSource, fadeSure));
        }
    }

    /// <summary>Sel afeti başladığı zaman afet bitene kadar çalar</summary>
    public void YagmurVeGokGurultusuCal(bool cal, float fadeSure = 0.5f)
    {
        if (yagmurSource == null || yagmurVeGokGurultusuClip == null) return;
        if (cal)
        {
            if (!yagmurSource.isPlaying)
            {
                yagmurSource.clip = yagmurVeGokGurultusuClip;
                yagmurSource.volume = 0f;
                yagmurSource.Play();
            }
            StartCoroutine(FadeVolume(yagmurSource, 0.75f, fadeSure));
        }
        else
        {
            if (yagmurSource.isPlaying)
                StartCoroutine(FadeAndStop(yagmurSource, fadeSure));
        }
    }

    public void TumAfetSesleriniDurdur(float fadeSure = 0.8f)
    {
        if (sirenSource != null && sirenSource.isPlaying) StartCoroutine(FadeAndStop(sirenSource, fadeSure));
        if (atesSource != null && atesSource.isPlaying) StartCoroutine(FadeAndStop(atesSource, fadeSure));
        if (yagmurSource != null && yagmurSource.isPlaying) StartCoroutine(FadeAndStop(yagmurSource, fadeSure));
    }
}
