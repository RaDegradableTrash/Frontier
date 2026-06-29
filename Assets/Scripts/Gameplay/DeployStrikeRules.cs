public static class DeployStrikeRules
{
    public const int StrikeStatThreshold = 11;

    public static bool ShouldTriggerStrike(RuntimeCard card)
    {
        return card != null
            && card.Type == CardType.Unit
            && card.Attack + card.Defense >= StrikeStatThreshold;
    }
}
