using System;

public static class UnitDeploymentRulesTests
{
    public static int Main()
    {
        RuntimeCard ordinaryUnit = Unit("Riflemen", CardKeyword.None);
        UnitDeploymentRules.MarkDeployed(ordinaryUnit);
        AssertTrue(ordinaryUnit.HasActed, "Ordinary deployed units should wait until the next turn before operating.");
        AssertEqual(0, ordinaryUnit.AttacksThisTurn, "Deploying should not consume an attack counter.");

        RuntimeCard blitzUnit = Unit("Commandos", CardKeyword.Blitz);
        UnitDeploymentRules.MarkDeployed(blitzUnit);
        AssertTrue(!blitzUnit.HasActed, "Blitz deployed units should be immediately ready.");
        AssertEqual(0, blitzUnit.AttacksThisTurn, "Blitz deployed units should start with a clean attack counter.");

        RuntimeCard nonUnit = new RuntimeCard { CardName = "Order", Type = CardType.Order };
        UnitDeploymentRules.MarkDeployed(nonUnit);
        AssertTrue(!nonUnit.HasActed, "Non-units should not receive unit deployment readiness.");
        return 0;
    }

    private static RuntimeCard Unit(string name, CardKeyword keywords)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Unit,
            Keywords = keywords,
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
