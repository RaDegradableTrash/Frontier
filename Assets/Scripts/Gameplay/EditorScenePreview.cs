using UnityEngine;

[ExecuteAlways]
public class EditorScenePreview : MonoBehaviour
{
    private const string PreviewRootName = "Editor Play Preview";

    [SerializeField] private bool showPreview = PlayableSceneRules.EditorPreviewEnabled;

    private Transform previewRoot;
    private bool refreshing;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Refresh();
    }

    private void OnTransformChildrenChanged()
    {
        if (!Application.isPlaying && showPreview && !refreshing && previewRoot != null)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (Application.isPlaying || refreshing || !showPreview || !isActiveAndEnabled)
        {
            ClearPreview();
            return;
        }

        EnsureRoot();

        refreshing = true;
        try
        {
            ClearPreviewChildren();
            CreatePreviewHands();
            CreatePreviewStatus();
            ApplyTabletopContourBackground();
            SetPreviewCommandButtons();
            }
        finally
        {
            refreshing = false;
        }
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

        RuntimeSafeDestroy.Destroy(existing.gameObject);
        previewRoot = null;
    }

    private void ClearPreviewChildren()
    {
        for (int i = previewRoot.childCount - 1; i >= 0; i--)
        {
            RuntimeSafeDestroy.Destroy(previewRoot.GetChild(i).gameObject);
        }
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

        private void CreatePreviewCard(string cardName, Vector3 position, bool hidden, Color bodyColor)
        {
            RuntimeCard previewCard = new RuntimeCard
            {
                CardName = cardName,
                Faction = CardFaction.Endfield,
                Rarity = CardRarity.Standard,
                Type = CardType.Unit,
                Zone = CardZone.Hand,
                Owner = cardName.StartsWith("Enemy", System.StringComparison.Ordinal) ? PlayerSide.Enemy : PlayerSide.Player,
                KreditCost = 0,
                OperationCost = 0,
                DeploymentCostBonus = 0,
                CurrentDefense = 1,
                RulesText = string.Empty
            };

            GameObject cardObject = new GameObject(cardName);
            cardObject.name = cardName;
            cardObject.transform.SetParent(previewRoot, false);
            cardObject.transform.position = position;
            cardObject.transform.localScale = Vector3.one * 0.52f;

            CardView view = cardObject.AddComponent<CardView>();
            view.Initialize(previewCard, null, hidden, true);

            ApplyPreviewTint(view, hidden ? new Color(0.18f, 0.22f, 0.3f) : Color.Lerp(bodyColor, Color.white, 0.35f));
    }

    private void ApplyPreviewTint(CardView view, Color color)
    {
        if (view == null)
        {
            return;
        }

        MeshRenderer[] renderers = view.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            Material material = renderer != null
                ? Application.isPlaying ? renderer.material : renderer.sharedMaterial
                : null;
            if (material == null || !material.HasProperty("_Color"))
            {
                continue;
            }

            material.color = color;
        }
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

}
