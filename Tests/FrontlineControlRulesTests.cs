using System;

public static class FrontlineControlRulesTests
{
    public static int Main()
    {
        FrontlineControlResult empty = FrontlineControlRules.Resolve(false, false);
        AssertTrue(!empty.HasController, "Empty frontline should be neutral.");

        FrontlineControlResult playerOnly = FrontlineControlRules.Resolve(true, false);
        AssertTrue(playerOnly.HasController, "Player-only frontline should be controlled.");
        AssertTrue(playerOnly.Controller == PlayerSide.Player, "Player-only frontline should be controlled by the player.");

        FrontlineControlResult enemyOnly = FrontlineControlRules.Resolve(false, true);
        AssertTrue(enemyOnly.HasController, "Enemy-only frontline should be controlled.");
        AssertTrue(enemyOnly.Controller == PlayerSide.Enemy, "Enemy-only frontline should be controlled by the enemy.");

        FrontlineControlResult contested = FrontlineControlRules.Resolve(true, true);
        AssertTrue(!contested.HasController, "Contested frontline should not be owned by whichever unit is scanned first.");
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
