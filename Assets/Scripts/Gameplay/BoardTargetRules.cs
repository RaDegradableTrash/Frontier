public static class BoardTargetRules
{
    public const int HeadquartersSlotIndex = -1;
    public const float MaximumTargetReleaseDistance = 1.45f;

    public static SlotZone HeadquartersTargetZone(PlayerSide headquartersOwner)
    {
        return headquartersOwner == PlayerSide.Player ? SlotZone.PlayerSupport : SlotZone.EnemySupport;
    }

    public static bool IsHeadquartersSlot(int slotIndex)
    {
        return slotIndex == HeadquartersSlotIndex;
    }

    public static bool ShouldReplaceClosestTarget(float candidateDistance, float currentBestDistance)
    {
        return candidateDistance < currentBestDistance;
    }

    public static bool ShouldAcceptClosestTarget(float closestDistance)
    {
        return closestDistance <= MaximumTargetReleaseDistance;
    }
}
