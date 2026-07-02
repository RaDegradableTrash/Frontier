using TMPro;
using UnityEngine;

public class SceneCardInspector : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float characterSize = PlayableSceneRules.InfoPanelCharacterSize;

    private TMP_Text textMesh;
    private bool hasCard;

    public bool HasCard => hasCard;

    private void Awake()
    {
        EnsureTextMesh();
        ShowCard(null);
    }

    public void ShowCard(RuntimeCard card)
    {
        EnsureTextMesh();
        if (card == null)
        {
            hasCard = false;
            textMesh.text = string.Empty;
            SetRendererVisible(false);
            return;
        }

        hasCard = true;
        textMesh.text = BuildDetailText(card);
        SetRendererVisible(true);
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        textMesh.fontSize = PlayableSceneRules.InfoPanelCharacterSize * 12f;
        textMesh.color = color;
        SetRendererVisible(!string.IsNullOrEmpty(textMesh.text));
    }

    private void EnsureTextMesh()
    {
        if (textMesh != null)
        {
            return;
        }

        TextMesh legacyText = GetComponent<TextMesh>();
        if (legacyText != null)
        {
            legacyText.text = string.Empty;
            RuntimeSafeDestroy.Destroy(legacyText);
        }

        textMesh = GetComponent<TMP_Text>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
        }

        textMesh.alignment = TextAlignmentOptions.TopLeft;
        textMesh.fontSize = characterSize * 12f;
        textMesh.enableWordWrapping = true;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.color = color;
        CardTmpFont.Apply(textMesh);
    }

    private void SetRendererVisible(bool visible)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private static string BuildDetailText(RuntimeCard card)
    {
        string text = CardTextRules.DisplayCardName(card);
        text += $"\n{TypeLabel(card.Type)}";
        text += $"\n部署: {card.KreditCost}K";
        if (card.Type == CardType.Unit)
        {
            text += $"    行动: {card.OperationCost}K";
            text += $"\n攻击: {card.Attack}    防御: {card.CurrentDefense}/{card.Defense}";
        }

        string keywords = KeywordLine(card);
        if (!string.IsNullOrEmpty(keywords))
        {
            text += $"\n\n词条\n{keywords}";
        }

        if (!string.IsNullOrWhiteSpace(card.RulesText))
        {
            text += $"\n\n特殊能力\n{card.RulesText}";
        }

        return text;
    }

    private static string TypeLabel(CardType type)
    {
        switch (type)
        {
            case CardType.Unit:
                return "单位";
            case CardType.Order:
                return "指令";
            case CardType.Countermeasure:
                return "反制";
            default:
                return "卡牌";
        }
    }

    private static string KeywordLine(RuntimeCard card)
    {
        if (card == null || card.Keywords == CardKeyword.None)
        {
            return string.Empty;
        }

        string text = string.Empty;
        AppendKeyword(card, CardKeyword.Blitz, "闪击", ref text);
        AppendKeyword(card, CardKeyword.Guard, "守护", ref text);
        AppendKeyword(card, CardKeyword.Smokescreen, "烟幕", ref text);
        AppendKeyword(card, CardKeyword.Ambush, "伏击", ref text);
        AppendKeyword(card, CardKeyword.Fury, "狂怒", ref text);
        return text;
    }

    private static void AppendKeyword(RuntimeCard card, CardKeyword keyword, string label, ref string text)
    {
        if (!card.HasKeyword(keyword))
        {
            return;
        }

        text = string.IsNullOrEmpty(text) ? label : $"{text} / {label}";
    }
}
