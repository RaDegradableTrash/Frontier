using System;

public static class DeckRulesTests
{
    public static int Main()
    {
        AssertEqual(40, DeckRules.MinimumDeckSize, "KARDS-style constructed decks require 40 cards.");
        AssertEqual(4, DeckRules.MaximumCopiesPerCard, "Starter deck editor allows up to four copies per card.");
        AssertTrue(DeckRules.IsValidDeckSize(40), "A 40-card deck is valid.");
        AssertTrue(!DeckRules.IsValidDeckSize(39), "A 39-card deck is not valid.");
        AssertEqual(1, DeckRules.CardsNeededForValidDeck(39), "A 39-card deck is missing one card.");
        AssertEqual(0, DeckRules.CardsNeededForValidDeck(40), "A 40-card deck is missing no cards.");
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
