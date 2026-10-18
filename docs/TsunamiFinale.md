# Limandaki iki ziyaretçi — tsunami finali

Final, mevcut 20. gün tsunami afetinin sinematik sunumudur. İkinci bir tsunami olayı veya 21. gün eklenmez. `GameManager.cs`, savunma, hasar ve can kaybı formülü değiştirilmedi. İki ziyaretçinin modeli ve kıyafetleri korunur; şehir nüfusuna ek ölüm yazılmaz.

## Akış

1. **19. gün:** siren kulesinin mevcut uyarısı bütün sakinleri ve hayvanları farkındalıktan bağımsız evlere gönderir. Mevcut yol rotaları korunur; tahliye hızı gün bitmeden varacak şekilde ayarlanır. Trafik durur. Bu gün tekne sinematiği başlamaz; normal şehir kamerası kullanılabilir.
2. **20. gün:** şehir boş kalır. Kamera iskele üzerinden açık denize yumuşakça geçer. Kullanılan gemi `Assets/Assets/Taha/LowPolyGemi.prefab` dosyasındaki özgün yelkenlidir. Arkasında aynı deniz materyalini kullanan tsunami yaklaşır.
3. Gemi açık denizden kıvrılarak yanaşır (6 saniye). Kamera yanaşma açısına akar. İki kişi sırayla güverteden iniş tahtasına, ardından iskeleye yürür (3,2 saniye).
4. Gün batımı sıcak ışık ve renk geçişiyle kurulur. Görünen güneş, birinci karakterin arkadaşına baktığı yönde, arkadaşının omuz arkasında kalır ve yavaşça alçalır.
5. Siren duyulur. Kamera göz hizasına yaklaşır (0,9 saniye), dalgaya bakar (1,5 saniye), arkadaşına döner (1,8 saniye), sonra yeniden dalgaya döner (1,8 saniye). Dalga bütün bu sırada ilerler.
6. Kırılan dalga objektifi tamamen sarar. Temasa yaklaşırken görüntü siyaha kararır. **“OYUN BİTTİ”** başlığı ve **“Farkındalık Hayat Kurtarır”** alt satırı siyah üzerinde görünür (2,2 saniye), sonra yavaşça kaybolur (3 saniye).
7. Mevcut tsunami savunma/hasar/can kaybı hesabı tamamlanır ve **oyun sonu raporu siyah fon üzerinde açılır**. Ara tsunami raporu gösterilmez. Raporun arkasında kalan şehir ve yakın dalga çizimi kapatılır; ESC menüsü kullanılabilir.

Toplam sinematik yaklaşık **20,4 saniye** sürer. 19 günlük takvim 2× hızda 114 saniye, önceki üç afet 24 saniyedir; raporlarda/oyuncu işlemlerinde geçen süre hariç toplam yaklaşık **158 saniye** olur. 1/2/3 hız tuşları final sırasında kapalıdır; okunabilir kamera planlarının süresi sabittir. Space ve ESC; gemiyi, kamerayı, yürümeyi, dalgayı ve sireni birlikte durdurur.

## Entegrasyon

- `DisasterController` gerçek tsunami coroutine’ini sinematik içinden bir kez çalıştırır. Normal dalganın renderer’ları saklanır, görünen sinematik dalga aynı modelin kopyasıdır. Güçlü set şehri korur; zayıf sette mevcut yerel su/hasar sonucu hesaplanır. İskele set dışında olduğu için iki ziyaretçinin finali iki durumda da oynar.
- Mevcut afet istatistikleri kaydedildikten sonra `GameManager.AfetSonrasiDevam()` çağrılır. Final siyah fonu korur; afet hesabı bitip oyun sonu durumu işaretlenince mevcut oyun sonu raporuna arayüzü bırakır. Diğer afetlerin rapor akışı değişmez.
- Limanda `BalikciTeknesi/Model`, özgün gemi mesh ve palet materyalini kullanır (uniform ölçek 1,5). Kaynak prefab/model değiştirilmez. `DeckMarkers` ve `CinematicMarkers` güverte, açık deniz yaklaşımı, yanaşma ve iniş noktalarını tanımlar.
- Gün batımı yalnız bu finalde `IslandWeather.SetCinematicSunset` ile uygulanır. Hafif güneş/ufuk geometrisi mevcut parçacık shader’ını kullanır. Yakın dalga yüzeyi yalnız iki üçgendir; yeni paket veya dış medya gerektirmez.
- Kamera, HUD, renderer durumları ve gün batımı override’ı sahne kapanışında geri alınır; çalışma anında üretilen mesh ve materyaller temizlenir.

## Kurulum ve kontrol

Derleme tamamlanmış Edit Mode’da `Temp/harbor_cinema_istegi` içine `setup` yazmak yalnız mevcut liman alt ağacını yeniler. Ortak ada prefabı MainScene ve Scene_Harita tarafından kullanılır. Sonuç: `Logs/HarborCinemaSetup.txt`.

`Temp/tsunami_finale_istegi` içine `weak`, `strong` veya `both` yazmak gerçek MainScene üzerinde odaklı kontrol çalıştırır. Kontrol; 19. gün tahliyesini, özgün gemiyi, gün batımını, duraklatmayı, bakış sırasını, ekranı kaplayan dalgayı, siyaha solmayı, savunma sonuçlarını ve raporsuz bitişi kapsar. Sonuç ve plan görselleri `Logs/TsunamiFinale_Weak_*` / `Logs/TsunamiFinale_Strong_*` dosyalarındadır.

Son rapor akışı ve performans düzenlemesinin doğrulaması `docs/EmergencyAndUiRevision.md` ve güncel `Logs/TsunamiFinale_Weak_Tests.txt` / `Logs/TsunamiFinale_Strong_Tests.txt` dosyalarındadır. Önceki raporsuz finalin sonuçları bu yeni akışın testi olarak değerlendirilmemelidir.
