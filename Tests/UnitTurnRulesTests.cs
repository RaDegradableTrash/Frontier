using System;

public static class UnitTurnRulesTests
{
    public static int Main()
    {
        RuntimeCard readyUnit = Unit("Ready Unit");
        readyUnit.HasActed = true;
        readyUnit.AttacksThisTurn = 1;

        UnitTurnRules.ReadyForTurn(readyUnit);
        AssertTrue(!readyUnit.HasActed, "Unpinned units should ready at turn start.");
        AssertEqual(0, readyUnit.AttacksThisTurn, "Readying should reset attack count.");

        RuntimeCard pinnedUnit = Unit("Pinned Unit");
        pinnedUnit.AddKeyword(CardKeyword.Pinned);

        UnitTurnRules.ReadyForTurn(pinnedUnit);
        AssertTrue(pinnedUnit.HasActed, "Pinned units should skip their next operation window.");
        AssertTrue(!pinnedUnit.HasKeyword(CardKeyword.Pinned), "Pinned should clear after consuming the next ready step.");
        AssertEqual(0, pinnedUnit.AttacksThisTurn, "Pinned units should still reset attack count for future turns.");
        return 0;
    }

    private static RuntimeCard Unit(string name)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Unit,
            CurrentDefense = 2
        };
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
