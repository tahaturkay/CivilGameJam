using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the shipped rounded body/block display theme and preserves the static
/// Turkish fallback. All three atlases are build assets, never operating-system fonts.
/// </summary>
public static class TurkishTMPFontSupport
{
    private const string FontResourcePath = "Fonts/LowPolyTurkish SDF";
    private const string BodyResourcePath = "Fonts/LowPolyTheme Body SDF";
    private const string DisplayResourcePath = "Fonts/LowPolyTheme Display SDF";
    private static TMP_FontAsset turkishFont;
    private static TMP_FontAsset bodyFont;
    private static TMP_FontAsset displayFont;
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string TurkceBuyukHarf(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.ToUpper(TurkishCulture);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        turkishFont = null;
        bodyFont = null;
        displayFont = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallBeforeFirstScene()
    {
        turkishFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyResourcePath);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayResourcePath);
        if (turkishFont == null)
        {
            Debug.LogError("Türkçe TMP font atlası bulunamadı. Araclar > Font > Türkçe Fontu Kur komutunu çalıştırın.");
            return;
        }

        if(bodyFont==null||displayFont==null)
            Debug.LogWarning("Tema fontları bulunamadı; mevcut Türkçe atlas kullanılıyor. Araclar > Font > Low Poly Tema Fontlarını Kur.");
        AttachToLoadedFonts();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static TMP_FontAsset PreferredFont
    {
        get
        {
            if (bodyFont == null) bodyFont = Resources.Load<TMP_FontAsset>(BodyResourcePath);
            return bodyFont != null ? bodyFont : TurkishFallback;
        }
    }

    /// <summary>Bold, compact Bungee typography for ribbons, titles and primary buttons.</summary>
    public static TMP_FontAsset DisplayFont
    {
        get
        {
            if(displayFont==null) displayFont=Resources.Load<TMP_FontAsset>(DisplayResourcePath);
            return displayFont!=null ? displayFont : PreferredFont;
        }
    }

    private static TMP_FontAsset TurkishFallback
    {
        get
        {
            if(turkishFont==null) turkishFont=Resources.Load<TMP_FontAsset>(FontResourcePath);
            return turkishFont;
        }
    }

    /// <summary>Replace plain legacy UI fonts with the themed body; keep an explicit display request.</summary>
    public static TMP_FontAsset EnsureFont(TMP_FontAsset font)
    {
        TMP_FontAsset preferred=PreferredFont;
        TMP_FontAsset selected=font!=null && font==DisplayFont ? font : preferred;
        if(selected==null) selected=font!=null ? font : TMP_Settings.defaultFontAsset;
        AttachFallback(selected);
        return selected;
    }

    private static void AttachFallback(TMP_FontAsset font)
    {
        TMP_FontAsset fallback=TurkishFallback;
        if(font==null||fallback==null||font==fallback) return;
        if (font.fallbackFontAssetTable == null)
            font.fallbackFontAssetTable = new List<TMP_FontAsset>();
        font.fallbackFontAssetTable.RemoveAll(candidate => candidate == null);
        font.fallbackFontAssetTable.Remove(fallback);
        font.fallbackFontAssetTable.Insert(0, fallback);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToLoadedFonts();
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text == null || !text.gameObject.scene.IsValid()) continue;
            text.font=EnsureFont(text.font);
        }
    }

    private static void AttachToLoadedFonts()
    {
        TMP_FontAsset fallback = TurkishFallback;
        if (fallback == null) return;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            AttachFallback(font);
        }
        AttachFallback(PreferredFont);
        AttachFallback(DisplayFont);
    }
}
