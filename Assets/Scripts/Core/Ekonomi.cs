using UnityEngine;

// Para ile ilgili bütün kurallar burada: gelir, yükseltme, onarım.
// Sahnede bir objeye ihtiyaç duymaz; her yerden Ekonomi.Yukselt(bina) diye çağrılır.
public static class Ekonomi
{
    // Hasarlı konut yarı gelir getirir (sıfır olsaydı para bitince oyun kilitlenebilirdi)
    const float HasarliKonutGelirCarpani = 0.5f;

    public static int GunlukGelir()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 0;

        float gelir = gm.nufus * DengeAyarlari.Aktif.kisiBasiGunlukGelir;
        float[] carpanlar=DengeAyarlari.Aktif.konutGelirCarpanlari;
        if(carpanlar!=null&&carpanlar.Length>0)
            gelir*=carpanlar[Mathf.Clamp(gm.temelBinaLevel-1,0,carpanlar.Length-1)];
        if (gm.temelBinaHasarli) gelir *= HasarliKonutGelirCarpani;
        return Mathf.RoundToInt(gelir);
    }

    public static int NufusKapasitesi()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 0;
        return DengeAyarlari.Aktif.KonutKapasitesi(gm.temelBinaLevel);
    }

    // Maks seviyedeyse -1
    public static int YukseltmeMaliyeti(BinaTuru bina)
    {
        var gm = GameManager.Instance;
        if (gm == null) return -1;
        return DengeAyarlari.Aktif.YukseltmeMaliyeti(bina, gm.Level(bina));
    }

    public static int OnarimMaliyeti(BinaTuru bina)
    {
        return DengeAyarlari.Aktif.OnarimMaliyeti(bina);
    }

    static bool IslemYapilabilir(GameManager gm)
    {
        // Popup açıkken oyunDurduMu true olduğu için ona bakılmaz; sadece afet ve oyun sonu engeller
        return gm != null && !gm.afetSuruyor && !gm.afetOlduMu && !gm.oyunBittiMi;
    }

    public static bool YukseltilebilirMi(BinaTuru bina)
    {
        var gm = GameManager.Instance;
        if (!IslemYapilabilir(gm) || gm.Hasarli(bina)) return false;
        int maliyet = YukseltmeMaliyeti(bina);
        return maliyet >= 0 && gm.para >= maliyet;
    }

    public static bool Yukselt(BinaTuru bina)
    {
        if (!YukseltilebilirMi(bina)) return false;

        var gm = GameManager.Instance;
        int maliyet = YukseltmeMaliyeti(bina);
        gm.para -= maliyet;
        gm.toplamHarcama += maliyet;
        gm.yukseltmeSayisi++;
        gm.LevelAyarla(bina, gm.Level(bina) + 1);
        gm.YapiIsleminiBildir(bina);
        return true;
    }

    public static bool OnarilabilirMi(BinaTuru bina)
    {
        var gm = GameManager.Instance;
        return IslemYapilabilir(gm) && gm.Hasarli(bina) && gm.para >= OnarimMaliyeti(bina);
    }

    public static bool Onar(BinaTuru bina)
    {
        if (!OnarilabilirMi(bina)) return false;

        var gm = GameManager.Instance;
        int maliyet = OnarimMaliyeti(bina);
        gm.para -= maliyet;
        gm.toplamHarcama += maliyet;
        gm.HasarliAyarla(bina, false);
        gm.YapiIsleminiBildir(bina);
        return true;
    }
}
