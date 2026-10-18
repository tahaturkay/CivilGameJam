using UnityEngine;
using UnityEngine.UI;

// Ana Canvas'a eklenir. HUD'daki Duraklat / 1x / 2x / 3x butonları.
// Hız Time.timeScale'e dokunmaz; sadece gün süresini değiştirir (GameManager.hizCarpani).
public class HizKontrol : MonoBehaviour
{
    public Button duraklatButonu;
    public Button[] hizButonlari = new Button[3]; // 1x, 2x, 3x

    public Color normalRenk = Color.white;
    public Color seciliRenk = new Color(1f, 0.8f, 0.3f);

    void Start()
    {
        if(GameManager.Instance!=null)
            HizAyarla(DengeAyarlari.Aktif.baslangicHizi);
    }

    public void HizAyarla(int hiz)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.hizCarpani = Mathf.Clamp(hiz, 1, 3);
        gm.kullaniciDurdurdu = false;
    }

    public void DuraklatDegistir()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.kullaniciDurdurdu = !gm.kullaniciDurdurdu;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Seçili olan butonu renklendir
        Boya(duraklatButonu, gm.kullaniciDurdurdu);
        for (int i = 0; i < hizButonlari.Length; i++)
            Boya(hizButonlari[i], !gm.kullaniciDurdurdu && gm.hizCarpani == i + 1);
    }

    void Boya(Button buton, bool secili)
    {
        if (buton == null || buton.targetGraphic == null) return;
        buton.targetGraphic.color = secili ? seciliRenk : normalRenk;
    }
}
