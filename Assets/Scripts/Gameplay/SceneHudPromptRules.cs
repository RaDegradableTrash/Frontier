public static class SceneHudPromptRules
{
    public static string Prompt(
        RuntimeCard selectedCard,
        int availableKredits,
        bool hasFrontlineController,
        PlayerSide frontlineController,
        GamePhase phase,
        PlayerSide activeSide,
        bool mulliganUsed)
    {
        if (selectedCard != null)
        {
            return CompactSelectedPrompt(selectedCard, availableKredits, hasFrontlineController, frontlineController);
        }

        if (phase == GamePhase.Mulligan)
        {
            return SceneGuidanceRules.OpeningHandPrompt(mulliganUsed);
        }

        return SceneGuidanceRules.ActionPrompt(phase, activeSide);
    }

    private static string CompactSelectedPrompt(RuntimeCard selectedCard, int availableKredits, bool hasFrontlineController, PlayerSide frontlineController)
    {
        if (selectedCard.Zone == CardZone.Hand && selectedCard.KreditCost > availableKredits)
        {
            return $"NEED {selectedCard.KreditCost}K";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport || selectedCard.Zone == CardZone.Frontline || selectedCard.Zone == CardZone.EnemySupport)
        {
            return "MOVE / ATTACK";
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Countermeasure)
        {
            return "CLICK BOARD";
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Order)
        {
            return "CLICK BOARD/TARGET";
        }

        if (selectedCard.Zone == CardZone.Countermeasure)
        {
            return "SET COUNTER";
        }

        return "CHOOSE TARGET";
    }
}
