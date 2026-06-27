using System;

public static class SceneHudPromptRulesTests
{
    public static int Main()
    {
        RuntimeCard supportUnit = new RuntimeCard
        {
            CardName = "M5 Stuart",
            Type = CardType.Unit,
            Zone = CardZone.PlayerSupport,
            Owner = PlayerSide.Player,
            OperationCost = 1
        };

        string blockedPrompt = SceneHudPromptRules.Prompt(
            supportUnit,
            3,
            true,
            PlayerSide.Enemy,
            GamePhase.PlayerTurn,
            PlayerSide.Player,
            false);

        AssertTrue(blockedPrompt.Length <= 42, "HUD selected-card prompt should be compact and not cover the battlefield.");
        AssertTrue(blockedPrompt.Contains("FRONTLINE"), "HUD selected-card prompt should preserve the key blocker.");
        AssertTrue(!blockedPrompt.Contains("CLICK ADVANCE HERE"), "HUD should not ask for impossible advance targets.");

        string mulliganPrompt = SceneHudPromptRules.Prompt(null, 0, false, PlayerSide.Player, GamePhase.Mulligan, PlayerSide.Player, true);
        AssertTrue(mulliganPrompt.Contains("KEEP HAND"), "HUD should preserve mulligan-used opening-hand prompt.");

        string turnPrompt = SceneHudPromptRules.Prompt(null, 0, false, PlayerSide.Player, GamePhase.PlayerTurn, PlayerSide.Player, false);
        AssertTrue(turnPrompt.Contains("YOUR TURN"), "HUD should preserve generic phase prompt when no card is selected.");
        AssertTrue(turnPrompt.Length <= 18, "Generic HUD prompt should remain a short state label.");
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
