using UnityEngine;

public class SceneCardLayout : MonoBehaviour
{
    public Vector3 playerHandAnchor = PlayableSceneRules.PlayerHandAnchor;
    public Vector3 playerHandRevealedAnchor = PlayableSceneRules.PlayerHandRevealedAnchor;
    public Vector3 enemyHandAnchor = PlayableSceneRules.EnemyHandAnchor;
    public Vector3 playerCountermeasureAnchor = PlayableSceneRules.PlayerCountermeasureAnchor;
    public Vector3 enemyCountermeasureAnchor = PlayableSceneRules.EnemyCountermeasureAnchor;
    public float handSpacing = PlayableSceneRules.HandSpacing;
    public float revealedHandSpacing = PlayableSceneRules.RevealedHandSpacing;
    public float countermeasureSpacing = 0.42f;

    public void ApplyPlayableDefaults()
    {
        playerHandAnchor = PlayableSceneRules.PlayerHandAnchor;
        playerHandRevealedAnchor = PlayableSceneRules.PlayerHandRevealedAnchor;
        enemyHandAnchor = PlayableSceneRules.EnemyHandAnchor;
        playerCountermeasureAnchor = PlayableSceneRules.PlayerCountermeasureAnchor;
        enemyCountermeasureAnchor = PlayableSceneRules.EnemyCountermeasureAnchor;
        handSpacing = PlayableSceneRules.HandSpacing;
        revealedHandSpacing = PlayableSceneRules.RevealedHandSpacing;
    }

    public Vector3 HandPosition(PlayerSide side, int index, int count)
    {
        return HandPosition(side, index, count, false);
    }

    public Vector3 HandPosition(PlayerSide side, int index, int count, bool playerHandRevealed)
    {
        Vector3 anchor = side == PlayerSide.Player
            ? (playerHandRevealed ? playerHandRevealedAnchor : playerHandAnchor)
            : enemyHandAnchor;
        float spacing = handSpacing;
        Vector3 position = anchor + Vector3.right * CardLayoutRules.OffsetIndex(index, count) * spacing;
        if (side == PlayerSide.Player)
        {
            position += Vector3.forward * CardLayoutRules.HandFanDepthOffset(index, count);
            position += Vector3.up * CardLayoutRules.HandLayerHeightOffset(index);
        }

        return position;
    }

    public Vector3 CountermeasurePosition(PlayerSide side, int index, int count)
    {
        Vector3 anchor = side == PlayerSide.Player ? playerCountermeasureAnchor : enemyCountermeasureAnchor;
        return anchor + Vector3.right * CardLayoutRules.OffsetIndex(index, count) * countermeasureSpacing;
    }
}
