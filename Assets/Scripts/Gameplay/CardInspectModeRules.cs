public static class CardInspectModeRules
{
    public static bool ShouldEnterInspectMode(GamePhase phase, PlayerSide activeSide, RuntimeCard clicked, RuntimeCard currentInspectCard)
    {
        return clicked != null
            && clicked.Owner == PlayerSide.Player
            && clicked.Zone == CardZone.Hand
            && (currentInspectCard == null || currentInspectCard.Id != clicked.Id);
    }

    public static bool ShouldExitInspectMode(RuntimeCard clicked, RuntimeCard currentInspectCard)
    {
        return currentInspectCard != null
            && clicked != null
            && currentInspectCard.Id == clicked.Id;
    }

    public static bool IsInspecting(RuntimeCard currentInspectCard)
    {
        return currentInspectCard != null;
    }
}
