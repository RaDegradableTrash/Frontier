using System;
using UnityEngine;

public static class PlayableSceneRulesTests
{
    public static int Main()
    {
        AssertTrue(PlayableSceneRules.AutoStartMatchByDefault, "Play mode should begin in a playable match, not a menu-only scene.");
        AssertTrue(PlayableSceneRules.EditorPreviewEnabled, "Edit mode should show the playable scene, not a different blank setup.");
        AssertTrue(PlayableSceneRules.PreviewPlayerHandSize >= 4, "Edit mode should preview a full playable hand.");
        AssertTrue(PlayableSceneRules.PreviewEnemyHandSize >= 4, "Edit mode should preview the opposing hand.");
        AssertTrue(PlayableSceneRules.PreviewSupportColumns == 4, "Edit mode support slots should match the playable board.");
        AssertTrue(PlayableSceneRules.PreviewFrontlineColumns == 5, "Edit mode frontline slots should match the playable board.");
        AssertTrue(PlayableSceneRules.TabletopColor.grayscale < 0.35f, "Tabletop should be dark enough that cards, slots, and labels are readable.");
        AssertTrue(PlayableSceneRules.StatusMaxLogLines == 0, "Status panel should not print an action-log rules feed on the tabletop.");
        AssertTrue(!PlayableSceneRules.TabletopInfoPanelsEnabled, "Large status and inspector panels should not be written onto the tabletop.");
        AssertTrue(!PlayableSceneRules.TabletopActionPromptEnabled, "The tabletop should not print a large phase prompt over the battlefield.");
        AssertTrue(PlayableSceneRules.PlayerHandAnchor.z < -3.75f, "Collapsed player hand should sit partly outside the bottom of the view.");
        AssertTrue(PlayableSceneRules.PlayerHandAnchor.z < PlayableSceneRules.PlayerHandRevealedAnchor.z - 0.35f, "Collapsed player hand should clearly slide up when hovered.");
        AssertTrue(PlayableSceneRules.PlayerHandAnchor.z > -4.05f, "Collapsed player hand should leave enough card face visible to understand it is a hand rail.");
        AssertTrue(PlayableSceneRules.HandHoverRevealPixelHeight >= 300f, "The hand rail should reveal before the cursor reaches the tiny bottom edge.");
        AssertTrue(PlayableSceneRules.PlayerHandRevealedAnchor.z > PlayableSceneRules.PlayerCommandRowZ, "Revealed player hand should slide into playable view.");
        AssertTrue(PlayableSceneRules.PlayerHandRevealedAnchor.z >= -3.65f, "Revealed player hand should be high enough that card labels are readable without jumping too far.");
        AssertTrue(!PlayableSceneRules.HandHintLabelEnabled, "The hand rail should not print a HAND label over readable cards.");
        AssertTrue(PlayableSceneRules.StatusPanelPosition.z > 1.8f, "Status guidance should live above the hand area.");
        AssertTrue(PlayableSceneRules.CardInspectorPosition.z < -1.8f && PlayableSceneRules.CardInspectorPosition.z > PlayableSceneRules.PlayerCommandRowZ, "Card inspector should sit above the hand rail without being clipped.");
        AssertTrue(PlayableSceneRules.InfoPanelCharacterSize <= 0.02f, "Any fallback info-panel text should stay small and not block the board view.");
        AssertTrue(PlayableSceneRules.InfoPanelBackgroundScale.x <= 2.1f && PlayableSceneRules.InfoPanelBackgroundScale.y <= 0.95f, "Tabletop panels should be compact UI, not large rule-book plaques.");
        AssertTrue(PlayableSceneRules.PlayerDeckPilePosition.x < PlayableSceneRules.LeftInfoPanelX - 0.35f, "Player deck count should sit outside the card-inspector panel.");
        AssertTrue(PlayableSceneRules.EnemyDeckPilePosition.x < PlayableSceneRules.LeftInfoPanelX - 0.35f, "Enemy deck count should sit outside the status panel.");
        AssertTrue(PlayableSceneRules.PlayerDiscardPilePosition.x > 3.8f, "Player discard count should sit on the right edge, away from guide text.");
        AssertTrue(PlayableSceneRules.EnemyDiscardPilePosition.x > 3.8f, "Enemy discard count should sit on the right edge, away from guide text.");
        AssertTrue(PlayableSceneRules.PileLabelCharacterSize <= 0.03f, "Pile labels should be compact side labels, not oversized floating text.");
        AssertTrue(PlayableSceneRules.PileLabelColor.grayscale > 0.45f, "Pile labels should stay readable on the dark desk surface.");
        AssertTrue(PlayableSceneRules.PileBadgeScale.x >= 0.65f && PlayableSceneRules.PileBadgeScale.y >= 0.42f, "Pile labels need a card-like dark backing badge for readability.");
        AssertTrue(PlayableSceneRules.PileStackLayerCount >= 3, "Deck and discard piles should look like stacked cards, not loose text.");
        AssertTrue(PlayableSceneRules.PileStackLayerOffset > 0f && PlayableSceneRules.PileStackLayerOffset <= 0.04f, "Pile stack layers should be visible but compact.");
        AssertTrue(PlayableSceneRules.KreditDisplayCharacterSize >= 0.036f && PlayableSceneRules.KreditDisplayCharacterSize <= 0.046f, "Kredit counters should read as compact Kards-like side badges.");
        AssertTrue(PlayableSceneRules.KreditDisplayBackingScale.x >= 0.58f && PlayableSceneRules.KreditDisplayBackingScale.x <= 0.70f, "Kredit counters should be compact side badges, not oversized table text.");
        AssertTrue(PlayableSceneRules.PlayerKreditDisplayPosition.z < 0f && PlayableSceneRules.EnemyKreditDisplayPosition.z > 0f, "Kredit counters should sit beside each side of the board.");
        AssertTrue(PlayableSceneRules.PlayerKreditDisplayPosition.x < -5f && PlayableSceneRules.EnemyKreditDisplayPosition.x < -5f, "Kredit counters should sit on the left-side player rail where they are visible.");
        AssertTrue(PlayableSceneRules.CommandButtonPlateSize.x >= 1.55f && PlayableSceneRules.CommandButtonPlateSize.y >= 0.38f, "Command buttons need large readable plates for first-time players.");
        AssertTrue(PlayableSceneRules.CommandButtonCharacterSize <= 0.036f, "Command button labels should be compact and not dominate the table.");
        AssertTrue(PlayableSceneRules.CommandButtonPlateLocalZ > 0f, "Command button plates should sit below rotated TextMesh labels instead of covering them.");
        AssertTrue(PlayableSceneRules.FloatingTextCharacterSize <= 0.05f, "Action feedback text should stay local and not cover HQ or cards.");
        AssertTrue(PlayableSceneRules.RuntimePresenterRefreshSeconds > 0f && PlayableSceneRules.RuntimePresenterRefreshSeconds <= 1f, "Play Mode layout should refresh fast enough to clear stale generated UI.");
        AssertTrue(!PlayableSceneRules.AutoDemoActionsEnabled, "Play Mode should not auto-spend cards and Kredits while manual Unity interaction is being tested.");
        AssertTrue(PlayableSceneRules.AutoDemoActionCount >= 6 && PlayableSceneRules.AutoDemoActionCount <= 12, "Auto demo should run enough actions to cross turns without taking over indefinitely.");
        AssertTrue(PlayableSceneRules.AutoDemoActionIntervalSeconds >= 0.8f && PlayableSceneRules.AutoDemoActionIntervalSeconds <= 1.5f, "Auto demo pacing should be visible but not sluggish.");
        AssertTrue(PlayableSceneRules.CommandButtonClearsHiddenLabels, "Hidden command buttons should clear their labels instead of leaving stale action text.");
        AssertTrue(PlayableSceneRules.CommandButtonClearsStaleChildLabels, "Command buttons should clear stale child labels left by previous generated layouts.");
        AssertTrue(PlayableSceneRules.CommandColumnX >= PlayableSceneRules.PlayerHeadquartersSlot.x + 2.6f, "Command buttons should sit in the right-side rail instead of crowding headquarters cards.");
        AssertTrue(PlayableSceneRules.DeckSelectorRowZ < 3.45f, "Deck selector buttons should stay inside the table area instead of floating at the top edge.");
        AssertTrue(PlayableSceneRules.PlayerHeadquartersSlot.z < 0f, "Player HQ should be a slot-like object on the player support side.");
        AssertTrue(PlayableSceneRules.EnemyHeadquartersSlot.z > 0f, "Enemy HQ should be a slot-like object on the enemy support side.");
        AssertTrue(PlayableSceneRules.CardNumberCharacterSize > PlayableSceneRules.CardTextCharacterSize * 2f, "Card numerals should stay larger than labels.");
        AssertTrue(PlayableSceneRules.CardNumberCharacterSize <= 0.046f, "Card numerals should not cover card names, badges, or nearby cards.");
        AssertTrue(PlayableSceneRules.CardBadgeLabelCharacterSize < PlayableSceneRules.CardTextCharacterSize, "Card badge labels should be smaller than card names/rules.");
        AssertTrue(PlayableSceneRules.CardStatusCharacterSize <= PlayableSceneRules.CardTextCharacterSize, "Status labels should stay compact even when they use clear words.");
        AssertTrue(PlayableSceneRules.CardTextCharacterSize <= 0.012f, "Card rules text should be small enough to avoid overlapping art and stat badges.");
        AssertTrue(PlayableSceneRules.CardStatusBadgeWidthRatio >= 0.74f, "Status badges need enough width for labels such as SET COUNTER.");
        AssertTrue(PlayableSceneRules.HandCardTextCharacterSize <= 0.02f, "Hand card labels should be compact enough not to overlap adjacent cards.");
        AssertTrue(PlayableSceneRules.HandCardNumberCharacterSize <= 0.048f, "Hand card cost numbers should not dominate the card name.");
        AssertTrue(PlayableSceneRules.HandCardBadgeLabelsEnabled == false, "Hand cards should not show small badge words that overlap the card art.");
        AssertTrue(PlayableSceneRules.HandSpacing >= 0.82f, "Hand cards need enough spacing that labels do not overlap for first-time players.");
        AssertTrue(PlayableSceneRules.HandCardScale >= 1.15f && PlayableSceneRules.HandCardScale <= 1.30f, "Hand cards should be noticeably larger than board cards for Kards-like readability.");
        AssertTrue(PlayableSceneRules.BoardCardScale < PlayableSceneRules.HandCardScale, "Board cards should read smaller than hand cards.");
        AssertTrue(PlayableSceneRules.MulliganHandAnchor.z > PlayableSceneRules.PlayerHandRevealedAnchor.z, "Mulligan hand should appear closer to the camera in screen center.");
        AssertTrue(PlayableSceneRules.HandCardTextCharacterSize <= 0.028f, "Hand card labels should stay compact now that art and numerals carry the card read.");
        AssertTrue(PlayableSceneRules.CountermeasureCardScale >= 0.65f, "Set countermeasures must be large enough to click and inspect.");
        AssertTrue(PlayableSceneRules.CountermeasureHintPosition.z < -1.7f, "Player countermeasure hint should sit near the inspectable countermeasure row.");
        AssertTrue(PlayableSceneRules.PlayerCountermeasureAnchor.x < PlayableSceneRules.PlayerHeadquartersSlot.x, "Player countermeasures should not overlap the headquarters card.");
        AssertTrue(!PlayableSceneRules.HighlightedSlotLabelEnabled, "Slots should use visual highlights, not rule labels written on the tabletop.");
        AssertTrue(PlayableSceneRules.HighlightedSlotLabelCharacterSize <= 0.024f, "Any fallback slot label should be small enough not to cover units.");
        AssertTrue(!PlayableSceneRules.HighlightedSlotLabelBackingEnabled, "Removing tabletop slot labels also removes their backing plaques.");
        AssertTrue(PlayableSceneRules.HighlightedSlotLabelBackingColor.grayscale < 0.25f, "Highlighted slot label backing should be dark enough for bright text contrast.");
        AssertTrue(PlayableSceneRules.HighlightedSlotLabelBackingScale.x >= 0.9f, "Highlighted slot label backing should be wide enough for action labels.");
        AssertTrue(PlayableSceneRules.TabletopScale.x >= 15f, "The tabletop should fill the wide game camera instead of leaving large gray side panels.");
        AssertTrue(PlayableSceneRules.BattlefieldSurfaceScale.x >= 13f, "The playable battlefield surface should cover the wide Kards-like desk, not only the center lanes.");
        AssertTrue(PlayableSceneRules.TableBorderScale.x > PlayableSceneRules.TabletopScale.x, "Table border should frame the battlefield with a darker desktop edge.");
        AssertTrue(PlayableSceneRules.TableBorderColor.grayscale < PlayableSceneRules.TabletopColor.grayscale, "Table border should be darker than the tabletop for KARDS-like depth.");
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
