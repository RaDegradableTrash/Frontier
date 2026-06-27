using System;

public static class CardInteractionRulesTests
{
    public static int Main()
    {
        RuntimeCard playerHandCard = new RuntimeCard
        {
            Owner = PlayerSide.Player,
            Zone = CardZone.Hand
        };
        AssertTrue(CardInteractionRules.ShouldHoldPlayerHandOpen(playerHandCard, false), "Visible player hand cards should keep the hand bar open while hovered.");

        RuntimeCard playerSupportCard = new RuntimeCard
        {
            Owner = PlayerSide.Player,
            Zone = CardZone.PlayerSupport
        };
        AssertTrue(!CardInteractionRules.ShouldHoldPlayerHandOpen(playerSupportCard, false), "Board cards should not keep the hand bar open.");

        RuntimeCard enemyHandCard = new RuntimeCard
        {
            Owner = PlayerSide.Enemy,
            Zone = CardZone.Hand
        };
        AssertTrue(!CardInteractionRules.ShouldHoldPlayerHandOpen(enemyHandCard, false), "Enemy hand cards should not control the player hand bar.");
        AssertTrue(!CardInteractionRules.ShouldHoldPlayerHandOpen(playerHandCard, true), "Hidden cards should not control the player hand bar.");
        AssertTrue(!CardInteractionRules.ShouldHoldPlayerHandOpen(null, false), "Null card hover should be safe.");
        AssertTrue(!CardInteractionRules.ShouldReleasePlayerHandHold(true), "Dragging a hand card out of the rail should not collapse the hand.");
        AssertTrue(CardInteractionRules.ShouldReleasePlayerHandHold(false), "Non-drag hover exit should release the hand hold.");
        AssertTrue(CardInteractionRules.ShouldReleaseHeldPlayerHand(true), "A previously held-open hand should release even if the card moved out of hand during drop.");
        AssertTrue(!CardInteractionRules.ShouldReleaseHeldPlayerHand(false), "No release call is needed when this card never held the hand open.");
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
