using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [SerializeField] private int frontlineColumns = 5;
    [SerializeField] private int supportColumns = 4;
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
    private TextMesh playerHeadquartersStrength;
    private TextMesh enemyHeadquartersStrength;

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
        ClearExistingSlots();
        playerHeadquartersSlot = null;
        enemyHeadquartersSlot = null;
        playerHeadquartersStrength = null;
        enemyHeadquartersStrength = null;
        int columns = Mathf.Max(frontlineColumns, supportColumns);
        grid = new SlotInteract[columns, 3];

        CreateRow(SlotZone.EnemySupport, supportColumns, 1.35f, PlayableSceneRules.EnemySlotColor);
        CreateRow(SlotZone.Frontline, frontlineColumns, 0f, PlayableSceneRules.FrontlineSlotColor);
        CreateRow(SlotZone.PlayerSupport, supportColumns, -1.35f, PlayableSceneRules.PlayerSlotColor);
        CreateHeadquartersSlot(PlayerSide.Enemy);
        CreateHeadquartersSlot(PlayerSide.Player);
        CreateCheckerboardCells();
    }

    private void CreateHeadquartersSlot(PlayerSide side)
    {
        Vector3 position = side == PlayerSide.Player ? PlayableSceneRules.PlayerHeadquartersSlot : PlayableSceneRules.EnemyHeadquartersSlot;
        Color tint = side == PlayerSide.Player ? PlayableSceneRules.PlayerSlotColor : PlayableSceneRules.EnemySlotColor;
        GameObject slotObject = new GameObject($"{side}_Headquarters_Slot");
        slotObject.transform.SetParent(transform, false);
        slotObject.transform.localPosition = position;

        BoxCollider collider = slotObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.96f, 0.08f, 1.26f);

        SlotVisualize_Temp visual = slotObject.AddComponent<SlotVisualize_Temp>();
        visual.Setup(CreateHeadquartersSlotCorners(), ResolveLineMaterial(), PlayableSceneRules.HeadquartersSlotColor);

        SlotInteract interact = slotObject.AddComponent<SlotInteract>();
        interact.Initialize(this, BoardTargetRules.HeadquartersSlotIndex, ZoneToRow(BoardTargetRules.HeadquartersTargetZone(side)), BoardTargetRules.HeadquartersTargetZone(side), visual);
        if (side == PlayerSide.Player)
        {
            playerHeadquartersSlot = interact;
        }
        else
        {
            enemyHeadquartersSlot = interact;
        }

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "HQ Plate";
        plate.transform.SetParent(slotObject.transform, false);
        plate.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        plate.transform.localScale = new Vector3(0.88f, 0.035f, 1.16f);
        DestroyGeneratedObject(plate.GetComponent<Collider>());

        MeshRenderer plateRenderer = plate.GetComponent<MeshRenderer>();
        AssignGeneratedMaterial(plateRenderer, Color.Lerp(tint, PlayableSceneRules.HeadquartersSlotColor, 0.35f));

        CreateHeadquartersTextureLines(slotObject.transform, tint);
        TextMesh strengthText = CreateHeadquartersText(
            slotObject.transform,
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
    }

    private Vector3[] CreateHeadquartersSlotCorners()
    {
        Vector3 halfSize = new Vector3(0.52f, 0.035f, 0.67f);
        return new[]
        {
            new Vector3(-halfSize.x, halfSize.y, halfSize.z),
            new Vector3(halfSize.x, halfSize.y, halfSize.z),
            new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            new Vector3(-halfSize.x, halfSize.y, -halfSize.z)
        };
    }

    private TextMesh CreateHeadquartersText(Transform parent, string name, string strength)
    {
        CreateHeadquartersTextMesh(parent, "HQ Name", name, new Vector3(0f, 0.09f, 0.36f), PlayableSceneRules.HeadquartersNameCharacterSize, TextAnchor.MiddleCenter);
        return CreateHeadquartersTextMesh(parent, "HQ Strength", strength, new Vector3(0f, 0.095f, -0.06f), PlayableSceneRules.HeadquartersStrengthCharacterSize, TextAnchor.MiddleCenter);
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
            slotObject.transform.SetParent(transform, false);
            slotObject.transform.localPosition = new Vector3(startX + x * SlotStepX, 0f, zOffset);

            BoxCollider collider = slotObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(slotWidth, 0.08f, slotHeight);

            SlotVisualize_Temp visual = slotObject.AddComponent<SlotVisualize_Temp>();
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
        CreateCheckerboardRow(SlotZone.EnemySupport, supportColumns, 1.35f, dark, light);
        CreateCheckerboardRow(SlotZone.Frontline, frontlineColumns, 0f, light, dark);
        CreateCheckerboardRow(SlotZone.PlayerSupport, supportColumns, -1.35f, dark, light);
    }

    private void CreateCheckerboardRow(SlotZone zone, int count, float zOffset, Color firstColor, Color secondColor)
    {
        float totalWidth = (count - 1) * SlotStepX;
        float supportOffset = zone == SlotZone.Frontline ? 0f : -0.58f;
        float startX = -totalWidth / 2f + supportOffset;

        for (int x = 0; x < count; x++)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.name = $"{zone}_Board_Cell_{x}";
            cell.transform.SetParent(transform, false);
            cell.transform.localPosition = new Vector3(startX + x * SlotStepX, 0.018f, zOffset);
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
        CreateLaneLabel("ENEMY SUPPORT", new Vector3(-3.1f, 0.04f, 1.35f), new Color(1f, 0.55f, 0.55f));
        CreateLaneLabel("FRONTLINE", new Vector3(-3.1f, 0.04f, 0f), new Color(1f, 0.9f, 0.25f));
        CreateLaneLabel("YOUR SUPPORT", new Vector3(-3.1f, 0.04f, -1.35f), new Color(0.55f, 0.78f, 1f));
    }

    private void CreateLaneLabel(string text, Vector3 localPosition, Color color)
    {
        GameObject labelObject = new GameObject($"Label_{text}");
        labelObject.transform.SetParent(transform, false);
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
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = labelText.Replace(" ", "_");
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localScale = new Vector3(1.8f, 0.04f, 0.55f);
        DestroyGeneratedObject(marker.GetComponent<Collider>());

        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        AssignGeneratedMaterial(renderer, color);

        CreateLaneLabel(labelText, localPosition + new Vector3(-0.62f, 0.08f, 0.02f), Color.white);
    }

    private void DestroyGeneratedObject(Object generatedObject)
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
