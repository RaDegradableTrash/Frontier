using System;

public static class SceneGuidanceRulesTests
{
    public static int Main()
    {
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.DeckBuilder, PlayerSide.Player).Contains("CHOOSE DECK"),
            "Deck builder prompt should tell a new player to choose a deck.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.DeckBuilder, PlayerSide.Player).Contains("CLICK START"),
            "Deck builder prompt should explicitly tell a new player to click Start.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.DeckBuilder, PlayerSide.Player).Contains("FACTION"),
            "Deck builder prompt should tell a new player the faction deck plates are clickable.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.DeckBuilder, PlayerSide.Player).Contains("START MATCH"),
            "Deck builder prompt should use the exact tabletop button label.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.Mulligan, PlayerSide.Player).Contains("KEEP OR MULLIGAN"),
            "Mulligan prompt should name the two opening-hand actions.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.Mulligan, PlayerSide.Player).Contains("INSPECT HAND"),
            "Mulligan prompt should tell a new player to inspect the opening hand before choosing.");
        AssertTrue(
            SceneGuidanceRules.OpeningHandPrompt(false).Contains("MULLIGAN"),
            "Opening-hand prompt should mention Mulligan before it is used.");
        AssertTrue(
            SceneGuidanceRules.OpeningHandPrompt(true).Contains("KEEP HAND"),
            "Opening-hand prompt should tell the player to keep after Mulligan is used.");
        AssertTrue(
            !SceneGuidanceRules.OpeningHandPrompt(true).Contains("OR MULLIGAN"),
            "Opening-hand prompt should not offer Mulligan again after it has been used.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.PlayerTurn, PlayerSide.Player).Contains("YOUR TURN"),
            "Player turn prompt should make ownership of the turn obvious.");
        AssertTrue(
            !SceneGuidanceRules.ActionPrompt(GamePhase.PlayerTurn, PlayerSide.Player).Contains("PLAY TO SUPPORT"),
            "Player turn prompt should stay compact instead of writing rules across the screen.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.PlayerTurn, PlayerSide.Player).Length <= 18,
            "Player turn prompt should be a short state label.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.EnemyTurn, PlayerSide.Enemy).Contains("ENEMY TURN"),
            "Enemy turn prompt should tell the player to wait.");
        AssertTrue(
            SceneGuidanceRules.ActionPrompt(GamePhase.GameOver, PlayerSide.Player).Contains("RESTART"),
            "Game-over prompt should expose the recovery action.");
        AssertTrue(
            SceneGuidanceRules.HelpPrompt().Contains("DRAG"),
            "Help prompt should mention drag-and-drop as a primary input.");
        AssertTrue(
            SceneGuidanceRules.HelpPrompt().Contains("P/A/F"),
            "Help prompt should expose quick play, advance, and attack shortcuts.");
        AssertTrue(
            SceneGuidanceRules.HelpPrompt().Contains("SPACE"),
            "Help prompt should expose the End Turn shortcut.");
        AssertTrue(
            SceneGuidanceRules.BlockedInteractionPrompt(GamePhase.EnemyTurn, PlayerSide.Enemy).Contains("ENEMY TURN"),
            "Blocked interaction during the enemy turn should explain why clicks do not act.");
        AssertTrue(
            SceneGuidanceRules.BlockedInteractionPrompt(GamePhase.DeckBuilder, PlayerSide.Player).Contains("START MATCH"),
            "Blocked interaction before the match should tell players to start the match.");
        AssertTrue(
            SceneGuidanceRules.BlockedInteractionPrompt(GamePhase.Mulligan, PlayerSide.Player).Contains("KEEP"),
            "Blocked interaction during mulligan should point players to Keep or Mulligan.");
        AssertTrue(
            SceneGuidanceRules.TablePrompt(GamePhase.Mulligan, PlayerSide.Player).Length <= 28,
            "Table prompt should stay short enough not to cover the opening hand.");
        AssertTrue(
            SceneGuidanceRules.TablePrompt(GamePhase.PlayerTurn, PlayerSide.Player).Length <= 32,
            "Table prompt should stay short enough not to cover cards during the main turn.");
        AssertTrue(
            SceneGuidanceRules.TablePrompt(GamePhase.PlayerTurn, PlayerSide.Player).Contains("YOUR TURN"),
            "Short table prompt should still preserve the main turn state.");
        AssertTrue(
            PlayableSceneRules.ActionPromptPosition.z > -2.25f && PlayableSceneRules.ActionPromptPosition.z < -1.65f,
            "Action prompt should sit clearly above the hand instead of being buried under card labels.");
        AssertTrue(
            PlayableSceneRules.ActionPromptPosition.y >= 0.4f,
            "Action prompt should render above cards so it is not depth-occluded by the hand.");
        AssertTrue(
            PlayableSceneRules.ActionPromptBackingScale.x <= 3.4f,
            "Action prompt backing should be a compact status strip, not a rule-book plaque.");
        AssertTrue(
            PlayableSceneRules.ActionPromptCharacterSize <= 0.038f,
            "Action prompt should be small enough not to obscure the battlefield.");
        AssertTrue(
            PlayableSceneRules.ActionPromptUsesUnlitText,
            "Action prompt text should use an unlit material so dark scene lighting cannot hide it.");
        AssertTrue(
            PlayableSceneRules.ActionPromptHudEnabled,
            "A readable HUD action prompt should remain visible even when 3D tabletop text is hard to read.");
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
