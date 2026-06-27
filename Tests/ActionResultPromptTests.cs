using System;

public static class ActionResultPromptTests
{
    public static int Main()
    {
        RuntimeCard unit = new RuntimeCard { CardName = "Infantry Section", Type = CardType.Unit };
        RuntimeCard blitzUnit = new RuntimeCard { CardName = "Forward Scouts", Type = CardType.Unit, Keywords = CardKeyword.Blitz };
        RuntimeCard costlyOperationUnit = new RuntimeCard { CardName = "Sherman Column", Type = CardType.Unit, Zone = CardZone.Frontline, OperationCost = 2 };
        RuntimeCard furyUnit = new RuntimeCard { CardName = "Field Artillery", Type = CardType.Unit, Zone = CardZone.Frontline, Keywords = CardKeyword.Fury, OperationCost = 2, AttacksThisTurn = 1 };
        RuntimeCard pinnedUnit = new RuntimeCard { CardName = "Pinned Riflemen", Type = CardType.Unit, Zone = CardZone.Frontline, Keywords = CardKeyword.Pinned, OperationCost = 1 };
        RuntimeCard actedUnit = new RuntimeCard { CardName = "Spent Riflemen", Type = CardType.Unit, HasActed = true, OperationCost = 1 };
        RuntimeCard spentAttacker = new RuntimeCard { CardName = "Spent Attacker", Type = CardType.Unit, Zone = CardZone.Frontline, OperationCost = 1, AttacksThisTurn = 1 };
        RuntimeCard supportAttacker = new RuntimeCard { CardName = "Support Riflemen", Type = CardType.Unit, Zone = CardZone.PlayerSupport, OperationCost = 1 };
        RuntimeCard friendlyTarget = new RuntimeCard { CardName = "Friendly Guard", Type = CardType.Unit, Owner = PlayerSide.Player };
        RuntimeCard enemyTarget = new RuntimeCard { CardName = "Enemy Riflemen", Type = CardType.Unit, Owner = PlayerSide.Enemy };
        RuntimeCard countermeasure = new RuntimeCard { CardName = "Prepared Defense", Type = CardType.Countermeasure };
        RuntimeCard order = new RuntimeCard { CardName = "Suppressing Fire", Type = CardType.Order };

        AssertTrue(SceneGuidanceRules.AfterDeployPrompt(unit).Contains("NEXT:"), "Deploy result should include a next-step hint.");
        AssertTrue(SceneGuidanceRules.AfterDeployPrompt(unit).Contains("NEXT TURN"), "Non-Blitz deploy result should explain that the new unit waits until next turn.");
        AssertTrue(!SceneGuidanceRules.AfterDeployPrompt(unit).Contains("SELECT A SUPPORT UNIT TO ADVANCE"), "Non-Blitz deploy result should not imply the deployed unit can act immediately.");
        AssertTrue(SceneGuidanceRules.AfterDeployPrompt(blitzUnit).Contains("ADVANCE"), "Blitz deploy result should tell players they may advance immediately.");
        AssertTrue(SceneGuidanceRules.AfterDeployPrompt(unit).Contains("END TURN"), "Deploy result should keep End Turn visible as a fallback action.");
        AssertTrue(SceneGuidanceRules.AfterCountermeasurePrompt(countermeasure).Contains("NEXT:"), "Countermeasure result should include a next-step hint.");
        AssertTrue(SceneGuidanceRules.AfterOrderPrompt(order).Contains("NEXT:"), "Order result should include a next-step hint.");
        AssertTrue(SceneGuidanceRules.AfterAdvancePrompt(unit).Contains("ATTACK"), "Advance result should tell players attacking is the next major action.");
        AssertTrue(SceneGuidanceRules.AfterAdvancePrompt(costlyOperationUnit, 1).Contains("NEED 2 KREDITS"), "Advance result should explain attack cost when remaining Kredits are insufficient.");
        AssertTrue(!SceneGuidanceRules.AfterAdvancePrompt(costlyOperationUnit, 1).Contains("SELECT A FRONTLINE UNIT TO ATTACK"), "Advance result should not point to attack targets when attack is unaffordable.");
        AssertTrue(SceneGuidanceRules.AfterAttackPrompt(unit).Contains("END TURN"), "Attack result should tell players to end the turn when finished.");
        AssertTrue(SceneGuidanceRules.AfterAttackPrompt(furyUnit, 2).Contains("FURY"), "Fury units should be called out when they can attack again.");
        AssertTrue(SceneGuidanceRules.AfterAttackPrompt(furyUnit, 2).Contains("SAME UNIT"), "Fury prompt should explain that the same unit can attack again.");
        AssertTrue(SceneGuidanceRules.AfterAttackPrompt(furyUnit, 1).Contains("NEED 2 KREDITS"), "Fury prompt should explain the cost if the second attack is unaffordable.");
        AssertTrue(SceneGuidanceRules.CannotAdvancePrompt(costlyOperationUnit, 1).Contains("NEED 2 KREDITS"), "Advance failure should show operation cost when Kredits are short.");
        AssertTrue(SceneGuidanceRules.CannotAdvancePrompt(pinnedUnit, 3).Contains("PINNED"), "Advance failure should explain pinned status.");
        AssertTrue(SceneGuidanceRules.CannotAdvancePrompt(actedUnit, 3).Contains("ALREADY ACTED"), "Advance failure should explain spent units.");
        AssertTrue(SceneGuidanceRules.CannotAttackPrompt(costlyOperationUnit, 1).Contains("NEED 2 KREDITS"), "Attack failure should show operation cost when Kredits are short.");
        AssertTrue(SceneGuidanceRules.CannotAttackPrompt(pinnedUnit, 3).Contains("PINNED"), "Attack failure should explain pinned status.");
        AssertTrue(SceneGuidanceRules.CannotAttackPrompt(spentAttacker, 3).Contains("ALREADY ATTACKED"), "Attack failure should explain spent attackers.");
        AssertTrue(SceneGuidanceRules.CannotAttackPrompt(supportAttacker, 3).Contains("ADVANCE"), "Support-line attackers should explain that they must advance before attacking.");
        AssertTrue(SceneGuidanceRules.IllegalAttackTargetPrompt(unit, friendlyTarget, SlotZone.PlayerSupport, false).Contains("ENEMY"), "Friendly targets should explain attacks need enemy targets.");
        AssertTrue(SceneGuidanceRules.IllegalAttackTargetPrompt(unit, enemyTarget, SlotZone.Frontline, false).Contains("SUPPORT"), "Wrong zone targets should explain support-line targeting.");
        AssertTrue(SceneGuidanceRules.IllegalAttackTargetPrompt(unit, enemyTarget, SlotZone.EnemySupport, true).Contains("GUARD"), "Guard should explain why non-Guard units cannot be attacked.");
        AssertTrue(SceneGuidanceRules.IllegalAttackTargetPrompt(unit, null, SlotZone.EnemySupport, true).Contains("GUARD"), "Guard should explain why HQ cannot be attacked.");
        AssertTrue(SceneGuidanceRules.NoAdvanceShortcutPrompt(false, false, false, false, false, false).Contains("SUPPORT"), "Advance shortcut should explain when no support unit exists.");
        AssertTrue(SceneGuidanceRules.NoAdvanceShortcutPrompt(true, true, false, false, false, false).Contains("KREDITS"), "Advance shortcut should explain operation cost blockers.");
        AssertTrue(SceneGuidanceRules.NoAdvanceShortcutPrompt(true, false, true, false, false, false).Contains("PINNED"), "Advance shortcut should explain pinned blockers.");
        AssertTrue(SceneGuidanceRules.NoAdvanceShortcutPrompt(true, false, false, true, false, false).Contains("ALREADY ACTED"), "Advance shortcut should explain spent blockers.");
        AssertTrue(SceneGuidanceRules.NoAdvanceShortcutPrompt(true, false, false, false, false, true).Contains("FRONTLINE"), "Advance shortcut should explain full frontline blockers.");
        AssertTrue(SceneGuidanceRules.NoAttackShortcutPrompt(false, false, false, false, false).Contains("FRONTLINE"), "Attack shortcut should explain when no frontline unit exists.");
        AssertTrue(SceneGuidanceRules.NoAttackShortcutPrompt(true, true, false, false, false).Contains("KREDITS"), "Attack shortcut should explain operation cost blockers.");
        AssertTrue(SceneGuidanceRules.NoAttackShortcutPrompt(true, false, true, false, false).Contains("PINNED"), "Attack shortcut should explain pinned blockers.");
        AssertTrue(SceneGuidanceRules.NoAttackShortcutPrompt(true, false, false, true, false).Contains("ALREADY ATTACKED"), "Attack shortcut should explain spent attackers.");
        AssertTrue(SceneGuidanceRules.NoAttackShortcutPrompt(true, false, false, false, true).Contains("TARGET"), "Attack shortcut should explain missing legal targets.");
        AssertTrue(SceneGuidanceRules.ShortcutBlockedPrompt("P", GamePhase.EnemyTurn, PlayerSide.Enemy).Contains("ENEMY TURN"), "Blocked shortcuts should explain enemy turn timing.");
        AssertTrue(SceneGuidanceRules.ShortcutBlockedPrompt("A", GamePhase.DeckBuilder, PlayerSide.Player).Contains("START MATCH"), "Blocked shortcuts before the match should tell players to start the match.");
        AssertTrue(SceneGuidanceRules.ShortcutBlockedPrompt("F", GamePhase.PlayerTurn, PlayerSide.Enemy).Contains("WAIT"), "Blocked shortcuts while enemy is active should tell players to wait.");
        AssertTrue(SceneGuidanceRules.ShortcutBlockedPrompt("N", GamePhase.EnemyTurn, PlayerSide.Enemy).Contains("ENEMY TURN"), "Recommended-action shortcut should explain when the enemy is active.");
        AssertTrue(SceneGuidanceRules.HelpPrompt().Contains("N AUTO-ACTION"), "Help should advertise the one-key recommended action for easier playtesting.");
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
