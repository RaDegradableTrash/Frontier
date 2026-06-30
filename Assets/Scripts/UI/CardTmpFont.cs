using TMPro;
using UnityEngine;

public static class CardTmpFont
{
    private static TMP_FontAsset cachedFont;

    public static TMP_FontAsset Shared
    {
        get
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            Font osFont = CreateChineseFont();
            if (osFont == null)
            {
                return null;
            }

            cachedFont = TMP_FontAsset.CreateFontAsset(osFont);
            if (cachedFont != null)
            {
                cachedFont.name = "FrontierChineseTMP_Runtime";
                cachedFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                cachedFont.isMultiAtlasTexturesEnabled = true;
            }

            return cachedFont;
        }
    }

    private static Font CreateChineseFont()
    {
        string[] fontPaths =
        {
            "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
            "/System/Library/Fonts/PingFang.ttc",
            "/System/Library/Fonts/STHeiti Light.ttc",
            "/System/Library/Fonts/STHeiti Medium.ttc",
            "/Library/Fonts/Arial Unicode.ttf"
        };

        for (int i = 0; i < fontPaths.Length; i++)
        {
            if (System.IO.File.Exists(fontPaths[i]))
            {
                return new Font(fontPaths[i]);
            }
        }

        return Font.CreateDynamicFontFromOSFont(
            new[] { "PingFang SC", "Heiti SC", "Songti SC", "Arial Unicode MS" },
            90);
    }

    public static void Apply(TMP_Text text)
    {
        TMP_FontAsset font = Shared;
        if (text != null && font != null)
        {
            text.font = font;
            if (font.material != null)
            {
                text.fontSharedMaterial = font.material;
            }
        }
    }
}
