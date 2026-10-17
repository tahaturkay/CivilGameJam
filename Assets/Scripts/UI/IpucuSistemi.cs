using System;
using UnityEngine;
using TMPro;

// Ana Canvas'a eklenir. Ekranın altında, oyun akarken sırayla tek seferlik yönlendirici ipuçları gösterir.
// Bir ipucu, koşulu sağlanınca görünür; oyuncu istenen şeyi yapınca ya da süre dolunca kaybolur.
// Popup, quiz, rapor açıkken gizlenir (süre de işlemez).
public class IpucuSistemi : MonoBehaviour
{
    public GameObject serit;
    public TMP_Text ipucuText;
    [Tooltip("Oyuncu bir şey yapmasa da ipucu en fazla bu kadar gerçek saniye görünür")]
    public float enFazlaSure = 9f;

    struct Ipucu
    {
        public string metin;
        public Func<GameManager, bool> gorunsunMu;  // ne zaman çıkacak
        public Func<GameManager, bool> tamamMi;     // oyuncu istenen şeyi yaptı mı
    }

    Ipucu[] ipuclari;
    int siradaki;
    float gorunenSure;

    public int SiradakiIndex { get { return siradaki; } } // otomatik test için

    void Awake()
    {
        serit.SetActive(false);

        ipuclari = new[]
        {
            new Ipucu
            {
                metin = "Binalara tıklayarak bilgi alabilir ve onları yükseltebilirsin.",
                gorunsunMu = gm => true,
                tamamMi = gm => BinaPopup.Instance != null && BinaPopup.Instance.panel.activeSelf,
            },
            new Ipucu
            {
                metin = "Okula tıkla ve quiz çöz: halkın farkındalığı arttıkça afetlerde daha az can kaybedilir.",
                gorunsunMu = gm => gm.yukseltmeSayisi >= 1 || gm.gun >= 2,
                tamamMi = gm => QuizManager.Instance != null && QuizManager.Instance.quizPanel.activeSelf,
            },
            new Ipucu
            {
                metin = "Sağ üstteki butonlarla oyunu hızlandırabilir (2x, 3x) ya da durdurabilirsin.",
                gorunsunMu = gm => gm.gun >= 2,
                tamamMi = gm => gm.hizCarpani > 1,
            },
            new Ipucu
            {
                metin = "Her afet şehrin farklı bir yapısını sınar. Parayı dengeli harca!",
                gorunsunMu = gm => gm.gun >= 4,
                tamamMi = gm => false,
            },
            new Ipucu
            {
                metin = "Hasar alan yapılar onarılana kadar görevini yapamaz. Binaya tıklayıp onarabilirsin.",
                gorunsunMu = gm => gm.toplamVefat > 0 && HasarliVarMi(gm),
                tamamMi = gm => !HasarliVarMi(gm),
            },
        };
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || ipuclari == null) return;

        if (siradaki >= ipuclari.Length || gm.oyunBittiMi)
        {
            serit.SetActive(false);
            return;
        }

        Ipucu ipucu = ipuclari[siradaki];

        // Önceden yapıldıysa hiç göstermeden geç
        if (gorunenSure == 0f && ipucu.tamamMi(gm) && ipucu.gorunsunMu(gm))
        {
            Sonraki();
            return;
        }

        bool baskaPanelAcik = gm.oyunDurduMu || gm.afetOlduMu || PauseMenu.DurdurulduMu;
        bool goster = ipucu.gorunsunMu(gm) && !baskaPanelAcik;
        serit.SetActive(goster);
        if (!goster)
        {
            // Popup/quiz açılıp istenen yapıldıysa (ör. ilk ipucu) yine de ilerle
            if (gorunenSure > 0f && ipucu.tamamMi(gm)) Sonraki();
            return;
        }

        ipucuText.text = ipucu.metin;
        gorunenSure += Time.unscaledDeltaTime;
        if (ipucu.tamamMi(gm) || gorunenSure >= enFazlaSure) Sonraki();
    }

    void Sonraki()
    {
        siradaki++;
        gorunenSure = 0f;
        serit.SetActive(false);
    }

    static bool HasarliVarMi(GameManager gm)
    {
        return gm.temelBinaHasarli || gm.okulHasarli || gm.akutHasarli
            || gm.itfaiyeHasarli || gm.setHasarli || gm.altyapiHasarli;
    }
}
