public static class MatchStartRules
{
    public static GamePhase PhaseAfterAutoStart()
    {
        return GamePhase.Mulligan;
    }

    public static bool ShouldAutoKeepOpeningHand()
    {
        return false;
    }

    public static bool ShouldForceRevealPlayerHand(GamePhase phase, PlayerSide activeSide)
    {
        return phase == GamePhase.Mulligan && activeSide == PlayerSide.Player;
    }

    public static bool ShouldInspectOnlyDuringOpeningHand(GamePhase phase, PlayerSide activeSide, RuntimeCard card)
    {
        return phase == GamePhase.Mulligan
            && activeSide == PlayerSide.Player
            && card != null
            && card.Owner == PlayerSide.Player
            && card.Zone == CardZone.Hand;
    }
}
