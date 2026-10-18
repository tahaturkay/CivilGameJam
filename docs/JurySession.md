# Üç dakikalık jüri akışı ve şehir tepkileri

> Bu rapor ilk jüri düzenlemesinin kaydıdır. Dere yatağı kararı sonraki kullanıcı isteğiyle kaldırıldı; güncel afet/kamera/trafik davranışı `IslandInteractionRevision.md`, güncel karar içermeyen denge sonuçları `JuryBalance.json` içindedir. Aşağıdaki 174,3 saniye ölçümü kararın hâlâ bulunduğu önceki sürüme aittir.

## Başlangıç analizi

`DengeAyarlari.asset` başlangıçta alanları kaydetmediği için sayılar `DengeAyarlari.cs` varsayılanlarından geliyordu: 28 gün, 12 saniye/gün, 7/14/21/28 afet takvimi, 500 coin ve 1000 nüfus. `DisasterController.afetSuresi` 15 saniyeydi; ada kurucusu da 15 yazıyordu. Hız yalnız gün sayacını etkiliyor, afet animasyonları ve raporlar hızdan bağımsız çalışıyordu.

1. günden son afetin başladığı 28. güne 27 gün aralığı vardır. Popup ve oyuncu beklemesi hariç önceki süre: 1× **324 + 60 = 384 sn**, 2× **162 + 60 = 222 sn**, 3× **108 + 60 = 168 sn**. Rapor/quiz beklemeleri sınırsız olduğundan gerçek insan oynanışına kesin üst sınır verilemez.

Mevcut mimari korunur: `GameManager` tür başına seviye ve hasar tutar, `Ekonomi` tek yükseltme/onarım girişidir, `EconomyAndTime` gün/gelir üretir. Binalar `BuildingVisuals` üzerinden seçilir. Kayıt sistemi eklenmedi. `GameManager.cs` değiştirilmedi.

## Yeni denge

- 20 gün; afetler 5/10/15/20. Başlangıç 2×; gün süresi 12 sn, afet süresi 8 sn.
- Temel zaman: **19 × 12 / 2 + 4 × 8 = 146 sn**. Rapor, iki quiz ve bir karar için yaklaşık 30 sn etkileşim payı.
- Başlangıç 500 coin; mevcut yükseltme ve onarım fiyatları korunur.
- Konut Lv1/Lv2/Lv3 günlük gelir çarpanı **1 / 1,35 / 1,75**. Nüfus kapasitesi ve göç etkisi de sürer; hasarlı konut geliri yarıya iner.
- Quiz doğru cevap ödülü 25 farkındalık; okul tavanları 40/70/100. Böylece kısa bir oturumda iki anlamlı quiz yeterli ilerleme sağlar.
- Yıldızlar: toplam kayıp ≤80 / ≤180 / ≤320 için 3 / 2 / 1 yıldız.
- Para birimi, önceki kullanıcı tercihiyle **coin**. Ekteki TL etiketleri uyum için coin oldu.

`docs/tools/jury_balance.py` yasal başlangıç parası, fiyat, nüfus, okul tavanı, onarım ve hasarlı AKUT sırasını kullanır. Ayrıntılı satın alma zamanları ve sonuçlar `docs/JuryBalance.json` içindedir. Bu, bütün rastgele oyunlar için başarı garantisi değil, açıkça tanımlı politikaların simülasyonudur.

## Oyun ve görseller

Yangın rastgele bir kaynaktan yakın binalara yayılır; düşük itfaiyede en çok 9 yapı, yeterli savunmada kısa süreli sınırlı alevler görülür. Yanmayan örnekler kararmadan ve küçülmeden kalır. Yerel hasar satın alınmış tür seviyesini düşürmez; onarım ihtiyacı ve görsel hasar ayrı tutulur. Depremde yalnız eşiğin altında kalan türlerden seçilmiş yapılar çöker; sel yolların ve düşük bölgelerin bir bölümünü hasarlar. Mevcut tür bazlı onarım, o grubun hasarlı örneklerini birlikte onarır; yükseltmeler de mevcut tür bazlı mimaride kalır.

Dışarıdaki insanlar su akıntısıyla sürüklenir; çatının altına ulaşanlar korunur. Tsunami akıntısı kişileri adanın dışına taşıyabilir. Bunlar ekonomik nüfus hesabının temsili görselleridir; nüfusu yalnız afetin tek kayıp hesabı değiştirir. Suya yakalanan araçlar kaldırıma savrulur ve eğilir; altyapı onarılmadan trafiğe dönmez. Trafikte başlangıç yerleşimi, şerit mesafesi, kavşak önceliği ve döndürülmüş araç izdüşümü birlikte kontrol edilir.

Tsunaminin set darbesinde köpük/su püskürmesi, kısa kamera sarsıntısı ve üretilmiş vurma sesi vardır. İç su seviyeleri zemine göre selde +0,30 m, tsunamide +0,62 m; kirli açık mavi ve opaktır. Deniz şeritleri kıyıya akar; tek çevre meshinde kıyı köpüğü fırtınada güçlenir. Gece ışığı 0,28 yerine 0,30 tabanına hafifçe açılır. Kırılan çatı yüzeyleri silinmek yerine çökmüş parçalar halinde kalır.

Ana menü, duraklatma ve ortak ayarlar paneli lacivert/krem/petrol/kehribar paletinde yenilendi. Ayarlarda tuş rehberi vardır. Normal bina kartı seçilen yapının yanında açılır, zamanı durdurmaz; quiz modal kalır. İpuçları tek kartta ve iki satırdır; hız düğmesi eğitimi kaldırılmıştır.

## Anlatı

Hikâyeler, son kartlar, dere yatağı kararı ve ipuçları `Assets/Resources/AnlatiVerisi.asset` içindedir; Inspector ile düzenlenebilir. İyi/kötü hikâye `sonSavunmaYeterli` ile seçilir; deprem iyi sonunun farkındalık varyantı 60 eşiğini kullanır. 7. gündeki karar bir kez açılır ve zamanı durdurur. Kabul +150 coin ve sel kaybı ×1,5; ret okul tavanına uyarak +5 farkındalık verir. Ayrı güven/yan görev sistemi kurulmadı.

Epilog önceliği: Gerçek Lider → Kefenin Cebi Yok → Beton Tabutlar → Çadırkent Şampiyonu → Yorgun Başkan. Lider: 4 savunma, farkındalık ≥60; kasa: kalan ≥900 ve kayıp ≥240; beton: ≥3 savunma ve farkındalık <40; çadır: ≤2 savunma, farkındalık ≥60, kayıp ≤320. Yalnız tek kart gösterilir.

## Dosya sorumlulukları

- `Core/DengeAyarlari`, kaynak asset, `Ekonomi`, `UI/HizKontrol`: takvim, başlangıç hızı, gelir ve quiz dengesi.
- `Taha/DisasterController`, prefab ve iki sahne: süre, yerel hasar, su sorgusu, darbe ve kararın kayıp etkisi.
- `Salih/Buildingvisuals`, `BuildingDamageVisuals`, `RoadNetworkState`, `IslandCityLife`: örnek hasarı, kalıcı duman/çatı, seçili yol işaretleri, akıntı ve trafik.
- `Salih/IslandCoastWaves`, `IslandSeaImpactSplash`, `IslandEffectsFactory`, kıyı shader/materiali, ocean shader ve `IslandWeather`: kıyıya akan köpük, setin üzerinden görünen darbe püskürmesi, ses, daha yoğun hasar dumanı ve çok az daha aydınlık gece.
- `UI/MainMenu`, `SettingsPanel`, `PauseMenu`, `BinaPopup`, `CivicUI`, `CityMenuArt`, `BuildingCardDismissSurface`: menüler, tuş rehberi, dinamik bina kartı ve ortak keskin UI çizimi.
- `Core/AnlatiVerisi`, `UI/DereYatagiKarari`, `AfetRaporu`, `OyunSonu`, `IpucuSistemi`: mevcut ekranlara anlatı ve tek karar.
- `Editor/IslandArtLandscape`, `IslandSceneSetup`, `IslandPresentation`, `JurySessionValidation`: T kavşak işaretleri, kalıcı sahne bağlantıları ve tekrar çalıştırılabilir kurulum.
- `Core/IslandPerformanceBudget`, URP ayarları: 60 FPS hedefi; gölge atlası 4096→2048, 4→2 kaskat, daha kısa gölge mesafesi, yarım çözünürlük AO, kullanılmayan opaque kopyası ve ek ışık gölgelerinin kaldırılması. Çizim ölçeği korunur.
- `Test/OyunTestRobotu`, `JuryGameplayTestRobotu`, `JuryHazardProbe`, `JuryMenuCapture` ve `docs/tools/jury_balance.py`: mevcut testlerin yeni takvime uyumu, gerçek akış, ölçülen süre, sekiz afet dalı, menü görüntüleri ve yasal denge senaryoları.
- `Assets/Resources/AnlatiVerisi.asset`, `DengeAyarlari.asset`, `IslandEffects/CoastFoam.mat`; `LowPolyAda.prefab`, `DisasterController.prefab`, birleşik yol işareti meshi, `MainMenu`, `MainScene`, `Scene_Harita` sahneleri: anlatı verisi, kalıcı denge, köpük, doğru kavşak çıkışları ve gerekli bileşen bağlantıları. Unity'nin Windows paketlemesinde serileştirdiği URP çalışma kaynakları da korunur.

Önceki kamera/imleç/gün döngüsü çalışmasının dosyaları çalışma alanında korundu; bu turda yeniden sıfırdan yazılmadı.

## Doğrulama

Gerçek Unity Play Mode ölçümü: **174,3 saniye (2 dakika 54 saniye)**. 1. günden 20. gün oyun sonuna doğal gün sayacıyla gidildi: 12 saniyelik gün, başlangıç 2×, Unity zaman ölçeği 1. Dört planlı afet, dört raporda dörder saniye okuma, iki quizde dörder saniye okuma, iki bina kartında ikişer saniye ve dere yatağı kararında dört saniye dahildir. Bina kartları sırasında oyun akmaya devam eder. İnsanların okuma ve karar süreleri değişebileceğinden bu ölçüm sabit bir senaryodur; serbest oynanışa kesin 180 saniye sınırı koyulmadı.

| Kontrol | Sonuç | Yerel kayıt |
|---|---:|---|
| Mevcut genel oyun robotu | 59 geçti, 0 kaldı | `Logs/OyunTesti.txt` |
| Yeni odaklı özellik robotu | 36 geçti, 0 kaldı | `Logs/JuryFocusedTests.txt` |
| Doğal 2× jüri oturumu | 9 geçti, 0 kaldı; 174,3 sn | `Logs/JuryTimedFinal.txt`, `Logs/JurySessionTiming.txt` |
| Sekiz gerçek afet dalı ve yerel hasar | 58 geçti, 0 kaldı | `Logs/JuryHazardTests.txt` |
| Denge simülasyonu | Beş son kartın hepsi yasal alımlarla ulaşılabilir | `docs/JuryBalance.json` |

Son afet turunda çalışma hatası **0**, Graphics Ring Buffer uyarısı **0**. Yangın, sel, deprem ve tsunami savunmalı/savunmasız çalıştırıldı. Ayrıca yanmış bir konutun tür hasarı, yükseltilmiş sağlam komşularını depremde yıkmıyor; yerel yangın satın alınmış konut seviyesini düşürmüyor; kırık çatının yüzeyleri korunuyor. Döndürülmüş araç izdüşümleri afet, durma ve onarım sonrası dönüşte kontrol edildi.

Son araç dönüş değişikliğinden sonra odaklı robot yeniden **36/36** geçti; yerinden savrulan araçlar onarım sonrası çakışmadan rotalarına döndü. Son `git diff --check` temiz; yeni Unity dosyalarının `.meta` dosyaları mevcut.

Ana menü, ayarlar, bina kartı, karar, rapor ve oyun sonu ekranları gerçek oyun görüntülerinden kontrol edildi. `Logs/Jury_MainMenu.png`, `Jury_Settings.png`, `Jury_BuildingCard.png`, `Jury_Decision.png`, `Jury_Report.png`, `Jury_Ending.png` ve `Jury_TsunamiImpact_Protected.png` örneklerdir. Oyun sonu görseli yerleşim önizlemesidir; tamamlanmış doğal oyun süresinin kanıtı ayrı zaman robotu kaydıdır.

Apple M4 / Metal / 3840×2160 editör afet örneklerinde ortalama kare süresi yaklaşık **16,7–19,1 ms** ölçüldü. Editör ölçümü Windows performansı veya eski sürüme göre hızlanma kanıtı değildir. 60 FPS sınırı ve daha düşük gölge/AO maliyeti uygulandı; sıcaklık sensörü ölçümü yapılmadığından derece cinsinden ısı azalması iddia edilmez. Windows derleme sonucu aşağıda kaydedilir; gerçek Windows cihazında çalıştırma ayrıca doğrulanmalıdır.

### Windows paketi

Unity 6000.6.6f1 üzerinden **StandaloneWindows64 derlemesi başarılı, 0 hata**. Ana menü ve MainScene pakete dahil. Derleme 4 dakika 19,7 saniye sürdü; çıktı toplamı 184.198.896 bayt (yaklaşık 175,7 MiB). Kayıt: `Logs/JuryWindowsBuild.txt`.

Çalıştırılabilir dosya: `Builds/Windows/CivilGameJam.exe`. Dağıtımda **Windows klasörünün tamamı** birlikte taşınmalıdır; yalnız exe yeterli değildir. Bu macOS üzerinde alınmış çapraz derlemedir; Windows cihazında açılış, fare/trackpad ve performans denemesi yapılmadı.

### Simülasyon sonuçları

| Politika | Vefat | Kalan coin | Savunulan afet | Kart |
|---|---:|---:|---:|---|
| leader: defend all and answer two quizzes | 76 | 1391 | 4 | Gerçek Lider |
| rich: save cash and accept the flood offer | 715 | 1580 | 0 | Kefenin Cebi Yok |
| concrete: defend every event, keep awareness low, spend on resilience | 99 | 887 | 4 | Beton Tabutlar |
| camp: learn, keep defenses selective, repair after the flood and quake | 222 | 683 | 2 | Çadırkent Şampiyonu |
| fallback: educate and strengthen response, but leave protection incomplete | 395 | 575 | 0 | Yorgun Başkan |
| leader plus housing 3 and school 3 after core defenses | 76 | 1184 | 4 | Gerçek Lider |

Ek Lv3 senaryosu konut Lv3’ü 11., okul Lv3’ü 13. günde satın alır. 500 sabit tohumlu rastgele harcama koşusunda ortalama savunmasız afet 1,73; ortalama kayıp 241,62. Bu politikanın dağılımı 0/1/2/3/4 savunmasız afet için 60/237/24/136/43 koşudur.

### Sınırlar

Yükseltme ve onarım mevcut mimariye uygun olarak yapı türü başına uygulanır; görsel hasar tek tek örneklere aittir. NPC/araç akıntısı, toplam nüfus kaybının bire bir fizik simülasyonu değildir. Rastgele afet ve harcama politikalarında tek bir son garanti edilmez. Yeni paket, kayıt sistemi, güven puanı veya ayrı anlatı yöneticisi eklenmedi; `GameManager.cs` değiştirilmedi; commit/push yapılmadı.
