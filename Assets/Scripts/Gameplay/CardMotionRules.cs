public static class CardMotionRules
{
    public const float MoveLerpSpeed = 14f;
    public const float ScaleLerpSpeed = 12f;
    public const float SpawnScaleMultiplier = 1f;
    public const float SnapDistance = 0.01f;
    public const float AttackLungeDistanceRatio = 0.24f;
    public const float AttackLungeReturnSeconds = 0.22f;
    public const float DeployDropSeconds = 0.46f;
    public const float DrawFlightSeconds = 0.86f;
    public const float MulliganDiscardFlightSeconds = 0.62f;
    public const float DrawExtractSecondsRatio = 0.22f;
    public const float DrawSettleSecondsRatio = 0.24f;
    public const float DrawExtractHeight = 0.36f;
    public const float DrawFlightArcHeight = 0.72f;
    public const float DrawSettleLift = 0.18f;
    public const float DiscardFlightArcHeight = 0.54f;
    public const float DiscardTuckDistance = 0.34f;
    public const float FailedReturnSeconds = 0.92f;
    public const float FailedReturnHopHeight = 0.24f;
    public const float FailedReturnSettleHeight = 0.20f;
    public const float FailedReturnSettleSeconds = 0.34f;
    public const float SelectedScaleMultiplier = 1.03f;
    public const float HoverScaleMultiplier = 1.04f;
    public const float HoverLift = 0.20f;

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
