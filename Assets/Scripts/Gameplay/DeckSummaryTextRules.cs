public static class DeckSummaryTextRules
{
    public static string BuildSummary(string deckName, string description)
    {
        return
            "当前卡组\n" +
            $"{deckName}\n" +
            $"{description}\n" +
            "点击 START 开始。";
    }
}
