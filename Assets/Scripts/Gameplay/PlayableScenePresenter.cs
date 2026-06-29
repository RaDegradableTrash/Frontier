using UnityEngine;

public class PlayableScenePresenter : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private const string SurfaceName = "Battlefield Surface";
    private const string TableBorderName = "Dark Table Border";
    private const string HandRailName = "Player Hand Rail";
    private const string HandHintName = "Player Hand Hint";
    private const string CountermeasureHintName = "Countermeasure Hint";
    private const string ActionPromptName = "Action Prompt";
    private const string ActionPromptBackingName = "Action Prompt Backing";
    private const string StatusPanelBackingName = "Status Panel Backing";
    private const string CardInspectorBackingName = "Card Inspector Backing";
    private const string PlayerKreditDisplayName = "Player Kredit Display";
    private const string EnemyKreditDisplayName = "Enemy Kredit Display";
    private const string KreditDisplayBackingSuffix = " Backing";
    private const string PileBackingSuffix = " Pile Backing";
    private const string TextureLinePrefix = "Surface Groove ";
    private Material cachedSurfaceMaterial;
    private Material cachedTableBorderMaterial;
    private Material cachedGrooveMaterial;
    private Material cachedHandRailMaterial;
    private Material cachedInfoPanelMaterial;
    private Material cachedActionPromptMaterial;
    private bool runtimePresentationEnabled;

private static bool CanMutateSceneHierarchy
        {
            get
            {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
            {
                return false;
            }

            if (!Application.isPlaying)
            {
                return false;
            }

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            if (Time.frameCount < 2)
            {
                return false;
            }

            return !EditorConsistencyCallbackInProgress();
#else
            return Application.isPlaying;
#endif
        }
    }

#if UNITY_EDITOR
    private static bool EditorConsistencyCallbackInProgress()
    {
        var stack = new System.Diagnostics.StackTrace();
        for (int i = 1; i < stack.FrameCount; i++)
        {
            var method = stack.GetFrame(i)?.GetMethod();
            var name = method?.Name ?? string.Empty;
            if (name.Contains("OnValidate") || name.Contains("CheckConsistency") || name.Contains("OnBeforeTransformParentChanged") || name.Contains("OnTransformParentChanged") || name.Contains("OnTransformChildrenChanged"))
            {
                return true;
            }
        }

        return false;
    }
#endif

    public void EnableRuntimePresentation()
    {
        runtimePresentationEnabled = true;
        Apply();
    }

    public void Apply()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!runtimePresentationEnabled || !CanMutateSceneHierarchy)
        {
            return;
        }

#if UNITY_EDITOR
        if (EditorConsistencyCallbackInProgress() || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
#endif

        ConfigureCamera();
        ConfigureTabletop();
        ConfigureTableBorder();
        ConfigureBattlefieldSurface();
        ConfigureHandRail();
        ConfigureHandHint();
        ConfigureCountermeasureHint();
        ConfigureActionPrompt();
        ConfigureInfoPanels();
        ConfigureKreditDisplays();
        ConfigurePileDisplays();
        ConfigureCommandButtons();
        ConfigureAuthoredLabels();
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.orthographic = true;
        camera.orthographicSize = PlayableSceneRules.OrthographicSize;
        camera.backgroundColor = PlayableSceneRules.CameraBackgroundColor;
        camera.transform.position = PlayableSceneRules.CameraPosition;
        camera.transform.rotation = Quaternion.Euler(PlayableSceneRules.CameraEuler);
    }

    private void ConfigureBattlefieldSurface()
    {
        GameObject surface = GameObject.Find(SurfaceName);
        if (surface == null)
        {
            surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = SurfaceName;
        }

        if (surface.GetComponent<BoxCollider>() == null)
        {
            surface.AddComponent<BoxCollider>();
        }

        if (surface.GetComponent<BoardAreaClickCatcher>() == null)
        {
            surface.AddComponent<BoardAreaClickCatcher>();
        }

        surface.transform.position = new Vector3(0f, 0.012f, 0f);
        surface.transform.localScale = PlayableSceneRules.BattlefieldSurfaceScale;

        MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedSurfaceMaterial, new Color(0.19f, 0.17f, 0.12f));
        }

        for (int i = 0; i < 7; i++)
        {
            CreateSurfaceGroove(i, -6f + i * 2f, true);
        }

        for (int i = 0; i < 5; i++)
        {
            CreateSurfaceGroove(10 + i, -2.5f + i * 1.25f, false);
        }
    }

    private void ConfigureTableBorder()
    {
        GameObject border = GameObject.Find(TableBorderName);
        if (border == null)
        {
            border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = TableBorderName;
            Collider collider = border.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        border.transform.position = new Vector3(0f, 0.006f, 0f);
        border.transform.localScale = PlayableSceneRules.TableBorderScale;

        MeshRenderer renderer = border.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedTableBorderMaterial, PlayableSceneRules.TableBorderColor);
        }
    }

    private void CreateSurfaceGroove(int index, float offset, bool vertical)
    {
        string grooveName = $"{TextureLinePrefix}{index}";
        GameObject groove = GameObject.Find(grooveName);
        if (groove == null)
        {
            groove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groove.name = grooveName;
            Collider collider = groove.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        groove.transform.position = vertical ? new Vector3(offset, 0.026f, 0f) : new Vector3(0f, 0.027f, offset);
        groove.transform.localScale = vertical ? new Vector3(0.025f, 0.012f, 7.7f) : new Vector3(13.2f, 0.012f, 0.02f);

        MeshRenderer renderer = groove.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedGrooveMaterial, new Color(0.10f, 0.085f, 0.055f));
        }
    }

    private void ConfigureHandRail()
    {
        GameObject handRail = GameObject.Find(HandRailName);
        if (handRail == null)
        {
            handRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handRail.name = HandRailName;
        }

        handRail.transform.position = new Vector3(0f, 0.045f, -4.42f);
        handRail.transform.localScale = new Vector3(6.25f, 0.035f, 0.34f);

        if (handRail.GetComponent<Collider>() == null)
        {
            handRail.AddComponent<BoxCollider>();
        }

        if (handRail.GetComponent<HandRevealZone>() == null)
        {
            handRail.AddComponent<HandRevealZone>();
        }

        MeshRenderer renderer = handRail.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedHandRailMaterial, new Color(0.055f, 0.055f, 0.052f));
        }
    }

    private void ConfigureHandHint()
    {
        GameObject hint = GameObject.Find(HandHintName);
        if (!PlayableSceneRules.HandHintLabelEnabled)
        {
            if (hint != null)
            {
                hint.SetActive(false);
            }

            return;
        }

        if (hint == null)
        {
            hint = new GameObject(HandHintName);
            hint.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh textMesh = hint.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 72;
        }

        hint.SetActive(true);
        hint.transform.position = PlayableSceneRules.HandHintPosition;
        TextMesh label = hint.GetComponent<TextMesh>();
        if (label != null)
        {
            label.text = "HAND";
            label.characterSize = 0.014f;
            label.color = new Color(1f, 0.9f, 0.58f, 1f);
        }
    }

    private void ConfigureCountermeasureHint()
    {
        GameObject hint = GameObject.Find(CountermeasureHintName);
        if (hint == null)
        {
            hint = new GameObject(CountermeasureHintName);
            hint.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh textMesh = hint.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 72;
        }

        hint.transform.position = PlayableSceneRules.CountermeasureHintPosition;
        TextMesh label = hint.GetComponent<TextMesh>();
        if (label != null)
        {
            label.text = "COUNTERS";
            label.characterSize = 0.014f;
            label.color = new Color(1f, 0.82f, 0.42f, 1f);
        }
    }

    private void ConfigureActionPrompt()
    {
        GameObject prompt = GameObject.Find(ActionPromptName);
        if (prompt == null)
        {
            prompt = new GameObject(ActionPromptName);
            prompt.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            prompt.AddComponent<SceneActionPrompt>();
        }

        SceneActionPrompt actionPrompt = prompt.GetComponent<SceneActionPrompt>();
        if (actionPrompt == null)
        {
            actionPrompt = prompt.AddComponent<SceneActionPrompt>();
        }

        actionPrompt.ApplyPresentation();
        ConfigureActionPromptBacking();
    }

    private void ConfigureActionPromptBacking()
    {
        GameObject backing = GameObject.Find(ActionPromptBackingName);
        if (backing == null)
        {
            backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = ActionPromptBackingName;
            Collider collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        backing.transform.position = new Vector3(PlayableSceneRules.ActionPromptPosition.x, 0.12f, PlayableSceneRules.ActionPromptPosition.z);
        Vector2 scale = PlayableSceneRules.ActionPromptBackingScale;
        backing.transform.localScale = new Vector3(scale.x, 0.035f, scale.y);

        MeshRenderer renderer = backing.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedActionPromptMaterial, new Color(0.035f, 0.032f, 0.026f));
            renderer.enabled = PlayableSceneRules.TabletopActionPromptEnabled;
        }
    }

    private void ConfigureInfoPanels()
    {
        SceneStatusDisplay status = FindObjectOfType<SceneStatusDisplay>();
        if (status != null)
        {
            status.transform.position = PlayableSceneRules.StatusPanelPosition;
            status.ApplyPresentation();
            SetRendererVisible(status.gameObject, PlayableSceneRules.TabletopInfoPanelsEnabled);
            ConfigureInfoBacking(StatusPanelBackingName, PlayableSceneRules.StatusPanelPosition, PlayableSceneRules.TabletopInfoPanelsEnabled);
        }

        SceneCardInspector inspector = FindObjectOfType<SceneCardInspector>();
        if (inspector != null)
        {
            inspector.transform.position = PlayableSceneRules.CardInspectorPosition;
            inspector.ApplyPresentation();
            SetRendererVisible(inspector.gameObject, PlayableSceneRules.TabletopInfoPanelsEnabled);
            ConfigureInfoBacking(CardInspectorBackingName, PlayableSceneRules.CardInspectorPosition, PlayableSceneRules.TabletopInfoPanelsEnabled);
        }

        SceneDeckSummary deckSummary = FindObjectOfType<SceneDeckSummary>();
        if (deckSummary != null)
        {
            deckSummary.transform.position = new Vector3(1.35f, 0.16f, 3.45f);
        }
    }

    private void ConfigureInfoBacking(string name, Vector3 textPosition, bool visible)
    {
        GameObject backing = GameObject.Find(name);
        if (backing == null)
        {
            backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = name;
            Collider collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        Vector2 scale = PlayableSceneRules.InfoPanelBackgroundScale;
        backing.transform.position = new Vector3(textPosition.x + scale.x * 0.48f, 0.052f, textPosition.z - scale.y * 0.45f);
        backing.transform.localScale = new Vector3(scale.x, 0.035f, scale.y);

        MeshRenderer renderer = backing.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = SurfaceMaterial(ref cachedInfoPanelMaterial, new Color(0.055f, 0.058f, 0.048f));
            renderer.enabled = visible;
        }
    }

    private void SetRendererVisible(GameObject target, bool visible)
    {
        Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void ConfigurePileDisplays()
    {
        ScenePileDisplay[] piles = FindObjectsOfType<ScenePileDisplay>();
        foreach (ScenePileDisplay pile in piles)
        {
            if (pile == null)
            {
                continue;
            }

            pile.transform.position = PilePosition(pile.Side, pile.Kind);
            pile.ApplyPresentation();
            ConfigurePileBacking(pile);
        }
    }

    private void ConfigureKreditDisplays()
    {
        ConfigureKreditDisplay(PlayerSide.Player, PlayerKreditDisplayName, PlayableSceneRules.PlayerKreditDisplayPosition);
        ConfigureKreditDisplay(PlayerSide.Enemy, EnemyKreditDisplayName, PlayableSceneRules.EnemyKreditDisplayPosition);
    }

    private void ConfigureKreditDisplay(PlayerSide side, string objectName, Vector3 position)
    {
        GameObject displayObject = GameObject.Find(objectName);
        if (displayObject == null)
        {
            displayObject = new GameObject(objectName);
        }

        displayObject.transform.position = position;
        SceneKreditDisplay display = displayObject.GetComponent<SceneKreditDisplay>();
        if (display == null)
        {
            display = displayObject.AddComponent<SceneKreditDisplay>();
        }

        display.Initialize(side);
        ConfigureKreditBacking(objectName + KreditDisplayBackingSuffix, position);
    }

    private void ConfigureKreditBacking(string name, Vector3 position)
    {
        GameObject backing = GameObject.Find(name);
        if (backing == null)
        {
            backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = name;
            Collider collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        Vector2 scale = PlayableSceneRules.KreditDisplayBackingScale;
        backing.transform.position = new Vector3(position.x, 0.055f, position.z);
        backing.transform.localScale = new Vector3(scale.x, 0.035f, scale.y);

        MeshRenderer renderer = backing.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = NewSurfaceMaterial(new Color(0.06f, 0.05f, 0.035f));
        }
    }

    private void ConfigurePileBacking(ScenePileDisplay pile)
    {
        string backingName = $"{pile.Side} {pile.Kind}{PileBackingSuffix}";
        GameObject backing = GameObject.Find(backingName);
        if (backing == null)
        {
            backing = new GameObject(backingName);
            backing.name = backingName;
        }

        backing.transform.position = pile.transform.position;
        Vector2 scale = PlayableSceneRules.PileBadgeScale;
        if (PlayableSceneRules.PileStackLayerCount <= 0)
        {
            for (int i = backing.transform.childCount - 1; i >= 0; i--)
            {
                DestroyGeneratedObject(backing.transform.GetChild(i).gameObject);
            }

            Vector3 baseScale = new Vector3(scale.x, 0.026f, scale.y);
            MeshRenderer existingRenderer = backing.GetComponent<MeshRenderer>();
            if (existingRenderer == null)
            {
                GameObject baseLayer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseLayer.name = "LayerFlat";
                baseLayer.transform.SetParent(backing.transform, false);
                baseLayer.transform.localPosition = new Vector3(0f, -0.108f, 0f);
                baseLayer.transform.localScale = baseScale;
                Collider collider = baseLayer.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyGeneratedObject(collider);
                }

                existingRenderer = baseLayer.GetComponent<MeshRenderer>();
            }

            if (existingRenderer != null)
            {
                Color color = Color.Lerp(new Color(0.05f, 0.055f, 0.05f), new Color(0.09f, 0.095f, 0.085f), 0.45f);
                existingRenderer.sharedMaterial = NewSurfaceMaterial(color);
            }

            return;
        }

        for (int i = 0; i < PlayableSceneRules.PileStackLayerCount; i++)
        {
            Transform layer = backing.transform.Find($"Layer {i}");
            if (layer == null)
            {
                GameObject layerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                layerObject.name = $"Layer {i}";
                layerObject.transform.SetParent(backing.transform, false);
                Collider collider = layerObject.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyGeneratedObject(collider);
                }

                layer = layerObject.transform;
            }

            float offset = i * PlayableSceneRules.PileStackLayerOffset;
            layer.localPosition = new Vector3(-offset, -0.108f - i * 0.002f, offset);
            layer.localScale = new Vector3(scale.x, 0.026f, scale.y);

            MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Color color = Color.Lerp(new Color(0.035f, 0.04f, 0.035f), new Color(0.075f, 0.08f, 0.07f), i / 3f);
                renderer.sharedMaterial = NewSurfaceMaterial(color);
            }
        }

        ConfigurePileTopPattern(backing.transform, pile.Kind, scale);
    }

    private void ConfigurePileTopPattern(Transform backing, ScenePileKind kind, Vector2 scale)
    {
        if (backing == null || PlayableSceneRules.PileStackLayerCount <= 0)
        {
            return;
        }

        float topOffset = (PlayableSceneRules.PileStackLayerCount - 1) * PlayableSceneRules.PileStackLayerOffset;
        float topY = -0.108f - (PlayableSceneRules.PileStackLayerCount - 1) * 0.002f + 0.018f;
        string prefix = kind == ScenePileKind.Deck ? "Deck Back" : "Discard Top";
        Color stripeColor = kind == ScenePileKind.Deck
            ? new Color(0.68f, 0.62f, 0.48f, 1f)
            : new Color(0.38f, 0.40f, 0.36f, 1f);

        for (int i = 0; i < 3; i++)
        {
            string stripeName = $"{prefix} Stripe {i}";
            Transform stripe = backing.Find(stripeName);
            if (stripe == null)
            {
                GameObject stripeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripeObject.name = stripeName;
                stripeObject.transform.SetParent(backing, false);
                Collider collider = stripeObject.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyGeneratedObject(collider);
                }

                stripe = stripeObject.transform;
            }

            stripe.localPosition = new Vector3(-topOffset, topY + i * 0.003f, topOffset - 0.18f + i * 0.18f);
            stripe.localScale = new Vector3(scale.x * 0.62f, 0.006f, 0.018f);
            MeshRenderer stripeRenderer = stripe.GetComponent<MeshRenderer>();
            if (stripeRenderer != null)
            {
                stripeRenderer.sharedMaterial = NewSurfaceMaterial(stripeColor);
            }
        }
    }

    private Vector3 PilePosition(PlayerSide side, ScenePileKind kind)
    {
        if (side == PlayerSide.Player)
        {
            return kind == ScenePileKind.Deck
                ? PlayableSceneRules.PlayerDeckPilePosition
                : PlayableSceneRules.PlayerDiscardPilePosition;
        }

        return kind == ScenePileKind.Deck
            ? PlayableSceneRules.EnemyDeckPilePosition
            : PlayableSceneRules.EnemyDiscardPilePosition;
    }

    private void ConfigureCommandButtons()
    {
        SceneCommandButton[] buttons = FindObjectsOfType<SceneCommandButton>();
        foreach (SceneCommandButton button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (TryGetCommandPosition(button.Command, out Vector3 position))
            {
                button.transform.position = position;
                button.ApplyPresentation();
            }
        }
    }

    private bool TryGetCommandPosition(SceneCommandType command, out Vector3 position)
    {
        switch (command)
        {
            case SceneCommandType.EndTurn:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, -0.10f);
                return true;
            case SceneCommandType.Restart:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, -0.62f);
                return true;
            case SceneCommandType.StrikeBoard:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, -1.14f);
                return true;
            case SceneCommandType.StartMatch:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, 1.14f);
                return true;
            case SceneCommandType.KeepHand:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, 0.62f);
                return true;
            case SceneCommandType.Mulligan:
                position = new Vector3(PlayableSceneRules.CommandColumnX, 0.16f, 0.10f);
                return true;
            case SceneCommandType.SelectAlliedTempo:
                position = new Vector3(-2.55f, 0.16f, PlayableSceneRules.DeckSelectorRowZ);
                return true;
            case SceneCommandType.SelectAxisArmor:
                position = new Vector3(-0.85f, 0.16f, PlayableSceneRules.DeckSelectorRowZ);
                return true;
            case SceneCommandType.SelectSovietControl:
                position = new Vector3(0.85f, 0.16f, PlayableSceneRules.DeckSelectorRowZ);
                return true;
            case SceneCommandType.SelectJapanAmbush:
                position = new Vector3(2.55f, 0.16f, PlayableSceneRules.DeckSelectorRowZ);
                return true;
            default:
                position = Vector3.zero;
                return false;
        }
    }

    private Material SurfaceMaterial(ref Material material, Color color)
    {
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
        }

        if (material.HasProperty(ColorProperty))
        {
            material.color = color;
        }

        return material;
    }

    private Material NewSurfaceMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        if (material.HasProperty(ColorProperty))
        {
            material.color = color;
        }

        return material;
    }

    private void ConfigureTabletop()
    {
        GameObject tabletop = GameObject.Find("DesktopQuad");
        if (tabletop == null)
        {
            return;
        }

        tabletop.transform.localScale = PlayableSceneRules.TabletopScale;
        if (tabletop.GetComponent<Collider>() == null)
        {
            tabletop.AddComponent<BoxCollider>();
        }

        if (tabletop.GetComponent<BoardAreaClickCatcher>() == null)
        {
            tabletop.AddComponent<BoardAreaClickCatcher>();
        }

        MeshRenderer renderer = tabletop.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssignTabletopMaterial(renderer, material);
        }

        if (material.HasProperty(ColorProperty))
        {
            material.color = PlayableSceneRules.TabletopColor;
        }
    }

    private void AssignTabletopMaterial(MeshRenderer renderer, Material material)
    {
        if (Application.isPlaying)
        {
            renderer.material = material;
        }
        else
        {
            renderer.sharedMaterial = material;
        }
    }

    private void ConfigureAuthoredLabels()
    {
        TextMesh[] textMeshes = FindObjectsOfType<TextMesh>();
        foreach (TextMesh textMesh in textMeshes)
        {
            if (textMesh == null)
            {
                continue;
            }

            if (textMesh.GetComponent<BoardSceneMarker>() != null || IsBoardLabel(textMesh.text))
            {
                Renderer renderer = textMesh.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
    }

    private bool IsBoardLabel(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Contains("HQ") || text.Contains("SUPPORT") || text.Contains("FRONTLINE");
    }

    private void DestroyGeneratedObject(Object generatedObject)
    {
        if (generatedObject == null)
        {
            return;
        }

        // Any non-runtime cleanup in edit mode can run during scene serialization/
        // restore paths and must never call Destroy while not playing.
        if (!Application.isPlaying)
        {
            return;
        }

        if (generatedObject is Collider collider)
        {
            collider.enabled = false;
            return;
        }

        RuntimeSafeDestroy.Destroy(generatedObject);
    }
}
