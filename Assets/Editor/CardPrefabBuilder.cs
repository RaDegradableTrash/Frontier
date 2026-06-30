using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CardPrefabBuilder
{
    private const string PrefabDirectory = "Assets/Resources/CardPrefabs";
    private const string MaterialDirectory = "Assets/Materials/CardPrefabGenerated";
    private const string FontDirectory = "Assets/Resources/Fonts";
    private const string FontAssetPath = FontDirectory + "/FrontierChineseTMP.asset";
    private const string SourceFontAssetPath = FontDirectory + "/ArialUnicode.ttf";
    private const float CardWidth = 0.96f;
    private const float CardHeight = 1.30f;

    static CardPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabsExist;
    }

    [MenuItem("Frontier/Cards/Rebuild Card Prefabs")]
    public static void RebuildCardPrefabs()
    {
        Directory.CreateDirectory(PrefabDirectory);
        Directory.CreateDirectory(MaterialDirectory);
        BuildUnitHandPrefab();
        BuildUnitBoardPrefab();
        BuildOrderHandPrefab();
        BuildCounterHandPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsurePrefabsExist()
    {
        if (!Directory.Exists(MaterialDirectory))
        {
            RebuildCardPrefabs();
            return;
        }

        if (File.Exists($"{PrefabDirectory}/UnitCard_Hand.prefab")
            && File.Exists($"{PrefabDirectory}/UnitCard_Board.prefab")
            && File.Exists($"{PrefabDirectory}/OrderCard_Hand.prefab")
            && File.Exists($"{PrefabDirectory}/CounterCard_Hand.prefab"))
        {
            return;
        }

        RebuildCardPrefabs();
    }

    private static void BuildUnitHandPrefab()
    {
        GameObject root = CreateCardRoot("UnitCard_Hand", 0.032f, Palette.UnitPaper);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandCardLayout(root.transform, template, "UNIT", true, Palette.UnitArt, Palette.UnitFrame);
        Save(root, "UnitCard_Hand");
    }

    private static void BuildUnitBoardPrefab()
    {
        GameObject root = CreateCardRoot("UnitCard_Board", 0.050f, Palette.BoardPaper);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddBoardUnitLayout(root.transform, template);
        Save(root, "UnitCard_Board");
    }

    private static void BuildOrderHandPrefab()
    {
        GameObject root = CreateCardRoot("OrderCard_Hand", 0.032f, Palette.OrderPaper);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandCardLayout(root.transform, template, "ORDER", false, Palette.OrderArt, Palette.OrderFrame);
        Save(root, "OrderCard_Hand");
    }

    private static void BuildCounterHandPrefab()
    {
        GameObject root = CreateCardRoot("CounterCard_Hand", 0.032f, Palette.CounterPaper);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandCardLayout(root.transform, template, "COUNTER", false, Palette.CounterArt, Palette.CounterFrame);
        Save(root, "CounterCard_Hand");
    }

    private static GameObject CreateCardRoot(string name, float thickness, Color paperColor)
    {
        GameObject root = new GameObject(name);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.AddComponent<CardPrefabTemplate>();

        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        Transform cardBody = CreateGroup(root.transform, "CardBody");
        Transform interactionGroup = CreateGroup(root.transform, "Interaction");

        template.faceRenderer = AddPanel(cardBody, "Face", 0f, 0f, CardWidth, CardHeight, 0.000f, thickness, paperColor);
        AddPanel(cardBody, "OuterBorder", 0f, 0f, CardWidth * 1.03f, CardHeight * 1.03f, -0.006f, 0.012f, Palette.DarkEdge);

        Transform dragShadow = CreateGroup(interactionGroup, "DragShadow");
        template.dragShadowRenderer = AddPanel(dragShadow, "Shadow", 0.035f, -0.035f, CardWidth * 0.98f, CardHeight * 0.98f, -0.020f, 0.010f, Palette.Shadow);
        template.dragShadowRenderer.enabled = false;

        Transform hoverFrame = CreateGroup(interactionGroup, "HoverFrame");
        template.selectionRenderer = AddPanel(hoverFrame, "WhiteFrame", 0f, 0f, CardWidth * 1.05f, CardHeight * 1.05f, 0.080f, 0.010f, Color.white);
        template.selectionRenderer.enabled = false;

        Transform selectionFrame = CreateGroup(interactionGroup, "SelectionFrame");
        template.selectionFrameRenderers = CreateFrame(selectionFrame, "Edge", Palette.WhiteFrame);
        return root;
    }

    private static void AddHandCardLayout(Transform root, CardPrefabTemplate template, string previewTitle, bool hasUnitStats, Color artColor, Color frameColor)
    {
        Transform cardBody = GetOrCreateGroup(root, "CardBody");
        Transform header = CreateGroup(root, "Header");
        Transform artwork = CreateGroup(root, "Artwork");
        Transform rules = CreateGroup(root, "Rules");
        Transform interaction = GetOrCreateGroup(root, "Interaction");

        AddPanel(cardBody, "InnerBorder", 0f, 0f, CardWidth * 0.90f, CardHeight * 0.91f, 0.018f, 0.010f, frameColor);
        AddPanel(cardBody, "PaperInset", 0f, -0.010f, CardWidth * 0.84f, CardHeight * 0.83f, 0.024f, 0.008f, Palette.PaperInset);

        Transform costBadge = CreateGroup(header, "CostBadge");
        template.costBadgeRenderer = AddPanel(costBadge, "Backing", -CardWidth * 0.365f, CardHeight * 0.405f, 0.155f, 0.155f, 0.050f, 0.020f, Palette.CostBadge);
        template.costLabel = CreateText(costBadge, "CostNumber", "5", -CardWidth * 0.365f, CardHeight * 0.405f, 0.010f, TextAlignmentOptions.Center, Palette.NumberText);

        Transform titlePlate = CreateGroup(header, "TitlePlate");
        AddPanel(titlePlate, "Backing", CardWidth * 0.055f, CardHeight * 0.405f, CardWidth * 0.610f, 0.135f, 0.045f, 0.012f, Palette.TitlePlate);
        template.titleLabel = CreateText(titlePlate, "TitleLabel", previewTitle, CardWidth * 0.055f, CardHeight * 0.405f, 0.0042f, TextAlignmentOptions.Center, Palette.TitleText);

        Transform flagPlate = CreateGroup(header, "FlagPlate");
        AddPanel(flagPlate, "Backing", CardWidth * 0.380f, CardHeight * 0.405f, 0.135f, 0.135f, 0.046f, 0.012f, frameColor);

        Transform artFrame = CreateGroup(artwork, "ArtFrame");
        AddPanel(artFrame, "Frame", 0f, CardHeight * 0.105f, CardWidth * 0.805f, CardHeight * 0.490f, 0.038f, 0.012f, Palette.ArtFrame);
        Transform artPanel = CreateGroup(artwork, "ArtPanel");
        AddPanel(artPanel, "Image", 0f, CardHeight * 0.105f, CardWidth * 0.745f, CardHeight * 0.430f, 0.052f, 0.010f, artColor);
        AddArtStripes(artPanel, artColor);

        Transform rulesPlate = CreateGroup(rules, "RulesPlate");
        AddPanel(rulesPlate, "Backing", 0f, -CardHeight * 0.285f, CardWidth * 0.805f, CardHeight * 0.205f, 0.040f, 0.012f, Palette.RulesPlate);
        template.statusLabel = CreateText(rulesPlate, "RulesLabel", hasUnitStats ? "KEYWORD / EFFECT" : "EFFECT", 0f, -CardHeight * 0.285f, 0.0034f, TextAlignmentOptions.Center, Palette.RulesText);

        Transform rarityBand = CreateGroup(cardBody, "RarityBand");
        template.rarityBandRenderer = AddPanel(rarityBand, "Backing", 0f, -CardHeight * 0.475f, CardWidth * 0.135f, 0.030f, 0.052f, 0.010f, Palette.RarityGold);

        template.selectionLabel = CreateText(interaction, "SelectionLabel", string.Empty, 0f, 0f, 0.012f, TextAlignmentOptions.Center, Palette.RulesText);

        if (!hasUnitStats)
        {
            return;
        }

        Transform stats = CreateGroup(root, "Stats");
        AddStatCluster(stats, template, CardHeight * -0.195f, 0.052f);
    }

    private static void AddBoardUnitLayout(Transform root, CardPrefabTemplate template)
    {
        Transform cardBody = GetOrCreateGroup(root, "CardBody");
        Transform header = CreateGroup(root, "Header");
        Transform artwork = CreateGroup(root, "Artwork");
        Transform stats = CreateGroup(root, "Stats");
        Transform interaction = GetOrCreateGroup(root, "Interaction");

        AddPanel(cardBody, "InnerBorder", 0f, 0f, CardWidth * 0.88f, CardHeight * 0.90f, 0.020f, 0.012f, Palette.BoardFrame);

        Transform artPanel = CreateGroup(artwork, "ArtPanel");
        AddPanel(artPanel, "Image", 0f, CardHeight * 0.080f, CardWidth * 0.760f, CardHeight * 0.600f, 0.045f, 0.014f, Palette.BoardArt);

        Transform titlePlate = CreateGroup(header, "TitlePlate");
        AddPanel(titlePlate, "Backing", 0f, CardHeight * 0.425f, CardWidth * 0.660f, 0.120f, 0.055f, 0.012f, Palette.BoardTitle);
        template.titleLabel = CreateText(titlePlate, "TitleLabel", "UNIT", 0f, CardHeight * 0.425f, 0.0042f, TextAlignmentOptions.Center, Palette.BoardTitleText);

        Transform costBadge = CreateGroup(header, "CostBadge");
        template.costBadgeRenderer = AddPanel(costBadge, "Backing", -CardWidth * 0.370f, CardHeight * 0.425f, 0.150f, 0.150f, 0.060f, 0.020f, Palette.CostBadge);
        template.costLabel = CreateText(costBadge, "CostNumber", "5", -CardWidth * 0.370f, CardHeight * 0.425f, 0.010f, TextAlignmentOptions.Center, Palette.NumberText);

        Transform rarityBand = CreateGroup(cardBody, "RarityBand");
        template.rarityBandRenderer = AddPanel(rarityBand, "Backing", 0f, -CardHeight * 0.485f, CardWidth * 0.115f, 0.028f, 0.060f, 0.010f, Palette.RarityGold);

        template.selectionLabel = CreateText(interaction, "SelectionLabel", string.Empty, 0f, 0f, 0.012f, TextAlignmentOptions.Center, Palette.RulesText);
        AddStatCluster(stats, template, CardHeight * -0.380f, 0.060f);
    }

    private static void AddStatCluster(Transform statsGroup, CardPrefabTemplate template, float z, float y)
    {
        Transform attackBadge = CreateGroup(statsGroup, "AttackBadge");
        AddPanel(attackBadge, "Backing", -CardWidth * 0.285f, z, 0.165f, 0.165f, y, 0.022f, Palette.AttackBadge);
        template.attackLabel = CreateText(attackBadge, "AttackNumber", "3", -CardWidth * 0.285f, z, 0.010f, TextAlignmentOptions.Center, Palette.NumberText);

        Transform operationBadge = CreateGroup(statsGroup, "OperationBadge");
        template.operationBadgeRenderer = AddPanel(operationBadge, "Backing", 0f, z, 0.170f, 0.165f, y + 0.001f, 0.022f, Palette.OperationBadge);
        template.operationLabel = CreateText(operationBadge, "OperationNumber", "2", 0f, z, 0.009f, TextAlignmentOptions.Center, Palette.NumberText);

        Transform defenseBadge = CreateGroup(statsGroup, "DefenseBadge");
        AddPanel(defenseBadge, "Backing", CardWidth * 0.285f, z, 0.165f, 0.165f, y, 0.022f, Palette.DefenseBadge);
        template.defenseLabel = CreateText(defenseBadge, "DefenseNumber", "4", CardWidth * 0.285f, z, 0.010f, TextAlignmentOptions.Center, Palette.NumberText);
    }

    private static void AddArtStripes(Transform root, Color baseColor)
    {
        Color stripe = Color.Lerp(baseColor, Color.white, 0.18f);
        AddPanel(root, "ArtStripeTop", 0f, CardHeight * 0.185f, CardWidth * 0.660f, 0.022f, 0.058f, 0.006f, stripe);
        AddPanel(root, "ArtStripeMid", 0f, CardHeight * 0.080f, CardWidth * 0.590f, 0.018f, 0.059f, 0.006f, Color.Lerp(baseColor, Color.black, 0.10f));
        AddPanel(root, "ArtStripeBottom", 0f, -CardHeight * 0.020f, CardWidth * 0.520f, 0.018f, 0.060f, 0.006f, stripe);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static Transform GetOrCreateGroup(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        return existing != null ? existing : CreateGroup(parent, name);
    }

    private static MeshRenderer[] CreateFrame(Transform parent, string prefix, Color color)
    {
        MeshRenderer top = AddPanel(parent, $"{prefix}_Top", 0f, CardHeight * 0.510f, CardWidth * 1.035f, 0.020f, 0.090f, 0.010f, color);
        MeshRenderer bottom = AddPanel(parent, $"{prefix}_Bottom", 0f, -CardHeight * 0.510f, CardWidth * 1.035f, 0.020f, 0.090f, 0.010f, color);
        MeshRenderer left = AddPanel(parent, $"{prefix}_Left", -CardWidth * 0.510f, 0f, 0.020f, CardHeight * 1.035f, 0.090f, 0.010f, color);
        MeshRenderer right = AddPanel(parent, $"{prefix}_Right", CardWidth * 0.510f, 0f, 0.020f, CardHeight * 1.035f, 0.090f, 0.010f, color);
        MeshRenderer[] frame = { top, bottom, left, right };
        foreach (MeshRenderer renderer in frame)
        {
            renderer.enabled = false;
        }

        return frame;
    }

    private static MeshRenderer AddPanel(Transform parent, string name, float x, float z, float width, float height, float y, float thickness, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(x, y, z);
        obj.transform.localScale = new Vector3(width, thickness, height);
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(name, color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static TMP_Text CreateText(Transform parent, string name, string previewText, float x, float z, float characterSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(x, 0.125f, z);
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TextMeshPro text = obj.AddComponent<TextMeshPro>();
        text.text = previewText;
        text.alignment = alignment;
        text.fontSize = characterSize * 150f;
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static TMP_FontAsset ResolveChineseFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            existing.isMultiAtlasTexturesEnabled = true;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Directory.CreateDirectory(FontDirectory);
        EnsureProjectChineseFontFile();
        AssetDatabase.ImportAsset(SourceFontAssetPath, ImportAssetOptions.ForceSynchronousImport);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontAssetPath);
        if (sourceFont == null)
        {
            sourceFont = CreateChineseFont();
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "FrontierChineseTMP";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        return fontAsset;
    }

    private static void EnsureProjectChineseFontFile()
    {
        if (File.Exists(SourceFontAssetPath))
        {
            return;
        }

        string[] sourcePaths =
        {
            "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
            "/Library/Fonts/Arial Unicode.ttf"
        };

        for (int i = 0; i < sourcePaths.Length; i++)
        {
            if (File.Exists(sourcePaths[i]))
            {
                File.Copy(sourcePaths[i], SourceFontAssetPath);
                return;
            }
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
            if (File.Exists(fontPaths[i]))
            {
                return new Font(fontPaths[i]);
            }
        }

        return Font.CreateDynamicFontFromOSFont(
            new[] { "PingFang SC", "Heiti SC", "Songti SC", "Arial Unicode MS" },
            90);
    }

    private static Material MaterialFor(string name, Color color)
    {
        string colorKey = ColorUtility.ToHtmlStringRGBA(color);
        string materialName = $"{SanitizeFileName(name)}_{colorKey}";
        string path = $"{MaterialDirectory}/{materialName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Directory.CreateDirectory(MaterialDirectory);
        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private static void Save(GameObject root, string fileName)
    {
        string path = $"{PrefabDirectory}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static class Palette
    {
        public static readonly Color UnitPaper = new Color(0.78f, 0.72f, 0.56f);
        public static readonly Color OrderPaper = new Color(0.58f, 0.54f, 0.70f);
        public static readonly Color CounterPaper = new Color(0.56f, 0.39f, 0.58f);
        public static readonly Color BoardPaper = new Color(0.70f, 0.58f, 0.36f);
        public static readonly Color PaperInset = new Color(0.88f, 0.84f, 0.72f);
        public static readonly Color UnitFrame = new Color(0.18f, 0.19f, 0.18f);
        public static readonly Color OrderFrame = new Color(0.22f, 0.20f, 0.32f);
        public static readonly Color CounterFrame = new Color(0.30f, 0.18f, 0.31f);
        public static readonly Color BoardFrame = new Color(0.48f, 0.31f, 0.12f);
        public static readonly Color UnitArt = new Color(0.58f, 0.63f, 0.56f);
        public static readonly Color OrderArt = new Color(0.42f, 0.43f, 0.62f);
        public static readonly Color CounterArt = new Color(0.48f, 0.30f, 0.55f);
        public static readonly Color BoardArt = new Color(0.56f, 0.60f, 0.52f);
        public static readonly Color ArtFrame = new Color(0.83f, 0.70f, 0.36f);
        public static readonly Color RulesPlate = new Color(0.86f, 0.80f, 0.66f);
        public static readonly Color TitlePlate = new Color(0.78f, 0.69f, 0.46f);
        public static readonly Color BoardTitle = new Color(0.60f, 0.42f, 0.20f);
        public static readonly Color CostBadge = new Color(0.08f, 0.075f, 0.045f);
        public static readonly Color AttackBadge = new Color(0.55f, 0.08f, 0.06f);
        public static readonly Color OperationBadge = new Color(0.10f, 0.10f, 0.085f);
        public static readonly Color DefenseBadge = new Color(0.06f, 0.18f, 0.50f);
        public static readonly Color RarityGold = new Color(0.94f, 0.66f, 0.20f);
        public static readonly Color DarkEdge = new Color(0.04f, 0.035f, 0.030f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color WhiteFrame = new Color(1f, 1f, 1f, 0.92f);
        public static readonly Color TitleText = new Color(0.08f, 0.06f, 0.035f);
        public static readonly Color BoardTitleText = new Color(1f, 0.92f, 0.62f);
        public static readonly Color RulesText = new Color(0.08f, 0.055f, 0.035f);
        public static readonly Color NumberText = new Color(1f, 0.88f, 0.42f);
    }
}
