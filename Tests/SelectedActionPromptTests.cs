using System;

public static class SelectedActionPromptTests
{
    public static int Main()
    {
        RuntimeCard handUnit = new RuntimeCard { CardName = "Forward Scouts", Type = CardType.Unit, Zone = CardZone.Hand };
        RuntimeCard mobilizeUnit = new RuntimeCard { CardName = "Fast Recon", Type = CardType.Unit, Zone = CardZone.Hand, Keywords = CardKeyword.Mobilize };
        RuntimeCard countermeasure = new RuntimeCard { CardName = "Prepared Defense", Type = CardType.Countermeasure, Zone = CardZone.Hand };
        RuntimeCard order = new RuntimeCard { CardName = "Suppressing Fire", Type = CardType.Order, Zone = CardZone.Hand };
        RuntimeCard supportUnit = new RuntimeCard { CardName = "M5 Stuart", Type = CardType.Unit, Zone = CardZone.PlayerSupport };
        RuntimeCard frontlineUnit = new RuntimeCard { CardName = "B-25 Mitchell", Type = CardType.Unit, Zone = CardZone.Frontline };
        RuntimeCard pinnedSupportUnit = new RuntimeCard { CardName = "Pinned Stuart", Type = CardType.Unit, Zone = CardZone.PlayerSupport, Keywords = CardKeyword.Pinned, OperationCost = 1 };
        RuntimeCard spentFrontlineUnit = new RuntimeCard { CardName = "Spent Mitchell", Type = CardType.Unit, Zone = CardZone.Frontline, AttacksThisTurn = 1, OperationCost = 1 };
        RuntimeCard setCountermeasure = new RuntimeCard { CardName = "Prepared Defense", Type = CardType.Countermeasure, Zone = CardZone.Countermeasure };
        RuntimeCard expensiveUnit = new RuntimeCard { CardName = "Sherman Column", Type = CardType.Unit, Zone = CardZone.Hand, KreditCost = 3 };
        RuntimeCard damageOrder = new RuntimeCard { CardName = "Air Strike", Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.DamageTargetUnit };
        RuntimeCard pinOrder = new RuntimeCard { CardName = "Suppressing Fire", Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.PinTargetUnit };
        RuntimeCard buffOrder = new RuntimeCard { CardName = "Ambush Orders", Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.BuffFriendlyUnit };
        RuntimeCard enemySmokescreen = new RuntimeCard { CardName = "Supply Convoy", Type = CardType.Unit, Owner = PlayerSide.Enemy, Keywords = CardKeyword.Smokescreen };
        RuntimeCard friendlyUnit = new RuntimeCard { CardName = "Forward Scouts", Type = CardType.Unit, Owner = PlayerSide.Player };
        RuntimeCard enemyUnit = new RuntimeCard { CardName = "Panzergrenadier", Type = CardType.Unit, Owner = PlayerSide.Enemy };

        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(handUnit).Contains("CLICK DEPLOY HERE"), "Selected hand units should tell players to click a deploy target.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(mobilizeUnit).Contains("MOBILIZE"), "Selected Mobilize units should tell players about direct frontline deployment.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(mobilizeUnit, 3, true, PlayerSide.Enemy).Contains("ENEMY CONTROLS"), "Selected Mobilize units should explain enemy frontline control before offering frontline deployment.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(mobilizeUnit, 3, true, PlayerSide.Enemy).Contains("OR MOBILIZE"), "Selected Mobilize units should not offer frontline deployment while enemy controls frontline.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(expensiveUnit, 1).Contains("NEED 3 KREDITS"), "Unaffordable hand cards should explain the Kredit requirement before target selection.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(expensiveUnit, 1).Contains("CLICK DEPLOY HERE"), "Unaffordable hand cards should not tell players to click a deploy target.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(countermeasure).Contains("CLICK BOARD TO SET COUNTER"), "Selected countermeasures should tell players they can click the board instead of dragging.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(order).Contains("CLICK BOARD"), "Selected orders should tell players they can click the board instead of dragging.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(supportUnit).Contains("CLICK ADVANCE HERE"), "Selected support units should tell players to click an advance target.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(supportUnit, 3, true, PlayerSide.Enemy).Contains("ENEMY CONTROLS"), "Selected support units should explain enemy frontline control before asking for advance targets.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(supportUnit, 3, true, PlayerSide.Enemy).Contains("CLICK ADVANCE HERE"), "Selected support units should not ask for advance targets while enemy controls the frontline.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(frontlineUnit).Contains("CLICK ATTACK TARGET OR HQ"), "Selected frontline units should tell players they can attack units or headquarters.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(pinnedSupportUnit, 3).Contains("PINNED"), "Selected pinned support units should explain why they cannot advance.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(pinnedSupportUnit, 3).Contains("CLICK ADVANCE HERE"), "Selected pinned support units should not ask for advance targets.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(spentFrontlineUnit, 3).Contains("ALREADY ATTACKED"), "Selected spent frontline units should explain why they cannot attack.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(spentFrontlineUnit, 3).Contains("CLICK ATTACK TARGET"), "Selected spent frontline units should not ask for attack targets.");
        AssertTrue(SceneGuidanceRules.SelectedActionPrompt(setCountermeasure).Contains("CHECKING SET COUNTER"), "Set player countermeasures should inspect without asking for a target.");
        AssertTrue(!SceneGuidanceRules.SelectedActionPrompt(setCountermeasure).Contains("CLICK SET COUNTER"), "Set countermeasures should not look playable again.");
        AssertTrue(SceneGuidanceRules.IllegalOrderTargetPrompt(damageOrder, enemySmokescreen, PlayerSide.Player).Contains("SMOKESCREEN"), "Smokescreen should explain why damage orders cannot target the unit.");
        AssertTrue(SceneGuidanceRules.IllegalOrderTargetPrompt(pinOrder, friendlyUnit, PlayerSide.Player).Contains("ENEMY UNIT"), "Pin orders should explain that they need an enemy unit.");
        AssertTrue(SceneGuidanceRules.IllegalOrderTargetPrompt(buffOrder, enemyUnit, PlayerSide.Player).Contains("FRIENDLY UNIT"), "Buff orders should explain that they need a friendly unit.");
        AssertTrue(SceneGuidanceRules.IllegalDeployTargetPrompt(handUnit, SlotZone.Frontline, false, false, PlayerSide.Player).Contains("SUPPORT"), "Non-Mobilize units should explain that they deploy to support.");
        AssertTrue(SceneGuidanceRules.IllegalDeployTargetPrompt(mobilizeUnit, SlotZone.Frontline, false, true, PlayerSide.Enemy).Contains("CONTROL"), "Mobilize units should explain frontline control restrictions.");
        AssertTrue(SceneGuidanceRules.IllegalDeployTargetPrompt(mobilizeUnit, SlotZone.PlayerSupport, true, false, PlayerSide.Player).Contains("OCCUPIED"), "Occupied deployment targets should explain that the slot is occupied.");
        AssertTrue(SceneGuidanceRules.CannotAffordCardPrompt(expensiveUnit, "DEPLOY", 1).Contains("SHERMAN COLUMN"), "Unaffordable direct deploy attempts should name the card.");
        AssertTrue(SceneGuidanceRules.CannotAffordCardPrompt(expensiveUnit, "DEPLOY", 1).Contains("NEED 3 KREDITS"), "Unaffordable direct deploy attempts should show the required card cost.");
        AssertTrue(SceneGuidanceRules.CannotAffordCardPrompt(expensiveUnit, "DEPLOY", 1).Contains("HAVE 1"), "Unaffordable direct deploy attempts should show current Kredits.");
        AssertTrue(SceneGuidanceRules.CountermeasureRowFullPrompt(countermeasure).Contains("COUNTERMEASURE ROW FULL"), "Full countermeasure rows should explain the zone limit.");
        AssertTrue(SceneGuidanceRules.CountermeasureRowFullPrompt(countermeasure).Contains("MAX 3"), "Full countermeasure rows should tell players the countermeasure limit.");
        AssertTrue(SceneGuidanceRules.HeadquartersClickedPrompt(PlayerSide.Enemy).Contains("ENEMY HQ"), "Clicked enemy headquarters should identify the target.");
        AssertTrue(SceneGuidanceRules.HeadquartersClickedPrompt(PlayerSide.Enemy).Contains("SELECT A FRONTLINE UNIT"), "Clicked enemy headquarters should explain how to attack it.");
        AssertTrue(SceneGuidanceRules.HeadquartersClickedPrompt(PlayerSide.Player).Contains("YOUR HQ"), "Clicked player headquarters should identify the friendly headquarters.");
        AssertTrue(SceneGuidanceRules.EmptySlotClickedPrompt(SlotZone.PlayerSupport).Contains("HAND UNIT"), "Clicked empty player support slots should tell players how to deploy.");
        AssertTrue(SceneGuidanceRules.EmptySlotClickedPrompt(SlotZone.Frontline).Contains("ADVANCE"), "Clicked empty frontline slots should tell players how to move units there.");
        AssertTrue(SceneGuidanceRules.EmptySlotClickedPrompt(SlotZone.EnemySupport).Contains("ENEMY SUPPORT"), "Clicked empty enemy support slots should identify that lane.");
        AssertTrue(SceneGuidanceRules.IllegalHeadquartersTargetPrompt(handUnit, PlayerSide.Player).Contains("NOT A DEPLOY SLOT"), "Hand units clicked on headquarters should explain that HQ is not a deploy target.");
        AssertTrue(SceneGuidanceRules.IllegalHeadquartersTargetPrompt(supportUnit, PlayerSide.Enemy).Contains("ADVANCE TO FRONTLINE"), "Support units clicked on enemy headquarters should explain that they must advance first.");
        AssertTrue(SceneGuidanceRules.IllegalHeadquartersTargetPrompt(frontlineUnit, PlayerSide.Player).Contains("ENEMY HQ"), "Frontline units clicked on friendly headquarters should direct attacks toward enemy HQ.");
        AssertTrue(SceneGuidanceRules.OpponentCardClickedPrompt(enemyUnit).Contains("INSPECTING ENEMY"), "Clicked enemy cards should tell players they are inspecting an enemy card.");
        AssertTrue(SceneGuidanceRules.OpponentCardClickedPrompt(enemyUnit).Contains("SELECT AN ORDER OR FRONTLINE UNIT"), "Clicked enemy cards should explain how to target them.");
        AssertTrue(SceneGuidanceRules.IllegalOpponentCardTargetPrompt(handUnit, enemyUnit).Contains("DEPLOY TO SUPPORT"), "Hand units clicked on enemy cards should explain deployment instead of silently returning.");
        AssertTrue(SceneGuidanceRules.IllegalOpponentCardTargetPrompt(supportUnit, enemyUnit).Contains("ADVANCE TO FRONTLINE"), "Support units clicked on enemy cards should explain that they cannot attack from support.");
        AssertTrue(SceneGuidanceRules.OwnCardClickedWhileHandUnitSelectedPrompt(handUnit, friendlyUnit).Contains("OCCUPIED"), "Hand units clicked on friendly occupied cards should explain the slot is occupied.");
        AssertTrue(SceneGuidanceRules.OwnCardClickedWhileHandUnitSelectedPrompt(handUnit, friendlyUnit).Contains("EMPTY SUPPORT"), "Hand units clicked on friendly occupied cards should direct players to empty support slots.");
        AssertTrue(SceneGuidanceRules.MissedDragTargetPrompt(handUnit).Contains("MISSED BOARD TARGET"), "Dragging a card away from the board should explain that no target was hit.");
        AssertTrue(SceneGuidanceRules.MissedDragTargetPrompt(handUnit).Contains("DROP ON A HIGHLIGHTED SLOT"), "Missed drag feedback should tell players where to drop cards.");
        AssertTrue(SceneGuidanceRules.NoPlayableCardPrompt(false, false, false, false).Contains("HAND"), "No-play shortcut should explain an empty hand.");
        AssertTrue(SceneGuidanceRules.NoPlayableCardPrompt(true, false, false, false).Contains("KREDITS"), "No-play shortcut should explain when all cards are unaffordable.");
        AssertTrue(SceneGuidanceRules.NoPlayableCardPrompt(true, true, true, false).Contains("SUPPORT"), "No-play shortcut should explain when unit deployment is blocked by full support.");
        AssertTrue(SceneGuidanceRules.NoPlayableCardPrompt(true, true, false, true).Contains("ORDER"), "No-play shortcut should explain when orders lack legal targets.");
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
