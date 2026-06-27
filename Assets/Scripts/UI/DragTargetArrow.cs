using UnityEngine;

public class DragTargetArrow : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private TextMesh label;

    public void Initialize()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.055f;
        lineRenderer.endWidth = 0.025f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startColor = new Color(1f, 0.88f, 0.28f, 1f);
        lineRenderer.endColor = new Color(1f, 0.22f, 0.08f, 1f);

        GameObject labelObject = new GameObject("Arrow Label");
        labelObject.transform.SetParent(transform, false);
        label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.035f;
        label.fontSize = 96;
        label.color = new Color(1f, 0.9f, 0.35f, 1f);
    }

    public void UpdateArrow(Vector3 start, Vector3 end, string text)
    {
        if (lineRenderer == null)
        {
            Initialize();
        }

        Vector3 raisedStart = start + Vector3.up * 0.34f;
        Vector3 raisedEnd = end + Vector3.up * 0.34f;
        lineRenderer.SetPosition(0, raisedStart);
        lineRenderer.SetPosition(1, raisedEnd);

        if (label != null)
        {
            label.text = text;
            label.transform.position = Vector3.Lerp(raisedStart, raisedEnd, 0.62f) + Vector3.up * 0.03f;
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
