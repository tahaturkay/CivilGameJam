# Yangın müdahalesi ve afet tepkileri — 18 Ekim 2026

## Mevcut mimari ve değişiklik

`DisasterController` afet takvimini, savunma kontrolünü ve can kaybı hesabını yönetir. `BuildingVisuals` bina bazında hasarı tutar; `BuildingDamageVisuals` onarıma kadar is/duman gösterir. `IslandCityLife` mevcut yol ağı üzerinde NPC ve araçları hareket ettirir. `AfetRaporu` olay sonucunu gösterir; ekonomi/onarım akışı `Ekonomi` ve mevcut GameManager API’si üzerinden devam eder.

Yangın güçlü savunmada da rastgele bir mahallede başlar. Güçlü itfaiyede en fazla 3, yetersiz savunmada en fazla 9 yakın bina tutuşur; 32 birimlik yayılma sınırı korunur. Yalnızca yanan binalar yerel hasar alır. Mevcut savunma/farkındalık/AKUT hesabı can kaybını azaltmaya devam eder. Yangın, rapor görünür olduğunda söndürülür; hasar, duman, is ve onarım ihtiyacı rapor kapandıktan sonra da kalır. Bütün bir bina kategorisinin model seviyesi düşürülmez.

Tek bir low-poly itfaiye aracı istasyonda bekler; yangında olay bölgesine gider ve rapor kapatıldıktan sonra geri döner. İki kırmızı tepe lambası, merdiven, ekipman dolapları ve pompa modeli vardır. Ortak mesh 6 materyal ve yaklaşık 1.708 üçgen kullanır. Su püskürtme animasyonu eklenmedi. Araç hareketi oyun hızı ve duraklatma durumunu izler.

Müdahale aracı mevcut 20 düğümlü yol ağı üzerinde, belediye meydanından geçmeden ilerler. Dolu bir kavşakta diğer bağlantı ucunu kullanabilir; karşı şeridi güvenli olduğunda değerlendirir. Diğer araçların gerçek model boyutlarıyla OBB çakışma denetimi yapar ve yangından 20 birimden uzakta durduğunda “ulaştı” sayılmaz. Normal trafik de dönüşte bu araca yol verir. Engel listesi yalnızca küçük araç hiyerarşisinden alınır; bütün ada her karede taranmaz.

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

## Doğrulama

**Son gerçek sahne turu: 47 geçti, 0 kaldı** (`Logs/FireResponseTests.txt`). Gerçek klavye girdileri, 60/80 sınırları, tsunami uyarısı, gerçek sel akıntısı, iki savunma düzeyindeki yangın, itfaiye aracının olay bölgesine ulaşması/çakışmaması/dönmesi, duraklatma, raporla söndürme ve onarım doğrulandı. Aynı farkındalık/AKUT değerlerinde savunmasız yangındaki 76 can kaybı, güçlü itfaiyede 15’e düştü. Bu turda derleme hatası, runtime exception ve Graphics Ring Buffer uyarısı görülmedi. Ayarlar, yangın ve en yüksek seviye kartı önizlemeleri `Logs/FireResponse_*.png` dosyalarındadır.

Derleme tamamlandıktan sonra Edit Mode’da `Temp/fire_response_istegi` içine `test` yazılması gerçek MainScene üzerinde `FireResponseProbe` çalıştırır; `engine` kısa rota tanılama modudur. Sonuç `Logs/FireResponseTests.txt`, ara ilerleme `Logs/FireResponseProgress.txt` dosyasına yazılır. Önceden kalan ilgili regresyon kontrolleri de yeni farkındalık/hasar kurallarına uyarlandı; bu turda ayrıca çalıştırılmadı.

GameManager.cs değiştirilmedi. Sahne/prefab referansları mevcut kaldı; yeni itfaiye aracı ortak mesh/materyal önbelleğiyle çalışma anında üretilir.

Windows paketi yeniden üretildi: **0 hata, 210.461.214 bayt, 12,70 saniye** (`Logs/JuryWindowsBuild.txt`). Gerçek Windows cihazında oynatılmadı; `Builds/Windows` klasörünün tamamı birlikte taşınmalıdır.
