using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    private void ResolveFieldIntelDraws(PlayerState state)
    {
        if (state == null)
        {
            return;
        }

        while (state.ConsumeFieldIntelDraw())
        {
            DrawCard(state);
        }
    }

    private void FlashSignalLostAffectedCards(PlayerState state)
    {
        if (state == null)
        {
            return;
        }

        foreach (CardView view in cardViews)
        {
            RuntimeCard card = view != null ? view.Card : null;
            if (card == null || card.Owner != state.Side || card.Type != CardType.Unit)
            {
                continue;
            }

            if (card.Zone == CardZone.Hand || card.Zone == CardZone.PlayerSupport || card.Zone == CardZone.EnemySupport || card.Zone == CardZone.Frontline)
            {
                view.FlashCostChange();
            }
        }
    }

    private void ApplySignalLostToAffectedCards(PlayerState state, int amount)
    {
        if (state == null || amount <= 0)
        {
            return;
        }

        foreach (RuntimeCard card in state.Hand)
        {
            if (card != null && card.Type == CardType.Unit)
            {
                card.DeploymentCostBonus += amount;
            }
        }

        foreach (RuntimeCard card in cardSlots.Keys)
        {
            if (card == null || card.Owner != state.Side || card.Type != CardType.Unit)
            {
                continue;
            }

            if (card.Zone == CardZone.PlayerSupport || card.Zone == CardZone.EnemySupport || card.Zone == CardZone.Frontline)
            {
                card.OperationCostBonus += amount;
            }
        }
    }

    private void TryPlayAirborneDeployment(SlotInteract slot)
    {
        if (selectedCard == null || selectedCard.Type != CardType.Order || selectedCard.EffectType != CardEffectType.DeployWithBlitz)
        {
            SetStatus("AIRBORNE: SELECT AIRBORNE, THEN AN EMPTY SLOT.");
            return;
        }

        if (!player.CanSpendKredits(selectedCard.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(selectedCard, "play airborne", player.Kredits));
            return;
        }

        if (slot == null || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot) || !IsEmptyZoneForAirborneDeployment(slot, PlayerSide.Player))
        {
            RejectSelectedHandCard("AIRBORNE: SELECT AN EMPTY SUPPORT OR FRONTLINE SLOT.");
            return;
        }

        pendingAirborneSlot = slot;
        pendingAirborneOrder = selectedCard;
        pendingAirborneUnit = null;
        ClearCardInspectState();
        centerInspectCard = pendingAirborneOrder;
        inspectedCard = pendingAirborneOrder;
        hoveredHandCardId = null;
        CancelAllCardPointerInteractions();
        SetStatus("AIRBORNE: SELECT A UNIT CARD FROM HAND.");
        RefreshAllViews();
    }

    private void CompleteAirborneDeployment()
    {
        SlotInteract slot = pendingAirborneSlot;
        RuntimeCard airborneOrder = pendingAirborneOrder;
        RuntimeCard unit = pendingAirborneUnit;
        if (airborneOrder == null || airborneOrder.Type != CardType.Order || airborneOrder.EffectType != CardEffectType.DeployWithBlitz || unit == null)
        {
            SetStatus("AIRBORNE: SELECT AIRBORNE, AN EMPTY SLOT, THEN A UNIT CARD.");
            return;
        }

        if (!player.CanSpendKredits(airborneOrder.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(airborneOrder, "play airborne", player.Kredits));
            return;
        }

        if (slot == null || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot) || !IsEmptyZoneForAirborneDeployment(slot, PlayerSide.Player))
        {
            RejectSelectedHandCard("AIRBORNE: SELECTED SLOT IS NO LONGER EMPTY.");
            return;
        }

        if (!player.Hand.Contains(unit))
        {
            SetStatus("AIRBORNE: SELECTED UNIT NO LONGER IN HAND.");
            pendingAirborneOrder = null;
            pendingAirborneUnit = null;
            pendingAirborneSlot = null;
            ClearSelection();
            return;
        }

        if (!player.TrySpendKredits(airborneOrder.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(airborneOrder, "play airborne", player.Kredits));
            return;
        }

        RemoveFromHand(player, airborneOrder);
        airborneOrder.Zone = CardZone.Discard;
        player.Discard.Add(airborneOrder);

        CardView unitView = FindView(unit);
        Vector3 deployFrom = unitView != null
            ? unitView.transform.position
            : HandPosition(PlayerSide.Player, player.Hand.IndexOf(unit), player.Hand.Count);
        DeployAirborneUnit(player, unit, slot, Color.cyan);
        player.RegisterCardPlayed();

        pendingAirborneOrder = null;
        pendingAirborneUnit = null;
        pendingAirborneSlot = null;
        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterDeployPrompt(unit));
        HighlightAirborneDeploymentTargets(null, false);
        centerInspectCard = null;
        inspectedCard = null;
        DestroyCenterInspectView();
        ClearSelection();
        RefreshAllViews();
        StartCoroutine(ShowPlayedOrder(airborneOrder));
        CardView deployedView = FindView(unit);
        if (deployedView != null)
        {
            deployedView.PlayDeployDrop(deployFrom, slot.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight);
            deployedView.RefreshKeywordIcons(true);
        }

        pendingDeployDropCardId = null;
    }

    private bool IsEmptyZoneForAirborneDeployment(SlotInteract slot, PlayerSide owner)
    {
        if (slot == null || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot))
        {
            return false;
        }

        return slot.Zone == SlotZone.Frontline || slot.Zone == SupportZoneFor(owner);
    }

    private void DeployAirborneUnit(PlayerState owner, RuntimeCard unit, SlotInteract slot, Color feedbackColor)
    {
        if (owner == null || unit == null || slot == null)
        {
            return;
        }

        RemoveFromHand(owner, unit);
        unit.AddKeyword(CardKeyword.Blitz);
        pendingDeployDropCardId = unit.Id;
        CardZone targetZone = slot.Zone == SlotZone.Frontline ? CardZone.Frontline : SupportCardZoneFor(owner.Side);
        PlaceCardInSlot(unit, slot, targetZone);
        SpawnFloatingText("AIRBORNE", slot.transform.position, feedbackColor);
        ResolveDeploymentEffect(owner, unit, slot);

        if (DeployStrikeRules.ShouldTriggerStrike(unit))
        {
            board.TriggerStrike(slot.X, slot.Zone);
            StartCoroutine(ReanchorBoardCardsAfterStrike());
        }
    }

    private bool RemoveFromHand(PlayerState state, RuntimeCard card)
    {
        if (state == null || card == null)
        {
            return false;
        }

        if (state.Hand.Remove(card))
        {
            return true;
        }

        int index = state.Hand.FindIndex(item => item != null && item.Id == card.Id);
        if (index < 0)
        {
            return false;
        }

        state.Hand.RemoveAt(index);
        return true;
    }

    private void HighlightAirborneDeploymentTargets(RuntimeCard unit, bool highlighted)
    {
        if (unit != null && highlighted)
        {
            HighlightEmptySlots(SlotZone.PlayerSupport, true, SlotHighlightLabelRules.LabelFor(unit, SlotZone.PlayerSupport));
            HighlightEmptySlots(SlotZone.Frontline, true, SlotHighlightLabelRules.LabelFor(unit, SlotZone.Frontline));
            return;
        }

        HighlightEmptySlots(SlotZone.PlayerSupport, false);
        HighlightEmptySlots(SlotZone.Frontline, false);
    }

    private void TrySetCountermeasure(PlayerState state, RuntimeCard card)
    {
        if (!state.CanSpendKredits(card.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(card, "set counter", state.Kredits));
            return;
        }

        if (state.Countermeasures.Count >= 3)
        {
            RejectSelectedHandCard(SceneGuidanceRules.CountermeasureRowFullPrompt(card));
            return;
        }

        if (!state.TrySpendKredits(card.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(card, "set counter", state.Kredits));
            return;
        }

        state.Hand.Remove(card);
        card.Zone = CardZone.Countermeasure;
        state.Countermeasures.Add(card);
        if (card.EffectType == CardEffectType.FieldIntel)
        {
            state.MarkFieldIntelPending();
        }

        SpawnFloatingText("COUNTER", CountermeasureFeedbackPosition(state), Color.magenta);
        SetStatus(state.Side == PlayerSide.Player ? SceneGuidanceRules.AfterCountermeasurePrompt(card) : "Enemy set a countermeasure.");
        ClearSelection();
        RefreshAllViews();
    }

    private bool TryPlayOrderOnSlot(SlotInteract slot)
    {
        if (selectedCard == null || selectedCard.Type != CardType.Order)
        {
            return false;
        }

        if (!player.CanSpendKredits(selectedCard.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(selectedCard, "play order", player.Kredits));
            return false;
        }

        if (!IsLegalOrderTarget(selectedCard, slot, PlayerSide.Player))
        {
            RejectSelectedHandCard(SceneGuidanceRules.IllegalOrderTargetPrompt(selectedCard, slot != null ? slot.Occupant : null, PlayerSide.Player));
            return false;
        }

        PlayOrder(player, selectedCard, slot);
        ClearSelection();
        RefreshAllViews();
        return true;
    }

    private void PlayOrder(PlayerState caster, RuntimeCard order, SlotInteract targetSlot)
    {
        caster.RegisterCardPlayed();
        if (!caster.TrySpendKredits(order.KreditCost))
        {
            SetStatus(SceneGuidanceRules.CannotAffordCardPrompt(order, "play order", caster.Kredits));
            return;
        }

        caster.Hand.Remove(order);
        order.Zone = CardZone.Discard;
        caster.Discard.Add(order);

        if (TryResolveAttachedOrderRule(caster, order, targetSlot))
        {
            CheckGameOver();
            StartCoroutine(ShowPlayedOrder(order));
            return;
        }

        switch (order.EffectType)
        {
            case CardEffectType.DamageEnemyHeadquarters:
                GetOpponentState(caster.Side).HeadquartersHealth -= order.EffectAmount;
                SpawnFloatingText($"-{order.EffectAmount} HQ", HeadquartersMarker(caster.Side == PlayerSide.Player ? PlayerSide.Enemy : PlayerSide.Player), Color.red, FeedbackCueType.Attack);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.DamageTargetUnit:
                DamageUnit(targetSlot.Occupant, order.EffectAmount, order.CardName);
                break;
            case CardEffectType.DamageTargetUnitAndAdjacent:
                if (targetSlot != null)
                {
                    ResolveAreaDamageOrder(order, targetSlot);
                }
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.RepairHeadquarters:
                caster.HeadquartersHealth = Mathf.Min(20, caster.HeadquartersHealth + order.EffectAmount);
                SpawnFloatingText($"+{order.EffectAmount} HQ", HeadquartersMarker(caster.Side), Color.green);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.DrawCards:
                for (int i = 0; i < order.EffectAmount; i++)
                {
                    DrawCard(caster);
                }
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.IncreaseEnemyCosts:
                PlayerState affectedState = GetOpponentState(caster.Side);
                ApplySignalLostToAffectedCards(affectedState, order.EffectAmount);
                FlashSignalLostAffectedCards(affectedState);
                SpawnFloatingText("COST +1", HeadquartersMarker(affectedState.Side), Color.red, FeedbackCueType.Countermeasure);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.FieldIntel:
                break;
            case CardEffectType.DeployWithBlitz:
                RuntimeCard airborneUnit = BestAirborneUnit(caster);
                if (airborneUnit != null && targetSlot != null && IsEmptyZoneForAirborneDeployment(targetSlot, caster.Side))
                {
                    DeployAirborneUnit(caster, airborneUnit, targetSlot, caster.Side == PlayerSide.Player ? Color.cyan : Color.red);
                    SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                }
                break;
            case CardEffectType.BuffFriendlyUnit:
                targetSlot.Occupant.Attack += order.EffectAmount;
                targetSlot.Occupant.CurrentDefense += order.EffectAmount;
                targetSlot.Occupant.Defense += order.EffectAmount;
                SpawnFloatingText($"+{order.EffectAmount}/+{order.EffectAmount}", targetSlot.transform.position, Color.green, FeedbackCueType.Buff);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.PinTargetUnit:
                targetSlot.Occupant.AddKeyword(CardKeyword.Pinned);
                targetSlot.Occupant.HasActed = true;
                SpawnFloatingText("PINNED", targetSlot.transform.position, Color.yellow);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
        }

        CheckGameOver();
        StartCoroutine(ShowPlayedOrder(order));
    }

    private bool TryResolveAttachedOrderRule(PlayerState caster, RuntimeCard order, SlotInteract targetSlot)
    {
        if (order == null || order.SpecialRules == null || order.SpecialRules.Length == 0)
        {
            return false;
        }

        CardRuleExecutionContext context = CreateRuleContext(caster, order, targetSlot);

        for (int i = 0; i < order.SpecialRules.Length; i++)
        {
            CardRule rule = order.SpecialRules[i];
            if (rule != null && rule.TryResolveOrder(context))
            {
                return true;
            }
        }

        return false;
    }

    private CardRuleExecutionContext CreateRuleContext(PlayerState caster, RuntimeCard card, SlotInteract targetSlot)
    {
        return new CardRuleExecutionContext
        {
            Caster = caster,
            Card = card,
            TargetSlot = targetSlot,
            Board = board,
            GetState = GetState,
            GetOpponentState = GetOpponentState,
            FindCardTemplate = FindCardTemplateByName,
            ChooseAirborneUnit = BestAirborneUnit,
            IsEmptyAirborneSlot = IsEmptyZoneForAirborneDeployment,
            DeployAirborneUnit = DeployAirborneUnit,
            DamageUnit = DamageUnit,
            DrawCard = DrawCard,
            AddCardToHand = AddCardToHand,
            GainFriendlyDefense = ApplyDefenseGainToFriendlyUnits,
            ApplyCostIncrease = ApplySignalLostToAffectedCards,
            FlashCostChange = FlashSignalLostAffectedCards,
            SetStatus = SetStatus,
            SpawnFloatingText = SpawnFloatingText,
            HeadquartersMarker = HeadquartersMarker,
            AdjacentAreaDamage = (sourceCard, slot) => AdjacentAreaDamage(sourceCard)
        };
    }

    private void ResolveAreaDamageOrder(RuntimeCard order, SlotInteract targetSlot)
    {
        if (order == null || targetSlot == null)
        {
            return;
        }

        if (BoardTargetRules.IsHeadquartersSlot(targetSlot) && !targetSlot.IsOccupied)
        {
            PlayerSide targetSide = targetSlot.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
            PlayerState targetState = GetState(targetSide);
            targetState.HeadquartersHealth -= order.EffectAmount;
            SpawnFloatingText($"-{order.EffectAmount} HQ", HeadquartersMarker(targetSide), Color.red, FeedbackCueType.Attack);
        }
        else if (targetSlot.IsOccupied)
        {
            DamageUnit(targetSlot.Occupant, order.EffectAmount, order.CardName);
        }

        if (board == null)
        {
            return;
        }

        ResolveAdjacentOrderDamage(order, board.GetSlot(targetSlot.X - 1, targetSlot.Zone));
        ResolveAdjacentOrderDamage(order, board.GetSlot(targetSlot.X + 1, targetSlot.Zone));
    }

    private void ResolveAdjacentOrderDamage(RuntimeCard order, SlotInteract slot)
    {
        if (order == null || slot == null || !slot.IsOccupied || slot.Occupant == null)
        {
            return;
        }

        DamageUnit(slot.Occupant, AdjacentAreaDamage(order), order.CardName);
    }

    private int AdjacentAreaDamage(RuntimeCard order)
    {
        return order != null && order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent
            ? Mathf.Max(0, order.EffectAmount - 2)
            : 0;
    }

    private System.Collections.IEnumerator ShowPlayedOrder(RuntimeCard order)
    {
        yield return ShowPlayedOrder(order, null);
    }

    private System.Collections.IEnumerator ShowPlayedOrder(RuntimeCard order, CardView existingView)
    {
        CardView displayView = existingView != null ? existingView : CreateTransientCardView(order);
        if (displayView == null)
        {
            yield break;
        }

        displayView.SetInteractionEnabled(false);
        displayView.SetDragEnabled(false);
        if (existingView == null)
        {
            displayView.SetLayout(
                PlayableSceneRules.OrderDisplayAnchor,
                new Vector3(PlayableSceneRules.OrderDisplayScale, 1f, PlayableSceneRules.OrderDisplayScale),
                Quaternion.identity,
                false);
        }
        displayView.SetDetailPresentation();
        yield return new WaitForSeconds(PlayableSceneRules.OrderDisplaySeconds);
        if (displayView == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Vector3 start = displayView.transform.position;
        Vector3 end = PlayableSceneRules.OrderDisplayExitAnchor;
        Vector3 startScale = displayView.transform.localScale;
        Vector3 endScale = new Vector3(PlayableSceneRules.BoardCardScale, 1f, PlayableSceneRules.BoardCardScale);
        while (elapsed < PlayableSceneRules.OrderFlyOffSeconds)
        {
            if (displayView == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PlayableSceneRules.OrderFlyOffSeconds);
            float easedT = t * t * (3f - 2f * t);
            Vector3 position = Vector3.Lerp(start, end, easedT);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.24f;
            displayView.transform.position = position;
            displayView.transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
            yield return null;
        }

        if (displayView != null)
        {
            RuntimeSafeDestroy.Destroy(displayView.gameObject);
        }
    }

    private CountermeasureResult TriggerCountermeasure(PlayerState defender, RuntimeCard attacker, RuntimeCard attackedUnit)
    {
        if (defender.Countermeasures.Count == 0)
        {
            return new CountermeasureResult();
        }

        RuntimeCard countermeasure = defender.Countermeasures[0];
        CountermeasureResult result = CountermeasureRules.Predict(countermeasure, attacker, attackedUnit);
        if (!result.Triggered)
        {
            return result;
        }

        defender.Countermeasures.RemoveAt(0);
        defender.Discard.Add(countermeasure);
        countermeasure.Zone = CardZone.Discard;
        StartCoroutine(ShowTriggeredCountermeasure(countermeasure));

        result = CountermeasureRules.Resolve(countermeasure, attacker, attackedUnit);
        if (result.DamageToAttacker > 0 && cardSlots.TryGetValue(attacker, out SlotInteract attackerSlot))
        {
            SpawnFloatingText($"-{result.DamageToAttacker}", attackerSlot.transform.position, Color.magenta, FeedbackCueType.Countermeasure);
        }

        if (attackedUnit != null && cardSlots.TryGetValue(attackedUnit, out SlotInteract attackedSlot))
        {
            if (result.BonusAttackToTarget > 0)
            {
                SpawnFloatingText($"+{result.BonusAttackToTarget} ATK", attackedSlot.transform.position, Color.magenta, FeedbackCueType.Countermeasure);
            }

            if (result.BonusDefenseToTarget > 0)
            {
                SpawnFloatingText($"+{result.BonusDefenseToTarget} DEF", attackedSlot.transform.position + Vector3.right * 0.25f, Color.magenta, FeedbackCueType.Countermeasure);
            }

            if (result.TargetGainsAmbush)
            {
                SpawnFloatingText("AMBUSH", attackedSlot.transform.position + Vector3.forward * 0.25f, Color.magenta, FeedbackCueType.Countermeasure);
            }
        }

        string message = result.DamageToAttacker > 0
            ? $"{defender.Side} countermeasure {countermeasure.CardName} hit {attacker.CardName} for {result.DamageToAttacker}."
            : $"{defender.Side} countermeasure {countermeasure.CardName} stopped {attacker.CardName}.";
        SetStatus(message);
        return result;
    }

        private System.Collections.IEnumerator ShowTriggeredCountermeasure(RuntimeCard countermeasure)
        {
            CardView displayView = CreateTransientCardView(countermeasure);
            if (displayView == null)
            {
                yield break;
            }
            Vector3 start = CountermeasurePosition(countermeasure.Owner, 0, 1, countermeasure.Owner == PlayerSide.Player ? -4.15f : 4.15f);
            Vector3 hold = new Vector3(-4.85f, PlayableSceneRules.OrderDisplayAnchor.y, countermeasure.Owner == PlayerSide.Player ? -1.35f : 1.35f);
            displayView.SetLayout(start, new Vector3(PlayableSceneRules.OrderDisplayScale * 0.78f, 1f, PlayableSceneRules.OrderDisplayScale * 0.78f), Quaternion.identity, false);
        yield return new WaitForSeconds(0.02f);
        displayView.SetLayout(hold, new Vector3(PlayableSceneRules.OrderDisplayScale * 0.78f, 1f, PlayableSceneRules.OrderDisplayScale * 0.78f), Quaternion.identity, true);
        yield return new WaitForSeconds(0.72f);
        if (displayView != null)
        {
            transientCardViews.Remove(displayView);
            RuntimeSafeDestroy.Destroy(displayView.gameObject);
        }
    }

    private Vector3 CountermeasureFeedbackPosition(PlayerState state)
    {
        int count = state != null ? state.Countermeasures.Count : 1;
        PlayerSide side = state != null ? state.Side : PlayerSide.Player;
        return CountermeasurePosition(side, CardLayoutRules.NewlyAddedIndex(count), Mathf.Max(1, count), 0f);
    }
}
