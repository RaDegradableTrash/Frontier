using System;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Procedural/Desk Contour Terrain Map")]
public class DeskContourTerrainGenerator : MonoBehaviour
{
    private const string SavedReferenceBackdropPresetKey = "DeskContourTerrainGenerator.ReferenceBackdropPreset.V1";
    private const int ReferenceBackdropStableSeed = 982741;
    private const int ReferenceBackdropRenderQueue = 2050;
    private const string ContourLineOverlayName = "DarkWhiteContourLineOverlay";
    private const string ContourTextureOverlayName = "DarkWhiteContourTextureOverlay";

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
    private static readonly int _BaseMapProperty = BaseMapProperty;
    private static readonly int BaseTexProperty = Shader.PropertyToID("_BaseTex");
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
    private GameObject contourLineOverlay;
    private Material contourLineOverlayMaterial;
    private GameObject contourTextureOverlay;
    private Material contourTextureOverlayMaterial;
    private Texture2D contourTextureOverlayTexture;
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
        if (gameObject != null && gameObject.name == "DesktopQuad"
            && (!tablePresetApplied || !HasGeneratedTexture || contourStyle != ContourStyle.ReferenceMatchBackdrop))
        {
            ApplyReferenceMatchBackdropWhiteLineDarkPreset();
        }
        else if (gameObject != null && gameObject.name == "DesktopQuad"
            && contourStyle == ContourStyle.ReferenceMatchBackdrop)
        {
            EnsureBackdropMaterialVisibleStrongly();
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
            RepairTabletopBackdropTransform();
            BuildHeightField();
            ApplyTexture(BuildTexture());
            if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
            {
                RebuildContourLineOverlay();
            }
            else
            {
                ClearContourLineOverlay();
            }
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

        ApplySeed((int)GetRandomSeed());
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

    private static bool IsUniversalRenderPipelineActive()
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
        {
            return false;
        }

        return pipeline.GetType().Name.Contains("UniversalRenderPipelineAsset", StringComparison.Ordinal);
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
        Debug.Log($"[DeskContour] ApplyToDesktopQuadInternal requested on {gameObject?.name}");
        GameObject desk = GameObject.Find("DesktopQuad");
        if (desk == null)
        {
            Debug.LogWarning("[DeskContourTerrainGenerator] 未找到名为 DesktopQuad 的对象");
            return;
        }

        if (desk == gameObject)
        {
            Debug.Log("[DeskContour] Using current component for DesktopQuad");
            applyPresetToTarget(this);
            return;
        }

        DeskContourTerrainGenerator deskGenerator = desk.GetComponent<DeskContourTerrainGenerator>();
        if (deskGenerator == null)
        {
            deskGenerator = desk.AddComponent<DeskContourTerrainGenerator>();
        }

        applyPresetToTarget(deskGenerator);
        Debug.Log($"[DeskContour] Applying to DesktopQuad component. tablePreset={deskGenerator.tablePresetApplied}, contourStyle={deskGenerator.contourStyle}");
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
            ApplySeed((int)GetRandomSeed());
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
        ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImage()
    {
        Debug.Log("[DeskContour] ApplyReferenceMatchBackdropPresetReferenceImage() called");
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
        contourStyle = ContourStyle.ReferenceMatchBackdrop;
        tablePresetApplied = true;
        Generate();
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
        ApplySeed((int)GetRandomSeed());
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Match Target + Random Seed)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageMatchTargetAndRandomize()
    {
        Debug.Log("[DeskContour] Execute Reference Match Backdrop Preset (Reference Image Match Target + Random Seed)");
        ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        tablePresetApplied = true;
        ApplySeed((int)GetRandomSeed());
        autoRegenerate = true;
        Generate();
        Debug.Log($"[DeskContour] Seed={seed}, contourColor={contourColor}, bgLow={backgroundColorLow}, bgHigh={backgroundColorHigh}, contrast={contrast}");
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Exact)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageExact()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(true);
        contourStyle = ContourStyle.ReferenceMatchBackdrop;
    }

    [ContextMenu("Apply Reference Match Backdrop Preset (Reference Image Match Target)")]
    public void ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget()
    {
        Debug.Log("[DeskContour] ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget() called");
        ApplyReferenceMatchBackdropPresetReferenceImageExactInternal(false);
        contourStyle = ContourStyle.ReferenceMatchBackdrop;
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
        tablePresetApplied = true;
        autoRegenerate = true;

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
        tablePresetApplied = true;

        if (generate)
        {
            Generate();
        }
        else
        {
            contourStyle = ContourStyle.ReferenceMatchBackdrop;
            autoRegenerate = true;
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

    [ContextMenu("Apply Reference Match Backdrop White Line Preset")]
    public void ApplyReferenceMatchBackdropWhiteLinePreset()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
        contourLayers = 36;
        lineWidth = 0.018f;
        lineSoftness = 0.92f;
        contourBias = 0.10f;
        contourColor = Color.white;
        glowColor = new Color(1f, 1f, 1f, 0.45f);
        baseScale = 103f;
        flowScale = 3.08f;
        rimRadius = 1.52f;
        rimIntensity = 1.04f;
        contrast = 1.04f;
        emissiveBoost = 0.46f;
        backgroundColorLow = new Color(0.004f, 0.005f, 0.007f, 1f);
        backgroundColorHigh = new Color(0.020f, 0.024f, 0.032f, 1f);
        contourStyle = ContourStyle.ReferenceMatchBackdrop;
        autoRegenerate = true;
        tablePresetApplied = true;
        Generate();
    }

    [ContextMenu("Apply Reference Match Backdrop White Line (Dark Base) Preset")]
    public void ApplyReferenceMatchBackdropWhiteLineDarkPreset()
    {
        ApplyReferenceMatchBackdropPresetReferenceImageExact();
        contourLayers = 18;
        lineWidth = 0.018f;
        lineSoftness = 0.12f;
        contourBias = 0.10f;
        contourColor = Color.white;
        glowColor = new Color(1f, 1f, 1f, 0.30f);
        baseScale = 36f;
        flowScale = 1.35f;
        rimRadius = 1.42f;
        rimIntensity = 0.92f;
        contrast = 1.0f;
        emissiveBoost = 0.38f;
        octaves = 4;
        noiseWarp = 0.18f;
        backgroundColorLow = new Color(0.0010f, 0.0012f, 0.0016f, 1f);
        backgroundColorHigh = new Color(0.014f, 0.017f, 0.022f, 1f);
        contourStyle = ContourStyle.ReferenceMatchBackdrop;
        autoRegenerate = true;
        tablePresetApplied = true;
        Generate();
    }

    [ContextMenu("Apply Dark Bottom White-Line Tabletop Preset")]
    public void ApplyDarkBottomWhiteLineTabletopPreset()
    {
        ApplyReferenceMatchBackdropWhiteLineDarkPreset();
        if (gameObject != null && gameObject.name == "DesktopQuad")
        {
            EnsureBackdropMaterialVisibleStrongly();
        }
    }

    [ContextMenu("Force Rebuild Tabletop Backdrop Visibility")]
    public void ForceRebuildTabletopBackdropVisibility()
    {
        EnsureTargets();
        RepairTabletopBackdropTransform();

        if (contourStyle != ContourStyle.ReferenceMatchBackdrop || !tablePresetApplied || !HasGeneratedTexture)
        {
            ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
            return;
        }

        ApplyTexture(generatedTexture);
    }

    [ContextMenu("Force Rebuild And Repair Tabletop Backdrop Visibility")]
    public void ForceRebuildAndRepairTabletopBackdropVisibility()
    {
        ForceRebuildTabletopBackdropVisibility();
        EnsureBackdropMaterialVisibleStrongly();
    }

    public void RepairTabletopBackdropVisibility()
    {
        ForceRebuildTabletopBackdropVisibility();
        EnsureBackdropMaterialVisibleStrongly();
    }

    [ContextMenu("Force Make Backdrop Visible")]
    public void ForceMakeBackdropVisible()
    {
        EnsureBackdropMaterialVisibleStrongly();
    }

    private void EnsureBackdropMaterialVisibleStrongly()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        EnsureTargets();
        RepairTabletopBackdropTransform();

        if (!HasGeneratedTexture)
        {
            if (contourStyle != ContourStyle.ReferenceMatchBackdrop || !tablePresetApplied)
            {
                ApplyReferenceMatchBackdropWhiteLineDarkPreset();
            }
            else
            {
                Generate();
            }
        }

        if (!HasGeneratedTexture || generatedTexture == null)
        {
            return;
        }

        if (renderer.sharedMaterial == null || !HasBackdropDrawableTexture(renderer.sharedMaterial, generatedTexture))
        {
            Shader fallback = Shader.Find("Sprites/Default");
            if (fallback == null)
            {
                fallback = Shader.Find("Unlit/Texture");
            }

            if (fallback == null)
            {
                fallback = Shader.Find("Unlit/Transparent");
            }

            if (fallback == null)
            {
                Debug.LogWarning($"[DeskContour] 无可用回退材质用于恢复桌布可见性: {name}");
                return;
            }

            Material fallbackMaterial = new Material(fallback);
            SetMaterialTexture(fallbackMaterial, generatedTexture);
            ApplyTransparentMaterialState(fallbackMaterial, true);
            ApplyBackdropOpaqueMaterialState(fallbackMaterial);
            renderer.sharedMaterial = fallbackMaterial;
            generatedMaterial = fallbackMaterial;
        }
        else
        {
            RebindBackdropMaterialTextures(renderer, renderer.sharedMaterial, generatedTexture);
            generatedMaterial = renderer.sharedMaterial;
        }

        ForceBackdropMaterialVisible(renderer, renderer.sharedMaterial, true);
        NormalizeBackdropMaterialRenderState(renderer, renderer.sharedMaterial, true);
        ApplyBackdropForegroundState(renderer.sharedMaterial, true);
        ClearBackdropMaterialKeywordNoise(renderer.sharedMaterial, true);
        renderer.sharedMaterial.mainTexture = generatedTexture;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 1000);
        ApplyBackdropVisibilityHardening(renderer, renderer.sharedMaterial, true);
        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        RebuildContourLineOverlay();
    }

    private void RebuildContourLineOverlay()
    {
        if (heightField == null || heightField.Length == 0 || contourStyle != ContourStyle.ReferenceMatchBackdrop)
        {
            ClearContourLineOverlay();
            return;
        }

        MeshFilter parentMeshFilter = GetComponent<MeshFilter>();
        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        if (parentMeshFilter == null || parentRenderer == null)
        {
            return;
        }

        Transform overlayParent = transform.parent != null ? transform.parent : transform;
        if (contourLineOverlay == null)
        {
            Transform existing = overlayParent.Find(ContourLineOverlayName);
            contourLineOverlay = existing != null ? existing.gameObject : new GameObject(ContourLineOverlayName);
            contourLineOverlay.transform.SetParent(overlayParent, false);
        }

        contourLineOverlay.hideFlags = HideFlags.DontSave;
        contourLineOverlay.SetActive(true);
        if (transform.parent != null)
        {
            contourLineOverlay.transform.localPosition = transform.localPosition + Vector3.up * 0.035f;
            contourLineOverlay.transform.localRotation = Quaternion.identity;
            contourLineOverlay.transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                1f,
                Mathf.Abs(transform.localScale.y));
        }
        else
        {
            contourLineOverlay.transform.position = transform.position + Vector3.up * 0.035f;
            contourLineOverlay.transform.rotation = Quaternion.identity;
            contourLineOverlay.transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                1f,
                Mathf.Abs(transform.localScale.y));
        }

        MeshFilter meshFilter = contourLineOverlay.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = contourLineOverlay.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = contourLineOverlay.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = contourLineOverlay.AddComponent<MeshRenderer>();
        }

        Mesh overlayMesh = BuildContourLineMesh();
        meshFilter.sharedMesh = overlayMesh;
        meshRenderer.sharedMaterial = GetContourLineOverlayMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRenderer.allowOcclusionWhenDynamic = false;
        meshRenderer.sortingOrder = 3000;
        meshRenderer.enabled = true;

        RebuildContourTextureOverlay(overlayParent);
    }

    private void RebuildContourTextureOverlay(Transform overlayParent)
    {
        if (contourTextureOverlay != null)
        {
            contourTextureOverlay.SetActive(false);
        }

        return;

#pragma warning disable CS0162
        if (generatedTexture == null || overlayParent == null)
        {
            return;
        }

        if (contourTextureOverlay == null)
        {
            Transform existing = overlayParent.Find(ContourTextureOverlayName);
            contourTextureOverlay = existing != null ? existing.gameObject : new GameObject(ContourTextureOverlayName);
            contourTextureOverlay.transform.SetParent(overlayParent, false);
        }

        contourTextureOverlay.hideFlags = HideFlags.DontSave;
        contourTextureOverlay.SetActive(true);
        contourTextureOverlay.transform.localPosition = transform.localPosition + Vector3.up * 0.060f;
        contourTextureOverlay.transform.localRotation = Quaternion.identity;
        contourTextureOverlay.transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            1f,
            Mathf.Abs(transform.localScale.y));

        MeshFilter meshFilter = contourTextureOverlay.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = contourTextureOverlay.AddComponent<MeshFilter>();
        }

        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = BuildTextureOverlayMesh();
        }

        MeshRenderer meshRenderer = contourTextureOverlay.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = contourTextureOverlay.AddComponent<MeshRenderer>();
        }

        Material material = GetContourTextureOverlayMaterial();
        SetMaterialTexture(material, BuildVisibleContourTextureOverlay());
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRenderer.allowOcclusionWhenDynamic = false;
        meshRenderer.sortingOrder = 2500;
        meshRenderer.enabled = true;
#pragma warning restore CS0162
    }

    private static Mesh BuildTextureOverlayMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "DarkWhiteContourTextureOverlayMesh";
        mesh.SetVertices(new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        });
        mesh.SetUVs(0, new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        });
        mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Texture2D BuildVisibleContourTextureOverlay()
    {
        const int overlayResolution = 1024;
        if (contourTextureOverlayTexture == null
            || contourTextureOverlayTexture.width != overlayResolution
            || contourTextureOverlayTexture.height != overlayResolution)
        {
            if (contourTextureOverlayTexture != null)
            {
                CleanupTexture(contourTextureOverlayTexture);
            }

            contourTextureOverlayTexture = new Texture2D(overlayResolution, overlayResolution, TextureFormat.RGBA32, false, true);
            contourTextureOverlayTexture.wrapMode = TextureWrapMode.Clamp;
            contourTextureOverlayTexture.filterMode = FilterMode.Bilinear;
        }

        Color[] pixels = new Color[overlayResolution * overlayResolution];
        int sourceResolution = Mathf.Clamp(textureResolution, MinResolution, MaxResolution);
        for (int y = 0; y < overlayResolution; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.RoundToInt(y / (float)(overlayResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
            for (int x = 0; x < overlayResolution; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.RoundToInt(x / (float)(overlayResolution - 1) * (sourceResolution - 1)), 0, sourceResolution - 1);
                float h = heightField[sourceY * sourceResolution + sourceX];
                float band = Mathf.Abs(Mathf.Sin((h * contourLayers + contourBias) * Mathf.PI));
                float line = 1f - Mathf.SmoothStep(0.015f, 0.070f, band);
                pixels[y * overlayResolution + x] = Color.Lerp(
                    new Color(0.001f, 0.0015f, 0.0022f, 1f),
                    Color.white,
                    Mathf.Pow(line, 0.9f));
            }
        }

        contourTextureOverlayTexture.SetPixels(pixels);
        contourTextureOverlayTexture.Apply(false, false);
        return contourTextureOverlayTexture;
    }

    private Mesh BuildContourLineMesh()
    {
        int resolution = Mathf.Clamp(textureResolution, MinResolution, MaxResolution);
        int step = Mathf.Max(6, resolution / 96);
        int gridSize = Mathf.Max(8, resolution / step);
        int layers = Mathf.Clamp(contourLayers, 10, 56);
        var vertices = new System.Collections.Generic.List<Vector3>(gridSize * gridSize);
        var indices = new System.Collections.Generic.List<int>(gridSize * layers);

        for (int layer = 1; layer < layers; layer++)
        {
            float threshold = layer / (float)layers;
            for (int gy = 0; gy < gridSize - 1; gy++)
            {
                int y0 = Mathf.Min(resolution - 1, gy * step);
                int y1 = Mathf.Min(resolution - 1, (gy + 1) * step);
                for (int gx = 0; gx < gridSize - 1; gx++)
                {
                    int x0 = Mathf.Min(resolution - 1, gx * step);
                    int x1 = Mathf.Min(resolution - 1, (gx + 1) * step);

                    float h00 = heightField[y0 * resolution + x0] - threshold;
                    float h10 = heightField[y0 * resolution + x1] - threshold;
                    float h11 = heightField[y1 * resolution + x1] - threshold;
                    float h01 = heightField[y1 * resolution + x0] - threshold;

                    AddMarchingSquareSegments(vertices, indices, h00, h10, h11, h01, x0, y0, x1, y1, resolution);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{name}_WhiteContourLineOverlay";
        mesh.indexFormat = vertices.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0, true);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddMarchingSquareSegments(
        System.Collections.Generic.List<Vector3> vertices,
        System.Collections.Generic.List<int> indices,
        float h00,
        float h10,
        float h11,
        float h01,
        int x0,
        int y0,
        int x1,
        int y1,
        int resolution)
    {
        Vector3[] crossings = new Vector3[4];
        int count = 0;
        TryAddCrossing(crossings, ref count, h00, h10, x0, y0, x1, y0, resolution);
        TryAddCrossing(crossings, ref count, h10, h11, x1, y0, x1, y1, resolution);
        TryAddCrossing(crossings, ref count, h11, h01, x1, y1, x0, y1, resolution);
        TryAddCrossing(crossings, ref count, h01, h00, x0, y1, x0, y0, resolution);

        if (count < 2)
        {
            return;
        }

        AddLine(vertices, indices, crossings[0], crossings[1]);
        if (count == 4)
        {
            AddLine(vertices, indices, crossings[2], crossings[3]);
        }
    }

    private static void TryAddCrossing(
        Vector3[] crossings,
        ref int count,
        float a,
        float b,
        int ax,
        int ay,
        int bx,
        int by,
        int resolution)
    {
        bool crosses = (a <= 0f && b > 0f) || (a > 0f && b <= 0f);
        if (!crosses || count >= crossings.Length)
        {
            return;
        }

        float t = Mathf.Clamp01(Mathf.Abs(a) / (Mathf.Abs(a) + Mathf.Abs(b) + 0.0001f));
        float x = Mathf.Lerp(ax, bx, t) / Mathf.Max(1f, resolution - 1f) - 0.5f;
        float y = Mathf.Lerp(ay, by, t) / Mathf.Max(1f, resolution - 1f) - 0.5f;
        crossings[count++] = new Vector3(x, 0f, y);
    }

    private static void AddLine(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<int> indices, Vector3 a, Vector3 b)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        indices.Add(start);
        indices.Add(start + 1);
    }

    private Material GetContourLineOverlayMaterial()
    {
        if (contourLineOverlayMaterial != null)
        {
            return contourLineOverlayMaterial;
        }

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        contourLineOverlayMaterial = new Material(shader);
        contourLineOverlayMaterial.name = $"{name}_WhiteContourLineOverlay_Material";
        if (contourLineOverlayMaterial.HasProperty(ColorProperty))
        {
            contourLineOverlayMaterial.SetColor(ColorProperty, Color.white);
        }

        if (contourLineOverlayMaterial.HasProperty(BaseColorProperty))
        {
            contourLineOverlayMaterial.SetColor(BaseColorProperty, Color.white);
        }

        if (contourLineOverlayMaterial.HasProperty("_ZWrite"))
        {
            contourLineOverlayMaterial.SetInt("_ZWrite", 0);
        }

        if (contourLineOverlayMaterial.HasProperty("_ZTest"))
        {
            contourLineOverlayMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (contourLineOverlayMaterial.HasProperty("_Cull"))
        {
            contourLineOverlayMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        contourLineOverlayMaterial.renderQueue = 3000;
        return contourLineOverlayMaterial;
    }

    private Material GetContourTextureOverlayMaterial()
    {
        if (contourTextureOverlayMaterial != null)
        {
            return contourTextureOverlayMaterial;
        }

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        contourTextureOverlayMaterial = new Material(shader);
        contourTextureOverlayMaterial.name = $"{name}_WhiteContourTextureOverlay_Material";
        if (contourTextureOverlayMaterial.HasProperty(ColorProperty))
        {
            contourTextureOverlayMaterial.SetColor(ColorProperty, Color.white);
        }

        if (contourTextureOverlayMaterial.HasProperty(BaseColorProperty))
        {
            contourTextureOverlayMaterial.SetColor(BaseColorProperty, Color.white);
        }

        if (contourTextureOverlayMaterial.HasProperty("_Cull"))
        {
            contourTextureOverlayMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (contourTextureOverlayMaterial.HasProperty("_ZWrite"))
        {
            contourTextureOverlayMaterial.SetInt("_ZWrite", 0);
        }

        if (contourTextureOverlayMaterial.HasProperty("_ZTest"))
        {
            contourTextureOverlayMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        contourTextureOverlayMaterial.renderQueue = 2990;
        return contourTextureOverlayMaterial;
    }

    private void ClearContourLineOverlay()
    {
        if (contourLineOverlay != null)
        {
            DestroyRuntimeAsset(contourLineOverlay);
            contourLineOverlay = null;
        }

        if (contourTextureOverlay != null)
        {
            DestroyRuntimeAsset(contourTextureOverlay);
            contourTextureOverlay = null;
        }
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
        ApplySeed((int)GetRandomSeed());
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
        ApplySeed((int)GetRandomSeed());
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

        ApplySeed((int)GetRandomSeed());
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
        float minOutputChannel = 1f;
        float maxOutputChannel = 0f;
        float maxContour = 0f;
        float backdropLineCoverage = 0f;
        float backdropLineIntensity = 0f;
        float backdropLinePixelCount = 0f;
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
                contour *= contourStyle == ContourStyle.ReferenceMatchBackdrop
                    ? Mathf.Lerp(0.72f, 1f, glowFlow)
                    : glowFlow;
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
                pixel.a *= Mathf.Lerp(0.78f, 1f, rim);
                if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
                {
                    float contourSignal = Mathf.Pow(Mathf.Clamp01(contour), 0.72f);
                    float baseLuma = Mathf.Lerp(0.002f, 0.018f, Mathf.Clamp01(h));
                    float detail = Mathf.SmoothStep(0f, 1f, haze);
                    float rimBlend = Mathf.Clamp01(1f - radial * 1.2f);

                    float back = Mathf.Lerp(baseLuma, 0.008f + baseLuma * 0.65f, rimBlend);
                    Color backdropBase = new Color(back, back, back + 0.0012f, 1f);

                    Color lineCore = Color.Lerp(
                        new Color(1f, 1f, 1f, 1f),
                        new Color(contourColor.r, contourColor.g, contourColor.b, 1f),
                        0.08f);
                    float lineMask = Mathf.SmoothStep(0.91f, 0.985f, nearestNormalized);
                    pixel = Color.Lerp(backdropBase, lineCore, lineMask);
                    pixel = Color.Lerp(pixel, Color.white, Mathf.Pow(lineMask, 5f) * 0.55f);

                    float lineGlow = Mathf.Pow(Mathf.Max(glowFlow, lineMask), 1.2f) * (0.16f + rim * 0.22f) * Mathf.Lerp(0.50f, 1f, lineMask);
                    pixel += lineGlow * new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                    pixel = Color.Lerp(pixel, Color.black, detail * 0.035f + (1f - rimBlend) * 0.08f);
                    pixel = new Color(
                        Mathf.Clamp01(pixel.r),
                        Mathf.Clamp01(pixel.g),
                        Mathf.Clamp01(pixel.b),
                        1f);
                    float contourBand = Mathf.Abs(Mathf.Sin((h * contourLayers + contourBias + flowMask * 0.08f) * Mathf.PI));
                    float finalLine = 1f - Mathf.SmoothStep(0.035f, 0.115f, contourBand);
                    pixel = Color.Lerp(
                        new Color(0.001f, 0.0015f, 0.0022f, 1f),
                        Color.white,
                        Mathf.Pow(finalLine, 1.1f));
                    backdropLineCoverage += contourSignal;
                    backdropLineIntensity += lineMask;
                    if (lineMask > 0.12f)
                    {
                        backdropLinePixelCount += 1f;
                    }
                }
                else
                {
                    pixel = Color.Lerp(pixel, pixel * 0.58f, radial * 0.45f);
                }

                if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
                {
                    pixel.a = 1f;
                }

                float sample = (pixel.r + pixel.g + pixel.b) / 3f;
                minOutputChannel = Mathf.Min(minOutputChannel, sample);
                maxOutputChannel = Mathf.Max(maxOutputChannel, sample);
                maxContour = Mathf.Max(maxContour, sample);
                pixels[i] = pixel;
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply(false, false);
        if (contourStyle == ContourStyle.ReferenceMatchBackdrop)
        {
            float pixelCount = resolution * (float)resolution;
            Debug.Log($"[DeskContour] Backdrop line coverage: avgSignal={backdropLineCoverage / pixelCount:F4}, avgMask={backdropLineIntensity / pixelCount:F4}, strongPixelRatio={backdropLinePixelCount / pixelCount:F4}");
        }
        Debug.Log($"[DeskContour] Generated texture stats: minRGB={minOutputChannel:F4}, maxRGB={maxOutputChannel:F4}, maxSample={maxContour:F4}");
        return generatedTexture;
    }

    private void ApplyTexture(Texture2D texture)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
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

        renderer.enabled = true;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        bool forceOpaqueBackdrop = contourStyle == ContourStyle.ReferenceMatchBackdrop;
        Shader shader = forceOpaqueBackdrop
            ? ChooseReferenceBackdropShader(renderer.sharedMaterial)
            : ChooseShader(renderer.sharedMaterial);
        if (shader == null)
        {
            Debug.LogError($"[DeskContour] Shader unavailable, skip texture apply. name={name}, forceOpaque={forceOpaqueBackdrop}");
            return;
        }
        Material sourceMaterial = renderer.sharedMaterial;
        if (sourceMaterial == null || sourceMaterial.shader != shader)
        {
            sourceMaterial = new Material(shader);
        }
        else
        {
            sourceMaterial = new Material(sourceMaterial);
        }

        if (!sourceMaterial.HasProperty(MainTexProperty) && !sourceMaterial.HasProperty(BaseMapProperty) && !sourceMaterial.HasProperty(BaseTexProperty))
        {
            Shader fallback = ChooseBackdropFallbackShader(sourceMaterial.shader);
            if (fallback != null && fallback != sourceMaterial.shader)
            {
                sourceMaterial = new Material(fallback);
            }
        }

        sourceMaterial.name = $"{name}_ContourMap_Material";
        SetMaterialTexture(sourceMaterial, texture);
        sourceMaterial.mainTextureOffset = Vector2.zero;
        sourceMaterial.mainTextureScale = Vector2.one;
        ApplyTransparentMaterialState(sourceMaterial, forceOpaqueBackdrop);
        if (forceOpaqueBackdrop)
        {
            ApplyBackdropOpaqueMaterialState(sourceMaterial);
        }
        if (!HasBackdropDrawableTexture(sourceMaterial, texture))
        {
            Shader fallback = ChooseBackdropFallbackShader(sourceMaterial.shader);
            if (fallback != null)
            {
                sourceMaterial = new Material(fallback);
                sourceMaterial.name = $"{name}_ContourMap_FallbackMaterial";
                SetMaterialTexture(sourceMaterial, texture);
                ApplyTransparentMaterialState(sourceMaterial, forceOpaqueBackdrop);
                if (forceOpaqueBackdrop)
                {
                    ApplyBackdropOpaqueMaterialState(sourceMaterial);
                }
            }
        }
        if (!HasBackdropDrawableTexture(sourceMaterial, texture))
        {
            Shader fallback = Shader.Find("Sprites/Default");
            if (fallback != null)
            {
                sourceMaterial = new Material(fallback);
                sourceMaterial.name = $"{name}_ContourMap_SpriteFallback";
                SetMaterialTexture(sourceMaterial, texture);
                ApplyTransparentMaterialState(sourceMaterial, forceOpaqueBackdrop);
                if (forceOpaqueBackdrop)
                {
                    ApplyBackdropOpaqueMaterialState(sourceMaterial);
                }
            }
            else
            {
                Debug.LogWarning($"[DeskContour] 无法为 {name} 找到可用的回退材质，尝试直接使用当前材质");
            }
        }

        ForceBackdropMaterialVisible(renderer, sourceMaterial, forceOpaqueBackdrop);
        ClearBackdropMaterialKeywordNoise(sourceMaterial, forceOpaqueBackdrop);
        ApplyBackdropForegroundState(sourceMaterial, forceOpaqueBackdrop);

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
        renderer.sharedMaterial.mainTexture = texture;
        renderer.material = sourceMaterial;
        RepairTabletopBackdropMaterialState();
        ApplyBackdropForegroundState(generatedMaterial, forceOpaqueBackdrop);
        ApplyBackdropVisibilityHardening(renderer, generatedMaterial, forceOpaqueBackdrop);
        Debug.Log($"[DeskContour] ApplyTexture finished: {name}, style={contourStyle}, shader={sourceMaterial.shader.name}, queue={sourceMaterial.renderQueue}, texRes={texture.width}x{texture.height}");

        if (forceOpaqueBackdrop)
        {
            float? texAlpha = null;
            if (generatedTexture != null)
            {
                try
                {
                    Color firstPixel = generatedTexture.GetPixel(0, 0);
                    texAlpha = firstPixel.a;
                }
                catch
                {
                }
            }

            bool hasMainTex = generatedMaterial != null && generatedMaterial.HasProperty(MainTexProperty) && generatedMaterial.mainTexture != null;
            bool hasBaseMap = generatedMaterial != null && generatedMaterial.HasProperty(BaseMapProperty) && generatedMaterial.GetTexture(BaseMapProperty) != null;
            bool hasColor = generatedMaterial != null && generatedMaterial.HasProperty(_BaseColorProperty);
            bool hasMainColor = generatedMaterial != null && generatedMaterial.HasProperty(ColorProperty);
            bool hasMode = generatedMaterial != null && generatedMaterial.HasProperty("_Mode");
            float? mode = null;
            if (generatedMaterial != null && hasMode)
            {
                try
                {
                    mode = generatedMaterial.GetFloat("_Mode");
                }
                catch
                {
                }
            }

            Debug.Log($"[DeskContour] Backdrop material check: hasMainTex={hasMainTex}, hasBaseMap={hasBaseMap}, hasColor={hasColor}, hasMainColor={hasMainColor}, hasMode={hasMode}, mode={mode}, firstPixelAlpha={texAlpha}, queue={renderer.sharedMaterial.renderQueue}, cull={renderer.sharedMaterial.HasProperty("_Cull")}, doubleSided={renderer.sharedMaterial.HasProperty("_DoubleSided")}, shader={generatedMaterial.shader?.name}");
        }
    }

    private static void ApplyBackdropForegroundState(Material material, bool forceOpaqueBackdrop)
    {
        if (material == null || !forceOpaqueBackdrop)
        {
            return;
        }

        // 牌桌专用兜底：提高可见优先级并避免被场景深度状态压掉
        material.renderQueue = ReferenceBackdropRenderQueue;

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        if (material.HasProperty("_ZTest"))
        {
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (material.HasProperty("ZTest"))
        {
            material.SetInt("ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("Cull"))
        {
            material.SetInt("Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }
    }

    private static void ApplyTransparentMaterialState(Material material, bool forceOpaqueBackdrop)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", 0);
            }

            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
        }
        else if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetInt("_AlphaClip", 0);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", 1);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", 0);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }

        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void ApplyBackdropOpaqueMaterialState(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 0f);
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 1);
            }
        }
        else if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", 1);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", 0);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 1);
            }

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetInt("_AlphaClip", 0);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("Cull"))
        {
            material.SetInt("Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_DoubleSided"))
        {
            material.SetFloat("_DoubleSided", 1f);
        }

        if (material.HasProperty("_DoubleSidedEnable"))
        {
            material.SetFloat("_DoubleSidedEnable", 1f);
        }

        material.renderQueue = ReferenceBackdropRenderQueue;
    }

    private static void RebindBackdropMaterialTextures(Renderer renderer, Material material, Texture2D texture)
    {
        if (material == null || texture == null)
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

        material.mainTexture = texture;

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static bool HasBackdropDrawableTexture(Material material, Texture2D texture)
    {
        if (material == null)
        {
            return false;
        }

        if (material.mainTexture == texture)
        {
            return true;
        }

        return (material.HasProperty(MainTexProperty) && material.GetTexture(MainTexProperty) == texture)
            || (material.HasProperty(BaseMapProperty) && material.GetTexture(BaseMapProperty) == texture)
            || (material.HasProperty(BaseTexProperty) && material.GetTexture(BaseTexProperty) == texture);
    }

    private static Shader ChooseBackdropFallbackShader(Shader currentShader)
    {
        bool isUrp = IsUniversalRenderPipelineActive();
        if (isUrp)
        {
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit != null)
            {
                return urpUnlit;
            }

            Shader urpUnlitSimple = Shader.Find("Universal Render Pipeline/Unlit (Simple Lit)");
            if (urpUnlitSimple != null)
            {
                return urpUnlitSimple;
            }
        }

        Shader sprites = Shader.Find("Sprites/Default");
        if (sprites != null)
        {
            return sprites;
        }

        Shader unlitTexture = Shader.Find("Unlit/Texture");
        if (unlitTexture != null)
        {
            return unlitTexture;
        }

        Shader unlitTransparent = Shader.Find("Unlit/Transparent");
        if (unlitTransparent != null)
        {
            return unlitTransparent;
        }

        Shader standard = Shader.Find("Standard");
        if (standard != null)
        {
            return standard;
        }

        return currentShader;
    }

    private static void ClearBackdropMaterialKeywordNoise(Material material, bool forceOpaqueBackdrop)
    {
        if (material == null)
        {
            return;
        }

        if (forceOpaqueBackdrop)
        {
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return;
        }

        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
    }

    private static void ForceBackdropMaterialVisible(Renderer renderer, Material material, bool forceOpaqueBackdrop)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.sortingOrder = forceOpaqueBackdrop ? 2000 : 1;

        if (material.HasProperty("_Color"))
        {
            Color color = material.GetColor("_Color");
            material.SetColor("_Color", new Color(color.r, color.g, color.b, 1f));
        }

        if (material.HasProperty(_BaseColorProperty))
        {
            Color baseColor = material.GetColor(_BaseColorProperty);
            material.SetColor(_BaseColorProperty, new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        material.renderQueue = forceOpaqueBackdrop
            ? ReferenceBackdropRenderQueue
            : (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void ApplyBackdropVisibilityHardening(Renderer renderer, Material material, bool forceOpaqueBackdrop)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.sortingOrder = forceOpaqueBackdrop ? 2000 : Mathf.Max(renderer.sortingOrder, 1);

        if (material.HasProperty(_BaseColorProperty))
        {
            Color baseColor = material.GetColor(_BaseColorProperty);
            material.SetColor(_BaseColorProperty, new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
        }

        if (material.HasProperty(ColorProperty))
        {
            Color color = material.GetColor(ColorProperty);
            material.SetColor(ColorProperty, new Color(color.r, color.g, color.b, 1f));
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        if (material.HasProperty("_ZTest"))
        {
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (material.HasProperty("ZTest"))
        {
            material.SetInt("ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("Cull"))
        {
            material.SetInt("Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        material.renderQueue = forceOpaqueBackdrop
            ? ReferenceBackdropRenderQueue
            : (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void NormalizeBackdropMaterialRenderState(Renderer renderer, Material material, bool forceOpaqueBackdrop)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;

        if (material.HasProperty("_MainTex"))
        {
            material.SetTextureOffset("_MainTex", Vector2.zero);
            material.SetTextureScale("_MainTex", Vector2.one);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetTextureScale("_BaseMap", Vector2.one);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_DoubleSided"))
        {
            material.SetFloat("_DoubleSided", 1f);
        }

        if (material.HasProperty("_DoubleSidedEnable"))
        {
            material.SetFloat("_DoubleSidedEnable", 1f);
        }

        if (material.HasProperty("Cull"))
        {
            material.SetInt("Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
        }

        material.renderQueue = forceOpaqueBackdrop
            ? ReferenceBackdropRenderQueue
            : (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void RepairTabletopBackdropMaterialState()
    {
        RepairTabletopBackdropTransform();
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return;
        }

        bool forceOpaqueBackdrop = contourStyle == ContourStyle.ReferenceMatchBackdrop;
        if (forceOpaqueBackdrop)
        {
            ApplyBackdropOpaqueMaterialState(renderer.sharedMaterial);
        }
        else
        {
            ApplyTransparentMaterialState(renderer.sharedMaterial, forceOpaqueBackdrop);
        }
        NormalizeBackdropMaterialRenderState(renderer, renderer.sharedMaterial, forceOpaqueBackdrop);
        ClearBackdropMaterialKeywordNoise(renderer.sharedMaterial, forceOpaqueBackdrop);
        if (generatedTexture != null)
        {
            RebindBackdropMaterialTextures(renderer, renderer.sharedMaterial, generatedTexture);
            ForceBackdropMaterialVisible(renderer, renderer.sharedMaterial, forceOpaqueBackdrop);
        }

        ApplyBackdropForegroundState(renderer.sharedMaterial, forceOpaqueBackdrop);
        ApplyBackdropVisibilityHardening(renderer, renderer.sharedMaterial, forceOpaqueBackdrop);
    }

    private void RepairTabletopBackdropTransform()
    {
        if (gameObject == null)
        {
            return;
        }

        gameObject.SetActive(true);
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            Mathf.Max(0.01f, Mathf.Abs(scale.y)),
            Mathf.Max(0.01f, Mathf.Abs(scale.z)));

        if (string.Equals(gameObject.name, "DesktopQuad", StringComparison.Ordinal))
        {
            Vector3 angles = transform.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(angles.x, 90f)) > 1f
                || Mathf.Abs(Mathf.DeltaAngle(angles.y, 0f)) > 1f
                || Mathf.Abs(Mathf.DeltaAngle(angles.z, 0f)) > 1f)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

    private static Shader ChooseReferenceBackdropShader(Material currentMaterial)
    {
        bool isUrp = IsUniversalRenderPipelineActive();
        Shader urpUnlit = isUrp ? Shader.Find("Universal Render Pipeline/Unlit") : null;
        if (urpUnlit != null)
        {
            return urpUnlit;
        }

        Shader sprites = Shader.Find("Sprites/Default");
        if (sprites != null)
        {
            return sprites;
        }

        Shader unlitTexture = Shader.Find("Unlit/Texture");
        if (unlitTexture != null)
        {
            return unlitTexture;
        }

        Shader unlitTransparent = Shader.Find("Unlit/Transparent");
        if (unlitTransparent != null)
        {
            return unlitTransparent;
        }

        if (currentMaterial != null && currentMaterial.shader != null)
        {
            string name = currentMaterial.shader.name;
            if (name.Contains("Sprites/Default", StringComparison.Ordinal) ||
                name.Contains("Unlit/Texture", StringComparison.Ordinal) ||
                name.Contains("Universal Render Pipeline/Unlit", StringComparison.Ordinal))
            {
                return currentMaterial.shader;
            }
        }

        return ChooseBackdropShader(currentMaterial);
    }

    private static Shader ChooseBackdropShader(Material currentMaterial)
    {
        bool isUrp = IsUniversalRenderPipelineActive();
        if (currentMaterial != null && currentMaterial.shader != null && currentMaterial.shader.name.Contains("Universal Render Pipeline/Unlit", StringComparison.Ordinal))
        {
            return isUrp ? currentMaterial.shader : null;
        }

        if (currentMaterial != null && currentMaterial.shader != null && currentMaterial.shader.name.Contains("Sprites/Default", StringComparison.Ordinal))
        {
            return currentMaterial.shader;
        }

        Shader current = currentMaterial != null ? currentMaterial.shader : null;
        if (current != null)
        {
            string name = current.name;
            if (isUrp && (name.Contains("URP/", StringComparison.Ordinal) || name.Contains("Universal Render Pipeline/")))
            {
                return current;
            }

            if (name.Contains("Sprites/Default"))
            {
                return current;
            }
        }

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit != null)
        {
            return urpUnlit;
        }

        Shader urpUnlitSimple = isUrp ? Shader.Find("Universal Render Pipeline/Unlit (Simple Lit)") : null;
        if (urpUnlitSimple != null)
        {
            return urpUnlitSimple;
        }

        Shader unlitTexture = Shader.Find("Unlit/Texture");
        if (unlitTexture != null)
        {
            return unlitTexture;
        }

        Shader unlitTransparent = Shader.Find("Unlit/Transparent");
        if (unlitTransparent != null)
        {
            return unlitTransparent;
        }

        Shader sprites = Shader.Find("Sprites/Default");
        if (sprites != null)
        {
            return sprites;
        }

        Shader standard = Shader.Find("Standard");
        if (standard != null)
        {
            return standard;
        }

        return ChooseShader(currentMaterial);
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

        if (material.HasProperty(BaseTexProperty))
        {
            material.SetTexture(BaseTexProperty, texture);
        }

        material.mainTexture = texture;
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

    private static void DestroyRuntimeAsset(UnityEngine.Object obj)
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
