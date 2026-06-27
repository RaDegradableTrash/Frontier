using System;

public static class UnitActionHighlightRulesTests
{
    public static int Main()
    {
        RuntimeCard supportUnit = Unit(CardZone.PlayerSupport);
        AssertTrue(UnitActionHighlightRules.ShouldHighlightAdvanceTargets(supportUnit, 1, false, PlayerSide.Player), "Ready support units with enough Kredits should highlight advance targets.");

        supportUnit.HasActed = true;
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAdvanceTargets(supportUnit, 1, false, PlayerSide.Player), "Spent support units should not highlight advance targets.");

        supportUnit.HasActed = false;
        supportUnit.AddKeyword(CardKeyword.Pinned);
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAdvanceTargets(supportUnit, 1, false, PlayerSide.Player), "Pinned support units should not highlight advance targets.");

        RuntimeCard costlySupportUnit = Unit(CardZone.PlayerSupport);
        costlySupportUnit.OperationCost = 2;
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAdvanceTargets(costlySupportUnit, 1, false, PlayerSide.Player), "Unaffordable support units should not highlight advance targets.");
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAdvanceTargets(Unit(CardZone.PlayerSupport), 1, true, PlayerSide.Enemy), "Enemy frontline control should suppress advance highlights.");

        RuntimeCard attacker = Unit(CardZone.Frontline);
        AssertTrue(UnitActionHighlightRules.ShouldHighlightAttackTargets(attacker, 1), "Ready frontline units with enough Kredits should highlight attack targets.");

        attacker.HasActed = true;
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAttackTargets(attacker, 1), "Spent frontline units should not highlight attack targets.");

        RuntimeCard costlyAttacker = Unit(CardZone.Frontline);
        costlyAttacker.OperationCost = 2;
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAttackTargets(costlyAttacker, 1), "Unaffordable frontline units should not highlight attack targets.");
        return 0;
    }

    private static RuntimeCard Unit(CardZone zone)
    {
        return new RuntimeCard
        {
            Type = CardType.Unit,
            Owner = PlayerSide.Player,
            Zone = zone,
            OperationCost = 1,
            CurrentDefense = 2
        };
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
