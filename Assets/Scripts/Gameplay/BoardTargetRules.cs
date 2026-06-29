public static class BoardTargetRules
{
    public const int HeadquartersSlotIndex = 2;
    public const float MaximumTargetReleaseDistance = 1.45f;
    public const float HeadquartersTargetRadius = 1.75f;
    public const float HeadquartersTargetBias = 0.65f;

    public static SlotZone HeadquartersTargetZone(PlayerSide headquartersOwner)
    {
        return headquartersOwner == PlayerSide.Player ? SlotZone.PlayerSupport : SlotZone.EnemySupport;
    }

    public static bool IsHeadquartersSlot(int slotIndex)
    {
        return slotIndex == HeadquartersSlotIndex;
    }

    public static bool IsHeadquartersSlot(SlotInteract slot)
    {
        return slot != null
            && slot.X == HeadquartersSlotIndex
            && (slot.Zone == SlotZone.PlayerSupport || slot.Zone == SlotZone.EnemySupport);
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
