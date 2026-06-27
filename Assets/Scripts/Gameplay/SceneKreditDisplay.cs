using UnityEngine;

public class SceneKreditDisplay : MonoBehaviour
{
    [SerializeField] private PlayerSide side = PlayerSide.Player;

    private TextMesh textMesh;

    public PlayerSide Side => side;

    public void Initialize(PlayerSide displaySide)
    {
        side = displaySide;
        ApplyPresentation();
    }

    public void UpdateKredits(PlayerState state)
    {
        EnsureTextMesh();
        textMesh.text = KreditDisplayTextRules.Build(state);
    }

    public void ApplyPresentation()
    {
        EnsureTextMesh();
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = PlayableSceneRules.KreditDisplayCharacterSize;
        textMesh.fontSize = 96;
        textMesh.color = PlayableSceneRules.KreditDisplayTextColor;
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
    }
}
