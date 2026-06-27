using System;

public static class DeckSummaryTextRulesTests
{
    public static int Main()
    {
        string readySummary = DeckSummaryTextRules.BuildSummary(
            "Allied Tempo",
            "Fast units and flexible orders.",
            1,
            false,
            36,
            true,
            40);

        AssertTrue(readySummary.Contains("CLICK A FACTION"), "Deck summary should tell new players that faction plates are clickable.");
        AssertTrue(readySummary.Contains("START MATCH"), "Deck summary should name the tabletop start button.");
        AssertTrue(readySummary.Contains("READY"), "Valid deck summary should use a clear ready label.");

        string invalidSummary = DeckSummaryTextRules.BuildSummary(
            "Edited Deck",
            "Custom card list.",
            2,
            true,
            12,
            false,
            40);

        AssertTrue(invalidSummary.Contains("ADD CARDS"), "Invalid edited decks should tell players how to fix the deck.");
        AssertTrue(invalidSummary.Contains("12/40"), "Invalid edited decks should show progress toward the minimum deck size.");
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
