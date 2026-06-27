using System;
using System.Collections.Generic;

public static class StatusSnapshotTextRulesTests
{
    public static int Main()
    {
        PlayerState player = new PlayerState(PlayerSide.Player)
        {
            HeadquartersHealth = 18,
            Kredits = 2,
            MaxKredits = 4
        };
        PlayerState enemy = new PlayerState(PlayerSide.Enemy)
        {
            HeadquartersHealth = 12,
            Kredits = 1,
            MaxKredits = 3
        };

        string text = StatusSnapshotTextRules.Build(
            player,
            enemy,
            GamePhase.PlayerTurn,
            PlayerSide.Player,
            "Enemy",
            "NO ADVANCE — ENEMY CONTROLS THE FRONTLINE.",
            new List<string> { "Newest action", "Older action", "Hidden action" },
            2);

        AssertTrue(text.Contains("YOU HQ 18"), "Status should show player HQ.");
        AssertTrue(text.Contains("ENEMY HQ 12"), "Status should show enemy HQ.");
        AssertTrue(text.Contains("KREDIT 2/4"), "Status should show current and max Kredits.");
        AssertTrue(text.Contains("TURN YOU"), "Status should show active side.");
        AssertTrue(text.Contains("FRONT ENEMY"), "Status should show frontline control in compact player-facing terms.");
        AssertTrue(!text.Contains("STEP:"), "Status panel should not print rule-book step text on the tabletop.");
        AssertTrue(text.Contains("NO ADVANCE"), "Status should include the latest actionable message.");
        AssertTrue(!text.Contains("Newest action"), "Status should omit action-log feed text from the tabletop.");
        AssertTrue(!text.Contains("Older action"), "Status should omit old action-log feed text from the tabletop.");
        AssertTrue(!text.Contains("Hidden action"), "Status should not exceed the configured log count.");
        AssertTrue(!text.Contains("PlayerSide"), "Status should not expose raw enum type names.");
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
