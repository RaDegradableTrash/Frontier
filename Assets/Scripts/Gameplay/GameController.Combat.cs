using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    private void ApplyDefenseGainToFriendlyUnits(PlayerState owner, int amount)
    {
        if (owner == null || amount == 0)
        {
            return;
        }

        foreach (RuntimeCard card in cardSlots.Keys)
        {
            if (card == null || card.Owner != owner.Side || !IsBoardCombatUnit(card))
            {
                continue;
            }

            card.CurrentDefense += amount;
        }
    }

    private void StartTurn(PlayerSide side)
    {
        SetActiveSide(side);
        SetGamePhase(side == PlayerSide.Player ? GamePhase.PlayerTurn : GamePhase.EnemyTurn);
        PlayerState state = GetState(side);
        ResolveFieldIntelDraws(state);
        state.StartTurn();
        ReadyUnits(side);
        DrawCard(state);
        UpdateFrontlineControl();
        SetStatus(side == PlayerSide.Player ? "Your turn: deploy, advance, order, or attack." : "Enemy turn.");
        if (side == PlayerSide.Player)
        {
            RevealPlayerHandBriefly();
        }
        RefreshAllViews();

        if (side == PlayerSide.Enemy)
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    private void EndPlayerTurn()
    {
        ClearSelection();
        StartTurn(PlayerSide.Enemy);
    }

    private void ReadyUnits(PlayerSide side)
    {
        foreach (RuntimeCard card in cardSlots.Keys)
        {
            if (card.Owner == side)
            {
                UnitTurnRules.ReadyForTurn(card);
            }
        }
    }

    private void RejectSelectedHandCard(string message)
    {
        SetStatus(message);
        ClearCardInspectState();
        CancelAllCardPointerInteractions();
        ClearSelection();
        ClearDragPreview();
        RefreshSceneInspector();
        RefreshAllViews();
    }

    private void TrySelectUnitInSlot(SlotInteract slot)
    {
        if (!slot.IsOccupied || slot.Occupant.Owner != PlayerSide.Player)
        {
            return;
        }

        CardView view = FindView(slot.Occupant);
        SelectCard(slot.Occupant, view);
    }

    private bool TryDeploySelectedUnit(SlotInteract slot)
    {
        if (slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot))
        {
            RejectSelectedHandCard(SceneGuidanceRules.IllegalDeployTargetPrompt(selectedCard, slot.Zone, slot.IsOccupied, hasFrontlineController, frontlineController));
            return false;
        }

        int deploymentCost = player.EffectiveDeploymentCost(selectedCard);
        if (!player.TrySpendDeploymentCost(deploymentCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(selectedCard, "deploy", player.Kredits));
            return false;
        }

        RuntimeCard deployedCard = selectedCard;
        Vector3 deployFrom = selectedView != null
            ? selectedView.transform.position
            : HandPosition(PlayerSide.Player, player.Hand.IndexOf(deployedCard), player.Hand.Count);
        player.Hand.Remove(deployedCard);
        UnitDeploymentRules.MarkDeployed(deployedCard);
        pendingDeployDropCardId = deployedCard.Id;
        PlaceCardInSlot(deployedCard, slot, PlacementZoneForSlot(slot));
        SpawnFloatingText("DEPLOY", slot.transform.position, Color.cyan);
        player.RegisterCardPlayed();
        ResolveDeploymentEffect(player, deployedCard, slot);
        if (DeployStrikeRules.ShouldTriggerStrike(deployedCard))
        {
            board.TriggerStrike(slot);
            StartCoroutine(ReanchorBoardCardsAfterStrike());
        }

        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterDeployPrompt(deployedCard));
        ClearSelection();
        RefreshAllViews();
        CardView deployedView = FindView(deployedCard);
        if (deployedView != null)
        {
            deployedView.PlayDeployDrop(deployFrom, slot.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight);
            deployedView.RefreshKeywordIcons(true);
        }

        pendingDeployDropCardId = null;
        return true;
    }

    private System.Collections.IEnumerator ReanchorBoardCardsAfterStrike()
    {
        yield return new WaitForSeconds(0.72f);
        RefreshAllViews();
    }

    private bool TryMoveToFrontline(SlotInteract destination)
    {
        return TryMoveToSlot(selectedCard, destination);
    }

    private bool TryMoveToSlot(RuntimeCard card, SlotInteract destination)
    {
        if (card == null || destination == null || destination.IsOccupied || BoardTargetRules.IsHeadquartersSlot(destination))
        {
            SetStatus("Select an empty grid slot.");
            return false;
        }

        if (!CanMoveOnBoard(card))
        {
            return false;
        }

        PlayerState owner = GetState(card.Owner);
        if (!owner.TrySpendKredits(EffectiveOperationCostForAction(card)))
        {
            SetStatus(SceneGuidanceRules.CannotAdvancePrompt(card, owner.Kredits));
            return false;
        }

        MoveCardToSlot(card, destination, PlacementZoneForSlot(destination));
        card.HasActed = true;
        SpawnFloatingText("MOVE", destination.transform.position, Color.yellow);
        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterAdvancePrompt(card, owner.Kredits));
        ClearSelection();
        RefreshAllViews();
        return true;
    }

    private bool CanMoveToFrontline(RuntimeCard card)
    {
        return CanMoveOnBoard(card);
    }

    private bool CanMoveOnBoard(RuntimeCard card)
    {
        if (!CanSpendUnitOperation(card, player.Kredits, out int requiredOperationCost))
        {
            SetStatus(SceneGuidanceRules.CannotAdvancePrompt(card, player.Kredits));
            return false;
        }

        if (requiredOperationCost > player.Kredits)
        {
            SetStatus(SceneGuidanceRules.CannotAdvancePrompt(card, player.Kredits));
            return false;
        }

        return true;
    }

    private bool TryAttack(SlotInteract targetSlot)
    {
        if (!CanAttack(selectedCard, player.Kredits))
        {
            SetStatus(SceneGuidanceRules.CannotAttackPrompt(selectedCard, player.Kredits));
            return false;
        }

        if (!IsLegalAttackTarget(selectedCard, targetSlot))
        {
            bool defenderHasGuard = selectedCard != null && IsProtectedByAdjacentGuard(targetSlot, GetOpponentState(selectedCard.Owner).Side);
            SetStatus(SceneGuidanceRules.IllegalAttackTargetPrompt(selectedCard, targetSlot != null ? targetSlot.Occupant : null, targetSlot != null ? targetSlot.Zone : SlotZone.Frontline, defenderHasGuard));
            return false;
        }

        if (!player.TrySpendKredits(EffectiveOperationCostForAction(selectedCard)))
        {
            SetStatus(SceneGuidanceRules.CannotAttackPrompt(selectedCard, player.Kredits));
            return false;
        }

        ResolveAttack(selectedCard, targetSlot);
        ClearSelection();
        RefreshAllViews();
        return true;
    }

    private bool CanAttack(RuntimeCard attacker, int availableKredits)
    {
        bool canAttackFromZone = attacker != null
            && (attacker.Zone == CardZone.Frontline || attacker.Zone == CardZone.PlayerSupport || attacker.Zone == CardZone.EnemySupport);
        if (attacker == null || attacker.Type != CardType.Unit || !canAttackFromZone || attacker.HasActed || attacker.HasKeyword(CardKeyword.Pinned))
        {
            return false;
        }

        int availableOperation;
        if (!CanSpendUnitOperation(attacker, availableKredits, out availableOperation))
        {
            return false;
        }

        int maxAttacks = attacker.HasKeyword(CardKeyword.Fury) ? 2 : 1;
        return attacker.AttacksThisTurn < maxAttacks;
    }

    private bool IsLegalAttackTarget(RuntimeCard attacker, SlotInteract targetSlot)
    {
        if (attacker == null || targetSlot == null)
        {
            return false;
        }

        if (!IsBoardCombatUnit(attacker))
        {
            return false;
        }

        if (targetSlot.IsOccupied)
        {
            if (targetSlot.Occupant == null || targetSlot.Occupant.Owner == attacker.Owner)
            {
                return false;
            }

            if (targetSlot.Occupant.HasKeyword(CardKeyword.Smokescreen))
            {
                return false;
            }

            bool protectedByGuard = IsProtectedByAdjacentGuard(targetSlot, targetSlot.Occupant.Owner);
            return !protectedByGuard || targetSlot.Occupant.HasKeyword(CardKeyword.Guard);
        }

        return BoardTargetRules.IsHeadquartersSlot(targetSlot)
            && HeadquartersSideForSlot(targetSlot) != attacker.Owner;
    }

    private bool IsProtectedByAdjacentGuard(SlotInteract targetSlot, PlayerSide defender)
    {
        if (targetSlot == null || board == null || !targetSlot.IsOccupied || targetSlot.Occupant == null || targetSlot.Occupant.Owner != defender)
        {
            return false;
        }

        return IsGuardAt(targetSlot.X - 1, targetSlot.Z, defender)
            || IsGuardAt(targetSlot.X + 1, targetSlot.Z, defender)
            || IsGuardAt(targetSlot.X, targetSlot.Z - 1, defender)
            || IsGuardAt(targetSlot.X, targetSlot.Z + 1, defender);
    }

    private bool IsGuardAt(int x, int row, PlayerSide owner)
    {
        SlotInteract slot = board.GetSlotInRow(x, row);
        return slot != null
            && slot.IsOccupied
            && slot.Occupant != null
            && slot.Occupant.Owner == owner
            && slot.Occupant.HasKeyword(CardKeyword.Guard);
    }

    private void ResolveAttack(RuntimeCard attacker, SlotInteract targetSlot)
    {
        UnitAttackRules.MarkAttackResolved(attacker);
        RemoveSmokescreen(attacker);

        PlayerState defender = GetOpponentState(attacker.Owner);
        RuntimeCard attackedUnit = targetSlot.IsOccupied ? targetSlot.Occupant : null;
        CountermeasureResult countermeasureResult = TriggerCountermeasure(defender, attacker, attackedUnit);
        if (countermeasureResult.Triggered)
        {
            if (!attacker.IsAlive)
            {
                DestroyCard(attacker);
                UpdateFrontlineControl();
                status += " Attacker was destroyed before combat.";
                return;
            }

            if (countermeasureResult.CancelsAttack)
            {
                SpawnFloatingText("CANCEL", HeadquartersMarker(defender.Side), Color.magenta, FeedbackCueType.Countermeasure);
                status += " Attack was canceled.";
                return;
            }
        }

        if (targetSlot.IsOccupied)
        {
            if (cardSlots.TryGetValue(attacker, out SlotInteract attackerSlotForTrace))
            {
                PlayAttackLunge(attacker, targetSlot.transform.position);
                SpawnAttackTracer(attackerSlotForTrace.transform.position, targetSlot.transform.position, Color.red);
            }
            ResolveCombat(attacker, targetSlot.Occupant);
        }
        else
        {
            defender.HeadquartersHealth -= attacker.Attack;
            if (cardSlots.TryGetValue(attacker, out SlotInteract attackerSlotForHqTrace))
            {
                PlayAttackLunge(attacker, HeadquartersMarker(defender.Side));
                SpawnAttackTracer(attackerSlotForHqTrace.transform.position, HeadquartersMarker(defender.Side), Color.red);
            }
            SpawnFloatingText($"-{attacker.Attack} HQ", HeadquartersMarker(defender.Side), Color.red, FeedbackCueType.Attack);
            SetStatus(SceneGuidanceRules.AfterAttackPrompt(attacker, GetState(attacker.Owner).Kredits));
            ResolveAfterAttackRules(attacker);
            CheckGameOver();
        }

        UpdateFrontlineControl();
    }

    private void ResolveCombat(RuntimeCard attacker, RuntimeCard defender)
    {
        CombatResolution plan = CombatRules.Plan(attacker, defender);

        if (plan.AmbushFirstStrike)
        {
            attacker.CurrentDefense -= plan.DamageToAttacker;
            defender.AttacksThisTurn++;
            if (cardSlots.TryGetValue(attacker, out SlotInteract ambushTargetSlot))
            {
                SpawnFloatingText($"AMBUSH -{plan.DamageToAttacker}", ambushTargetSlot.transform.position, Color.magenta, FeedbackCueType.Countermeasure);
            }

            if (!attacker.IsAlive)
            {
                DestroyCard(attacker);
                SetStatus($"{defender.CardName} ambushed and destroyed {attacker.CardName}.");
                CheckGameOver();
                return;
            }

            defender.CurrentDefense -= plan.DamageToDefender;
        }
        else
        {
            defender.CurrentDefense -= plan.DamageToDefender;
            attacker.CurrentDefense -= plan.DamageToAttacker;
        }

        if (cardSlots.TryGetValue(defender, out SlotInteract defenderSlot))
        {
            SpawnFloatingText($"-{plan.DamageToDefender}", defenderSlot.transform.position, Color.red, FeedbackCueType.Attack);
        }
        if (cardSlots.TryGetValue(attacker, out SlotInteract combatAttackerSlot))
        {
            SpawnFloatingText($"-{plan.DamageToAttacker}", combatAttackerSlot.transform.position, Color.red);
        }
        SetStatus(SceneGuidanceRules.AfterAttackPrompt(attacker, GetState(attacker.Owner).Kredits));
        ResolveAfterAttackRules(attacker);

        if (!defender.IsAlive)
        {
            DestroyCard(defender);
        }

        if (!attacker.IsAlive)
        {
            DestroyCard(attacker);
        }

        CheckGameOver();
    }

    private void ResolveAfterAttackRules(RuntimeCard attacker)
    {
        if (attacker == null || attacker.SpecialRules == null || attacker.SpecialRules.Length == 0 || !attacker.IsAlive)
        {
            return;
        }

        CardRuleExecutionContext context = CreateRuleContext(GetState(attacker.Owner), attacker, cardSlots.TryGetValue(attacker, out SlotInteract attackerSlot) ? attackerSlot : null);
        for (int i = 0; i < attacker.SpecialRules.Length; i++)
        {
            attacker.SpecialRules[i]?.TryResolveAfterAttack(context);
        }
    }

    private void DamageUnit(RuntimeCard target, int amount, string sourceName)
    {
        if (target == null)
        {
            return;
        }

        int damage = ModifiedDamage(amount, target);
        target.CurrentDefense -= damage;
        if (cardSlots.TryGetValue(target, out SlotInteract targetSlot))
        {
            SpawnFloatingText($"-{damage}", targetSlot.transform.position, Color.red);
        }
        SetStatus($"{sourceName} dealt {damage} damage to {target.CardName}.");
        if (!target.IsAlive)
        {
            QueueDestroyedUnitRemoval(target);
        }

        CheckGameOver();
    }

    private void QueueDestroyedUnitRemoval(RuntimeCard target)
    {
        if (target == null || string.IsNullOrEmpty(target.Id) || pendingDestroyedUnitIds.Contains(target.Id))
        {
            return;
        }

        pendingDestroyedUnitIds.Add(target.Id);
        StartCoroutine(RemoveDestroyedUnitAfterDelay(target));
    }

    private System.Collections.IEnumerator RemoveDestroyedUnitAfterDelay(RuntimeCard target)
    {
        yield return new WaitForSeconds(0.28f);
        if (target != null && !target.IsAlive && cardSlots.ContainsKey(target))
        {
            DestroyCard(target);
            UpdateFrontlineControl();
            CheckGameOver();
        }

        if (target != null && !string.IsNullOrEmpty(target.Id))
        {
            pendingDestroyedUnitIds.Remove(target.Id);
        }
    }

    private int ModifiedDamage(int amount, RuntimeCard target)
    {
        return CombatRules.ModifiedDamage(amount, target);
    }

    private void PlaceCardInSlot(RuntimeCard card, SlotInteract slot, CardZone zone)
    {
        slot.SetOccupant(card);
        card.Zone = zone;
        if (zone == CardZone.Frontline)
        {
            RemoveSmokescreen(card);
        }

        cardSlots[card] = slot;
    }

    private void RemoveSmokescreen(RuntimeCard card)
    {
        if (card != null && card.HasKeyword(CardKeyword.Smokescreen))
        {
            card.RemoveKeyword(CardKeyword.Smokescreen);
        }
    }

    private void MoveCardToSlot(RuntimeCard card, SlotInteract destination, CardZone zone)
    {
        if (cardSlots.TryGetValue(card, out SlotInteract source))
        {
            source.ClearOccupant(card);
        }

        PlaceCardInSlot(card, destination, zone);
    }

    private void DestroyCard(RuntimeCard card)
    {
        Vector3 discardFlightStart = Vector3.zero;
        bool hasDiscardFlightStart = false;
        if (cardSlots.TryGetValue(card, out SlotInteract slot))
        {
            discardFlightStart = slot.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight;
            hasDiscardFlightStart = true;
            slot.ClearOccupant(card);
            cardSlots.Remove(card);
        }

        if (hasDiscardFlightStart)
        {
            PlayDiscardFlight(card, discardFlightStart);
        }

        GetState(card.Owner).Discard.Add(card);
        card.Zone = CardZone.Discard;
        if (!string.IsNullOrEmpty(card.Id))
        {
            pendingDestroyedUnitIds.Remove(card.Id);
        }
    }

    private void PlayDiscardFlight(RuntimeCard card, Vector3 startPosition)
    {
        if (card == null)
        {
            return;
        }

        CardView flightView = CreateTransientCardView(card);
        if (flightView == null)
        {
            return;
        }

        flightView.SetInteractionEnabled(false);
        flightView.SetDragEnabled(false);
        flightView.SetLayout(
            startPosition,
            new Vector3(PlayableSceneRules.BoardCardScale, 1f, PlayableSceneRules.BoardCardScale),
            Quaternion.identity,
            false);
        flightView.PlayMulliganDiscardFlight(startPosition, DiscardWorldPosition(card.Owner));
        StartCoroutine(DestroyTransientViewAfterDelay(flightView, CardMotionRules.MulliganDiscardFlightSeconds + 0.08f));
    }
}
