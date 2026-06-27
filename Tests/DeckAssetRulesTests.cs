using System;

public static class DeckAssetRulesTests
{
    public static int Main()
    {
        AssertEqual(0, DeckAssetRules.TemplateIndexForPosition(0, 0), "Empty template sets should return zero.");
        AssertEqual(0, DeckAssetRules.TemplateIndexForPosition(0, 3), "First deck card should use first asset.");
        AssertEqual(2, DeckAssetRules.TemplateIndexForPosition(5, 3), "Deck asset positions should cycle through assets.");
        AssertEqual(40, DeckAssetRules.TargetDeckSize(6), "Authored asset decks should expand to 40 cards.");
        AssertEqual(0, DeckAssetRules.TargetDeckSize(0), "Empty asset decks should not produce cards.");
        return 0;
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }
}
