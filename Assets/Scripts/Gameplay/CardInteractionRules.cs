public static class CardInteractionRules
{
    public static bool ShouldHoldPlayerHandOpen(RuntimeCard card, bool hidden)
    {
        return !hidden
            && card != null
            && card.Owner == PlayerSide.Player
            && card.Zone == CardZone.Hand;
    }

    public static bool ShouldReleasePlayerHandHold(bool isDragging)
    {
        return !isDragging;
    }

    public static bool ShouldReleaseHeldPlayerHand(bool isHoldingPlayerHandOpen)
    {
        return isHoldingPlayerHandOpen;
    }
}
