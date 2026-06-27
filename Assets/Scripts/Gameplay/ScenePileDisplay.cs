using UnityEngine;

public enum ScenePileKind
{
    Deck,
    Discard
}

public class ScenePileDisplay : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    [SerializeField] private PlayerSide side = PlayerSide.Player;
    [SerializeField] private ScenePileKind kind = ScenePileKind.Deck;
    [SerializeField] private string label = "Deck";

    private TextMesh textMesh;

    public PlayerSide Side => side;
    public ScenePileKind Kind => kind;

    private void Awake()
    {
        EnsureTextMesh();
        UpdateCount(0);
    }

    public void UpdateCount(int count)
    {
        EnsureTextMesh();
        ApplyPresentation();
        textMesh.text = $"{label.ToUpperInvariant()}\n{count}";
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        textMesh.characterSize = PlayableSceneRules.PileLabelCharacterSize;
        textMesh.color = PlayableSceneRules.PileLabelColor;
        ApplyRendererColor();
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
        textMesh.characterSize = PlayableSceneRules.PileLabelCharacterSize;
        textMesh.fontSize = 72;
        textMesh.color = PlayableSceneRules.PileLabelColor;
        ApplyRendererColor();
    }

    private void ApplyRendererColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (material != null && material.HasProperty(ColorProperty))
        {
            material.color = PlayableSceneRules.PileLabelColor;
        }
    }
}
