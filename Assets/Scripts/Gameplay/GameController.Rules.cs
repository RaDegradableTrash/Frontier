using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    public int AvailableKreditsFor(PlayerSide side)
    {
        return GetState(side).Kredits;
    }

    public int DisplayKreditCostFor(RuntimeCard card)
    {
        if (card == null)
        {
            return 0;
        }

        return card.Type == CardType.Unit && card.Zone == CardZone.Hand
            ? GetState(card.Owner).EffectiveDeploymentCost(card)
            : card.KreditCost;
    }

    public int DisplayOperationCostFor(RuntimeCard card)
    {
        return card == null || card.Type != CardType.Unit ? 0 : EffectiveOperationCost(card);
    }

    private int HandPlayCost(PlayerState state, RuntimeCard card)
    {
        if (state == null || card == null)
        {
            return int.MaxValue;
        }

        return card.Type == CardType.Unit
            ? state.EffectiveDeploymentCost(card)
            : card.KreditCost;
    }

    private bool IsAirborneUnitSelectionActive()
    {
        return pendingAirborneSlot != null
            && pendingAirborneOrder != null
            && pendingAirborneOrder.Owner == PlayerSide.Player
            && pendingAirborneOrder.Zone == CardZone.Hand
            && pendingAirborneOrder.Type == CardType.Order
            && pendingAirborneOrder.EffectType == CardEffectType.DeployWithBlitz;
    }

    private RuntimeCard FindCardTemplateByName(string cardName)
    {
        RuntimeCard[] templates = StarterTemplates(selectedPlayerDeck);
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i].CardName == cardName)
            {
                return templates[i];
            }
        }

        templates = StarterTemplates(selectedEnemyDeck);
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i].CardName == cardName)
            {
                return templates[i];
            }
        }

        return null;
    }

    private int EffectiveOperationCost(RuntimeCard card)
    {
        if (card == null)
        {
            return int.MaxValue;
        }

        return GetState(card.Owner).EffectiveOperationCost(card);
    }

    private int EffectiveOperationCostForAction(RuntimeCard card)
    {
        return card == null ? int.MaxValue : EffectiveOperationCost(card);
    }

    private bool CanSpendUnitOperation(RuntimeCard card, int availableKredits, out int effectiveCost)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            effectiveCost = int.MaxValue;
            return false;
        }

        effectiveCost = EffectiveOperationCostForAction(card);
        return KreditRules.CanSpend(availableKredits, effectiveCost);
    }

    private PlayerState GetState(PlayerSide side)
    {
        return side == PlayerSide.Player ? player : enemy;
    }

    private PlayerState GetOpponentState(PlayerSide side)
    {
        return side == PlayerSide.Player ? enemy : player;
    }

    private SlotZone SupportZoneFor(PlayerSide side)
    {
        return side == PlayerSide.Player ? SlotZone.PlayerSupport : SlotZone.EnemySupport;
    }

    private CardZone SupportCardZoneFor(PlayerSide side)
    {
        return side == PlayerSide.Player ? CardZone.PlayerSupport : CardZone.EnemySupport;
    }

    private int BoardColumnCount()
    {
        return board != null ? board.BoardColumnsForAllRows : 7;
    }

    private int BoardRowCount()
    {
        return board != null ? board.BoardRows : 5;
    }

    private bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < BoardRowCount() && col >= 0 && col < BoardColumnCount();
    }

    private SlotInteract TryGetSlot(int col, int row)
    {
        return board != null ? board.GetSlotInRow(col, row) : null;
    }

    private IEnumerable<SlotInteract> AllBoardSlots()
    {
        for (int row = 0; row < BoardRowCount(); row++)
        {
            for (int col = 0; col < BoardColumnCount(); col++)
            {
                SlotInteract slot = TryGetSlot(col, row);
                if (slot != null)
                {
                    yield return slot;
                }
            }
        }
    }

    private bool IsBoardCombatUnit(RuntimeCard card)
    {
        return card != null
            && (card.Zone == CardZone.PlayerSupport || card.Zone == CardZone.EnemySupport || card.Zone == CardZone.Frontline);
    }

    private int UnitScore(RuntimeCard card)
    {
        if (card == null)
        {
            return -999;
        }

        int score = card.Attack * 3 + card.CurrentDefense * 2 - card.KreditCost;
        if (card.HasKeyword(CardKeyword.Guard)) score += 4;
        if (card.HasKeyword(CardKeyword.Fury)) score += 4;
        if (card.HasKeyword(CardKeyword.HeavyArmor)) score += 3;
        if (card.HasKeyword(CardKeyword.Ambush)) score += 3;
        if (card.HasKeyword(CardKeyword.Blitz)) score += 2;
        if (card.HasKeyword(CardKeyword.Mobilize)) score += 2;
        if (card.HasKeyword(CardKeyword.Smokescreen)) score += 2;
        return score;
    }

    private int TargetPriority(RuntimeCard target)
    {
        if (target == null)
        {
            return -999;
        }

        int score = UnitScore(target);
        if (target.HasKeyword(CardKeyword.Guard)) score += 8;
        if (target.HasKeyword(CardKeyword.Fury)) score += 6;
        if (target.Zone == CardZone.Frontline) score += 4;
        if (target.CurrentDefense <= 2) score += 3;
        return score;
    }

    private RuntimeCard BestPlayableEnemyUnit()
    {
        RuntimeCard best = null;
        int bestScore = int.MinValue;
        foreach (RuntimeCard card in enemy.Hand)
        {
            if (card.Type != CardType.Unit || !enemy.CanSpendKredits(HandPlayCost(enemy, card)) || FindEnemyDeploymentSlot(card) == null)
            {
                continue;
            }

            int score = UnitScore(card);
            if (card.HasKeyword(CardKeyword.Mobilize))
            {
                score += 5;
            }

            if (score > bestScore)
            {
                best = card;
                bestScore = score;
            }
        }

        return best;
    }

    private RuntimeCard BestEnemyAttacker()
    {
        RuntimeCard best = null;
        int bestScore = int.MinValue;
        foreach (RuntimeCard card in cardSlots.Keys)
        {
            if (card.Owner != PlayerSide.Enemy || !IsBoardCombatUnit(card) || !CanAttack(card, enemy.Kredits))
            {
                continue;
            }

            SlotInteract target = FindBestAttackTarget(card);
            if (!IsLegalAttackTarget(card, target))
            {
                continue;
            }

            int score = card.Attack * 5 - EffectiveOperationCostForAction(card) + (target != null && target.IsOccupied ? TargetPriority(target.Occupant) : 8);
            if (score > bestScore)
            {
                best = card;
                bestScore = score;
            }
        }

        return best;
    }

    private SlotInteract FindBestAttackTarget(RuntimeCard attacker)
    {
        if (attacker == null)
        {
            return null;
        }

        PlayerSide defender = attacker.Owner == PlayerSide.Player ? PlayerSide.Enemy : PlayerSide.Player;

        SlotInteract bestUnit = null;
        int bestScore = int.MinValue;
        SlotInteract frontline = FindHighestPriorityTargetInZone(SlotZone.Frontline, defender);
        if (frontline != null && IsLegalAttackTarget(attacker, frontline))
        {
            bestUnit = frontline;
            bestScore = TargetPriority(frontline.Occupant) + 6;
        }

        for (int x = 0; x < BoardColumnCount(); x++)
        {
            for (int z = 0; z < BoardRowCount(); z++)
            {
                SlotInteract slot = TryGetSlot(x, z);
                if (slot == null || !slot.IsOccupied || slot.Occupant.Owner != defender)
                {
                    continue;
                }

                if (IsProtectedByAdjacentGuard(slot, defender) && !slot.Occupant.HasKeyword(CardKeyword.Guard))
                {
                    continue;
                }

                if (slot.Occupant.HasKeyword(CardKeyword.Smokescreen))
                {
                    continue;
                }

                int score = TargetPriority(slot.Occupant);
                if (slot.Occupant.CurrentDefense <= attacker.Attack)
                {
                    score += 8;
                }

                if (score > bestScore)
                {
                    bestUnit = slot;
                    bestScore = score;
                }
            }
        }

        SlotInteract headquartersTarget = board.GetHeadquartersSlot(defender);
        return bestUnit ?? headquartersTarget;
    }

    private SlotInteract FindHighestPriorityTarget(PlayerSide owner)
    {
        SlotInteract best = null;
        int bestScore = int.MinValue;
        SlotInteract support = FindHighestPriorityTargetInZone(SupportZoneFor(owner), owner);
        if (support != null)
        {
            best = support;
            bestScore = TargetPriority(support.Occupant);
        }

        SlotInteract frontline = FindHighestPriorityTargetInZone(SlotZone.Frontline, owner);
        if (frontline != null)
        {
            int frontlineScore = TargetPriority(frontline.Occupant);
            if (frontlineScore > bestScore)
            {
                best = frontline;
            }
        }

        return best;
    }

    private SlotInteract FindHighestPriorityTargetInZone(SlotZone zone, PlayerSide owner)
    {
        SlotInteract best = null;
        int bestScore = int.MinValue;
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot == null || !slot.IsOccupied || slot.Occupant.Owner != owner)
            {
                continue;
            }

            if (slot.Occupant.HasKeyword(CardKeyword.Smokescreen))
            {
                continue;
            }

            int score = TargetPriority(slot.Occupant);
            if (score > bestScore)
            {
                best = slot;
                bestScore = score;
            }
        }

        return best;
    }

    private SlotInteract FindHighestValueFriendlyUnit(PlayerSide owner)
    {
        SlotInteract best = null;
        int bestScore = int.MinValue;
        SlotInteract support = FindHighestValueFriendlyUnitInZone(SupportZoneFor(owner), owner);
        if (support != null)
        {
            best = support;
            bestScore = UnitScore(support.Occupant);
        }

        SlotInteract frontline = FindHighestValueFriendlyUnitInZone(SlotZone.Frontline, owner);
        if (frontline != null)
        {
            int frontlineScore = UnitScore(frontline.Occupant);
            if (frontlineScore > bestScore)
            {
                best = frontline;
            }
        }

        return best;
    }

    private SlotInteract FindHighestValueFriendlyUnitInZone(SlotZone zone, PlayerSide owner)
    {
        SlotInteract best = null;
        int bestScore = int.MinValue;
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot == null || !slot.IsOccupied || slot.Occupant.Owner != owner)
            {
                continue;
            }

            int score = UnitScore(slot.Occupant);
            if (score > bestScore)
            {
                best = slot;
                bestScore = score;
            }
        }

        return best;
    }

    private bool IsLegalOrderTarget(RuntimeCard order, SlotInteract slot, PlayerSide caster)
    {
        if (order == null || order.Type != CardType.Order)
        {
            return false;
        }

        switch (order.EffectType)
        {
            case CardEffectType.DamageEnemyHeadquarters:
            case CardEffectType.RepairHeadquarters:
            case CardEffectType.DrawCards:
            case CardEffectType.IncreaseEnemyCosts:
                return true;
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.PinTargetUnit:
                return slot != null && slot.IsOccupied && slot.Occupant.Owner != caster && !slot.Occupant.HasKeyword(CardKeyword.Smokescreen);
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return IsEnemyOrderTargetSlot(slot, caster);
            case CardEffectType.DeployWithBlitz:
                return slot != null && !slot.IsOccupied && !BoardTargetRules.IsHeadquartersSlot(slot) && IsEmptyZoneForAirborneDeployment(slot, caster);
            case CardEffectType.BuffFriendlyUnit:
                return slot != null && slot.IsOccupied && slot.Occupant.Owner == caster && !slot.Occupant.HasKeyword(CardKeyword.Smokescreen);
            default:
                return false;
        }
    }

    private bool OrderNeedsTarget(RuntimeCard order)
    {
        return order.EffectType == CardEffectType.DamageTargetUnit
            || order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent
            || order.EffectType == CardEffectType.DeployWithBlitz
            || order.EffectType == CardEffectType.PinTargetUnit
            || order.EffectType == CardEffectType.BuffFriendlyUnit;
    }

    private bool IsEnemyOrderTargetSlot(SlotInteract slot, PlayerSide caster)
    {
        if (slot == null)
        {
            return false;
        }

        if (BoardTargetRules.IsHeadquartersSlot(slot) && !slot.IsOccupied)
        {
            PlayerSide headquartersSide = slot.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
            return headquartersSide != caster;
        }

        return slot.IsOccupied
            && slot.Occupant != null
            && slot.Occupant.Owner != caster
            && !slot.Occupant.HasKeyword(CardKeyword.Smokescreen);
    }

    private SlotInteract FindEmptySlot(SlotZone zone)
    {
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot != null && !slot.IsOccupied && !BoardTargetRules.IsHeadquartersSlot(slot))
            {
                return slot;
            }
        }

        return null;
    }

    private SlotInteract FindOccupiedSlot(SlotZone zone, PlayerSide owner)
    {
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot != null && slot.IsOccupied && slot.Occupant.Owner == owner && !slot.Occupant.HasKeyword(CardKeyword.Smokescreen))
            {
                return slot;
            }
        }

        return null;
    }

    private SlotInteract FindGuardSlot(SlotZone zone, PlayerSide owner)
    {
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot != null && slot.IsOccupied && GuardProtectionRules.ProtectsSupportTargets(slot.Occupant, owner))
            {
                return slot;
            }
        }

        return null;
    }

    private bool HasGuardUnit(PlayerSide owner)
    {
        return FindGuardSlot(SupportZoneFor(owner), owner) != null;
    }

    private void UpdateFrontlineControl()
    {
        hasFrontlineController = false;
        frontlineController = PlayerSide.Player;
        foreach (SlotInteract slot in AllBoardSlots())
        {
            if (slot != null && slot.IsOccupied)
            {
                RemoveSmokescreen(slot.Occupant);
            }
        }
    }

    private string FrontlineLabel()
    {
        return hasFrontlineController ? frontlineController.ToString() : "Neutral";
    }

    private void CheckGameOver()
    {
        if (phase == GamePhase.GameOver)
        {
            return;
        }

        if (player.HeadquartersHealth <= 0 && enemy.HeadquartersHealth <= 0)
        {
            SetGamePhase(GamePhase.GameOver);
            SetStatus("Draw. Both headquarters are destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
        else if (enemy.HeadquartersHealth <= 0)
        {
            SetGamePhase(GamePhase.GameOver);
            SetStatus("Victory. Enemy headquarters destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
        else if (player.HeadquartersHealth <= 0)
        {
            SetGamePhase(GamePhase.GameOver);
            SetStatus("Defeat. Your headquarters was destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
    }

    private Vector3 HeadquartersMarker(PlayerSide side)
    {
        SlotInteract headquartersSlot = board != null ? board.GetHeadquartersSlot(side) : null;
        if (headquartersSlot != null)
        {
            return headquartersSlot.transform.position;
        }

        float z = side == PlayerSide.Player ? PlayableSceneRules.PlayerHeadquartersSlot.z : PlayableSceneRules.EnemyHeadquartersSlot.z;
        return new Vector3(PlayableSceneRules.PlayerHeadquartersSlot.x, 0f, z);
    }

    private delegate bool SlotPredicate(SlotInteract slot);

}
