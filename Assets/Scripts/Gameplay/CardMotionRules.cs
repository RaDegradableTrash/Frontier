public static class CardMotionRules
{
    public const float MoveLerpSpeed = 14f;
    public const float ScaleLerpSpeed = 12f;
    public const float SpawnScaleMultiplier = 0.82f;
    public const float SnapDistance = 0.01f;
    public const float AttackLungeDistanceRatio = 0.24f;
    public const float AttackLungeReturnSeconds = 0.22f;

    public static bool ShouldSnapToTarget(float distance)
    {
        return distance <= SnapDistance;
    }

    public static bool ShouldAnimatePosition(bool isDragging, bool hasTargetPosition)
    {
        return hasTargetPosition && !isDragging;
    }

    public static bool ShouldAnimateLayout(bool hasPreviousLayout, bool requestedAnimation)
    {
        return hasPreviousLayout && requestedAnimation;
    }

    public static bool ShouldApplyLunge(bool isDragging, bool hasTargetPosition)
    {
        return !isDragging && !hasTargetPosition;
    }
}
