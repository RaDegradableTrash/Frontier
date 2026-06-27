using System;

public static class BoardTargetRulesTests
{
    public static int Main()
    {
        AssertTrue(BoardTargetRules.HeadquartersSlotIndex == -1, "Headquarters pseudo-slots should use a non-grid slot index.");
        AssertTrue(
            BoardTargetRules.HeadquartersTargetZone(PlayerSide.Enemy) == SlotZone.EnemySupport,
            "Enemy headquarters should be attackable through the enemy support target lane.");
        AssertTrue(
            BoardTargetRules.HeadquartersTargetZone(PlayerSide.Player) == SlotZone.PlayerSupport,
            "Player headquarters should be attackable through the player support target lane.");
        AssertTrue(
            BoardTargetRules.IsHeadquartersSlot(BoardTargetRules.HeadquartersSlotIndex),
            "Only the headquarters pseudo-slot should represent HQ attacks.");
        AssertTrue(
            !BoardTargetRules.IsHeadquartersSlot(0),
            "Empty ordinary support slots should not be treated as headquarters attacks.");
        AssertTrue(
            BoardTargetRules.ShouldReplaceClosestTarget(0.2f, 0.8f),
            "Drag lookup should prefer headquarters when the release point is closer to headquarters than to the grid.");
        AssertTrue(
            !BoardTargetRules.ShouldReplaceClosestTarget(0.8f, 0.2f),
            "Drag lookup should keep the existing grid slot when it is closer than headquarters.");
        AssertTrue(
            BoardTargetRules.ShouldAcceptClosestTarget(1.2f),
            "Releases near a slot should still resolve to the nearest board target.");
        AssertTrue(
            !BoardTargetRules.ShouldAcceptClosestTarget(3.5f),
            "Releases far away from the board should not accidentally resolve to the nearest slot.");
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
