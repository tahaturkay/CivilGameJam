# Türkçe, şehir hızı, siren ve arayüz ayarları

## Davranış

- Türkçe harfler ana TMP atlasında eksikti. Mevcut Liberation Sans kaynağından statik, build'e dahil bir atlas üretildi: `Assets/Resources/Fonts/LowPolyTurkish SDF.asset`. Ana menü, oyun, pasif paneller ve sonradan üretilen metinler bu fallback'i kullanır. Büyük harf dönüşümü Türkçe kültürüne uyar. Script, kaynak verisi ve sahne metinlerinde UTF-8 bozuk karakter taraması temiz.
- İnsan, kedi/köpek, araç, yürüme animasyonu ve suyla sürüklenme, mevcut `hizCarpani` ile 1×/2×/3× çalışır. HUD durdurması, menü/quiz/rapor duraklaması hepsini durdurur. Afetin gün sayacını durdurması, hareketleri kendiliğinden durdurmaz. Afet efektlerinin 8 saniyelik süresi korunur.
- Afet günlerinde güneş/gece karartması uygulanmaz. Yangın kızıllığı, sel bulutları, yağmur ve şimşek ayrı hava katmanı olarak korunur. Önceki akşam ve sonraki sabah geçişi yumuşatılır; diğer günlerin gece/gündüz döngüsü sürer.
- Belediye meydanında bir siren kulesi vardır. Tsunamiden bir gün önce kırmızı döner ışık ve hafif yerel yansımalar başlar. Farkındalıktan bağımsız tüm canlı aktörler en yakın eve yönelir; tsunami günü evde kalır. Diğer afetlerde önceden uyarı yoktur, ışık yalnız afet boyunca çalışır. Üç yerel ışık da gölge üretmez; şehir geneline kırmızı filtre eklenmez.
- Hasarlı yol engelleri A ayak, ağırlıklı taban, hacimli kırmızı/krem şeritler, reflektörler, koniler ve üç piktogram içerir. Mesh ve malzemeler paylaşılır. Yolun iki yanında, suyla savrulmuş araçlara yer bırakan bir konumda dururlar; ek fizik collider'ı yoktur.
- Ayarlar → Üst panel: boyut %75–125, üst kenar boşluğu 0–80 px. Ayarlar anında uygulanır, kaydedilir ve tek düğmeyle sıfırlanır. Dar ekranlarda gruplar güvenli alana sığdırılır.

## Dosyalar

- `UI/TurkishTMPFontSupport`, `UI/Editor/TurkishFontAssetInstaller`, font kaynak asseti: kalıcı Türkçe glyph desteği.
- `UI/UIManager`, `SettingsPanel`, `CivicUI`, `BinaPopup`: tüm UI fontları, Türkçe büyük harf ve kaydedilen HUD ayarları.
- `Core/IslandDisasterCalendar`, `Salih/IslandWeather`: mevcut afet takviminden gündüz ve uyarı penceresi.
- `Salih/IslandCityLife`: seçili hız, tam duraklama ve tsunami öncesi/afet günü sığınma.
- `Salih/IslandSirenController`, `IslandSirenArt`, `IslandCivicModelParts`: takvime bağlı tek siren ve ortak düşük poligonlu model kaynakları.
- `Salih/IslandRoadBarrierArt`, `RoadNetworkState`: yeni yol çalışma modelleri.
- `Editor/IslandSceneSetup`, `CityFeedbackValidation`, ada prefabı ve iki oyun sahnesi: mevcut sahneye ekleme ve gelecekteki kurulumlarla uyumluluk.
- `Test/CityFeedbackTestRobotu`: yeni davranışların gerçek sahne üzerinde kontrolü.

`GameManager.cs` değiştirilmedi. Yeni paket eklenmedi. Önceki görsel kalite/URP ayarlarına bu turda dokunulmadı.

## Kontroller

- Yeni gerçek sahne kontrolleri: **42 geçti, 0 kaldı**, çalışma hatası 0. Türkçe üst/alt harfler 101 aktif/pasif TMP metninde çözümlendi. Normal bir NPC'nin ölçülen hızları yaklaşık **2,37 / 4,73 / 7,10 m/oyun saniyesi**; araçta **4,27 / 12,82 m/oyun saniyesi** (1×/3×). Gerçek frame süresine göre ölçüm yapıldı; kayıt/screenshot kaynaklı kare gecikmesi hız hesabını çarpıtmıyor.
- Farkındalık 0 iken 100 canlı aktörün tamamı uyarı gününde eve ulaştı ve gerçek tsunami boyunca içeride kaldı. Dört afetin gece katmanı kapalı, üç diğer afetin sireni yalnız olay sırasında aktif. HUD durdurması hareketleri ve siren rotorunu tutuyor.
- Su/araç regresyonu: **36 geçti, 0 kaldı**. Güncellenen durdurma davranışı için testler aktif afetin zaten gün sayacını durdurduğunu kullanıyor; NPC hareketi beklenirken HUD durdurması açılmıyor. Savrulan araçlar onarım sonrası rotaya çakışmadan döndü. Kayıt: `Logs/CityFeedbackTrafficRegression.txt`.
- Ana menü, ortak ayarlar, bina kartları, siren ve yeni engeller gerçek görüntülerden kontrol edildi. `Logs/Jury_MainMenu.png`, `CityFeedback_Settings.png`, `CityFeedback_Siren.png` ve `CityFeedback_BarrierDetail.png`.

Genel oyun regresyonu da **59 geçti, 0 kaldı** (`Logs/OyunTesti.txt`). Son `git diff --check` temiz. Yerel ana test kaydı `Logs/CityFeedbackTests.txt`.

Güncellenmiş **StandaloneWindows64 paketi başarılı, 0 hata**: `Builds/Windows/CivilGameJam.exe`. 12,1 saniyelik derleme, 188.442.210 bayt çıktı. Kayıt `Logs/JuryWindowsBuild.txt`. Exe ile birlikte **Windows klasörünün tamamı** taşınmalıdır. Windows üzerinde gerçek cihaz oynanışı bu macOS ortamında doğrulanmadı.
