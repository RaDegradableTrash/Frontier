using UnityEngine;

public class CuePulse : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Color baseColor;
    private float lifetime = 0.45f;
    private float elapsed;

    public void Initialize(Color color, float duration)
    {
        baseColor = color;
        lifetime = duration;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        transform.localScale = Vector3.Lerp(transform.localScale, transform.localScale * 1.08f, Time.deltaTime * 8f);

        if (meshRenderer != null)
        {
            Color color = baseColor;
            color.a = 1f - t;
            meshRenderer.material.color = color;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
