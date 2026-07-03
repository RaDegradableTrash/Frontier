using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
        private const float PointerDragStartThresholdPixels = 18f;
        private const float PointerDragStartHoldSeconds = 0.02f;
        private CardView unifiedHandPointerView;
        private Vector3 unifiedHandPointerDownScreenPosition;
        private float unifiedHandPointerDownTime;
        private bool unifiedHandPointerDragging;

        private void UpdateCardHover()
        {
            if (isResolvingEvents)
            {
                SetHoveredCardView(null);
                return;
            }

            Camera mainCamera = Camera.main;
            CardView pointerView = CardView.RaycastPointerCard(mainCamera, Input.mousePosition);
            if (pointerView == null)
            {
                pointerView = FindProjectedPointerCard(mainCamera, Input.mousePosition);
            }

            SetHoveredCardView(pointerView);
        }

        private void SetHoveredCardView(CardView view)
        {
            if (view == hoveredCardView)
            {
                return;
            }

            CardView previous = hoveredCardView;
            hoveredCardView = view;
            if (previous != null)
            {
                previous.SetPointerHovered(false);
                HandleCardHoverEnded(previous);
            }

            if (hoveredCardView != null)
            {
                hoveredCardView.SetPointerHovered(true);
                HandleCardHovered(hoveredCardView);
            }
        }

        private void UpdateHandReveal()
        {
            if (MatchStartRules.ShouldForceRevealPlayerHand(phase, activeSide))
            {
                if (!playerHandRevealed)
                {
                    SetPlayerHandRevealed(true);
                }

                return;
            }

            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                if (playerHandRevealed)
                {
                    SetPlayerHandRevealed(false);
                }

                return;
            }

            bool pointerOverPlayerHand = IsPointerRaycastOverPlayerHand();
            if (pointerOverPlayerHand)
            {
                playerHandRevealGraceUntil = Time.time + 0.14f;
            }

            bool shouldReveal = pointerOverPlayerHand || Time.time < playerHandRevealGraceUntil;
            if (shouldReveal == playerHandRevealed)
            {
                return;
            }

            SetPlayerHandRevealed(shouldReveal);
        }

        public void SetPlayerHandRevealRequested(bool revealRequested)
        {
            if (revealRequested)
            {
                playerHandRevealGraceUntil = Time.time + 0.14f;
                playerHandRevealRequested = true;
                UpdateHandReveal();
            }
        }

        public void RegisterDirectPointerInteraction(CardView view)
        {
        }

        private bool IsPointerRaycastOverPlayerHand()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            Vector3 pointer = Input.mousePosition;
            if (pointer.x < 0f || pointer.y < 0f || pointer.x > Screen.width || pointer.y > Screen.height)
            {
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(pointer);
            int hitCount = Physics.RaycastNonAlloc(ray, pointerRaycastHits, 50f);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = pointerRaycastHits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                CardView cardView = hitCollider.GetComponent<CardView>();
                if (cardView != null
                    && cardView.Card != null
                    && cardView.Card.Owner == PlayerSide.Player
                    && cardView.Card.Zone == CardZone.Hand)
                {
                    return true;
                }
            }

            return false;
        }

        private void RevealPlayerHandBriefly(float seconds = 4f)
        {
            playerHandRevealGraceUntil = Time.time + seconds;
            SetPlayerHandRevealed(true);
        }

        public void HandleCardClicked(CardView view)
        {
            if (isResolvingEvents || view == null || view.Card == null)
            {
                return;
            }

            RuntimeCard clicked = view.Card;
            if (lastCardClickHandledFrame == Time.frameCount && lastClickedCard == clicked)
            {
                return;
            }

            lastCardClickHandledFrame = Time.frameCount;
            lastClickedCard = clicked;

            if (MulliganRules.CanMarkForDiscard(phase, activeSide, clicked))
            {
                bool marked = MulliganRules.ToggleMarked(mulliganMarkedIds, clicked);
                view.SetMulliganMarked(marked);
                view.PlayMulliganSelectionPulse();
                SetStatus(marked
                    ? $"Marked {clicked.CardName} for mulligan. Click Mulligan to replace marked cards."
                    : $"Unmarked {clicked.CardName}.");
                return;
            }

            if (IsAirborneUnitSelectionActive())
            {
                hoveredHandCardId = null;
                if (clicked.Owner == PlayerSide.Player && clicked.Zone == CardZone.Hand && clicked.Type == CardType.Unit)
                {
                    pendingAirborneUnit = clicked;
                    CompleteAirborneDeployment();
                    return;
                }

                inspectedCard = pendingAirborneOrder;
                centerInspectCard = pendingAirborneOrder;
                RefreshSceneInspector();
                SetStatus("AIRBORNE: SELECT A UNIT CARD FROM HAND.");
                RefreshAllViews();
                return;
            }

            if (CardInspectModeRules.ShouldExitInspectMode(clicked, centerInspectCard))
            {
                CloseCardInspectView();
                return;
            }

            if (CardInspectModeRules.ShouldEnterInspectMode(phase, activeSide, clicked, centerInspectCard))
            {
                ClearSelection();
                hoveredHandCardId = null;
                SetPlayerHandRevealed(true);
                playerHandRevealGraceUntil = Time.time + 2.25f;
                centerInspectCard = clicked;
                inspectedCard = clicked;
                SetStatus($"Viewing {clicked.CardName}. Click again to close.");
                RefreshSceneInspector();
                RefreshAllViews();
                return;
            }

            if (MatchStartRules.ShouldInspectOnlyDuringOpeningHand(phase, activeSide, clicked))
            {
                inspectedCard = clicked;
                SetStatus($"Inspecting opening hand: {clicked.CardName}. Keep or Mulligan.");
                RefreshSceneInspector();
                return;
            }

            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
                return;
            }

            inspectedCard = clicked;
            RefreshSceneInspector();
            if (clicked.Owner != PlayerSide.Player)
            {
                if (selectedCard != null && cardSlots.TryGetValue(clicked, out SlotInteract targetSlot))
                {
                    if (selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Order)
                    {
                        TryPlayOrderOnSlot(targetSlot);
                    }
                    else if (IsBoardCombatUnit(selectedCard))
                    {
                        TryAttack(targetSlot);
                    }
                    else
                    {
                        SetStatus(SceneGuidanceRules.IllegalOpponentCardTargetPrompt(selectedCard, clicked));
                    }
                }
                else
                {
                    SetStatus(SceneGuidanceRules.OpponentCardClickedPrompt(clicked));
                }

                return;
            }

            if (clicked.Zone == CardZone.Countermeasure)
            {
                inspectedCard = clicked;
                SetStatus($"Checked countermeasure: {clicked.CardName}.");
                RefreshSceneInspector();
                return;
            }

            if (pendingAirborneOrder != null && pendingAirborneOrder.EffectType == CardEffectType.DeployWithBlitz
                && pendingAirborneOrder.Zone == CardZone.Hand && pendingAirborneOrder.Type == CardType.Order
                && pendingAirborneSlot != null && clicked.Type == CardType.Unit && clicked.Zone == CardZone.Hand)
            {
                pendingAirborneUnit = clicked;
                CompleteAirborneDeployment();
                return;
            }

            if (pendingAirborneSlot != null && pendingAirborneOrder != null && pendingAirborneOrder.EffectType == CardEffectType.DeployWithBlitz)
            {
                SetStatus("AIRBORNE: SELECT A UNIT CARD FROM HAND.");
                return;
            }

            if (clicked.Owner == PlayerSide.Player && clicked.Zone == CardZone.Hand)
            {
                ClearSelection();
                hoveredHandCardId = null;
                centerInspectCard = clicked;
                inspectedCard = clicked;
                SetStatus($"Viewing {clicked.CardName}. Drag the card to play it.");
                RefreshSceneInspector();
                RefreshAllViews();
                return;
            }

            if (selectedCard != null && selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit && cardSlots.ContainsKey(clicked))
            {
                SetStatus(SceneGuidanceRules.OwnCardClickedWhileHandUnitSelectedPrompt(selectedCard, clicked));
                return;
            }

            if (selectedCard != null && selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Countermeasure && cardSlots.ContainsKey(clicked))
            {
                TrySetCountermeasure(player, selectedCard);
                return;
            }

            if (selectedCard != null && selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Order && cardSlots.TryGetValue(clicked, out SlotInteract friendlyTarget))
            {
                TryPlayOrderOnSlot(friendlyTarget);
                return;
            }

            SelectCard(clicked, view);
        }

        public bool ShouldRouteCardClickToMulligan(CardView view)
        {
            return view != null && MulliganRules.CanMarkForDiscard(phase, activeSide, view.Card);
        }

        public void HandleCardInspectRequested(CardView view)
        {
            if (isResolvingEvents || view == null || view.Card == null)
            {
                return;
            }

            if (lastInspectClickHandledFrame == Time.frameCount && lastInspectClickedCard == view.Card)
            {
                return;
            }

            lastInspectClickHandledFrame = Time.frameCount;
            lastInspectClickedCard = view.Card;
            lastSceneCommandPointerFrame = Time.frameCount;

            if (view == centerInspectView)
            {
                CloseCardInspectView();
                return;
            }

            RuntimeCard clicked = view.Card;
            if (phase == GamePhase.Mulligan && clicked.Owner == PlayerSide.Player && clicked.Zone == CardZone.Hand)
            {
                HandleCardClicked(view);
                return;
            }

            if (IsAirborneUnitSelectionActive())
            {
                HandleCardClicked(view);
                return;
            }

            if (MulliganRules.CanMarkForDiscard(phase, activeSide, clicked))
            {
                HandleCardClicked(view);
                return;
            }

            if (clicked.Owner == PlayerSide.Player && clicked.Zone == CardZone.Hand)
            {
                if (CardInspectModeRules.ShouldExitInspectMode(clicked, centerInspectCard))
                {
                    CloseCardInspectView();
                    return;
                }

                ClearSelection();
                hoveredHandCardId = null;
                SetPlayerHandRevealed(true);
                playerHandRevealGraceUntil = Time.time + 2.25f;
                centerInspectCard = clicked;
                inspectedCard = clicked;
                SetStatus($"Viewing {clicked.CardName}. Drag the hand card to play it.");
                RefreshSceneInspector();
                RefreshAllViews();
                return;
            }

            if (CardInspectModeRules.ShouldExitInspectMode(clicked, centerInspectCard))
            {
                CloseCardInspectView();
                return;
            }

            if (!CardInspectModeRules.ShouldEnterInspectMode(phase, activeSide, clicked, centerInspectCard))
            {
                HandleCardClicked(view);
                return;
            }

            ClearSelection();
            hoveredHandCardId = null;
                SetPlayerHandRevealed(true);
                playerHandRevealGraceUntil = Time.time + 2.25f;
                centerInspectCard = clicked;
                inspectedCard = clicked;
            SetStatus($"Viewing {clicked.CardName}. Drag the hand card to play it.");
            RefreshSceneInspector();
            RefreshAllViews();
        }

        private void CloseCardInspectView()
        {
            centerInspectCard = null;
            inspectedCard = null;
            hoveredHandCardId = null;
            SetStatus("Closed card detail view.");
            RefreshSceneInspector();
            RefreshAllViews();            
        }

        public void HandleCardDragStarted(CardView view)
        {
            if (view == null || view.Card == null)
            {
                return;
            }

            if (centerInspectCard != null && centerInspectCard.Id == view.Card.Id)
            {
                centerInspectCard = null;
                hoveredHandCardId = null;
                DestroyCenterInspectView();
                inspectedCard = view.Card;
            }

            if (view.Card.Owner == PlayerSide.Player && view.Card.Zone == CardZone.Hand)
            {
                BeginDraggingHandCard(view.Card);
            }
        }

        public void HandleCardHovered(CardView view)
        {
            if (isResolvingEvents || view == null || view.Card == null || !CardTextRules.CanHoverInspect(view.Card, view.IsHidden))
            {
                return;
            }

            if (Input.GetMouseButtonUp(0)
                && view.Card.Owner == PlayerSide.Player
                && view.Card.Zone == CardZone.Hand)
            {
                HandlePointerClickRelease(view);
                return;
            }

            inspectedCard = view.Card;
            if (IsAirborneUnitSelectionActive())
            {
                RefreshSceneInspector();
                return;
            }

            if (view.Card.Zone == CardZone.Hand && view.Card.Owner == PlayerSide.Player)
            {
                if (view != centerInspectView && hoveredHandCardId != view.Card.Id)
                {
                    hoveredHandCardId = view.Card.Id;
                    RefreshHandLayouts();
                }
            }

            RefreshSceneInspector();
        }

        public void HandleCardHoverEnded(CardView view)
        {
            if (view == null || view == centerInspectView || view.Card == null || hoveredHandCardId != view.Card.Id)
            {
                return;
            }

            hoveredHandCardId = null;
            RefreshHandLayouts();
        }

        public void HandleCardReleased(CardView view, Vector3 releasePosition)
        {
            if (isResolvingEvents || view == null || view.Card == null)
            {
                return;
            }

            if (view != centerInspectView && view.Card.Zone == CardZone.Hand)
            {
                ClearCardInspectState();
            }

            if (IsAirborneUnitSelectionActive()
                && view.Card.Owner == PlayerSide.Player
                && view.Card.Zone == CardZone.Hand
                && view.Card.Type == CardType.Unit)
            {
                pendingAirborneUnit = view.Card;
                CompleteAirborneDeployment();
                return;
            }

            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
                ClearDraggedHandCardIfNeeded(true);
                ClearCardInspectState();
                ClearSelection();
                RefreshAllViews();
                return;
            }

            SlotInteract slot = ResolvePointerSlot(releasePosition, view.Card);
            if (slot == null)
            {
                if (IsDraggingHandCard(view.Card))
                {
                    ClearDraggedHandCardIfNeeded(true);
                }

                HandleFailedCardPlacement(view, SceneGuidanceRules.MissedDragTargetPrompt(view.Card));
                return;
            }

            if (selectedCard != view.Card)
            {
                SelectCard(view.Card, view);
            }

            HandleSlotClicked(slot);

            if (selectedCard == view.Card || IsDraggingHandCard(view.Card))
            {
                ClearDraggedHandCardIfNeeded(false);
            }
        }

        private void HandleFailedCardPlacement(CardView view, string statusMessage)
        {
            if (view == null || view.Card == null)
            {
                return;
            }

            SetStatus(statusMessage);
            if (view.Card.Zone == CardZone.Hand && view.Card.Owner == PlayerSide.Player)
            {
                Vector3 returnPosition = DraggedHandReturnPosition(view.Card);
                view.PlayFailedReturn(view.transform.position, returnPosition);
            }

            if (selectedCard == view.Card)
            {
                ClearSelection();
            }

            RefreshSceneInspector();
            RefreshHandLayouts();
        }

        public void HandleBoardCardDragPreview(CardView view, Vector3 pointerPosition)
        {
            if (view == null || view.Card == null || board == null)
            {
                return;
            }

            ClearCardDamagePreviews();
            SlotInteract targetSlot = ResolvePointerSlot(pointerPosition, view.Card);
            bool canAttack = CanAttack(view.Card, player.Kredits);
            bool legalAttack = canAttack && IsLegalAttackTarget(view.Card, targetSlot);
            string label = DragTargetLabelRules.LabelFor(view.Card, targetSlot, legalAttack);
            DamagePreview preview = canAttack ? BuildAttackDamagePreview(view.Card, targetSlot) : default;
            ApplyDamagePreviewToSlot(targetSlot, preview);
            ApplyDamagePreviewToView(view, preview.CounterDamage, preview.AttackerLethal);
            if (dragTargetArrow == null)
            {
                GameObject arrowObject = new GameObject("Drag Target Arrow");
                dragTargetArrow = arrowObject.AddComponent<DragTargetArrow>();
                dragTargetArrow.Initialize();
            }

            dragTargetArrow.UpdateArrow(view.transform.position, pointerPosition, label, preview, view.transform.position);
        }

        public void HandleHandOrderDragPreview(CardView view, Vector3 pointerPosition)
        {
            if (view == null || view.Card == null || view.Card.Type != CardType.Order)
            {
                return;
            }

            if (OrderDragRules.ShouldFollowPointer(view.Card))
            {
                ClearDragPreview();
                return;
            }

            SlotInteract targetSlot = ResolvePointerOrderSlot(pointerPosition, view.Card);
            bool legalTarget = IsLegalOrderTarget(view.Card, targetSlot, PlayerSide.Player);
            string label = legalTarget ? "PLAY ORDER" : "TARGET";
            DamagePreview preview = BuildOrderDamagePreview(view.Card, targetSlot);
            ClearCardDamagePreviews();
            ApplyDamagePreviewToSlot(targetSlot, preview);
            ApplyOrderAdjacentDamagePreviews(view.Card, targetSlot);
            if (dragTargetArrow == null)
            {
                GameObject arrowObject = new GameObject("Drag Target Arrow");
                dragTargetArrow = arrowObject.AddComponent<DragTargetArrow>();
                dragTargetArrow.Initialize();
            }

            dragTargetArrow.UpdateArrow(view.transform.position + Vector3.up * 0.2f, pointerPosition, label, preview, view.transform.position);
        }

        public void HandleHandOrderReleased(CardView view, Vector3 releasePosition)
        {
            if (view == null || view.Card == null || view.Card.Type != CardType.Order)
            {
                return;
            }

            ClearCardInspectState();
            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
                ClearDraggedHandCardIfNeeded(true);
                RefreshAllViews();
                return;
            }

            SelectCard(view.Card, view);
            if (OrderDragRules.ShouldFollowPointer(view.Card))
            {
                if (player.CanSpendKredits(view.Card.KreditCost) && IsLegalOrderTarget(view.Card, null, PlayerSide.Player))
                {
                    TryPlayOrderOnSlot(null);
                }
                else
                {
                    RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(view.Card, "play order", player.Kredits));
                    ClearDraggedHandCardIfNeeded(true);
                }

                return;
            }

            SlotInteract slot = ResolvePointerOrderSlot(releasePosition, view.Card);
            if (view.Card.EffectType == CardEffectType.DeployWithBlitz)
            {
                TryPlayAirborneDeployment(slot);
                ClearDraggedHandCardIfNeeded(false);
                return;
            }

            if (slot == null || !IsLegalOrderTarget(view.Card, slot, PlayerSide.Player))
            {
                RejectSelectedHandCard(SceneGuidanceRules.IllegalOrderTargetPrompt(view.Card, slot != null ? slot.Occupant : null, PlayerSide.Player));
                ClearDraggedHandCardIfNeeded(true);
                return;
            }

            bool playedOrder = TryPlayOrderOnSlot(slot);
            if (playedOrder)
            {
                ClearDraggedHandCardIfNeeded(false);
            }
        }

        public void ClearDragPreview()
        {
            if (dragTargetArrow != null)
            {
                RuntimeSafeDestroy.Destroy(dragTargetArrow.gameObject);
                dragTargetArrow = null;
            }

            ClearCardDamagePreviews();
        }

        public void HandleBoardAreaClicked()
        {
            if (isResolvingEvents)
            {
                return;
            }

            if (IsAirborneUnitSelectionActive())
            {
                CancelPendingAirborneDeployment();
                return;
            }

            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
                return;
            }

            BoardAreaClickAction action = BoardAreaClickRules.ActionFor(selectedCard);
            switch (action)
            {
                case BoardAreaClickAction.SetCountermeasure:
                    TrySetCountermeasure(player, selectedCard);
                    break;
                case BoardAreaClickAction.PlayOrder:
                    if (selectedCard != null && selectedCard.EffectType == CardEffectType.DeployWithBlitz)
                    {
                        SetStatus("AIRBORNE: SELECT AN EMPTY SLOT.");
                        break;
                    }
                    TryPlayOrderOnSlot(OrderNeedsTarget(selectedCard) ? FindQuickOrderTarget(selectedCard) : null);
                    break;
                case BoardAreaClickAction.NeedsSlot:
                    SetStatus(SceneGuidanceRules.EmptySlotClickedPrompt(SlotZone.PlayerSupport));
                    break;
                default:
                    SetStatus("Select a hand card, then click a slot or the board.");
                    break;
            }
        }

        public void HandleSlotClicked(SlotInteract slot)
        {
            if (isResolvingEvents || slot == null)
            {
                return;
            }

            if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
            {
                SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
                return;
            }

            if (selectedCard == null)
            {
                if (BoardTargetRules.IsHeadquartersSlot(slot))
                {
                    PlayerSide headquartersSide = HeadquartersSideForSlot(slot);
                    SetStatus(SceneGuidanceRules.HeadquartersClickedPrompt(headquartersSide));
                    return;
                }

                if (!slot.IsOccupied)
                {
                    SetStatus(SceneGuidanceRules.EmptySlotClickedPrompt(slot.Zone));
                    return;
                }

                TrySelectUnitInSlot(slot);
                return;
            }

            if (BoardTargetRules.IsHeadquartersSlot(slot))
            {
                PlayerSide headquartersSide = HeadquartersSideForSlot(slot);
                bool handUnitCannotDeployToHeadquarters = selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit;
                bool selectedUnitTargetingOwnHeadquarters = selectedCard.Type == CardType.Unit && headquartersSide == selectedCard.Owner;
                if (handUnitCannotDeployToHeadquarters || selectedUnitTargetingOwnHeadquarters)
                {
                    SetStatus(SceneGuidanceRules.IllegalHeadquartersTargetPrompt(selectedCard, headquartersSide));
                    return;
                }
            }

            if (selectedCard.Zone == CardZone.Hand)
            {
                if (selectedCard.Type == CardType.Unit)
                {
                    TryDeploySelectedUnit(slot);
                }
                else if (selectedCard.Type == CardType.Order)
                {
                    if (selectedCard.EffectType == CardEffectType.DeployWithBlitz)
                    {
                        TryPlayAirborneDeployment(slot);
                    }
                    else
                    {
                    TryPlayOrderOnSlot(slot);
                    }
                }
                else if (selectedCard.Type == CardType.Countermeasure)
                {
                    TrySetCountermeasure(player, selectedCard);
                }

                return;
            }

            if (selectedCard.Type == CardType.Unit)
            {
                if (BoardTargetRules.IsHeadquartersSlot(slot) && HeadquartersSideForSlot(slot) != selectedCard.Owner)
                {
                    TryAttack(slot);
                }
                else if (slot.IsOccupied && slot.Occupant != null && slot.Occupant.Owner != selectedCard.Owner)
                {
                    TryAttack(slot);
                }
                else
                {
                    TryMoveToSlot(selectedCard, slot);
                }
                return;
            }
        }

        public void ExecuteSceneCommand(SceneCommandType command)
        {
            if (isResolvingEvents && command != SceneCommandType.Restart && command != SceneCommandType.EndTurn)
            {
                SetStatus("Wait for the current action to finish.");
                return;
            }

            switch (command)
            {
                case SceneCommandType.StartMatch:
                    if (phase == GamePhase.DeckBuilder)
                    {
                        StartNewMatch();
                    }
                    else
                    {
                        SetStatus("Start Match is only available from a valid deck setup.");
                    }
                    break;

                case SceneCommandType.KeepHand:
                    if (phase == GamePhase.Mulligan)
                    {
                        KeepOpeningHand();
                    }
                    else
                    {
                        SetStatus("Keep Hand is only available during mulligan.");
                    }
                    break;

                case SceneCommandType.Mulligan:
                    if (phase == GamePhase.Mulligan && !mulliganUsed)
                    {
                        MulliganOpeningHand();
                    }
                    else
                    {
                        SetStatus("Mulligan is not available now.");
                    }
                    break;

                case SceneCommandType.EndTurn:
                    if (phase == GamePhase.PlayerTurn && activeSide == PlayerSide.Player)
                    {
                        EndPlayerTurn();
                    }
                    else
                    {
                        SetStatus("End Turn is only available during your turn.");
                    }
                    break;

                case SceneCommandType.Restart:
                    RestartGame();
                    break;

                case SceneCommandType.StrikeBoard:
                    if (board != null)
                    {
                        board.TriggerStrike(2, SlotZone.Frontline);
                    }
                    break;

                case SceneCommandType.SelectDeck:
                    SelectDeckFromScene(DeckArchetype.Endfield);
                    break;
            }
        }

        private void OnGUI()
        {
            HandleCardPointerGuiFallback();
            DrawPlayerHandGuiHitAreas();
            DrawActionPromptHud();
            DrawCenterInspectDetailHud();
            DrawSceneCommandHitAreas();
        }

        private void DrawPlayerHandGuiHitAreas()
        {
            return;
#pragma warning disable CS0162
            if (player == null
                || player.Hand == null
                || player.Hand.Count == 0
                || activeSide != PlayerSide.Player
                || (phase != GamePhase.PlayerTurn && phase != GamePhase.Mulligan)
                || Event.current == null
                || Event.current.type != EventType.MouseUp
                || Event.current.button != 0
                || pointerFallbackActive
                || lastSceneCommandPointerFrame == Time.frameCount)
            {
                return;
            }

            if (TryHandlePlayerHandBandGuiClick(Screen.width, Screen.height)
                || TryHandlePlayerHandBandGuiClick(1280f, 720f)
                || TryHandlePlayerHandBandGuiClick(1920f, 1080f))
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            for (int i = player.Hand.Count - 1; i >= 0; i--)
            {
                RuntimeCard card = player.Hand[i];
                CardView view = FindView(card);
                if (card == null || view == null || view == centerInspectView)
                {
                    continue;
                }

                Rect hitRect = PlayerHandCardGuiRect(mainCamera, view);
                if (hitRect.width <= 1f || hitRect.height <= 1f)
                {
                    continue;
                }

                if (hitRect.Contains(Event.current.mousePosition))
                {
                    HandlePointerClickRelease(view);
                    lastSceneCommandPointerFrame = Time.frameCount;
                    Event.current?.Use();
                    return;
                }
            }
        }

        private bool TryHandlePlayerHandBandGuiClick(float coordinateWidth, float coordinateHeight)
        {
            if (coordinateWidth <= 1f || coordinateHeight <= 1f)
            {
                return false;
            }

            Rect handBand = new Rect(
                coordinateWidth * 0.16f,
                coordinateHeight * 0.50f,
                coordinateWidth * 0.68f,
                coordinateHeight * 0.42f);
            Vector2 pointer = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (!handBand.Contains(pointer))
            {
                return false;
            }
            if (!TryFindPlayerHandCardByScreenIndex(pointer, coordinateWidth, coordinateHeight, out CardView view) || view == null)
            {
                return false;
            }

            HandlePointerClickRelease(view);
            lastSceneCommandPointerFrame = Time.frameCount;
            Event.current?.Use();
            return true;
#pragma warning restore CS0162
        }

        private void HandleUnifiedPlayerHandPointerInput()
        {
            if (isResolvingEvents
                || player == null
                || player.Hand == null
                || activeSide != PlayerSide.Player
                || (phase != GamePhase.PlayerTurn && phase != GamePhase.Mulligan))
            {
                unifiedHandPointerView = null;
                unifiedHandPointerDragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                CardView view = FindPointerPlayerHandCard();
                if (view == null || view.Card == null)
                {
                    unifiedHandPointerView = null;
                    unifiedHandPointerDragging = false;
                    return;
                }

                unifiedHandPointerView = view;
                unifiedHandPointerDownScreenPosition = Input.mousePosition;
                unifiedHandPointerDownTime = Time.time;
                unifiedHandPointerDragging = false;
                view.BeginPointerInteraction(Input.mousePosition, Time.time);
                lastSceneCommandPointerFrame = Time.frameCount;
                return;
            }

            if (unifiedHandPointerView == null)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                if (Vector3.Distance(Input.mousePosition, unifiedHandPointerDownScreenPosition) >= PointerDragStartThresholdPixels
                    && Time.time - unifiedHandPointerDownTime >= PointerDragStartHoldSeconds)
                {
                    unifiedHandPointerDragging = true;
                    unifiedHandPointerView.DragPointerInteraction();
                    lastSceneCommandPointerFrame = Time.frameCount;
                }

                return;
            }

            if (!Input.GetMouseButtonUp(0))
            {
                return;
            }

            CardView releasedView = unifiedHandPointerView;
            bool wasDragging = unifiedHandPointerDragging;
            unifiedHandPointerView = null;
            unifiedHandPointerDragging = false;
            lastSceneCommandPointerFrame = Time.frameCount;

            if (releasedView == null || releasedView.Card == null)
            {
                return;
            }

            if (wasDragging)
            {
                releasedView.EndPointerInteraction();
                return;
            }

            releasedView.CancelPointerInteraction();
            HandlePointerClickRelease(releasedView);
        }

        private CardView FindPointerPlayerHandCard()
        {
            Vector2 guiPointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if ((TryFindPlayerHandCardByScreenIndex(guiPointer, Screen.width, Screen.height, out CardView indexedView)
                    || TryFindPlayerHandCardByScreenIndex(guiPointer, 1280f, 720f, out indexedView)
                    || TryFindPlayerHandCardByScreenIndex(guiPointer, 1920f, 1080f, out indexedView))
                && indexedView != null
                && indexedView.Card != null
                && indexedView.Card.Owner == PlayerSide.Player
                && indexedView.Card.Zone == CardZone.Hand)
            {
                return indexedView;
            }

            Camera mainCamera = Camera.main;
            CardView raycastView = CardView.RaycastPointerCard(mainCamera, Input.mousePosition);
            if (raycastView == null)
            {
                raycastView = FindProjectedPointerCard(mainCamera, Input.mousePosition);
            }

            return raycastView != null
                && raycastView.Card != null
                && raycastView.Card.Owner == PlayerSide.Player
                && raycastView.Card.Zone == CardZone.Hand
                    ? raycastView
                    : null;
        }

        private static Rect PlayerHandCardGuiRect(Camera mainCamera, CardView view)
        {
            Vector3 center = mainCamera.WorldToScreenPoint(view.transform.position);
            if (center.z < 0f)
            {
                return Rect.zero;
            }

            float cardWidth = Mathf.Max(110f, Screen.width * 0.075f);
            float cardHeight = Mathf.Max(210f, Screen.height * 0.20f);
            float guiX = center.x - cardWidth * 0.5f;
            float guiY = Screen.height - center.y - cardHeight * 0.5f;
            return new Rect(guiX, guiY, cardWidth, cardHeight);
        }

        private void HandleCardPointerGuiFallback()
        {
            return;
#pragma warning disable CS0162
            Event currentEvent = Event.current;
            if (currentEvent == null
                || currentEvent.type != EventType.MouseUp
                || currentEvent.button != 0
                || pointerFallbackActive
                || lastInspectClickHandledFrame == Time.frameCount)
            {
                return;
            }

            CardView clickedView = FindGuiPointerCard(currentEvent.mousePosition);
            if (clickedView == null
                || clickedView.Card == null
                || clickedView.Card.Owner != PlayerSide.Player
                || clickedView.Card.Zone != CardZone.Hand)
            {
                return;
            }

            if (MulliganRules.CanMarkForDiscard(phase, activeSide, clickedView.Card))
            {
                HandleCardClicked(clickedView);
            }
            else
            {
                HandleCardInspectRequested(clickedView);
            }
            currentEvent.Use();
#pragma warning restore CS0162
        }

        private void CancelPendingAirborneDeployment()
        {
            if (pendingAirborneOrder == null && pendingAirborneSlot == null)
            {
                return;
            }

            RuntimeCard returningOrder = pendingAirborneOrder;
            if (returningOrder != null && player.Hand.Contains(returningOrder))
            {
                StartCoroutine(ReturnPendingOrderDisplayToHand(returningOrder));
                return;
            }

            pendingAirborneOrder = null;
            pendingAirborneUnit = null;
            pendingAirborneSlot = null;
            ClearSelection();
            ClearDragPreview();
            SetStatus("AIRBORNE cancelled.");
            RefreshAllViews();
        }

        private System.Collections.IEnumerator ReturnPendingOrderDisplayToHand(RuntimeCard order)
        {
            CardView returningView = centerInspectView;
            int index = Mathf.Max(0, player.Hand.IndexOf(order));
            CardView handView = FindView(order);
            Vector3 returnPosition = handView != null
                ? handView.transform.position
                : HandPosition(PlayerSide.Player, index, player.Hand.Count);
            if (returningView == null)
            {
                returningView = CreateTransientCardView(order);
                if (returningView == null)
                {
                    yield break;
                }

                returningView.SetLayout(
                    PlayableSceneRules.OrderDisplayAnchor,
                    new Vector3(PlayableSceneRules.OrderDisplayScale, 1f, PlayableSceneRules.OrderDisplayScale),
                    Quaternion.identity,
                    false);
                returningView.SetDetailPresentation();
            }

            if (returningView != null)
            {
                returningView.SetInteractionEnabled(false);
                returningView.PlayDeployDrop(returningView.transform.position, returnPosition);
            }

            pendingAirborneOrder = null;
            pendingAirborneUnit = null;
            pendingAirborneSlot = null;
            ClearSelection();
            ClearDragPreview();
            SetStatus("AIRBORNE cancelled.");
            yield return new WaitForSeconds(CardMotionRules.DeployDropSeconds);
            centerInspectCard = null;
            inspectedCard = null;
            if (returningView != null && returningView != centerInspectView)
            {
                RuntimeSafeDestroy.Destroy(returningView.gameObject);
            }
            DestroyCenterInspectView();
            RefreshAllViews();
        }

        private void DrawCenterInspectDetailHud()
        {
            if (centerInspectCard == null)
            {
                return;
            }

            Rect inspectRect = CenterInspectScreenRect();
            const float gap = 18f;
            const float margin = 8f;
            float uiScale = InspectHudScale();
            float availableLeftWidth = Mathf.Max(96f * uiScale, inspectRect.xMin - gap * uiScale - margin * uiScale);
            float width = Mathf.Min(520f * uiScale, Screen.width * 0.30f, availableLeftWidth);
            float height = Mathf.Min(430f * uiScale, Screen.height * 0.48f);
            Rect panelRect = new Rect(inspectRect.xMin - width - gap, inspectRect.yMin, width, height);
            if (panelRect.xMin < margin * uiScale)
            {
                panelRect.x = margin * uiScale;
                panelRect.width = Mathf.Max(96f * uiScale, inspectRect.xMin - gap * uiScale - margin * uiScale);
            }
            Color previousColor = GUI.color;
            GUI.color = new Color(0.035f, 0.032f, 0.026f, 0.94f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = previousColor;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * uiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.88f, 0.52f, 1f) },
                wordWrap = true
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15f * uiScale),
                normal = { textColor = new Color(0.96f, 0.92f, 0.80f, 1f) },
                wordWrap = true
            };

            Rect titleRect = new Rect(panelRect.x + 14f * uiScale, panelRect.y + 10f * uiScale, panelRect.width - 28f * uiScale, 32f * uiScale);
            GUI.Label(titleRect, CardTextRules.DisplayCardName(centerInspectCard), titleStyle);

            Rect bodyRect = new Rect(panelRect.x + 14f * uiScale, panelRect.y + 48f * uiScale, panelRect.width - 28f * uiScale, panelRect.height - 58f * uiScale);
            GUI.Label(bodyRect, BuildInspectHudText(centerInspectCard), bodyStyle);
        }

        private Rect CenterInspectScreenRect()
        {
            float cardHeight = Mathf.Clamp(Screen.height * 1.10f, 560f, Screen.height * 1.20f);
            float cardWidth = cardHeight * PlayableSceneRules.HandCardAspectRatio;
            float x = Mathf.Clamp(Screen.width * 0.40f - cardWidth * 0.5f, 48f * InspectHudScale(), Screen.width * 0.54f - cardWidth);
            float y = Screen.height * 0.045f;
            return new Rect(x, y, cardWidth, cardHeight);
        }

        private static float InspectHudScale()
        {
            return Mathf.Clamp(Screen.height / 540f, 1.35f, 2.75f);
        }

        private static Color CardHudFrameColor(RuntimeCard card)
        {
            if (card == null)
            {
                return new Color(0.72f, 0.66f, 0.50f, 1f);
            }

            switch (card.Type)
            {
                case CardType.Order:
                    return new Color(0.30f, 0.25f, 0.52f, 1f);
                case CardType.Countermeasure:
                    return new Color(0.48f, 0.24f, 0.54f, 1f);
                default:
                    return new Color(0.72f, 0.66f, 0.50f, 1f);
            }
        }

        private static Texture2D InspectArtworkTexture(RuntimeCard card)
        {
            if (card == null)
            {
                return null;
            }

            return CardView.ResolveArtworkTexture(card, false);
        }

        private string BuildInspectHudText(RuntimeCard card)
        {
            string text = TypeLabelForInspectHud(card.Type);
            text += $"\n部署: {DisplayKreditCostFor(card)}K";
            if (card.Type == CardType.Unit)
            {
                text += $"    行动: {DisplayOperationCostFor(card)}K";
                text += $"\n攻击: {card.Attack}    防御: {card.CurrentDefense}/{card.Defense}";
            }

            string keywords = KeywordLineForInspectHud(card);
            if (!string.IsNullOrEmpty(keywords))
            {
                text += $"\n\n词条: {keywords}";
            }

            if (!string.IsNullOrWhiteSpace(card.RulesText))
            {
                text += $"\n\n特殊能力:\n{card.RulesText}";
            }

            return text;
        }

        private static string TypeLabelForInspectHud(CardType type)
        {
            switch (type)
            {
                case CardType.Unit:
                    return "单位";
                case CardType.Order:
                    return "指令";
                case CardType.Countermeasure:
                    return "反制";
                default:
                    return "卡牌";
            }
        }

        private static string KeywordLineForInspectHud(RuntimeCard card)
        {
            if (card == null || card.Keywords == CardKeyword.None)
            {
                return string.Empty;
            }

            string text = string.Empty;
            AppendKeywordForInspectHud(card, CardKeyword.Blitz, "闪击", ref text);
            AppendKeywordForInspectHud(card, CardKeyword.Guard, "守护", ref text);
            AppendKeywordForInspectHud(card, CardKeyword.Smokescreen, "烟幕", ref text);
            AppendKeywordForInspectHud(card, CardKeyword.Ambush, "伏击", ref text);
            AppendKeywordForInspectHud(card, CardKeyword.Fury, "狂怒", ref text);
            return text;
        }

        private static void AppendKeywordForInspectHud(RuntimeCard card, CardKeyword keyword, string label, ref string text)
        {
            if (!card.HasKeyword(keyword))
            {
                return;
            }

            text = string.IsNullOrEmpty(text) ? label : $"{text} / {label}";
        }

        private CardView FindGuiPointerCard(Vector2 guiPosition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            Vector3 screenPointer = new Vector3(guiPosition.x, Screen.height - guiPosition.y, 0f);
            CardView indexedHandView = FindPlayerHandCardByScreenIndex(guiPosition);
            if (indexedHandView != null)
            {
                return indexedHandView;
            }

            CardView handBandView = FindPlayerHandCardInScreenBand(mainCamera, screenPointer);
            if (handBandView != null)
            {
                return handBandView;
            }

            CardView nearestHandView = FindNearestPlayerHandCard(mainCamera, screenPointer);
            if (nearestHandView != null)
            {
                return nearestHandView;
            }

            CardView bestView = null;
            float bestDistance = float.MaxValue;
            float distance;
            foreach (CardView view in cardViews)
            {
                if (view == null || !view.TryPointerRaycastDistance(mainCamera, screenPointer, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            if (bestView == null)
            {
                foreach (CardView view in cardViews)
                {
                    if (view == null || !view.TryPointerProjectedDistance(mainCamera, screenPointer, out distance))
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestView = view;
                    }
                }
            }

            return bestView;
        }

        private CardView FindProjectedPointerCard(Camera mainCamera, Vector3 screenPointer)
        {
            if (mainCamera == null)
            {
                return null;
            }

            CardView bestView = null;
            float bestDistance = float.MaxValue;
            float distance;
            foreach (CardView view in cardViews)
            {
                if (view == null || !view.TryPointerProjectedDistance(mainCamera, screenPointer, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            return bestView;
        }

        private void ExecuteDefaultSceneCommand()
        {
            if (phase == GamePhase.DeckBuilder)
            {
                ExecuteSceneCommand(SceneCommandType.StartMatch);
            }
            else if (phase == GamePhase.Mulligan)
            {
                ExecuteSceneCommand(SceneCommandType.KeepHand);
            }
            else if (phase == GamePhase.PlayerTurn)
            {
                ExecuteSceneCommand(SceneCommandType.EndTurn);
            }
            else if (phase == GamePhase.GameOver)
            {
                ExecuteSceneCommand(SceneCommandType.Restart);
            }
        }

        private void HandleCardPointerFallbackInput()
        {
            if (lastInspectClickHandledFrame == Time.frameCount)
            {
                pointerFallbackActive = false;
                pointerFallbackCard = null;
                pointerPressedCard = null;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                CardView pressedView = FindPointerCardFallback();
                if (pressedView == null)
                {
                    pressedView = hoveredCardView;
                }

                pointerFallbackCard = null;
                pointerFallbackActive = false;
                pointerPressedCard = pressedView;
                pointerPressedScreenPosition = Input.mousePosition;
                pointerPressedTime = Time.time;
            }

            if (Input.GetMouseButton(0) && pointerPressedCard != null && pointerPressedCard != centerInspectView)
            {
                if (!pointerFallbackActive && ShouldStartPointerDrag())
                {
                    if (pointerPressedCard.BeginPointerInteraction(pointerPressedScreenPosition, pointerPressedTime))
                    {
                        pointerFallbackCard = pointerPressedCard;
                        pointerFallbackActive = true;
                    }
                }

                if (pointerFallbackActive && pointerFallbackCard != null && pointerFallbackCard.DragPointerInteraction())
                {
                    lastSceneCommandPointerFrame = Time.frameCount;
                }
            }

            if (!Input.GetMouseButtonUp(0))
            {
                return;
            }

            if (!pointerFallbackActive || pointerFallbackCard == null)
            {
                CardView releaseClickCard = null;
                if (IsPointerReleaseClick(pointerPressedCard))
                {
                    releaseClickCard = pointerPressedCard;
                }
                else if (pointerPressedCard != null && Vector3.Distance(Input.mousePosition, pointerPressedScreenPosition) < PointerDragStartThresholdPixels)
                {
                    releaseClickCard = pointerPressedCard;
                }
                else if (pointerPressedCard == null)
                {
                    releaseClickCard = FindPointerCardFallback();
                    if (releaseClickCard == null)
                    {
                        releaseClickCard = hoveredCardView;
                    }
                }

                if (releaseClickCard != null)
                {
                    HandlePointerClickRelease(releaseClickCard);
                    lastSceneCommandPointerFrame = Time.frameCount;
                }

                pointerPressedCard = null;

                return;
            }

            if (IsPointerReleaseClick(pointerFallbackCard))
            {
                CardView clickedView = pointerFallbackCard;
                pointerFallbackCard.CancelPointerInteraction();
                HandlePointerClickRelease(clickedView);
                lastSceneCommandPointerFrame = Time.frameCount;
                pointerFallbackActive = false;
                pointerFallbackCard = null;
                pointerPressedCard = null;
                return;
            }

            pointerFallbackCard.EndPointerInteraction();

            lastSceneCommandPointerFrame = Time.frameCount;
            pointerFallbackActive = false;
            pointerFallbackCard = null;
            pointerPressedCard = null;
        }

        private void HandleHoveredCardClickShortcut()
        {
            return;
#pragma warning disable CS0162
            if (!Input.GetMouseButtonUp(0)
                || pointerFallbackActive
                || hoveredCardView == null
                || hoveredCardView.Card == null
                || hoveredCardView.Card.Owner != PlayerSide.Player
                || hoveredCardView.Card.Zone != CardZone.Hand)
            {
                return;
            }

            HandlePointerClickRelease(hoveredCardView);
            lastSceneCommandPointerFrame = Time.frameCount;
#pragma warning restore CS0162
        }

        private void HandlePointerClickRelease(CardView clickedView)
        {
            if (clickedView == null || clickedView.Card == null)
            {
                return;
            }

            if (phase == GamePhase.Mulligan && clickedView.Card.Owner == PlayerSide.Player && clickedView.Card.Zone == CardZone.Hand)
            {
                HandleCardClicked(clickedView);
                return;
            }

            if (clickedView == centerInspectView
                || (clickedView.Card.Owner == PlayerSide.Player && clickedView.Card.Zone == CardZone.Hand))
            {
                HandleCardInspectRequested(clickedView);
                return;
            }

            HandleCardClicked(clickedView);
        }

        private bool IsPointerReleaseClick(CardView view)
        {
            return view != null
                && pointerPressedCard == view
                && (!pointerFallbackActive || Vector3.Distance(Input.mousePosition, pointerPressedScreenPosition) < PointerDragStartThresholdPixels);
        }

        private bool ShouldStartPointerDrag()
        {
            return Vector3.Distance(Input.mousePosition, pointerPressedScreenPosition) >= PointerDragStartThresholdPixels
                && Time.time - pointerPressedTime >= PointerDragStartHoldSeconds;
        }

        private CardView FindPointerCardFallback()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            CardView handView = FindPlayerHandCardByScreenIndex(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            if (handView != null)
            {
                return handView;
            }

            handView = FindPlayerHandCardUnderPointer(mainCamera, Input.mousePosition);
            if (handView != null)
            {
                return handView;
            }

            handView = FindPlayerHandCardInScreenBand(mainCamera, Input.mousePosition);
            if (handView != null)
            {
                return handView;
            }

            handView = FindNearestPlayerHandCard(mainCamera, Input.mousePosition);
            if (handView != null)
            {
                return handView;
            }

            if (centerInspectView != null)
            {
                float centerInspectDistance;
                if (centerInspectView.TryPointerProjectedDistance(mainCamera, Input.mousePosition, out centerInspectDistance))
                {
                    return centerInspectView;
                }
            }
            CardView bestView = null;
            float bestDistance = float.MaxValue;
            float distance;
            foreach (CardView view in cardViews)
            {
                if (view == null || !view.TryPointerScreenDistance(mainCamera, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            if (bestView != null)
            {
                return bestView;
            }

            foreach (CardView view in cardViews)
            {
                if (view == null || !view.TryPointerProjectedDistance(mainCamera, Input.mousePosition, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            if (bestView != null)
            {
                return bestView;
            }

            EnsureSceneCommandButtonsCached();

            foreach (SceneCommandButton button in sceneCommandButtons)
            {
                if (button == null)
                {
                    continue;
                }

                bool available = IsSceneCommandAvailable(button.Command);
                float commandDistance;
                if (IsSceneCommandVisible(button.Command, available)
                    && TryPointerSceneCommandDistance(button, mainCamera, out commandDistance))
                {
                    return null;
                }
            }

            return null;
        }

        private CardView FindPlayerHandCardUnderPointer(Camera mainCamera, Vector3 screenPointer)
        {
            if (mainCamera == null)
            {
                return null;
            }

            for (int i = cardViews.Count - 1; i >= 0; i--)
            {
                CardView view = cardViews[i];
                RuntimeCard viewCard = view != null ? view.Card : null;
                if (viewCard == null || IsCenterInspectCard(viewCard) || viewCard.Owner != PlayerSide.Player || viewCard.Zone != CardZone.Hand)
                {
                    continue;
                }

                float unusedDistance;
                if (view.TryPointerProjectedDistance(mainCamera, screenPointer, out unusedDistance))
                {
                    return view;
                }
            }

            return null;
        }

        private CardView FindNearestPlayerHandCard(Camera mainCamera, Vector3 screenPointer)
        {
            if (mainCamera == null || !CanUsePlayerHandPointerFallback())
            {
                return null;
            }

            CardView bestView = null;
            float bestDistance = float.MaxValue;
            foreach (CardView view in cardViews)
            {
                RuntimeCard viewCard = view != null ? view.Card : null;
                if (viewCard == null || IsCenterInspectCard(viewCard) || viewCard.Owner != PlayerSide.Player || viewCard.Zone != CardZone.Hand)
                {
                    continue;
                }

                Vector3 centerScreen = mainCamera.WorldToScreenPoint(view.transform.position);
                if (centerScreen.z < 0f)
                {
                    continue;
                }

                float distance = Vector2.Distance(
                    new Vector2(screenPointer.x, screenPointer.y),
                    new Vector2(centerScreen.x, centerScreen.y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            const float nearestHandCardMaxDistancePixels = 420f;
            return bestDistance <= nearestHandCardMaxDistancePixels ? bestView : null;
        }

        private CardView FindPlayerHandCardInScreenBand(Camera mainCamera, Vector3 screenPointer)
        {
            if (mainCamera == null || !CanUsePlayerHandPointerFallback())
            {
                return null;
            }

            float minHandY = float.MaxValue;
            float maxHandY = float.MinValue;
            float minHandX = float.MaxValue;
            float maxHandX = float.MinValue;
            foreach (CardView view in cardViews)
            {
                RuntimeCard viewCard = view != null ? view.Card : null;
                if (viewCard == null || IsCenterInspectCard(viewCard) || viewCard.Owner != PlayerSide.Player || viewCard.Zone != CardZone.Hand)
                {
                    continue;
                }

                Vector3 centerScreen = mainCamera.WorldToScreenPoint(view.transform.position);
                if (centerScreen.z < 0f)
                {
                    continue;
                }

                minHandY = Mathf.Min(minHandY, centerScreen.y);
                maxHandY = Mathf.Max(maxHandY, centerScreen.y);
                minHandX = Mathf.Min(minHandX, centerScreen.x);
                maxHandX = Mathf.Max(maxHandX, centerScreen.x);
            }

            if (minHandY == float.MaxValue)
            {
                return null;
            }

            const float handBandVerticalPaddingPixels = 210f;
            const float handBandHorizontalPaddingPixels = 170f;
            bool insideHandBand = screenPointer.y >= minHandY - handBandVerticalPaddingPixels
                && screenPointer.y <= maxHandY + handBandVerticalPaddingPixels
                && screenPointer.x >= minHandX - handBandHorizontalPaddingPixels
                && screenPointer.x <= maxHandX + handBandHorizontalPaddingPixels;
            if (!insideHandBand)
            {
                return null;
            }

            CardView bestView = null;
            float bestDistance = float.MaxValue;
            foreach (CardView view in cardViews)
            {
                RuntimeCard viewCard = view != null ? view.Card : null;
                if (viewCard == null || IsCenterInspectCard(viewCard) || viewCard.Owner != PlayerSide.Player || viewCard.Zone != CardZone.Hand)
                {
                    continue;
                }

                Vector3 centerScreen = mainCamera.WorldToScreenPoint(view.transform.position);
                if (centerScreen.z < 0f)
                {
                    continue;
                }

                float distance = Mathf.Abs(screenPointer.x - centerScreen.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestView = view;
                }
            }

            return bestView;
        }

        private CardView FindPlayerHandCardByScreenIndex(Vector2 guiPosition)
        {
            if (!CanUsePlayerHandPointerFallback()
                || player.Hand == null
                || player.Hand.Count == 0
                || (phase != GamePhase.PlayerTurn && phase != GamePhase.Mulligan)
                || activeSide != PlayerSide.Player)
            {
                return null;
            }

            if (TryFindPlayerHandCardByScreenIndex(guiPosition, Screen.width, Screen.height, out CardView screenView))
            {
                return screenView;
            }

            if (TryFindPlayerHandCardByScreenIndex(guiPosition, 1280f, 720f, out CardView editorHdView))
            {
                return editorHdView;
            }

            if (TryFindPlayerHandCardByScreenIndex(guiPosition, 1920f, 1080f, out CardView editorFullHdView))
            {
                return editorFullHdView;
            }

            return null;
        }

        private bool TryFindPlayerHandCardByScreenIndex(Vector2 guiPosition, float coordinateWidth, float coordinateHeight, out CardView view)
        {
            view = null;
            if (coordinateWidth <= 1f || coordinateHeight <= 1f)
            {
                return false;
            }

            float normalizedY = guiPosition.y / coordinateHeight;
            if (normalizedY < 0.55f || normalizedY > 0.92f)
            {
                return false;
            }

            float handWidth = Mathf.Min(coordinateWidth * 0.54f, coordinateWidth <= 1400f ? 650f : 1180f);
            float handLeft = coordinateWidth * 0.5f - handWidth * 0.5f;
            float handRight = coordinateWidth * 0.5f + handWidth * 0.5f;
            if (guiPosition.x < handLeft || guiPosition.x > handRight)
            {
                return false;
            }

            float normalizedX = Mathf.InverseLerp(handLeft, handRight, guiPosition.x);
            int index = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (player.Hand.Count - 1)), 0, player.Hand.Count - 1);
            RuntimeCard card = player.Hand[index];
            view = FindView(card);
            return view != null;
        }

        private bool CanUsePlayerHandPointerFallback()
        {
            return activeSide == PlayerSide.Player
                && (phase == GamePhase.PlayerTurn || phase == GamePhase.Mulligan);
        }

        private bool IsCenterInspectCard(RuntimeCard card)
        {
            return card != null && centerInspectCard != null && card.Id == centerInspectCard.Id;
        }

        private void HandleSceneCommandPointerInput()
        {
            if (!Input.GetMouseButtonUp(0) || lastSceneCommandPointerFrame == Time.frameCount)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            EnsureSceneCommandButtonsCached();

            SceneCommandButton bestButton = null;
            float bestDistance = float.MaxValue;
            foreach (SceneCommandButton button in sceneCommandButtons)
            {
                if (button == null)
                {
                    continue;
                }

                bool available = IsSceneCommandAvailable(button.Command);
                if (!IsSceneCommandVisible(button.Command, available))
                {
                    continue;
                }

                float distance;
                if (!TryPointerSceneCommandDistance(button, mainCamera, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestButton = button;
                }
            }

            if (bestButton != null)
            {
                lastSceneCommandPointerFrame = Time.frameCount;
                ExecuteSceneCommand(bestButton.Command);
            }
        }

        private bool TryPointerSceneCommandDistance(SceneCommandButton button, Camera mainCamera, out float distance)
        {
            distance = float.MaxValue;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(button.transform.position);
            if (screenPosition.z < 0f)
            {
                return false;
            }

            float pixelsPerWorldUnit = Screen.height / (mainCamera.orthographicSize * 2f);
            float halfWidth = PlayableSceneRules.CommandButtonPlateSize.x * pixelsPerWorldUnit * 0.64f;
            float halfHeight = PlayableSceneRules.CommandButtonPlateSize.y * pixelsPerWorldUnit * 0.48f;
            Vector3 pointer = Input.mousePosition;
            float deltaX = pointer.x - screenPosition.x;
            float deltaY = pointer.y - screenPosition.y;
            if (Mathf.Abs(deltaX) > halfWidth || Mathf.Abs(deltaY) > halfHeight)
            {
                return false;
            }

            distance = deltaX * deltaX + deltaY * deltaY;
            return true;
        }

        private SlotInteract ResolvePointerSlot(Vector3 worldPosition, RuntimeCard attacker)
        {
            SlotInteract slot = board.GetSlot(worldPosition);
            if (attacker == null || !IsBoardCombatUnit(attacker) || board == null)
            {
                return slot;
            }

            PlayerSide defenderSide = GetOpponentState(attacker.Owner).Side;
            SlotInteract headquartersSlot = board.GetHeadquartersSlot(defenderSide);
            if (headquartersSlot == null || !IsLegalAttackTarget(attacker, headquartersSlot))
            {
                return slot;
            }

            float headquartersDistance = Vector3.Distance(worldPosition, headquartersSlot.transform.position);
            if (headquartersDistance > BoardTargetRules.HeadquartersTargetRadius)
            {
                return slot;
            }

            if (slot == null)
            {
                return headquartersSlot;
            }

            float slotDistance = Vector3.Distance(worldPosition, slot.transform.position);
            return headquartersDistance + BoardTargetRules.HeadquartersTargetBias <= slotDistance
                ? headquartersSlot
                : slot;
        }

        private SlotInteract ResolvePointerOrderSlot(Vector3 worldPosition, RuntimeCard order)
        {
            SlotInteract slot = board.GetSlot(worldPosition);
            if (order == null || order.Type != CardType.Order || board == null || order.EffectType != CardEffectType.DamageTargetUnitAndAdjacent)
            {
                return slot;
            }

            SlotInteract headquartersSlot = board.GetHeadquartersSlot(GetOpponentState(PlayerSide.Player).Side);
            if (headquartersSlot == null || !IsLegalOrderTarget(order, headquartersSlot, PlayerSide.Player))
            {
                return slot;
            }

            float headquartersDistance = Vector3.Distance(worldPosition, headquartersSlot.transform.position);
            if (headquartersDistance > BoardTargetRules.HeadquartersTargetRadius)
            {
                return slot;
            }

            if (slot == null)
            {
                return headquartersSlot;
            }

            float slotDistance = Vector3.Distance(worldPosition, slot.transform.position);
            return headquartersDistance + BoardTargetRules.HeadquartersTargetBias <= slotDistance
                ? headquartersSlot
                : slot;
        }
}
