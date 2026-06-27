using UnityEngine;

public class SceneCardInspector : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float characterSize = PlayableSceneRules.InfoPanelCharacterSize;

    private TextMesh textMesh;

    private void Awake()
    {
        EnsureTextMesh();
        ShowCard(null);
    }

    public void ShowCard(RuntimeCard card)
    {
        EnsureTextMesh();
        if (!PlayableSceneRules.TabletopInfoPanelsEnabled)
        {
            textMesh.text = string.Empty;
            SetRendererVisible(false);
            return;
        }

        if (card == null)
        {
            textMesh.text = CardInspectorTextRules.EmptyHint();
            SetRendererVisible(true);
            return;
        }

        textMesh.text = CardInspectorTextRules.ForCard(card);
        SetRendererVisible(true);
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        textMesh.characterSize = PlayableSceneRules.InfoPanelCharacterSize;
        textMesh.color = color;
        SetRendererVisible(PlayableSceneRules.TabletopInfoPanelsEnabled);
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

        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 72;
        textMesh.color = color;
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
