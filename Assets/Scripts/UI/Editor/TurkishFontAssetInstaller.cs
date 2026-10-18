using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Builds a static, build-safe Turkish TMP atlas from the project's Liberation Sans source font.
/// </summary>
[InitializeOnLoad]
public static class TurkishFontAssetInstaller
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string OutputDirectory = "Assets/Resources/Fonts";
    private const string OutputAssetPath = OutputDirectory + "/LowPolyTurkish SDF.asset";
    private const string MainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string RequiredTurkish = "İıŞşĞğÜüÖöÇç";
    private const string ExtraPunctuation = "•€₺…—–‘’“”·→←↑↓×÷";

    static TurkishFontAssetInstaller()
    {
        EditorApplication.delayCall += InstallIfMissing;
    }

    [MenuItem("Araclar/Font/Türkçe Fontu Kur")]
    public static void InstallOrRepair()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath);
        if (fontAsset == null)
            fontAsset = CreateStaticFontAsset();

        if (fontAsset == null) return;
        LinkAsFallback(fontAsset);
    }

    private static void InstallIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InstallIfMissing;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath) == null)
            InstallOrRepair();
    }

    private static TMP_FontAsset CreateStaticFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError("Türkçe font kurulamadı: LiberationSans.ttf bulunamadı: " + SourceFontPath);
            return null;
        }

        if (!AssetDatabase.IsValidFolder(OutputDirectory))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA_HINTED,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError("TextMesh Pro, Türkçe font atlasını oluşturamadı.");
            return null;
        }

        fontAsset.name = "LowPolyTurkish SDF";
        if (!fontAsset.TryAddCharacters(BuildRequiredCharacterSet(), out string missingCharacters))
        {
            Debug.LogError("Font atlasına gerekli Türkçe karakterler eklenemedi: " + missingCharacters);
            Object.DestroyImmediate(fontAsset);
            return null;
        }

        if (!string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogError("Liberation Sans bu karakterleri sağlayamadı: " + missingCharacters);
            Object.DestroyImmediate(fontAsset);
            return null;
        }

        fontAsset.TryAddCharacters(BuildOptionalCharacterSet(), out string missingOptionalCharacters);
        if (!string.IsNullOrEmpty(missingOptionalCharacters))
            Debug.LogWarning("Fontun isteğe bağlı bazı Latin/sembol karakterleri yok; Türkçe harfler tamam: " + missingOptionalCharacters);

        // Glyph atlası çalışma anında değişmez; böylece TMP'nin build sırasında dinamik
        // glyph verisini temizlemesi Türkçe harfleri etkileyemez.
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.ReadFontAssetDefinition();
        AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);

        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        Texture2D[] atlases = fontAsset.atlasTextures;
        if (atlases != null)
        {
            foreach (Texture2D atlas in atlases)
            {
                if (atlas != null && !AssetDatabase.Contains(atlas))
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OutputAssetPath, ImportAssetOptions.ForceSynchronousImport);
        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath);
        Debug.Log("Türkçe font atlası oluşturuldu: " + OutputAssetPath);
        return fontAsset;
    }

    private static string BuildRequiredCharacterSet()
    {
        StringBuilder characters = new StringBuilder(120);
        for (int codePoint = 32; codePoint <= 126; codePoint++)
            characters.Append((char)codePoint);
        characters.Append(RequiredTurkish);
        return characters.ToString();
    }

    private static string BuildOptionalCharacterSet()
    {
        StringBuilder characters = new StringBuilder(180);
        for (int codePoint = 160; codePoint <= 255; codePoint++)
            characters.Append((char)codePoint);
        for (int codePoint = 0x2000; codePoint <= 0x206F; codePoint++)
            characters.Append((char)codePoint);
        characters.Append(ExtraPunctuation);
        characters.Append('₺');
        return characters.ToString();
    }

    private static void LinkAsFallback(TMP_FontAsset fontAsset)
    {
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (mainFont == null)
        {
            Debug.LogError("Türkçe font ana TMP fontuna bağlanamadı: " + MainFontPath);
            return;
        }

        if (mainFont.fallbackFontAssetTable == null)
            mainFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        mainFont.fallbackFontAssetTable.RemoveAll(candidate => candidate == null);
        mainFont.fallbackFontAssetTable.Remove(fontAsset);
        mainFont.fallbackFontAssetTable.Insert(0, fontAsset);

        EditorUtility.SetDirty(mainFont);
        AssetDatabase.SaveAssets();
    }
}
