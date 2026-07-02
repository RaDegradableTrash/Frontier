using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeskContourTerrainGenerator))]
public class DeskContourTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);
        DeskContourTerrainGenerator generator = (DeskContourTerrainGenerator)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("生成等高线地形图", GUILayout.Height(28f)))
        {
            generator.Generate();
        }

        if (GUILayout.Button("随机种子重生成", GUILayout.Height(28f)))
        {
            generator.RandomizeSeedAndRegenerate();
        }

        if (GUILayout.Button("当前参数重生", GUILayout.Height(28f)))
        {
            generator.RegenerateCurrentSeed();
        }

        if (GUILayout.Button("回放最近种子", GUILayout.Height(28f)))
        {
            generator.ReplayLastSeed();
        }

        if (GUILayout.Button("导出当前参数", GUILayout.Height(28f)))
        {
            generator.CopyCurrentSettings();
        }
        if (GUILayout.Button("保存当前参数", GUILayout.Height(28f)))
        {
            generator.SaveCurrentTabletopParameters();
        }
        if (GUILayout.Button("加载保存参数", GUILayout.Height(28f)))
        {
            generator.ApplySavedTabletopParameters();
        }
        if (GUILayout.Button("复制并应用到牌桌", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Copy Contour Parameters To Tabletop");
                targetGenerator.LoadPresetPayload(generator.GetCurrentPresetPayloadForSerialization());
                targetGenerator.Generate();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("应用为牌桌科幻背景", GUILayout.Height(28f)))
        {
            generator.ApplyTabletopSciFiPreset(true, true);
        }

        if (GUILayout.Button("仅应用牌桌预设", GUILayout.Height(28f)))
        {
            generator.ApplyTabletopSciFiPreset(false, true);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置为参考图复刻", GUILayout.Height(28f)))
        {
            generator.ResetToReferenceMatchBackdropPreset();
        }
        if (GUILayout.Button("一键应用到牌桌（参考图复刻）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版固定）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Stable");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageStable();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版基线）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Baseline");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageBaseline();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版目标贴近）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Target Match");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版精确）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Exact");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版更亮）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Bright");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageBright();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（参考图牌桌版更暗）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Dark");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageDark();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }

        if (GUILayout.Button("一键应用到牌桌（目标贴近随机重置）", GUILayout.Height(28f)))
        {
            GameObject tabletop = GameObject.Find("DesktopQuad");
            if (tabletop != null)
            {
                DeskContourTerrainGenerator targetGenerator = tabletop.GetComponent<DeskContourTerrainGenerator>();
                if (targetGenerator == null)
                {
                    targetGenerator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
                }

                Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop To Tabletop Target Match Random");
                targetGenerator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize();
                EditorUtility.SetDirty(targetGenerator);
                Selection.activeGameObject = tabletop;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "桌布未找到",
                    "当前场景中未找到名为 DesktopQuad 的对象。",
                    "OK");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("预设：冷霧蓝(推荐)", GUILayout.Height(28f)))
        {
            generator.ApplyCyanNeonPreset();
        }

        if (GUILayout.Button("预设：冷蓝薄雾", GUILayout.Height(28f)))
        {
            generator.ApplyCyanMysticPreset();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("预设：参考图风格", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchPreset();
        }

        if (GUILayout.Button("预设：参考图（更亮）", GUILayout.Height(28f)))
        {
            generator.ApplyBrightReferenceMatchPreset();
        }

        if (GUILayout.Button("预设：参考图（更暗）", GUILayout.Height(28f)))
        {
            generator.ApplyDimReferenceMatchPreset();
        }

        if (GUILayout.Button("预设：参考图牌桌版（平衡）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetBalanced();
        }

        if (GUILayout.Button("预设：参考图牌桌版（原图复刻）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
        }

        if (GUILayout.Button("预设：参考图牌桌版（基线复刻）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageBaseline();
        }

        if (GUILayout.Button("预设：参考图牌桌版（目标贴近）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }

        if (GUILayout.Button("预设：参考图牌桌版（更亮）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageBright();
        }

        if (GUILayout.Button("预设：参考图牌桌版（更暗）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageDark();
        }

        if (GUILayout.Button("预设：参考图牌桌版（锐利）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetSharp();
        }

        if (GUILayout.Button("预设：参考图牌桌版（柔和）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetSoft();
        }

        if (GUILayout.Button("预设：参考图牌桌版（发光）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetGlowy();
        }

        if (GUILayout.Button("预设：参考图牌桌版", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPreset();
        }

        if (GUILayout.Button("参考图牌桌版（随机重置）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetRandomized();
        }

        if (GUILayout.Button("参考图牌桌版（基线随机重置）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageBaselineAndRandomize();
        }

        if (GUILayout.Button("参考图牌桌版（目标贴近随机重置）", GUILayout.Height(28f)))
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize();
        }

        if (GUILayout.Button("预设：高对比", GUILayout.Height(28f)))
        {
            generator.ApplyHighContrastPreset();
        }

        if (GUILayout.Button("预设：参考图(随机)", GUILayout.Height(28f)))
        {
            generator.ApplyTabletopStyleAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatch);
        }

        if (GUILayout.Button("预设：参考图(更亮随机)", GUILayout.Height(28f)))
        {
            generator.ApplyTabletopStyleAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchBright);
        }

        if (GUILayout.Button("预设：参考图(更暗随机)", GUILayout.Height(28f)))
        {
            generator.ApplyTabletopStyleAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchDim);
        }

        if (GUILayout.Button("参考图牌桌版（轻微微调）", GUILayout.Height(28f)))
        {
            generator.TweakReferenceMatchBackdropMild();
        }

        if (GUILayout.Button("参考图牌桌版（极轻微微调）", GUILayout.Height(28f)))
        {
            generator.TweakReferenceMatchBackdropVeryMild();
        }

        if (GUILayout.Button("参考图牌桌版（中等微调）", GUILayout.Height(28f)))
        {
            generator.TweakReferenceMatchBackdrop();
        }

        if (GUILayout.Button("参考图牌桌版（强微调）", GUILayout.Height(28f)))
        {
            generator.TweakReferenceMatchBackdropStrong();
        }

        if (GUILayout.Button("随机种子重生成(保留预设)", GUILayout.Height(28f)))
        {
            generator.RandomizeTabletopSeed();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("清空纹理"))
        {
            generator.ClearGenerated();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }
}
