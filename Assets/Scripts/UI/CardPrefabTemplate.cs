using TMPro;
using UnityEngine;

public class CardPrefabTemplate : MonoBehaviour
{
    public MeshRenderer faceRenderer;
    public MeshRenderer rarityBandRenderer;
    public MeshRenderer selectionRenderer;
    public MeshRenderer dragShadowRenderer;
    public MeshRenderer costBadgeRenderer;
    public MeshRenderer operationBadgeRenderer;
    public MeshRenderer[] selectionFrameRenderers;

    public TMP_Text titleLabel;
    public TMP_Text costLabel;
    public TMP_Text operationLabel;
    public TMP_Text attackLabel;
    public TMP_Text defenseLabel;
    public TMP_Text costBadgeLabel;
    public TMP_Text attackBadgeLabel;
    public TMP_Text defenseBadgeLabel;
    public TMP_Text statusLabel;
    public TMP_Text selectionLabel;
}
