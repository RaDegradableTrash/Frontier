public static class GuardProtectionRules
{
    public static bool ProtectsSupportTargets(RuntimeCard card, PlayerSide defender)
    {
        return card != null
            && card.Owner == defender
            && card.Zone == SupportZoneFor(defender)
            && card.HasKeyword(CardKeyword.Guard);
    }

    private static CardZone SupportZoneFor(PlayerSide side)
    {
        return side == PlayerSide.Player ? CardZone.PlayerSupport : CardZone.EnemySupport;
    }
}
