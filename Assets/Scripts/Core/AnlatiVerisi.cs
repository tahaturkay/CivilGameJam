using System;
using UnityEngine;

[CreateAssetMenu(fileName="AnlatiVerisi",menuName="Oyun/Anlatı Verisi")]
public sealed class AnlatiVerisi : ScriptableObject
{
    [Serializable] public struct Hikaye
    {
        public AfetTuru afet;
        public string karakter;
        [TextArea] public string iyi,kotu,yuksekFarkindalik,dereYatagi;
    }
    [Serializable] public struct Kart {public string baslik;[TextArea] public string metin;}
    [Serializable] public struct Ipucu {public AfetTuru afet;[TextArea] public string yonlendirme,duygu;}
    public Hikaye[] hikayeler={
        new Hikaye{afet=AfetTuru.Yangin,karakter="Fırıncı Hasan Usta",
            kotu="İtfaiye geç kaldı. Fırıncı Hasan Usta'nın kırk yıllık dükkânı kül oldu; çırağını dumanın içinden son anda kendisi çıkardı.",
            iyi="İtfaiye ekibi dakikalar içinde geldi. Hasan Usta'nın dükkânında sadece iki çuval un yandı; ertesi sabah fırın yine açıktı."},
        new Hikaye{afet=AfetTuru.Sel,karakter="Zemin kattaki öğrenci Mert",
            kotu="Tıkanan rögarlar sokakları göle çevirdi. Zemin katta oturan Mert'in bilgisayarı ve aylardır hazırladığı bitirme projesi çamurlu suya gömüldü.",
            dereYatagi="Dere yatağına dikilen binalar suyun yolunu kesti. Sel bu kez Mert'in sokağını değil, bütün mahalleyi yuttu.",
            iyi="Güçlendirilmiş altyapı yağmuru yuttu. Mert camdan fırtınayı izleyip kahvesini yudumladı ve sınavına çalışmaya devam etti."},
        new Hikaye{afet=AfetTuru.Deprem,karakter="Ayşe Öğretmen ve 3-A sınıfı",
            kotu="Zayıf binalar sarsıntıya dayanamadı. Ayşe Öğretmen'in 3-A sınıfı haftalarca okula dönemedi; bazı sıralar hâlâ boş.",
            yuksekFarkindalik="Sarsıntı başlar başlamaz Ayşe Öğretmen ve öğrencileri Çök-Kapan-Tutun yaptı. Güçlendirilmiş okul ayakta kaldı, kimse yaralanmadı.",
            iyi="Okul ayakta kaldı ama ne yapacağını bilmeyen öğrenciler panikle merdivenlere koştu; birkaç çocuk itişmede yaralandı."},
        new Hikaye{afet=AfetTuru.Tsunami,karakter="Balıkçı Mehmet Amca",
            kotu="Sen bütçeyi denkleştirmeye çalışırken Mehmet Amca'nın kıyıdaki tek göz odalı evi ilk dalgada yıkıldı.",
            iyi="Dev dalgalar Seviye 3 deniz setine çarpıp dağıldı. Mehmet Amca kıyıdan seslendi: 'Bizi sen kurtardın Başkan.'"}
    };
    public Kart[] kartlar={
        new Kart{baslik="Kefenin Cebi Yok",metin="Kasayı doldurdun ama o sokaklarda artık sadece hayaletler yürüyor."},
        new Kart{baslik="Beton Tabutlar",metin="Binalar ayakta kaldı ama eğitimsiz halk panikle ezildi."},
        new Kart{baslik="Çadırkent Şampiyonu",metin="Şehir harabeye döndü ama insanlar tatbikatlarda öğrendikleriyle birbirini kurtardı."},
        new Kart{baslik="Gerçek Lider",metin="Sen sadece bir başkan değil, nesiller boyu anlatılacak bir kahraman oldun."},
        new Kart{baslik="Yorgun Başkan",metin="Şehir ayakta, ama bir sonraki afete hâlâ hazır değil."}
    };
    [Header("Kart eşikleri; öncelik: Lider, Kasa, Beton, Çadır, Yorgun")]
    public float yuksekFarkindalik=60,dusukFarkindalik=40;
    public int zenginKasa=900,yuksekKayip=240,cadirEnFazlaKayip=320;
    public Ipucu[] afetIpuclari={
        new Ipucu{afet=AfetTuru.Yangin,yonlendirme="Kuru otlar tutuşmaya hazır: İtfaiye'yi güçlendir.",duygu="...çünkü ilk kıvılcım en yakın çatıya sıçrar."},
        new Ipucu{afet=AfetTuru.Sel,yonlendirme="Yaklaşan yağmurlar için Altyapı'yı güçlendir.",duygu="...çünkü önce zemin kattaki beşikler suya gömülür."},
        new Ipucu{afet=AfetTuru.Deprem,yonlendirme="Konut, Okul, AKUT ve İtfaiye en az Seviye 2 olmalı.",duygu="...çünkü deprem haber vermez; bina ya hazırdır ya değildir."},
        new Ipucu{afet=AfetTuru.Tsunami,yonlendirme="Kıyıyı korumak için Deniz Seti'ni Seviye 3'e çıkar.",duygu="...çünkü deniz çekildiğinde kaçmak için çok geç olur."}
    };
    public string ilkIpucu="Bir binaya tıkla; yapının görevini ve bir sonraki seviyeyi aynı kartta görebilirsin.";
    public string okulIpucu="Okulda kısa bir quiz çöz; doğru bilgi, afet anında hayat kurtarır.";
    public string okulDuygu="...çünkü Ayşe Öğretmen'in sınıfı deprem çantası nedir bilmiyor.";
    public string onarimIpucu="Hasarlı yapıya tıkla ve onar; yollar açılınca şehir yeniden hareket eder.";
    [Header("7. gün kararı")]
    public int kararGunu=7,kararKazanci=150;
    public float kararFarkindalikOdulu=5,selKayipCarpani=1.5f;
    public string kararBasligi="DERE YATAĞI";
    [TextArea] public string kararMetni="Müteahhit Rıza: “Dere yatağını imara aç, kasaya anında 150 coin gireyim.”";
    public string kabulMetni="İmara aç · +150 coin",retMetni="Dereyi koru · +5 farkındalık";
    public string retGeriBildirimi="Halk, kararını duydu. Güvenleri tazelendi.";
    public string kabulGeriBildirimi="Kasaya 150 coin girdi. Selin geçeceği alan daraldı.";
    public string selEkSatiri="Dere yatağındaki kaçak binalar felaketi katladı.";

    public static AnlatiVerisi Aktif=>Resources.Load<AnlatiVerisi>("AnlatiVerisi");
    public string HikayeMetni(AfetTuru afet,bool yeterli,float farkindalik,bool imar)
    {
        foreach(var h in hikayeler)if(h.afet==afet)
        {
            string metin=!yeterli ? (imar&&!string.IsNullOrEmpty(h.dereYatagi)?h.dereYatagi:h.kotu)
                : (farkindalik>=60&&!string.IsNullOrEmpty(h.yuksekFarkindalik)?h.yuksekFarkindalik:h.iyi);
            return "<b>"+h.karakter+"</b>\n"+metin;
        }
        return "";
    }
    public int KartIndex(int savunulan,float farkindalik,int kasa,int kayip)
    {
        if(savunulan==4&&farkindalik>=yuksekFarkindalik)return 3;
        if(kasa>=zenginKasa&&kayip>=yuksekKayip)return 0;
        if(savunulan>=3&&farkindalik<dusukFarkindalik)return 1;
        if(savunulan<=2&&farkindalik>=yuksekFarkindalik&&kayip<=cadirEnFazlaKayip)return 2;
        return 4;
    }
    public string AfetIpucu(AfetTuru afet)
    {foreach(var i in afetIpuclari)if(i.afet==afet)return i.yonlendirme+"\n<i>"+i.duygu+"</i>";return "";}
}
