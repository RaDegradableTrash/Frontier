using UnityEngine;

public class SlotVisualize_Temp : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private MeshRenderer fillRenderer;
    private Material fillMaterial;
    private TextMesh highlightLabel;
    private MeshRenderer highlightLabelBackingRenderer;
    private Color baseColor;

    public void Setup(Vector3[] localPoints, Material material, Color color)
    {
        EnsureFill(localPoints, color);
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = material;
        lineRenderer.positionCount = localPoints.Length;
        lineRenderer.loop = true;
        lineRenderer.startWidth = lineRenderer.endWidth = 0.035f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPositions(localPoints);
        baseColor = color;
        UpdateColor(color);
    }

    public void SetHighlighted(bool highlighted)
    {
        SetHighlighted(highlighted, PlayableSceneRules.HighlightedSlotLabel);
    }

    public void SetHighlighted(bool highlighted, string label)
    {
        UpdateColor(highlighted ? Color.white : baseColor, highlighted);
        SetHighlightLabelVisible(highlighted, label);
    }

    public void UpdateColor(Color color)
    {
        UpdateColor(color, false);
    }

    private void UpdateColor(Color color, bool highlighted)
    {
        if (fillMaterial != null)
        {
            fillMaterial.color = FillColor(color, highlighted);
        }

        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void EnsureFill(Vector3[] localPoints, Color color)
    {
        if (fillRenderer != null || localPoints == null || localPoints.Length < 4)
        {
            return;
        }

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Slot Fill";
        fill.transform.SetParent(transform, false);
        fill.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        fill.transform.localScale = new Vector3(Mathf.Abs(localPoints[1].x - localPoints[0].x), 0.012f, Mathf.Abs(localPoints[0].z - localPoints[2].z));
        DestroyGeneratedObject(fill.GetComponent<Collider>());

        fillRenderer = fill.GetComponent<MeshRenderer>();
        fillMaterial = new Material(Shader.Find("Unlit/Color"));
        fillMaterial.color = FillColor(color);
        fillRenderer.material = fillMaterial;
    }

    private void SetHighlightLabelVisible(bool visible, string label)
    {
        if (!PlayableSceneRules.HighlightedSlotLabelEnabled)
        {
            return;
        }

        EnsureHighlightLabel();
        if (highlightLabel == null)
        {
            return;
        }

        highlightLabel.text = visible ? label : string.Empty;
        Renderer renderer = highlightLabel.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }

        if (highlightLabelBackingRenderer != null)
        {
            highlightLabelBackingRenderer.enabled = visible;
        }
    }

    private void EnsureHighlightLabel()
    {
        if (highlightLabel != null)
        {
            return;
        }

        GameObject labelObject = new GameObject("Highlight Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        highlightLabel = labelObject.AddComponent<TextMesh>();
        highlightLabel.anchor = TextAnchor.MiddleCenter;
        highlightLabel.alignment = TextAlignment.Center;
        highlightLabel.fontSize = 72;
        highlightLabel.characterSize = PlayableSceneRules.HighlightedSlotLabelCharacterSize;
        highlightLabel.color = new Color(1f, 0.95f, 0.45f, 1f);
        highlightLabel.text = string.Empty;

        EnsureHighlightLabelBacking(labelObject.transform);

        Renderer renderer = highlightLabel.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private void EnsureHighlightLabelBacking(Transform labelTransform)
    {
        if (!PlayableSceneRules.HighlightedSlotLabelBackingEnabled || highlightLabelBackingRenderer != null)
        {
            return;
        }

        GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backing.name = "Highlight Label Backing";
        backing.transform.SetParent(labelTransform, false);
        backing.transform.localPosition = new Vector3(0f, 0.012f, 0.018f);
        backing.transform.localScale = new Vector3(
            PlayableSceneRules.HighlightedSlotLabelBackingScale.x,
            0.018f,
            PlayableSceneRules.HighlightedSlotLabelBackingScale.y);
        DestroyGeneratedObject(backing.GetComponent<Collider>());

        highlightLabelBackingRenderer = backing.GetComponent<MeshRenderer>();
        Material backingMaterial = new Material(Shader.Find("Unlit/Color"));
        backingMaterial.color = PlayableSceneRules.HighlightedSlotLabelBackingColor;
        highlightLabelBackingRenderer.material = backingMaterial;
        highlightLabelBackingRenderer.enabled = false;
    }

    private Color FillColor(Color color)
    {
        return FillColor(color, false);
    }

    private Color FillColor(Color color, bool highlighted)
    {
        if (highlighted)
        {
            return Color.Lerp(PlayableSceneRules.TabletopColor * 0.72f, Color.white, 0.12f);
        }

        return Color.Lerp(PlayableSceneRules.TabletopColor * 0.72f, color, 0.26f);
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
}
