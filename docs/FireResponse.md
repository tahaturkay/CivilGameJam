# Yangın müdahalesi ve afet tepkileri — 18 Ekim 2026

## Mevcut mimari ve değişiklik

`DisasterController` afet takvimini, savunma kontrolünü ve can kaybı hesabını yönetir. `BuildingVisuals` bina bazında hasarı tutar; `BuildingDamageVisuals` onarıma kadar is/duman gösterir. `IslandCityLife` mevcut yol ağı üzerinde NPC ve araçları hareket ettirir. `AfetRaporu` olay sonucunu gösterir; ekonomi/onarım akışı `Ekonomi` ve mevcut GameManager API’si üzerinden devam eder.

Yangın güçlü savunmada da rastgele bir mahallede başlar. Güçlü itfaiyede en fazla 3, yetersiz savunmada en fazla 9 yakın bina tutuşur; 32 birimlik yayılma sınırı korunur. Yalnızca yanan binalar yerel hasar alır. Mevcut savunma/farkındalık/AKUT hesabı can kaybını azaltmaya devam eder. Yangın, rapor görünür olduğunda söndürülür; hasar, duman, is ve onarım ihtiyacı rapor kapandıktan sonra da kalır. Bütün bir bina kategorisinin model seviyesi düşürülmez.

Bir low-poly itfaiye aracı ve üç ekipman varyantlı AKUT aracı istasyonlarında bekler. Yangına itfaiye; depremde gerçek hasar noktalarına itfaiye ve üç AKUT ekibi gider. Yeterli bina seviyelerinde yıkım yoksa yerleşimlerde önlem amaçlı kontrol yapılır. Araçlar kısa aralıklarla çıkar, rapor kapandıktan sonra geri döner. Ortak mesh ve materyal önbelleği kullanılır; yeni fizik gövdeleri veya dış paket eklenmez.

**Yangında normal trafik sürer.** Müdahale aracının yaklaşma koridoruna girmek üzere olan araçlar kısa süre yol verir; kavşağın içinde olanlar alanı boşaltır. Deprem, sel, tsunami ve tsunami uyarısında normal araç sürüşü durur. Suya kapılma/yer değiştirme mekaniği korunur. Sel/tsunamide müdahale aracı gönderilmez; önceki çağrıdan dönmekte olan müdahale araçları da durur.

Rota, mevcut 20 düğümlü yol ağına bağlıdır ve belediye meydanındaki eksik bağlantıyı kullanmaz. Hareket eden bir araç bütün bir yolu haritadan silmez. Yolun kenarları değerlendirilir; park halindeki müdahale araçları ve ilk bağlantıya uzaklık dikkate alınarak giriş tarafı seçilir. Kısa engellerde yerel kaçınma, hareket adımlarında süpürülmüş OBB kontrolleri kullanılır. Normal ve müdahale araçları aynı boyut hesabını kullanır. Rota dizileri tekrar kullanılır; her yeniden aramada engel hiyerarşisi taraması veya yeni Dijkstra dizileri üretilmez.

## Farkındalık

Kesişen eşiklerin tutarlı olması için üst uç alt aralığa dahildir: **0–60**, **60’ın üstü–80**, **80’in üstü–100**.

| Afet | 0–60 | >60–80 | >80–100 |
| --- | --- | --- | --- |
| Yangın / deprem | İnsanların yaklaşık %70’i yanlışlıkla evlere sığınır; kalanlar toplanma alanlarında panikler | Toplanma alanlarında panikle dolaşır | Toplanma alanlarında sakin bekler |
| Sel | Sokaklarda panikler; su ulaşırsa mevcut akıntı mekaniği sürükler | Evlerde kalır | Evlerde kalır |
| Tsunami | Bir gün önce sirenle eve gider | Bir gün önce sirenle eve gider | Bir gün önce sirenle eve gider |

Tsunami günü dışarıda NPC kalmaması için mevcut günlük takvim/sığınma koruması korunur. Selde %60’ın üstünün eve girmesi kullanıcı tarafından ayrıca doğrulandı. Hayvanlar yangın/depremde güvenli alanlara yönelir; selde aynı su tepkisine, tsunamide aynı sığınma emrine uyar.

## Arayüz

- `1`, `2`, `3` ve numpad karşılıkları 1×/2×/3× hızı seçer. Space mevcut durdur/devam işlevini korur. Modal, rapor, quiz ve metin girişi sırasında hız tuşları çalışmaz; modifier tuşlu editör kısayolları korunur.
- Ayarlardaki tuş rehberi ve Nasıl Oynanır savunma açıklamaları güncellendi.
- En yüksek seviye bina kartı tematik fontla tek bir “TAMAMEN GELİŞTİRİLDİ” mesajı gösterir; yinelenen “hazır” açıklaması kaldırıldı.
- Güçlü savunma yangın raporu artık şehrin hasarsız kaldığını söylemez.

## Son düzenleme ve doğrulama

Son afet/trafik, beyaz bayrak, deprem zemini, popup, gün sayacı ve final raporu değişiklikleri `docs/EmergencyAndUiRevision.md` içinde açıklanır. Önceki 47 kontrollük farkındalık/yangın turu tarihsel doğrulamadır; yeni filo için `Temp/emergency_revision_istegi` içindeki `test` isteği ve `Logs/EmergencyRevisionTests.txt` kullanılır.

`GameManager.cs` değiştirilmedi. Sahne/prefab referansları korunur. Gerçek Windows cihazında ayrıca oynanış kontrolü gerekir.
