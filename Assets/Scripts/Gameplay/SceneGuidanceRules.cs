public static class SceneGuidanceRules
{
    public static string ActionPrompt(GamePhase phase, PlayerSide activeSide)
    {
        switch (phase)
        {
            case GamePhase.DeckBuilder:
                return "CHOOSE DECK — CLICK FACTION OR CLICK START MATCH";
            case GamePhase.Mulligan:
                return OpeningHandPrompt(false);
            case GamePhase.PlayerTurn:
                return activeSide == PlayerSide.Player ? "YOUR TURN" : "ENEMY TURN";
            case GamePhase.EnemyTurn:
                return "ENEMY TURN — WAIT";
            case GamePhase.GameOver:
                return "GAME OVER — RESTART";
            default:
                return "FOLLOW HIGHLIGHTS — THEN END TURN";
        }
    }

    public static string HelpPrompt()
    {
        return "HELP — CLICK/DRAG CARDS. N AUTO-ACTION. P/A/F PLAY/ADVANCE/ATTACK. SPACE END TURN.";
    }

    public static string BlockedInteractionPrompt(GamePhase phase, PlayerSide activeSide)
    {
        switch (phase)
        {
            case GamePhase.DeckBuilder:
                return "MATCH NOT STARTED — CHOOSE A DECK, THEN CLICK START MATCH.";
            case GamePhase.Mulligan:
                return "OPENING HAND — INSPECT CARDS, THEN KEEP OR MULLIGAN.";
            case GamePhase.EnemyTurn:
                return "ENEMY TURN — WAIT FOR YOUR TURN BEFORE PLAYING CARDS.";
            case GamePhase.PlayerTurn:
                return activeSide == PlayerSide.Player
                    ? "YOUR TURN — SELECT A CARD OR SLOT."
                    : "ENEMY TURN — WAIT FOR YOUR TURN BEFORE PLAYING CARDS.";
            case GamePhase.GameOver:
                return "GAME OVER — CLICK RESTART TO PLAY AGAIN.";
            default:
                return "ACTION NOT AVAILABLE NOW — FOLLOW THE CURRENT PROMPT.";
        }
    }

    public static string ShortcutBlockedPrompt(string shortcut, GamePhase phase, PlayerSide activeSide)
    {
        return $"{shortcut} SHORTCUT NOT AVAILABLE — {BlockedInteractionPrompt(phase, activeSide)}";
    }

    public static string OpeningHandPrompt(bool mulliganUsed)
    {
        return mulliganUsed
            ? "OPENING HAND — INSPECT HAND, THEN KEEP HAND"
            : "OPENING HAND — INSPECT HAND, KEEP OR MULLIGAN";
    }

    public static string TablePrompt(GamePhase phase, PlayerSide activeSide)
    {
        switch (phase)
        {
            case GamePhase.DeckBuilder:
                return "CHOOSE DECK";
            case GamePhase.Mulligan:
                return "KEEP OR MULLIGAN";
            case GamePhase.PlayerTurn:
                return activeSide == PlayerSide.Player ? "YOUR TURN" : "ENEMY TURN";
            case GamePhase.EnemyTurn:
                return "ENEMY TURN";
            case GamePhase.GameOver:
                return "GAME OVER";
            default:
                return "FOLLOW HIGHLIGHTS";
        }
    }

    public static string SelectedActionPrompt(RuntimeCard selectedCard)
    {
        return SelectedActionPrompt(selectedCard, int.MaxValue);
    }

    public static string SelectedActionPrompt(RuntimeCard selectedCard, int availableKredits)
    {
        return SelectedActionPrompt(selectedCard, availableKredits, false, PlayerSide.Player);
    }

    public static string SelectedActionPrompt(RuntimeCard selectedCard, int availableKredits, bool hasFrontlineController, PlayerSide frontlineController)
    {
        if (selectedCard == null)
        {
            return string.Empty;
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.KreditCost > availableKredits)
        {
            return $"SELECTED {ShortName(selectedCard)} — NEED {selectedCard.KreditCost} KREDITS";
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit && selectedCard.HasKeyword(CardKeyword.Mobilize))
        {
            if (hasFrontlineController && frontlineController != selectedCard.Owner)
            {
                return $"SELECTED {ShortName(selectedCard)} — CLICK DEPLOY HERE. ENEMY CONTROLS FRONTLINE.";
            }

            return $"SELECTED {ShortName(selectedCard)} — CLICK DEPLOY HERE OR MOBILIZE";
        }

        if (selectedCard.Zone == CardZone.Countermeasure)
        {
            return $"CHECKING SET COUNTER — {ShortName(selectedCard)} WAITS FOR ENEMY ATTACK";
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Countermeasure)
        {
            return $"SELECTED {ShortName(selectedCard)} — CLICK BOARD TO SET COUNTER";
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Order)
        {
            return OrderNeedsTarget(selectedCard)
                ? $"SELECTED {ShortName(selectedCard)} — CLICK TARGET OR BOARD AUTO-TARGET"
                : $"SELECTED {ShortName(selectedCard)} — CLICK BOARD TO PLAY";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport && !selectedCard.CanOperate(availableKredits))
        {
            return $"SELECTED {ShortName(selectedCard)} — {CannotAdvancePrompt(selectedCard, availableKredits)}";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport && hasFrontlineController && frontlineController != selectedCard.Owner)
        {
            return $"SELECTED {ShortName(selectedCard)} — ENEMY CONTROLS FRONTLINE. CLEAR IT BEFORE ADVANCING.";
        }

        if (selectedCard.Zone == CardZone.Frontline)
        {
            int maxAttacks = selectedCard.HasKeyword(CardKeyword.Fury) ? 2 : 1;
            if (selectedCard.HasKeyword(CardKeyword.Pinned) || selectedCard.OperationCost > availableKredits || selectedCard.AttacksThisTurn >= maxAttacks)
            {
                return $"SELECTED {ShortName(selectedCard)} — {CannotAttackPrompt(selectedCard, availableKredits)}";
            }

            return $"SELECTED {ShortName(selectedCard)} — CLICK ATTACK TARGET OR HQ";
        }

        string label = SlotHighlightLabelRules.LabelFor(selectedCard, PreferredTargetZone(selectedCard));
        return $"SELECTED {ShortName(selectedCard)} — CLICK {label}";
    }

    public static string AfterDeployPrompt(RuntimeCard card)
    {
        if (card != null && card.HasKeyword(CardKeyword.Blitz))
        {
            return $"{ShortName(card)} DEPLOYED. NEXT: SELECT A SUPPORT UNIT TO ADVANCE, PLAY ANOTHER CARD, OR END TURN.";
        }

        return $"{ShortName(card)} DEPLOYED. NEXT: THIS UNIT CAN ACT NEXT TURN. PLAY ANOTHER CARD OR END TURN.";
    }

    public static string AfterCountermeasurePrompt(RuntimeCard card)
    {
        return $"{ShortName(card)} SET. NEXT: PLAY ANOTHER CARD OR END TURN.";
    }

    public static string AfterOrderPrompt(RuntimeCard card)
    {
        return $"{ShortName(card)} RESOLVED. NEXT: PLAY ANOTHER CARD, ATTACK, OR END TURN.";
    }

    public static string IllegalOrderTargetPrompt(RuntimeCard order, RuntimeCard targetCard, PlayerSide caster)
    {
        if (order == null || order.Type != CardType.Order)
        {
            return "SELECT AN ORDER FIRST.";
        }

        switch (order.EffectType)
        {
            case CardEffectType.DamageTargetUnit:
                if (targetCard != null && targetCard.HasKeyword(CardKeyword.Smokescreen))
                {
                    return $"{ShortName(targetCard)} HAS SMOKESCREEN — ORDERS CANNOT TARGET IT.";
                }

                return $"{ShortName(order)} NEEDS AN ENEMY UNIT TARGET.";
            case CardEffectType.PinTargetUnit:
                if (targetCard != null && targetCard.HasKeyword(CardKeyword.Smokescreen))
                {
                    return $"{ShortName(targetCard)} HAS SMOKESCREEN — ORDERS CANNOT TARGET IT.";
                }

                return $"{ShortName(order)} NEEDS AN ENEMY UNIT TARGET.";
            case CardEffectType.BuffFriendlyUnit:
                return $"{ShortName(order)} NEEDS A FRIENDLY UNIT TARGET.";
            case CardEffectType.DamageEnemyHeadquarters:
            case CardEffectType.RepairHeadquarters:
            case CardEffectType.DrawCards:
                return $"{ShortName(order)} CAN BE PLAYED ON ANY BOARD SLOT.";
            default:
                return "THAT IS NOT A LEGAL TARGET FOR THIS ORDER.";
        }
    }

    public static string IllegalDeployTargetPrompt(RuntimeCard card, SlotZone targetZone, bool occupied, bool hasFrontlineController, PlayerSide frontlineController)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return "SELECT A UNIT CARD TO DEPLOY.";
        }

        if (occupied)
        {
            return $"{ShortName(card)} CANNOT DEPLOY THERE — SLOT IS OCCUPIED.";
        }

        if (targetZone == SlotZone.PlayerSupport)
        {
            return $"{ShortName(card)} CAN DEPLOY TO EMPTY SUPPORT SLOTS.";
        }

        if (!card.HasKeyword(CardKeyword.Mobilize))
        {
            return $"{ShortName(card)} DEPLOYS TO SUPPORT. ONLY MOBILIZE UNITS CAN ENTER FRONTLINE DIRECTLY.";
        }

        if (targetZone == SlotZone.Frontline && hasFrontlineController && frontlineController != card.Owner)
        {
            return $"{ShortName(card)} CANNOT MOBILIZE — ENEMY CONTROLS THE FRONTLINE.";
        }

        return $"{ShortName(card)} CAN DEPLOY TO EMPTY SUPPORT OR CONTROLLED FRONTLINE SLOTS.";
    }

    public static string CannotAffordCardPrompt(RuntimeCard card, string action, int availableKredits)
    {
        if (card == null)
        {
            return "NOT ENOUGH KREDITS.";
        }

        string verb = string.IsNullOrEmpty(action) ? "PLAY" : action.ToUpperInvariant();
        return $"{ShortName(card)} CANNOT {verb} — NEED {card.KreditCost} KREDITS, HAVE {availableKredits}.";
    }

    public static string CountermeasureRowFullPrompt(RuntimeCard card)
    {
        return $"COUNTERMEASURE ROW FULL — MAX 3 SET COUNTERS. {ShortName(card)} STAYS IN HAND.";
    }

    public static string HeadquartersClickedPrompt(PlayerSide side)
    {
        return side == PlayerSide.Enemy
            ? "ENEMY HQ — SELECT A FRONTLINE UNIT, THEN CLICK HQ TO ATTACK."
            : "YOUR HQ — DEFEND IT. ENEMY UNITS ATTACK THIS SLOT.";
    }

    public static string EmptySlotClickedPrompt(SlotZone zone)
    {
        switch (zone)
        {
            case SlotZone.PlayerSupport:
                return "EMPTY SUPPORT SLOT — SELECT A HAND UNIT, THEN CLICK HERE TO DEPLOY.";
            case SlotZone.Frontline:
                return "EMPTY FRONTLINE SLOT — SELECT A SUPPORT UNIT TO ADVANCE OR A MOBILIZE UNIT.";
            case SlotZone.EnemySupport:
                return "EMPTY ENEMY SUPPORT SLOT — ENEMY UNITS DEPLOY HERE.";
            default:
                return "EMPTY SLOT — SELECT A CARD FIRST.";
        }
    }

    public static string IllegalHeadquartersTargetPrompt(RuntimeCard selectedCard, PlayerSide headquartersSide)
    {
        if (selectedCard == null)
        {
            return HeadquartersClickedPrompt(headquartersSide);
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit)
        {
            return $"{ShortName(selectedCard)} CANNOT DEPLOY TO HQ — HQ IS NOT A DEPLOY SLOT.";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport)
        {
            return $"{ShortName(selectedCard)} MUST ADVANCE TO FRONTLINE BEFORE ATTACKING HQ.";
        }

        if (selectedCard.Zone == CardZone.Frontline && headquartersSide == selectedCard.Owner)
        {
            return $"{ShortName(selectedCard)} CANNOT ATTACK YOUR HQ — CLICK ENEMY HQ OR ENEMY UNIT.";
        }

        return "THAT HQ IS NOT A LEGAL TARGET FOR THE SELECTED CARD.";
    }

    public static string OpponentCardClickedPrompt(RuntimeCard card)
    {
        return $"INSPECTING ENEMY {ShortName(card)} — SELECT AN ORDER OR FRONTLINE UNIT TO TARGET IT.";
    }

    public static string IllegalOpponentCardTargetPrompt(RuntimeCard selectedCard, RuntimeCard targetCard)
    {
        if (selectedCard == null)
        {
            return OpponentCardClickedPrompt(targetCard);
        }

        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit)
        {
            return $"{ShortName(selectedCard)} CANNOT TARGET ENEMY CARDS — DEPLOY TO SUPPORT FIRST.";
        }

        if (selectedCard.Zone == CardZone.PlayerSupport)
        {
            return $"{ShortName(selectedCard)} CANNOT ATTACK FROM SUPPORT — ADVANCE TO FRONTLINE FIRST.";
        }

        return $"{ShortName(selectedCard)} CANNOT TARGET {ShortName(targetCard)} THIS WAY.";
    }

    public static string OwnCardClickedWhileHandUnitSelectedPrompt(RuntimeCard selectedCard, RuntimeCard targetCard)
    {
        return $"{ShortName(targetCard)} SLOT IS OCCUPIED — DEPLOY {ShortName(selectedCard)} TO AN EMPTY SUPPORT SLOT.";
    }

    public static string MissedDragTargetPrompt(RuntimeCard selectedCard)
    {
        return $"MISSED BOARD TARGET — DROP ON A HIGHLIGHTED SLOT TO USE {ShortName(selectedCard)}.";
    }

    public static string NoPlayableCardPrompt(bool hasHandCards, bool hasAffordableCard, bool supportFull, bool missingOrderTarget)
    {
        if (!hasHandCards)
        {
            return "NO PLAYABLE CARD — HAND IS EMPTY.";
        }

        if (!hasAffordableCard)
        {
            return "NO PLAYABLE CARD — NEED MORE KREDITS.";
        }

        if (supportFull)
        {
            return "NO PLAYABLE UNIT — SUPPORT LINE IS FULL.";
        }

        if (missingOrderTarget)
        {
            return "NO PLAYABLE ORDER — NO LEGAL ORDER TARGET.";
        }

        return "NO PLAYABLE CARD — CHECK SPACE, KREDITS, OR LEGAL TARGETS.";
    }

    public static string NoAdvanceShortcutPrompt(bool hasSupportUnit, bool needsKredits, bool pinned, bool alreadyActed, bool frontlineBlocked, bool frontlineFull)
    {
        if (frontlineFull)
        {
            return "NO ADVANCE — FRONTLINE IS FULL.";
        }

        if (!hasSupportUnit)
        {
            return "NO ADVANCE — NO SUPPORT UNIT TO ADVANCE.";
        }

        if (frontlineBlocked)
        {
            return "NO ADVANCE — ENEMY CONTROLS THE FRONTLINE.";
        }

        if (needsKredits)
        {
            return "NO ADVANCE — NEED MORE KREDITS.";
        }

        if (pinned)
        {
            return "NO ADVANCE — SUPPORT UNIT IS PINNED.";
        }

        if (alreadyActed)
        {
            return "NO ADVANCE — SUPPORT UNIT ALREADY ACTED.";
        }

        return "NO ADVANCE — SELECT A READY SUPPORT UNIT.";
    }

    public static string NoAttackShortcutPrompt(bool hasFrontlineUnit, bool needsKredits, bool pinned, bool alreadyAttacked, bool missingTarget)
    {
        if (!hasFrontlineUnit)
        {
            return "NO ATTACK — NO FRONTLINE UNIT TO ATTACK WITH.";
        }

        if (needsKredits)
        {
            return "NO ATTACK — NEED MORE KREDITS.";
        }

        if (pinned)
        {
            return "NO ATTACK — FRONTLINE UNIT IS PINNED.";
        }

        if (alreadyAttacked)
        {
            return "NO ATTACK — FRONTLINE UNIT ALREADY ATTACKED.";
        }

        if (missingTarget)
        {
            return "NO ATTACK — NO LEGAL ATTACK TARGET.";
        }

        return "NO ATTACK — SELECT A READY FRONTLINE UNIT.";
    }

    public static string AfterAdvancePrompt(RuntimeCard card)
    {
        return AfterAdvancePrompt(card, int.MaxValue);
    }

    public static string CannotAdvancePrompt(RuntimeCard card, int availableKredits)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return "SELECT A SUPPORT UNIT TO ADVANCE.";
        }

        if (card.HasKeyword(CardKeyword.Pinned))
        {
            return $"{ShortName(card)} IS PINNED — IT SKIPS THIS OPERATION WINDOW.";
        }

        if (card.HasActed)
        {
            return $"{ShortName(card)} ALREADY ACTED THIS TURN.";
        }

        if (card.OperationCost > availableKredits)
        {
            return $"{ShortName(card)} NEED {card.OperationCost} KREDITS TO ADVANCE.";
        }

        return $"{ShortName(card)} CANNOT ADVANCE NOW.";
    }

    public static string AfterAdvancePrompt(RuntimeCard card, int remainingKredits)
    {
        if (card != null && card.OperationCost > remainingKredits)
        {
            return $"{ShortName(card)} ADVANCED. NEXT: NEED {card.OperationCost} KREDITS TO ATTACK, OR END TURN.";
        }

        return $"{ShortName(card)} ADVANCED. NEXT: SELECT A FRONTLINE UNIT TO ATTACK, OR END TURN.";
    }

    public static string AfterAttackPrompt(RuntimeCard card)
    {
        return AfterAttackPrompt(card, int.MaxValue);
    }

    public static string CannotAttackPrompt(RuntimeCard card, int availableKredits)
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return "SELECT A FRONTLINE UNIT TO ATTACK.";
        }

        if (card.Zone != CardZone.Frontline)
        {
            return $"{ShortName(card)} MUST ADVANCE TO FRONTLINE BEFORE ATTACKING.";
        }

        if (card.HasKeyword(CardKeyword.Pinned))
        {
            return $"{ShortName(card)} IS PINNED — IT CANNOT ATTACK THIS TURN.";
        }

        if (card.OperationCost > availableKredits)
        {
            return $"{ShortName(card)} NEED {card.OperationCost} KREDITS TO ATTACK.";
        }

        int maxAttacks = card.HasKeyword(CardKeyword.Fury) ? 2 : 1;
        if (card.AttacksThisTurn >= maxAttacks)
        {
            return $"{ShortName(card)} ALREADY ATTACKED THIS TURN.";
        }

        return $"{ShortName(card)} CANNOT ATTACK NOW.";
    }

    public static string IllegalAttackTargetPrompt(RuntimeCard attacker, RuntimeCard targetCard, SlotZone targetZone, bool defenderHasGuard)
    {
        if (attacker == null || attacker.Type != CardType.Unit)
        {
            return "SELECT A FRONTLINE UNIT TO ATTACK.";
        }

        SlotZone enemySupport = attacker.Owner == PlayerSide.Player ? SlotZone.EnemySupport : SlotZone.PlayerSupport;
        if (targetZone != enemySupport)
        {
            return $"{ShortName(attacker)} ATTACKS ENEMY SUPPORT OR HQ, NOT THIS LANE.";
        }

        if (targetCard != null && targetCard.Owner == attacker.Owner)
        {
            return $"{ShortName(attacker)} NEEDS AN ENEMY TARGET.";
        }

        if (defenderHasGuard && (targetCard == null || !targetCard.HasKeyword(CardKeyword.Guard)))
        {
            return "GUARD IS ACTIVE — ATTACK A GUARD UNIT FIRST.";
        }

        return "THAT IS NOT A LEGAL ATTACK TARGET.";
    }

    public static string AfterAttackPrompt(RuntimeCard card, int remainingKredits)
    {
        if (card != null && card.HasKeyword(CardKeyword.Fury) && card.AttacksThisTurn == 1)
        {
            if (card.OperationCost > remainingKredits)
            {
                return $"{ShortName(card)} ATTACKED. NEXT: FURY NEED {card.OperationCost} KREDITS TO ATTACK AGAIN, OR END TURN.";
            }

            return $"{ShortName(card)} ATTACKED. NEXT: FURY — SAME UNIT CAN ATTACK AGAIN, OR END TURN.";
        }

        return $"{ShortName(card)} ATTACKED. NEXT: ATTACK WITH ANOTHER UNIT OR END TURN.";
    }

    private static SlotZone PreferredTargetZone(RuntimeCard selectedCard)
    {
        if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit)
        {
            return SlotZone.PlayerSupport;
        }

        if (selectedCard.Zone == CardZone.PlayerSupport)
        {
            return SlotZone.Frontline;
        }

        if (selectedCard.Zone == CardZone.Frontline || selectedCard.Type == CardType.Order)
        {
            return SlotZone.EnemySupport;
        }

        return SlotZone.PlayerSupport;
    }

    private static bool OrderNeedsTarget(RuntimeCard order)
    {
        return order.EffectType == CardEffectType.DamageTargetUnit
            || order.EffectType == CardEffectType.PinTargetUnit
            || order.EffectType == CardEffectType.BuffFriendlyUnit;
    }

    private static string ShortName(RuntimeCard selectedCard)
    {
        if (string.IsNullOrEmpty(selectedCard.CardName))
        {
            return "CARD";
        }

        const int maxLength = 16;
        return selectedCard.CardName.Length <= maxLength
            ? selectedCard.CardName.ToUpperInvariant()
            : selectedCard.CardName.Substring(0, maxLength - 1).ToUpperInvariant() + "…";
    }
}
