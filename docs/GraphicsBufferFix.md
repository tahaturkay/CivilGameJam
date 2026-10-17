# Graphics Ring Buffer ve Git kilidi — 18 Ekim 2026

## Yapılan düzeltmeler

- `IslandCityLife`: 48 yayanın 528 mesh parçası, paylaşılan mesh ve materyalle en fazla 14 `RenderMeshInstanced` grubu üzerinden gönderilir. Yürüyüş, gölgeler, tahliye ve sığınma korunur. Instancing desteklenmeyen cihazlarda normal renderer kullanılır; bileşen kapatılınca normal renderer'lar geri açılır. Yeniden kurulum eski aktörleri ve geçici materyalleri temizler.
- `IslandEffectsFactory`: yaya parçaları ortak küp meshini kullanır; gereksiz collider üretimi kaldırılmıştır. Resources içindeki `CitizenInstancing.mat`, player için Lit instancing shader varyantlarını korur.
- `BuildingDamageVisuals`: temiz/onarılmış yapılarda boş `MaterialPropertyBlock` gerçekten kaldırılır. Anlamlı mevcut bloklar ve hasar/vurgu renkleri korunur.
- Ada doğrulamasındaki art arda kamera çekimleri ayrı editör güncellemelerine; yangın testindeki iki çekim ayrı karelere dağıtılır. Böylece tek GUI olayında birden çok tam kamera çizimi yığılmaz.

Çözünürlük, NPC sayısı ve görsel kalite ayarları azaltılmadı. Editör başlatma seçeneklerine belirsiz bir tampon boyutu eklenmedi.

## Gerçek editör ölçümü

Unity 6000.6.6f1, Apple M4 / Metal, 3840×2160 Game görünümü. Aşağıdaki değerler bu editör oturumunun örnek ortalamalarıdır.

| Ölçüm | Önce | Sonra |
|---|---:|---:|
| Normal durum çizim çağrısı | 7.103 | 4.725 |
| Normal durum kare süresi | 16,6 ms | 11,0 ms |
| NPC/araç hiyerarşisinde açık MeshRenderer | 540 | 12 |
| Aktif property block taşıyan renderer | 140 | 8 |
| Açıkça gönderilen yaya parçaları / grup | — | 528 / 14 |

Çizim çağrısı yaklaşık %33,5 azaldı. Normal oyun, popup, toplu yükseltme/vurgu ve yangın/sel/deprem/tsunami stres testinde **0 yeni Graphics Ring Buffer uyarısı, 0 çalışma zamanı hatası** kaydedildi. Ön ölçümde de uyarı oluşmadı; önceki logdaki aralıklı uyarının her koşulda ortadan kalktığı bu testten çıkarılamaz. Player build bu turda çalıştırılmadı.

Ham ölçümler: `Logs/GraphicsBuffer-baseline.txt`, `Logs/GraphicsBuffer-after.txt`. Profiler'ın bu oturumda kullanılabilir veri vermeyen `Batches Count` alanı karşılaştırmada kullanılmadı. Metal üzerindeki toplam çizim sayısı ile kodun 14 instance grubu farklı ölçümlerdir.

Son regresyon turunda **44 oyun entegrasyon kontrolü geçti**; buna instancing açma/kapatma yedeği, aktörleri yeniden kurma, hasar/onarım, tahliye ve savunmalı/savunmasız afetler dahildir. Ada seviye/collider/set çevresi/popup doğrulaması da geçti. Bu iki test boyunca native logda yeni tampon uyarısı, C# derleme hatası veya çalışma zamanı exception'ı görülmedi. Yangın testi, kamera çekimlerinin geciktirebildiği sabit bekleme yerine süre sınırı içinde gerçek afet bitişini izler. Sonuçlar `Logs/AdaSistemTesti.txt` ve `Logs/AdaDogrulama.txt` içindedir.

Editörde yeniden stres testi: `Temp/graphics_buffer_probe` dosyasına `after` yazılır. Araç MainScene'i Play modunda açar, test sonunda önceki gün süresini/zaman hızını geri yükler ve Play modundan çıkar. `Temp/ada_sistem_testi` oyun entegrasyonunu; `Temp/ada_kurulum_istegi` içine `test` yazılması ada seviye/collider/popup doğrulamasını başlatır.

## Git kilidi

`.git/index.lock` boştu, bir saatten eskiydi; aktif Git işlemi ve dosyayı yazmakta olan süreç bulunmadı. Kilit silinmek yerine `Logs/GitRecovery/stale-index-lock-20261017-235051.backup` dosyasına taşındı.

Ardından `git update-index -q --refresh` başarılı tamamlandı. Öncesi/sonrası staged girişlerin SHA-256 özeti aynıydı; staging ve çalışma dosyaları korunmuştur. GitHub Desktop depo ekranı kilit hatası olmadan açıldı.
