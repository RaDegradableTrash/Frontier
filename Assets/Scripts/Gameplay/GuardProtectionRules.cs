public static class GuardProtectionRules
{
    public static bool ProtectsSupportTargets(RuntimeCard card, PlayerSide defender)
    {
        return card != null
            && card.Owner == defender
            && card.HasKeyword(CardKeyword.Guard);
    }
}
