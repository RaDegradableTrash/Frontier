using UnityEngine;

[ExecuteAlways]
public class EditorScenePreview : MonoBehaviour
{
    private const string PreviewRootName = "Editor Play Preview";

    [SerializeField] private bool showPreview = PlayableSceneRules.EditorPreviewEnabled;

    private Transform previewRoot;
    private bool previewBuilt;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
    }

    private void Update()
    {
        if (!Application.isPlaying && showPreview && (!previewBuilt || previewRoot == null || PreviewChildCountMismatch()))
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (Application.isPlaying)
        {
            ClearPreview();
            return;
        }

        if (!showPreview)
        {
            ClearPreview();
            return;
        }

        EnsureRoot();
        ClearPreviewChildren();
        CreatePreviewHands();
        CreatePreviewStatus();
        ApplyTabletopContourBackground();
        SetPreviewCommandButtons();
        previewBuilt = true;
    }

    private void EnsureRoot()
    {
        Transform existing = transform.Find(PreviewRootName);
        if (existing != null)
        {
            previewRoot = existing;
            return;
        }

        GameObject rootObject = new GameObject(PreviewRootName);
        rootObject.transform.SetParent(transform, false);
        previewRoot = rootObject.transform;
    }

    private void ClearPreview()
    {
        Transform existing = transform.Find(PreviewRootName);
        if (existing == null)
        {
            return;
        }

        DestroyImmediate(existing.gameObject);
        previewRoot = null;
        previewBuilt = false;
    }

    private void ClearPreviewChildren()
    {
        for (int i = previewRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(previewRoot.GetChild(i).gameObject);
        }
        previewBuilt = false;
    }

    private void CreatePreviewHands()
    {
        for (int i = 0; i < PlayableSceneRules.PreviewEnemyHandSize; i++)
        {
            CreatePreviewCard($"Enemy Preview {i + 1}", HandPosition(PlayerSide.Enemy, i, PlayableSceneRules.PreviewEnemyHandSize), true, Color.black);
        }

        Color[] playerColors =
        {
            new Color(0.45f, 0.28f, 0.7f),
            new Color(0.24f, 0.48f, 0.72f),
            new Color(0.68f, 0.62f, 0.38f),
            new Color(0.52f, 0.56f, 0.62f),
            new Color(0.32f, 0.56f, 0.48f)
        };

        for (int i = 0; i < PlayableSceneRules.PreviewPlayerHandSize; i++)
        {
            CreatePreviewCard($"Player Preview {i + 1}", HandPosition(PlayerSide.Player, i, PlayableSceneRules.PreviewPlayerHandSize), false, playerColors[i % playerColors.Length]);
        }
    }

    private bool PreviewChildCountMismatch()
    {
        return previewRoot != null
            && previewRoot.childCount != PlayableSceneRules.PreviewPlayerHandSize + PlayableSceneRules.PreviewEnemyHandSize;
    }

    private void CreatePreviewCard(string cardName, Vector3 position, bool hidden, Color bodyColor)
    {
        GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
        card.name = cardName;
        card.transform.SetParent(previewRoot, false);
        card.transform.position = position;
        card.transform.localScale = new Vector3(0.72f, 0.035f, 1.04f);
        DisableGeneratedCollider(card.GetComponent<Collider>());

        MeshRenderer renderer = card.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = PreviewMaterial(hidden ? new Color(0.08f, 0.1f, 0.16f) : bodyColor);

        CreateCardStrip(card.transform, hidden ? new Color(0.18f, 0.22f, 0.3f) : Color.Lerp(bodyColor, Color.white, 0.35f));
    }

    private void CreateCardStrip(Transform parent, Color color)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "Preview Card Art";
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = new Vector3(0f, 0.62f, 0.05f);
        strip.transform.localScale = new Vector3(0.8f, 0.08f, 0.36f);
        DisableGeneratedCollider(strip.GetComponent<Collider>());

        MeshRenderer renderer = strip.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = PreviewMaterial(color);
    }

    private void CreatePreviewStatus()
    {
        SceneStatusDisplay status = FindObjectOfType<SceneStatusDisplay>();
        if (status != null)
        {
            status.UpdateSnapshot(
                new PlayerState(PlayerSide.Player) { HeadquartersHealth = 20, Kredits = 1, MaxKredits = 1 },
                new PlayerState(PlayerSide.Enemy) { HeadquartersHealth = 20, Kredits = 1, MaxKredits = 1 },
                GamePhase.PlayerTurn,
                PlayerSide.Player,
                "Neutral",
                "Editor preview: same playable layout as Play mode.",
                System.Array.Empty<string>());
        }

        SceneDeckSummary deckSummary = FindObjectOfType<SceneDeckSummary>();
        if (deckSummary != null)
        {
            deckSummary.Clear();
        }

        SceneCardInspector inspector = FindObjectOfType<SceneCardInspector>();
        if (inspector != null)
        {
            inspector.ShowCard(null);
        }
    }

    private void DisableGeneratedCollider(Collider generatedCollider)
    {
        if (generatedCollider != null)
        {
            generatedCollider.enabled = false;
        }
    }

    private void SetPreviewCommandButtons()
    {
        SceneCommandButton[] buttons = FindObjectsOfType<SceneCommandButton>();
        foreach (SceneCommandButton button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            bool visible = button.Command == SceneCommandType.EndTurn
                || button.Command == SceneCommandType.Restart
                || button.Command == SceneCommandType.StrikeBoard;
            button.SetAvailable(visible);
            button.SetVisible(visible);
        }
    }

    private void ApplyTabletopContourBackground()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = tabletop.AddComponent<DeskContourTerrainGenerator>();
        }

        if (generator.CurrentContourStyle != DeskContourTerrainGenerator.ContourStyle.ReferenceMatchBackdrop
            || !generator.IsTablePresetApplied
            || !generator.HasGeneratedTexture)
        {
            generator.ApplyReferenceMatchBackdropPresetReferenceImageMatchTarget();
        }
        else
        {
            generator.ApplyTabletopSciFiPresetIfNeeded();
        }
    }

    private Vector3 HandPosition(PlayerSide side, int index, int count)
    {
        Vector3 anchor = side == PlayerSide.Player ? PlayableSceneRules.PlayerHandAnchor : PlayableSceneRules.EnemyHandAnchor;
        float offset = CardLayoutRules.OffsetIndex(index, count) * PlayableSceneRules.HandSpacing;
        return anchor + Vector3.right * offset;
    }

    private Material PreviewMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }
}
