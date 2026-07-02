using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CardArtworkBinder
{
    private const string CardDataFolder = "Assets/Cards";
    private const string CardArtFolder = "Assets/Resources/CardArt";
    private const string CardArtBlurFolder = "Assets/Resources/CardArtBlur";
    private const string EndfieldFolder = "Assets/Endfield";
    private static readonly string[] ValidExtensions = { ".png", ".jpg", ".jpeg", ".avif" };

    [MenuItem("Frontier/Cards/Sync Endfield Art and Bind Card Artwork Textures")]
    public static void SyncEndfieldArtworkAndBind()
    {
        if (!Directory.Exists(EndfieldFolder))
        {
            Debug.LogWarning("[CardArtworkBinder] 未找到 Assets/Endfield，先放入最新美术再重跑。");
            return;
        }

        Dictionary<string, string> endfieldPaths = BuildNormalizedPathMap(EndfieldFolder);
        if (endfieldPaths.Count == 0)
        {
            Debug.LogWarning("[CardArtworkBinder] Assets/Endfield 里没发现图片。");
            return;
        }

        SyncResourceFolder(CardArtFolder, endfieldPaths);
        SyncResourceFolder(CardArtBlurFolder, endfieldPaths);
        PurgeNonEndfieldAssets(endfieldPaths);

        Dictionary<string, string> preferredSourceExtByKey = BuildNormalizedPathExtensions(endfieldPaths);
        Dictionary<string, Texture2D> cardArtTextures = BuildTextureIndex(CardArtFolder, preferredSourceExtByKey);
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { CardDataFolder });
        int updated = 0;

        foreach (string guid in cardGuids)
        {
            string cardPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
            if (cardData == null)
            {
                continue;
            }

            string matchedKey = ResolveArtworkKey(cardData, cardArtTextures);
            if (string.IsNullOrWhiteSpace(matchedKey))
            {
                Debug.LogWarning($"[CardArtworkBinder] 无法匹配卡图: {cardData.name} ({cardData.cardName})");
                continue;
            }

            if (!cardArtTextures.TryGetValue(matchedKey, out Texture2D artwork))
            {
                continue;
            }

            if (!ReferenceEquals(cardData.artwork, artwork))
            {
                cardData.artwork = artwork;
                EditorUtility.SetDirty(cardData);
                updated++;
            }
        }

        if (updated > 0)
        {
            AssetDatabase.SaveAssets();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CardArtworkBinder] 已同步绑定完成，更新 {updated} 张卡牌图。");
    }

    [MenuItem("Frontier/Cards/Hard Reset Endfield Art and Rebind Card Artwork Textures")]
    public static void HardResetEndfieldArtworkAndRebind()
    {
        if (!Directory.Exists(EndfieldFolder) || !Directory.Exists(CardDataFolder))
        {
            Debug.LogWarning("[CardArtworkBinder] 资源目录缺失，无法执行硬清理。");
            return;
        }

        Dictionary<string, string> endfieldPaths = BuildNormalizedPathMap(EndfieldFolder);
        if (endfieldPaths.Count == 0)
        {
            Debug.LogWarning("[CardArtworkBinder] Assets/Endfield 里没发现可用图片。");
            return;
        }

        PurgeFolder(CardArtFolder, endfieldPaths, forceClear: true);
        PurgeFolder(CardArtBlurFolder, endfieldPaths, forceClear: true);
        SyncResourceFolder(CardArtFolder, endfieldPaths);
        SyncResourceFolder(CardArtBlurFolder, endfieldPaths);

        Dictionary<string, string> preferredSourceExtByKey = BuildNormalizedPathExtensions(endfieldPaths);
        Dictionary<string, Texture2D> cardArtTextures = BuildTextureIndex(CardArtFolder, preferredSourceExtByKey);
        int updated = RebindCardArtTexture(cardArtTextures);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CardArtworkBinder] 已执行硬清理并绑定完成，更新 {updated} 张卡牌图。");
    }

    [MenuItem("Frontier/Cards/Hard Reset Endfield Art and Rebind Card Artwork Textures", true)]
    private static bool ValidateHardReset()
    {
        return Directory.Exists(EndfieldFolder) && Directory.Exists(CardDataFolder);
    }

    [MenuItem("Frontier/Cards/Sync Endfield Art and Bind Card Artwork Textures", true)]
    private static bool ValidateSync()
    {
        return Directory.Exists(EndfieldFolder) && Directory.Exists(CardDataFolder);
    }

    private static Dictionary<string, string> BuildNormalizedPathMap(string folder)
    {
        string[] files = Directory.GetFiles(folder)
            .Where(path => ValidExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .ToArray();
        Dictionary<string, string> map = new();
        foreach (string file in files)
        {
            string key = NormalizeArtworkKey(Path.GetFileNameWithoutExtension(file));
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!map.TryGetValue(key, out string existingPath))
            {
                map[key] = file;
                continue;
            }

            if (TexturePriority(file) > TexturePriority(existingPath))
            {
                map[key] = file;
            }
        }

        return map;
    }

    private static Dictionary<string, string> BuildNormalizedPathExtensions(Dictionary<string, string> pathsByKey)
    {
        Dictionary<string, string> result = new();
        foreach (KeyValuePair<string, string> kvp in pathsByKey)
        {
            string ext = Path.GetExtension(kvp.Value).ToLowerInvariant();
            result[kvp.Key] = ext;
        }

        return result;
    }

    private static void SyncResourceFolder(string targetFolder, Dictionary<string, string> endfieldPaths)
    {
        Directory.CreateDirectory(targetFolder);
        foreach (KeyValuePair<string, string> kvp in endfieldPaths)
        {
            string sourcePath = kvp.Value;
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(targetFolder, fileName);
            string normalizedSource = NormalizeArtworkKey(Path.GetFileNameWithoutExtension(sourcePath));
            if (!validKeyNameEquals(normalizedSource, kvp.Key))
            {
                continue;
            }

            // Keep only Endfield-derived asset for this key; remove any old duplicate variants.
            RemoveAllVariantsForKey(targetFolder, kvp.Key);
            File.Copy(sourcePath, destPath, true);
            AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void RemoveAllVariantsForKey(string folder, string normalizedKey)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        string prefixKey = NormalizeArtworkKey(normalizedKey);
        foreach (string file in Directory.GetFiles(folder))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (!ValidExtensions.Contains(ext))
            {
                continue;
            }

            string key = NormalizeArtworkKey(Path.GetFileNameWithoutExtension(file));
            if (key == prefixKey)
            {
                AssetDatabase.DeleteAsset(RelToProjectPath(file));
            }
        }
    }

    private static void PurgeNonEndfieldAssets(Dictionary<string, string> endfieldPaths)
    {
        PurgeFolder(CardArtFolder, endfieldPaths);
        PurgeFolder(CardArtBlurFolder, endfieldPaths);
    }

    private static int RebindCardArtTexture(Dictionary<string, Texture2D> cardArtTextures)
    {
        int updated = 0;
        string[] cardGuids = AssetDatabase.FindAssets($"t:CardData", new[] { CardDataFolder });
        foreach (string guid in cardGuids)
        {
            string cardPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
            if (cardData == null)
            {
                continue;
            }

            string matchedKey = ResolveArtworkKey(cardData, cardArtTextures);
            if (string.IsNullOrWhiteSpace(matchedKey))
            {
                continue;
            }

            if (!cardArtTextures.TryGetValue(matchedKey, out Texture2D artwork))
            {
                continue;
            }

            if (!ReferenceEquals(cardData.artwork, artwork))
            {
                cardData.artwork = artwork;
                EditorUtility.SetDirty(cardData);
                updated++;
            }
        }

        return updated;
    }

    private static void PurgeFolder(string folder, Dictionary<string, string> validEndfieldKeys, bool forceClear = false)
    {
        Directory.CreateDirectory(folder);
        foreach (string file in Directory.GetFiles(folder))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (!ValidExtensions.Contains(ext))
            {
                continue;
            }

            string key = NormalizeArtworkKey(Path.GetFileNameWithoutExtension(file));
            if (forceClear || !validEndfieldKeys.ContainsKey(key))
            {
                AssetDatabase.DeleteAsset(RelToProjectPath(file));
            }
        }
    }

    private static Dictionary<string, Texture2D> BuildTextureIndex(string folder, Dictionary<string, string> preferredSourceExtByKey = null)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        Dictionary<string, Texture2D> map = new();
        // Prefer latest in order: source extension from Endfield when available, otherwise fallback by default extension ranking.
        Dictionary<string, string> sourcePathByKey = new();
        Dictionary<string, int> sourcePriorityByKey = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string key = NormalizeArtworkKey(Path.GetFileNameWithoutExtension(path));
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                int priority = TexturePriority(path);
                if (preferredSourceExtByKey != null
                    && preferredSourceExtByKey.TryGetValue(key, out string preferredExt)
                    && !string.IsNullOrWhiteSpace(preferredExt)
                    && preferredExt.Equals(ext, System.StringComparison.OrdinalIgnoreCase))
                {
                    priority = Mathf.Max(priority, 99);
                }

                if (!sourcePathByKey.TryGetValue(key, out string bestPath)
                    || priority > sourcePriorityByKey[key]
                    || (priority == sourcePriorityByKey[key] && string.CompareOrdinal(path, bestPath) > 0))
                {
                    sourcePathByKey[key] = path;
                    sourcePriorityByKey[key] = priority;
                }
            }
        }

        foreach (KeyValuePair<string, string> kvp in sourcePathByKey)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kvp.Value);
            if (texture != null)
            {
                map[kvp.Key] = texture;
            }
        }

        return map;
    }

    private static int TexturePriority(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".png")
        {
            return 4;
        }
        if (ext == ".jpg")
        {
            return 3;
        }
        if (ext == ".jpeg")
        {
            return 2;
        }
        if (ext == ".avif")
        {
            return 1;
        }
        return 0;
    }

    private static string ResolveArtworkKey(CardData cardData, Dictionary<string, Texture2D> availableTextures)
    {
        foreach (string candidate in ResolveCandidates(cardData))
        {
            string normalized = NormalizeArtworkKey(candidate);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (availableTextures.ContainsKey(normalized))
            {
                return normalized;
            }

            string boardVariant = $"{normalized}_avator";
            if (availableTextures.ContainsKey(boardVariant))
            {
                return boardVariant;
            }
        }

        if (availableTextures.TryGetValue("m3", out _))
        {
            return "m3";
        }

        return null;
    }

    private static IEnumerable<string> ResolveCandidates(CardData cardData)
    {
        string name = cardData != null ? cardData.cardName : string.Empty;
        string asset = cardData != null ? cardData.name : string.Empty;
        string normalized = NormalizeArtworkKey(name);
        string[] baseCandidates = { name, asset };
        foreach (string c in baseCandidates)
        {
            if (!string.IsNullOrWhiteSpace(c))
            {
                yield return c;
            }
        }

        if (normalized.Contains("perlica") || name.Contains("佩丽卡"))
        {
            yield return "Perlica";
        }
        else if (normalized.Contains("chenqianyu") || name.Contains("陈千语"))
        {
            yield return "Chenqianyu";
        }
        else if (normalized.Contains("gilberta") || name.Contains("洁尔佩塔"))
        {
            yield return "Gilberta";
        }
        else if (normalized.Contains("signallost") || name.Contains("连接丢失"))
        {
            yield return "FieldIntel";
        }
        else if (normalized.Contains("omvdijiang") || normalized.Contains("dijiang") || name.Contains("帝江号"))
        {
            yield return "DijiangClearTheArea";
        }
        else if (normalized.Contains("fieldintel") || normalized.Contains("lifeng"))
        {
            yield return "FieldIntel";
        }
        else if (normalized.Contains("airborne") || name.Contains("空降"))
        {
            yield return "Airborne";
        }
    }

    private static string NormalizeArtworkKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c) || c > 127 || c == '_')
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static bool validKeyNameEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string RelToProjectPath(string absPath)
    {
        string projectRoot = Path.GetFullPath(Application.dataPath + "/../");
        string normalizedAbs = Path.GetFullPath(absPath);
        if (normalizedAbs.StartsWith(projectRoot, System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedAbs.Substring(projectRoot.Length).Replace('\\', '/');
        }

        if (normalizedAbs.StartsWith("Assets" + Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedAbs.Replace('\\', '/');
        }

        return normalizedAbs.Replace('\\', '/');
    }
}
