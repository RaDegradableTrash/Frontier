using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [SerializeField] private int frontlineColumns = 5;
    [SerializeField] private int supportColumns = 5;
    [SerializeField] private float slotWidth = 0.96f;
    [SerializeField] private float slotHeight = 1.26f;
    [SerializeField] private float slotPadding = 0.20f;
    [SerializeField] private Material lineMaterial;

    [Header("Strike Settings")]
    [SerializeField, Range(0, 5f)] private float rippleForce = 1.0f;
    [SerializeField, Range(0, 1f)] private float cameraShakeForce = 0.2f;
    [SerializeField] private CameraInteraction camInteraction;

    private SlotInteract[,] grid;
    private GameController controller;
    private BoardPresentationSettings presentationSettings;
    private SlotInteract playerHeadquartersSlot;
    private SlotInteract enemyHeadquartersSlot;
    private Transform rowsRoot;
    private Transform surfaceRoot;
    private Transform labelsRoot;
    private Transform markersRoot;
    private TextMesh playerHeadquartersStrength;
    private TextMesh enemyHeadquartersStrength;
    private TextMesh playerHeadquartersDamagePreview;
    private TextMesh enemyHeadquartersDamagePreview;
    private GameObject playerHeadquartersSkullPreview;
    private GameObject enemyHeadquartersSkullPreview;

    public int FrontlineColumns => frontlineColumns;
    public int SupportColumns => supportColumns;
    public float SlotHeight => slotHeight;
    public float SlotStepX => slotWidth + slotPadding;

    public void Initialize(GameController owner)
    {
        controller = owner;
        BuildGrid();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            BuildGrid();
        }
    }

    private void OnValidate()
    {
    }

    public SlotInteract GetSlot(int x, SlotZone zone)
    {
        int z = ZoneToRow(zone);
        if (grid == null || x < 0 || x >= grid.GetLength(0) || z < 0 || z >= grid.GetLength(1))
        {
            return null;
        }

        return grid[x, z];
    }

    public SlotInteract GetSlot(Vector3 worldPosition)
    {
        if (grid == null)
        {
            return null;
        }

        SlotInteract closest = null;
        float bestDistance = float.MaxValue;
        foreach (SlotInteract slot in grid)
        {
            if (slot == null)
            {
                continue;
            }

            float distance = Vector3.Distance(worldPosition, slot.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = slot;
            }
        }

        TryReplaceClosestSlot(playerHeadquartersSlot, worldPosition, ref closest, ref bestDistance);
        TryReplaceClosestSlot(enemyHeadquartersSlot, worldPosition, ref closest, ref bestDistance);
        if (!BoardTargetRules.ShouldAcceptClosestTarget(bestDistance))
        {
            return null;
        }

        return closest;
    }

    private void TryReplaceClosestSlot(SlotInteract candidate, Vector3 worldPosition, ref SlotInteract closest, ref float bestDistance)
    {
        if (candidate == null)
        {
            return;
        }

        float distance = Vector3.Distance(worldPosition, candidate.transform.position);
        if (!BoardTargetRules.ShouldReplaceClosestTarget(distance, bestDistance))
        {
            return;
        }

        bestDistance = distance;
        closest = candidate;
    }

    public SlotInteract GetHeadquartersSlot(PlayerSide side)
    {
        return side == PlayerSide.Player ? playerHeadquartersSlot : enemyHeadquartersSlot;
    }

    public void UpdateHeadquartersHealth(int playerHealth, int enemyHealth)
    {
        if (playerHeadquartersStrength != null)
        {
            playerHeadquartersStrength.text = HeadquartersDisplayTextRules.Health(playerHealth);
        }

        if (enemyHeadquartersStrength != null)
        {
            enemyHeadquartersStrength.text = HeadquartersDisplayTextRules.Health(enemyHealth);
        }
    }

    public void HandleSlotClicked(SlotInteract slot)
    {
        controller?.HandleSlotClicked(slot);
    }

    public void TriggerStrike(int x, SlotZone zone)
    {
        SlotInteract origin = GetSlot(x, zone);
        if (origin == null)
        {
            return;
        }

        Vector3 strikePosition = origin.transform.position;
        camInteraction?.ShakeCamera(cameraShakeForce);

        foreach (SlotInteract slot in grid)
        {
            slot?.DoStrike(strikePosition, rippleForce);
        }
    }

    private void Start()
    {
        if (camInteraction == null)
        {
            camInteraction = FindObjectOfType<CameraInteraction>();
        }

        if (controller == null)
        {
            BuildGrid();
        }
    }

    private void BuildGrid()
    {
        supportColumns = 5;
        ClearExistingSlots();
        playerHeadquartersSlot = null;
        enemyHeadquartersSlot = null;
        playerHeadquartersStrength = null;
        enemyHeadquartersStrength = null;
        playerHeadquartersDamagePreview = null;
        enemyHeadquartersDamagePreview = null;
        playerHeadquartersSkullPreview = null;
        enemyHeadquartersSkullPreview = null;
        PrepareGeneratedHierarchy();
        int columns = Mathf.Max(frontlineColumns, supportColumns);
        grid = new SlotInteract[columns, 3];

        CreateRow(SlotZone.EnemySupport, supportColumns, PlayableSceneRules.SupportRowZ, PlayableSceneRules.EnemySlotColor);
        CreateRow(SlotZone.Frontline, frontlineColumns, 0f, PlayableSceneRules.FrontlineSlotColor);
        CreateRow(SlotZone.PlayerSupport, supportColumns, -PlayableSceneRules.SupportRowZ, PlayableSceneRules.PlayerSlotColor);
        ConfigureHeadquartersSlot(PlayerSide.Enemy);
        ConfigureHeadquartersSlot(PlayerSide.Player);
        CreateCheckerboardCells();
    }

    private void ConfigureHeadquartersSlot(PlayerSide side)
    {
        SlotZone zone = BoardTargetRules.HeadquartersTargetZone(side);
        SlotInteract interact = GetSlot(BoardTargetRules.HeadquartersSlotIndex, zone);
        if (interact == null)
        {
            return;
        }

        Color tint = side == PlayerSide.Player ? PlayableSceneRules.PlayerSlotColor : PlayableSceneRules.EnemySlotColor;
        if (side == PlayerSide.Player)
        {
            playerHeadquartersSlot = interact;
        }
        else
        {
            enemyHeadquartersSlot = interact;
        }

        Transform hqVisualRoot = CreateGeneratedGroup(interact.transform, "HQ Visual");
        Transform hqFrameRoot = CreateGeneratedGroup(hqVisualRoot, "Frame");
        Transform hqPreviewRoot = CreateGeneratedGroup(hqVisualRoot, "Preview");

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "HQ Card Body";
        plate.transform.SetParent(hqFrameRoot, false);
        plate.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        plate.transform.localScale = new Vector3(0.88f, 0.060f, 1.16f);
        DestroyGeneratedObject(plate.GetComponent<Collider>());

        MeshRenderer plateRenderer = plate.GetComponent<MeshRenderer>();
        AssignGeneratedMaterial(plateRenderer, Color.Lerp(tint, PlayableSceneRules.HeadquartersSlotColor, 0.35f));

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "HQ Card Frame";
        frame.transform.SetParent(hqFrameRoot, false);
        frame.transform.localPosition = new Vector3(0f, 0.064f, 0f);
        frame.transform.localScale = new Vector3(0.94f, 0.014f, 1.22f);
        DestroyGeneratedObject(frame.GetComponent<Collider>());
        AssignGeneratedMaterial(frame.GetComponent<MeshRenderer>(), new Color(0.92f, 0.88f, 0.72f, 1f));

        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
        face.name = "HQ Card Face";
        face.transform.SetParent(hqFrameRoot, false);
        face.transform.localPosition = new Vector3(0f, 0.078f, 0f);
        face.transform.localScale = new Vector3(0.78f, 0.012f, 1.02f);
        DestroyGeneratedObject(face.GetComponent<Collider>());
        AssignGeneratedMaterial(face.GetComponent<MeshRenderer>(), Color.Lerp(new Color(0.13f, 0.13f, 0.10f, 1f), tint, 0.35f));

        GameObject titlePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Transform titlePlateGroup = CreateGeneratedGroup(hqFrameRoot, "TitlePlate");
        titlePlate.name = "Backing";
        titlePlate.transform.SetParent(titlePlateGroup, false);
        titlePlate.transform.localPosition = new Vector3(0f, 0.092f, 0.39f);
        titlePlate.transform.localScale = new Vector3(0.64f, 0.014f, 0.16f);
        DestroyGeneratedObject(titlePlate.GetComponent<Collider>());
        AssignGeneratedMaterial(titlePlate.GetComponent<MeshRenderer>(), new Color(0.08f, 0.075f, 0.055f, 1f));

        GameObject healthBadge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Transform healthBadgeGroup = CreateGeneratedGroup(hqFrameRoot, "HealthBadge");
        healthBadge.name = "Backing";
        healthBadge.transform.SetParent(healthBadgeGroup, false);
        healthBadge.transform.localPosition = new Vector3(0f, 0.094f, -0.08f);
        healthBadge.transform.localScale = new Vector3(0.48f, 0.014f, 0.28f);
        DestroyGeneratedObject(healthBadge.GetComponent<Collider>());
        AssignGeneratedMaterial(healthBadge.GetComponent<MeshRenderer>(), new Color(0.045f, 0.047f, 0.040f, 1f));

        CreateHeadquartersTextureLines(hqFrameRoot, tint);
        TextMesh strengthText = CreateHeadquartersText(
            titlePlateGroup,
            healthBadgeGroup,
            side == PlayerSide.Player ? "WASHINGTON" : "RANGOON",
            HeadquartersDisplayTextRules.Health(20));
        if (side == PlayerSide.Player)
        {
            playerHeadquartersStrength = strengthText;
        }
        else
        {
            enemyHeadquartersStrength = strengthText;
        }

        TextMesh previewText = CreateHeadquartersTextMesh(
            hqPreviewRoot,
            "HQ Damage Preview",
            string.Empty,
            new Vector3(0f, 0.126f, -0.08f),
            0.105f,
            TextAnchor.MiddleCenter);
        previewText.color = new Color(1f, 0.24f, 0.14f, 1f);
        Renderer previewRenderer = previewText.GetComponent<Renderer>();
        if (previewRenderer != null)
        {
            previewRenderer.enabled = false;
        }

        GameObject skullPreview = CreateHeadquartersSkullPreview(hqPreviewRoot);
        if (side == PlayerSide.Player)
        {
            playerHeadquartersDamagePreview = previewText;
            playerHeadquartersSkullPreview = skullPreview;
        }
        else
        {
            enemyHeadquartersDamagePreview = previewText;
            enemyHeadquartersSkullPreview = skullPreview;
        }
    }

    public void ShowHeadquartersDamagePreview(PlayerSide side, int damage, bool lethal)
    {
        TextMesh damagePreview = side == PlayerSide.Player ? playerHeadquartersDamagePreview : enemyHeadquartersDamagePreview;
        GameObject skullPreview = side == PlayerSide.Player ? playerHeadquartersSkullPreview : enemyHeadquartersSkullPreview;
        if (damagePreview == null || skullPreview == null)
        {
            return;
        }

        if (lethal)
        {
            damagePreview.text = string.Empty;
            Renderer damageRenderer = damagePreview.GetComponent<Renderer>();
            if (damageRenderer != null)
            {
                damageRenderer.enabled = false;
            }

            skullPreview.SetActive(true);
            return;
        }

        if (damage <= 0)
        {
            HideHeadquartersDamagePreview(side);
            return;
        }

        skullPreview.SetActive(false);
        damagePreview.text = damage.ToString();
        Renderer renderer = damagePreview.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

    public void HideHeadquartersDamagePreviews()
    {
        HideHeadquartersDamagePreview(PlayerSide.Player);
        HideHeadquartersDamagePreview(PlayerSide.Enemy);
    }

    private void HideHeadquartersDamagePreview(PlayerSide side)
    {
        TextMesh damagePreview = side == PlayerSide.Player ? playerHeadquartersDamagePreview : enemyHeadquartersDamagePreview;
        GameObject skullPreview = side == PlayerSide.Player ? playerHeadquartersSkullPreview : enemyHeadquartersSkullPreview;
        if (damagePreview != null)
        {
            damagePreview.text = string.Empty;
            Renderer renderer = damagePreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        if (skullPreview != null)
        {
            skullPreview.SetActive(false);
        }
    }

    private GameObject CreateHeadquartersSkullPreview(Transform parent)
    {
        GameObject skull = GameObject.CreatePrimitive(PrimitiveType.Quad);
        skull.name = "HQ Death Preview";
        skull.transform.SetParent(parent, false);
        skull.transform.localPosition = new Vector3(0f, 0.128f, -0.08f);
        skull.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        skull.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
        DestroyGeneratedObject(skull.GetComponent<Collider>());

        MeshRenderer renderer = skull.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            Texture2D skullTexture = SceneIconRegistry.Active != null
                ? SceneIconRegistry.Active.EstimatedDeathSkullIcon
                : Resources.Load<Texture2D>("Icons/EstimatedDeathSkull");
            if (skullTexture != null)
            {
                material.mainTexture = skullTexture;
            }

            renderer.material = material;
        }

        skull.SetActive(false);
        return skull;
    }

    private TextMesh CreateHeadquartersText(Transform titlePlateParent, Transform healthBadgeParent, string name, string strength)
    {
        CreateHeadquartersTextMesh(titlePlateParent, "HQ Name", name, new Vector3(0f, 0.09f, 0.36f), PlayableSceneRules.HeadquartersNameCharacterSize, TextAnchor.MiddleCenter);
        return CreateHeadquartersTextMesh(healthBadgeParent, "HQ Strength", strength, new Vector3(0f, 0.095f, -0.06f), PlayableSceneRules.HeadquartersStrengthCharacterSize, TextAnchor.MiddleCenter);
    }

    private void CreateHeadquartersTextureLines(Transform parent, Color tint)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = $"HQ Texture Stripe {i + 1}";
            stripe.transform.SetParent(parent, false);
            stripe.transform.localPosition = new Vector3(0f, 0.055f, -0.39f + i * 0.22f);
            stripe.transform.localScale = new Vector3(0.72f, 0.012f, 0.018f);
            DestroyGeneratedObject(stripe.GetComponent<Collider>());

            MeshRenderer renderer = stripe.GetComponent<MeshRenderer>();
            AssignGeneratedMaterial(renderer, Color.Lerp(new Color(0.06f, 0.055f, 0.042f), tint, 0.25f));
        }
    }

    private TextMesh CreateHeadquartersTextMesh(Transform parent, string objectName, string text, Vector3 localPosition, float characterSize, TextAnchor anchor)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = localPosition;
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = anchor;
        label.alignment = TextAlignment.Center;
        label.characterSize = characterSize;
        label.fontSize = 90;
        label.color = Color.white;
        return label;
    }

    private void PrepareGeneratedHierarchy()
    {
        rowsRoot = CreateGeneratedGroup(transform, "Rows");
        surfaceRoot = CreateGeneratedGroup(transform, "Board Surface");
        labelsRoot = CreateGeneratedGroup(transform, "Labels");
        markersRoot = CreateGeneratedGroup(transform, "Markers");
    }

    private Transform CreateGeneratedGroup(Transform parent, string groupName)
    {
        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(parent, false);
        return groupObject.transform;
    }

    private Transform RowGroup(SlotZone zone)
    {
        if (rowsRoot == null)
        {
            rowsRoot = CreateGeneratedGroup(transform, "Rows");
        }

        string groupName = zone.ToString();
        Transform existing = rowsRoot.Find(groupName);
        return existing != null ? existing : CreateGeneratedGroup(rowsRoot, groupName);
    }

    private Transform SurfaceGroup(SlotZone zone)
    {
        if (surfaceRoot == null)
        {
            surfaceRoot = CreateGeneratedGroup(transform, "Board Surface");
        }

        string groupName = zone.ToString();
        Transform existing = surfaceRoot.Find(groupName);
        return existing != null ? existing : CreateGeneratedGroup(surfaceRoot, groupName);
    }

    private void ClearExistingSlots()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<BoardSceneMarker>() != null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private bool UseAuthoredPresentation()
    {
        if (presentationSettings == null)
        {
            presentationSettings = GetComponent<BoardPresentationSettings>();
        }

        return presentationSettings != null && presentationSettings.useAuthoredPresentation;
    }

    private void CreateRow(SlotZone zone, int count, float zOffset, Color color)
    {
        float totalWidth = (count - 1) * SlotStepX;
        float supportOffset = zone == SlotZone.Frontline ? 0f : -0.58f;
        float startX = -totalWidth / 2f + supportOffset;
        int row = ZoneToRow(zone);

        for (int x = 0; x < count; x++)
        {
            GameObject slotObject = new GameObject($"{zone}_{x}");
            slotObject.transform.SetParent(RowGroup(zone), false);
            slotObject.transform.localPosition = new Vector3(startX + x * SlotStepX, 0f, zOffset);
            CreateGeneratedGroup(slotObject.transform, "HitArea");
            Transform visualRoot = CreateGeneratedGroup(slotObject.transform, "Visual");

            BoxCollider collider = slotObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(slotWidth, 0.08f, slotHeight);

            SlotVisualize_Temp visual = visualRoot.gameObject.AddComponent<SlotVisualize_Temp>();
            visual.Setup(CreateSlotCorners(), ResolveLineMaterial(), color);

            SlotInteract interact = slotObject.AddComponent<SlotInteract>();
            interact.Initialize(this, x, row, zone, visual);
            grid[x, row] = interact;
        }
    }

    private void CreateCheckerboardCells()
    {
        Color dark = new Color(0.12f, 0.105f, 0.075f, 1f);
        Color light = new Color(0.17f, 0.145f, 0.095f, 1f);
        CreateCheckerboardRow(SlotZone.EnemySupport, supportColumns, PlayableSceneRules.SupportRowZ, dark, light);
        CreateCheckerboardRow(SlotZone.Frontline, frontlineColumns, 0f, light, dark);
        CreateCheckerboardRow(SlotZone.PlayerSupport, supportColumns, -PlayableSceneRules.SupportRowZ, dark, light);
    }

    private void CreateCheckerboardRow(SlotZone zone, int count, float zOffset, Color firstColor, Color secondColor)
    {
        float totalWidth = (count - 1) * SlotStepX;
        float supportOffset = zone == SlotZone.Frontline ? 0f : -0.58f;
        float startX = -totalWidth / 2f + supportOffset;

        for (int x = 0; x < count; x++)
        {
            Transform cellRoot = CreateGeneratedGroup(SurfaceGroup(zone), $"{zone}_Board_Cell_{x}");
            cellRoot.localPosition = new Vector3(startX + x * SlotStepX, 0f, zOffset);

            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.name = "Surface Tile";
            cell.transform.SetParent(cellRoot, false);
            cell.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            cell.transform.localScale = new Vector3(slotWidth * 1.03f, 0.010f, slotHeight * 1.04f);
            DestroyGeneratedObject(cell.GetComponent<Collider>());

            MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
            AssignGeneratedMaterial(renderer, x % 2 == 0 ? firstColor : secondColor);
        }
    }

    private Vector3[] CreateSlotCorners()
    {
        Vector3 halfSize = new Vector3(slotWidth / 2f, 0.03f, slotHeight / 2f);
        return new[]
        {
            new Vector3(-halfSize.x, halfSize.y, halfSize.z),
            new Vector3(halfSize.x, halfSize.y, halfSize.z),
            new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            new Vector3(-halfSize.x, halfSize.y, -halfSize.z)
        };
    }

    private void CreateBoardLabels()
    {
        CreateLaneLabel("ENEMY SUPPORT", new Vector3(-3.1f, 0.04f, PlayableSceneRules.SupportRowZ), new Color(1f, 0.55f, 0.55f));
        CreateLaneLabel("FRONTLINE", new Vector3(-3.1f, 0.04f, 0f), new Color(1f, 0.9f, 0.25f));
        CreateLaneLabel("YOUR SUPPORT", new Vector3(-3.1f, 0.04f, -PlayableSceneRules.SupportRowZ), new Color(0.55f, 0.78f, 1f));
    }

    private void CreateLaneLabel(string text, Vector3 localPosition, Color color)
    {
        Transform labelRoot = CreateGeneratedGroup(labelsRoot != null ? labelsRoot : transform, $"Label_{text}");

        GameObject labelObject = new GameObject("Text");
        labelObject.transform.SetParent(labelRoot, false);
        labelObject.transform.localPosition = localPosition;
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.characterSize = 0.12f;
        label.fontSize = 64;
        label.color = color;
    }

    private void CreateHeadquartersMarker(string labelText, Vector3 localPosition, Color color)
    {
        Transform markerRoot = CreateGeneratedGroup(markersRoot != null ? markersRoot : transform, labelText.Replace(" ", "_"));
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Backing";
        marker.transform.SetParent(markerRoot, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localScale = new Vector3(1.8f, 0.04f, 0.55f);
        DestroyGeneratedObject(marker.GetComponent<Collider>());

        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        AssignGeneratedMaterial(renderer, color);

        CreateLaneLabel(labelText, localPosition + new Vector3(-0.62f, 0.08f, 0.02f), Color.white);
    }

    private void DestroyGeneratedObject(UnityEngine.Object generatedObject)
    {
        if (generatedObject == null)
        {
            return;
        }

        if (generatedObject is Collider collider)
        {
            collider.enabled = false;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedObject);
        }
        else
        {
            DestroyImmediate(generatedObject);
        }
    }

    private Material ResolveLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = Color.white;
        return material;
    }

    private void AssignGeneratedMaterial(MeshRenderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        if (Application.isPlaying)
        {
            renderer.material = material;
        }
        else
        {
            renderer.sharedMaterial = material;
        }
    }

    private int ZoneToRow(SlotZone zone)
    {
        switch (zone)
        {
            case SlotZone.PlayerSupport:
                return 0;
            case SlotZone.Frontline:
                return 1;
            case SlotZone.EnemySupport:
                return 2;
            default:
                return 0;
        }
    }
}
