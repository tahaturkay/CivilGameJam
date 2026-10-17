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
