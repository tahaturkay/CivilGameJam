using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>Produces self-contained static atlases for the game's block/rounded typography.</summary>
[InitializeOnLoad]
public static class LowPolyThemeFontInstaller
{
    const string Request="Temp/theme_font_istegi";
    const string Output="Assets/Resources/Fonts/";
    const string Sources="Assets/Fonts/LowPolyTheme/";
    const string Turkish="İıŞşĞğÜüÖöÇç";

    static LowPolyThemeFontInstaller() { EditorApplication.update+=PollRequest; }

    [PostProcessBuild(40)]
    static void IncludeFontLicenses(BuildTarget target,string builtPlayerPath)
    {
        string directory=target==BuildTarget.StandaloneOSX
            ? Path.Combine(builtPlayerPath,"Contents","Resources","ThirdPartyLicenses")
            : Path.Combine(Path.GetDirectoryName(builtPlayerPath) ?? ".","ThirdPartyLicenses");
        Directory.CreateDirectory(directory);
        foreach(string name in new[]{"Bungee-OFL.txt","Nunito-OFL.txt"})
            File.Copy(Sources+name,Path.Combine(directory,name),true);
        File.Copy(Sources+"Sources.json",Path.Combine(directory,"ThemeFontSources.json"),true);
    }

    static void PollRequest()
    {
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { InstallOrRepair(); }
        catch(Exception exception)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ThemeFontInstall.txt","FAIL\n"+exception);
            Debug.LogException(exception);
        }
    }

    [MenuItem("Araclar/Font/Low Poly Tema Fontlarını Kur")]
    public static void InstallOrRepair()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var body=CreateOrValidate("Nunito-SemiBold.ttf","LowPolyTheme Body SDF");
        var display=CreateOrValidate("Bungee-Regular.ttf","LowPolyTheme Display SDF");
        var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Output+"LowPolyTurkish SDF.asset");
        if(fallback==null) throw new InvalidOperationException("Existing Turkish fallback atlas is missing.");
        foreach(TMP_FontAsset font in new[]{body,display})
        {
            font.fallbackFontAssetTable=new List<TMP_FontAsset>{fallback};
            // Preserve normal/italic styles without synthesising a different typeface.
            EditorUtility.SetDirty(font);
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        string report="PASS\nBody: Nunito SemiBold, "+body.characterTable.Count+" static characters.\n"+
            "Display: Bungee Regular, "+display.characterTable.Count+" static characters.\n"+
            "Both atlases contain ASCII and "+Turkish+"; existing Turkish atlas retained as fallback.\n"+
            "OFL 1.1 source licenses: Assets/Fonts/LowPolyTheme; no OS font or runtime atlas generation.\n";
        File.WriteAllText("Logs/ThemeFontInstall.txt",report);
        Debug.Log(report);
    }

    static TMP_FontAsset CreateOrValidate(string sourceName,string assetName)
    {
        string path=Output+assetName+".asset";
        TMP_FontAsset existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null)
        {
            Validate(existing);
            return existing;
        }
        Font source=AssetDatabase.LoadAssetAtPath<Font>(Sources+sourceName);
        if(source==null) throw new InvalidOperationException("Theme font source was not imported: "+sourceName);
        TMP_FontAsset asset=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA_HINTED,2048,2048,AtlasPopulationMode.Dynamic,false);
        if(asset==null) throw new InvalidOperationException("Could not create theme font: "+sourceName);
        asset.name=assetName;
        if(!asset.TryAddCharacters(RequiredCharacters(),out string missing) || !string.IsNullOrEmpty(missing))
        {
            UnityEngine.Object.DestroyImmediate(asset);
            throw new InvalidOperationException(sourceName+" lacks required glyphs: "+missing);
        }
        // Symbols absent from a source remain delegated to the shipped fallback.
        asset.TryAddCharacters(OptionalCharacters(),out string optionalMissing);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;
        asset.ReadFontAssetDefinition();
        AssetDatabase.CreateAsset(asset,path);
        if(asset.material!=null&&!AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material,asset);
        foreach(Texture2D atlas in asset.atlasTextures)
            if(atlas!=null&&!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas,asset);
        EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        Validate(asset);
        return asset;
    }

    static void Validate(TMP_FontAsset font)
    {
        if(font==null||font.atlasPopulationMode!=AtlasPopulationMode.Static||font.atlasTexture==null)
            throw new InvalidOperationException("Theme atlas is not static/build-safe.");
        foreach(char c in RequiredCharacters())
            if(!font.HasCharacter(c)) throw new InvalidOperationException(font.name+" missing U+"+((int)c).ToString("X4"));
    }

    static string RequiredCharacters()
    {
        var characters=new StringBuilder();
        for(int i=32;i<=126;i++) characters.Append((char)i);
        return characters.Append(Turkish).ToString();
    }

    static string OptionalCharacters()
    {
        var characters=new StringBuilder();
        for(int i=160;i<=255;i++) characters.Append((char)i);
        return characters.Append("•€₺…—–‘’“”·→←↑↓×÷✓★☆►◄").ToString();
    }
}
