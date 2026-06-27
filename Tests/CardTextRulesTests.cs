using System;

public static class CardTextRulesTests
{
    public static int Main()
    {
        RuntimeCard counter = new RuntimeCard
        {
            CardName = "2K Prepared Defense",
            Type = CardType.Countermeasure,
            EffectType = CardEffectType.DamageTargetUnit,
            EffectAmount = 2
        };

        string counterLine = CardTextRules.CardFaceLine(counter);
        AssertTrue(counterLine.Contains("TYPE: COUNTER"), "Countermeasure cards should clearly label the card type.");
        AssertTrue(counterLine.Contains("EFFECT: DMG 2"), "Damage effects should clearly label the effect value.");
        AssertTrue(CardTextRules.StatusLabel(counter) == string.Empty, "Countermeasures in hand should not repeat COUNTER in the status badge.");

        RuntimeCard cancelCounter = new RuntimeCard
        {
            CardName = "Prepared Defense",
            Type = CardType.Countermeasure,
            EffectType = CardEffectType.CancelAttack,
            EffectAmount = 0
        };
        AssertTrue(CardTextRules.CardFaceLine(cancelCounter).Contains("EFFECT: STOP"), "Zero-value counter effects should keep the clear effect label.");
        AssertTrue(!CardTextRules.CardFaceLine(cancelCounter).Contains("STOP 0"), "Zero-value counter effects should not print a noisy 0 on the card face.");

        counter.Zone = CardZone.Countermeasure;
        AssertTrue(CardTextRules.StatusLabel(counter) == "SET COUNTER", "Set countermeasures should have a compact inspectable status badge.");

        RuntimeCard unit = new RuntimeCard
        {
            CardName = "Forward Scouts",
            Type = CardType.Unit,
            OperationCost = 1,
            Zone = CardZone.Hand
        };

        string unitLine = CardTextRules.CardFaceLine(unit);
        AssertTrue(unitLine.Contains("TYPE: UNIT"), "Unit cards should clearly label the card type.");
        AssertTrue(unitLine.Contains("OPERATE: 1"), "Unit cards should clearly label operation cost.");
        AssertTrue(!CardTextRules.ShowBattlefieldStats(unit), "Units in hand should not show large battlefield stat badges.");

        unit.Zone = CardZone.PlayerSupport;
        AssertTrue(CardTextRules.ShowBattlefieldStats(unit), "Deployed units should show large battlefield stat badges.");
        AssertTrue(CardTextRules.CanHoverInspect(unit, false), "Visible cards should be inspectable on hover.");
        AssertTrue(!CardTextRules.CanHoverInspect(unit, true), "Hidden enemy cards should not reveal details on hover.");

        unit.AddKeyword(CardKeyword.Pinned);
        AssertTrue(CardTextRules.StatusLabel(unit) == "PINNED", "Pinned units should use a compact readable status badge.");

        RuntimeCard spentUnit = new RuntimeCard
        {
            CardName = "Spent Unit",
            Type = CardType.Unit,
            Zone = CardZone.Frontline,
            HasActed = true
        };
        AssertTrue(CardTextRules.StatusLabel(spentUnit) == "SPENT", "Units that already acted should show a compact spent badge.");

        spentUnit.Zone = CardZone.Hand;
        AssertTrue(CardTextRules.StatusLabel(spentUnit) == string.Empty, "Hand cards should not show spent badges.");
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
