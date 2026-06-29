using System;

public static class CardOperationBadgeRulesTests
{
    public static int Main()
    {
        RuntimeCard readyUnit = Unit(CardZone.PlayerSupport, 2, false, CardKeyword.None);
        RuntimeCard expensiveUnit = Unit(CardZone.PlayerSupport, 3, false, CardKeyword.None);
        RuntimeCard spentUnit = Unit(CardZone.Frontline, 1, true, CardKeyword.None);
        RuntimeCard pinnedUnit = Unit(CardZone.Frontline, 1, false, CardKeyword.Pinned);
        RuntimeCard handUnit = Unit(CardZone.Hand, 1, false, CardKeyword.None);

        AssertEqual("2", CardOperationBadgeRules.Text(readyUnit, 2), "Ready units should show operation cost as a Kards-like corner number.");
        AssertEqual(CardOperationBadgeState.Ready, CardOperationBadgeRules.State(readyUnit, 2), "Affordable ready units should be marked ready.");
        AssertEqual(CardOperationBadgeState.NeedKredits, CardOperationBadgeRules.State(expensiveUnit, 2), "Unaffordable units should show that operation cost is short.");
        AssertEqual(CardOperationBadgeState.Spent, CardOperationBadgeRules.State(spentUnit, 5), "Units that already acted should show spent state.");
        AssertEqual(CardOperationBadgeState.Pinned, CardOperationBadgeRules.State(pinnedUnit, 5), "Pinned units should show pinned state.");
        AssertEqual(string.Empty, CardOperationBadgeRules.Text(handUnit, 5), "Hand cards should not show operation cost badge.");
        return 0;
    }

    private static RuntimeCard Unit(CardZone zone, int operationCost, bool hasActed, CardKeyword keywords)
    {
        return new RuntimeCard
        {
            CardName = "Forward Scouts",
            Type = CardType.Unit,
            Owner = PlayerSide.Player,
            Zone = zone,
            OperationCost = operationCost,
            HasActed = hasActed,
            Keywords = keywords
        };
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!object.Equals(expected, actual))
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }
}
