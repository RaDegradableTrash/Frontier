using System;

public static class CardInspectorTextRulesTests
{
    public static int Main()
    {
        RuntimeCard counter = new RuntimeCard
        {
            CardName = "Prepared Defense",
            Type = CardType.Countermeasure,
            Zone = CardZone.Countermeasure,
            KreditCost = 2,
            EffectType = CardEffectType.CancelAttack,
            EffectAmount = 0,
            Keywords = CardKeyword.None,
            RulesText = "Cancel the next enemy attack."
        };

        string counterText = CardInspectorTextRules.ForCard(counter);
        AssertTrue(counterText.Contains("TYPE: COUNTERMEASURE"), "Inspector should clearly label countermeasure type.");
        AssertTrue(counterText.Contains("EFFECT: CANCEL ATTACK"), "Inspector should translate effect enums into player-facing words.");
        AssertTrue(counterText.Contains("STATUS: SET COUNTER"), "Inspector should show set countermeasure status.");
        AssertTrue(!counterText.Contains("CancelAttack"), "Inspector should not expose raw enum names.");

        RuntimeCard pinnedGuard = new RuntimeCard
        {
            CardName = "Infantry Section",
            Type = CardType.Unit,
            Zone = CardZone.PlayerSupport,
            KreditCost = 1,
            OperationCost = 1,
            Attack = 1,
            CurrentDefense = 2,
            Keywords = CardKeyword.Pinned | CardKeyword.Guard
        };

        string unitText = CardInspectorTextRules.ForCard(pinnedGuard);
        AssertTrue(unitText.Contains("COST: 1"), "Inspector should label card cost.");
        AssertTrue(unitText.Contains("ATTACK: 1"), "Inspector should label attack.");
        AssertTrue(unitText.Contains("DEFENSE: 2"), "Inspector should label defense.");
        AssertTrue(unitText.Contains("OPERATE: 1"), "Inspector should label operation cost.");
        AssertTrue(unitText.Contains("STATUS: PINNED, GUARD"), "Inspector should list keyword statuses in readable words.");
        AssertTrue(!unitText.Contains("PlayerSupport"), "Inspector should not expose raw zone enum names.");

        string emptyText = CardInspectorTextRules.EmptyHint();
        AssertTrue(emptyText.Contains("HOVER BOTTOM EDGE"), "Empty inspector should explain hidden-hand reveal.");
        AssertTrue(emptyText.Contains("CLICK SET COUNTERS"), "Empty inspector should tell players countermeasures are inspectable.");
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
