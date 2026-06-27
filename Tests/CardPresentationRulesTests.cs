using System;

public static class CardPresentationRulesTests
{
    public static int Main()
    {
        RuntimeCard handCard = new RuntimeCard { Zone = CardZone.Hand };
        RuntimeCard supportCard = new RuntimeCard { Zone = CardZone.PlayerSupport };
        RuntimeCard frontlineCard = new RuntimeCard { Zone = CardZone.Frontline };
        RuntimeCard countermeasure = new RuntimeCard { Zone = CardZone.Countermeasure };

        AssertTrue(CardPresentationRules.ShouldUseHandPresentation(handCard), "Cards in hand should use the tucked hand presentation.");
        AssertTrue(!CardPresentationRules.ShouldUseHandPresentation(supportCard), "Support cards should restore normal battlefield presentation.");
        AssertTrue(!CardPresentationRules.ShouldUseHandPresentation(frontlineCard), "Frontline cards should restore normal battlefield presentation.");
        AssertTrue(!CardPresentationRules.ShouldUseHandPresentation(countermeasure), "Set countermeasures should restore normal inspectable presentation.");
        AssertTrue(!CardPresentationRules.ShouldUseHandPresentation(null), "Null cards should safely use normal presentation.");
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
