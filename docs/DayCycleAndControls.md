# Gün döngüsü, kamera ve imleç

## Kesintisiz gün döngüsü

Eski kararma aralığı günün yalnız %8'iydi: 12 saniyelik günde 0,96 saniye, 3× hızda 0,32 saniye. Hız değiştirmek de gün sayacının bölünme oranını değiştirdiği için saati ileri/geri sıçratabiliyordu.

`EconomyAndTime` artık temel oyun saniyelerini biriktirir; hız sadece ilerleme hızını çarpar. Mevcut saat, gelir, gün ve afet takvimi korunur. `IslandSolarCycle` gün sonu ile yeni gün başlangıcında aynı değer ve eğimi veren periyodik eğriler kullanır.

- Gün doğumunda altın/şeftali ışığı, uzun gölgeler ve yavaşça sönen şehir ışıkları.
- Öğlen daha yüksek güneş, açık ve nötr tonlar.
- Gün batımında sıcak turuncu/pembe ortam, denizde sıcak parıltılar ve yavaşça yanan cam/far/fenerler.
- Gece mavi ay ışığı ve mevcut şehir aydınlatması. %50 üzeri karanlık aralığı günün son %40'ıdır; alacakaranlık bu sınıra yumuşak yayılır.
- Yeni güne geçerken ortam bir anda gündüze dönmez; gün doğumu eğrisinin devamından ilerler. Oyun durakladığında saat ve gün ışığı sabit kalır. Yangın/fırtına/şimşek atmosferi bu paletin üstüne uygulanır.

Bu değişiklik oyun gününün temel süresini veya ekonomik dengeyi değiştirmez. Editörde günü atlama kısayolu kamera sürüklemesiyle çakışmaması için **Ctrl + Shift + Space** oldu.

## Kamera kontrolleri

| Girdi | İşlem |
|---|---|
| WASD / oklar | Yumuşak kamera kaydırma |
| Sağ veya orta mouse tuşu + sürükleme | Haritayı tutup sürükleme |
| Space + sol tuş/trackpad tıklaması + sürükleme | Tek tuşlu cihazda kamera kaydırma |
| Tekerlek / dikey iki parmak kaydırma | İmlecin gösterdiği noktaya doğru yumuşak zoom |
| Shift + iki parmak kaydırma | İki eksende kamera kaydırma |
| Yatay iki parmak kaydırma | Yatay kamera kaydırma |
| E / Q | Yakınlaşma / uzaklaşma |
| Home | Bütün adayı çerçeveleme |

Kamera ada sınırlarında ve zoom limitlerinde kalır. Popup, quiz, oyun sonu ve afet sırasında hareket ivmeleri temizlenir. WASD, imleç HUD üstündeyken de çalışır; metin alanına yazarken çalışmaz. Space ile sürükleme binaları açmaz. Yerel pinch hareketi için ayrı bir platform eklentisi kullanılmadı; touchpad zoom'u Unity'nin kaydırma girdisini kullanır.

## Seçilen özgün imleç

Kullanıcı üç öneri arasından **C — Fener Oku** tasarımını seçti. Lacivert dış hat, fildişi ve kehribar yüzeyler deniz fenerinin paletinden gelir. 64×64 şeffaf PNG ve SVG kaynakları `Assets/Art/Cursors/` içindedir; tıklama ucu **(6,5)**.

`IslandCursor` iki ada sahnesinin kameralarına bağlanır. Player'ın varsayılan imleci de aynı görseldir. Oyun odağı kaybolunca runtime imleci serbest bırakılır.

[Üç tasarımın karşılaştırması](images/cursors/CursorOptions.png)
