public static class UnitActionHighlightRules
{
    public static bool ShouldHighlightAdvanceTargets(RuntimeCard card, int availableKredits, bool hasFrontlineController, PlayerSide frontlineController)
    {
        return card != null
            && card.Zone == CardZone.PlayerSupport
            && card.CanOperate(availableKredits)
            && (!hasFrontlineController || frontlineController == card.Owner);
    }

    public static bool ShouldHighlightAttackTargets(RuntimeCard card, int availableKredits)
    {
        if (card == null || card.Type != CardType.Unit || card.Zone != CardZone.Frontline || card.HasActed || card.HasKeyword(CardKeyword.Pinned) || !KreditRules.CanSpend(availableKredits, card.OperationCost))
        {
            return false;
        }

        int maxAttacks = card.HasKeyword(CardKeyword.Fury) ? 2 : 1;
        return card.AttacksThisTurn < maxAttacks;
    }
}
