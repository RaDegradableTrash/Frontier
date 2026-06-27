using UnityEngine;

public class SceneVisualStyle : MonoBehaviour
{
    public static SceneVisualStyle Active { get; private set; }

    [SerializeField] private Material britainCardMaterial;
    [SerializeField] private Material usaCardMaterial;
    [SerializeField] private Material germanyCardMaterial;
    [SerializeField] private Material sovietCardMaterial;
    [SerializeField] private Material japanCardMaterial;
    [SerializeField] private Material countermeasureCardMaterial;

    private void Awake()
    {
        Active = this;
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    public Material CardMaterialFor(RuntimeCard card)
    {
        if (card != null && card.Type == CardType.Countermeasure && countermeasureCardMaterial != null)
        {
            return countermeasureCardMaterial;
        }

        switch (card != null ? card.Faction : CardFaction.Britain)
        {
            case CardFaction.USA:
                return usaCardMaterial;
            case CardFaction.Germany:
                return germanyCardMaterial;
            case CardFaction.Soviet:
                return sovietCardMaterial;
            case CardFaction.Japan:
                return japanCardMaterial;
            default:
                return britainCardMaterial;
        }
    }
}
