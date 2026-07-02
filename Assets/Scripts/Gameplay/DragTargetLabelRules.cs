public static class DragTargetLabelRules
{
    public static string LabelFor(RuntimeCard card, SlotInteract targetSlot, bool legalAttackTarget)
    {
        if (legalAttackTarget)
        {
            return "ATTACK";
        }

        if (card != null
            && card.Zone == CardZone.PlayerSupport
            && targetSlot != null
            && targetSlot.Zone == SlotZone.Frontline
            && targetSlot.IsOccupied)
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

        if (card.Zone == CardZone.PlayerSupport)
        {
            return targetZone == SlotZone.Frontline ? "ADVANCE" : "MOVE";
        }

        if (card.Zone == CardZone.Frontline)
        {
            return legalAttackTarget ? "ATTACK" : "TARGET";
        }

        return "TARGET";
    }
}
