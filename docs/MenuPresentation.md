# Animasyonlu ana menü, tipografi ve HUD — 18 Ekim 2026

## Menü ve fontlar

- Ana menüde gerçek `MainScene` adasından kaydedilen **12 saniyelik, 1920×1080, 24 FPS** sessiz H.264 kesit döner. Deniz, araçlar, NPC’ler ve hafif kamera hareketi gerçek oyun görselleridir; HUD kayda dahil değildir.
- `IslandMenuBackdrop` bir video ve RenderTexture kullanır; menünün arkasında ikinci bir şehir simülasyonu çalışmaz. Kapak görseli ilk video karesi gelene kadar korunur; video, menü/ayarlar açıkken ölçeklenmemiş zamanda ilerler. Görsel ekranı oranını bozmadan doldurur.
- Geniş ekranlarda menü kartı sola alınarak adaya yer açılır; dar ekranlarda kart ortalanır. Mevcut şerit, kart ve düğme tasarımı korunur.
- Başlık ve ana düğmelerde **Bungee Regular**, gövde/HUD metinlerinde **Nunito SemiBold** kullanılır. İki statik TMP atlasında `İıŞşĞğÜüÖöÇç` doğrudan bulunur; mevcut Türkçe atlas fallback olarak korunur. Kaynak TTF, OFL lisansları ve SHA-256 manifesti `Assets/Fonts/LowPolyTheme` altındadır. Başlıklar kartlarına göre otomatik boyutlanır.

## HUD ve yeni standart

İlk istekle sol HUD 1,5 katına; sağ grubun tasarım alanı 250×124 yerine 325×218’e çıktı. Gün kartı, afet uyarısı ve hız/ayarlar düğmeleri ayrı satırlardadır. Uyarı artık gün sayısını veya düğmeleri örtmez.

Son kullanıcı isteğiyle **bu görünümün ayarlardaki %125 hali yeni %100 standart** kabul edildi. Yerleşimde 1,25 taban çarpanı uygulanır; ayardaki ölçek bunun üzerine %75–%150 aralığındadır. Yeni varsayılan %100, üst sınır yeni standardın 1,5 katıdır. Eski kaydın tekrar çarpılmasını önlemek için ölçek `ui_ust_panel_olcegi_v2` anahtarında tutulur; dikey boşluk tercihi korunur.

Dar ekranda iki grup yan yana sığmazsa sağ grup alt satıra geçer. Ölçek ve 0–80 px kenar boşluğu ekranın güvenli alanına göre sınırlandırılır.

## Space kısayolu

Space, oyun içindeki HUD durdurma yolunu kullanarak durdurur/devam ettirir. Afet sürerken yangın yayılımı ve görsel simülasyon da bekler. Seçili bir UI düğmesinin aynı tuşla Submit almasını önlemek için kısayol EventSystem’den önce ele alınır. Ayarlar, ESC menüsü, quiz, rapor, oyun sonu ve metin girişi açıkken kısayol uygulanmaz.

Space + sol tuş kamera sürüklemesi bu atamayla kaldırıldı. Kamera için WASD/oklar, sağ/orta tuş, tekerlek/trackpad ve Shift + kaydırma korunur. Ayarlar panelindeki tuş rehberi güncellendi.

## Doğrulama

- `Logs/ThemeFontInstall.txt`: iki statik font atlası ve Türkçe kapsamı doğrulandı.
- `Logs/MenuClipCapture.txt`: 288 gerçek oyun karesi başarıyla kodlandı; MP4 12.065.580 bayt.
- `Logs/MenuPresentationTests.txt`: **40 geçti, 0 kaldı**. Video hazırlığı/kare ilerlemesi, fontlar, yeni %100/%150 ayarları ve Space ile gerçek yangının durması/devam etmesi kontrol edildi.
- HUD; gerçek Game view **3840×2160, 1920×1080, 1366×768** boyutlarında, %75/%100/%150 ölçek ve 80 px ek boşlukla doğrulandı. Sol/sağ gruplar ve uyarı/gün/düğme satırları çakışmadı, güvenli alan dışına taşmadı. Normal/yaklaşan/afet günü uyarıları metin alanına sığdı.
- Yeni fontlarla menü, rehber, ayarlar, duraklatma, rapor ve oyun sonu akışı ayrıca **14/14** geçti (`Logs/JuryMenuCapture.txt`; son Space/ölçek ekinden önceki tur).
- Son kontrollü turda C# derleme hatası ve çalışma zamanı exception’ı yok. `git diff --check` temiz.

Önizlemeler: `Logs/MenuPresentation_Menu.png`, `MenuPresentation_Settings.png`, `MenuPresentation_HUD_3840.png`, `MenuPresentation_HUD_1920.png`, `MenuPresentation_HUD_1366.png`.

Windows paketi başarıyla üretildi: **0 hata, 210.440.926 bayt, 12,88 saniye** (`Logs/JuryWindowsBuild.txt`). Font lisansları paketin `ThirdPartyLicenses` klasörüne kopyalandı. Gerçek Windows cihazında çalıştırma yapılmadı; dağıtımda `Builds/Windows` klasörünün tamamı birlikte taşınmalıdır.

Kaydı yeniden üretmek için Edit Mode’da `Temp/menu_presentation_istegi` içine `capture`, sunum testini başlatmak için `test` yazılır. Font atlaslarını yeniden kurmak için `Temp/theme_font_istegi` kullanılır. Yeni Unity paketi yüklenmedi, `GameManager.cs` değiştirilmedi.

Kayıt aracı Unity’nin yerleşik [MediaEncoder API’sini](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/media/mediaencoder) kullanır. Font kaynak/lisans bağlantıları `Assets/Fonts/LowPolyTheme/Sources.json` içindedir.
