using UnityEngine;

public class SceneDeckSummary : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float characterSize = 0.032f;

    private TextMesh textMesh;

    private void Awake()
    {
        EnsureTextMesh();
    }

    public void UpdateSummary(string deckName, string description)
    {
        EnsureTextMesh();
        textMesh.text = DeckSummaryTextRules.BuildSummary(deckName, description);
    }

    public void Clear()
    {
        EnsureTextMesh();
        textMesh.text = string.Empty;
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
}
