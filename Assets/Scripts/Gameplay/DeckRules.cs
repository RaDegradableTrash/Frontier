public static class DeckRules
{
    public const int MinimumDeckSize = 40;
    public const int MaximumCopiesPerCard = 4;

    public static bool IsValidDeckSize(int cardCount)
    {
        return cardCount >= MinimumDeckSize;
    }

    public static int CardsNeededForValidDeck(int cardCount)
    {
        return cardCount >= MinimumDeckSize ? 0 : MinimumDeckSize - cardCount;
    }
}
