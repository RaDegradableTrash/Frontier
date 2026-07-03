public static class DragTargetLabelRules
{
    public static string LabelFor(RuntimeCard card, SlotInteract targetSlot, bool legalAttackTarget)
    {
        if (legalAttackTarget)
        {
            return "ATTACK";
        }

        if (card != null && targetSlot != null && targetSlot.IsOccupied)
        {
            return "TARGET";
        }

        return LabelFor(card, targetSlot != null ? targetSlot.Zone : (SlotZone?)null, legalAttackTarget);
    }

    public static string LabelFor(RuntimeCard card, SlotZone? targetZone, bool legalAttackTarget)
    {
        if (card == null)
        {
            return "TARGET";
        }

        if (card.Zone == CardZone.PlayerSupport || card.Zone == CardZone.EnemySupport)
        {
            return "MOVE";
        }

        if (card.Zone == CardZone.Frontline)
        {
            return legalAttackTarget ? "ATTACK" : "TARGET";
        }

        return "TARGET";
    }
}
