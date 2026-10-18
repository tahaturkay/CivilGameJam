# Afet kontrolü, trafik ışıkları ve ODTÜ kimliği

## Uygulama

- Kamera afet sürerken kilitlenmez. WASD, sürükleme ve zoom mevcut giriş yolundan çalışır. Deprem/tsunami sarsıntısı navigasyon konumuna eklenen geçici bir ofsettir; afet sonunda eski kamera konumuna dönülmez. Gerçek modal/pause menüsü kamerayı engellemeye devam eder; HUD'dan simülasyon durdurulduğunda haritada gezinilebilir.
- HUD durdurması artık ölçekli Unity zamanını durdurur. Afet coroutine'leri, yayılma/hasar, parçacık saatleri, su/dalga hareketleri ve shader zamanı birlikte bekler. Devamda önceki pozitif zaman ölçeği geri gelir. 1×/2×/3× seçimi mevcut gün/NPC hız çarpanını kullanmayı sürdürür; afetlerin normal 8 saniyelik süresi değiştirilmez.
- Selin önceki +0,30 m düzlemi, yükseltilmiş yol yüzeyinin altında kalıyordu. Kayıtlı seviye +0,52 m oldu; ayrıca aktif yol meshlerinin üst yüzeyi ölçülerek en az 0,18 m su payı sağlanır. Böylece su yolları da sürekli örter. Su kirli açık mavi, opak ve setin iç sınırlarıyla kısıtlı kalır; dağlar/eğimli yüksek yerler doğal olarak yüzeyin dışında kalır.
- Dere yatağı karar bileşenleri iki oyun sahnesinden çıkarıldı. Para/farkındalık ödülü, sel kayıp çarpanı, ek rapor satırı ve karar geri bildirimi bağlantıları kaldırıldı. Eski dosya referansları için kalan uyumluluk sınıfı hiçbir popup veya oyun etkisi üretmez.
- Araçların kullandığı iki dolaşım halkasının sekiz köşesinde 16 trafik ışığı ve dur çizgileri bulunur. İki yaklaşım sırayla yeşil → sarı → iki yön kırmızı boşaltma evresi alır. Yeni araç yalnız yeşilde girer; kavşağa girmiş araç boşaltmadan karşı yön yeşil alamaz. Kırmızıda bekleyen araç, yeşil yaklaşımın önceliğini sahiplenmez. Mevcut mesafe ve döndürülmüş izdüşüm çakışma kontrolleri korunur.
- Bina kökleri ve enum'lar aynı kalır: `Belediye`/`Okul` bağlantıları kırılmaz. Görünen belediye tabelası **KALKANLI MUHTARLIĞI**, üç okul seviyesi ve UI adı **ODTÜ** oldu. Beş bayrakta, gönderilen referansın D/5 aralıklı daire geometrisini kullanan beyaz amblem ve kırmızı kumaş vardır; amblem iki yüzde de mesh olarak çizilir. Eski birleşik görselden yalnız 120 eski kumaş/kare üçgeni çıkarıldı, mimari ve direkler korundu.

Karar kaldırıldıktan sonra denge simülasyonu da tekrar üretildi: beş oyun sonu hâlâ erişilebilir. Dikkatli politika dört afeti savunur, 78 vefat ve 1391 coin ile biter; buna konut ve ODTÜ Lv3 alımları eklenince dört savunma ve 1184 coin korunur. Fiyat veya kart eşiği değiştirmek gerekmedi. Bunlar tanımlı politikaların simülasyonudur; kayıt `docs/JuryBalance.json`.

Kavşak dur çizgileri şerit kayması hesaba katılarak fiziksel boşaltma alanının dışında tutulur. Geçiş hakkı yalnız başarılı fiziksel girişte alınır; araç köşeyi geçip alanı terk edince bırakılır. Dar dönüşte öndeki aracın arkasının salınması için aynı şerit takip aralığı 6,5 m’dir. Böylece çakışma korumasının dönüşü karşılıklı kilitlemesi engellenir.

## Dosya grupları

- `Core/IslandSimulationPause`, `UIManager`, `HizKontrol`, `PauseMenu`: ortak durdurma ve anlık düğme bağlantıları.
- `IslandCameraController`, `DisasterController`, `IslandWeather`: bağımsız navigasyon/sarsıntı, durabilen afet akışı ve gerçek yol yüksekliğine göre sel.
- `IslandCityLife`, `IslandTrafficSignal`: kavşak fazları, fiziksel ışıklar, giriş/boşaltma ve çakışma koruması.
- `IslandArtCivic`, `Editor/IslandOdtuIdentity`, ilgili birleşik mesh/ada prefabı: hedefli bayrak ve tabela yenilemesi.
- `DereYatagiKarari`, `AfetRaporu`, `IpucuSistemi`, `AnlatiVerisi`, `BinaPopup`: kaldırılan karar ve yeni görünen adlar.
- `IslandSceneSetup`, `JurySessionValidation`, `IslandRevisionValidation`, iki sahne/controller prefabı: kalıcı kurulum ve tekrar kurulum uyumluluğu.
- `Test/IslandRevisionTestRobotu`: gerçek afetler içinde kamera, durdurma, su seviyesi ve 180 simülasyon saniyelik trafik akışı kontrolü.

`GameManager.cs` değiştirilmedi. Yeni paket yüklenmedi. Görsel kalite ayarları azaltılmadı.

## Doğrulama

- Özellik robotu: **48 geçti, 0 kaldı** (`Logs/IslandRevisionTests.txt`). Dört gerçek afet boyunca pan/zoom, durdurulan yayılma/hasar, parçacık saatleri, su/dalga konumları ve devam sonrası kamera konumu doğrulandı. Sel yüzeyi **2,92 m**, yol yüzeyi **2,74 m** ölçüldü. Su kontrolü altyapı kategorisindeki pompa binasını değil, gerçek `Yollar` hiyerarşisini ölçer.
- Son 180 simülasyon saniyelik trafik turu: **137 kavşak geçişi**, her araçta en az **481,3 m** ilerleme; en uzun bekleme **12,5 simülasyon saniyesi**. Döndürülmüş araç izdüşümlerinde örtüşme yok; en küçük pay **0,04 m**. Sekiz kavşağın tüm yeşil/sarı/iki yön kırmızı evreleri ve karşılıklı yeşil olmaması kontrol edildi. Önceki bağımsız trafik turu da 15/15 geçti.
- Genel MainScene robotu: **59 geçti, 0 kaldı** (`Logs/OyunTesti.txt`, 18 Ekim 10:59:57 oturumu). Eski robotun elle başlattığı afetlerde yalnız takvim durdurulur; gerçek HUD durdurması geçersiz kılınmaz. Yarıda kesilen testler geçici gün süresini geri yükler.
- Hedefli prefab/sahne kurulumu tamam (`Logs/IslandRevisionSetup.txt`); bayrak, tabela ve sel görüntüleri `Logs/Revision_*.png` altında. Denge simülasyonunda beş son kartın tamamı erişilebilir. `git diff --check` temiz.

Son özellik turunun native editör günlüğünde **1 Graphics Ring Buffer uyarısı** görüldü; ardından genel oyun turunda **0** görüldü. C# çalışma zamanı hatası özellik turunda 0’dır. Bu sonuç, Metal tampon uyarısının bütünüyle giderildiği anlamına gelmez; görsel kalite düşürülmedi.

### Eşzamanlı UI çalışması / Windows

**Güncelleme:** UI çalışması tamamlandıktan sonra ortak API derleme hataları giderildi; yeni UI ile 14 UI ve 48 afet kontrolü geçti, Windows64 paketi başarıyla üretildi. Güncel sonuçlar `UIIntegrationFix.md` içindedir. Aşağıdaki paragraf önceki engelin kaydıdır.

Kullanıcı, menü/rapor/ayarların ayrı bir sohbette eşzamanlı düzenlendiğini doğruladı. Yukarıdaki testler çalışan oyun derlemesinde alındı; daha sonra gelen UI sürümünü doğrulamaz. Yeni UI çağrılarının ortak `CivicUI` API’siyle uyuşmaması nedeniyle son Windows paketleme girişimi derlenemedi (`Logs/JuryWindowsBuild.txt`: Unknown/0 bytes). Yeni bir Windows paketi üretildiği iddia edilmez; mevcut exe önceki sürümdür. Bu çalışmanın geçici UI uyumluluk ekleri kaldırıldı, ayrı UI tasarımına müdahale bırakılmadı ve bekleyen paketleme isteği yoktur. UI derleme hataları giderilince Windows paketi yeniden alınmalıdır; gerçek Windows cihazında çalıştırma yapılmadı.
