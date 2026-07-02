using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Procedural/Desk Contour Terrain Map")]
public class DeskContourTerrainGenerator : MonoBehaviour
{
    private const string SavedReferenceBackdropPresetKey = "DeskContourTerrainGenerator.ReferenceBackdropPreset.V1";
    private const int ReferenceBackdropStableSeed = 982741;

    public enum ContourStyle
    {
        CyanMystic,
        CyanNeon,
        HighContrast,
        ReferenceMatch,
        ReferenceMatchBright,
        ReferenceMatchDim,
        ReferenceMatchBackdrop
    }

    private const int MinResolution = 64;
    private const int MaxResolution = 2048;

    [Serializable]
    private struct BackdropParameterSnapshot
    {
        public string contourStyle;
        public float seed;
        public int textureResolution;
        public int contourLayers;
        public int octaves;
        public float lineWidth;
        public float lineSoftness;
        public float contourBias;
        public float baseScale;
        public float lacunarity;
        public float persistence;
        public float noiseWarp;
        public float flowScale;
        public float rimRadius;
        public float rimIntensity;
        public float contrast;
        public float emissiveBoost;
        public Color backgroundColorLow;
        public Color backgroundColorHigh;
        public Color contourColor;
        public Color glowColor;
    }

    private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int _BaseColorProperty = BaseColorProperty;

    [Header("Topology")]
    [SerializeField] private int seed = 12345;
    [SerializeField, Range(MinResolution, MaxResolution)] private int textureResolution = 1024;
    [SerializeField, Range(2, 64)] private int contourLayers = 10;
    [SerializeField, Range(0.01f, 0.45f)] private float lineWidth = 0.12f;
    [SerializeField, Range(0f, 1f)] private float lineSoftness = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float contourBias = 0.12f;

    [Header("Noise")]
    [SerializeField, Range(16f, 240f)] private float baseScale = 120f;
    [SerializeField, Range(1, 6)] private int octaves = 4;
    [SerializeField, Range(0.2f, 2f)] private float lacunarity = 1.95f;
    [SerializeField, Range(0.1f, 0.9f)] private float persistence = 0.48f;
    [SerializeField, Range(0f, 1f)] private float noiseWarp = 0.35f;

    [Header("Color")]
    [SerializeField] private Color backgroundColorLow = new Color(0.02f, 0.09f, 0.18f, 0.05f);
    [SerializeField] private Color backgroundColorHigh = new Color(0.06f, 0.20f, 0.36f, 0.10f);
    [SerializeField] private Color contourColor = new Color(0.46f, 0.95f, 1f, 0.78f);
    [SerializeField] private Color glowColor = new Color(0.30f, 0.80f, 1f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float contrast = 0.72f;
    [SerializeField, Range(0f, 1f)] private float emissiveBoost = 0.40f;

    [Header("Reference-Style Controls")]
    [SerializeField, Range(1f, 10f)] private float flowScale = 4f;
    [SerializeField, Range(0.1f, 2f)] private float rimRadius = 1.25f;
    [SerializeField, Range(0f, 1f)] private float rimIntensity = 0.72f;

    [Header("Behavior")]
    [SerializeField] private bool autoRegenerate = false;
    [SerializeField] private ContourStyle contourStyle = ContourStyle.ReferenceMatch;

    private float[] heightField;
    private Texture2D generatedTexture;
    private Sprite generatedSprite;
    private Material generatedMaterial;
    [SerializeField, HideInInspector] private bool tablePresetApplied;
    [SerializeField, HideInInspector] private int latestSeed = 12345;
    [SerializeField, HideInInspector] private bool latestSeedInitialized = false;
    private bool isGenerating;

    public bool IsTablePresetApplied => tablePresetApplied;
    public bool HasGeneratedTexture => generatedTexture != null;
    public int LatestSeed => latestSeed;
    public ContourStyle CurrentContourStyle => contourStyle;

    private void OnEnable()
    {
        if (!tablePresetApplied && gameObject != null && gameObject.name == "DesktopQuad")
        {
            ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }

        if (autoRegenerate)
        {
            Generate();
        }
    }

    private void OnDisable()
    {
        CleanupGeneratedAssets();
    }

    private void OnDestroy()
    {
        CleanupGeneratedAssets();
    }

    private void OnValidate()
    {
        textureResolution = Math.Min(MaxResolution, Math.Max(MinResolution, textureResolution));
        contourLayers = Math.Min(64, Math.Max(2, contourLayers));
        octaves = Math.Min(6, Math.Max(1, octaves));

        if (autoRegenerate && !isGenerating && !Application.isPlaying)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Contour Map")]
    public void Generate()
    {
        isGenerating = true;
        try
        {
            SetLatestSeed(seed);
            EnsureTargets();
            BuildHeightField();
            ApplyTexture(BuildTexture());
        }
        finally
        {
            isGenerating = false;
        }
    }

    [ContextMenu("Randomize Seed and Regenerate")]
    public void RandomizeSeedAndRegenerate()
    {
        if (!tablePresetApplied)
        {
            ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }

        ApplySeed(GetRandomSeed());
        Generate();
    }

    [ContextMenu("Regenerate With Current Seed")]
    public void RegenerateCurrentSeed()
    {
        Generate();
    }

    [ContextMenu("Replay Last Generated Seed")]
    public void ReplayLastSeed()
    {
        if (!tablePresetApplied)
        {
            ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }
        else if (!latestSeedInitialized)
        {
            ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }

        seed = latestSeed;
        Generate();
    }

    [ContextMenu("Export Current Parameters (Debug Log)")]
    public void ExportCurrentParameters()
    {
        string payload = GetCurrentPresetPayload();
        Debug.Log($"[DeskContourTerrainGenerator] {payload}");
    }

    [ContextMenu("Save Current Tabletop Parameters")]
    public void SaveCurrentTabletopParameters()
    {
        string payload = GetCurrentPresetPayload();
        PlayerPrefs.SetString(SavedReferenceBackdropPresetKey, payload);
        PlayerPrefs.Save();
        Debug.Log($"[DeskContourTerrainGenerator] 当前牌桌参数已保存（长度 {payload.Length}）");
    }

    [ContextMenu("Apply Saved Tabletop Parameters")]
    public void ApplySavedTabletopParameters()
    {
        if (!LoadPresetPayload(PlayerPrefs.GetString(SavedReferenceBackdropPresetKey, string.Empty)))
        {
            Debug.LogWarning("[DeskContourTerrainGenerator] 未检测到可用的保存参数");
            return;
        }

        Generate();
    }

    public bool HasSavedTabletopParameters()
    {
        return PlayerPrefs.HasKey(SavedReferenceBackdropPresetKey);
    }

    public void Regenerate() => Generate();

    [ContextMenu("Apply Tabletop Sci-Fi Preset and Generate")]
    public void ApplyTabletopSciFiPreset()
    {
        ApplyTabletopSciFiPreset(true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset To DesktopQuad")]
    public void ApplyReferenceBackdropPresetToDesktopQuad()
    {
        ApplyReferenceMatchBackdropPresetToDesktopQuadInternal(deskGenerator => deskGenerator.ApplyReferenceMatchBackdropPresetReferenceImageExact());
    }

    [ContextMenu("Apply Reference Match Backdrop (Match Target) to DesktopQuad")]
    public void ApplyReferenceMatchBackdropMatchTargetPresetToDesktopQuad()
    {
        ApplyReferenceMatchBackdropPresetToDesktopQuadInternal(generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget());
    }

    [ContextMenu("Apply Reference Match Backdrop (Match Target + Random Seed) to DesktopQuad")]
    public void ApplyReferenceMatchBackdropMatchTargetPresetAndRandomToDesktopQuad()
    {
        ApplyReferenceMatchBackdropPresetToDesktopQuadInternal(generator => generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize());
    }

    private void ApplyReferenceMatchBackdropPresetToDesktopQuadInternal(Action<DeskContourTerrainGenerator> applyPresetToTarget)
    {
        GameObject desk = GameObject.Find("DesktopQuad");
        if (desk == null)
        {
            Debug.LogWarning("[DeskContourTerrainGenerator] 未找到名为 DesktopQuad 的对象");
            return;
        }

        if (desk == gameObject)
        {
            applyPresetToTarget(this);
            return;
        }

        DeskContourTerrainGenerator deskGenerator = desk.GetComponent<DeskContourTerrainGenerator>();
        if (deskGenerator == null)
        {
            deskGenerator = desk.AddComponent<DeskContourTerrainGenerator>();
        }

        applyPresetToTarget(deskGenerator);
        deskGenerator.Generate();
    }

    public void ApplyTabletopSciFiPreset(bool forceRandomSeed, bool forceRegenerate = false)
    {
        if (tablePresetApplied && !forceRegenerate)
        {
            return;
        }

        tablePresetApplied = true;
        if (forceRandomSeed)
        {
            ApplySeed(GetRandomSeed());
        }

        ApplyTabletopSciFiPresetStyle(contourStyle, true);
        autoRegenerate = true;
        Generate();
    }

    public void ApplyTabletopSciFiPresetStyle(ContourStyle style, bool forceRegenerate)
    {
        if (tablePresetApplied && !forceRegenerate)
        {
            return;
        }

        tablePresetApplied = true;
        autoRegenerate = true;
        contourStyle = style;
        textureResolution = 1024;
        contourLayers = 14;
        octaves = 5;
        lacunarity = 2.02f;
        persistence = 0.47f;
        noiseWarp = 0.34f;

        switch (style)
        {
            case ContourStyle.CyanMystic:
                baseScale = 114f;
                lineWidth = 0.07f;
                lineSoftness = 0.48f;
                contourBias = 0.10f;
                backgroundColorLow = new Color(0.003f, 0.018f, 0.055f, 0.03f);
                backgroundColorHigh = new Color(0.020f, 0.080f, 0.185f, 0.09f);
                contourColor = new Color(0.36f, 0.98f, 1f, 0.88f);
                glowColor = new Color(0.18f, 0.72f, 1f, 0.42f);
                contrast = 0.84f;
                emissiveBoost = 0.52f;
                break;

            case ContourStyle.HighContrast:
                baseScale = 98f;
                lineWidth = 0.05f;
                lineSoftness = 0.36f;
                contourBias = 0.17f;
                backgroundColorLow = new Color(0.008f, 0.038f, 0.076f, 0.01f);
                backgroundColorHigh = new Color(0.050f, 0.110f, 0.220f, 0.12f);
                contourColor = new Color(0.65f, 1f, 1f, 0.96f);
                glowColor = new Color(0.38f, 0.95f, 1f, 0.52f);
                contrast = 0.90f;
                emissiveBoost = 0.65f;
                break;

            case ContourStyle.ReferenceMatch:
                baseScale = 106f;
                contourLayers = 16;
                lineWidth = 0.06f;
                lineSoftness = 0.62f;
                contourBias = 0.12f;
                flowScale = 4.4f;
                rimRadius = 1.28f;
                rimIntensity = 0.74f;
                backgroundColorLow = new Color(0.004f, 0.020f, 0.064f, 0.018f);
                backgroundColorHigh = new Color(0.032f, 0.098f, 0.196f, 0.10f);
                contourColor = new Color(0.55f, 0.95f, 1f, 0.98f);
                glowColor = new Color(0.20f, 0.70f, 1f, 0.60f);
                contrast = 0.86f;
                emissiveBoost = 0.72f;
                break;

            case ContourStyle.ReferenceMatchBright:
                baseScale = 104f;
                contourLayers = 18;
                lineWidth = 0.055f;
                lineSoftness = 0.68f;
                contourBias = 0.11f;
                flowScale = 4.8f;
                rimRadius = 1.22f;
                rimIntensity = 0.82f;
                backgroundColorLow = new Color(0.006f, 0.028f, 0.078f, 0.04f);
                backgroundColorHigh = new Color(0.040f, 0.125f, 0.235f, 0.12f);
                contourColor = new Color(0.65f, 1f, 1f, 0.98f);
                glowColor = new Color(0.30f, 0.84f, 1f, 0.66f);
                contrast = 0.82f;
                emissiveBoost = 0.78f;
                break;

            case ContourStyle.ReferenceMatchDim:
                baseScale = 108f;
                contourLayers = 14;
                lineWidth = 0.065f;
                lineSoftness = 0.58f;
                contourBias = 0.13f;
                flowScale = 3.8f;
                rimRadius = 1.34f;
                rimIntensity = 0.68f;
                backgroundColorLow = new Color(0.002f, 0.014f, 0.045f, 0.014f);
                backgroundColorHigh = new Color(0.020f, 0.072f, 0.160f, 0.08f);
                contourColor = new Color(0.50f, 0.92f, 1f, 0.92f);
                glowColor = new Color(0.16f, 0.54f, 0.92f, 0.52f);
                contrast = 0.88f;
                emissiveBoost = 0.60f;
                break;

            case ContourStyle.ReferenceMatchBackdrop:
                baseScale = 116f;
                contourLayers = 28;
                lineWidth = 0.028f;
                lineSoftness = 1f;
                contourBias = 0.098f;
                flowScale = 3.24f;
                rimRadius = 1.32f;
                rimIntensity = 0.96f;
                backgroundColorLow = new Color(0.017f, 0.017f, 0.020f, 0.018f);
                backgroundColorHigh = new Color(0.048f, 0.050f, 0.058f, 0.060f);
                contourColor = new Color(0.82f, 0.82f, 0.88f, 0.95f);
                glowColor = new Color(0.46f, 0.48f, 0.56f, 0.18f);
                contrast = 0.97f;
                emissiveBoost = 0.42f;
                break;

            case ContourStyle.CyanNeon:
            default:
                baseScale = 112f;
                lineWidth = 0.08f;
                lineSoftness = 0.54f;
                contourBias = 0.13f;
                backgroundColorLow = new Color(0.006f, 0.028f, 0.070f, 0.02f);
                backgroundColorHigh = new Color(0.026f, 0.110f, 0.205f, 0.09f);
                contourColor = new Color(0.53f, 1f, 1f, 0.95f);
                glowColor = new Color(0.28f, 0.85f, 1f, 0.45f);
                contrast = 0.78f;
                emissiveBoost = 0.56f;
                break;
        }
    }

    [ContextMenu("Apply Cyan Neon Preset")]
    public void ApplyCyanNeonPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.CyanNeon, true);
        Generate();
    }

    [ContextMenu("Apply Reference Match Preset")]
    public void ApplyReferenceMatchPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.ReferenceMatch, true);
        Generate();
    }

    [ContextMenu("Apply Bright Reference Match Preset")]
    public void ApplyBrightReferenceMatchPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.ReferenceMatchBright, true);
        Generate();
    }

    [ContextMenu("Apply Dim Reference Match Preset")]
    public void ApplyDimReferenceMatchPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.ReferenceMatchDim, true);
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset")]
    public void ApplyReferenceMatchBackdropPreset()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImage()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Baseline)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageBaseline()
    {
        ApplyReferenceMatchBackdropPresetStyle(0, true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Baseline + Random Seed)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageBaselineAndRandomize()
    {
        ApplyReferenceMatchBackdropPresetStyle(0, false);
        ApplySeed(GetRandomSeed());
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Match Target + Random Seed)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        ApplySeed(GetRandomSeed());
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Exact)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageExact()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Match Target)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(false);
        contourLayers = 34;
        lineWidth = 0.022f;
        lineSoftness = 1.08f;
        contourBias = 0.102f;
        baseScale = 99.5f;
        flowScale = 3.28f;
        rimRadius = 1.38f;
        rimIntensity = 1.00f;
        backgroundColorLow = new Color(0.010f, 0.011f, 0.013f, 0.020f);
        backgroundColorHigh = new Color(0.034f, 0.036f, 0.044f, 0.054f);
        contourColor = new Color(0.72f, 0.73f, 0.79f, 0.93f);
        glowColor = new Color(0.20f, 0.22f, 0.28f, 0.08f);
        contrast = 1.06f;
        emissiveBoost = 0.25f;

        Generate();
    }

    private void ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(bool generate)
    {
        seed = ReferenceBackdropStableSeed;
        SetLatestSeed(seed);
        ApplyTabletopSciFiPresetStyle(ContourStyle.ReferenceMatchBackdrop, true);
        autoRegenerate = true;
        textureResolution = 1024;
        octaves = 5;
        lacunarity = 2.02f;
        persistence = 0.47f;
        noiseWarp = 0.34f;
        contourLayers = 32;
        lineWidth = 0.023f;
        lineSoftness = 1.02f;
        contourBias = 0.103f;
        baseScale = 100f;
        flowScale = 3.22f;
        rimRadius = 1.40f;
        rimIntensity = 0.96f;
        backgroundColorLow = new Color(0.008f, 0.009f, 0.011f, 0.020f);
        backgroundColorHigh = new Color(0.030f, 0.032f, 0.039f, 0.050f);
        contourColor = new Color(0.63f, 0.66f, 0.74f, 0.93f);
        glowColor = new Color(0.24f, 0.28f, 0.34f, 0.10f);
        contrast = 1.03f;
        emissiveBoost = 0.28f;

        if (generate)
        {
            Generate();
        }
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Stable)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageStable()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Dark)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageDark()
    {
        ApplyReferenceMatchBackdropPresetIntensity(
            bgScale: 0.86f,
            contourScale: 0.94f,
            glowScale: 0.80f,
            contrastDelta: 0.06f,
            emissiveDelta: -0.06f,
            lineWidthScale: 1.06f,
            rimRadiusAdd: 0.02f,
            rimIntensityScale: 0.96f);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Bright)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageBright()
    {
        ApplyReferenceMatchBackdropPresetIntensity(
            bgScale: 1.14f,
            contourScale: 1.02f,
            glowScale: 1.25f,
            contrastDelta: -0.06f,
            emissiveDelta: 0.10f,
            lineWidthScale: 0.97f,
            rimRadiusAdd: -0.01f,
            rimIntensityScale: 1.05f);
    }

    [ContextMenu("Reset to Reference Match Backdrop Preset")]
    public void ResetToReferenceMatchBackdropPreset()
    {
        ApplyReferenceMatchBackdropPresetReferenceImage();
    }

    private void ApplyReferenceMatchBackdropPresetIntensity(
        float bgScale,
        float contourScale,
        float glowScale,
        float contrastDelta,
        float emissiveDelta,
        float lineWidthScale = 1f,
        float rimRadiusAdd = 0f,
        float rimIntensityScale = 1f)
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(false);

        backgroundColorLow = new Color(
            Mathf.Clamp01(backgroundColorLow.r * bgScale),
            Mathf.Clamp01(backgroundColorLow.g * bgScale),
            Mathf.Clamp01(backgroundColorLow.b * bgScale),
            Mathf.Clamp01(backgroundColorLow.a * bgScale));

        backgroundColorHigh = new Color(
            Mathf.Clamp01(backgroundColorHigh.r * bgScale),
            Mathf.Clamp01(backgroundColorHigh.g * bgScale),
            Mathf.Clamp01(backgroundColorHigh.b * bgScale),
            Mathf.Clamp01(backgroundColorHigh.a * bgScale));

        contourColor = new Color(
            Mathf.Clamp01(contourColor.r * contourScale),
            Mathf.Clamp01(contourColor.g * contourScale),
            Mathf.Clamp01(contourColor.b * contourScale),
            contourColor.a);

        glowColor = new Color(
            Mathf.Clamp01(glowColor.r * glowScale),
            Mathf.Clamp01(glowColor.g * glowScale),
            Mathf.Clamp01(glowColor.b * glowScale),
            Mathf.Clamp01(glowColor.a * glowScale));

        lineWidth = Mathf.Clamp(lineWidth * lineWidthScale, 0.02f, 0.12f);
        rimRadius = Mathf.Max(1f, rimRadius + rimRadiusAdd);
        rimIntensity = Mathf.Clamp01(rimIntensity * rimIntensityScale);
        contrast = Mathf.Clamp01(contrast + contrastDelta);
        emissiveBoost = Mathf.Clamp01(emissiveBoost + emissiveDelta);

        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Balanced)")]
    public void ApplyReferenceMatchBackdropPresetBalanced()
    {
        ApplyReferenceMatchBackdropPresetStyle(1, true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Sharp)")]
    public void ApplyReferenceMatchBackdropPresetSharp()
    {
        ApplyReferenceMatchBackdropPresetStyle(2, true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Soft)")]
    public void ApplyReferenceMatchBackdropPresetSoft()
    {
        ApplyReferenceMatchBackdropPresetStyle(3, true);
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Glowy)")]
    public void ApplyReferenceMatchBackdropPresetGlowy()
    {
        ApplyReferenceMatchBackdropPresetStyle(4, true);
    }

    public void ApplyReferenceMatchBackdropPresetStyle(int preset, bool generate = true)
    {
        preset = Mathf.Clamp(preset, 0, 4);
        ApplyTabletopSciFiPresetStyle(ContourStyle.ReferenceMatchBackdrop, true);
        autoRegenerate = true;

        textureResolution = 1024;
        octaves = 5;
        lacunarity = 2.02f;
        persistence = 0.47f;
        noiseWarp = 0.34f;

        // 0: Reference image baseline (direct target)
        // 1: Balanced
        // 2: Sharp
        // 3: Soft
        // 4: Glowy

        switch (preset)
        {
            case 0:
                ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(false);
                break;

            case 1:
                contourLayers = 23;
                lineWidth = 0.036f;
                lineSoftness = 0.89f;
                contourBias = 0.11f;
                baseScale = 101.2f;
                flowScale = 3.42f;
                rimRadius = 1.3f;
                rimIntensity = 0.94f;
                backgroundColorLow = new Color(0.0018f, 0.0042f, 0.0088f, 0.0040f);
                backgroundColorHigh = new Color(0.013f, 0.031f, 0.062f, 0.050f);
                contourColor = new Color(0.36f, 0.88f, 1f, 0.94f);
                glowColor = new Color(0.11f, 0.46f, 0.78f, 0.64f);
                contrast = 0.94f;
                emissiveBoost = 0.72f;
                break;

            case 3:
                contourLayers = 20;
                lineWidth = 0.048f;
                lineSoftness = 1.0f;
                contourBias = 0.118f;
                flowScale = 3.55f;
                rimRadius = 1.34f;
                rimIntensity = 0.64f;
                contrast = 0.88f;
                emissiveBoost = 0.58f;
                backgroundColorLow = new Color(0.003f, 0.006f, 0.014f, 0.006f);
                backgroundColorHigh = new Color(0.018f, 0.044f, 0.080f, 0.052f);
                contourColor = new Color(0.44f, 0.80f, 1f, 0.94f);
                glowColor = new Color(0.15f, 0.52f, 0.95f, 0.52f);
                break;

            case 4:
                contourLayers = 22;
                lineWidth = 0.042f;
                lineSoftness = 0.86f;
                contourBias = 0.104f;
                flowScale = 3.4f;
                rimRadius = 1.26f;
                rimIntensity = 1.04f;
                contrast = 0.94f;
                emissiveBoost = 1.0f;
                backgroundColorLow = new Color(0.002f, 0.004f, 0.010f, 0.003f);
                backgroundColorHigh = new Color(0.013f, 0.030f, 0.060f, 0.052f);
                contourColor = new Color(0.44f, 0.92f, 1f, 0.96f);
                glowColor = new Color(0.18f, 0.64f, 1f, 0.78f);
                break;

            case 2:
                contourLayers = 28;
                lineWidth = 0.032f;
                lineSoftness = 0.9f;
                contourBias = 0.105f;
                baseScale = 100f;
                flowScale = 3.25f;
                rimRadius = 1.22f;
                rimIntensity = 1.03f;
                contrast = 1.02f;
                emissiveBoost = 0.94f;
                backgroundColorLow = new Color(0.001f, 0.003f, 0.008f, 0.004f);
                backgroundColorHigh = new Color(0.014f, 0.031f, 0.062f, 0.06f);
                contourColor = new Color(0.40f, 0.86f, 1f, 0.95f);
                glowColor = new Color(0.14f, 0.56f, 1f, 0.78f);
                break;
        }

        if (generate)
        {
            Generate();
        }
    }

    public void ApplyReferenceMatchBackdropPresetAndRandomize()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(false);
        ApplySeed(GetRandomSeed());
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop + Random Seed")]
    public void ApplyReferenceMatchBackdropPresetRandomized()
    {
        ApplyReferenceMatchBackdropPresetAndRandomize();
    }

    [ContextMenu("Tweak Reference Match Backdrop")]
    public void TweakReferenceMatchBackdrop()
    {
        ApplyReferenceMatchBackdropTweak(0.65f);
    }

    [ContextMenu("Tweak Reference Match Backdrop (Strong)")]
    public void TweakReferenceMatchBackdropStrong()
    {
        ApplyReferenceMatchBackdropTweak(1.2f);
    }

    [ContextMenu("Tweak Reference Match Backdrop (Mild)")]
    public void TweakReferenceMatchBackdropMild()
    {
        ApplyReferenceMatchBackdropTweak(0.35f);
    }

    [ContextMenu("Tweak Reference Match Backdrop (Very Mild)")]
    public void TweakReferenceMatchBackdropVeryMild()
    {
        ApplyReferenceMatchBackdropTweak(0.18f);
    }

    private void ApplyReferenceMatchBackdropTweak(float intensity)
    {
        if (contourStyle != ContourStyle.ReferenceMatchBackdrop)
        {
            ApplyReferenceMatchBackdropPresetReferenceImage();
        }

        float widthMin = 1f - 0.20f * Mathf.Max(0.2f, Mathf.Min(1f, intensity));
        float widthMax = 1f + 0.35f * Mathf.Max(0.2f, Mathf.Min(1.6f, intensity));
        float softnessMin = 1f - 0.2f * intensity;
        float softnessMax = 1f + 0.3f * intensity;
        float bias = 0.06f * intensity;
        float flow = 0.85f + 0.22f * intensity;
        float rimMin = 0.9f;
        float rimMax = 1f + 0.10f * intensity;

        lineWidth = Mathf.Clamp(lineWidth * UnityEngine.Random.Range(widthMin, widthMax), 0.02f, 0.1f);
        lineSoftness = Mathf.Clamp(lineSoftness * UnityEngine.Random.Range(softnessMin, softnessMax), 0.45f, 1f);
        contourBias = Mathf.Clamp(contourBias + UnityEngine.Random.Range(-bias, bias), 0.06f, 0.24f);
        flowScale = Mathf.Clamp(flowScale * UnityEngine.Random.Range(flow, 1.25f + 0.25f * intensity), 2.2f, 6f);
        rimIntensity = Mathf.Clamp(rimIntensity * UnityEngine.Random.Range(rimMin, rimMax), 0.45f, 1f);
        contourColor = new Color(
            Mathf.Clamp01(contourColor.r + UnityEngine.Random.Range(-0.02f, 0.02f) * intensity),
            Mathf.Clamp01(contourColor.g + UnityEngine.Random.Range(-0.04f, 0.04f) * intensity),
            Mathf.Clamp01(contourColor.b + UnityEngine.Random.Range(-0.06f, 0.06f) * intensity),
            contourColor.a);

        Generate();
    }

    public void ApplyTabletopStyleAndRandomize(ContourStyle style)
    {
        ApplyTabletopSciFiPresetStyle(style, true);
        ApplySeed(GetRandomSeed());
        Generate();
    }

    [ContextMenu("Apply Cyan Mystic Preset")]
    public void ApplyCyanMysticPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.CyanMystic, true);
        Generate();
    }

    [ContextMenu("Apply High Contrast Preset")]
    public void ApplyHighContrastPreset()
    {
        ApplyTabletopSciFiPresetStyle(ContourStyle.HighContrast, true);
        Generate();
    }

    [ContextMenu("Random Seed and Apply Tabletop Preset")]
    public void RandomizeTabletopSeed()
    {
        tablePresetApplied = true;
        if (contourStyle == default)
        {
            contourStyle = ContourStyle.ReferenceMatchBackdrop;
        }

        ApplySeed(GetRandomSeed());
        Generate();
    }

    private string GetCurrentPresetPayload()
    {
        BackdropParameterSnapshot snapshot = new BackdropParameterSnapshot
        {
            contourStyle = contourStyle.ToString(),
            seed = latestSeed,
            textureResolution = textureResolution,
            contourLayers = contourLayers,
            octaves = octaves,
            lineWidth = lineWidth,
            lineSoftness = lineSoftness,
            contourBias = contourBias,
            baseScale = baseScale,
            lacunarity = lacunarity,
            persistence = persistence,
            noiseWarp = noiseWarp,
            flowScale = flowScale,
            rimRadius = rimRadius,
            rimIntensity = rimIntensity,
            contrast = contrast,
            emissiveBoost = emissiveBoost,
            backgroundColorLow = backgroundColorLow,
            backgroundColorHigh = backgroundColorHigh,
            contourColor = contourColor,
            glowColor = glowColor
        };
        return JsonUtility.ToJson(snapshot, true);
    }

    public string GetCurrentPresetPayloadForSerialization()
    {
        return GetCurrentPresetPayload();
    }

    public bool LoadPresetPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            BackdropParameterSnapshot snapshot = JsonUtility.FromJson<BackdropParameterSnapshot>(payload);
            if (snapshot.contourStyle == null)
            {
                return false;
            }

            if (!Enum.TryParse(snapshot.contourStyle, out ContourStyle parsedStyle))
            {
                return false;
            }

            contourStyle = parsedStyle;
            tablePresetApplied = true;
            int loadedSeed = Mathf.Clamp(Mathf.RoundToInt(snapshot.seed), int.MinValue, int.MaxValue);
            seed = loadedSeed;
            SetLatestSeed(loadedSeed);
            textureResolution = Mathf.Clamp(snapshot.textureResolution, MinResolution, MaxResolution);
            contourLayers = Mathf.Clamp(snapshot.contourLayers, 2, 64);
            octaves = Mathf.Clamp(snapshot.octaves, 1, 6);
            lineWidth = snapshot.lineWidth;
            lineSoftness = snapshot.lineSoftness;
            contourBias = snapshot.contourBias;
            baseScale = snapshot.baseScale;
            lacunarity = snapshot.lacunarity;
            persistence = snapshot.persistence;
            noiseWarp = snapshot.noiseWarp;
            flowScale = snapshot.flowScale;
            rimRadius = snapshot.rimRadius;
            rimIntensity = snapshot.rimIntensity;
            contrast = snapshot.contrast;
            emissiveBoost = snapshot.emissiveBoost;
            backgroundColorLow = snapshot.backgroundColorLow;
            backgroundColorHigh = snapshot.backgroundColorHigh;
            contourColor = snapshot.contourColor;
            glowColor = snapshot.glowColor;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void SetLatestSeed(int value)
    {
        latestSeed = value;
        latestSeedInitialized = true;
    }

    [ContextMenu("Copy Current Settings")]
    public void CopyCurrentSettings()
    {
        string payload = GetCurrentPresetPayload();
        GUIUtility.systemCopyBuffer = payload;
        Debug.Log($"[DeskContourTerrainGenerator] 参数已复制到剪贴板（长度 {payload.Length}）\n{payload}");
    }

    private static int RandomInt(int minInclusive, int maxInclusive)
    {
        int min = Math.Min(minInclusive, maxInclusive);
        int max = Math.Max(minInclusive, maxInclusive);
        if (min == max)
        {
            return min;
        }

        return UnityEngine.Random.Range(min, max + 1);
    }

    private void ApplySeed(int value)
    {
        seed = value;
        SetLatestSeed(seed);
    }

    private static int GetRandomSeed()
    {
        return RandomInt(-1000000, 1000000);
    }

    public void ApplyTabletopSciFiPresetIfNeeded()
    {
        if (!tablePresetApplied)
        {
            ApplyReferenceMatchBackdropPresetReferenceImage();
            Generate();
            return;
        }

        if (HasGeneratedTexture)
        {
            return;
        }

        Generate();
    }

    public void ClearGenerated()
    {
        tablePresetApplied = false;
        CleanupGeneratedAssets();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null && generatedMaterial != null)
        {
            renderer.sharedMaterial = null;
        }
    }

    private void BuildHeightField()
    {
        int width = (int)textureResolution;
        int height = (int)textureResolution;
        heightField = new float[width * height];

        float[] octaveOffsetX = new float[octaves];
        float[] octaveOffsetY = new float[octaves];
        float maxPossibleHeight = 0f;
        float amplitude = 1f;

        System.Random rng = new System.Random(seed);
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsetX[i] = ((float)rng.NextDouble() * 20000f) - 10000f;
            octaveOffsetY[i] = ((float)rng.NextDouble() * 20000f) - 10000f;
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                float u = x / (float)width;
                float v = y / (float)height;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = u * baseScale * frequency + octaveOffsetX[o] + Mathf.Sin((v * 64f + contourBias) * noiseWarp) * 5f;
                    float sampleY = v * baseScale * frequency + octaveOffsetY[o] + Mathf.Cos((u * 64f - contourBias) * noiseWarp) * 5f;
                    float perlin = Mathf.PerlinNoise(sampleX * 0.0025f, sampleY * 0.0025f) * 2f - 1f;
                    noiseHeight += perlin * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                float normalized = (noiseHeight / maxPossibleHeight + 1f) * 0.5f;
                int idx = y * width + x;
                heightField[idx] = normalized;
                minHeight = Math.Min(minHeight, normalized);
                maxHeight = Math.Max(maxHeight, normalized);
            }
        }

        float range = Mathf.Max(0.0001f, maxHeight - minHeight);
        for (int i = 0; i < heightField.Length; i++)
        {
            float v = (heightField[i] - minHeight) / range;
            v = Mathf.Pow(Mathf.Clamp01(v), 1f + contrast * 1.6f - 0.8f);
            heightField[i] = Mathf.Lerp(0f, 1f, v);
        }
    }

    private Texture2D BuildTexture()
    {
        int resolution = (int)textureResolution;
        if (generatedTexture == null || generatedTexture.width != resolution || generatedTexture.height != resolution)
        {
            if (generatedTexture != null)
            {
                CleanupTexture(generatedTexture);
            }

            generatedTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            generatedTexture.wrapMode = TextureWrapMode.Clamp;
            generatedTexture.filterMode = FilterMode.Bilinear;
        }

        Color[] pixels = new Color[resolution * resolution];
        float contourStep = 1f / contourLayers;
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < resolution; y++)
        {
            float ny = y / (float)resolution;
            for (int x = 0; x < resolution; x++)
            {
                int i = y * resolution + x;
                float h = heightField[i];
                float nx = x / (float)resolution;

                float radial = Vector2.Distance(new Vector2(nx, ny), center);
                float rim = Mathf.Pow(Mathf.Clamp01(1f - radial * rimRadius), rimIntensity);
                float ambient = Mathf.Lerp(backgroundColorLow.a, backgroundColorHigh.a, h);
                Color baseColor = Color.Lerp(backgroundColorLow, backgroundColorHigh, h);
                Color pixel = new Color(baseColor.r, baseColor.g, baseColor.b, ambient);

                float nearestContour = Mathf.Abs(Mathf.Repeat(h + contourBias, contourStep) - contourStep * 0.5f);
                float nearestNormalized = nearestContour / (contourStep * 0.5f + 0.0001f);
                float flowMask = Mathf.PerlinNoise((nx + contourBias) * flowScale, (ny - contourBias) * flowScale);
                float haze = Mathf.PerlinNoise((nx + 7f) * 0.8f, (ny - 11f) * 0.8f);
                float glowFlow = Mathf.SmoothStep(0.2f, 1f, flowMask);
                float lineCoreWidth = Mathf.Max(0.008f, lineWidth);
                float contour = 1f - Mathf.InverseLerp(lineCoreWidth, lineCoreWidth + lineSoftness * 0.6f, nearestNormalized);
                contour = Mathf.Clamp01(contour);
                contour *= glowFlow;
                if (contour > 0f)
                {
                    Color contourFinal = Color.Lerp(
                        Color.clear,
                        contourColor,
                        Mathf.Pow(contour, 2.4f));
                    pixel = Color.Lerp(pixel, contourFinal, contour);
                }

                float glow = contour * rim * 0.5f;
                Color glowColorFinal = new Color(
                    glowColor.r * (0.9f + rim),
                    glowColor.g * (0.9f + rim),
                    glowColor.b * (0.9f + rim),
                    glow);
                pixel = Color.Lerp(pixel, pixel + glowColorFinal, contour * 0.8f);
                pixel = Color.Lerp(pixel, new Color(glowColor.r * 0.45f, glowColor.g * 0.45f, glowColor.b * 0.45f, 0.04f + haze * 0.03f), haze * 0.22f);
                pixel.a = Mathf.Clamp01(pixel.a + contour * 0.75f + rim * 0.06f + emissiveBoost * 0.02f);
                pixel = Color.Lerp(pixel, pixel * (0.6f + rim * 2.4f), emissiveBoost * 0.6f);
                pixel = Color.Lerp(pixel, new Color(contourColor.r, contourColor.g, contourColor.b, 0.05f), glowFlow * 0.12f);
                if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
                {
                    float pixelLuma = 0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b;
                    Color monochrome = new Color(pixelLuma, pixelLuma, pixelLuma * 1.02f, pixel.a);
                    float neutralTone = Mathf.Pow(Mathf.Clamp01(pixelLuma), 0.94f);
                    Color neutral = new Color(neutralTone, neutralTone, neutralTone * 0.99f, pixel.a);
                    pixel = Color.Lerp(pixel, neutral, 0.98f);
                    pixel = Color.Lerp(pixel, monochrome, 0.35f);
                    pixel *= new Color(0.74f, 0.74f, 0.78f, 0.88f);
                    pixel.a = Mathf.Lerp(0.93f, pixel.a, 0.22f);
                }
                pixel.a *= Mathf.Lerp(0.78f, 1f, rim);
                if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
                {
                    float radialFade = Mathf.SmoothStep(0.0f, 1f, radial);
                    pixel = Color.Lerp(pixel, pixel * 0.82f, radialFade * 0.18f);
                }
                else
                {
                    pixel = Color.Lerp(pixel, pixel * 0.58f, radial * 0.45f);
                }

                pixels[i] = pixel;
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply(false, false);
        return generatedTexture;
    }

    private void ApplyTexture(Texture2D texture)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (generatedSprite == null || generatedSprite.texture != texture)
            {
                if (generatedSprite != null)
                {
                    CleanupSprite(generatedSprite);
                }

                generatedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), textureResolution);
            }

            spriteRenderer.sprite = generatedSprite;
            spriteRenderer.color = Color.white;
            return;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Shader shader = ChooseShader(renderer.sharedMaterial);
        Material sourceMaterial = renderer.sharedMaterial;
        if (sourceMaterial == null || sourceMaterial.shader != shader)
        {
            sourceMaterial = new Material(shader);
        }
        else
        {
            sourceMaterial = new Material(sourceMaterial);
        }

        sourceMaterial.name = $"{name}_ContourMap_Material";
        sourceMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        SetMaterialTexture(sourceMaterial, texture);
        ApplyTransparentMaterialState(sourceMaterial);

        if (sourceMaterial.HasProperty(ColorProperty))
        {
            sourceMaterial.SetColor(ColorProperty, Color.white);
        }
        if (sourceMaterial.HasProperty(BaseColorProperty))
        {
            sourceMaterial.SetColor(BaseColorProperty, Color.white);
        }
        
        if (sourceMaterial.HasProperty(EmissionColorProperty))
        {
            sourceMaterial.SetColor(EmissionColorProperty, glowColor * 0.3f * emissiveBoost);
            sourceMaterial.EnableKeyword("_EMISSION");
        }

        generatedMaterial = sourceMaterial;
        renderer.sharedMaterial = generatedMaterial;
    }

    private static void ApplyTransparentMaterialState(Material material)
    {
        if (material == null || !material.HasProperty("_Mode"))
        {
            if (material != null && material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material != null && material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material != null && material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material != null && material.HasProperty(_BaseColorProperty))
            {
                material.SetColor(_BaseColorProperty, Color.white);
            }

            if (material != null && material.HasProperty(ColorProperty))
            {
                material.SetColor(ColorProperty, Color.white);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return;
        }

        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void EnsureTargets()
    {
        if (GetComponent<SpriteRenderer>() != null)
        {
            return;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (GetComponent<Renderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshFilter.sharedMesh == null)
        {
            GameObject helper = GameObject.CreatePrimitive(PrimitiveType.Quad);
            MeshFilter helperFilter = helper.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = helperFilter != null ? helperFilter.sharedMesh : null;
            if (Application.isPlaying)
            {
                Destroy(helper);
            }
            else
            {
                DestroyImmediate(helper);
            }
        }
    }

    private static Shader ChooseShader(Material sourceMaterial)
    {
        if (sourceMaterial != null && sourceMaterial.shader != null)
        {
            return sourceMaterial.shader;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Transparent");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Sprites/Default");
    }

    private static void SetMaterialTexture(Material material, Texture texture)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(BaseMapProperty))
        {
            material.SetTexture(BaseMapProperty, texture);
        }

        if (material.HasProperty(MainTexProperty))
        {
            material.SetTexture(MainTexProperty, texture);
        }
    }

    private void CleanupGeneratedAssets()
    {
        if (generatedSprite != null)
        {
            CleanupSprite(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            CleanupTexture(generatedTexture);
            generatedTexture = null;
        }

        if (generatedMaterial != null)
        {
            DestroyRuntimeAsset(generatedMaterial);
            generatedMaterial = null;
        }
    }

    private static void CleanupSprite(Sprite sprite)
    {
        if (Application.isPlaying)
        {
            Destroy(sprite);
        }
        else
        {
            DestroyImmediate(sprite);
        }
    }

    private static void CleanupTexture(Texture texture)
    {
        if (Application.isPlaying)
        {
            Destroy(texture);
        }
        else
        {
            DestroyImmediate(texture);
        }
    }

    private static void DestroyRuntimeAsset(UnityObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
