using UnityEngine;

/// <summary>Runtime-built maritime warning cue used by the tsunami finale.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class IslandSirenAudio : MonoBehaviour
{
    const int SampleRate = 24000;
    const float CueDuration = 4f;
    const float BaseVolume = .38f;

    static IslandSirenAudio instance;
    AudioSource source;
    AudioClip ownedClip;
    bool effectsMixerAssigned;
    bool pausedBySimulation;

    public static IslandSirenAudio Instance => instance;
    public bool IsPlaying => source != null && (source.isPlaying || pausedBySimulation);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.pitch = 1f;
        source.volume = BaseVolume;

        var mixer = Ayarlar.Mixer;
        if (mixer != null)
        {
            var groups = mixer.FindMatchingGroups(Ayarlar.EfektGrubu);
            if (groups.Length > 0)
            {
                source.outputAudioMixerGroup = groups[0];
                effectsMixerAssigned = true;
            }
        }

        if (!effectsMixerAssigned)
            source.volume = BaseVolume * Mathf.Clamp01(Ayarlar.EfektSesi);

        ownedClip = CreateWarningClip();
        source.clip = ownedClip;
    }

    void Update()
    {
        if (source == null) return;

        // The mixer owns the effects slider when present. Keep the direct-volume
        // fallback in sync when a project has no usable Effects mixer group.
        if (!effectsMixerAssigned)
            source.volume = BaseVolume * Mathf.Clamp01(Ayarlar.EfektSesi);

        var gm = GameManager.Instance;
        bool shouldPause = IslandSimulationPause.IsPaused ||
                           (gm != null && (gm.oyunDurduMu || gm.afetOlduMu || gm.oyunBittiMi));

        if (shouldPause && source.isPlaying)
        {
            source.Pause();
            pausedBySimulation = true;
        }
        else if (!shouldPause && pausedBySimulation)
        {
            source.UnPause();
            pausedBySimulation = false;
        }
    }

    public void PlayCue()
    {
        if (source == null || ownedClip == null) return;
        pausedBySimulation = false;
        source.Stop();
        source.clip = ownedClip;
        source.loop = false;
        source.time = 0f;
        source.Play();

        var gm = GameManager.Instance;
        if (IslandSimulationPause.IsPaused ||
            (gm != null && (gm.oyunDurduMu || gm.afetOlduMu || gm.oyunBittiMi)))
        {
            source.Pause();
            pausedBySimulation = true;
        }
    }

    public void StopCue()
    {
        if (source == null) return;
        source.Stop();
        pausedBySimulation = false;
    }

    AudioClip CreateWarningClip()
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * CueDuration);
        var clip = AudioClip.Create("IslandSirenAudio_RuntimeWarning", sampleCount, 1, SampleRate, false);
        var samples = new float[sampleCount];
        float sweepPhase = 0f;
        float lowPhase = 0f;

        // Two smooth maritime up/down sweeps with a restrained lower harmonic
        // make the warning recognizable while leaving room for dialogue and music.
        const float sweepPeriod = 2f;
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float phase01 = Mathf.PingPong(time / (sweepPeriod * .5f), 1f);
            float smoothSweep = phase01 * phase01 * (3f - 2f * phase01);
            float frequency = Mathf.Lerp(430f, 760f, smoothSweep);

            float envelope = Mathf.Min(1f, time / .08f) * Mathf.Min(1f, (CueDuration - time) / .32f);
            envelope = Mathf.Clamp01(envelope);
            float slowPulse = .91f + .09f * Mathf.Sin(2f * Mathf.PI * .72f * time);
            float fundamental = Mathf.Sin(sweepPhase);
            float harmonic = .19f * Mathf.Sin(sweepPhase * 2f + .18f);
            float lowBody = .08f * Mathf.Sin(lowPhase);
            samples[i] = (fundamental + harmonic + lowBody) * envelope * slowPulse * .66f;

            sweepPhase = Mathf.Repeat(sweepPhase + 2f * Mathf.PI * frequency / SampleRate, 2f * Mathf.PI);
            lowPhase = Mathf.Repeat(lowPhase + 2f * Mathf.PI * 215f / SampleRate, 2f * Mathf.PI);
        }

        clip.SetData(samples, 0);
        return clip;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;

        if (source != null && source.clip == ownedClip)
            source.clip = null;

        if (ownedClip != null)
        {
            Destroy(ownedClip);
            ownedClip = null;
        }
    }
}
