using System.Collections.Generic;

public static class MulliganRules
{
    public static bool CanMarkForDiscard(GamePhase phase, PlayerSide activeSide, RuntimeCard card)
    {
        return phase == GamePhase.Mulligan
            && activeSide == PlayerSide.Player
            && card != null
            && card.Owner == PlayerSide.Player
            && card.Zone == CardZone.Hand;
    }

    public static bool ToggleMarked(HashSet<string> markedIds, RuntimeCard card)
    {
        if (card == null || string.IsNullOrEmpty(card.Id))
        {
            return false;
        }

        if (markedIds.Contains(card.Id))
        {
            markedIds.Remove(card.Id);
            return false;
        }

        markedIds.Add(card.Id);
        return true;
    }

    public static bool IsMarked(HashSet<string> markedIds, RuntimeCard card)
    {
        return card != null && !string.IsNullOrEmpty(card.Id) && markedIds.Contains(card.Id);
    }

    public static int MarkedCount(HashSet<string> markedIds)
    {
        return markedIds != null ? markedIds.Count : 0;
    }

    public static bool ShouldRedrawMarkedCards(HashSet<string> markedIds)
    {
        return markedIds != null && markedIds.Count > 0;
    }
}
