using System;

public static class BoardAreaClickRulesTests
{
    public static int Main()
    {
        AssertEqual(BoardAreaClickAction.SetCountermeasure, BoardAreaClickRules.ActionFor(Card(CardType.Countermeasure, CardZone.Hand)), "Selected hand countermeasures should be set by clicking the tabletop.");
        AssertEqual(BoardAreaClickAction.PlayOrder, BoardAreaClickRules.ActionFor(Card(CardType.Order, CardZone.Hand)), "Selected hand orders should be playable by clicking the tabletop.");
        AssertEqual(BoardAreaClickAction.NeedsSlot, BoardAreaClickRules.ActionFor(Card(CardType.Unit, CardZone.Hand)), "Selected hand units should still require a deployment slot.");
        AssertEqual(BoardAreaClickAction.None, BoardAreaClickRules.ActionFor(Card(CardType.Countermeasure, CardZone.Countermeasure)), "Set countermeasures should inspect, not play again.");
        AssertEqual(BoardAreaClickAction.None, BoardAreaClickRules.ActionFor(null), "No selected card should produce no board-area action.");
        return 0;
    }

    private static RuntimeCard Card(CardType type, CardZone zone)
    {
        return new RuntimeCard
        {
            Type = type,
            Zone = zone
        };
    }

    private static void AssertEqual(BoardAreaClickAction expected, BoardAreaClickAction actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }
}
