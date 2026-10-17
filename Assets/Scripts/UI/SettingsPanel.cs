using UnityEngine;
using UnityEngine.UI;

// SettingsPanel prefab'ının köküne eklenir. Hem ana menüde hem oyundaki ESC menüsünde aynı prefab kullanılır.
// Kod sadece slider/toggle'lara bağlı; tasarımcı görünümü serbestçe değiştirebilir.
public class SettingsPanel : MonoBehaviour
{
    public Slider muzikSlider;
    public Slider efektSlider;
    public Toggle tamEkranToggle;

    public bool AcikMi { get { return gameObject.activeSelf; } }

    void Awake()
    {
        muzikSlider.minValue = 0f; muzikSlider.maxValue = 1f;
        efektSlider.minValue = 0f; efektSlider.maxValue = 1f;

        muzikSlider.onValueChanged.AddListener(v => Ayarlar.MuzikSesi = v);
        efektSlider.onValueChanged.AddListener(v => Ayarlar.EfektSesi = v);
        tamEkranToggle.onValueChanged.AddListener(v => Ayarlar.TamEkran = v);
    }

    void OnEnable()
    {
        // Panel her açıldığında kayıtlı değerleri göster (değiştirme olayı tetiklemeden)
        muzikSlider.SetValueWithoutNotify(Ayarlar.MuzikSesi);
        efektSlider.SetValueWithoutNotify(Ayarlar.EfektSesi);
        tamEkranToggle.SetIsOnWithoutNotify(Ayarlar.TamEkran);
    }

    public void Ac()
    {
        gameObject.SetActive(true);
    }

    public void Kapat()
    {
        Ayarlar.Kaydet();
        gameObject.SetActive(false);
    }
}
