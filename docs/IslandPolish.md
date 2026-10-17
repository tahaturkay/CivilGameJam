# Ada yaşamı ve görsel düzenleme — 18 Ekim 2026

Mevcut `GameManager → Ekonomi → BuildingVisuals` akışı ve yapı türü başına seviye/hasar korunur. MainScene ve Scene_Harita ortak ada prefabını kullanır.

## Uygulanan sıra

1. Sel/tsunami arkası su opak, kirli açık mavi ve hafif tortulu dalgalanma içerir. İç set poligonu ve mevcut savunma seviyeleri korunur.
2. HUD krem paneller, şehir renkleri ve yeniden ölçülen ikonlarla hizalandı; güvenli ekran alanına uyar, dar ekranda zaman kontrolleri alt sıraya geçer. Para birimi bütün oyun UI'larında **coin**. Okul kartı 700×460, quiz 1000×720; ikisi de süslemeler ve kapatma düğmesi için pay bırakılarak ekrana sığdırılır.
3. Kamera başlangıçta 58 ortografik boyutta kent merkezine yaklaşır. Tekerlek/sürükleme/WASD korunur; **Home** bütün adayı yeniden çerçeveler. Kamera konumu da önceki 180 birim yerine 140 birim yüksekliktedir.
4. Günün ilk %60'ı gündüz, son %40'ı gece dönemidir; alacakaranlık ve şafak geçişleri yumuşaktır. Saat `EconomyAndTime.DayProgress` üzerinden okunur, oyun günü duraklayınca döngü de durur. Afet havası gece ışığıyla birlikte uygulanır.
5. Gece pencereler ve fener camları sıcak ışık yayar; sokak ışık havuzları ve araç farları toplu çizilir. En fazla 8 yakın yerel ışık + 1 deniz feneri spotu kullanılır, bu ışıklar gölge üretmez. Tek deniz fenerinin başlığı 360° döner; hacimsel huzme ve deniz üstündeki yansıma başlığı izler.
6. Altyapı hasarı sürdüğü sürece araçlar konumlarını korur; mevcut Onar işlemi trafiği yeniden başlatır. Koni ve yol çalışması/bozuk yol/yavaşla levhaları şeritlerin dışında kalır.
7. Farkındalık afet başlangıcında okunur. Sınırdaki 40 orta gruba, 80 yüksek gruba aittir; örtüşen talepler bu şekilde tek bir davranışa bağlanmıştır.

| Farkındalık | Yangın / deprem | Sel / tsunami |
|---|---|---|
| 0 ≤ değer < 40 | İnsanların yaklaşık %70'i evlere koşar; kalanlar toplanır. | Aynı çoğunluk dışarıda panikle koşuşturur; kalanlar toplanır. |
| 40 ≤ değer < 80 | Hepsi park, otopark veya kumsalda toplanır; insanlar alanda huzursuzca dolaşır. | Aynı davranış. |
| 80 ≤ değer ≤ 100 | Hepsi toplanma alanlarına gider ve sakin bekler. | Aynı davranış. |

Hayvanlar güvenli toplanma alanlarına gider. Rapor kapanınca sakinler ve hayvanlar normal rotalarına döner. NPC davranışları mevcut farkındalığın can kaybına etkisini değiştirmez.

8. Deprem sırasında zemin çatlakları, hafif toz ve toplu çizilen 96 sıçrayan çakıl görünür. Konut hasarı onarılana kadar çatlaklar kalır. Konutların üçte birinde çatı meshinin bir bölümü kırılır, yere kiremitler dökülür; onarım orijinal mesh ve hiyerarşiyi geri yükler. Asset meshleri değiştirilmez.
9. Evlerde 8 cephe ve 4 çatı/panjur paleti ile dört cephe detayı kullanılır. NPC'lerde göz, yaka, saç/şapka, cep/çanta çeşitleri vardır. Araçlarda spor otomobil, kamyon, taksi ve farklı gövdeler bulunur. 95 insan + 3 kedi + 2 köpek = **100 canlı şehir aktörü**; 12 hareketli araç bu sayıya dahil değildir. Ekonomik nüfus sayacı ayrı kalır.

## Doğrulama araçları

- `Temp/ada_polish_testi`: gece/gündüz, yakın kamera, UI güvenli alanı, opak su, yol onarımı ve farkındalık sınırları; `Logs/AdaPolishTesti.txt`.
- `Temp/ada_sistem_testi`: gerçek ekonomi/afet/hasar/onarım akışları ve 100 aktörün entegrasyonu; `Logs/AdaSistemTesti.txt`.
- `Temp/ada_kurulum_istegi` içine `test`: seviye modelleri, collider, set çevresi ve bina tıklaması; `Logs/AdaDogrulama.txt`.
- `Temp/graphics_buffer_probe` içine `polish`: normal/gece/popup/yükseltme ve dört afet boyunca gerçek çizim yükü ve native tampon uyarıları; `Logs/GraphicsBuffer-polish.txt`.

Yaya/hayvan gönderimleri mesh + materyal + submesh temelinde gruplandırılır. Nonuniform ölçekli Lit parçaları 511 instance sınırıyla bölünür; animasyonlu Transform'lar korunur. Kalabalık fizik veya NavMesh maliyeti eklemez.

## Bu turdaki sonuçlar

- 48 sistem entegrasyon kontrolü geçti: 100 aktörün hareketi, tahliye/dönüş, opak su, trafik onarımı, zemin ve çatı hasarı, ekonomi ve UI.
- Yeni özellik testi 3840×2160, 1920×1080 ve 1366×768 Game görünümünde geçti. Okul/quiz kartı ve aktif düğmeleri güvenli alanda kaldı; gerçek soru ve uzun cevaplar görüntülenerek kontrol edildi. Farkındalıkta 0, 39.99, 40, 79.99, 80 ve 100 sınırları doğrulandı.
- Ada 1→2→3, collider, 180 yönden kapalı set çevresi ve bina tıklaması testi geçti.
- Apple M4 / Metal, gerçek 3840×2160 editör örneği: normal 11,7 ms, gece 12,0 ms, dört afet 12,3–12,7 ms ortalama kare süresi. 1440 insan/hayvan parça-submesh gönderimi 30 grupta çizildi. Bu değerler bu editör oturumuna aittir; farklı cihazlar ve player build ayrıca ölçülmelidir.
- Normal/gece/popup/yükseltme ve dört afet stres testinde **0 Graphics Ring Buffer uyarısı, 0 çalışma zamanı hatası**. Ham rapor: `Logs/GraphicsBuffer-polish.txt`.
- Mevcut genel MainScene oyun testi de **56 geçti, 0 başarısız** verdi; yükseltme/onarım, quiz, doğal gün ilerleyişi, afet raporu ve oyun sonu akışı korundu. Son raporda para birimi coin olarak doğrulandı. Son kontrol turunda C# / shader derleme hatası veya çalışma zamanı exception'ı görülmedi. Player build bu turda çalıştırılmadı.

Önizlemeler: [Gece ve fener](images/LowPolyAda_Night.png), [Quiz](images/LowPolyAda_Quiz.png), [Opak sel suyu](images/LowPolyAda_OpaqueFlood.png), [Çatı ve zemin hasarı](images/LowPolyAda_RoofDamage.png).
