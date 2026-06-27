using System;

public static class SceneCommandRulesTests
{
    public static int Main()
    {
        AssertTrue(
            SceneCommandRules.IsVisible(SceneCommandType.StartMatch, GamePhase.DeckBuilder, false, true),
            "Deck builder should show START.");
        AssertTrue(
            !SceneCommandRules.IsVisible(SceneCommandType.StrikeBoard, GamePhase.DeckBuilder, false, false),
            "Deck builder should not show STRIKE as an available action.");
        AssertTrue(
            SceneCommandRules.IsAvailable(SceneCommandType.StrikeBoard, GamePhase.PlayerTurn, PlayerSide.Player, false, true, true, false),
            "STRIKE debug action should only be available during the player's turn when a board exists.");
        AssertTrue(
            !SceneCommandRules.IsVisible(SceneCommandType.StrikeBoard, GamePhase.PlayerTurn, true, false),
            "STRIKE debug action should not appear in the normal player UI.");
        AssertTrue(
            SceneCommandRules.IsVisible(SceneCommandType.StrikeBoard, GamePhase.PlayerTurn, true, true),
            "STRIKE debug action should remain visible only when debug commands are enabled.");
        AssertTrue(
            !SceneCommandRules.IsAvailable(SceneCommandType.StrikeBoard, GamePhase.DeckBuilder, PlayerSide.Player, false, true, true, false),
            "STRIKE should not be available before the match starts.");
        AssertTrue(
            SceneCommandRules.ShouldForwardVisibleClick(true),
            "Visible disabled command buttons should still forward clicks so the status panel can explain why they are unavailable.");
        AssertTrue(
            !SceneCommandRules.ShouldForwardVisibleClick(false),
            "Hidden command buttons should not forward clicks.");
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
