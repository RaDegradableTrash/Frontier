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

    public TextMesh titleLabel;
    public TextMesh costLabel;
    public TextMesh operationLabel;
    public TextMesh attackLabel;
    public TextMesh defenseLabel;
    public TextMesh costBadgeLabel;
    public TextMesh attackBadgeLabel;
    public TextMesh defenseBadgeLabel;
    public TextMesh statusLabel;
    public TextMesh selectionLabel;
}
