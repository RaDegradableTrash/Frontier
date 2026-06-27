public static class CardPresentationRules
{
    public static bool ShouldUseHandPresentation(RuntimeCard card)
    {
        return card != null && card.Zone == CardZone.Hand;
    }
}
