using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attaches the shipped, static Turkish glyph atlas to every TMP font used by runtime UI.
/// The static atlas avoids relying on dynamic glyph generation that can be cleared in builds.
/// </summary>
public static class TurkishTMPFontSupport
{
    private const string FontResourcePath = "Fonts/LowPolyTurkish SDF";
    private static TMP_FontAsset turkishFont;
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string TurkceBuyukHarf(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.ToUpper(TurkishCulture);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        turkishFont = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallBeforeFirstScene()
    {
        turkishFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (turkishFont == null)
        {
            Debug.LogError("Türkçe TMP font atlası bulunamadı. Araclar > Font > Türkçe Fontu Kur komutunu çalıştırın.");
            return;
        }

        AttachToLoadedFonts();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static TMP_FontAsset PreferredFont
    {
        get
        {
            if (turkishFont == null)
                turkishFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
            return turkishFont;
        }
    }

    /// <summary>Keep the requested typeface and add the Turkish atlas as its fallback.</summary>
    public static TMP_FontAsset EnsureFont(TMP_FontAsset font)
    {
        TMP_FontAsset fallback = PreferredFont;
        if (font == null) return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
        if (fallback == null || font == fallback) return font;

        if (font.fallbackFontAssetTable == null)
            font.fallbackFontAssetTable = new List<TMP_FontAsset>();
        font.fallbackFontAssetTable.RemoveAll(candidate => candidate == null);
        font.fallbackFontAssetTable.Remove(fallback);
        font.fallbackFontAssetTable.Insert(0, fallback);
        return font;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToLoadedFonts();
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text == null || !text.gameObject.scene.IsValid()) continue;
            if (text.font == null) text.font = PreferredFont;
            else EnsureFont(text.font);
        }
    }

    private static void AttachToLoadedFonts()
    {
        TMP_FontAsset fallback = PreferredFont;
        if (fallback == null) return;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font == null || font == fallback) continue;
            if (font.fallbackFontAssetTable == null)
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            font.fallbackFontAssetTable.Remove(fallback);
            font.fallbackFontAssetTable.Insert(0, fallback);
        }

        if (TMP_Settings.defaultFontAsset != null)
            EnsureFont(TMP_Settings.defaultFontAsset);
    }
}
