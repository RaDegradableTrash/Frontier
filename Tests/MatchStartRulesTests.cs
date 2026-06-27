using System;

public static class MatchStartRulesTests
{
    public static int Main()
    {
        AssertTrue(
            MatchStartRules.PhaseAfterAutoStart() == GamePhase.Mulligan,
            "Auto-start should enter the opening-hand mulligan phase, not skip directly to the player turn.");
        AssertTrue(
            MatchStartRules.ShouldAutoKeepOpeningHand() == false,
            "Auto-start should not auto-keep the opening hand before a new player can inspect it.");
        AssertTrue(
            MatchStartRules.ShouldForceRevealPlayerHand(GamePhase.Mulligan, PlayerSide.Player),
            "Opening-hand mulligan phase should keep the player hand visible for inspection.");
        AssertTrue(
            !MatchStartRules.ShouldForceRevealPlayerHand(GamePhase.EnemyTurn, PlayerSide.Enemy),
            "Enemy turns should not force the player hand to remain visible.");
        RuntimeCard openingHandCard = new RuntimeCard { Owner = PlayerSide.Player, Zone = CardZone.Hand };
        AssertTrue(
            MatchStartRules.ShouldInspectOnlyDuringOpeningHand(GamePhase.Mulligan, PlayerSide.Player, openingHandCard),
            "Opening-hand cards should be clickable for inspection before Keep or Mulligan.");
        AssertTrue(
            !MatchStartRules.ShouldInspectOnlyDuringOpeningHand(GamePhase.PlayerTurn, PlayerSide.Player, openingHandCard),
            "Normal player turns should allow ordinary card selection instead of inspection-only clicks.");
        return 0;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
