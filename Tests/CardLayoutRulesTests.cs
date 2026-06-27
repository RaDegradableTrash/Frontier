using System;

public static class CardLayoutRulesTests
{
    public static int Main()
    {
        AssertEqual(0, CardLayoutRules.OffsetIndex(0, 1), "Single card should be centered.");
        AssertEqual(-1, CardLayoutRules.OffsetIndex(0, 3), "First of three cards should offset left.");
        AssertEqual(0, CardLayoutRules.OffsetIndex(1, 3), "Middle of three cards should be centered.");
        AssertEqual(1, CardLayoutRules.OffsetIndex(2, 3), "Last of three cards should offset right.");
        AssertTrue(CardLayoutRules.HandFanRotationDegrees(0, 5) > 0f, "Left hand cards should rotate slightly clockwise in the fan.");
        AssertTrue(CardLayoutRules.HandFanRotationDegrees(4, 5) < 0f, "Right hand cards should rotate slightly counter-clockwise in the fan.");
        AssertTrue(CardLayoutRules.HandFanDepthOffset(0, 5) < CardLayoutRules.HandFanDepthOffset(2, 5), "Outer hand cards should sit lower than center cards.");
        AssertEqual(0, CardLayoutRules.NewlyAddedIndex(0), "Empty rows should fall back to the first visual index.");
        AssertEqual(0, CardLayoutRules.NewlyAddedIndex(1), "First added card should use index zero.");
        AssertEqual(2, CardLayoutRules.NewlyAddedIndex(3), "Newly added cards should use the last occupied row index.");

        AssertTrue(
            PlayableSceneRules.PlayerHeadquartersSlot.z == -1.35f && PlayableSceneRules.PlayerHeadquartersSlot.x < 2.55f,
            "Player headquarters should sit on the support line as a slot-like object.");
        AssertTrue(
            PlayableSceneRules.EnemyHeadquartersSlot.z == 1.35f && PlayableSceneRules.EnemyHeadquartersSlot.x < 2.55f,
            "Enemy headquarters should sit on the support line as a slot-like object.");
        AssertTrue(
            PlayableSceneRules.PlayerHandAnchor.z < PlayableSceneRules.PlayerCommandRowZ,
            "Hidden player hand should sit partly outside the bottom of the camera view.");
        AssertTrue(
            PlayableSceneRules.PlayerHandRevealedAnchor.z >= -3.65f,
            "Revealed player hand should slide into view without a jarring jump.");
        AssertTrue(
            PlayableSceneRules.CardArtPanelHeightRatio >= 0.58f,
            "Card image panels should dominate the card face.");
        AssertTrue(
            PlayableSceneRules.CardTextCharacterSize <= 0.019f,
            "Card rules text should be smaller than the large numerals.");
        AssertTrue(
            PlayableSceneRules.CardNumberCharacterSize <= 0.06f,
            "Card numerals should not obscure card art or labels.");
        AssertTrue(
            PlayableSceneRules.HeadquartersStrengthCharacterSize <= 0.075f,
            "Headquarters strength numerals should fit inside the HQ card.");
        return 0;
    }

    private static void AssertEqual(float expected, float actual, string message)
    {
        if (Math.Abs(expected - actual) > 0.0001f)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
