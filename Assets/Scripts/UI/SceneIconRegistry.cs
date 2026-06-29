using UnityEngine;

public class SceneIconRegistry : MonoBehaviour
{
    public static SceneIconRegistry Active { get; private set; }

    [SerializeField] private Texture2D discardThisCardIcon;
    [SerializeField] private Texture2D estimatedDeathSkullIcon;

    private void Awake()
    {
        Active = this;
        discardThisCardIcon = discardThisCardIcon ?? LoadIcon("DiscardThisCard");
        estimatedDeathSkullIcon = estimatedDeathSkullIcon ?? LoadIcon("EstimatedDeathSkull");
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    public Texture2D DiscardThisCardIcon => discardThisCardIcon;
    public Texture2D EstimatedDeathSkullIcon => estimatedDeathSkullIcon;

    private static Texture2D LoadIcon(string iconName)
    {
        return Resources.Load<Texture2D>($"Icons/{iconName}");
    }
}
