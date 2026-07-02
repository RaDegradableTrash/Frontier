#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CardRuleAssetAssigner
{
    private const string RuleFolder = "Assets/Cards/Rules";
    private const string AutoAssignKey = "Frontier.CardRuleAssetAssigner.AutoAssigned";

    static CardRuleAssetAssigner()
    {
        EditorApplication.delayCall += AutoAssignOnce;
    }

    private static void AutoAssignOnce()
    {
        if (SessionState.GetBool(AutoAssignKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoAssignKey, true);
        RebuildCardRuleAssets();
    }

    [MenuItem("Frontier/Rebuild Card Rule Assets")]
    public static void RebuildCardRuleAssets()
    {
        Directory.CreateDirectory(RuleFolder);
        var rules = new Dictionary<string, CardRule>
        {
            { "空降", EnsureRule<AirborneRule>("AirborneRule") },
            { "连接丢失", EnsureRule<SignalLostRule>("SignalLostRule") },
            { "帝江号，清空区域", EnsureRule<OmvDijiangRule>("OMVDijiangRule") },
            { "佩丽卡", EnsureRule<PerlicaRule>("PerlicaRule") },
            { "M3", EnsureRule<M3Rule>("M3Rule") },
            { "洁尔佩塔", EnsureRule<GilbertaRule>("GilbertaRule") },
            { "诱饵", EnsureRule<TrapRule>("TrapRule") },
            { "FIELD INTEL", EnsureRule<FieldIntelRule>("FieldIntelRule") }
        };

        string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Cards" });
        foreach (string guid in cardGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card == null || string.IsNullOrWhiteSpace(card.cardName))
            {
                continue;
            }

            if (rules.TryGetValue(card.cardName, out CardRule rule))
            {
                card.specialRules = new[] { rule };
                EditorUtility.SetDirty(card);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static T EnsureRule<T>(string assetName) where T : CardRule
    {
        string path = $"{RuleFolder}/{assetName}.asset";
        T rule = AssetDatabase.LoadAssetAtPath<T>(path);
        if (rule != null)
        {
            return rule;
        }

        rule = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(rule, path);
        return rule;
    }
}
#endif
