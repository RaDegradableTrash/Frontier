public enum CardOperationBadgeState
{
    Hidden,
    Ready,
    NeedKredits,
    Spent,
    Pinned
}

public static class CardOperationBadgeRules
{
    public static string Text(RuntimeCard card, int availableKredits)
    {
        int operationCost = card != null ? card.OperationCost : 0;
        return Text(card, availableKredits, operationCost);
    }

    public static string Text(RuntimeCard card, int availableKredits, int operationCost)
    {
        return State(card, availableKredits, operationCost) == CardOperationBadgeState.Hidden
            ? string.Empty
            : operationCost.ToString();
    }

    public static CardOperationBadgeState State(RuntimeCard card, int availableKredits, int operationCost)
    {
        if (card == null || card.Type != CardType.Unit || card.Zone == CardZone.Hand || card.Zone == CardZone.Discard)
        {
            return CardOperationBadgeState.Hidden;
        }

        if (card.HasKeyword(CardKeyword.Pinned))
        {
            return CardOperationBadgeState.Pinned;
        }

        if (card.HasActed)
        {
            return CardOperationBadgeState.Spent;
        }

        return operationCost <= availableKredits
            ? CardOperationBadgeState.Ready
            : CardOperationBadgeState.NeedKredits;
    }

    public static CardOperationBadgeState State(RuntimeCard card, int availableKredits)
    {
        if (card == null || card.Type != CardType.Unit || card.Zone == CardZone.Hand || card.Zone == CardZone.Discard)
        {
            return CardOperationBadgeState.Hidden;
        }

        if (card.HasKeyword(CardKeyword.Pinned))
        {
            return CardOperationBadgeState.Pinned;
        }

        if (card.HasActed)
        {
            return CardOperationBadgeState.Spent;
        }

        return card.OperationCost <= availableKredits
            ? CardOperationBadgeState.Ready
            : CardOperationBadgeState.NeedKredits;
    }
}
