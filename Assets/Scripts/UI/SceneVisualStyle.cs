using UnityEngine;

public class SceneVisualStyle : MonoBehaviour
{
    public static SceneVisualStyle Active { get; private set; }

    [SerializeField] private Material endfieldCardMaterial;
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

        return endfieldCardMaterial;
    }
}
