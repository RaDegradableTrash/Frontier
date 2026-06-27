using System.Collections.Generic;
using UnityEngine;

public class SceneStatusDisplay : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float characterSize = PlayableSceneRules.InfoPanelCharacterSize;
    [SerializeField] private int maxLogLines = PlayableSceneRules.StatusMaxLogLines;

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
        if (!PlayableSceneRules.TabletopInfoPanelsEnabled)
        {
            textMesh.text = string.Empty;
            SetRendererVisible(false);
            return;
        }

        textMesh.text = StatusSnapshotTextRules.Build(player, enemy, phase, activeSide, frontline, status, actionLog, maxLogLines);
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
