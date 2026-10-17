# Low-Poly Ada: mimari ve kullanım

## İncelenen mevcut mimari

- Etkileşim: `BuildingVisuals.OnMouseDown` → `BinaTiklandi` olayı → `BinaPopup.Ac(BinaTuru)`. Kök Collider tıklamayı alır; EventSystem UI tıklamalarını engeller. `UIManager` hover bilgisini 3D raycast ile okur. Bir etkileşim interface'i veya trigger tabanlı bina sistemi yoktur.
- Durum: `GameManager` seviye ve hasarı **yapı türü başına** tutar. Bireysel konut kimliği yoktur; bir konutu yükseltmek bütün konutları, bir seti yükseltmek bütün kıyı setini günceller.
- Kurallar: `Ekonomi` para/yükseltme/onarım işlemlerinin tek girişidir. `DengeAyarlari` ScriptableObject sınıfı fiyatları ve 3 seviye sınırını tanımlar. Resources içinde ayar asset'i bulunmazsa mevcut varsayılan değerler kullanılır.
- Görsel: `BuildingVisuals` her kare seviyeyi okur; yalnız değiştiğinde `levelModelleri` içindeki uygun child'ı açar. Seviye modelleri olmayan eski nesnelerde ölçekleme yedeği korunur.
- Yaşam döngüsü: sahneler prefab referansları taşır. Menü `MainScene` açar. Yeni ada editörde üretilip kaydedilir; Play başlangıcında yeniden üretilmez.
- Afetler: `DisasterController` aynı GameManager durumunu değiştirir; bina kök Transform referansları VFX hedefidir. Seviyede düşüş mevcut görsel sisteminde otomatik görünür.
- Kayıt: oyun ilerlemesi için JSON veya başka kayıt sistemi yoktur. PlayerPrefs yalnız ses/ekran ayarları içindir. Ada eklenmesi yeni bir ilerleme kayıt biçimi getirmez.

## Kurulan sahneler

`Assets/Scenes/MainScene.unity` ve `Assets/Scenes/Scene_Harita.unity` aynı `Assets/Prefabs/Salih/LowPolyAda.prefab` dosyasını kullanır. Her iki sahnede tek GameManager, EconomyAndTime, DisasterController, mevcut Ana Canvas ve kamera bulunur. Yangın/depremin bina referansları, yağmur, su, dalga ve kamera bağlantıları atanmıştır. Tsunami çarpma Z koordinatı yeni kıyı setinden alınır.

```text
LowPolyAda
├── Arazi
├── Yollar
├── YukseltilebilirYapilar
│   ├── Konut_01 … Konut_40
│   ├── AKUT_Birimi
│   ├── Itfaiye
│   ├── Okul
│   └── Altyapi_PompaIstasyonu
├── SabitAlanlar
│   ├── Belediye
│   ├── Park
│   ├── Kumsal
│   ├── Lunapark
│   ├── Otopark
│   ├── Orman
│   ├── Tepe
│   ├── TicaretVePazar
│   ├── Liman
│   └── SelaleVeDere
└── DenizSeti
    └── DenizSeti_01 … DenizSeti_72
```

Yükseltilebilir her kökte `BoxCollider`, `BuildingVisuals`, `Level_1`, `Level_2`, `Level_3` vardır. Başlangıç seviyesi 1'dir; seviyeler arasında bina eni/yüksekliği, set yüksekliği ve kalınlığı artar. Aktif model değişince kök BoxCollider model sınırlarına uyar. Bina kökleri değişmediği için afet referansları korunur.

Tsunami modelinin FBX Read/Write ayarı CPU dalga animasyonu için açılmıştır. Animator okunamayan mesh durumunda her kare hata üretmek yerine bir uyarıyla durur; animasyon sırasında mesh sınırlarını günceller.

Sabit yedi alan çevre elemanıdır; yükseltme scripti, ücret veya sahte seviye atanmaz. Mevcut sel savunması oynanabilir kalsın diye Altyapi servis noktası da eklenmiştir.

Geniş tasarımda kıyı yaklaşık 190×146 birimdir; önceki adanın yaklaşık beş katı kara alanı sunar. Belediye ve kent meydanı merkezde, hizmet kampüsleri merkez çevresinde; 40 bahçeli konut bağlı yol adalarına dağılır. Arka kuşakta katmanlı kayalıklar, yoğun orman, şelale ve dere; kıyıda liman ve sosyal alanlar vardır. 72 set parçası aynı kıyı noktalarının ardışık çiftlerinden oluşturulur. Setlerde eğimli temel, payanda, harpuşta, burç ve kule detayları; dış kıyıda kaya dolgusu ve köpük geometrisi kullanılır.

## Görsel sunum ve kamera

- Konut ve okul modelleri seviyeye göre tek, iki ve üç katlı mimariye geçer; çatı, pencere, balkon ve cephe elemanları ayrı geometridir. İtfaiye dört garaj ve büyüyen idare katlarına, AKUT helipad ve kurtarma ekipmanlarına sahiptir.
- Dekoratif geometri çok materyalli tek meshler halinde birleştirilir; etkileşim kökleri ve seviyeler ayrı tutulur. İlk üretimin kullanılmayan ara meshleri temizlenmiştir.
- `IslandPresentation` geniş kamera mesafesinde görünür gölgeleri, ortam ışığını, AO'yu ve sahneye özel renk profilini kurar. PC gölge haritası 4096, gölge mesafesi 450 birimdir.
- `CivilIsland/Stylized Ocean` shader'ı denizde hareketli dalga/parıltı üretir. Dalga ve sel efektlerinin boyutları, güzergâhı ve yüksekliği yeni ada sınırlarından türetilir.
- `IslandCameraController`: tekerlekle imlece doğru yakınlaştırma, sağ/orta tuşla sürükleme, WASD/oklarla kaydırma, Home ile tam ada görünümü. UI/popup ve afet animasyonu sırasında kamera girdisi engellenir.

## Editör araçları

- `Oyun > Ada Haritası > Install Low Poly Island`: ortak prefabı ve iki sahnenin bağlantılarını yeniden kurar. Mevcut açık sahneleri kaydeder; iki sahnedeki bilinen ada köklerini değiştirir. Elle harita düzenlediysen yeniden kurmadan önce değişikliklerini koru.
- `Oyun > Ada Haritası > Validate Island`: iki sahnedeki hiyerarşi, model, kategori, yönetici ve afet referanslarını kontrol eder.
- `Oyun > Ada Haritası > Test Island In Play Mode`: gerçek Ekonomi/GameManager/BuildingVisuals akışında 1→2→3, görsel büyüme, ortak seviye, maksimum sınır, düşüş, hasar/onarım, collider, set çevresi ve okul popup'ını kontrol eder. Test değişiklikleri Play modu kapatılınca geri alınır.

Ada kontrol çıktısı `Logs/AdaDogrulama.txt`; Level 1/3 önizlemeleri `Logs/LowPolyAda_Level1.png` ve `Logs/LowPolyAda_Level3.png` dosyalarındadır. Mevcut genel oyun testi `Araclar > Otomatik Oyun Testi` üzerinden çalışır; `Temp/otomatik_test_baslat` dosyasına sahne yolu yazılarak MainScene üzerinde de koşturulabilir.

## Doğrulama sonucu — 17 Ekim 2026

- Unity 6000.6.6f1 içinde script derlemesi ve her iki sahnenin yapısal kontrolü geçti.
- Geniş sürümde 116 etkileşimli nesnenin (40 konut + 4 kampüs + 72 set) bağlantıları doğrulandı.
- Ada Play testi: bütün türlerde 1→2→3, ortak seviye, büyüme, aktif collider, seviye sınırı, düşüş, hasar/onarım, 180 yönden kapalı set çevresi, okul tıklaması/quiz düğmesi, CPU dalga animasyonu ve kameranın tam ada görünümüne dönüşü geçti.
- Mevcut `OyunTestRobotu`, yeni MainScene üzerinde **56 geçti, 0 başarısız** verdi; yangın, sel, tsunami, ekonomi, UI ve oyun sonu akışı çalıştı.
- Son test turunda derleme, mesh erişimi ve boş referans hatası görülmedi. Level 1 ve Level 3 görüntüleri görsel olarak incelendi.

Önizlemeler: [Level 1](images/LowPolyAda_Level1.png), [Level 3](images/LowPolyAda_Level3.png), [Kent merkezi yakın planı](images/LowPolyAda_Detail.png), [Dalgakıran yakın planı](images/LowPolyAda_Breakwater.png).

## Canlı şehir ve afet görselleri

- `Yollar` altındaki 12 yol parçası `BuildingVisuals/Altyapi` kullanır. Bütün altyapı birlikte yükselir: Lv1 toprak, Lv2 çakıl, Lv3 asfalt ve yaya geçitleri. Pompa istasyonu aynı altyapı seviyesini paylaşır. Hasarlı altyapı `RoadNetworkState` ile onarım dubalarını gösterir.
- `GameManager.HasarVer(tür, afet)` hem seviyeyi bir düşürür (en az 1) hem onarım ihtiyacını ve afet türünü tutar. `BuildingDamageVisuals` yangında kararma/is/duman, depremde eğilen ve basılan model + enkaz üretir. Onarım malzeme renklerini ve modeli geri getirir. Bunlar mevcut tür başına ortak hasar modelini izler.
- Başarılı ekonomi işlemi `YapiIslemiTamamlandi` olayını yayınlar. Aktif görsel 0.2 saniyelik hafif ölçek vurgusu ve beyaz parlamayla tepki verir; shared materyaller değiştirilmez.
- `IslandCityLife`, runtime'da 95 insan, 5 sokak hayvanı ve 12 araç oluşturur. Yayalar kaldırım grafında yürür; araçlar yol döngülerini kullanır. Afet davranışı farkındalık düzeyine bağlıdır (aşağıdaki güncelleme). Bu kalabalık görsel nüfus temsilidir; GameManager nüfus sayısını değiştirmez.
- `IslandWeather`, yangında sıcak/kızıl atmosfer; selden bir gün önce gri bulutlar; sel sırasında koyu hava, yağmur ve kısa şimşek aydınlanmaları uygular. Bulutlar kamera görüşünün üst kenarında tutulur. Işık ve postprocess profili runtime kopyası üzerinden değiştirilir ve geri yüklenir.
- Sel: hasarsız Lv2 altyapı yeterlidir. Yetersizse setin iç sınırındaki opak, kirli açık mavi su zeminden yalnız 0.42 birim yükselir. Sular çekildiğinde yol hasarı onarılana kadar kalır; bu sırada trafik hareket etmez.
- Tsunami: hasarsız Lv3 set yeterlidir. Dalga denizle aynı materyali kullanır; güçlü sette çarpma noktasında eriyerek geri çekilir. Yetersiz sette ada boyunca ilerlerken arkasında opak sel suyu bırakır (zemin +0.85); görüşten çıkar ve su çekilir.
- Afet boyunca gün sayacı ve yapı işlemleri durur; tahliye ve efektler sürer. Afet raporu mevcut Devam akışını kullanır.
- HUD ikonları önceki ada sürümüne göre 2 kat; bina popup'ı nominal 2 kat boyuttadır ve küçük ekrana sığacak şekilde sınırlandırılır. Günlük gelir ve rapordaki vefat sayacı yaklaşık 1 saniyelik yavaşlayarak biten animasyonla gösterilir.

Yeni kapsamlı entegrasyon testi için `Temp/ada_sistem_testi` dosyası oluşturulur. Test gerçek MainScene üzerinde yaya/araç hareketini, hasar/onarımı, vurgu animasyonunu, sayaçları, hava tahminini, savunmalı/savunmasız sel ve tsunamiyi doğrular; sonucu `Logs/AdaSistemTesti.txt` içine yazar ve Play modundan çıkar. Testler sahne asset'lerine çalışma zamanı durumunu kaydetmez.

### Güncellenmiş kapsamın doğrulanması

- İki sahnede **128 etkileşimli nesne**: önceki 116 yapıya ek olarak 12 yol parçası. Her yol üç seviye modeline ve mevcut tıklama/ekonomi bağlantısına sahiptir.
- Ada Play doğrulaması, 1→2→3 modellerini, collider'ları, kapalı set çevresini ve popup bağlantısını geçti.
- Yeni sistem entegrasyon testinde **40 kontrol geçti**: canlı hareket, ölçek/beyaz vurgu, gelir/vefat sayaçları, yangın ve deprem sonrası kalıcı hasar/onarım, hava tahmini, savunmalı/savunmasız sel ve tsunami, tahliye ve sığınaktan dönüş. Yangında **48/48 yaya** toplanma noktalarına ulaştı.
- Alev, duman, yağmur ve is için ortak saydam particle shader'ı kullanılır. Bitmap alfa dokuları runtime'da üretilir; shader referansı Resources materyali ile player build'e dahil edilir. Görünürlük gerçek oyun kamerasında da kontrol edildi.
- Yol ışıkları/ağaçları ayrı bir sabit mesh grubunda birleştirildi. Kaydedilen ada meshleri yalnız kullanılan vertex kanallarını içerir; seviyelerin ayrı açılıp kapanması korunur.

Son genel MainScene oyun testi: **56 geçti, 0 başarısız**. Yeni afet sistem testi: **40 geçti**. Son turda C# derleme, shader derleme ve boş referans hatası görülmedi.

Yeni görsel örnekler: [Yangın ve tahliye](images/LowPolyAda_Fire.png), [Deprem hasarı](images/LowPolyAda_Earthquake.png), [Büyütülmüş yapı popup’ı](images/LowPolyAda_Popup.png).

18 Ekim güncellemesi: NPC çizimleri ve temiz bina materyal blokları optimize edildi; kamera doğrulama çekimleri ayrı karelere dağıtıldı. 4K stres testinde çizim çağrıları yaklaşık %33,5 azaldı ve yeni Graphics Ring Buffer uyarısı görülmedi. Son sistem testi **44 kontrolü geçti**. Ölçümler, sınırlar ve Git kilidi düzeltmesi: [GraphicsBufferFix.md](GraphicsBufferFix.md).

18 Ekim sonraki kapsam: **95 insan + 5 hayvan**, araç/ev çeşitleri, gece–gündüz ve deniz feneri, opak sel suyu, yakın kamera, coin UI, farkındalığa bağlı tepkiler ve ek deprem hasarı uygulandı. Güncel entegrasyon **48**, genel oyun testi **56** kontrolü geçti; yeni özellik testi üç çözünürlükte geçti. Ayrıntılar ve 4K ölçümler: [IslandPolish.md](IslandPolish.md).
