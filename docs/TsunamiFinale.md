# Limandaki iki ziyaretçi — tsunami finali

Final, mevcut 20. gün tsunami afetinin sinematik sunumudur. İkinci bir tsunami olayı veya 21. gün eklenmez. `GameManager.cs`, savunma, hasar ve can kaybı formülü değiştirilmedi. İki ziyaretçinin modeli ve kıyafetleri korunur; şehir nüfusuna ek ölüm yazılmaz.

## Akış

1. **19. gün:** siren kulesinin mevcut uyarısı bütün sakinleri ve hayvanları farkındalıktan bağımsız evlere gönderir. Mevcut yol rotaları korunur; tahliye hızı gün bitmeden varacak şekilde ayarlanır. Trafik durur. Bu gün tekne sinematiği başlamaz; normal şehir kamerası kullanılabilir.
2. **20. gün:** şehir boş kalır. Kamera iskele üzerinden açık denize yumuşakça geçer. Kullanılan gemi `Assets/Assets/Taha/LowPolyGemi.prefab` dosyasındaki özgün yelkenlidir. Arkasında aynı deniz materyalini kullanan tsunami yaklaşır.
3. Gemi açık denizden kıvrılarak yanaşır (6 saniye). Kamera yanaşma açısına akar. İki kişi sırayla güverteden iniş tahtasına, ardından iskeleye yürür (3,2 saniye).
4. Gün batımı sıcak ışık ve renk geçişiyle kurulur. Görünen güneş, birinci karakterin arkadaşına baktığı yönde, arkadaşının omuz arkasında kalır ve yavaşça alçalır.
5. Siren duyulur. Kamera göz hizasına yaklaşır (0,9 saniye), dalgaya bakar (1,5 saniye), arkadaşına döner (1,8 saniye), sonra yeniden dalgaya döner (1,8 saniye). Dalga bütün bu sırada ilerler.
6. Kırılan dalganın yüzeyi objektifi tamamen sarar. Aynı deniz shader/paletini kullanan yakın yüzey tüm ekran kenarlarını aşar; gökyüzü, iskele veya sinema bandı boşluğu kalmaz. Başlık **“OYUN SONU”** ve alt satır **“UYARILAR HAYAT KURTARIR”**, mavi suyun üzerinde görünür (2,2 saniye).
7. Su 3 saniyede siyaha kararır, yazılar aynı sırada solup kaybolur. Müzik de azalır. **Oyun siyah ekranda biter.** Tsunami raporu, final karnesi veya oyuncu kartı açılmaz. ESC menüsüyle ana menüye dönülebilir.

Toplam sinematik yaklaşık **20,4 saniye** sürer. 19 günlük takvim 2× hızda 114 saniye, önceki üç afet 24 saniyedir; raporlarda/oyuncu işlemlerinde geçen süre hariç toplam yaklaşık **158 saniye** olur. 1/2/3 hız tuşları final sırasında kapalıdır; okunabilir kamera planlarının süresi sabittir. Space ve ESC; gemiyi, kamerayı, yürümeyi, dalgayı ve sireni birlikte durdurur.

## Entegrasyon

- `DisasterController` gerçek tsunami coroutine’ini sinematik içinden bir kez çalıştırır. Normal dalganın renderer’ları saklanır, görünen sinematik dalga aynı modelin kopyasıdır. Güçlü set şehri korur; zayıf sette mevcut yerel su/hasar sonucu hesaplanır. İskele set dışında olduğu için iki ziyaretçinin finali iki durumda da oynar.
- Mevcut afet istatistikleri kaydedildikten sonra `GameManager.AfetSonrasiDevam()` çağrılır. Final kendi siyah katmanını ve arayüz engelini korur. Diğer afetlerin rapor akışı değişmez.
- Limanda `BalikciTeknesi/Model`, özgün gemi mesh ve palet materyalini kullanır (uniform ölçek 1,5). Kaynak prefab/model değiştirilmez. `DeckMarkers` ve `CinematicMarkers` güverte, açık deniz yaklaşımı, yanaşma ve iniş noktalarını tanımlar.
- Gün batımı yalnız bu finalde `IslandWeather.SetCinematicSunset` ile uygulanır. Hafif güneş/ufuk geometrisi mevcut parçacık shader’ını kullanır. Yakın dalga yüzeyi yalnız iki üçgendir; yeni paket veya dış medya gerektirmez.
- Kamera, HUD, renderer durumları ve gün batımı override’ı sahne kapanışında geri alınır; çalışma anında üretilen mesh ve materyaller temizlenir.

## Kurulum ve kontrol

Derleme tamamlanmış Edit Mode’da `Temp/harbor_cinema_istegi` içine `setup` yazmak yalnız mevcut liman alt ağacını yeniler. Ortak ada prefabı MainScene ve Scene_Harita tarafından kullanılır. Sonuç: `Logs/HarborCinemaSetup.txt`.

`Temp/tsunami_finale_istegi` içine `weak`, `strong` veya `both` yazmak gerçek MainScene üzerinde odaklı kontrol çalıştırır. Kontrol; 19. gün tahliyesini, özgün gemiyi, gün batımını, duraklatmayı, bakış sırasını, ekranı kaplayan dalgayı, siyaha solmayı, savunma sonuçlarını ve raporsuz bitişi kapsar. Sonuç ve plan görselleri `Logs/TsunamiFinale_Weak_*` / `Logs/TsunamiFinale_Strong_*` dosyalarındadır.

Son gerçek sahne doğrulaması: **zayıf set 34/34, güçlü set 34/34 (toplam 68/68)**. Merkez ve dört köşenin gerçek render pikselleri siyah olarak doğrulandı. Çalışma zamanı hatası, derleme hatası veya Graphics Ring Buffer uyarısı görülmedi. Plan görselleri incelendi; arkadaşın arkasındaki güneş ufka iniyor.

Windows paketi **0 hata, 210.787.153 bayt, 12,39 saniyede** üretildi (`Logs/JuryWindowsBuild.txt`). Gerçek Windows cihazında oynatılmadı. Dağıtım için `Builds/Windows` klasörünün tamamı taşınmalıdır.
