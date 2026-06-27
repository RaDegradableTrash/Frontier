public static class UnitDeploymentRules
{
    public static void MarkDeployed(RuntimeCard card)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return;
        }

        card.AttacksThisTurn = 0;
        card.HasActed = !card.HasKeyword(CardKeyword.Blitz);
    }
}
