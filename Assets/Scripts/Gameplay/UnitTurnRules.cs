public static class UnitTurnRules
{
    public static void ReadyForTurn(RuntimeCard card)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return;
        }

        bool wasPinned = card.HasKeyword(CardKeyword.Pinned);
        card.AttacksThisTurn = 0;
        card.HasActed = wasPinned;
        card.RemoveKeyword(CardKeyword.Pinned);
    }
}
