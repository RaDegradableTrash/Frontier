using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    private System.Collections.IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(0.35f);
        EnemyPlaySimpleOrder();
        yield return new WaitForSeconds(0.35f);
        EnemyDeployFirstPlayable();
        yield return new WaitForSeconds(0.35f);
        EnemyAdvanceFirstSupportUnit();
        yield return new WaitForSeconds(0.35f);
        EnemyAttackWithFrontline();
        if (phase == GamePhase.GameOver)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.35f);
        StartTurn(PlayerSide.Player);
    }

    private void PlayFirstAvailableCard()
    {
        TryPlayFirstAvailableCard(true);
    }

    private bool TryPlayFirstAvailableCard(bool showFailure)
    {
        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player || isResolvingEvents)
        {
            if (showFailure)
            {
                SetStatus(SceneGuidanceRules.ShortcutBlockedPrompt("P", phase, activeSide));
            }
            return false;
        }

        bool hasHandCards = player.Hand.Count > 0;
        bool hasAffordableCard = false;
        bool supportFull = false;
        bool missingOrderTarget = false;

        foreach (RuntimeCard card in new List<RuntimeCard>(player.Hand))
        {
            if (!player.CanSpendKredits(HandPlayCost(player, card)))
            {
                continue;
            }

            hasAffordableCard = true;
            if (card.Type == CardType.Unit)
            {
                SlotInteract slot = FindEmptySlot(SlotZone.PlayerSupport);
                if (slot == null)
                {
                    supportFull = true;
                    continue;
                }

                selectedCard = card;
                if (TryDeploySelectedUnit(slot))
                {
                    return true;
                }

                ClearSelection();
                continue;
            }

            if (card.Type == CardType.Countermeasure)
            {
                TrySetCountermeasure(player, card);
                return true;
            }

            if (card.Type == CardType.Order)
            {
                SlotInteract target = FindQuickOrderTarget(card);
                if (!IsLegalOrderTarget(card, target, PlayerSide.Player))
                {
                    missingOrderTarget = true;
                    continue;
                }

                selectedCard = card;
                if (TryPlayOrderOnSlot(target))
                {
                    return true;
                }

                ClearSelection();
            }
        }

        ClearSelection();
        if (showFailure)
        {
            SetStatus(SceneGuidanceRules.NoPlayableCardPrompt(hasHandCards, hasAffordableCard, supportFull, missingOrderTarget));
        }
        return false;
    }

    private void AdvanceFirstAvailableUnit()
    {
        TryAdvanceFirstAvailableUnit(true);
    }

    private bool TryAdvanceFirstAvailableUnit(bool showFailure)
    {
        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player || isResolvingEvents)
        {
            if (showFailure)
            {
                SetStatus(SceneGuidanceRules.ShortcutBlockedPrompt("A", phase, activeSide));
            }
            return false;
        }

        SlotInteract destination = FindEmptySlot(SlotZone.Frontline);
        if (destination == null)
        {
            if (showFailure)
            {
                SetStatus(SceneGuidanceRules.NoAdvanceShortcutPrompt(true, false, false, false, false, true));
            }
            return false;
        }

        bool hasMovableUnit = false;
        bool needsKredits = false;
        bool pinned = false;
        bool alreadyActed = false;

        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Player || !IsBoardCombatUnit(card))
            {
                continue;
            }

            hasMovableUnit = true;
            int availableOperation;
            if (!CanSpendUnitOperation(card, player.Kredits, out availableOperation))
            {
                needsKredits = true;
                continue;
            }

            if (card.HasKeyword(CardKeyword.Pinned))
            {
                pinned = true;
                continue;
            }

            if (card.HasActed)
            {
                alreadyActed = true;
                continue;
            }

            selectedCard = card;
            if (TryMoveToSlot(card, destination))
            {
                return true;
            }

            ClearSelection();
        }

        ClearSelection();
        if (showFailure)
        {
            SetStatus(SceneGuidanceRules.NoAdvanceShortcutPrompt(hasMovableUnit, needsKredits, pinned, alreadyActed, false, false));
        }
        return false;
    }

    private void AttackWithFirstAvailableUnit()
    {
        TryAttackWithFirstAvailableUnit(true);
    }

    private bool TryAttackWithFirstAvailableUnit(bool showFailure)
    {
        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player || isResolvingEvents)
        {
            if (showFailure)
            {
                SetStatus(SceneGuidanceRules.ShortcutBlockedPrompt("F", phase, activeSide));
            }
            return false;
        }

        bool hasAttackUnit = false;
        bool needsKredits = false;
        bool pinned = false;
        bool alreadyAttacked = false;
        bool missingTarget = false;

        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Player || !IsBoardCombatUnit(card))
            {
                continue;
            }

            hasAttackUnit = true;
            int availableOperation;
            if (!CanSpendUnitOperation(card, player.Kredits, out availableOperation))
            {
                needsKredits = true;
                continue;
            }

            if (card.HasKeyword(CardKeyword.Pinned))
            {
                pinned = true;
                continue;
            }

            int maxAttacks = card.HasKeyword(CardKeyword.Fury) ? 2 : 1;
            if (card.AttacksThisTurn >= maxAttacks)
            {
                alreadyAttacked = true;
                continue;
            }

            SlotInteract target = FindQuickAttackTarget(card);
            if (target == null)
            {
                missingTarget = true;
                continue;
            }

            selectedCard = card;
            if (TryAttack(target))
            {
                return true;
            }

            ClearSelection();
        }

        ClearSelection();
        if (showFailure)
        {
            SetStatus(SceneGuidanceRules.NoAttackShortcutPrompt(hasAttackUnit, needsKredits, pinned, alreadyAttacked, missingTarget));
        }
        return false;
    }

    private void ExecuteRecommendedAction()
    {
        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player || isResolvingEvents)
        {
            SetStatus(SceneGuidanceRules.ShortcutBlockedPrompt("N", phase, activeSide));
            return;
        }

        if (TryAttackWithFirstAvailableUnit(false))
        {
            return;
        }

        if (TryAdvanceFirstAvailableUnit(false))
        {
            return;
        }

        if (TryPlayFirstAvailableCard(false))
        {
            return;
        }

        SetStatus("N NEXT — NO ACTION AVAILABLE, ENDING TURN.");
        ExecuteSceneCommand(SceneCommandType.EndTurn);
    }

    private SlotInteract FindQuickOrderTarget(RuntimeCard card)
    {
        switch (card.EffectType)
        {
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.DamageTargetUnitAndAdjacent:
            case CardEffectType.PinTargetUnit:
                return FindOccupiedSlot(SlotZone.EnemySupport, PlayerSide.Enemy);
            case CardEffectType.BuffFriendlyUnit:
                return FindOccupiedSlot(SlotZone.PlayerSupport, PlayerSide.Player);
            default:
                return board.GetSlot(0, SlotZone.Frontline);
        }
    }

    private SlotInteract FindQuickAttackTarget(RuntimeCard attacker)
    {
        PlayerSide defender = attacker != null && attacker.Owner == PlayerSide.Enemy ? PlayerSide.Player : PlayerSide.Enemy;
        SlotInteract guardedTarget = FindGuardSlot(SlotZone.EnemySupport, defender);
        if (guardedTarget != null && IsLegalAttackTarget(attacker, guardedTarget))
        {
            return guardedTarget;
        }

        SlotInteract occupiedTarget = FindOccupiedSlot(SlotZone.EnemySupport, defender);
        if (occupiedTarget != null && IsLegalAttackTarget(attacker, occupiedTarget))
        {
            return occupiedTarget;
        }

        SlotInteract headquartersTarget = board.GetHeadquartersSlot(defender);
        return headquartersTarget != null && IsLegalAttackTarget(attacker, headquartersTarget) ? headquartersTarget : null;
    }

    private void EnemyPlaySimpleOrder()
    {
        RuntimeCard order = BestEnemyOrder(out SlotInteract target);
        if (order == null || target == null)
        {
            return;
        }

        PlayOrder(enemy, order, target);
        RefreshAllViews();
    }

    private RuntimeCard BestEnemyOrder(out SlotInteract bestTarget)
    {
        RuntimeCard bestOrder = null;
        bestTarget = null;
        int bestScore = int.MinValue;

        foreach (RuntimeCard order in enemy.Hand)
        {
            if (order.Type != CardType.Order || !enemy.CanSpendKredits(order.KreditCost))
            {
                continue;
            }

            SlotInteract target = ChooseEnemyOrderTarget(order);
            if (!IsLegalOrderTarget(order, target, PlayerSide.Enemy))
            {
                continue;
            }

            int score = OrderScore(order, target);
            if (score > bestScore)
            {
                bestOrder = order;
                bestTarget = target;
                bestScore = score;
            }
        }

        return bestOrder;
    }

    private int OrderScore(RuntimeCard order, SlotInteract target)
    {
        switch (order.EffectType)
        {
            case CardEffectType.DamageEnemyHeadquarters:
                return player.HeadquartersHealth <= order.EffectAmount ? 100 : 12 + order.EffectAmount * 2;
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return target != null && target.IsOccupied ? TargetPriority(target.Occupant) + order.EffectAmount * 4 : -999;
            case CardEffectType.PinTargetUnit:
                return target != null && target.IsOccupied ? TargetPriority(target.Occupant) + 6 : -999;
            case CardEffectType.RepairHeadquarters:
                return enemy.HeadquartersHealth <= 12 ? 16 + (20 - enemy.HeadquartersHealth) : 2;
            case CardEffectType.DrawCards:
                return enemy.Hand.Count <= 4 ? 14 : 4;
            case CardEffectType.DeployWithBlitz:
                return target != null && BestAirborneUnit(enemy) != null ? 18 + UnitScore(BestAirborneUnit(enemy)) : -999;
            case CardEffectType.BuffFriendlyUnit:
                return target != null && target.IsOccupied ? UnitScore(target.Occupant) + 6 : -999;
            default:
                return 0;
        }
    }

    private SlotInteract ChooseEnemyOrderTarget(RuntimeCard order)
    {
        switch (order.EffectType)
        {
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.PinTargetUnit:
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return FindHighestPriorityTarget(PlayerSide.Player);
            case CardEffectType.DeployWithBlitz:
                return FindAirborneDeploymentSlot(PlayerSide.Enemy);
            case CardEffectType.BuffFriendlyUnit:
                return FindHighestValueFriendlyUnit(PlayerSide.Enemy);
            default:
                return board.GetSlot(0, SlotZone.Frontline);
        }
    }

    private void EnemyDeployFirstPlayable()
    {
        RuntimeCard countermeasure = enemy.Hand.Find(item => item.Type == CardType.Countermeasure && item.KreditCost <= enemy.Kredits);
        if (countermeasure != null && enemy.Countermeasures.Count == 0)
        {
            TrySetCountermeasure(enemy, countermeasure);
            return;
        }

        RuntimeCard card = BestPlayableEnemyUnit();
        SlotInteract slot = FindEnemyDeploymentSlot(card);
        if (card == null || slot == null)
        {
            return;
        }

        if (!enemy.TrySpendKredits(HandPlayCost(enemy, card)))
        {
            return;
        }

        enemy.Hand.Remove(card);
        UnitDeploymentRules.MarkDeployed(card);
        PlaceCardInSlot(card, slot, PlacementZoneForSlot(slot));
        SpawnFloatingText("DEPLOY", slot.transform.position, Color.red);
        ResolveDeploymentEffect(enemy, card, slot);
        SetStatus($"Enemy deployed {card.CardName}.");
        RefreshAllViews();
    }

    private SlotInteract FindEnemyDeploymentSlot(RuntimeCard card)
    {
        return FindEmptySlot(SlotZone.EnemySupport);
    }

    private SlotInteract FindAirborneDeploymentSlot(PlayerSide owner)
    {
        return FindEmptySlot(SupportZoneFor(owner));
    }

    private RuntimeCard BestAirborneUnit(PlayerState state)
    {
        if (state == null)
        {
            return null;
        }

        RuntimeCard best = null;
        int bestScore = int.MinValue;
        foreach (RuntimeCard card in state.Hand)
        {
            if (card == null || card.Type != CardType.Unit)
            {
                continue;
            }

            int score = UnitScore(card);
            if (score > bestScore)
            {
                best = card;
                bestScore = score;
            }
        }

        return best;
    }

    private void EnemyAdvanceFirstSupportUnit()
    {
        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Enemy || !IsBoardCombatUnit(card) || !card.CanOperate(enemy.Kredits))
            {
                continue;
            }

            SlotInteract destination = FindEmptySlot(SlotZone.Frontline);
            if (destination == null)
            {
                return;
            }

            if (!enemy.TrySpendKredits(EffectiveOperationCostForAction(card)))
            {
                return;
            }

            MoveCardToSlot(card, destination, PlacementZoneForSlot(destination));
            card.HasActed = true;
            SpawnFloatingText("MOVE", destination.transform.position, Color.yellow);
            UpdateFrontlineControl();
            SetStatus($"Enemy moved {card.CardName}.");
            RefreshAllViews();
            return;
        }
    }

    private void EnemyAttackWithFrontline()
    {
        RuntimeCard card = BestEnemyAttacker();
        SlotInteract target = FindBestAttackTarget(card);
        if (card == null || !IsLegalAttackTarget(card, target))
        {
            return;
        }

        if (!enemy.TrySpendKredits(EffectiveOperationCostForAction(card)))
        {
            return;
        }

        ResolveAttack(card, target);
        RefreshAllViews();
    }
}
