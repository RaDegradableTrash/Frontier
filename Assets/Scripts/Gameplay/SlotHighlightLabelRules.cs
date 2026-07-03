public static class SlotHighlightLabelRules
{
    public static string LabelFor(RuntimeCard card, SlotZone targetZone)
    {
        if (card == null)
        {
            return PlayableSceneRules.HighlightedSlotLabel;
        }

        if (card.Zone == CardZone.Hand)
        {
            if (card.Type == CardType.Unit)
            {
                return "DEPLOY HERE";
            }

            if (card.Type == CardType.Countermeasure)
            {
                return "SET COUNTER";
            }

            return OrderLabelFor(card);
        }

        if (card.Zone == CardZone.PlayerSupport || card.Zone == CardZone.EnemySupport)
        {
            return "MOVE HERE";
        }

        if (card.Zone == CardZone.Frontline)
        {
            return "ATTACK HERE";
        }

        return PlayableSceneRules.HighlightedSlotLabel;
    }

    public static string AttackLabelFor(RuntimeCard targetCard, bool defenderHasGuard)
    {
        if (targetCard == null)
        {
            return "ATTACK HQ";
        }

        if (defenderHasGuard && targetCard != null && targetCard.HasKeyword(CardKeyword.Guard))
        {
            return "ATTACK GUARD";
        }

        return "ATTACK HERE";
    }

    private static string OrderLabelFor(RuntimeCard card)
    {
        switch (card.EffectType)
        {
            case CardEffectType.DamageEnemyHeadquarters:
                return "DAMAGE HQ";
            case CardEffectType.DamageTargetUnit:
                return "DAMAGE UNIT";
            case CardEffectType.RepairHeadquarters:
                return "REPAIR HQ";
            case CardEffectType.DrawCards:
                return "DRAW CARDS";
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return "DMG + AOE";
            case CardEffectType.DrawForCardsPlayed:
                return "DRAW CARDS";
            case CardEffectType.BuffFriendlyUnit:
                return "BUFF ALLY";
            case CardEffectType.PinTargetUnit:
                return "PIN UNIT";
            case CardEffectType.IncreaseEnemyCosts:
                return "ENEMY +1 COST";
            case CardEffectType.DeployWithBlitz:
                return "AIRBORNE";
            case CardEffectType.FieldIntel:
                return "FIELD INTEL";
            default:
                return "PLAY ORDER";
        }
    }
}
