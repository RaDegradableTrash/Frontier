public static class DeckSummaryTextRules
{
    public static string BuildSummary(
        string deckName,
        string description,
        int slot,
        bool usingCustomDeck,
        int customDeckSize,
        bool customDeckValid,
        int minimumDeckSize)
    {
        string source = usingCustomDeck ? "EDITED DECK" : "STARTER DECK";
        string readiness = !usingCustomDeck || customDeckValid ? "READY" : "ADD CARDS";
        string nextStep = !usingCustomDeck || customDeckValid
            ? "NEXT: CLICK A FACTION PLATE OR START MATCH"
            : "NEXT: ADD CARDS OR USE A STARTER DECK";

        return
            "DECK SELECT\n" +
            $"SLOT {slot}: {deckName}\n" +
            $"{description}\n" +
            $"{source}    {customDeckSize}/{minimumDeckSize}\n" +
            $"{readiness}\n" +
            nextStep;
    }
}
