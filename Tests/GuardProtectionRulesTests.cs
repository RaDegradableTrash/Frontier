using System;

public static class GuardProtectionRulesTests
{
    public static int Main()
    {
        RuntimeCard supportGuard = new RuntimeCard
        {
            Owner = PlayerSide.Enemy,
            Zone = CardZone.EnemySupport,
            Keywords = CardKeyword.Guard
        };
        AssertTrue(GuardProtectionRules.ProtectsSupportTargets(supportGuard, PlayerSide.Enemy), "Support-line Guard should protect HQ and support units.");

        RuntimeCard frontlineGuard = new RuntimeCard
        {
            Owner = PlayerSide.Enemy,
            Zone = CardZone.Frontline,
            Keywords = CardKeyword.Guard
        };
        AssertTrue(!GuardProtectionRules.ProtectsSupportTargets(frontlineGuard, PlayerSide.Enemy), "Frontline Guard should not block support/HQ attacks when frontline is not a legal target lane.");

        RuntimeCard friendlyGuard = new RuntimeCard
        {
            Owner = PlayerSide.Player,
            Zone = CardZone.PlayerSupport,
            Keywords = CardKeyword.Guard
        };
        AssertTrue(!GuardProtectionRules.ProtectsSupportTargets(friendlyGuard, PlayerSide.Enemy), "Friendly Guard should not protect the opposing headquarters.");

        RuntimeCard ordinarySupport = new RuntimeCard
        {
            Owner = PlayerSide.Enemy,
            Zone = CardZone.EnemySupport
        };
        AssertTrue(!GuardProtectionRules.ProtectsSupportTargets(ordinarySupport, PlayerSide.Enemy), "Non-Guard support units should not force target priority.");
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
