using UnityEngine;

public class SceneCommandButton : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    [SerializeField] private GameController controller;
    [SerializeField] private SceneCommandType command = SceneCommandType.StartMatch;
    [SerializeField] private string label = "Command";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.88f, 0.96f, 1f, 1f);
    [SerializeField] private Color plateColor = new Color(0.08f, 0.1f, 0.12f, 0.9f);
    [SerializeField] private Color plateHoverColor = new Color(0.14f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color disabledLabelColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color disabledPlateColor = new Color(0.03f, 0.03f, 0.035f, 0.85f);
    [SerializeField] private Vector2 plateSize = PlayableSceneRules.CommandButtonPlateSize;
    [SerializeField] private float labelCharacterSize = PlayableSceneRules.CommandButtonCharacterSize;

    private TextMesh textMesh;
    private MeshRenderer plateRenderer;
    private Material plateMaterial;
    private Collider hitCollider;
    private bool isAvailable = true;
    private bool isVisible = true;
    private bool isHovering;

    public SceneCommandType Command => command;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<GameController>();
        }

        EnsureText();
        EnsureCollider();
        EnsurePlate();
        ApplyPresentation();
        SetHover(false);
    }

    private void OnMouseDown()
    {
        SetHover(true);
    }

    private void OnMouseUpAsButton()
    {
        if (SceneCommandRules.ShouldForwardVisibleClick(isVisible) && controller != null)
        {
            controller.ExecuteSceneCommand(command);
        }
    }

    private void OnMouseEnter()
    {
        isHovering = true;
        RefreshVisualState();
    }

    private void OnMouseExit()
    {
        isHovering = false;
        RefreshVisualState();
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        RefreshVisualState();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        EnsureText();
        EnsureCollider();
        EnsurePlate();
        ApplyPresentation();

        textMesh.text = visible ? label : string.Empty;
        ClearStaleChildLabels();
        plateRenderer.enabled = visible;
        hitCollider.enabled = visible;
    }

    public void ApplyPresentation()
    {
        EnsureText();
        EnsureCollider();
        EnsurePlate();

        labelCharacterSize = PlayableSceneRules.CommandButtonCharacterSize;
        plateSize = PlayableSceneRules.CommandButtonPlateSize;
        textMesh.characterSize = labelCharacterSize;
        if (hitCollider is BoxCollider box)
        {
            box.size = new Vector3(plateSize.x, plateSize.y, 0.1f);
        }

        Transform existingPlate = transform.Find("Button Plate");
        if (existingPlate != null)
        {
            existingPlate.localScale = new Vector3(plateSize.x, plateSize.y, 1f);
            existingPlate.localPosition = new Vector3(0f, 0f, PlayableSceneRules.CommandButtonPlateLocalZ);
        }

    }

    private void SetHover(bool isHovering)
    {
        this.isHovering = isHovering;
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        EnsureText();
        EnsurePlate();
        if (!isAvailable)
        {
            ApplyTextColor(disabledLabelColor);
            plateMaterial.color = disabledPlateColor;
            return;
        }

        ApplyTextColor(isHovering ? hoverColor : normalColor);
        plateMaterial.color = isHovering ? plateHoverColor : plateColor;
    }

    private void ApplyTextColor(Color color)
    {
        textMesh.color = color;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (material != null && material.HasProperty(ColorProperty))
        {
            material.color = color;
        }
    }

    private void EnsureText()
    {
        if (textMesh != null)
        {
            return;
        }

        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        ClearStaleChildLabels();
        textMesh.text = label;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = labelCharacterSize;
        textMesh.fontSize = 72;
    }

    private void EnsureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            box = gameObject.AddComponent<BoxCollider>();
        }

        box.size = new Vector3(plateSize.x, plateSize.y, 0.1f);
        hitCollider = box;
    }

    private void ClearStaleChildLabels()
    {
        Transform staleLabel = transform.Find("Button Label");
        if (staleLabel == null)
        {
            return;
        }

        TextMesh staleText = staleLabel.GetComponent<TextMesh>();
        if (staleText != null)
        {
            staleText.text = string.Empty;
        }

        Renderer staleRenderer = staleLabel.GetComponent<Renderer>();
        if (staleRenderer != null)
        {
            staleRenderer.enabled = false;
        }
    }

    private void EnsurePlate()
    {
        if (plateRenderer != null)
        {
            return;
        }

        Transform existingPlate = transform.Find("Button Plate");
        GameObject plateObject;
        if (existingPlate != null)
        {
            plateObject = existingPlate.gameObject;
        }
        else
        {
            plateObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plateObject.name = "Button Plate";
            plateObject.transform.SetParent(transform, false);
            Collider generatedCollider = plateObject.GetComponent<Collider>();
            if (generatedCollider != null)
            {
                DestroyGeneratedObject(generatedCollider);
            }
        }

        plateObject.transform.localPosition = new Vector3(0f, 0f, PlayableSceneRules.CommandButtonPlateLocalZ);
        plateObject.transform.localRotation = Quaternion.identity;
        plateObject.transform.localScale = new Vector3(plateSize.x, plateSize.y, 1f);
        plateRenderer = plateObject.GetComponent<MeshRenderer>();
        plateMaterial = new Material(Shader.Find("Standard"));
        plateMaterial.color = plateColor;
        if (Application.isPlaying)
        {
            plateRenderer.material = plateMaterial;
        }
        else
        {
            plateRenderer.sharedMaterial = plateMaterial;
        }
    }

    private void DestroyGeneratedObject(UnityEngine.Object generatedObject)
    {
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
}
