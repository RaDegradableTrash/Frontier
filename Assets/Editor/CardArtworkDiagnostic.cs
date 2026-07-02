using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CardArtworkDiagnostic
{
    private const string CardArtFolder = "Assets/Resources/CardArt";
    private const string CardArtBlurFolder = "Assets/Resources/CardArtBlur";
    private const string EndfieldFolder = "Assets/Endfield";
    private const string CardPrefabGeneratedMaterialsFolder = "Assets/Materials/CardPrefabGenerated";

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    private static readonly string[] LegacyMaterialPrefixes =
    {
        "Image_",
        "ReferenceImage_",
        "ReferenceWhiteField_",
        "Face_"
    };

    [MenuItem("Frontier/Cards/Cleanup/Cleanup Endfield Card Art")]
    public static void CleanupEndfieldCardArtwork()
    {
        if (!Directory.Exists(EndfieldFolder))
        {
            Debug.LogWarning("[CardArtworkDiagnostic] Assets/Endfield 不存在，已取消清理。");
            return;
        }

        HashSet<string> validKeys = BuildEndfieldArtworkKeys();

        int deleted = 0;
        PurgeLegacyFilesInFolder(CardArtFolder, validKeys, ref deleted);
        PurgeLegacyFilesInFolder(CardArtBlurFolder, validKeys, ref deleted);
        PurgeLegacyPrefabMaterials(validKeys, ref deleted);

        AssetDatabase.Refresh();
        Debug.Log($"[CardArtworkDiagnostic] 清理完成，已删除 {deleted} 个旧文件。");
    }

    private static void PurgeLegacyFilesInFolder(string folder, HashSet<string> validKeys, ref int deletedCount)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
        {
            if (!IsImageFile(file))
            {
                continue;
            }

            string key = NormalizeInspectArtworkKey(Path.GetFileNameWithoutExtension(file));
            if (validKeys.Contains(key))
            {
                continue;
            }

            string relativePath = ConvertToProjectPath(file);
            if (AssetDatabase.DeleteAsset(relativePath))
            {
                deletedCount++;
            }
        }
    }

    private static void PurgeLegacyPrefabMaterials(HashSet<string> validKeys, ref int deletedCount)
    {
        if (!Directory.Exists(CardPrefabGeneratedMaterialsFolder))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(CardPrefabGeneratedMaterialsFolder, "*.mat", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string lowerName = fileName.ToLowerInvariant();

            bool isLegacyMaterial = false;
            foreach (string prefix in LegacyMaterialPrefixes)
            {
                if (lowerName.StartsWith(prefix.ToLowerInvariant()))
                {
                    isLegacyMaterial = true;
                    break;
                }
            }

            if (!isLegacyMaterial)
            {
                continue;
            }

            string key = NormalizeInspectArtworkKey(fileName);
            if (validKeys.Contains(key))
            {
                continue;
            }

            string relativePath = ConvertToProjectPath(file);
            if (AssetDatabase.DeleteAsset(relativePath))
            {
                deletedCount++;
            }
        }
    }

    private static HashSet<string> BuildEndfieldArtworkKeys()
    {
        var keys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.GetFiles(EndfieldFolder, "*", SearchOption.AllDirectories))
        {
            if (!IsImageFile(file))
            {
                continue;
            }

            string key = NormalizeInspectArtworkKey(Path.GetFileNameWithoutExtension(file));
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        keys.Add(NormalizeInspectArtworkKey("M3"));
        keys.Add(NormalizeInspectArtworkKey("M3_Avator"));
        return keys;
    }

    private static bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path);
        foreach (string valid in ImageExtensions)
        {
            if (ext.Equals(valid, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ConvertToProjectPath(string absoluteOrProjectPath)
    {
        string normalized = absoluteOrProjectPath.Replace('\\', '/');
        if (normalized.StartsWith("Assets/", System.StringComparison.Ordinal))
        {
            return normalized;
        }

        string projectRoot = Path.GetFullPath(Application.dataPath + "/../").Replace('\\', '/') + "/";
        string fullPath = Path.GetFullPath(absoluteOrProjectPath).Replace('\\', '/');

        if (fullPath.StartsWith(projectRoot, System.StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(projectRoot.Length);
        }

        return absoluteOrProjectPath;
    }

    private static string NormalizeInspectArtworkKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c) || c > 127 || c == '_')
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
