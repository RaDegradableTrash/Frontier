using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
        private void SetStatus(string message)
        {
            status = message;
            if (!string.IsNullOrEmpty(message))
            {
                actionLog.Insert(0, message);
                if (actionLog.Count > 8)
                {
                    actionLog.RemoveAt(actionLog.Count - 1);
                }
            }

            RefreshSceneStatus();
            RefreshSceneCommandButtons();
        }

        private Rect SceneCommandGuiRect(SceneCommandButton button, Camera mainCamera)
        {
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(button.transform.position);
            float pixelsPerWorldUnit = Screen.height / (mainCamera.orthographicSize * 2f);
            float width = PlayableSceneRules.CommandButtonPlateSize.x * pixelsPerWorldUnit * 1.05f;
            float height = PlayableSceneRules.CommandButtonPlateSize.y * pixelsPerWorldUnit * 0.9f;
            return new Rect(
                screenPosition.x - width * 0.5f,
                Screen.height - screenPosition.y - height * 0.5f,
                width,
                height);
        }

    private void SpawnFloatingText(string text, Vector3 position, Color color)
        {
            SpawnFloatingText(text, position, color, CueTypeForText(text));
        }

        private void SpawnFloatingText(string text, Vector3 position, Color color, FeedbackCueType cueType)
        {
            resolutionEvents.Enqueue(new ResolutionEvent(text, position, color, cueType));
            if (!isResolvingEvents)
            {
                StartCoroutine(ProcessResolutionEvents());
            }
        }

        private System.Collections.IEnumerator ProcessResolutionEvents()
        {
            isResolvingEvents = true;
            RefreshSceneCommandButtons();
            while (resolutionEvents.Count > 0)
            {
                ResolutionEvent resolutionEvent = resolutionEvents.Dequeue();
                CreateFloatingTextNow(resolutionEvent.Text, resolutionEvent.Position, resolutionEvent.Color);
                feedbackManager?.PlayCue(resolutionEvent.CueType, resolutionEvent.Position);
                yield return new WaitForSeconds(resolutionEvent.Delay);
            }

            isResolvingEvents = false;
            RefreshSceneCommandButtons();
        }

        private void CreateFloatingTextNow(string text, Vector3 position, Color color)
        {
            GameObject marker = new GameObject($"FloatingText_{text}");
            marker.transform.position = position + Vector3.up * 0.45f;
            FloatingText floatingText = marker.AddComponent<FloatingText>();
            floatingText.Initialize(text, color);
        }

        private void SpawnAttackTracer(Vector3 start, Vector3 end, Color color)
        {
            GameObject tracerObject = new GameObject("AttackTracer");
            AttackTracer tracer = tracerObject.AddComponent<AttackTracer>();
            tracer.Initialize(start, end, color);
        }

        private void PlayAttackLunge(RuntimeCard attacker, Vector3 target)
        {
            CardView view = FindView(attacker);
            view?.PlayAttackLunge(target);
        }


        private FeedbackCueType CueTypeForText(string text)
        {
            if (text.Contains("DEPLOY")) return FeedbackCueType.Deploy;
            if (text.Contains("ADVANCE")) return FeedbackCueType.Advance;
            if (text.Contains("COUNTER")) return FeedbackCueType.Countermeasure;
            if (text.Contains("PINNED")) return FeedbackCueType.Pin;
            if (text.Contains("+")) return FeedbackCueType.Heal;
            if (text.Contains("-")) return FeedbackCueType.Damage;
            return FeedbackCueType.Draw;
        }

        private void EnsureFeedbackManager()
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
            if (feedbackManager != null)
            {
                return;
            }

            GameObject feedbackObject = new GameObject("FeedbackManager");
            feedbackManager = feedbackObject.AddComponent<FeedbackManager>();
        }

        private void EnsureBoard()
        {
            EnsurePlayablePresentation();
            if (board == null)
            {
                board = FindObjectOfType<BoardManager>();
            }

            if (board == null)
            {
                GameObject boardObject = new GameObject("Board");
                board = boardObject.AddComponent<BoardManager>();
            }

            if (cameraInteraction == null)
            {
                cameraInteraction = FindObjectOfType<CameraInteraction>();
            }

            if (sceneCardLayout == null)
            {
                sceneCardLayout = FindObjectOfType<SceneCardLayout>();
            }
            sceneCardLayout?.ApplyPlayableDefaults();

            if (sceneStatusDisplay == null)
            {
                sceneStatusDisplay = FindObjectOfType<SceneStatusDisplay>();
            }

            if (sceneActionPrompt == null)
            {
                sceneActionPrompt = FindObjectOfType<SceneActionPrompt>();
            }

            if (sceneCardInspector == null)
            {
                sceneCardInspector = FindObjectOfType<SceneCardInspector>();
            }

            if (sceneDeckSummary == null)
            {
                sceneDeckSummary = FindObjectOfType<SceneDeckSummary>();
            }

            RefreshPileDisplays();
            RefreshKreditDisplays();
            RefreshSceneStatus();
            RefreshSceneActionPrompt();
            RefreshSceneInspector();
            RefreshSceneDeckSummary();
            RefreshSceneCommandButtons();
        }

        private void DrawActionPromptHud()
        {
            if (!PlayableSceneRules.ActionPromptHudEnabled)
            {
                return;
            }

            string prompt = SceneHudPromptRules.Prompt(
                selectedCard,
                player.Kredits,
                hasFrontlineController,
                frontlineController,
                phase,
                activeSide,
                mulliganUsed);
            Rect boxRect = new Rect(Screen.width * 0.5f - 180f, 12f, 360f, 28f);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            boxStyle.normal.textColor = new Color(1f, 0.92f, 0.55f, 1f);
            GUI.Box(boxRect, prompt, boxStyle);
        }

        private void HighlightLegalTargets(RuntimeCard card, bool highlighted)
        {
            if (highlighted && card.Zone == CardZone.Hand && !player.CanSpendKredits(HandPlayCost(player, card)))
            {
                return;
            }

            if (card.Zone == CardZone.Hand)
            {
                if (card.Type == CardType.Unit)
                {
                    HighlightEmptySlots(SlotZone.Frontline, highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.Frontline));
                }
                else if (card.Type == CardType.Order)
                {
                    HighlightOrderTargets(card, highlighted);
                }
                else if (card.Type == CardType.Countermeasure)
                {
                    HighlightAllSlots(highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.PlayerSupport));
                }
            }
            else if (IsBoardCombatUnit(card))
            {
                if (card.Owner == PlayerSide.Player
                    && !card.HasActed
                    && !card.HasKeyword(CardKeyword.Pinned)
                    && player.Kredits >= EffectiveOperationCostForAction(card))
                {
                    HighlightEmptySlots(SlotZone.Frontline, highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.Frontline));
                }

                if (UnitActionHighlightRules.ShouldHighlightAttackTargets(card, player.Kredits))
                {
                    HighlightAttackTargets(card, highlighted);
                }
            }
        }

        private void HighlightOrderTargets(RuntimeCard card, bool highlighted)
        {
            if (!OrderNeedsTarget(card))
            {
                HighlightAllSlots(highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.PlayerSupport));
                return;
            }

            HighlightMatchingSlots(slot => IsLegalOrderTarget(card, slot, PlayerSide.Player), highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.EnemySupport));
        }

        private void HighlightAttackTargets(RuntimeCard attacker, bool highlighted)
        {
            HighlightAttackTargets(attacker, SlotZone.Frontline, highlighted);
        }

        private void HighlightHeadquartersAttackTarget(RuntimeCard attacker, bool highlighted)
        {
            if (attacker == null || board == null)
            {
                return;
            }

            PlayerSide defenderSide = GetOpponentState(attacker.Owner).Side;
            SlotInteract headquartersSlot = board.GetHeadquartersSlot(defenderSide);
            if (headquartersSlot == null || !IsLegalAttackTarget(attacker, headquartersSlot))
            {
                return;
            }

            headquartersSlot.SetHighlighted(highlighted, SlotHighlightLabelRules.AttackLabelFor(null, false));
        }

        private void HighlightAttackTargets(RuntimeCard attacker, SlotZone zone, bool highlighted)
        {
            foreach (SlotInteract slot in AllBoardSlots())
            {
                if (slot != null && IsLegalAttackTarget(attacker, slot))
                {
                    bool defenderHasGuard = attacker != null && IsProtectedByAdjacentGuard(slot, GetOpponentState(attacker.Owner).Side);
                    string label = SlotHighlightLabelRules.AttackLabelFor(slot.Occupant, defenderHasGuard);
                    slot.SetHighlighted(highlighted, label);
                }
            }
        }

        private void HighlightMatchingSlots(SlotPredicate predicate, bool highlighted)
        {
            HighlightMatchingSlots(predicate, highlighted, PlayableSceneRules.HighlightedSlotLabel);
        }

        private void HighlightMatchingSlots(SlotPredicate predicate, bool highlighted, string label)
        {
            foreach (SlotInteract slot in AllBoardSlots())
            {
                if (slot != null && predicate(slot))
                {
                    slot.SetHighlighted(highlighted, label);
                }
            }
        }

        private void HighlightSlots(SlotZone zone, SlotPredicate predicate, bool highlighted)
        {
            HighlightSlots(zone, predicate, highlighted, PlayableSceneRules.HighlightedSlotLabel);
        }

        private void HighlightSlots(SlotZone zone, SlotPredicate predicate, bool highlighted, string label)
        {
            foreach (SlotInteract slot in AllBoardSlots())
            {
                if (slot != null && predicate(slot))
                {
                    slot.SetHighlighted(highlighted, label);
                }
            }
        }

        private void HighlightEmptySlots(SlotZone zone, bool highlighted)
        {
            HighlightEmptySlots(zone, highlighted, PlayableSceneRules.HighlightedSlotLabel);
        }

        private void HighlightEmptySlots(SlotZone zone, bool highlighted, string label)
        {
            HighlightSlots(zone, slot => !slot.IsOccupied && !BoardTargetRules.IsHeadquartersSlot(slot), highlighted, label);
        }

        private void HighlightAllSlots(bool highlighted)
        {
            HighlightAllSlots(highlighted, PlayableSceneRules.HighlightedSlotLabel);
        }

        private void HighlightAllSlots(bool highlighted, string label)
        {
            HighlightMatchingSlots(slot => true, highlighted, label);
        }

        private CardView FindView(RuntimeCard card)
        {
            return cardViews.Find(view => view.Card == card);
        }

        private void RefreshHandLayouts()
        {
            RefreshHandLayoutsFor(player.Hand, PlayerSide.Player);
            RefreshHandLayoutsFor(enemy.Hand, PlayerSide.Enemy);
            RefreshSceneInspector();
            RefreshSceneActionPrompt();
        }

        private void RefreshCameraAnchoredViewsIfNeeded(bool pointerMoved)
        {
            Vector3 cameraOffset = CameraFollowOffset();
            bool cameraMoved = (cameraOffset - lastCameraAnchoredViewOffset).sqrMagnitude > 0.00001f;
            if (!cameraMoved && !pointerMoved)
            {
                return;
            }

            lastCameraAnchoredViewOffset = cameraOffset;
            RefreshHandLayouts();
            RefreshKreditDisplays();
        }

        private void RefreshHandLayoutsFor(List<RuntimeCard> hand, PlayerSide side)
        {
            bool mulliganPresentation = MatchStartRules.ShouldUseMulliganPresentation(phase, activeSide) && side == PlayerSide.Player;
            for (int i = 0; i < hand.Count; i++)
            {
                RuntimeCard runtimeCard = hand[i];
                CardView view = FindView(runtimeCard);
                if (view == null)
                {
                    continue;
                }

                bool centerInspect = false;
                Quaternion rotation = HandBarRotation(side, hand, mulliganPresentation) *
                    (mulliganPresentation ? Quaternion.identity : HandRotation(side, i, hand.Count));
                Vector3 position = centerInspect
                    ? PlayableSceneRules.CenterInspectAnchor
                    : (mulliganPresentation ? MulliganHandPosition(i, hand.Count) : HandPosition(side, i, hand.Count));
                if (!centerInspect)
                {
                    position += FocusedHandHoverOffset(hand, side, i, mulliganPresentation);
                    position += AirborneHandUnitLift(runtimeCard, side, mulliganPresentation);
                    position += PendingOrderHandLift(runtimeCard, side, mulliganPresentation);
                }
                float scale = centerInspect
                    ? PlayableSceneRules.CenterInspectScale
                    : (mulliganPresentation ? PlayableSceneRules.MulliganHandScale : PlayableSceneRules.HandCardScale);
                view.SetLayout(position, new Vector3(scale, 1f, scale), rotation, !centerInspect);

                bool focusedHandCard = IsFocusedPlayerHandCard(side, runtimeCard);
                if (centerInspect)
                {
                    view.SetDetailPresentation();
                    view.SetCenterInspectPresentation(true);
                }
                else if (side == PlayerSide.Player && focusedHandCard)
                {
                    view.SetRevealedHandPresentation();
                    view.SetCenterInspectPresentation(false);
                }
                else
                {
                    view.SetHandPresentation(mulliganPresentation);
                    view.SetCenterInspectPresentation(false);
                }
            }
        }

        private void RefreshAllViews()
        {
            ClearOrphanCenterInspectViews();
            List<CardView> previousViews = new List<CardView>(cardViews);
            reusableCardViews = previousViews;
            cardViews.Clear();
            CreateHandViews(player.Hand, PlayerSide.Player);
            CreateHandViews(enemy.Hand, PlayerSide.Enemy);
            RefreshCenterInspectView();
            CreateCountermeasureViews(player, -4.15f);
            CreateCountermeasureViews(enemy, 4.15f);
            CreateBoardViews();
            DestroyUnusedCardViews(previousViews);
            reusableCardViews = null;
            RefreshHeadquartersDisplays();
            RefreshPileDisplays();
            RefreshKreditDisplays();
            RefreshSceneStatus();
            RefreshSceneActionPrompt();
            RefreshSceneInspector();
            RefreshSceneDeckSummary();
            RefreshSceneCommandButtons();
            ResyncSelectionVisuals();
        }

        private void RefreshPileDisplays()
        {
            if (pileDisplays.Count == 0 || pileDisplays.Exists(display => display == null))
            {
                pileDisplays.Clear();
                pileDisplays.AddRange(FindObjectsOfType<ScenePileDisplay>());
            }

            foreach (ScenePileDisplay display in pileDisplays)
            {
                if (display == null)
                {
                    continue;
                }

                PlayerState state = GetState(display.Side);
                int count = display.Kind == ScenePileKind.Discard ? state.Discard.Count : state.Deck.Count;
                display.UpdateCount(count);
            }
        }

        private void RefreshHeadquartersDisplays()
        {
            if (board == null)
            {
                return;
            }

            board.UpdateHeadquartersHealth(player.HeadquartersHealth, enemy.HeadquartersHealth);
        }

        private void RefreshKreditDisplays()
        {
            if (kreditDisplays.Count == 0 || kreditDisplays.Exists(display => display == null))
            {
                kreditDisplays.Clear();
                kreditDisplays.AddRange(FindObjectsOfType<SceneKreditDisplay>());
            }

            foreach (SceneKreditDisplay display in kreditDisplays)
            {
                if (display == null)
                {
                    continue;
                }

                display.UpdateKredits(GetState(display.Side));
                display.transform.position = KreditDisplayPosition(display.Side);
            }
        }

        private void RefreshSceneStatus()
        {
            EnsureSceneUiReferences();
            if (sceneStatusDisplay == null)
            {
                return;
            }

            sceneStatusDisplay.UpdateSnapshot(player, enemy, phase, activeSide, FrontlineLabel(), status, actionLog);
        }

        private void RefreshSceneActionPrompt()
        {
            EnsureSceneUiReferences();

            if (sceneActionPrompt == null)
            {
                GameObject promptObject = new GameObject("Action Prompt");
                sceneActionPrompt = promptObject.AddComponent<SceneActionPrompt>();
            }

            sceneActionPrompt.ApplyPresentation();
            sceneActionPrompt.UpdatePrompt(phase, activeSide, mulliganUsed);
        }

        private void RefreshSceneInspector()
        {
            EnsureSceneUiReferences();

            if (sceneCardInspector == null)
            {
                GameObject inspectorObject = new GameObject("Card Inspector");
                sceneCardInspector = inspectorObject.AddComponent<SceneCardInspector>();
            }

            sceneCardInspector.transform.position = PlayableSceneRules.CardInspectorPosition;
            sceneCardInspector.ApplyPresentation();
            sceneCardInspector.ShowCard(null);
            RefreshCenterInspectView();
        }

        private void RefreshCenterInspectView()
        {
            if (centerInspectCard == null)
            {
                DestroyCenterInspectView();
                return;
            }

            if (centerInspectView != null && centerInspectView.Card != centerInspectCard)
            {
                DestroyCenterInspectView();
            }

            if (centerInspectView == null)
            {
                GameObject cardObject = new GameObject($"CenterInspect_{centerInspectCard.CardName}");
                centerInspectView = cardObject.AddComponent<CardView>();
                centerInspectView.Initialize(centerInspectCard, this, false, true);
                transientCardViews.Add(centerInspectView);
            }
            centerInspectView.SetLayout(
                CenterInspectAnchorFor(centerInspectCard),
                new Vector3(PlayableSceneRules.CenterInspectScale, 1f, PlayableSceneRules.CenterInspectScale),
                Quaternion.identity,
                false);
            centerInspectView.SetDetailPresentation();
            centerInspectView.SetInteractionEnabled(true);
            centerInspectView.SetDragEnabled(false);
            centerInspectView.SetCenterInspectPresentation(true);
        }

        private bool ShouldUseWorldCenterInspectView(RuntimeCard card)
        {
            return card != null
                && pendingAirborneOrder != null
                && card == pendingAirborneOrder
                && IsAirborneUnitSelectionActive();
        }

        private void DestroyCenterInspectView()
        {
            if (centerInspectView == null)
            {
                return;
            }
            transientCardViews.Remove(centerInspectView);
            RuntimeSafeDestroy.Destroy(centerInspectView.gameObject);
            centerInspectView = null;
        }

        private void ClearOrphanCenterInspectViews()
        {
            CardView[] views = FindObjectsOfType<CardView>();
            for (int i = 0; i < views.Length; i++)
            {
                CardView view = views[i];
                if (view == null || view == centerInspectView || !view.name.StartsWith("CenterInspect_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                transientCardViews.Remove(view);
                RuntimeSafeDestroy.Destroy(view.gameObject);
            }
        }

        private Vector3 CenterInspectAnchorFor(RuntimeCard card)
        {
            return PlayableSceneRules.CenterInspectAnchor;
        }

        private void ClearCardInspectState()
        {
            centerInspectCard = null;
            inspectedCard = null;
            hoveredHandCardId = null;
            DestroyCenterInspectView();
            RefreshSceneInspector();
        }

        private void RefreshSceneDeckSummary()
        {
            EnsureSceneUiReferences();

            if (sceneDeckSummary == null)
            {
                return;
            }

            if (phase != GamePhase.DeckBuilder)
            {
                sceneDeckSummary.Clear();
                return;
            }

            sceneDeckSummary.UpdateSummary(
                "Endfield Deck",
                "干员、指令、反制。");
        }

        private void EnsureSceneUiReferences()
        {
            if (sceneStatusDisplay == null)
            {
                sceneStatusDisplay = FindObjectOfType<SceneStatusDisplay>();
            }

            if (sceneActionPrompt == null)
            {
                sceneActionPrompt = FindObjectOfType<SceneActionPrompt>();
            }

            if (sceneCardInspector == null)
            {
                sceneCardInspector = FindObjectOfType<SceneCardInspector>();
            }

            if (sceneDeckSummary == null)
            {
                sceneDeckSummary = FindObjectOfType<SceneDeckSummary>();
            }
        }

        private void RefreshSceneCommandButtons()
        {
            if (sceneCommandButtons.Count == 0 || sceneCommandButtons.Exists(button => button == null))
            {
                sceneCommandButtons.Clear();
                sceneCommandButtons.AddRange(FindObjectsOfType<SceneCommandButton>());
            }

            foreach (SceneCommandButton button in sceneCommandButtons)
            {
                if (button == null)
                {
                    continue;
                }

                bool available = IsSceneCommandAvailable(button.Command);
                button.SetAvailable(available);
                button.SetVisible(IsSceneCommandVisible(button.Command, available));
            }
        }

        private bool IsSceneCommandVisible(SceneCommandType command, bool available)
        {
            return SceneCommandRules.IsVisible(command, phase, available, false);
        }

        private bool IsSceneCommandAvailable(SceneCommandType command)
        {
            return SceneCommandRules.IsAvailable(command, phase, activeSide, isResolvingEvents, board != null, true, mulliganUsed);
        }

        private void CreateHandViews(List<RuntimeCard> hand, PlayerSide side)
        {
            bool mulliganPresentation = MatchStartRules.ShouldUseMulliganPresentation(phase, activeSide) && side == PlayerSide.Player;
            for (int i = 0; i < hand.Count; i++)
            {
                bool hidden = side == PlayerSide.Enemy;
                RuntimeCard runtimeCard = hand[i];
                CardView view = GetOrCreateCardView(runtimeCard, hidden, true);
                if (view == null)
                {
                    continue;
                }
                Quaternion rotation = HandBarRotation(side, hand, mulliganPresentation) *
                    (mulliganPresentation ? Quaternion.identity : HandRotation(side, i, hand.Count));
                bool centerInspect = false;
                Vector3 position = centerInspect
                    ? PlayableSceneRules.CenterInspectAnchor
                    : (mulliganPresentation ? MulliganHandPosition(i, hand.Count) : HandPosition(side, i, hand.Count));
                if (!centerInspect)
                {
                    position += FocusedHandHoverOffset(hand, side, i, mulliganPresentation);
                    position += AirborneHandUnitLift(runtimeCard, side, mulliganPresentation);
                    position += PendingOrderHandLift(runtimeCard, side, mulliganPresentation);
                }
                float scale = centerInspect
                    ? PlayableSceneRules.CenterInspectScale
                    : (mulliganPresentation ? PlayableSceneRules.MulliganHandScale : PlayableSceneRules.HandCardScale);
                bool focusedHandCard = IsFocusedPlayerHandCard(side, runtimeCard);
                bool compactHandPresentation = !centerInspect
                    && (mulliganPresentation || !(side == PlayerSide.Player && focusedHandCard));
                view.SetLayout(
                    position,
                    new Vector3(scale, 1f, scale),
                    rotation,
                    !centerInspect);
                if (compactHandPresentation)
                {
                    view.SetHandPresentation(mulliganPresentation);
                }
                else if (centerInspect)
                {
                    view.SetDetailPresentation();
                }
                else if (side == PlayerSide.Player && focusedHandCard && !centerInspect)
                {
                    view.SetRevealedHandPresentation();
                }

                view.SetInteractionEnabled(!mulliganUsed || phase != GamePhase.Mulligan || side != PlayerSide.Player);
                view.SetDragEnabled(!mulliganPresentation && !centerInspect);
                view.SetCenterInspectPresentation(centerInspect);
                if (side == PlayerSide.Player)
                {
                    view.SetMulliganMarked(MulliganRules.IsMarked(mulliganMarkedIds, runtimeCard));
                }

                if (ConsumePendingDrawAnimation(runtimeCard, out PlayerSide drawSide, out int drawIndex))
                {
                    Vector3 stagedPosition = HandInsertStagingPosition(drawSide, position, drawIndex);
                    view.PlayDrawFlight(DeckWorldPosition(drawSide), stagedPosition, position);
                }
            }
        }

        private bool ConsumePendingDrawAnimation(RuntimeCard card, out PlayerSide side, out int handIndex)
        {
            side = PlayerSide.Player;
            handIndex = -1;
            if (card == null)
            {
                return false;
            }

            for (int i = 0; i < pendingDrawAnimations.Count; i++)
            {
                if (pendingDrawAnimations[i].CardId == card.Id)
                {
                    side = pendingDrawAnimations[i].Side;
                    handIndex = pendingDrawAnimations[i].HandIndex;
                    pendingDrawAnimations.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private Vector3 HandInsertStagingPosition(PlayerSide side, Vector3 finalPosition, int handIndex)
        {
            float direction = side == PlayerSide.Player ? 1f : -1f;
            float stagger = Mathf.Clamp(handIndex, 0, 8) * 0.015f;
            return finalPosition + Vector3.forward * (0.48f * direction + stagger * direction) + Vector3.up * 0.08f;
        }

        private Quaternion HandRotation(PlayerSide side, int index, int count)
        {
            float baseRotation = side == PlayerSide.Enemy ? 180f : 0f;
            float fanRotation = 0f;
            return Quaternion.Euler(0f, baseRotation + fanRotation, 0f);
        }

        private Quaternion HandBarRotation(PlayerSide side, List<RuntimeCard> hand, bool mulliganPresentation)
        {
            if (side != PlayerSide.Player || hand == null || hand.Count == 0)
            {
                return Quaternion.identity;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return Quaternion.identity;
            }

            Vector3 pointer = Input.mousePosition;
            if (pointer.x < 0f || pointer.x > Screen.width || pointer.y < 0f || pointer.y > Screen.height)
            {
                return Quaternion.identity;
            }

            Ray ray = mainCamera.ScreenPointToRay(pointer);
            Plane handPlane = new Plane(Vector3.up, mulliganPresentation
                ? PlayableSceneRules.MulliganHandAnchor.y
                : PlayableSceneRules.PlayerHandAnchor.y);
            if (!handPlane.Raycast(ray, out float enter))
            {
                return Quaternion.identity;
            }

            Vector3 hit = ray.GetPoint(enter);
            Vector3 center = mulliganPresentation
                ? MulliganHandPosition((hand.Count - 1) / 2, hand.Count)
                : HandPosition(side, (hand.Count - 1) / 2, hand.Count);
            Vector3 delta = hit - center;
            float halfWidth = Mathf.Max(0.7f, PlayableSceneRules.HandSpacing * Mathf.Max(1f, hand.Count - 1) * 0.5f + 0.45f);
            float halfDepth = 0.62f;
            float x = Mathf.Clamp(delta.x / halfWidth, -1f, 1f);
            float z = Mathf.Clamp(delta.z / halfDepth, -1f, 1f);
            float reach = Mathf.Clamp01(1f - Mathf.Max(0f, new Vector2(x, z).magnitude - 0.2f) * 0.55f);

            float pitch = z * PlayableSceneRules.HandBarTiltMaxPitch * reach;
            float roll = -x * PlayableSceneRules.HandBarTiltMaxRoll * reach;
            float yaw = -x * PlayableSceneRules.HandBarTiltMaxYaw * reach;
            return Quaternion.Euler(pitch, yaw, roll);
        }

        private Vector3 FocusedHandHoverOffset(List<RuntimeCard> hand, PlayerSide side, int index, bool mulliganPresentation)
        {
            if (side != PlayerSide.Player || mulliganPresentation || IsAirborneUnitSelectionActive() || string.IsNullOrEmpty(hoveredHandCardId))
            {
                return Vector3.zero;
            }

            int hoveredIndex = -1;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] != null && hand[i].Id == hoveredHandCardId)
                {
                    hoveredIndex = i;
                    break;
                }
            }

            if (hoveredIndex < 0 || index <= hoveredIndex)
            {
                return Vector3.zero;
            }

            return Vector3.right * PlayableSceneRules.RevealedHandSpacing;
        }

        private bool IsFocusedPlayerHandCard(PlayerSide side, RuntimeCard card)
        {
            return side == PlayerSide.Player
                && card != null
                && !IsAirborneUnitSelectionActive()
                && !string.IsNullOrEmpty(hoveredHandCardId)
                && card.Id == hoveredHandCardId;
        }

        private Vector3 AirborneHandUnitLift(RuntimeCard card, PlayerSide side, bool mulliganPresentation)
        {
            if (side != PlayerSide.Player
                || mulliganPresentation
                || pendingAirborneSlot == null
                || pendingAirborneOrder == null
                || pendingAirborneOrder.Zone != CardZone.Hand
                || pendingAirborneOrder.EffectType != CardEffectType.DeployWithBlitz
                || card == null
                || card.Zone != CardZone.Hand
                || card.Type != CardType.Unit)
            {
                return Vector3.zero;
            }

            return Vector3.forward * 0.28f + Vector3.up * 0.035f;
        }

        private Vector3 PendingOrderHandLift(RuntimeCard card, PlayerSide side, bool mulliganPresentation)
        {
            if (side != PlayerSide.Player
                || mulliganPresentation
                || card == null
                || card.Zone != CardZone.Hand
                || card.Type != CardType.Order)
            {
                return Vector3.zero;
            }

            bool isSelectedOrder = selectedCard == card;
            bool isPendingAirborneOrder = pendingAirborneOrder == card;
            if (!isSelectedOrder && !isPendingAirborneOrder)
            {
                return Vector3.zero;
            }

            return Vector3.forward * 0.20f + Vector3.up * 0.05f;
        }

        private void CreateCountermeasureViews(PlayerState state, float z)
        {
            for (int i = 0; i < state.Countermeasures.Count; i++)
            {
                bool hidden = state.Side == PlayerSide.Enemy;
                CardView view = GetOrCreateCardView(state.Countermeasures[i], hidden, false);
                if (view == null)
                {
                    continue;
                }
                Quaternion rotation = state.Side == PlayerSide.Enemy ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                view.SetLayout(
                    CountermeasurePosition(state.Side, i, state.Countermeasures.Count, z),
                    new Vector3(PlayableSceneRules.CountermeasureCardScale, 1f, PlayableSceneRules.CountermeasureCardScale),
                    rotation,
                    true);
                view.SetInteractionEnabled(true);
                view.SetDragEnabled(false);
                view.SetHandPresentation(false);
            }
        }

        private Vector3 HandPosition(PlayerSide side, int index, int count)
        {
            Vector3 cameraOffset = CameraFollowOffset();
            if (sceneCardLayout != null)
            {
                Vector3 scenePosition = sceneCardLayout.HandPosition(side, index, count, playerHandRevealed)
                    + cameraOffset
                    + Vector3.up * CardLayoutRules.HandLayerHeightOffset(index);
                if (side == PlayerSide.Player)
                {
                    scenePosition.z = ConstrainedPlayerHandZ(playerHandRevealed, scenePosition.z, scenePosition.y);
                }

                return scenePosition;
            }

            float spacing = PlayableSceneRules.HandSpacing;
            float z = side == PlayerSide.Player
                ? (playerHandRevealed ? PlayableSceneRules.PlayerHandRevealedAnchor.z : PlayableSceneRules.PlayerHandAnchor.z)
                : PlayableSceneRules.EnemyHandAnchor.z;
            if (side == PlayerSide.Player)
            {
                z += CardLayoutRules.HandFanDepthOffset(index, count);
            }

            Vector3 anchor = side == PlayerSide.Player
                ? (playerHandRevealed ? PlayableSceneRules.PlayerHandRevealedAnchor : PlayableSceneRules.PlayerHandAnchor)
                : PlayableSceneRules.EnemyHandAnchor;
            Vector3 position = new Vector3(
                    anchor.x + CardLayoutRules.OffsetIndex(index, count) * spacing,
                    anchor.y + CardLayoutRules.HandLayerHeightOffset(index),
                    z)
                + cameraOffset;
            if (side == PlayerSide.Player)
            {
                position.z = ConstrainedPlayerHandZ(playerHandRevealed, position.z, position.y);
            }

            return position;
        }

        private float ConstrainedPlayerHandZ(bool revealed, float preferredZ, float handPlaneY)
        {
            float halfDepth = PlayableSceneRules.HandCardHeight * PlayableSceneRules.HandCardScale * 0.5f;
            float screenBottomZ = PlayerHandScreenBottomZ(handPlaneY);
            float maxZ = revealed
                ? screenBottomZ + halfDepth
                : screenBottomZ - halfDepth * 0.65f;
            return Mathf.Min(preferredZ, maxZ);
        }

        private float PlayerHandScreenBottomZ(float handPlaneY)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                return PlayableSceneRules.CameraPosition.z - PlayableSceneRules.OrthographicSize;
            }

            Ray bottomRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
            Plane handPlane = new Plane(Vector3.up, handPlaneY);
            if (!handPlane.Raycast(bottomRay, out float enter))
            {
                return mainCamera.transform.position.z - mainCamera.orthographicSize;
            }

            return bottomRay.GetPoint(enter).z;
        }

        private Vector3 CameraFollowOffset()
        {
            if (cameraInteraction == null)
            {
                cameraInteraction = FindObjectOfType<CameraInteraction>();
            }

            if (cameraInteraction == null)
            {
                return Vector3.zero;
            }

            Vector3 offset = cameraInteraction.ViewOffset;
            offset.y = 0f;
            return offset;
        }

        private Vector3 KreditDisplayPosition(PlayerSide side)
        {
            Vector3 basePosition = side == PlayerSide.Player
                ? PlayableSceneRules.PlayerKreditDisplayPosition
                : PlayableSceneRules.EnemyKreditDisplayPosition;
            return basePosition + CameraFollowOffset();
        }

        private Vector3 CountermeasurePosition(PlayerSide side, int index, int count, float fallbackZ)
        {
            if (sceneCardLayout != null)
            {
                return sceneCardLayout.CountermeasurePosition(side, index, count);
            }

            return new Vector3(CardLayoutRules.OffsetIndex(index, count) * 0.42f, 0.05f, fallbackZ);
        }

        private void CreateBoardViews()
        {
            foreach (KeyValuePair<RuntimeCard, SlotInteract> pair in cardSlots)
            {
                RuntimeCard card = pair.Key;
                if (card.Zone == CardZone.Discard || pair.Value == null)
                {
                    continue;
                }

                if (player.Hand.Contains(card) || enemy.Hand.Contains(card))
                {
                    continue;
                }

                CardView view = GetOrCreateCardView(card, false, false);
                if (view == null)
                {
                    continue;
                }
                view.SetLayout(
                    pair.Value.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight,
                    BoardCardScaleFor(card),
                    Quaternion.identity,
                    true);
                if (card.Type == CardType.Unit)
                {
                    view.SetBoardUnitPresentation();
                }
                view.SetInteractionEnabled(true);
                view.SetDragEnabled(true);
                view.RefreshKeywordIcons(pendingDeployDropCardId == card.Id);
            }
        }

        private Vector3 BoardCardScaleFor(RuntimeCard card)
        {
            return new Vector3(PlayableSceneRules.BoardCardScale, 1f, PlayableSceneRules.BoardCardScale);
        }

        private CardView GetOrCreateCardView(RuntimeCard card, bool hidden)
        {
            return GetOrCreateCardView(card, hidden, card != null && card.Zone == CardZone.Hand);
        }

        private CardView GetOrCreateCardView(RuntimeCard card, bool hidden, bool handPrefab)
        {
            CardView existing = reusableCardViews != null
                ? reusableCardViews.Find(view => view != null && view.Card == card)
                : FindView(card);
            if (existing != null && existing.IsHidden == hidden && existing.UsesHandPrefab == handPrefab)
            {
                reusableCardViews?.Remove(existing);
                cardViews.Remove(existing);
                cardViews.Add(existing);
                existing.Refresh();
                return existing;
            }

            if (existing != null)
            {
                reusableCardViews?.Remove(existing);
                cardViews.Remove(existing);
                RuntimeSafeDestroy.Destroy(existing.gameObject);
            }

            return CreateCardView(card, hidden, handPrefab);
        }

        private CardView CreateCardView(RuntimeCard card, bool hidden)
        {
            return CreateCardView(card, hidden, card != null && card.Zone == CardZone.Hand);
        }

        private CardView CreateCardView(RuntimeCard card, bool hidden, bool handPrefab)
        {
            GameObject cardObject = new GameObject($"Card_{card.CardName}");
            CardView view = cardObject.AddComponent<CardView>();
            view.Initialize(card, this, hidden, handPrefab);
            cardViews.Add(view);
            return view;
        }

        private CardView CreateTransientCardView(RuntimeCard card, bool handPrefab)
        {
            GameObject cardObject = new GameObject($"PlayedOrder_{card.CardName}");
            CardView view = cardObject.AddComponent<CardView>();
            view.Initialize(card, this, false, handPrefab);
            transientCardViews.Add(view);
            return view;
        }

        private CardView CreateTransientCardView(RuntimeCard card)
        {
            bool handPrefab = card != null && card.Zone == CardZone.Hand;
            return CreateTransientCardView(card, handPrefab);
        }

        private void ResyncSelectionVisuals()
        {
            if (selectedCard == null)
            {
                return;
            }

            selectedView = FindView(selectedCard);
            selectedView?.SetSelected(true);
            HighlightLegalTargets(selectedCard, true);
        }

        private DamagePreview BuildAttackDamagePreview(RuntimeCard attacker, SlotInteract targetSlot)
        {
            if (attacker == null || targetSlot == null || !IsLegalAttackTarget(attacker, targetSlot))
            {
                return default;
            }

            if (targetSlot.IsOccupied)
            {
                CountermeasureResult countermeasurePrediction = PredictCountermeasureForAttack(attacker, targetSlot.Occupant);
                return DamagePreviewRules.ForUnitAttack(attacker, targetSlot.Occupant, countermeasurePrediction);
            }

            PlayerSide defenderSide = GetOpponentState(attacker.Owner).Side;
            return DamagePreviewRules.ForHeadquartersAttack(attacker, GetState(defenderSide).HeadquartersHealth);
        }

        private DamagePreview BuildOrderDamagePreview(RuntimeCard order, SlotInteract targetSlot)
        {
            if (order == null || !player.CanSpendKredits(order.KreditCost))
            {
                return default;
            }

            if (order.EffectType == CardEffectType.DamageEnemyHeadquarters)
            {
                return DamagePreviewRules.ForOrder(order, null);
            }

            if (targetSlot == null || !IsLegalOrderTarget(order, targetSlot, PlayerSide.Player))
            {
                return default;
            }

            DamagePreview preview = BoardTargetRules.IsHeadquartersSlot(targetSlot) && !targetSlot.IsOccupied
                ? new DamagePreview
                {
                    DamageToTarget = order.EffectAmount,
                    TargetLethal = order.EffectAmount >= GetState(HeadquartersSideForSlot(targetSlot)).HeadquartersHealth
                }
                : DamagePreviewRules.ForOrder(order, targetSlot.Occupant);
            if (order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent)
            {
                AddOrderAdjacentDamagePreview(order, targetSlot, out int adjacentTargets, out int adjacentDamage);
                preview.AdjacentTargets = adjacentTargets;
                preview.AdjacentDamage = adjacentDamage;
            }

            return preview;
        }

        private CountermeasureResult PredictCountermeasureForAttack(RuntimeCard attacker, RuntimeCard attackedUnit)
        {
            if (attacker == null)
            {
                return new CountermeasureResult();
            }

            PlayerState defender = GetOpponentState(attacker.Owner);
            if (defender == null || defender.Countermeasures.Count == 0)
            {
                return new CountermeasureResult();
            }

            return CountermeasureRules.Predict(defender.Countermeasures[0], attacker, attackedUnit);
        }

        private void AddOrderAdjacentDamagePreview(RuntimeCard order, SlotInteract targetSlot, out int adjacentTargets, out int adjacentDamage)
        {
            adjacentTargets = 0;
            adjacentDamage = 0;
            if (order == null || targetSlot == null || board == null)
            {
                return;
            }

            AddAdjacentOrderDamagePreview(order, board.GetSlot(targetSlot.X - 1, targetSlot.Zone), ref adjacentTargets, ref adjacentDamage);
            AddAdjacentOrderDamagePreview(order, board.GetSlot(targetSlot.X + 1, targetSlot.Zone), ref adjacentTargets, ref adjacentDamage);
        }

        private void AddAdjacentOrderDamagePreview(RuntimeCard order, SlotInteract slot, ref int adjacentTargets, ref int adjacentDamage)
        {
            if (order == null || slot == null || !slot.IsOccupied || slot.Occupant == null)
            {
                return;
            }

            adjacentTargets++;
            adjacentDamage += ModifiedDamage(AdjacentAreaDamage(order), slot.Occupant);
        }

        private Vector3 MulliganHandPosition(int index, int count)
        {
            float spacing = PlayableSceneRules.MulliganHandSpacing;
            Vector3 anchor = PlayableSceneRules.MulliganHandAnchor;
            return anchor
                + Vector3.right * CardLayoutRules.OffsetIndex(index, count) * spacing
                + Vector3.up * CardLayoutRules.HandLayerHeightOffset(index)
                + CameraFollowOffset();
        }

        private Vector3 DeckWorldPosition(PlayerSide side)
        {
            float stackHeight = PlayableSceneRules.PileCubeDepthHeight + PlayableSceneRules.PileTopDrawClearance;
            return side == PlayerSide.Player
                ? PlayableSceneRules.PlayerDeckPilePosition + Vector3.up * stackHeight
                : PlayableSceneRules.EnemyDeckPilePosition + Vector3.up * stackHeight;
        }

        private Vector3 DiscardWorldPosition(PlayerSide side)
        {
            float stackHeight = PlayableSceneRules.PileCubeDepthHeight + PlayableSceneRules.PileTopDrawClearance * 0.55f;
            return side == PlayerSide.Player
                ? PlayableSceneRules.PlayerDiscardPilePosition + Vector3.up * stackHeight
                : PlayableSceneRules.EnemyDiscardPilePosition + Vector3.up * stackHeight;
        }

        private void EnsureSceneIconRegistry()
        {
            if (FindObjectOfType<SceneIconRegistry>() != null)
            {
                return;
            }

            GameObject registryObject = new GameObject("Scene Icon Registry");
            registryObject.AddComponent<SceneIconRegistry>();
        }

    private void DestroyUnusedCardViews(List<CardView> previousViews)
    {
        foreach (CardView view in previousViews)
        {
            if (view != null && !cardViews.Contains(view))
            {
                RuntimeSafeDestroy.Destroy(view.gameObject);
            }
        }
    }
}
