# Müdahale filosu, arayüz ve performans düzenlemesi

## Afet trafiği ve müdahale

- Yangında normal trafik sürer. Yakındaki araçlar müdahale koridoruna girmeden yol verir; koridorun içinde olan araçlar alanı boşaltır.
- Deprem, sel, tsunami ve 19. gün tsunami uyarısında normal araç sürüşü durur. Sel akıntısının araçları sürüklemesi, motorla sürüşten bağımsız olarak korunur.
- Bir itfaiye aracı yangına gider. Depremde itfaiye ve üç AKUT aracı farklı hasar noktalarına yönelir. Yıkım yoksa yerleşimlerde önlem kontrolü yapılır. Sel ve tsunamiye müdahale filosu gönderilmez; bu sırada önceki çağrıdan dönen araçlar da bekler.
- Filonun çıkış/dönüşü kısa aralıklarla yapılır. Mevcut 20 düğümlü yol ağı, eksik merkez bağlantısı ve istasyon girişleri korunur. Rota giriş tarafı, park edilmiş müdahale araçları ve mevcut konuma göre seçilir. Hareketli trafik bütün bir yolu kapalı saydırmaz.
- Her hareket adımı canlı araç boyutlarıyla OBB denetiminden geçer. Normal araçlar da aynı boyut hesabıyla müdahale araçlarına yol verir. Kısa kaçınmalar, önbellekli 21×21 konum/yön aramasında kontrol edilir; dar istasyon girişinde geri manevra ve yanal geçiş yapılabilir. Aynı doğrultudaki gereksiz ara duraklar kaldırılır. Hareket ve dönüşte teleport kullanılmaz.
- AKUT modelleri `IslandActorArt.CreateAkutVehicle` üzerinden ortak mesh/materyal önbelleğinde üretilir. Üç varyantın ekipmanları farklıdır; gövde, far ve alarm bağlantıları aynı sözleşmeyi izler. Hareket oyun hızı ve duraklatmaya bağlıdır.

## Görsel ve arayüz

- Mevcut beş ODTÜ bayrağı beyaz kumaş/kırmızı amblem oldu. Direkler, amblem geometrisi ve civic modeller korunur. Kaynak üretici ve ortak prefab birlikte günceldir.
- Kuzey/güney yol yatakları 25 mm aşağı alınarak doğu/batı yüzeyleriyle kavşaklarda aynı düzlemde kalmaları önlendi. Model, doku, seviye sayısı ve etkileşim değişmedi. Deprem toprak çatlakları yol yüzeylerinden geçirilmez.
- Bina kartında açıklama, işlem ipucu ve düğmeler ayrı ölçülür; uzun metinde kart uzar. Hesaplama yalnız içerik değiştiğinde yapılır. 18 bina/seviye/hasar kombinasyonu için odaklı UI kontrolü vardır.
- Gün sayacında yalnız gün numarası yaklaşık 0,44 saniyede dikey flip yapar. Başlık ve toplam gün alanı sabittir; arka arkaya gelen gün değişiklikleri sıraya alınır.
- Tsunami temasa yaklaşırken siyaha kararır. `OYUN BİTTİ` / `Farkındalık Hayat Kurtarır` görünür, sonra mevcut oyun sonu raporu siyah fon üzerinde açılır. Ara tsunami raporu açılmaz. Afet muhasebesi tamamlanmadan rapor gösterilmez.

## Kaliteyi koruyan optimizasyonlar

- Rota listeleri ve arama dizileri yeniden kullanılır. Araçlar için bütün sahne veya engel hiyerarşisi her kare taranmaz.
- Uzak şehir kadrajında küçük tepe lambaları için gereksiz noktasal ışık hesabı yapılmaz; araç ve alarm geometrisi korunur, yakın kadrajda ışıklar çalışır.
- Molozların aynı 15 parçalık geometrisi, uygun GPU’larda bina başına tek instanced gönderimle çizilir; destek yoksa önceki renderer yolu kullanılır. Görünüm ve gölgeler korunur.
- Ana deniz zaten mevcut GPU shader’ını kullanır ve değişmedi. Tsunami dalgasında normal okumayan okyanus shader’ı için gereksiz CPU normal hesabı kaldırılır; diğer shader’lar önceki hesabı korur. Yağmurun aynı yoğunluk, eğim, renk ve damla görünümü ortak mesh/materyal ile toplu çizilir; destek olmayan cihazda önceki parçacık yolu korunur.
- Son rapor siyah fon üzerindeyken görünmeyen şehir çizimi ve yakın dalga animasyonu kapatılır. Sahne kapanınca kamera durumu geri alınır.
- Model veya doku çözünürlüğü, ada içeriği ve render kalite ayarları düşürülmedi. FPS/ısı kazancı cihaz üzerinde ayrıca ölçülmelidir.

## Tekrarlanabilir kontrol

Derlenmiş Edit Mode’da istek dosyaları sırayla kullanılmalıdır; aynı anda iki Play testi başlatılmamalıdır.

| İstek | İçerik | Sonuç |
| --- | --- | --- |
| `Temp/white_flags_istegi` | `setup` | `Logs/WhiteFlagsSetup.txt` |
| `Temp/emergency_revision_istegi` | `setup` | Yalnız mevcut yol yataklarının derinlik düzenlemesi |
| `Temp/emergency_revision_istegi` | `test` | `Logs/EmergencyRevisionTests.txt` |
| `Temp/hud_revision_istegi` | `test` | `Logs/HudRevisionTests.txt` |
| `Temp/tsunami_finale_istegi` | `both` | İki set senaryosu için `Logs/TsunamiFinale_*_Tests.txt` |

`GameManager.cs` değiştirilmedi. Yeni paket eklenmedi. Windows paketinin gerçek Windows cihazında oynanması derleme kontrolünden ayrıdır.

## Bu revizyonun kontrol kaydı

- `Logs/HudRevisionTests.txt`: 59 başarılı, 0 başarısız. Altı bina türünde normal/hasarlı/son seviye açıklama yerleşimi, okul kartı ve gün 2→3 flip geçişi kontrol edildi.
- `Logs/EmergencyRevisionTests.txt`: 23 başarılı mekanik kontrolü; çalışma zamanı hatası yok. Dört semte gidiş/dönüş, yangında akan trafik, duraklatma, depremde dört aracın varışı ve dönüşü, araç sınırlarının ayrıklığı, selde filo gönderilmemesi, bayraklar ve yol yatakları doğrulandı.
- Aynı acil durum koşusunda sel sırasında Metal `Graphics Ring Buffer` uyarısı bir kez tekrarlandı (4194 draw call / 922382 triangle anlık sayımı). Bu nedenle grafik uyarısı kontrolü başarısızdır; uyarının çözüldüğü iddia edilmez. Yağmurun toplu çizimi bu uyarıyı ortadan kaldırmadı.
- Full HD Game View ve Windows derlemesi, gerçek Windows oynanış performansının yerine geçmez.

- Güncel tsunami kontrolleri: zayıf set 35/35, güçlü set 35/35. Yakın temasta tam kararma, doğru başlık/alt yazı, tam siyah fon üzerindeki son rapor, koruma/hasar muhasebesi ve Space/ESC duraklatması geçti; iki son koşuda grafik tampon uyarısı veya runtime hatası yok. Bir önceki zayıf senaryoda sentetik ESC kontrolü başarısız oldu; Game View odaklanarak tekrar edildi ve geçti.
- Windows64 paketleme: `Logs/JuryWindowsBuild.txt` — Succeeded, 0 errors, 210804833 byte. Çıktı: `Builds/Windows/CivilGameJam.exe` ve yanındaki veri klasörleri. Gerçek Windows cihazında çalıştırılmadı.
