using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 1.1f;
    private float elapsed;
    private Vector3 velocity = new Vector3(0f, 0.75f, 0f);

    public void Initialize(string text, Color color, float size = PlayableSceneRules.FloatingTextCharacterSize)
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = size;
        textMesh.fontSize = 64;
        transform.rotation = Quaternion.Euler(70f, 0f, 0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (textMesh != null)
        {
            Color color = textMesh.color;
            color.a = Mathf.Clamp01(1f - elapsed / lifetime);
            textMesh.color = color;
        }

        if (elapsed >= lifetime)
        {
            RuntimeSafeDestroy.Destroy(gameObject);
        }
    }
}
