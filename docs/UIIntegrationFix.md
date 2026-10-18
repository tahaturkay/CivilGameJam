# UI derleme ve Play Mode düzeltmesi — 18 Ekim 2026

Yeni ana menü, ayarlar, duraklatma, afet raporu ve oyun sonu ekranları gerçek Unity Play Mode’da incelendi. Tasarım korunarak ortak API ve yerleşim sorunları giderildi.

## Hata nedeni

Yeni ekranların çağırdığı `CivicUI.Green`, `Cyan`, gölge renkleri ve bazı şerit/düğme metot imzaları ortak sınıfta bulunmuyordu. `IpucuSistemi` de `Image` kullanırken `UnityEngine.UI` ad alanını içermiyordu. Bunlar C# derlemesini engelliyor; Unity bu nedenle “All compiler errors have to be fixed before you can enter playmode” mesajını gösteriyordu.

## Değişiklikler

- `CivicUI`: mevcut palete bağlı uyumlu renk adları; şerit görsellerini ve başlığını döndüren overload; iki argümanlı şerit kuyruğu çağrıları; yeni düğme parametre sırası. Önceki API korunur.
- `IpucuSistemi`: eksik UI ad alanı eklendi.
- `SettingsPanel`: müzik ve efekt kaydırıcıları, etiketlerinin bulunduğu sol kartın altına taşındı. Fontta bulunmayan dekoratif onay işareti yerine mevcut düğme ikonu kullanılır.
- `AfetRaporu`: kayıp/kurtarılan metinleri kalp ve kalkan ikonlarıyla çakışmayacak şekilde içerden başlatılır.
- `OyunSonu`: yıldızlar zaten ayrı görsellerle çizildiğinden eksik glif kutuları üreten metin yıldızları kaldırıldı.
- `JuryMenuCapture`: gerçek menü → rehber → ayarlar → şehir → duraklatma → rapor → oyun sonu akışını ve ayar bağlantılarını doğrular. Denenen ses/HUD ayarları önceki değerlerine geri getirilir.

`GameManager.cs`, grafik kalite ayarları ve ada assetleri bu düzeltmede değiştirilmedi.

## Son doğrulama

| Kontrol | Sonuç | Kayıt |
|---|---|---|
| Yeni UI akışı ve ayar bağlantıları | **14 geçti, 0 kaldı** | `Logs/JuryMenuCapture.txt` |
| Güncel UI ile afet/kamera/durdurma/sel/trafik | **48 geçti, 0 kaldı** | `Logs/IslandRevisionTests.txt` |
| Unity C# derlemesi | **0 derleme hatası**; Play Mode’a giriş doğrulandı | `Logs/Editor.log`, `Logs/UIIntegrationFinal.offset` sonrası |
| Windows64 paketi | **Succeeded, 0 hata**, 188.537.506 bayt, 13,55 sn | `Logs/JuryWindowsBuild.txt` |
| Dosya biçimi kontrolü | `git diff --check` temiz | çalışma ağacı |

Trafik turunda 180 simülasyon saniyesi boyunca 139 kavşak geçişi, araç başına en az 527,0 m ilerleme ve en uzun 12,3 saniyelik bekleme ölçüldü. Araç izdüşümü örtüşmesi görülmedi. Dört afette kamera hareketi/zoom, HUD durdurması, parçacık saatleri, yangın yayılımı ve su/dalga hareketi kontrol edildi.

3840×2160 gerçek oyun görüntüleri `Logs/Jury_MainMenu.png`, `Jury_Guide.png`, `Jury_Settings.png`, `Jury_BuildingCard.png`, `Jury_Pause.png`, `Jury_PauseSettings.png`, `Jury_Report.png` ve `Jury_Ending.png` dosyalarında incelendi. Oyun sonu görüntüsü yerleşim önizlemesidir; doğal 20 günlük oturum süresi ölçümü değildir.

UI turunda native tampon uyarısı görülmedi; sonraki uzun afet/trafik turunun günlüğünde **1 Graphics Ring Buffer uyarısı** görüldü. Bu C# derleme hatasından ayrıdır ve tamamen giderildiği iddia edilmez. Her iki robotta çalışma zamanı hata sayısı 0’dır.

Windows çıktısı `Builds/Windows/CivilGameJam.exe`; dağıtım için **Windows klasörünün tamamı** taşınmalıdır. macOS üzerinden çapraz derleme alındı; gerçek Windows cihazında çalıştırılmadı. Unity’nin paketleme sırasında değiştirdiği kaynak font/ProjectSettings serileştirmeleri önceki içerikleriyle korundu.
