using System;

public static class CardMotionRulesTests
{
    public static int Main()
    {
        AssertTrue(CardMotionRules.MoveLerpSpeed >= 10f, "Card movement should be fast enough to keep play responsive.");
        AssertTrue(CardMotionRules.MoveLerpSpeed <= 20f, "Card movement should be smooth instead of instant teleporting.");
        AssertTrue(CardMotionRules.ScaleLerpSpeed >= 8f, "Card scale changes should settle quickly during layout changes.");
        AssertTrue(CardMotionRules.SpawnScaleMultiplier < 1f, "New cards should pop in with a visible spawn scale.");
        AssertTrue(CardMotionRules.SpawnScaleMultiplier > 0.7f, "New cards should not spawn from an unreadably tiny scale.");
        AssertTrue(CardMotionRules.ShouldSnapToTarget(0.005f), "Cards should snap cleanly when nearly at their target.");
        AssertTrue(!CardMotionRules.ShouldSnapToTarget(0.2f), "Cards should animate when visibly away from their target.");
        AssertTrue(!CardMotionRules.ShouldAnimatePosition(true, true), "Cards should not auto-animate while being dragged.");
        AssertTrue(CardMotionRules.ShouldAnimatePosition(false, true), "Cards should animate toward layout targets when not dragged.");
        AssertTrue(!CardMotionRules.ShouldAnimateLayout(false, true), "Newly created cards should not slide in from the world origin.");
        AssertTrue(!CardMotionRules.ShouldAnimateLayout(true, false), "Explicit non-animated layout should snap to target.");
        AssertTrue(CardMotionRules.ShouldAnimateLayout(true, true), "Existing cards should animate between board slots.");
        AssertTrue(CardMotionRules.AttackLungeDistanceRatio > 0.16f, "Attacking units should visibly lunge toward their target.");
        AssertTrue(CardMotionRules.AttackLungeDistanceRatio < 0.36f, "Attack lunge should not move units so far that board state becomes confusing.");
        AssertTrue(CardMotionRules.AttackLungeReturnSeconds >= 0.12f, "Attack lunge should be visible before returning.");
        AssertTrue(CardMotionRules.AttackLungeReturnSeconds <= 0.35f, "Attack lunge should resolve quickly enough to keep turns responsive.");
        AssertTrue(CardMotionRules.ShouldApplyLunge(false, false), "Idle cards should be allowed to play lunge feedback.");
        AssertTrue(!CardMotionRules.ShouldApplyLunge(true, false), "Dragged cards should not be interrupted by lunge feedback.");
        AssertTrue(!CardMotionRules.ShouldApplyLunge(false, true), "Cards already moving to a slot should not stack a lunge animation.");
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
