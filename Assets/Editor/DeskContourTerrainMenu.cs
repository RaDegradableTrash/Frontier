using UnityEditor;
using UnityEngine;
using System;

public static class DeskContourTerrainMenu
{
    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/创建等高线地形")]
    public static void CreateContourTerrainObject()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Quad);
        root.name = "Desk Contour Terrain";
        Undo.RegisterCreatedObjectUndo(root, "Create Contour Terrain");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        root.transform.localScale = new Vector3(8f, 8f, 1f);

        Collider collider = root.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        DeskContourTerrainGenerator generator = root.AddComponent<DeskContourTerrainGenerator>();
        generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize();

        Selection.activeGameObject = root;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用到牌桌背景(参考图复刻)")]
    public static void ApplyToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。请先使用“创建等高线地形”。",
                "OK");
            return;
        }

        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Exact) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageExact());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用到牌桌背景(固定复刻)")]
    public static void ApplyReferenceBackdropPresetReferenceImageStableToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Stable) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageStable());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用到牌桌背景(参考图精确复刻)")]
    public static void ApplyReferenceBackdropPresetReferenceImageExactToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Exact) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageExact());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/参考图牌桌版到桌布（更亮）")]
    public static void ApplyReferenceBackdropPresetReferenceImageBrightToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Bright) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageBright());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/参考图牌桌版到桌布（更暗）")]
    public static void ApplyReferenceBackdropPresetReferenceImageDarkToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Dark) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageDark());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用到牌桌背景 (参考图牌桌版随机)")]
    public static void ApplyReferenceBackdropPresetAndRandomizeToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop Contour Preset (Randomized) On Tabletop");
        generator.ApplyReferenceMatchBackdropPresetAndRandomize();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/重置牌桌背景到参考图复刻")]
    public static void ResetToReferenceBackdropOnTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Reset Contour Tabletop Preset");
        generator.ResetToReferenceMatchBackdropPreset();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用保存参数到牌桌背景")]
    public static void ApplySavedPresetToTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Saved Contour Parameters On Tabletop");
        if (!generator.HasSavedTabletopParameters())
        {
            EditorUtility.DisplayDialog(
                "未找到保存参数",
                "当前尚未保存过牌桌参数。请先在牌桌上执行“保存当前牌桌参数”。",
                "OK");
            return;
        }

        generator.ApplySavedTabletopParameters();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/保存当前牌桌参数")]
    public static void SaveTabletopParameters()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Save Contour Parameters From Tabletop");
        generator.SaveCurrentTabletopParameters();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/随机重生牌桌背景")]
    public static void RandomizeTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Randomize Contour Terrain On Tabletop");
        generator.ApplyReferenceMatchBackdropPresetAndRandomize();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/回放牌桌背景最近种子")]
    public static void ReplayTabletopSeed()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
            generator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Replay Contour Terrain Seed On Tabletop");
        generator.ReplayLastSeed();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/冷霧蓝")]
    public static void ApplyMysticPreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.CyanMystic, "Apply Mystic Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/冷蓝霓虹")]
    public static void ApplyNeonPreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.CyanNeon, "Apply Neon Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/高对比")]
    public static void ApplyHighContrastPreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.HighContrast, "Apply High Contrast Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图")]
    public static void ApplyReferencePreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.ReferenceMatch, "Apply Reference Match Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图更亮")]
    public static void ApplyReferenceBrightPreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchBright, "Apply Bright Reference Match Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图更暗")]
    public static void ApplyReferenceDimPreset()
    {
        ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchDim, "Apply Dim Reference Match Contour Preset On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）")]
    public static void ApplyReferenceBackdropPreset()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Exact) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageExact());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/平衡风格")]
    public static void ApplyReferenceBackdropPresetBalanced()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Balanced) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetBalanced());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/原图复刻")]
    public static void ApplyReferenceBackdropPresetReferenceImage()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Exact) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageExact());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/基线复刻")]
    public static void ApplyReferenceBackdropPresetReferenceImageBaseline()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Baseline) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageBaseline());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/基线随机重置")]
    public static void ApplyReferenceBackdropPresetReferenceImageBaselineAndRandomize()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Baseline + Random) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageBaselineAndRandomize());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/目标贴近")]
    public static void ApplyReferenceBackdropPresetReferenceImageMatchTarget()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Match Target) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/目标贴近随机重置")]
    public static void ApplyReferenceBackdropPresetReferenceImageMatchTargetAndRandomize()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Reference Image Match Target + Random) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/锐利风格")]
    public static void ApplyReferenceBackdropPresetSharp()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Sharp) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetSharp());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/柔和风格")]
    public static void ApplyReferenceBackdropPresetSoft()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Soft) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetSoft());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）/发光风格")]
    public static void ApplyReferenceBackdropPresetGlowy()
    {
        ApplyReferenceBackdropPresetProfileToTabletop(
            "Apply Reference Match Backdrop Contour Preset (Glowy) On Tabletop",
            generator => generator.ApplyReferenceMatchBackdropPresetGlowy());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图(随机)")]
    public static void ApplyReferencePresetAndRandomize()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match And Randomize Contour On Tabletop");
        generator.ApplyTabletopStyleAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatch);
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图更亮(随机)")]
    public static void ApplyReferenceBrightPresetAndRandomize()
    {
        ApplyStyleToTabletopAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchBright, "Apply Bright Reference Match And Randomize Contour On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图更暗(随机)")]
    public static void ApplyReferenceDimPresetAndRandomize()
    {
        ApplyStyleToTabletopAndRandomize(DeskContourTerrainGenerator.ContourStyle.ReferenceMatchDim, "Apply Dim Reference Match And Randomize Contour On Tabletop");
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图（牌桌版）(随机)")]
    public static void ApplyReferenceBackdropPresetAndRandomize()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, "Apply Reference Match Backdrop And Randomize Contour On Tabletop");
        generator.ApplyReferenceMatchBackdropPresetAndRandomize();
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图牌桌版（轻微微调）")]
    public static void ApplyReferenceBackdropPresetTweak()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
            generator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
        }

        ApplyReferenceBackdropPresetTweakIntensity(
            "Tweak Reference Match Backdrop Contour On Tabletop",
            generator => generator.TweakReferenceMatchBackdropMild());
        Selection.activeGameObject = tabletop;
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图牌桌版（中等微调）")]
    public static void ApplyReferenceBackdropPresetTweakModerate()
    {
        ApplyReferenceBackdropPresetTweakIntensity(
            "Tweak Reference Match Backdrop Contour (Moderate) On Tabletop",
            generator => generator.TweakReferenceMatchBackdrop());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图牌桌版（强微调）")]
    public static void ApplyReferenceBackdropPresetTweakStrong()
    {
        ApplyReferenceBackdropPresetTweakIntensity(
            "Tweak Reference Match Backdrop Contour (Strong) On Tabletop",
            generator => generator.TweakReferenceMatchBackdropStrong());
    }

    [MenuItem("Frontier/Generate/Sci-Fi Contour Terrain/应用预设/参考图牌桌版（极轻微微调）")]
    public static void ApplyReferenceBackdropPresetTweakMild()
    {
        ApplyReferenceBackdropPresetTweakIntensity(
            "Tweak Very Mild Reference Match Backdrop Contour On Tabletop",
            generator => generator.TweakReferenceMatchBackdropVeryMild());
    }

    private static void ApplyReferenceBackdropPresetTweakIntensity(string undoLabel, Action<DeskContourTerrainGenerator> applyTweak)
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
            generator.ApplyReferenceMatchBackdropPresetReferenceImageExact();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, undoLabel);
        applyTweak(generator);

        Selection.activeGameObject = tabletop;
    }

    private static void ApplyStyleToTabletop(DeskContourTerrainGenerator.ContourStyle style, string undoLabel)
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, undoLabel);
        generator.ApplyTabletopSciFiPresetStyle(style, true);
        generator.Generate();
        Selection.activeGameObject = tabletop;
    }

    private static void ApplyStyleToTabletopAndRandomize(DeskContourTerrainGenerator.ContourStyle style, string undoLabel)
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, undoLabel);
        generator.ApplyTabletopStyleAndRandomize(style);
        Selection.activeGameObject = tabletop;
    }

    private static void ApplyReferenceBackdropPresetProfileToTabletop(
        string undoLabel,
        Action<DeskContourTerrainGenerator> applyPreset)
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            EditorUtility.DisplayDialog(
                "桌布未找到",
                "当前场景中未找到名为 DesktopQuad 的对象。",
                "OK");
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        Undo.RegisterFullObjectHierarchyUndo(tabletop, undoLabel);
        applyPreset(generator);
        Selection.activeGameObject = tabletop;
    }
}
