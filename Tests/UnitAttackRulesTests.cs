using System;

public static class UnitAttackRulesTests
{
    public static int Main()
    {
        RuntimeCard ordinaryUnit = new RuntimeCard { CardName = "Riflemen", Type = CardType.Unit };
        RuntimeCard furyUnit = new RuntimeCard { CardName = "Field Artillery", Type = CardType.Unit, Keywords = CardKeyword.Fury };

        UnitAttackRules.MarkAttackResolved(ordinaryUnit);
        AssertEqual(1, ordinaryUnit.AttacksThisTurn, "Ordinary unit should count one resolved attack.");
        AssertTrue(ordinaryUnit.HasActed, "Ordinary unit should be spent after one attack.");

        UnitAttackRules.MarkAttackResolved(furyUnit);
        AssertEqual(1, furyUnit.AttacksThisTurn, "Fury unit should count exactly one attack after first attack.");
        AssertTrue(!furyUnit.HasActed, "Fury unit should remain ready after first attack.");

        UnitAttackRules.MarkAttackResolved(furyUnit);
        AssertEqual(2, furyUnit.AttacksThisTurn, "Fury unit should count two attacks after second attack.");
        AssertTrue(furyUnit.HasActed, "Fury unit should be spent after second attack.");
        return 0;
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
