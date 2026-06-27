public static class CardTextRules
{
    public static string CardFaceLine(RuntimeCard card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        if (card.Type == CardType.Unit)
        {
            return $"TYPE: UNIT\nOPERATE: {card.OperationCost}";
        }

        string type = card.Type == CardType.Countermeasure ? "COUNTER" : "ORDER";
        string effect = EffectLabel(card.EffectType);
        if (string.IsNullOrEmpty(effect))
        {
            return $"TYPE: {type}";
        }

        return card.EffectAmount > 0 ? $"TYPE: {type}\nEFFECT: {effect} {card.EffectAmount}" : $"TYPE: {type}\nEFFECT: {effect}";
    }

    public static string ShortCardName(RuntimeCard card)
    {
        if (card == null || string.IsNullOrEmpty(card.CardName))
        {
            return string.Empty;
        }

        const int maxLength = 18;
        return card.CardName.Length <= maxLength ? card.CardName : card.CardName.Substring(0, maxLength - 1) + "…";
    }

    public static string StatusLabel(RuntimeCard card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        if (card.Zone == CardZone.Hand)
        {
            return string.Empty;
        }

        if (card.HasKeyword(CardKeyword.Pinned))
        {
            return "PINNED";
        }

        if (card.Type == CardType.Unit && card.HasActed)
        {
            return "SPENT";
        }

        if (card.HasKeyword(CardKeyword.Guard))
        {
            return "GUARD";
        }

        if (card.HasKeyword(CardKeyword.Blitz))
        {
            return "BLITZ";
        }

        if (card.HasKeyword(CardKeyword.Ambush))
        {
            return "AMBUSH";
        }

        if (card.Type == CardType.Countermeasure && card.Zone == CardZone.Countermeasure)
        {
            return "SET COUNTER";
        }

        return string.Empty;
    }

    public static bool ShowBattlefieldStats(RuntimeCard card)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return false;
        }

        return card.Zone == CardZone.PlayerSupport
            || card.Zone == CardZone.Frontline
            || card.Zone == CardZone.EnemySupport;
    }

    public static bool CanHoverInspect(RuntimeCard card, bool hidden)
    {
        return card != null && !hidden;
    }

    private static string EffectLabel(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.DamageEnemyHeadquarters:
                return "DMG";
            case CardEffectType.RepairHeadquarters:
                return "HEAL";
            case CardEffectType.DrawCards:
                return "DRAW";
            case CardEffectType.BuffFriendlyUnit:
                return "BUFF";
            case CardEffectType.PinTargetUnit:
                return "PIN";
            case CardEffectType.CancelAttack:
                return "STOP";
            default:
                return string.Empty;
        }
    }
}
