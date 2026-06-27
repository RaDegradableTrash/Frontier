public static class BoardAreaClickRules
{
    public static BoardAreaClickAction ActionFor(RuntimeCard selectedCard)
    {
        if (selectedCard == null || selectedCard.Zone != CardZone.Hand)
        {
            return BoardAreaClickAction.None;
        }

        switch (selectedCard.Type)
        {
            case CardType.Countermeasure:
                return BoardAreaClickAction.SetCountermeasure;
            case CardType.Order:
                return BoardAreaClickAction.PlayOrder;
            case CardType.Unit:
                return BoardAreaClickAction.NeedsSlot;
            default:
                return BoardAreaClickAction.None;
        }
    }
}
