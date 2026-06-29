using System.Collections.Generic;
using UnityEngine;

public class SceneStatusDisplay : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float characterSize = PlayableSceneRules.InfoPanelCharacterSize;

    private TextMesh textMesh;

    private void Awake()
    {
        EnsureTextMesh();
    }

    public void UpdateSnapshot(
        PlayerState player,
        PlayerState enemy,
        GamePhase phase,
        PlayerSide activeSide,
        string frontline,
        string status,
        IList<string> actionLog)
    {
        EnsureTextMesh();
        textMesh.text = string.Empty;
        SetRendererVisible(false);
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        textMesh.characterSize = PlayableSceneRules.InfoPanelCharacterSize;
        textMesh.color = color;
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
