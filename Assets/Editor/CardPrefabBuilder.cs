using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CardPrefabBuilder
{
    private const string PrefabDirectory = "Assets/Resources/CardPrefabs";
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
        BuildUnitHandPrefab();
        BuildUnitBoardPrefab();
        BuildOrderHandPrefab();
        BuildCounterHandPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsurePrefabsExist()
    {
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
        GameObject root = CreateCardRoot("UnitCard_Hand", 0.030f);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandLayout(root.transform, template, true, new Color(0.76f, 0.72f, 0.58f), new Color(0.16f, 0.20f, 0.22f));
        Save(root, "UnitCard_Hand");
    }

    private static void BuildUnitBoardPrefab()
    {
        GameObject root = CreateCardRoot("UnitCard_Board", 0.050f);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddBoardLayout(root.transform, template);
        Save(root, "UnitCard_Board");
    }

    private static void BuildOrderHandPrefab()
    {
        GameObject root = CreateCardRoot("OrderCard_Hand", 0.030f);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandLayout(root.transform, template, false, new Color(0.58f, 0.53f, 0.70f), new Color(0.18f, 0.17f, 0.28f));
        Save(root, "OrderCard_Hand");
    }

    private static void BuildCounterHandPrefab()
    {
        GameObject root = CreateCardRoot("CounterCard_Hand", 0.030f);
        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        AddHandLayout(root.transform, template, false, new Color(0.55f, 0.34f, 0.56f), new Color(0.24f, 0.14f, 0.25f));
        Save(root, "CounterCard_Hand");
    }

    private static GameObject CreateCardRoot(string name, float thickness)
    {
        GameObject root = new GameObject(name);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.AddComponent<CardPrefabTemplate>();

        CardPrefabTemplate template = root.GetComponent<CardPrefabTemplate>();
        template.faceRenderer = CreateCube(root.transform, "Face", Vector3.zero, new Vector3(CardWidth, thickness, CardHeight), new Color(0.20f, 0.21f, 0.18f));
        template.dragShadowRenderer = CreateCube(root.transform, "DragShadow", new Vector3(0.045f, -0.020f, -0.045f), new Vector3(CardWidth * 0.98f, 0.010f, CardHeight * 0.98f), new Color(0f, 0f, 0f, 0.55f));
        template.dragShadowRenderer.enabled = false;
        template.selectionRenderer = CreateCube(root.transform, "HoverWhiteFrame", new Vector3(0f, 0.055f, 0f), new Vector3(CardWidth * 1.03f, 0.010f, CardHeight * 1.03f), Color.white);
        template.selectionRenderer.enabled = false;
        template.selectionFrameRenderers = CreateFrame(root.transform, "SelectionFrame", new Color(1f, 1f, 1f, 0.92f));
        return root;
    }

    private static void AddHandLayout(Transform root, CardPrefabTemplate template, bool hasUnitStats, Color artColor, Color frameColor)
    {
        CreateCube(root, "OuterFrame", new Vector3(0f, 0.045f, 0f), new Vector3(CardWidth * 0.94f, 0.012f, CardHeight * 0.94f), frameColor);
        CreateCube(root, "TitlePlate", new Vector3(0.08f, 0.070f, CardHeight * 0.40f), new Vector3(CardWidth * 0.70f, 0.012f, 0.16f), new Color(0.86f, 0.80f, 0.62f));
        CreateCube(root, "ArtFrame", new Vector3(0f, 0.065f, CardHeight * 0.12f), new Vector3(CardWidth * 0.82f, 0.014f, CardHeight * 0.54f), new Color(0.94f, 0.86f, 0.55f));
        CreateCube(root, "ArtPanel", new Vector3(0f, 0.078f, CardHeight * 0.12f), new Vector3(CardWidth * 0.76f, 0.012f, CardHeight * 0.46f), artColor);
        CreateCube(root, "RulesPlate", new Vector3(0f, 0.065f, -CardHeight * 0.25f), new Vector3(CardWidth * 0.82f, 0.012f, CardHeight * 0.26f), new Color(0.88f, 0.84f, 0.72f));

        template.costBadgeRenderer = CreateCube(root, "CostBadge", new Vector3(-CardWidth * 0.38f, 0.086f, CardHeight * 0.41f), new Vector3(0.22f, 0.026f, 0.22f), new Color(0.08f, 0.075f, 0.045f));
        template.operationBadgeRenderer = hasUnitStats ? CreateCube(root, "OperationBadge", new Vector3(0f, 0.086f, -CardHeight * 0.39f), new Vector3(0.22f, 0.026f, 0.22f), new Color(0.10f, 0.10f, 0.085f)) : null;
        if (hasUnitStats)
        {
            CreateCube(root, "AttackBadge", new Vector3(-CardWidth * 0.29f, 0.086f, -CardHeight * 0.39f), new Vector3(0.23f, 0.026f, 0.23f), new Color(0.62f, 0.10f, 0.08f));
            CreateCube(root, "DefenseBadge", new Vector3(CardWidth * 0.29f, 0.086f, -CardHeight * 0.39f), new Vector3(0.23f, 0.026f, 0.23f), new Color(0.08f, 0.22f, 0.62f));
        }

        template.rarityBandRenderer = CreateCube(root, "RarityBand", new Vector3(0f, 0.090f, CardHeight * 0.495f), new Vector3(CardWidth * 0.85f, 0.012f, 0.035f), new Color(0.92f, 0.63f, 0.18f));
        template.titleLabel = CreateText(root, "TitleLabel", new Vector3(0.06f, 0.102f, CardHeight * 0.40f), 0.034f, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.035f));
        template.costLabel = CreateText(root, "CostNumber", new Vector3(-CardWidth * 0.38f, 0.110f, CardHeight * 0.41f), 0.090f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        template.operationLabel = CreateText(root, "OperationNumber", new Vector3(0f, 0.110f, -CardHeight * 0.39f), 0.072f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.attackLabel = CreateText(root, "AttackNumber", new Vector3(-CardWidth * 0.29f, 0.110f, -CardHeight * 0.39f), 0.082f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.defenseLabel = CreateText(root, "DefenseNumber", new Vector3(CardWidth * 0.29f, 0.110f, -CardHeight * 0.39f), 0.082f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.statusLabel = CreateText(root, "RulesLabel", new Vector3(0f, 0.104f, -CardHeight * 0.23f), 0.017f, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.035f));
        template.selectionLabel = CreateText(root, "SelectionLabel", new Vector3(0f, 0.118f, 0f), 0.014f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.015f));
    }

    private static void AddBoardLayout(Transform root, CardPrefabTemplate template)
    {
        CreateCube(root, "ArtPanel", new Vector3(0f, 0.075f, CardHeight * 0.11f), new Vector3(CardWidth * 0.76f, 0.014f, CardHeight * 0.62f), new Color(0.48f, 0.52f, 0.44f));
        template.costBadgeRenderer = CreateCube(root, "CostBadge", new Vector3(-CardWidth * 0.39f, 0.088f, CardHeight * 0.43f), new Vector3(0.20f, 0.026f, 0.20f), new Color(0.08f, 0.075f, 0.045f));
        template.operationBadgeRenderer = CreateCube(root, "OperationBadge", new Vector3(0f, 0.088f, -CardHeight * 0.42f), new Vector3(0.22f, 0.026f, 0.22f), new Color(0.10f, 0.10f, 0.085f));
        CreateCube(root, "AttackBadge", new Vector3(-CardWidth * 0.30f, 0.088f, -CardHeight * 0.42f), new Vector3(0.23f, 0.026f, 0.23f), new Color(0.62f, 0.10f, 0.08f));
        CreateCube(root, "DefenseBadge", new Vector3(CardWidth * 0.30f, 0.088f, -CardHeight * 0.42f), new Vector3(0.23f, 0.026f, 0.23f), new Color(0.08f, 0.22f, 0.62f));
        template.rarityBandRenderer = CreateCube(root, "RarityBand", new Vector3(0f, 0.090f, CardHeight * 0.50f), new Vector3(CardWidth * 0.82f, 0.012f, 0.032f), new Color(0.92f, 0.63f, 0.18f));
        template.titleLabel = CreateText(root, "TitleLabel", new Vector3(0f, 0.108f, CardHeight * 0.43f), 0.024f, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.62f));
        template.costLabel = CreateText(root, "CostNumber", new Vector3(-CardWidth * 0.39f, 0.112f, CardHeight * 0.43f), 0.070f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        template.operationLabel = CreateText(root, "OperationNumber", new Vector3(0f, 0.112f, -CardHeight * 0.42f), 0.064f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.attackLabel = CreateText(root, "AttackNumber", new Vector3(-CardWidth * 0.30f, 0.112f, -CardHeight * 0.42f), 0.074f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.defenseLabel = CreateText(root, "DefenseNumber", new Vector3(CardWidth * 0.30f, 0.112f, -CardHeight * 0.42f), 0.074f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        template.selectionLabel = CreateText(root, "SelectionLabel", new Vector3(0f, 0.118f, 0f), 0.014f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.015f));
    }

    private static MeshRenderer[] CreateFrame(Transform parent, string prefix, Color color)
    {
        MeshRenderer top = CreateCube(parent, $"{prefix}_Top", new Vector3(0f, 0.092f, CardHeight * 0.505f), new Vector3(CardWidth * 1.03f, 0.012f, 0.018f), color);
        MeshRenderer bottom = CreateCube(parent, $"{prefix}_Bottom", new Vector3(0f, 0.092f, -CardHeight * 0.505f), new Vector3(CardWidth * 1.03f, 0.012f, 0.018f), color);
        MeshRenderer left = CreateCube(parent, $"{prefix}_Left", new Vector3(-CardWidth * 0.505f, 0.092f, 0f), new Vector3(0.018f, 0.012f, CardHeight * 1.03f), color);
        MeshRenderer right = CreateCube(parent, $"{prefix}_Right", new Vector3(CardWidth * 0.505f, 0.092f, 0f), new Vector3(0.018f, 0.012f, CardHeight * 1.03f), color);
        MeshRenderer[] frame = { top, bottom, left, right };
        foreach (MeshRenderer renderer in frame)
        {
            renderer.enabled = false;
        }

        return frame;
    }

    private static MeshRenderer CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(color);
        return renderer;
    }

    private static TextMesh CreateText(Transform parent, string name, Vector3 position, float characterSize, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TextMesh text = obj.AddComponent<TextMesh>();
        text.text = name;
        text.anchor = anchor;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;
        text.fontSize = 384;
        text.color = color;
        return text;
    }

    private static Material MaterialFor(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    private static void Save(GameObject root, string fileName)
    {
        string path = $"{PrefabDirectory}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
