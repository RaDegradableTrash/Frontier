using UnityEngine;

public class SceneActionPrompt : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    [SerializeField] private Color color = new Color(1f, 0.95f, 0.72f, 1f);
    [SerializeField] private float characterSize = PlayableSceneRules.ActionPromptCharacterSize;

    private TextMesh textMesh;

    private void Awake()
    {
        EnsureTextMesh();
    }

    public void UpdatePrompt(GamePhase phase, PlayerSide activeSide)
    {
        UpdatePrompt(phase, activeSide, false);
    }

    public void UpdatePrompt(GamePhase phase, PlayerSide activeSide, bool mulliganUsed)
    {
        EnsureTextMesh();
        textMesh.text = string.Empty;
        SetRendererVisible(false);
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        transform.position = PlayableSceneRules.ActionPromptPosition;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        textMesh.characterSize = PlayableSceneRules.ActionPromptCharacterSize;
        textMesh.color = color;
        ApplyUnlitTextMaterial();
        textMesh.text = string.Empty;
        SetRendererVisible(false);
    }

    private void EnsureTextMesh()
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

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 96;
        textMesh.color = color;
        ApplyUnlitTextMaterial();
    }

    private void ApplyUnlitTextMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null || textMesh == null || textMesh.font == null)
        {
            return;
        }

        Shader shader = Shader.Find("GUI/Text Shader");
        Material fontMaterial = textMesh.font.material;
        Material material = renderer.sharedMaterial;
        if (shader != null && (material == null || material.shader != shader || material.mainTexture == null))
        {
            material = fontMaterial != null ? new Material(fontMaterial) : new Material(shader);
            material.shader = shader;
            renderer.sharedMaterial = material;
        }

        if (material != null && material.HasProperty(ColorProperty))
        {
            material.color = color;
        }
    }

    private void SetRendererVisible(bool visible)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }
}
