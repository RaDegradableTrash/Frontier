using UnityEngine;

public class AttackTracer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Vector3 start;
    private Vector3 end;
    private Color color;
    private float lifetime = 0.28f;
    private float elapsed;

    public void Initialize(Vector3 startPosition, Vector3 endPosition, Color tracerColor)
    {
        start = startPosition + Vector3.up * 0.28f;
        end = endPosition + Vector3.up * 0.28f;
        color = tracerColor;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        Vector3 animatedEnd = Vector3.Lerp(start, end, t);
        lineRenderer.SetPosition(1, animatedEnd);

        Color faded = color;
        faded.a = 1f - t;
        lineRenderer.startColor = faded;
        lineRenderer.endColor = faded;

        if (elapsed >= lifetime)
        {
            RuntimeSafeDestroy.Destroy(gameObject);
        }
    }
}
