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

        if (selectedCard.Zone == CardZone.PlayerSupport && hasFrontlineController && frontlineController != selectedCard.Owner)
        {
            return "CLEAR FRONTLINE";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport)
        {
            return "ADVANCE";
        }

        if (selectedCard.Zone == CardZone.Frontline)
        {
            return "ATTACK";
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
