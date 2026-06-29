using System;
using System.Collections.Generic;

public static class MulliganRulesTests
{
    public static int Main()
    {
        HashSet<string> marked = new HashSet<string>();
        RuntimeCard card = new RuntimeCard { Id = "a", Owner = PlayerSide.Player, Zone = CardZone.Hand };
        AssertTrue(MulliganRules.CanMarkForDiscard(GamePhase.Mulligan, PlayerSide.Player, card), "Opening hand cards should be markable.");
        AssertTrue(MulliganRules.ToggleMarked(marked, card), "First toggle should mark card.");
        AssertTrue(MulliganRules.IsMarked(marked, card), "Marked card should stay marked.");
        AssertTrue(!MulliganRules.ToggleMarked(marked, card), "Second toggle should unmark card.");
        AssertTrue(MulliganRules.ShouldRedrawMarkedCards(marked) == false, "Empty mark set should not redraw.");
        MulliganRules.ToggleMarked(marked, card);
        AssertTrue(MulliganRules.ShouldRedrawMarkedCards(marked), "Marked cards should trigger partial mulligan.");
        return 0;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
