# Limandaki iki ziyaretçi — tsunami finali

Final, mevcut afet takvimine bağlıdır. `GameManager.cs` ve can kaybı hesabı değişmedi. İki karakter, şehir nüfusu simülasyonunun dışında, tekneyle yeni gelen ziyaretçilerdir. Şehir sakinleri ve hayvanlar, önceki tsunami uyarısına uyarak evlerine sığınmaya devam eder.

## Akış

1. Tsunamiden önceki gün gündüz kalır. Önceki akşamdan aydınlığa geçiş mevcut yumuşak güneş döngüsüyle yapılır.
2. Limandaki mevcut balıkçı teknesi açık denizden yanaşır. Kamera limanı kısa süre gösterir, sonra eski şehir görünümüne döner. Teknedeki iki ziyaretçi güvertede görünür. Yaklaşma 6 oyun saniyesidir; 2× hızda 3 gerçek saniye sürer.
3. Tsunami günü kamera alçak iskeleye geçer. İki kişi sırayla güverteden iniş tahtasına, sonra iskeleye yürür (3 saniye).
4. Son kişi indiğinde 4 saniyelik siren başlar; denizde mevcut dalga modeli ve deniz materyaliyle tehdit görünür.
5. Kamera birinci karakterin göz hizasına geçer. Dalgayı izler, arkadaşına döner; arkadaşı da ona bakar. Dalga bu sırada ilerlemeyi sürdürür.
6. Kamera yeniden denize döner. Dalga tepesinin iskeledeki karaktere ulaşmasına 0,70 dünya birimi kala sert siyah kesme yapılır. Temas veya ölüm animasyonu gösterilmez.
7. Temaya uygun “OYUN SONU” başlığı ve “UYARILAR HAYAT KURTARIR” satırı görünür.
8. Gerçek tsunami raporu açılır. Devam düğmesi mevcut şehir karnesini/oyun sonu raporunu açar.

İskele deniz setinin dışındadır. Set şehri korusa da dışarıda kalan ziyaretçilerin sahnesi devam eder; güçlü setin koruma, hasar ve can kaybı sonucu raporda aynen korunur. Ziyaretçiler nedeniyle nüfusa fazladan ölüm eklenmez.

## Bağlantılar

- `DisasterController.Start`, `IslandTsunamiFinale` bileşenini ekler. Son tsunami olayında bu bileşen mevcut `TsunamiEfekti` coroutine’ini paralel yürütür. Afet muhasebesi yine mevcut controller’da kalır.
- Normal afet dalgasının renderer’ları sinematik sırasında saklanır. Görünen yakın dalga aynı model/materyalin geçici kopyasıdır; sekans sonunda yok edilir. Şehrin sel/set sonucu hesaplanmaya devam eder.
- Tekne ve iskele ayrı birer birleşik mesh’tir. Mevcut liman kökü korunur. Düzenleme yalnızca `SabitAlanlar/Liman` altındadır; ortak prefab iki oyun sahnesine miras verir.
- `Liman/CinematicMarkers` ve `BalikciTeknesi/DeckMarkers` yanaşma, iniş ve karakter duruş noktalarını içerir.
- Siren, çalışma anında üretilen tek ses klibini mevcut Efekt mikserine bağlar. Yeni paket veya dışarıdan medya eklenmedi.

## Kontroller ve süre

Space ve ESC ile duraklatma; tekneyi, yürümeyi, kamera planını, dalgayı ve sireni birlikte durdurur. Sekansta 1/2/3 hız tuşları kapalıdır; kamera geçişleri okunabilir sabit sürelerde oynar. Kamera ve HUD, final dışındaki önceki durumlarına döner. ESC menüsü sinematik katmanın üzerinde kullanılabilir.

Önceki gündeki yaklaşma, mevcut gün sayacı içinde ilerler. Son günün yaklaşık 11 saniyelik sekansı, mevcut 8 saniyelik tsunami efektinin yerine geçer; oyuna yaklaşık 3 saniye eklenir. Diğer günler ve afet süreleri değiştirilmedi.

## Kurulum ve doğrulama

Limanı yeniden üretmek için derleme tamamlanmış Edit Mode’da `Temp/harbor_cinema_istegi` içine `setup` yazılır. Sonuç `Logs/HarborCinemaSetup.txt` dosyasındadır. Bu işlem yalnızca liman alt ağacını günceller.

Odaklı gerçek sahne kontrolü için `Temp/tsunami_finale_istegi` içine `weak`, `strong` veya `both` yazılır. Her set senaryosu ayrı MainScene oturumunda çalışır. İlgili sonuç ve görseller `Logs/TsunamiFinale_Weak_*` ve `Logs/TsunamiFinale_Strong_*` dosyalarına yazılır. Kontroller; önceki günün gelişi/ışığı/sığınma emrini, duraklatmayı, POV bakışlarını, bakış sırasında ilerleyen dalgayı, temas öncesi kesmeyi ve iki raporun sırasını kapsar.

Son gerçek sahne turu: **zayıf sette 37/37, güçlü sette 37/37**. Çalışma zamanı exception’ı ve Graphics Ring Buffer uyarısı yok. Son kompozisyonda dalga teknenin yanından açıkça görünür; arkadaş planında hafif, gölgesiz bir yüz ışığı kullanılır. Ana kamera, HUD ve dalga renderer durumları geri yüklenir. Son başlıkta olası tek karelik parlamayı önlemek için başlangıç alfa değeri sıfırlandı; bu küçük görsel düzenlemeden sonra Windows derlemesi alındı.

Windows paketi başarıyla üretildi: **0 hata, 210.491.745 bayt, 12,45 saniye** (`Logs/JuryWindowsBuild.txt`). Gerçek Windows cihazında oynatılmadı; dağıtımda `Builds/Windows` klasörünün tamamı birlikte taşınmalıdır.
