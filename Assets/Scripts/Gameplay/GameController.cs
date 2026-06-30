using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private const string DeckSavePrefix = "Frontier.CustomDeck.";
    private const string AirborneOrderName = "空降 AIRBORNE";
    private const string SignalLostOrderName = "连接丢失 SIGNAL LOST";
    private const string TrapCountermeasureName = "诱饵 TRAP";
    private const string FieldIntelCountermeasureName = "FIELD INTEL";
    private const string DiJiangOrderName = "帝江号，清空区域";
    private const string PerlicaUnitName = "佩丽卡 Perlica";
    private const string M3UnitName = "M3 M3";
    private const string ChenQianyuUnitName = "陈千语 ChenQianyu";
    private const string GilbertaUnitName = "洁尔佩塔 Gilberta";

    [SerializeField] private BoardManager board;
    [SerializeField] private CameraInteraction cameraInteraction;
    [SerializeField] private SceneCardLayout sceneCardLayout;
    [SerializeField] private SceneStatusDisplay sceneStatusDisplay;
    [SerializeField] private SceneActionPrompt sceneActionPrompt;
    [SerializeField] private SceneCardInspector sceneCardInspector;
    [SerializeField] private SceneDeckSummary sceneDeckSummary;
    [SerializeField] private List<CardData> playerDeckAssets = new List<CardData>();
    [SerializeField] private List<CardData> enemyDeckAssets = new List<CardData>();
    [SerializeField] private int openingHandSize = 4;
    [SerializeField] private bool autoStartMatch = PlayableSceneRules.AutoStartMatchByDefault;
    [SerializeField] private bool showLegacyOverlay;

    private readonly PlayerState player = new PlayerState(PlayerSide.Player);
    private readonly PlayerState enemy = new PlayerState(PlayerSide.Enemy);
    private readonly List<CardView> cardViews = new List<CardView>();
    private readonly List<CardView> transientCardViews = new List<CardView>();
    private List<CardView> reusableCardViews;
    private readonly List<string> actionLog = new List<string>();
    private readonly Queue<ResolutionEvent> resolutionEvents = new Queue<ResolutionEvent>();
    private readonly Dictionary<string, int> customDeckCounts = new Dictionary<string, int>();
    private readonly Dictionary<RuntimeCard, SlotInteract> cardSlots = new Dictionary<RuntimeCard, SlotInteract>();
    private readonly List<ScenePileDisplay> pileDisplays = new List<ScenePileDisplay>();
    private readonly List<SceneKreditDisplay> kreditDisplays = new List<SceneKreditDisplay>();
    private readonly List<SceneCommandButton> sceneCommandButtons = new List<SceneCommandButton>();
    private readonly HashSet<string> mulliganMarkedIds = new HashSet<string>();

    private RuntimeCard selectedCard;
    private CardView selectedView;
    private GamePhase phase = GamePhase.DeckBuilder;
    private DeckArchetype selectedPlayerDeck = DeckArchetype.AlliedTempo;
    private DeckArchetype selectedEnemyDeck = DeckArchetype.AxisArmor;
    private CardType collectionTypeFilter = CardType.Unit;
    private CardFaction collectionFactionFilter = CardFaction.Britain;
    private CardRarity collectionRarityFilter = CardRarity.Standard;
    private PlayerSide activeSide = PlayerSide.Player;
    private PlayerSide frontlineController = PlayerSide.Player;
    private bool hasFrontlineController;
    private bool mulliganUsed;
    private bool useCustomDeck;
    private bool isResolvingEvents;
    private bool showAllCardTypes = true;
    private bool showAllFactions = true;
    private bool showAllRarities = true;
    private bool playerHandRevealed;
    private bool playerHandRevealRequested;
    private float playerHandRevealGraceUntil;
    private int selectedDeckSlot = 1;
    private RuntimeCard inspectedCard;
    private RuntimeCard centerInspectCard;
    private string hoveredHandCardId;
    private string pendingDeployDropCardId;
    private readonly List<PendingDrawAnimation> pendingDrawAnimations = new List<PendingDrawAnimation>();
    private RuntimeCard pendingAirborneUnit;
    private string collectionSearch = string.Empty;
    private FeedbackManager feedbackManager;
    private string status = "Choose a starter deck.";
    private DragTargetArrow dragTargetArrow;
    private int autoDemoActionsRemaining;
    private float nextAutoDemoActionTime;
    private int lastSceneCommandPointerFrame = -1;
    private CardView pointerFallbackCard;
    private bool pointerFallbackActive;

    private struct PendingDrawAnimation
    {
        public string CardId;
        public PlayerSide Side;
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            EnsurePlayablePresentation();
            EnsureBoard();
            EnsureFeedbackManager();
            EnsureSceneIconRegistry();
            board.Initialize(this);
        }
    }

    private void Start()
    {
        phase = GamePhase.DeckBuilder;
        LoadDeckBuilderState();
        if (autoStartMatch)
        {
            StartNewMatch();
            if (MatchStartRules.ShouldAutoKeepOpeningHand())
            {
                KeepOpeningHand();
            }
            StartAutoDemoIfConfigured();
            return;
        }

        SetStatus("Choose a starter deck, then start the match.");
        RefreshAllViews();
    }

    private void Update()
    {
        UpdateHandReveal();
        HandleCardPointerFallbackInput();
        HandleSceneCommandPointerInput();

        if (Input.GetKeyDown(KeyCode.F1))
        {
            showLegacyOverlay = !showLegacyOverlay;
            SetStatus(SceneGuidanceRules.HelpPrompt());
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ExecuteSceneCommand(SceneCommandType.SelectAlliedTempo);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ExecuteSceneCommand(SceneCommandType.SelectAxisArmor);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ExecuteSceneCommand(SceneCommandType.SelectSovietControl);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ExecuteSceneCommand(SceneCommandType.SelectJapanAmbush);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteDefaultSceneCommand();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            ExecuteSceneCommand(SceneCommandType.Mulligan);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteSceneCommand(SceneCommandType.EndTurn);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ExecuteSceneCommand(SceneCommandType.Restart);
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            PlayFirstAvailableCard();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            AdvanceFirstAvailableUnit();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            AttackWithFirstAvailableUnit();
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            ExecuteRecommendedAction();
        }

        UpdateAutoDemo();
    }

    private void StartAutoDemoIfConfigured()
    {
        if (!PlayableSceneRules.AutoDemoActionsEnabled)
        {
            return;
        }

        autoDemoActionsRemaining = PlayableSceneRules.AutoDemoActionCount;
        nextAutoDemoActionTime = Time.time + PlayableSceneRules.AutoDemoActionIntervalSeconds;
        SetStatus("AUTO DEMO — WATCHING BASIC PLAY/ACTION FLOW. PRESS R TO RESTART.");
    }

    private void UpdateAutoDemo()
    {
        if (autoDemoActionsRemaining <= 0 || Time.time < nextAutoDemoActionTime || isResolvingEvents || phase == GamePhase.GameOver)
        {
            return;
        }

        if (phase == GamePhase.EnemyTurn || activeSide != PlayerSide.Player)
        {
            nextAutoDemoActionTime = Time.time + PlayableSceneRules.AutoDemoActionIntervalSeconds;
            return;
        }

        if (phase == GamePhase.Mulligan)
        {
            ExecuteSceneCommand(SceneCommandType.KeepHand);
            autoDemoActionsRemaining--;
            nextAutoDemoActionTime = Time.time + PlayableSceneRules.AutoDemoActionIntervalSeconds;
            return;
        }

        ExecuteRecommendedAction();
        autoDemoActionsRemaining--;
        nextAutoDemoActionTime = Time.time + PlayableSceneRules.AutoDemoActionIntervalSeconds;
    }

    private void UpdateHandReveal()
    {
        if (MatchStartRules.ShouldForceRevealPlayerHand(phase, activeSide))
        {
            if (!playerHandRevealed)
            {
                playerHandRevealed = true;
                RefreshAllViews();
            }

            return;
        }

        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
        {
            if (playerHandRevealed)
            {
                playerHandRevealed = false;
                RefreshAllViews();
            }

            return;
        }

        bool shouldReveal = Time.time < playerHandRevealGraceUntil
            || IsPointerRaycastOverPlayerHand();
        if (shouldReveal == playerHandRevealed)
        {
            return;
        }

        playerHandRevealed = shouldReveal;
        RefreshAllViews();
    }

    public void SetPlayerHandRevealRequested(bool revealRequested)
    {
        if (playerHandRevealRequested == revealRequested)
        {
            return;
        }

        playerHandRevealRequested = revealRequested;
        UpdateHandReveal();
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
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.GetComponent<HandRevealZone>() != null)
            {
                return true;
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
        playerHandRevealed = true;
    }

    public void HandleCardClicked(CardView view)
    {
        if (isResolvingEvents || view == null || view.Card == null)
        {
            return;
        }

        RuntimeCard clicked = view.Card;
        if (MulliganRules.CanMarkForDiscard(phase, activeSide, clicked))
        {
            bool marked = MulliganRules.ToggleMarked(mulliganMarkedIds, clicked);
            view.SetMulliganMarked(marked);
            SetStatus(marked
                ? $"Marked {clicked.CardName} for mulligan. Click Mulligan to replace marked cards."
                : $"Unmarked {clicked.CardName}.");
            return;
        }

        if (CardInspectModeRules.ShouldExitInspectMode(clicked, centerInspectCard))
        {
            centerInspectCard = null;
            SetStatus("Closed card detail view.");
            RefreshAllViews();
            return;
        }

        if (CardInspectModeRules.ShouldEnterInspectMode(phase, activeSide, clicked, centerInspectCard))
        {
            ClearSelection();
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
                else if (selectedCard.Zone == CardZone.Frontline)
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

        if (selectedCard != null && selectedCard.EffectType == CardEffectType.DeployWithBlitz
            && selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Order
            && clicked.Type == CardType.Unit && clicked.Zone == CardZone.Hand)
        {
            pendingAirborneUnit = clicked;
            SelectCard(selectedCard, selectedView);
            SetStatus($"AIRBORNE: CLICK ANY EMPTY SLOT TO DEPLOY {clicked.CardName}." );
            HighlightAirborneDeploymentTargets(clicked, true);
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

    public int AvailableKreditsFor(PlayerSide side)
    {
        return GetState(side).Kredits;
    }

    public void HandleCardHovered(CardView view)
    {
        if (isResolvingEvents || view == null || view.Card == null || !CardTextRules.CanHoverInspect(view.Card, view.IsHidden))
        {
            return;
        }

        inspectedCard = view.Card;
        if (view.Card.Zone == CardZone.Hand && view.Card.Owner == PlayerSide.Player)
        {
            if (hoveredHandCardId != view.Card.Id)
            {
                hoveredHandCardId = view.Card.Id;
                RefreshAllViews();
            }
        }

        RefreshSceneInspector();
    }

    public void HandleCardHoverEnded(CardView view)
    {
        if (view == null || view.Card == null || hoveredHandCardId != view.Card.Id)
        {
            return;
        }

        hoveredHandCardId = null;
        RefreshAllViews();
    }

    public void HandleCardReleased(CardView view, Vector3 releasePosition)
    {
        if (isResolvingEvents || view == null || view.Card == null)
        {
            return;
        }

        if (view.Card.Zone == CardZone.Hand)
        {
            centerInspectCard = null;
        }

        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
        {
            SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
            centerInspectCard = null;
            ClearSelection();
            RefreshAllViews();
            return;
        }

        SlotInteract slot = ResolvePointerSlot(releasePosition, view.Card);
        if (slot == null)
        {
            RejectSelectedHandCard(SceneGuidanceRules.MissedDragTargetPrompt(view.Card));
            return;
        }

        if (selectedCard != view.Card)
        {
            SelectCard(view.Card, view);
        }

        HandleSlotClicked(slot);
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
        bool legalAttack = canAttack && view.Card.Zone == CardZone.Frontline && IsLegalAttackTarget(view.Card, targetSlot);
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

        SlotInteract targetSlot = board.GetSlot(pointerPosition);
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

        centerInspectCard = null;
        if (phase != GamePhase.PlayerTurn || activeSide != PlayerSide.Player)
        {
            SetStatus(SceneGuidanceRules.BlockedInteractionPrompt(phase, activeSide));
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
            }

            return;
        }

        SlotInteract slot = board.GetSlot(releasePosition);
        if (slot == null || !IsLegalOrderTarget(view.Card, slot, PlayerSide.Player))
        {
            RejectSelectedHandCard(SceneGuidanceRules.IllegalOrderTargetPrompt(view.Card, slot != null ? slot.Occupant : null, PlayerSide.Player));
            return;
        }

        TryPlayOrderOnSlot(slot);
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

    private void ClearCardDamagePreviews()
    {
        foreach (CardView view in cardViews)
        {
            if (view != null)
            {
                view.HideDamagePreview();
            }
        }

        board?.HideHeadquartersDamagePreviews();
    }

    private void ApplyDamagePreviewToSlot(SlotInteract slot, DamagePreview preview)
    {
        if (slot == null)
        {
            return;
        }

        if (BoardTargetRules.IsHeadquartersSlot(slot))
        {
            PlayerSide headquartersSide = slot.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
            board?.ShowHeadquartersDamagePreview(headquartersSide, preview.DamageToTarget, preview.TargetLethal);
            return;
        }

        if (!slot.IsOccupied || slot.Occupant == null)
        {
            return;
        }

        CardView targetView = FindView(slot.Occupant);
        if (targetView == null)
        {
            return;
        }

        targetView.ShowDamagePreview(preview.DamageToTarget, preview.TargetLethal);
    }

    private void ApplyDamagePreviewToView(CardView view, int damage, bool lethal)
    {
        if (view == null)
        {
            return;
        }

        view.ShowDamagePreview(damage, lethal);
    }

    private void ApplyOrderAdjacentDamagePreviews(RuntimeCard order, SlotInteract targetSlot)
    {
        if (order == null || targetSlot == null || board == null || order.EffectType != CardEffectType.DamageTargetUnitAndAdjacent)
        {
            return;
        }

        ApplyAdjacentOrderDamagePreview(order, board.GetSlot(targetSlot.X - 1, targetSlot.Zone));
        ApplyAdjacentOrderDamagePreview(order, board.GetSlot(targetSlot.X + 1, targetSlot.Zone));
    }

    private void ApplyAdjacentOrderDamagePreview(RuntimeCard order, SlotInteract slot)
    {
        if (order == null || slot == null || !slot.IsOccupied || slot.Occupant == null)
        {
            return;
        }

        int damage = ModifiedDamage(order.EffectAmount, slot.Occupant);
        CardView adjacentView = FindView(slot.Occupant);
        if (adjacentView != null)
        {
            adjacentView.ShowDamagePreview(damage, damage >= slot.Occupant.CurrentDefense);
        }
    }

    public void HandleBoardAreaClicked()
    {
        if (isResolvingEvents)
        {
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
                PlayerSide headquartersSide = slot.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
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
            PlayerSide headquartersSide = slot.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
            bool handUnitCannotDeployToHeadquarters = selectedCard.Zone == CardZone.Hand && selectedCard.Type == CardType.Unit;
            bool supportUnitCannotAttackHeadquarters = selectedCard.Zone == CardZone.PlayerSupport;
            bool frontlineUnitTargetsOwnHeadquarters = selectedCard.Zone == CardZone.Frontline && headquartersSide == selectedCard.Owner;
            if (handUnitCannotDeployToHeadquarters || supportUnitCannotAttackHeadquarters || frontlineUnitTargetsOwnHeadquarters)
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

        if (selectedCard.Zone == CardZone.PlayerSupport && slot.Zone == SlotZone.Frontline)
        {
            TryMoveToFrontline(slot);
            return;
        }

        if (selectedCard.Zone == CardZone.Frontline)
        {
            TryAttack(slot);
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
                if (phase == GamePhase.DeckBuilder && (!useCustomDeck || DeckRules.IsValidDeckSize(CurrentCustomDeckSize())))
                {
                    SaveDeckBuilderState();
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

            case SceneCommandType.SelectAlliedTempo:
                SelectDeckFromScene(DeckArchetype.AlliedTempo);
                break;

            case SceneCommandType.SelectAxisArmor:
                SelectDeckFromScene(DeckArchetype.AxisArmor);
                break;

            case SceneCommandType.SelectSovietControl:
                SelectDeckFromScene(DeckArchetype.SovietControl);
                break;

            case SceneCommandType.SelectJapanAmbush:
                SelectDeckFromScene(DeckArchetype.JapaneseAmbush);
                break;
        }
    }

    private void OnGUI()
    {
        HandleCardPointerGuiFallback();
        DrawActionPromptHud();
        DrawSceneCommandHitAreas();

        if (!showLegacyOverlay)
        {
            return;
        }

        GUI.Box(new Rect(10, 10, 470, 204), "Frontier Command Prototype");
        GUI.Label(new Rect(25, 38, 430, 22), $"Player HQ: {player.HeadquartersHealth}   Enemy HQ: {enemy.HeadquartersHealth}");
        GUI.Label(new Rect(25, 62, 430, 22), $"Kredits: {player.Kredits}/{player.MaxKredits}   Phase: {phase}   Turn: {activeSide}");
        GUI.Label(new Rect(25, 86, 430, 22), $"Deck: {player.Deck.Count}   Hand: {player.Hand.Count}   Discard: {player.Discard.Count}");
        GUI.Label(new Rect(25, 110, 430, 22), $"Frontline: {FrontlineLabel()}   Countermeasures: {player.Countermeasures.Count}");
        GUI.Label(new Rect(25, 134, 430, 38), status);
        DrawInspectionPanel();
        DrawActionLog();

        if (phase == GamePhase.DeckBuilder)
        {
            DrawDeckBuilderPanel();
            return;
        }

        if (phase == GamePhase.Mulligan)
        {
            if (GUI.Button(new Rect(25, 174, 110, 24), "Keep Hand"))
            {
                KeepOpeningHand();
            }

            GUI.enabled = !mulliganUsed;
            if (GUI.Button(new Rect(142, 174, 110, 24), "Mulligan"))
            {
                MulliganOpeningHand();
            }
            GUI.enabled = true;
            return;
        }

        if (phase == GamePhase.GameOver)
        {
            if (GUI.Button(new Rect(25, 174, 110, 24), "Restart"))
            {
                RestartGame();
            }
            return;
        }

        GUI.enabled = !isResolvingEvents;
        if (phase == GamePhase.PlayerTurn && GUI.Button(new Rect(25, 174, 100, 24), "End Turn"))
        {
            EndPlayerTurn();
        }

        GUI.enabled = true;
        if (GUI.Button(new Rect(132, 174, 110, 24), "Strike Board"))
        {
            board.TriggerStrike(2, SlotZone.Frontline);
        }

        if (isResolvingEvents)
        {
            GUI.Label(new Rect(250, 178, 170, 22), "Resolving...");
        }
    }

    private void HandleCardPointerGuiFallback()
    {
        Event current = Event.current;
        if (current == null || current.button != 0)
        {
            return;
        }

        if (current.type == EventType.MouseDown)
        {
            CardView view = FindGuiPointerCard(current.mousePosition);
            if (view != null && view.BeginPointerInteraction())
            {
                pointerFallbackCard = view;
                pointerFallbackActive = true;
                lastSceneCommandPointerFrame = Time.frameCount;
                current.Use();
            }
        }
        else if (current.type == EventType.MouseDrag && pointerFallbackActive && pointerFallbackCard != null)
        {
            if (pointerFallbackCard.DragPointerInteraction())
            {
                lastSceneCommandPointerFrame = Time.frameCount;
                current.Use();
            }
        }
        else if (current.type == EventType.MouseUp && pointerFallbackActive && pointerFallbackCard != null)
        {
            pointerFallbackCard.EndPointerInteraction();
            pointerFallbackActive = false;
            pointerFallbackCard = null;
            lastSceneCommandPointerFrame = Time.frameCount;
            current.Use();
        }
    }

    private CardView FindGuiPointerCard(Vector2 guiPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        Vector3 screenPointer = new Vector3(guiPosition.x, Screen.height - guiPosition.y, 0f);
        CardView bestView = null;
        float bestDistance = float.MaxValue;
        foreach (CardView view in cardViews)
        {
            if (view == null || !view.TryPointerRaycastDistance(mainCamera, screenPointer, out float distance))
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

    private void DrawActionLog()
    {
        GUI.Box(new Rect(Screen.width - 330, 10, 320, 188), "Action Log");
        for (int i = 0; i < actionLog.Count; i++)
        {
            GUI.Label(new Rect(Screen.width - 315, 38 + i * 18, 290, 18), actionLog[i]);
        }
    }

    private void DrawSceneCommandHitAreas()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (sceneCommandButtons.Count == 0)
        {
            sceneCommandButtons.AddRange(FindObjectsOfType<SceneCommandButton>());
        }

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

            Rect hitArea = SceneCommandGuiRect(button, mainCamera);
            if (GUI.Button(hitArea, GUIContent.none, GUIStyle.none))
            {
                ExecuteSceneCommand(button.Command);
                return;
            }
        }
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

    private void DrawInspectionPanel()
    {
        GUI.Box(new Rect(10, Screen.height - 164, 430, 154), "Card Detail");
        if (inspectedCard == null)
        {
            GUI.Label(new Rect(25, Screen.height - 136, 390, 24), "Select a visible card to inspect its cost, type, stats, and rules.");
            return;
        }

        GUI.Label(new Rect(25, Screen.height - 136, 390, 22), $"{inspectedCard.KreditCost}K {inspectedCard.CardName} — {inspectedCard.Nation} {inspectedCard.Type}");
        GUI.Label(new Rect(25, Screen.height - 112, 390, 22), inspectedCard.Type == CardType.Unit ? $"Stats {inspectedCard.Attack}/{inspectedCard.CurrentDefense}  Op {inspectedCard.OperationCost}  Zone {inspectedCard.Zone}" : $"Effect {inspectedCard.EffectType} {inspectedCard.EffectAmount}  Zone {inspectedCard.Zone}");
        GUI.Label(new Rect(25, Screen.height - 88, 390, 22), $"Keywords: {inspectedCard.Keywords}");
        GUI.Label(new Rect(25, Screen.height - 64, 390, 44), inspectedCard.RulesText);
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

    private void DrawDeckBuilderPanel()
    {
        GUI.Label(new Rect(25, 174, 430, 22), $"Deck slot {selectedDeckSlot}: {DeckDisplayName(selectedPlayerDeck)}");
        if (GUI.Button(new Rect(25, 198, 62, 24), "Slot 1"))
        {
            SelectDeckSlot(1);
        }
        if (GUI.Button(new Rect(93, 198, 62, 24), "Slot 2"))
        {
            SelectDeckSlot(2);
        }
        if (GUI.Button(new Rect(161, 198, 62, 24), "Slot 3"))
        {
            SelectDeckSlot(3);
        }

        if (GUI.Button(new Rect(25, 226, 106, 24), "Allied Tempo"))
        {
            SelectDeckArchetype(DeckArchetype.AlliedTempo);
        }
        if (GUI.Button(new Rect(137, 226, 96, 24), "Axis Armor"))
        {
            SelectDeckArchetype(DeckArchetype.AxisArmor);
        }
        if (GUI.Button(new Rect(239, 226, 108, 24), "Soviet Control"))
        {
            SelectDeckArchetype(DeckArchetype.SovietControl);
        }
        if (GUI.Button(new Rect(353, 226, 108, 24), "Japan Ambush"))
        {
            SelectDeckArchetype(DeckArchetype.JapaneseAmbush);
        }

        GUI.Label(new Rect(25, 254, 430, 22), DeckDescription(selectedPlayerDeck));
        DrawCollectionFilters();
        DrawDeckEditorRows();

        useCustomDeck = GUI.Toggle(new Rect(25, 520, 190, 24), useCustomDeck, "Use edited deck");
        GUI.Label(new Rect(218, 520, 170, 24), $"Deck size: {CurrentCustomDeckSize()}/{DeckRules.MinimumDeckSize}");

        if (GUI.Button(new Rect(25, 548, 110, 26), "Save Deck"))
        {
            SaveDeckBuilderState();
            SetStatus("Deck list saved locally.");
        }

        if (GUI.Button(new Rect(142, 548, 116, 26), "Reset Deck"))
        {
            ResetCustomDeckCounts();
            SaveDeckBuilderState();
            SetStatus("Deck list reset to starter ratios.");
        }

        GUI.enabled = !useCustomDeck || DeckRules.IsValidDeckSize(CurrentCustomDeckSize());
        if (GUI.Button(new Rect(265, 548, 128, 26), "Start Match"))
        {
            SaveDeckBuilderState();
            StartNewMatch();
        }
        GUI.enabled = true;
    }

    private void DrawCollectionFilters()
    {
        GUI.Label(new Rect(25, 278, 80, 22), "Search:");
        collectionSearch = GUI.TextField(new Rect(84, 276, 178, 22), collectionSearch ?? string.Empty);
        if (GUI.Button(new Rect(268, 276, 52, 22), "Clear"))
        {
            collectionSearch = string.Empty;
        }

        GUI.Label(new Rect(25, 304, 80, 22), "Type:");
        if (GUI.Button(new Rect(84, 302, 52, 22), "All"))
        {
            showAllCardTypes = true;
        }
        if (GUI.Button(new Rect(142, 302, 52, 22), "Units"))
        {
            showAllCardTypes = false;
            collectionTypeFilter = CardType.Unit;
        }
        if (GUI.Button(new Rect(200, 302, 58, 22), "Orders"))
        {
            showAllCardTypes = false;
            collectionTypeFilter = CardType.Order;
        }
        if (GUI.Button(new Rect(264, 302, 104, 22), "Counters"))
        {
            showAllCardTypes = false;
            collectionTypeFilter = CardType.Countermeasure;
        }

        GUI.Label(new Rect(25, 330, 80, 22), "Faction:");
        if (GUI.Button(new Rect(84, 328, 42, 22), "All"))
        {
            showAllFactions = true;
        }
        if (GUI.Button(new Rect(132, 328, 52, 22), "Brit"))
        {
            showAllFactions = false;
            collectionFactionFilter = CardFaction.Britain;
        }
        if (GUI.Button(new Rect(190, 328, 42, 22), "USA"))
        {
            showAllFactions = false;
            collectionFactionFilter = CardFaction.USA;
        }
        if (GUI.Button(new Rect(238, 328, 52, 22), "Ger"))
        {
            showAllFactions = false;
            collectionFactionFilter = CardFaction.Germany;
        }
        if (GUI.Button(new Rect(296, 328, 52, 22), "Sov"))
        {
            showAllFactions = false;
            collectionFactionFilter = CardFaction.Soviet;
        }
        if (GUI.Button(new Rect(354, 328, 52, 22), "Jap"))
        {
            showAllFactions = false;
            collectionFactionFilter = CardFaction.Japan;
        }

        GUI.Label(new Rect(25, 356, 80, 22), "Rarity:");
        if (GUI.Button(new Rect(84, 354, 42, 22), "All"))
        {
            showAllRarities = true;
        }
        if (GUI.Button(new Rect(132, 354, 72, 22), "Standard"))
        {
            showAllRarities = false;
            collectionRarityFilter = CardRarity.Standard;
        }
        if (GUI.Button(new Rect(210, 354, 62, 22), "Limited"))
        {
            showAllRarities = false;
            collectionRarityFilter = CardRarity.Limited;
        }
        if (GUI.Button(new Rect(278, 354, 60, 22), "Special"))
        {
            showAllRarities = false;
            collectionRarityFilter = CardRarity.Special;
        }
        if (GUI.Button(new Rect(344, 354, 48, 22), "Elite"))
        {
            showAllRarities = false;
            collectionRarityFilter = CardRarity.Elite;
        }
    }

    private void EnsurePlayablePresentation()
    {
        PlayableScenePresenter presenter = FindObjectOfType<PlayableScenePresenter>();
        if (presenter == null)
        {
            presenter = gameObject.AddComponent<PlayableScenePresenter>();
        }

        if (ShouldMutateRuntime())
        {
            presenter.EnableRuntimePresentation();
        }
    }

    private void StartNewMatch()
    {
        StopAllCoroutines();
        ClearAllCardViews();
        ClearSelection();
        ClearRuntimeCards();
        player.HeadquartersHealth = 20;
        enemy.HeadquartersHealth = 20;
        player.MaxKredits = 0;
        player.Kredits = 0;
        enemy.MaxKredits = 0;
        enemy.Kredits = 0;
        mulliganUsed = false;
        mulliganMarkedIds.Clear();
        centerInspectCard = null;
        hasFrontlineController = false;
        selectedEnemyDeck = EnemyDeckFor(selectedPlayerDeck);
        BuildDecks();
        DrawOpeningHands();
        phase = MatchStartRules.PhaseAfterAutoStart();
        SetStatus("Opening hand: click cards to mark for mulligan, then Mulligan or Keep Hand.");
        RefreshAllViews();
    }

    private void SelectDeckArchetype(DeckArchetype archetype)
    {
        if (selectedPlayerDeck == archetype)
        {
            return;
        }

        SaveDeckBuilderState();
        selectedPlayerDeck = archetype;
        LoadDeckBuilderState();
        SetStatus($"Selected {DeckDisplayName(selectedPlayerDeck)}.");
        RefreshSceneDeckSummary();
    }

    private void SelectDeckFromScene(DeckArchetype archetype)
    {
        if (phase != GamePhase.DeckBuilder)
        {
            SetStatus("Deck selection is only available before the match starts.");
            return;
        }

        SelectDeckArchetype(archetype);
    }

    private void SelectDeckSlot(int slot)
    {
        if (selectedDeckSlot == slot)
        {
            return;
        }

        SaveDeckBuilderState();
        selectedDeckSlot = Mathf.Clamp(slot, 1, 3);
        LoadDeckBuilderState();
        SetStatus($"Loaded deck slot {selectedDeckSlot}.");
        RefreshSceneDeckSummary();
    }

    private void DrawDeckEditorRows()
    {
        RuntimeCard[] templates = StarterTemplates(selectedPlayerDeck);
        GUI.Box(new Rect(20, 382, 450, 130), "Editable Collection");

        int visibleRow = 0;
        for (int i = 0; i < templates.Length; i++)
        {
            RuntimeCard card = templates[i];
            if (!CardPassesCollectionFilters(card))
            {
                continue;
            }

            string key = DeckCardKey(card);
            int count = GetCustomDeckCount(key);
            float y = 408 + visibleRow * 17;
            visibleRow++;

            GUI.Label(new Rect(35, y, 245, 18), $"{card.KreditCost}K {card.CardName} [{card.Type}]");
            if (GUI.Button(new Rect(288, y, 24, 18), "-"))
            {
                SetCustomDeckCount(key, count - 1);
            }

            GUI.Label(new Rect(318, y, 28, 18), count.ToString());
            if (GUI.Button(new Rect(348, y, 24, 18), "+"))
            {
                SetCustomDeckCount(key, count + 1);
            }
        }
    }

    private bool CardPassesCollectionFilters(RuntimeCard card)
    {
        if (!showAllCardTypes && card.Type != collectionTypeFilter)
        {
            return false;
        }

        if (!showAllFactions && card.Faction != collectionFactionFilter)
        {
            return false;
        }

        if (!showAllRarities && card.Rarity != collectionRarityFilter)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(collectionSearch))
        {
            return true;
        }

        string search = collectionSearch.ToLowerInvariant();
        return card.CardName.ToLowerInvariant().Contains(search)
            || card.Nation.ToLowerInvariant().Contains(search)
            || card.RulesText.ToLowerInvariant().Contains(search)
            || card.Type.ToString().ToLowerInvariant().Contains(search)
            || card.Faction.ToString().ToLowerInvariant().Contains(search)
            || card.Rarity.ToString().ToLowerInvariant().Contains(search);
    }

    private void LoadDeckBuilderState()
    {
        selectedDeckSlot = Mathf.Clamp(PlayerPrefs.GetInt("Frontier.SelectedDeckSlot", selectedDeckSlot), 1, 3);
        string archetypeName = PlayerPrefs.GetString(SlottedKey("SelectedDeck"), selectedPlayerDeck.ToString());
        if (System.Enum.TryParse(archetypeName, out DeckArchetype savedArchetype))
        {
            selectedPlayerDeck = savedArchetype;
        }

        useCustomDeck = PlayerPrefs.GetInt(SlottedKey("UseCustomDeck"), 0) == 1;
        showAllCardTypes = PlayerPrefs.GetInt(SlottedKey("ShowAllCardTypes"), 1) == 1;
        showAllFactions = PlayerPrefs.GetInt(SlottedKey("ShowAllFactions"), 1) == 1;
        showAllRarities = PlayerPrefs.GetInt(SlottedKey("ShowAllRarities"), 1) == 1;
        collectionSearch = PlayerPrefs.GetString(SlottedKey("CollectionSearch"), string.Empty);
        string savedFilter = PlayerPrefs.GetString(SlottedKey("CollectionTypeFilter"), collectionTypeFilter.ToString());
        if (System.Enum.TryParse(savedFilter, out CardType parsedFilter))
        {
            collectionTypeFilter = parsedFilter;
        }
        string savedFaction = PlayerPrefs.GetString(SlottedKey("CollectionFactionFilter"), collectionFactionFilter.ToString());
        if (System.Enum.TryParse(savedFaction, out CardFaction parsedFaction))
        {
            collectionFactionFilter = parsedFaction;
        }
        string savedRarity = PlayerPrefs.GetString(SlottedKey("CollectionRarityFilter"), collectionRarityFilter.ToString());
        if (System.Enum.TryParse(savedRarity, out CardRarity parsedRarity))
        {
            collectionRarityFilter = parsedRarity;
        }

        ResetCustomDeckCounts();

        string savedDeck = PlayerPrefs.GetString(DeckSaveKey(selectedPlayerDeck), string.Empty);
        if (string.IsNullOrEmpty(savedDeck))
        {
            return;
        }

        string[] entries = savedDeck.Split(';');
        foreach (string entry in entries)
        {
            if (string.IsNullOrEmpty(entry))
            {
                continue;
            }

            string[] pair = entry.Split('=');
            if (pair.Length == 2 && int.TryParse(pair[1], out int count))
            {
                customDeckCounts[pair[0]] = Mathf.Clamp(count, 0, DeckRules.MaximumCopiesPerCard);
            }
        }
    }

    private void SaveDeckBuilderState()
    {
        PlayerPrefs.SetInt("Frontier.SelectedDeckSlot", selectedDeckSlot);
        PlayerPrefs.SetString(SlottedKey("SelectedDeck"), selectedPlayerDeck.ToString());
        PlayerPrefs.SetInt(SlottedKey("UseCustomDeck"), useCustomDeck ? 1 : 0);
        PlayerPrefs.SetInt(SlottedKey("ShowAllCardTypes"), showAllCardTypes ? 1 : 0);
        PlayerPrefs.SetInt(SlottedKey("ShowAllFactions"), showAllFactions ? 1 : 0);
        PlayerPrefs.SetInt(SlottedKey("ShowAllRarities"), showAllRarities ? 1 : 0);
        PlayerPrefs.SetString(SlottedKey("CollectionTypeFilter"), collectionTypeFilter.ToString());
        PlayerPrefs.SetString(SlottedKey("CollectionFactionFilter"), collectionFactionFilter.ToString());
        PlayerPrefs.SetString(SlottedKey("CollectionRarityFilter"), collectionRarityFilter.ToString());
        PlayerPrefs.SetString(SlottedKey("CollectionSearch"), collectionSearch ?? string.Empty);

        string serialized = string.Empty;
        foreach (KeyValuePair<string, int> pair in customDeckCounts)
        {
            serialized += $"{pair.Key}={pair.Value};";
        }

        PlayerPrefs.SetString(DeckSaveKey(selectedPlayerDeck), serialized);
        PlayerPrefs.Save();
    }

    private string DeckSaveKey(DeckArchetype archetype)
    {
        return $"{DeckSavePrefix}{selectedDeckSlot}.{archetype}";
    }

    private string SlottedKey(string key)
    {
        return $"Frontier.DeckSlot.{selectedDeckSlot}.{key}";
    }

    private void ResetCustomDeckCounts()
    {
        customDeckCounts.Clear();
        RuntimeCard[] templates = StarterTemplates(selectedPlayerDeck);
        for (int i = 0; i < templates.Length; i++)
        {
            customDeckCounts[DeckCardKey(templates[i])] = DeckRules.MaximumCopiesPerCard;
        }
    }

    private string DeckCardKey(RuntimeCard card)
    {
        return card.CardName;
    }

    private int GetCustomDeckCount(string key)
    {
        return customDeckCounts.TryGetValue(key, out int count) ? count : 0;
    }

    private void SetCustomDeckCount(string key, int count)
    {
        customDeckCounts[key] = Mathf.Clamp(count, 0, DeckRules.MaximumCopiesPerCard);
        RefreshSceneDeckSummary();
    }

    private int CurrentCustomDeckSize()
    {
        int total = 0;
        foreach (int count in customDeckCounts.Values)
        {
            total += count;
        }

        return total;
    }

    private void KeepOpeningHand()
    {
        mulliganMarkedIds.Clear();
        centerInspectCard = null;
        StartTurn(PlayerSide.Player);
    }

    private void MulliganOpeningHand()
    {
        if (mulliganUsed)
        {
            return;
        }

        mulliganUsed = true;
        List<RuntimeCard> markedCards = new List<RuntimeCard>();
        Dictionary<string, Vector3> discardFlightStarts = new Dictionary<string, Vector3>();
        foreach (RuntimeCard card in player.Hand)
        {
            if (MulliganRules.IsMarked(mulliganMarkedIds, card))
            {
                markedCards.Add(card);
                CardView view = FindView(card);
                discardFlightStarts[card.Id] = view != null
                    ? view.transform.position
                    : MulliganHandPosition(player.Hand.IndexOf(card), player.Hand.Count);
            }
        }

        if (!MulliganRules.ShouldRedrawMarkedCards(mulliganMarkedIds))
        {
            SetStatus("Select cards to replace, then click Mulligan.");
            mulliganUsed = false;
            return;
        }

        foreach (RuntimeCard card in markedCards)
        {
            player.Hand.Remove(card);
            card.Zone = CardZone.Discard;
            player.Discard.Add(card);
        }

        PlayMulliganDiscardFlights(markedCards, discardFlightStarts);
        for (int i = 0; i < markedCards.Count; i++)
        {
            DrawCard(player);
        }

        mulliganMarkedIds.Clear();
        centerInspectCard = null;
        CancelAllCardPointerInteractions();
        StartTurn(PlayerSide.Player);
        SetStatus($"Mulligan replaced {markedCards.Count} card(s). Your turn.");
        RefreshSceneStatus();
    }

    private void PlayMulliganDiscardFlights(List<RuntimeCard> discardedCards, Dictionary<string, Vector3> startPositions)
    {
        foreach (RuntimeCard card in discardedCards)
        {
            if (card == null)
            {
                continue;
            }

            Vector3 start = startPositions != null && startPositions.TryGetValue(card.Id, out Vector3 storedStart)
                ? storedStart
                : PlayableSceneRules.MulliganHandAnchor;
            CardView flightView = CreateTransientCardView(card);
            flightView.SetInteractionEnabled(false);
            flightView.SetDragEnabled(false);
            flightView.SetLayout(
                start,
                new Vector3(PlayableSceneRules.MulliganHandScale, 1f, PlayableSceneRules.MulliganHandScale),
                Quaternion.identity,
                false);
            flightView.PlayMulliganDiscardFlight(start, DiscardWorldPosition(PlayerSide.Player));
            StartCoroutine(DestroyTransientViewAfterDelay(flightView, CardMotionRules.MulliganDiscardFlightSeconds + 0.08f));
        }
    }

    private System.Collections.IEnumerator DestroyTransientViewAfterDelay(CardView view, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (view != null)
        {
            transientCardViews.Remove(view);
            RuntimeSafeDestroy.Destroy(view.gameObject);
        }
    }

    private void ClearTransientCardViews()
    {
        for (int i = transientCardViews.Count - 1; i >= 0; i--)
        {
            CardView view = transientCardViews[i];
            if (view != null)
            {
                RuntimeSafeDestroy.Destroy(view.gameObject);
            }
        }

        transientCardViews.Clear();
    }

    private void ClearAllCardViews()
    {
        CardView[] views = FindObjectsOfType<CardView>();
        foreach (CardView view in views)
        {
            if (view != null)
            {
                RuntimeSafeDestroy.Destroy(view.gameObject);
            }
        }

        cardViews.Clear();
        transientCardViews.Clear();
        reusableCardViews = null;
    }

    private void ReturnHandToDeck(PlayerState state)
    {
        foreach (RuntimeCard card in state.Hand)
        {
            card.Zone = CardZone.Deck;
            state.Deck.Add(card);
        }
        state.Hand.Clear();
    }

    private void RestartGame()
    {
        StopAllCoroutines();
        ClearAllCardViews();
        ClearSelection();
        ClearRuntimeCards();
        player.HeadquartersHealth = 20;
        enemy.HeadquartersHealth = 20;
        player.MaxKredits = 0;
        player.Kredits = 0;
        enemy.MaxKredits = 0;
        enemy.Kredits = 0;
        mulliganUsed = false;
        mulliganMarkedIds.Clear();
        centerInspectCard = null;
        hasFrontlineController = false;
        phase = GamePhase.DeckBuilder;
        SetStatus("Choose a starter deck, then start the match.");
        RefreshAllViews();
    }

    private void ClearRuntimeCards()
    {
        inspectedCard = null;
        pendingDrawAnimations.Clear();
        resolutionEvents.Clear();
        isResolvingEvents = false;
        actionLog.Clear();
        player.Deck.Clear();
        player.Hand.Clear();
        player.Discard.Clear();
        player.Countermeasures.Clear();
        enemy.Deck.Clear();
        enemy.Hand.Clear();
        enemy.Discard.Clear();
        enemy.Countermeasures.Clear();

        foreach (SlotInteract slot in cardSlots.Values)
        {
            if (slot != null && slot.Occupant != null)
            {
                slot.ClearOccupant(slot.Occupant);
            }
        }
        cardSlots.Clear();
    }

    private void BuildDecks()
    {
        inspectedCard = null;
        resolutionEvents.Clear();
        isResolvingEvents = false;
        actionLog.Clear();
        player.Deck.Clear();
        enemy.Deck.Clear();

        if (useCustomDeck && DeckRules.IsValidDeckSize(CurrentCustomDeckSize()))
        {
            AddCustomDeck(player.Deck, PlayerSide.Player, selectedPlayerDeck);
        }
        else
        {
            AddStarterDeck(player.Deck, PlayerSide.Player, selectedPlayerDeck);
        }

        AddStarterDeck(enemy.Deck, PlayerSide.Enemy, selectedEnemyDeck);

        Shuffle(player.Deck);
        Shuffle(enemy.Deck);
    }

    private void AddStarterDeck(List<RuntimeCard> deck, PlayerSide owner, DeckArchetype archetype)
    {
        RuntimeCard[] templates = StarterTemplates(archetype);
        for (int i = 0; i < DeckRules.MinimumDeckSize; i++)
        {
            deck.Add(templates[i % templates.Length].CloneFor(owner));
        }
    }

    private void AddAssetDeck(List<RuntimeCard> deck, PlayerSide owner, List<CardData> assets)
    {
        int targetDeckSize = DeckAssetRules.TargetDeckSize(assets.Count);
        for (int i = 0; i < targetDeckSize; i++)
        {
            CardData cardData = assets[DeckAssetRules.TemplateIndexForPosition(i, assets.Count)];
            if (cardData != null)
            {
                deck.Add(cardData.ToRuntimeCard(owner));
            }
        }
    }

    private void AddCustomDeck(List<RuntimeCard> deck, PlayerSide owner, DeckArchetype archetype)
    {
        RuntimeCard[] templates = StarterTemplates(archetype);
        foreach (RuntimeCard template in templates)
        {
            int count = GetCustomDeckCount(DeckCardKey(template));
            for (int i = 0; i < count; i++)
            {
                deck.Add(template.CloneFor(owner));
            }
        }
    }

    private RuntimeCard[] StarterTemplates(DeckArchetype archetype)
    {
        return new[]
        {
            Unit(
                PerlicaUnitName,
                "Britain",
                5,
                3,
                4,
                2,
                CardKeyword.None,
                CardTrigger.Deployment,
                CardEffectType.AddUnitToHand,
                1,
                "部署: 将一张【帝江号，清空区域】加入手牌。",
                DiJiangOrderName),
            Unit(
                ChenQianyuUnitName,
                "Britain",
                5,
                4,
                6,
                1,
                CardKeyword.Blitz,
                "闪击。"),
            Unit(
                M3UnitName,
                "Britain",
                6,
                6,
                6,
                2,
                CardKeyword.Guard | CardKeyword.Smokescreen,
                "守护。烟幕。攻击时使所有友方目标具有+1防御。"),
            Unit(
                GilbertaUnitName,
                "Britain",
                4,
                3,
                4,
                2,
                CardKeyword.Smokescreen,
                "烟幕。攻击时对相邻敌方目标造成1点伤害。此单位在场时，场上其他友方单位具有-1行动费用。"),
            Order(
                AirborneOrderName,
                "Britain",
                5,
                CardEffectType.DeployWithBlitz,
                0,
                "选择手牌中的一张单位牌，选择并将其部署于场上任意位置，使其具有闪击。"),
            Order(
                SignalLostOrderName,
                "Britain",
                2,
                CardEffectType.IncreaseEnemyCosts,
                1,
                "使对方手牌中所有单位牌+1部署费用，对方场上所有单位牌+1行动费用。"),
            Order(
                DiJiangOrderName,
                "Britain",
                4,
                CardEffectType.DamageTargetUnitAndAdjacent,
                5,
                "对一个敌方目标造成5点伤害，对周围目标造成3点伤害。"),
            Countermeasure(
                TrapCountermeasureName,
                "Britain",
                3,
                CardEffectType.Trap,
                2,
                "友方单位即将受到攻击时，使其先获得+2+1与伏击。"),
            Countermeasure(
                FieldIntelCountermeasureName,
                "Britain",
                3,
                CardEffectType.FieldIntel,
                0,
                "敌方回合结束时，抽若干张牌，其数量与本回合内对方打出的手牌数等同。")
        };
    }


    private CardFaction FactionFromNation(string nation)
    {
        switch (nation)
        {
            case "USA":
                return CardFaction.USA;
            case "Germany":
                return CardFaction.Germany;
            case "Soviet":
                return CardFaction.Soviet;
            case "Japan":
                return CardFaction.Japan;
            default:
                return CardFaction.Britain;
        }
    }

    private CardRarity RarityFromCost(int cost)
    {
        if (cost >= 5)
        {
            return CardRarity.Elite;
        }

        if (cost >= 4)
        {
            return CardRarity.Special;
        }

        return cost >= 3 ? CardRarity.Limited : CardRarity.Standard;
    }

    private DeckArchetype EnemyDeckFor(DeckArchetype playerDeck)
    {
        switch (playerDeck)
        {
            case DeckArchetype.AlliedTempo:
                return DeckArchetype.AxisArmor;
            case DeckArchetype.AxisArmor:
                return DeckArchetype.SovietControl;
            case DeckArchetype.SovietControl:
                return DeckArchetype.JapaneseAmbush;
            default:
                return DeckArchetype.AlliedTempo;
        }
    }

    private string DeckDisplayName(DeckArchetype archetype)
    {
        switch (archetype)
        {
            case DeckArchetype.AxisArmor:
                return "Axis Armor";
            case DeckArchetype.SovietControl:
                return "Soviet Control";
            case DeckArchetype.JapaneseAmbush:
                return "Japanese Ambush";
            default:
                return "Allied Tempo";
        }
    }

    private string DeckPreview(DeckArchetype archetype)
    {
        RuntimeCard[] templates = StarterTemplates(archetype);
        string preview = "Collection preview:";
        for (int i = 0; i < templates.Length; i++)
        {
            RuntimeCard card = templates[i];
            preview += $"\n{card.KreditCost}K {card.CardName} [{card.Type}]";
        }

        return preview;
    }

    private string DeckDescription(DeckArchetype archetype)
    {
        switch (archetype)
        {
            case DeckArchetype.AxisArmor:
                return "High-stat units, repairs, and punishing countermeasures.";
            case DeckArchetype.SovietControl:
                return "Guards, pinning orders, card draw, and artillery finishers.";
            case DeckArchetype.JapaneseAmbush:
                return "Smokescreen units, countermeasures, buffs, and HQ pressure.";
            default:
                return "Low-cost guards, blitz units, draw, and flexible strikes.";
        }
    }

    private void ResolveDeploymentEffect(PlayerState owner, RuntimeCard card, SlotInteract slot)
    {
        DeploymentResult result = DeploymentRules.Resolve(card);
        if (!result.Triggered)
        {
            return;
        }

        if (card != null && card.CardName == GilbertaUnitName)
        {
            owner.RegisterGilbertaAura();
        }

        if (result.GiveCardToHand)
        {
            RuntimeCard template = FindCardTemplateByName(result.CardNameToHand);
            if (template != null)
            {
                RuntimeCard reward = template.CloneFor(owner.Side);
                reward.Zone = CardZone.Hand;
                owner.Hand.Add(reward);
            }

            SpawnFloatingText($"+{card.CardName} reward", slot.transform.position, Color.yellow, FeedbackCueType.Draw);
            SetStatus($"{card.CardName} added {result.CardNameToHand} to hand.");
        }

        if (result.CardsToDraw > 0)
        {
            for (int i = 0; i < result.CardsToDraw; i++)
            {
                DrawCard(owner);
            }

            SpawnFloatingText($"+{result.CardsToDraw} CARD", slot.transform.position, Color.green, FeedbackCueType.Buff);
            SetStatus($"{card.CardName} drew {result.CardsToDraw} card{(result.CardsToDraw == 1 ? string.Empty : "s")} on deployment.");
        }

        if (result.EnemyDeploymentCostIncrease > 0 || result.EnemyOperationCostIncrease > 0)
        {
            int amount = Mathf.Max(result.EnemyDeploymentCostIncrease, result.EnemyOperationCostIncrease);
            if (amount > 0)
            {
                GetOpponentState(owner.Side).ApplySignalLostPenalty(amount);
                SpawnFloatingText($"OP/DEPLOY +{amount}", slot.transform.position, Color.red, FeedbackCueType.Countermeasure);
                SetStatus($"{owner.Side} order increased enemy deploy/attack costs by {amount}.");
            }
        }

        if (result.FriendlyDefenseGain != 0)
        {
            ApplyDefenseGainToFriendlyUnits(owner, result.FriendlyDefenseGain);
            SpawnFloatingText($"DEF +{result.FriendlyDefenseGain}", slot.transform.position, Color.cyan, FeedbackCueType.Buff);
        }

        if (result.DrawForCardsPlayed && result.DrawForCardsPlayedAmount > 0)
        {
            int totalDraws = owner.CardsPlayedThisTurn * Mathf.Max(1, result.DrawForCardsPlayedAmount);
            for (int i = 0; i < totalDraws; i++)
            {
                DrawCard(owner);
            }

            if (totalDraws > 0)
            {
                SpawnFloatingText($"+{totalDraws} CARD", slot.transform.position, Color.green, FeedbackCueType.Buff);
                SetStatus($"{card.CardName} drew {totalDraws} cards.");
            }
        }
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

    private void ApplyDefenseGainToFriendlyUnits(PlayerState owner, int amount)
    {
        if (owner == null || amount == 0)
        {
            return;
        }

        foreach (RuntimeCard card in cardSlots.Keys)
        {
            if (card == null || card.Owner != owner.Side || card.Zone != CardZone.PlayerSupport && card.Zone != CardZone.Frontline)
            {
                continue;
            }

            card.CurrentDefense += amount;
        }
    }

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

    private int EffectiveOperationCost(RuntimeCard card)
    {
        if (card == null)
        {
            return int.MaxValue;
        }

        int effectiveCost = GetState(card.Owner).EffectiveOperationCost(card.OperationCost);
        if (card.CardName == GilbertaUnitName)
        {
            PlayerState ownerState = GetState(card.Owner);
            if (ownerState.GilbertaAuraSources > 0)
            {
                effectiveCost += 1;
            }
        }

        return effectiveCost;
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

    private RuntimeCard Unit(
        string name,
        string nation,
        int cost,
        int attack,
        int defense,
        int operationCost,
        CardKeyword keywords,
        CardTrigger trigger,
        CardEffectType effectType,
        int effectAmount,
        string text,
        string addedCardName)
    {
        RuntimeCard card = Unit(name, nation, cost, attack, defense, operationCost, keywords, trigger, effectType, effectAmount, text);
        card.AddedCardName = addedCardName;
        return card;
    }

    private RuntimeCard Unit(string name, string nation, int cost, int attack, int defense, int operationCost, CardKeyword keywords, string text)
    {
        return Unit(name, nation, cost, attack, defense, operationCost, keywords, CardTrigger.None, CardEffectType.None, 0, text);
    }

    private RuntimeCard Unit(string name, string nation, int cost, int attack, int defense, int operationCost, CardKeyword keywords, CardTrigger trigger, CardEffectType effectType, int effectAmount, string text)
    {
        CardFaction faction = FactionFromNation(nation);
        CardRarity rarity = RarityFromCost(cost);
        return new RuntimeCard
        {
            CardName = name,
            Nation = nation,
            Faction = faction,
            Rarity = rarity,
            Type = CardType.Unit,
            KreditCost = cost,
            OperationCost = operationCost,
            Attack = attack,
            Defense = defense,
            CurrentDefense = defense,
            Keywords = keywords,
            Trigger = trigger,
            EffectType = effectType,
            EffectAmount = effectAmount,
            RulesText = text
        };
    }

    private RuntimeCard Order(string name, string nation, int cost, CardEffectType effectType, int effectAmount, string text)
    {
        CardFaction faction = FactionFromNation(nation);
        CardRarity rarity = RarityFromCost(cost);
        return new RuntimeCard
        {
            CardName = name,
            Nation = nation,
            Faction = faction,
            Rarity = rarity,
            Type = CardType.Order,
            KreditCost = cost,
            EffectType = effectType,
            EffectAmount = effectAmount,
            RulesText = text
        };
    }

    private RuntimeCard Countermeasure(string name, string nation, int cost, int damage, string text)
    {
        return Countermeasure(name, nation, cost, CardEffectType.DamageTargetUnit, damage, text);
    }

    private RuntimeCard Countermeasure(string name, string nation, int cost, CardEffectType effectType, int effectAmount, string text)
    {
        CardFaction faction = FactionFromNation(nation);
        CardRarity rarity = RarityFromCost(cost);
        return new RuntimeCard
        {
            CardName = name,
            Nation = nation,
            Faction = faction,
            Rarity = rarity,
            Type = CardType.Countermeasure,
            KreditCost = cost,
            EffectType = effectType,
            EffectAmount = effectAmount,
            RulesText = text
        };
    }

    private void DrawOpeningHands()
    {
        for (int i = 0; i < openingHandSize; i++)
        {
            DrawCard(player);
            DrawCard(enemy);
        }
    }

    private void StartTurn(PlayerSide side)
    {
        activeSide = side;
        phase = side == PlayerSide.Player ? GamePhase.PlayerTurn : GamePhase.EnemyTurn;
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

    private void EndPlayerTurn()
    {
        ClearSelection();
        StartTurn(PlayerSide.Enemy);
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
            if (!player.CanSpendKredits(card.KreditCost))
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

        bool hasSupportUnit = false;
        bool needsKredits = false;
        bool pinned = false;
        bool alreadyActed = false;
        bool frontlineBlocked = hasFrontlineController && frontlineController != PlayerSide.Player;

        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Player || card.Zone != CardZone.PlayerSupport)
            {
                continue;
            }

            hasSupportUnit = true;
            if (!CanSpendUnitOperation(card, player.Kredits, out _))
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
            if (TryMoveToFrontline(destination))
            {
                return true;
            }

            ClearSelection();
        }

        ClearSelection();
        if (showFailure)
        {
            SetStatus(SceneGuidanceRules.NoAdvanceShortcutPrompt(hasSupportUnit, needsKredits, pinned, alreadyActed, frontlineBlocked, false));
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

        bool hasFrontlineUnit = false;
        bool needsKredits = false;
        bool pinned = false;
        bool alreadyAttacked = false;
        bool missingTarget = false;

        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Player || card.Zone != CardZone.Frontline)
            {
                continue;
            }

            hasFrontlineUnit = true;
            if (!CanSpendUnitOperation(card, player.Kredits, out _))
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
            SetStatus(SceneGuidanceRules.NoAttackShortcutPrompt(hasFrontlineUnit, needsKredits, pinned, alreadyAttacked, missingTarget));
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
                return FindOccupiedSlot(SlotZone.EnemySupport, PlayerSide.Enemy) ?? FindOccupiedSlot(SlotZone.Frontline, PlayerSide.Enemy);
            case CardEffectType.BuffFriendlyUnit:
                return FindOccupiedSlot(SlotZone.PlayerSupport, PlayerSide.Player) ?? FindOccupiedSlot(SlotZone.Frontline, PlayerSide.Player);
            default:
                return board.GetSlot(0, SlotZone.Frontline);
        }
    }

    private SlotInteract FindQuickAttackTarget(RuntimeCard attacker)
    {
        SlotInteract guardedTarget = FindGuardSlot(SlotZone.EnemySupport, PlayerSide.Enemy);
        if (guardedTarget != null && IsLegalAttackTarget(attacker, guardedTarget))
        {
            return guardedTarget;
        }

        SlotInteract occupiedTarget = FindOccupiedSlot(SlotZone.EnemySupport, PlayerSide.Enemy);
        if (occupiedTarget != null && IsLegalAttackTarget(attacker, occupiedTarget))
        {
            return occupiedTarget;
        }

        SlotInteract headquartersTarget = board.GetHeadquartersSlot(PlayerSide.Enemy);
        return headquartersTarget != null && IsLegalAttackTarget(attacker, headquartersTarget) ? headquartersTarget : null;
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

    private void DrawCard(PlayerState state)
    {
        if (state.Deck.Count == 0 || state.Hand.Count >= 9)
        {
            return;
        }

        RuntimeCard card = state.Deck[0];
        state.Deck.RemoveAt(0);
        card.Zone = CardZone.Hand;
        state.Hand.Add(card);
        pendingDrawAnimations.Add(new PendingDrawAnimation
        {
            CardId = card.Id,
            Side = state.Side
        });
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

    private void SelectCard(RuntimeCard card, CardView view)
    {
        ClearSelection();
        selectedCard = card;
        inspectedCard = card;
        selectedView = view;
        selectedView?.SetSelected(true);
        SetStatus(SceneGuidanceRules.SelectedActionPrompt(card, player.Kredits, hasFrontlineController, frontlineController));
        RefreshSceneInspector();
        HighlightLegalTargets(card, true);
    }

    private void ClearSelection()
    {
        if (selectedCard != null)
        {
            HighlightLegalTargets(selectedCard, false);
        }

        if (pendingAirborneUnit != null)
        {
            HighlightAirborneDeploymentTargets(pendingAirborneUnit, false);
            pendingAirborneUnit = null;
        }

        selectedCard = null;
        selectedView?.SetSelected(false);
        selectedView = null;
    }

    private void RejectSelectedHandCard(string message)
    {
        SetStatus(message);
        centerInspectCard = null;
        CancelAllCardPointerInteractions();
        ClearSelection();
        ClearDragPreview();
        RefreshSceneInspector();
        RefreshAllViews();
    }

    private void CancelAllCardPointerInteractions()
    {
        foreach (CardView view in cardViews)
        {
            if (view != null)
            {
                view.CancelPointerInteraction();
            }
        }
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
        bool canMobilizeToFrontline = selectedCard.HasKeyword(CardKeyword.Mobilize)
            && slot.Zone == SlotZone.Frontline
            && (!hasFrontlineController || frontlineController == selectedCard.Owner);
        bool canDeployToSupport = slot.Zone == SlotZone.PlayerSupport;

        if ((!canDeployToSupport && !canMobilizeToFrontline) || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot))
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

        Vector3 deployFrom = selectedView != null
            ? selectedView.transform.position
            : HandPosition(PlayerSide.Player, player.Hand.IndexOf(selectedCard), player.Hand.Count);
        player.Hand.Remove(selectedCard);
        UnitDeploymentRules.MarkDeployed(selectedCard);
        pendingDeployDropCardId = selectedCard.Id;
        PlaceCardInSlot(selectedCard, slot, slot.Zone == SlotZone.Frontline ? CardZone.Frontline : CardZone.PlayerSupport);
        SpawnFloatingText("DEPLOY", slot.transform.position, Color.cyan);
        player.RegisterCardPlayed();
        DrawCard(player);
        ResolveDeploymentEffect(player, selectedCard, slot);
        if (DeployStrikeRules.ShouldTriggerStrike(selectedCard))
        {
            board.TriggerStrike(slot.X, slot.Zone);
        }

        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterDeployPrompt(selectedCard));
        ClearSelection();
        RefreshAllViews();
        CardView deployedView = FindView(selectedCard);
        if (deployedView != null)
        {
            deployedView.PlayDeployDrop(deployFrom, slot.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight);
            deployedView.RefreshKeywordIcons(true);
        }

        pendingDeployDropCardId = null;
        return true;
    }

    private void TryPlayAirborneDeployment(SlotInteract slot)
    {
        if (selectedCard == null || selectedCard.Type != CardType.Order || selectedCard.EffectType != CardEffectType.DeployWithBlitz || pendingAirborneUnit == null)
        {
            SetStatus("AIRBORNE: SELECT AN ORDER IN HAND AND THEN A TARGET UNIT.");
            return;
        }

        if (!player.CanSpendKredits(selectedCard.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(selectedCard, "play airborne", player.Kredits));
            return;
        }

        if (slot == null || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot) || !IsEmptyZoneForAirborneDeployment(slot))
        {
            RejectSelectedHandCard("AIRBORNE: SELECT AN EMPTY SUPPORT OR FRONTLINE SLOT.");
            return;
        }

        if (!player.Hand.Contains(pendingAirborneUnit))
        {
            SetStatus("AIRBORNE: SELECTED UNIT NO LONGER IN HAND.");
            pendingAirborneUnit = null;
            ClearSelection();
            return;
        }

        if (!player.TrySpendKredits(selectedCard.KreditCost))
        {
            RejectSelectedHandCard(SceneGuidanceRules.CannotAffordCardPrompt(selectedCard, "play airborne", player.Kredits));
            return;
        }

        player.Hand.Remove(selectedCard);
        selectedCard.Zone = CardZone.Discard;
        player.Discard.Add(selectedCard);

        RuntimeCard unit = pendingAirborneUnit;
        player.Hand.Remove(unit);

        if (!unit.HasKeyword(CardKeyword.Blitz))
        {
            unit.AddKeyword(CardKeyword.Blitz);
        }

        unit.Zone = CardZone.Frontline;
        pendingDeployDropCardId = unit.Id;
        PlaceCardInSlot(unit, slot, slot.Zone == SlotZone.Frontline ? CardZone.Frontline : CardZone.PlayerSupport);
        SpawnFloatingText("AIRBORNE", slot.transform.position, Color.cyan);
        player.RegisterCardPlayed();
        DrawCard(player);
        ResolveDeploymentEffect(player, unit, slot);

        if (DeployStrikeRules.ShouldTriggerStrike(unit))
        {
            board.TriggerStrike(slot.X, slot.Zone);
        }

        pendingAirborneUnit = null;
        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterDeployPrompt(unit));
        HighlightAirborneDeploymentTargets(null, false);
        ClearSelection();
        RefreshAllViews();
        CardView deployedView = FindView(unit);
        if (deployedView != null)
        {
            deployedView.PlayDeployDrop(selectedView != null ? selectedView.transform.position : HandPosition(PlayerSide.Player, 0, 1), slot.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight);
            deployedView.RefreshKeywordIcons(true);
        }

        pendingDeployDropCardId = null;
    }

    private bool IsEmptyZoneForAirborneDeployment(SlotInteract slot)
    {
        if (slot == null || slot.IsOccupied || BoardTargetRules.IsHeadquartersSlot(slot))
        {
            return false;
        }

        return slot.Zone == SlotZone.PlayerSupport || slot.Zone == SlotZone.Frontline;
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
                GetOpponentState(caster.Side).ApplySignalLostPenalty(1);
                SetStatus(SceneGuidanceRules.AfterOrderPrompt(order));
                break;
            case CardEffectType.FieldIntel:
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

    private void ResolveAreaDamageOrder(RuntimeCard order, SlotInteract targetSlot)
    {
        if (order == null || targetSlot == null || !targetSlot.IsOccupied)
        {
            return;
        }

        DamageUnit(targetSlot.Occupant, order.EffectAmount, order.CardName);

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

        DamageUnit(slot.Occupant, order.EffectAmount, order.CardName);
    }

    private System.Collections.IEnumerator ShowPlayedOrder(RuntimeCard order)
    {
        CardView displayView = CreateTransientCardView(order);
        displayView.SetInteractionEnabled(false);
        displayView.SetDragEnabled(false);
        displayView.SetLayout(
            PlayableSceneRules.OrderDisplayAnchor,
            new Vector3(PlayableSceneRules.OrderDisplayScale, 1f, PlayableSceneRules.OrderDisplayScale),
            Quaternion.identity,
            false);
        displayView.SetDetailPresentation();
        yield return new WaitForSeconds(PlayableSceneRules.OrderDisplaySeconds);
        float elapsed = 0f;
        Vector3 start = displayView.transform.position;
        Vector3 end = DiscardWorldPosition(order.Owner);
        while (elapsed < PlayableSceneRules.OrderFlyOffSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / PlayableSceneRules.OrderFlyOffSeconds);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.24f;
            displayView.transform.position = position;
            displayView.transform.localScale = Vector3.Lerp(
                new Vector3(PlayableSceneRules.OrderDisplayScale, 1f, PlayableSceneRules.OrderDisplayScale),
                new Vector3(PlayableSceneRules.BoardCardScale, 1f, PlayableSceneRules.BoardCardScale),
                t);
            yield return null;
        }

        if (displayView != null)
        {
            RuntimeSafeDestroy.Destroy(displayView.gameObject);
        }
    }

    private bool TryMoveToFrontline(SlotInteract destination)
    {
        if (destination.IsOccupied)
        {
            SetStatus("That frontline slot is occupied.");
            return false;
        }

        if (!CanMoveToFrontline(selectedCard))
        {
            return false;
        }

        if (!player.TrySpendKredits(EffectiveOperationCostForAction(selectedCard)))
        {
            SetStatus(SceneGuidanceRules.CannotAdvancePrompt(selectedCard, player.Kredits));
            return false;
        }

        MoveCardToSlot(selectedCard, destination, CardZone.Frontline);
        selectedCard.HasActed = true;
        SpawnFloatingText("ADVANCE", destination.transform.position, Color.yellow);
        UpdateFrontlineControl();
        SetStatus(SceneGuidanceRules.AfterAdvancePrompt(selectedCard, player.Kredits));
        ClearSelection();
        RefreshAllViews();
        return true;
    }

    private bool CanMoveToFrontline(RuntimeCard card)
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

        if (hasFrontlineController && frontlineController != card.Owner)
        {
            SetStatus("Enemy controls the frontline. Clear it before advancing.");
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
            bool defenderHasGuard = selectedCard != null && HasGuardUnit(GetOpponentState(selectedCard.Owner).Side);
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
        if (attacker == null || attacker.Type != CardType.Unit || attacker.Zone != CardZone.Frontline || attacker.HasActed || attacker.HasKeyword(CardKeyword.Pinned))
        {
            return false;
        }

        if (!CanSpendUnitOperation(attacker, availableKredits, out _))
        {
            return false;
        }

        int maxAttacks = attacker.HasKeyword(CardKeyword.Fury) ? 2 : 1;
        return attacker.AttacksThisTurn < maxAttacks;
    }

    private bool IsLegalAttackTarget(RuntimeCard attacker, SlotInteract targetSlot)
    {
        if (attacker == null || targetSlot == null || attacker.Zone != CardZone.Frontline)
        {
            return false;
        }

        SlotZone enemySupport = attacker.Owner == PlayerSide.Player ? SlotZone.EnemySupport : SlotZone.PlayerSupport;
        if (targetSlot.Zone != enemySupport)
        {
            return false;
        }

        bool enemyHasGuard = HasGuardUnit(GetOpponentState(attacker.Owner).Side);
        if (targetSlot.IsOccupied)
        {
            if (targetSlot.Occupant.Owner == attacker.Owner)
            {
                return false;
            }

            return !enemyHasGuard || targetSlot.Occupant.HasKeyword(CardKeyword.Guard);
        }

        return BoardTargetRules.IsHeadquartersSlot(targetSlot) && !enemyHasGuard;
    }

    private void ResolveAttack(RuntimeCard attacker, SlotInteract targetSlot)
    {
        UnitAttackRules.MarkAttackResolved(attacker);

        PlayerState defender = GetOpponentState(attacker.Owner);
        CountermeasureResult countermeasureResult = TriggerCountermeasure(defender, attacker);
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
            CheckGameOver();
        }

        UpdateFrontlineControl();
    }

    private CountermeasureResult TriggerCountermeasure(PlayerState defender, RuntimeCard attacker)
    {
        if (defender.Countermeasures.Count == 0)
        {
            return new CountermeasureResult();
        }

        RuntimeCard countermeasure = defender.Countermeasures[0];
        defender.Countermeasures.RemoveAt(0);
        defender.Discard.Add(countermeasure);
        countermeasure.Zone = CardZone.Discard;

        CountermeasureResult result = CountermeasureRules.Resolve(countermeasure, attacker);
        if (result.DamageToAttacker > 0 && cardSlots.TryGetValue(attacker, out SlotInteract attackerSlot))
        {
            SpawnFloatingText($"-{result.DamageToAttacker}", attackerSlot.transform.position, Color.magenta, FeedbackCueType.Countermeasure);
        }

        string message = result.DamageToAttacker > 0
            ? $"{defender.Side} countermeasure {countermeasure.CardName} hit {attacker.CardName} for {result.DamageToAttacker}."
            : $"{defender.Side} countermeasure {countermeasure.CardName} stopped {attacker.CardName}.";
        SetStatus(message);
        return result;
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
        ApplyM3AttackDefenseAura(attacker);

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

    private void ApplyM3AttackDefenseAura(RuntimeCard attacker)
    {
        if (attacker == null || attacker.CardName != M3UnitName || !attacker.IsAlive)
        {
            return;
        }

        foreach (RuntimeCard friendly in cardSlots.Keys)
        {
            if (friendly == null || friendly == attacker || friendly.Owner != attacker.Owner)
            {
                continue;
            }

            if (friendly.Zone != CardZone.Frontline && friendly.Zone != CardZone.PlayerSupport)
            {
                continue;
            }

            friendly.CurrentDefense += 1;
            if (cardSlots.TryGetValue(friendly, out SlotInteract friendlySlot))
            {
                SpawnFloatingText("+1 DEF", friendlySlot.transform.position, Color.cyan, FeedbackCueType.Buff);
            }
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
            DestroyCard(target);
            UpdateFrontlineControl();
        }

        CheckGameOver();
    }

    private int ModifiedDamage(int amount, RuntimeCard target)
    {
        return CombatRules.ModifiedDamage(amount, target);
    }

    private void PlaceCardInSlot(RuntimeCard card, SlotInteract slot, CardZone zone)
    {
        slot.SetOccupant(card);
        card.Zone = zone;
        cardSlots[card] = slot;
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
    }

    private void PlayDiscardFlight(RuntimeCard card, Vector3 startPosition)
    {
        if (card == null)
        {
            return;
        }

        CardView flightView = CreateTransientCardView(card);
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

        if (!enemy.TrySpendKredits(card.KreditCost))
        {
            return;
        }

        enemy.Hand.Remove(card);
        UnitDeploymentRules.MarkDeployed(card);
        PlaceCardInSlot(card, slot, slot.Zone == SlotZone.Frontline ? CardZone.Frontline : CardZone.EnemySupport);
        SpawnFloatingText("DEPLOY", slot.transform.position, Color.red);
        ResolveDeploymentEffect(enemy, card, slot);
        DrawCard(enemy);
        SetStatus($"Enemy deployed {card.CardName}.");
        RefreshAllViews();
    }

    private SlotInteract FindEnemyDeploymentSlot(RuntimeCard card)
    {
        if (card != null && card.HasKeyword(CardKeyword.Mobilize) && (!hasFrontlineController || frontlineController == PlayerSide.Enemy))
        {
            SlotInteract frontlineSlot = FindEmptySlot(SlotZone.Frontline);
            if (frontlineSlot != null)
            {
                return frontlineSlot;
            }
        }

        return FindEmptySlot(SlotZone.EnemySupport);
    }

    private void EnemyAdvanceFirstSupportUnit()
    {
        if (hasFrontlineController && frontlineController != PlayerSide.Enemy)
        {
            return;
        }

        foreach (RuntimeCard card in new List<RuntimeCard>(cardSlots.Keys))
        {
            if (card.Owner != PlayerSide.Enemy || card.Zone != CardZone.EnemySupport || !card.CanOperate(enemy.Kredits))
            {
                continue;
            }

            SlotInteract destination = FindEmptySlot(SlotZone.Frontline);
            if (destination == null)
            {
                return;
            }

            if (!enemy.TrySpendKredits(card.OperationCost))
            {
                return;
            }

            MoveCardToSlot(card, destination, CardZone.Frontline);
            card.HasActed = true;
            SpawnFloatingText("ADVANCE", destination.transform.position, Color.yellow);
            UpdateFrontlineControl();
            SetStatus($"Enemy advanced {card.CardName}.");
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

        if (!enemy.TrySpendKredits(card.OperationCost))
        {
            return;
        }

        ResolveAttack(card, target);
        RefreshAllViews();
    }

    private Vector3 CountermeasureFeedbackPosition(PlayerState state)
    {
        int count = state != null ? state.Countermeasures.Count : 1;
        PlayerSide side = state != null ? state.Side : PlayerSide.Player;
        return CountermeasurePosition(side, CardLayoutRules.NewlyAddedIndex(count), Mathf.Max(1, count), 0f);
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
            if (card.Type != CardType.Unit || !enemy.CanSpendKredits(card.KreditCost) || FindEnemyDeploymentSlot(card) == null)
            {
                continue;
            }

            int score = UnitScore(card);
            if (card.HasKeyword(CardKeyword.Mobilize) && (!hasFrontlineController || frontlineController == PlayerSide.Enemy))
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
            if (card.Owner != PlayerSide.Enemy || card.Zone != CardZone.Frontline || !CanAttack(card, enemy.Kredits))
            {
                continue;
            }

            SlotInteract target = FindBestAttackTarget(card);
            if (!IsLegalAttackTarget(card, target))
            {
                continue;
            }

            int score = card.Attack * 5 - card.OperationCost + (target != null && target.IsOccupied ? TargetPriority(target.Occupant) : 8);
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
        SlotZone supportZone = SupportZoneFor(defender);

        SlotInteract bestUnit = null;
        int bestScore = int.MinValue;
        bool mustHitGuard = HasGuardUnit(defender);
        int count = board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, supportZone);
            if (slot == null || !slot.IsOccupied || slot.Occupant.Owner != defender)
            {
                continue;
            }

            if (mustHitGuard && !slot.Occupant.HasKeyword(CardKeyword.Guard))
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
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
            if (slot == null || !slot.IsOccupied || slot.Occupant.Owner != owner)
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
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
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
                return true;
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.DamageTargetUnitAndAdjacent:
            case CardEffectType.PinTargetUnit:
                return slot != null && slot.IsOccupied && slot.Occupant.Owner != caster && !slot.Occupant.HasKeyword(CardKeyword.Smokescreen);
            case CardEffectType.BuffFriendlyUnit:
                return slot != null && slot.IsOccupied && slot.Occupant.Owner == caster;
            default:
                return false;
        }
    }

    private bool OrderNeedsTarget(RuntimeCard order)
    {
        return order.EffectType == CardEffectType.DamageTargetUnit
            || order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent
            || order.EffectType == CardEffectType.PinTargetUnit
            || order.EffectType == CardEffectType.BuffFriendlyUnit;
    }

    private SlotInteract FindEmptySlot(SlotZone zone)
    {
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
            if (slot != null && !slot.IsOccupied && !BoardTargetRules.IsHeadquartersSlot(slot))
            {
                return slot;
            }
        }

        return null;
    }

    private SlotInteract FindOccupiedSlot(SlotZone zone, PlayerSide owner)
    {
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
            if (slot != null && slot.IsOccupied && slot.Occupant.Owner == owner)
            {
                return slot;
            }
        }

        return null;
    }

    private SlotInteract FindGuardSlot(SlotZone zone, PlayerSide owner)
    {
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
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
        bool hasPlayerUnit = false;
        bool hasEnemyUnit = false;
        for (int x = 0; x < board.FrontlineColumns; x++)
        {
            SlotInteract slot = board.GetSlot(x, SlotZone.Frontline);
            if (slot != null && slot.IsOccupied)
            {
                if (slot.Occupant.Owner == PlayerSide.Player)
                {
                    hasPlayerUnit = true;
                }
                else
                {
                    hasEnemyUnit = true;
                }
            }
        }

        FrontlineControlResult control = FrontlineControlRules.Resolve(hasPlayerUnit, hasEnemyUnit);
        hasFrontlineController = control.HasController;
        if (control.HasController)
        {
            frontlineController = control.Controller;
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
            phase = GamePhase.GameOver;
            SetStatus("Draw. Both headquarters are destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
        else if (enemy.HeadquartersHealth <= 0)
        {
            phase = GamePhase.GameOver;
            SetStatus("Victory. Enemy headquarters destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
        else if (player.HeadquartersHealth <= 0)
        {
            phase = GamePhase.GameOver;
            SetStatus("Defeat. Your headquarters was destroyed.");
            ClearSelection();
            StopAllCoroutines();
        }
    }

    private void HighlightLegalTargets(RuntimeCard card, bool highlighted)
    {
        if (highlighted && card.Zone == CardZone.Hand && !player.CanSpendKredits(card.KreditCost))
        {
            return;
        }

        if (card.Zone == CardZone.Hand)
        {
            if (card.Type == CardType.Unit)
            {
                HighlightEmptySlots(SlotZone.PlayerSupport, highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.PlayerSupport));
                if (card.HasKeyword(CardKeyword.Mobilize) && (!hasFrontlineController || frontlineController == card.Owner))
                {
                    HighlightEmptySlots(SlotZone.Frontline, highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.Frontline));
                }
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
        else if (card.Zone == CardZone.PlayerSupport)
        {
            if (UnitActionHighlightRules.ShouldHighlightAdvanceTargets(card, player.Kredits, hasFrontlineController, frontlineController))
            {
                HighlightEmptySlots(SlotZone.Frontline, highlighted, SlotHighlightLabelRules.LabelFor(card, SlotZone.Frontline));
            }
        }
        else if (card.Zone == CardZone.Frontline)
        {
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
        HighlightAttackTargets(attacker, SlotZone.PlayerSupport, highlighted);
        HighlightAttackTargets(attacker, SlotZone.EnemySupport, highlighted);
        HighlightAttackTargets(attacker, SlotZone.Frontline, highlighted);
        HighlightHeadquartersAttackTarget(attacker, highlighted);
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
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        bool defenderHasGuard = attacker != null && HasGuardUnit(GetOpponentState(attacker.Owner).Side);
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
            if (slot != null && IsLegalAttackTarget(attacker, slot))
            {
                string label = SlotHighlightLabelRules.AttackLabelFor(slot.Occupant, defenderHasGuard);
                slot.SetHighlighted(highlighted, label);
            }
        }
    }

    private delegate bool SlotPredicate(SlotInteract slot);

    private void HighlightMatchingSlots(SlotPredicate predicate, bool highlighted)
    {
        HighlightMatchingSlots(predicate, highlighted, PlayableSceneRules.HighlightedSlotLabel);
    }

    private void HighlightMatchingSlots(SlotPredicate predicate, bool highlighted, string label)
    {
        HighlightSlots(SlotZone.PlayerSupport, predicate, highlighted, label);
        HighlightSlots(SlotZone.Frontline, predicate, highlighted, label);
        HighlightSlots(SlotZone.EnemySupport, predicate, highlighted, label);
    }

    private void HighlightSlots(SlotZone zone, SlotPredicate predicate, bool highlighted)
    {
        HighlightSlots(zone, predicate, highlighted, PlayableSceneRules.HighlightedSlotLabel);
    }

    private void HighlightSlots(SlotZone zone, SlotPredicate predicate, bool highlighted, string label)
    {
        int count = zone == SlotZone.Frontline ? board.FrontlineColumns : board.SupportColumns;
        for (int x = 0; x < count; x++)
        {
            SlotInteract slot = board.GetSlot(x, zone);
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

    private void RefreshAllViews()
    {
        List<CardView> previousViews = new List<CardView>(cardViews);
        reusableCardViews = previousViews;
        cardViews.Clear();
        CreateHandViews(player.Hand, PlayerSide.Player);
        CreateHandViews(enemy.Hand, PlayerSide.Enemy);
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
        if (ShouldMutateRuntime())
        {
            SceneHierarchyOrganizer.Organize();
        }
    }

#if UNITY_EDITOR
    private bool ShouldMutateRuntime()
    {
        return Application.isPlaying
            && !UnityEditor.EditorApplication.isCompiling
            && !UnityEditor.EditorApplication.isUpdating
            && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
    }
#else
    private bool ShouldMutateRuntime()
    {
        return Application.isPlaying;
    }
#endif

    private void RefreshPileDisplays()
    {
        pileDisplays.Clear();
        pileDisplays.AddRange(FindObjectsOfType<ScenePileDisplay>());

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
        kreditDisplays.Clear();
        kreditDisplays.AddRange(FindObjectsOfType<SceneKreditDisplay>());

        foreach (SceneKreditDisplay display in kreditDisplays)
        {
            if (display == null)
            {
                continue;
            }

            display.UpdateKredits(GetState(display.Side));
        }
    }

    private void RefreshSceneStatus()
    {
        if (sceneStatusDisplay == null)
        {
            sceneStatusDisplay = FindObjectOfType<SceneStatusDisplay>();
        }

        if (sceneStatusDisplay == null)
        {
            return;
        }

        sceneStatusDisplay.UpdateSnapshot(player, enemy, phase, activeSide, FrontlineLabel(), status, actionLog);
    }

    private void RefreshSceneActionPrompt()
    {
        if (sceneActionPrompt == null)
        {
            sceneActionPrompt = FindObjectOfType<SceneActionPrompt>();
        }

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
        if (sceneCardInspector == null)
        {
            sceneCardInspector = FindObjectOfType<SceneCardInspector>();
        }

        if (sceneCardInspector == null)
        {
            return;
        }

        sceneCardInspector.ShowCard(inspectedCard);
    }

    private void RefreshSceneDeckSummary()
    {
        if (sceneDeckSummary == null)
        {
            sceneDeckSummary = FindObjectOfType<SceneDeckSummary>();
        }

        if (sceneDeckSummary == null)
        {
            return;
        }

        if (phase != GamePhase.DeckBuilder)
        {
            sceneDeckSummary.Clear();
            return;
        }

        int customDeckSize = CurrentCustomDeckSize();
        sceneDeckSummary.UpdateSummary(
            DeckDisplayName(selectedPlayerDeck),
            DeckDescription(selectedPlayerDeck),
            selectedDeckSlot,
            useCustomDeck,
            customDeckSize,
            DeckRules.IsValidDeckSize(customDeckSize),
            DeckRules.MinimumDeckSize);
    }

    private void RefreshSceneCommandButtons()
    {
        sceneCommandButtons.Clear();
        sceneCommandButtons.AddRange(FindObjectsOfType<SceneCommandButton>());

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
        bool hasValidDeck = !useCustomDeck || DeckRules.IsValidDeckSize(CurrentCustomDeckSize());
        return SceneCommandRules.IsAvailable(command, phase, activeSide, isResolvingEvents, board != null, hasValidDeck, mulliganUsed);
    }

    private void HandleCardPointerFallbackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointerFallbackCard = null;
            pointerFallbackActive = false;

            if (CardView.LastDirectMouseDownFrame == Time.frameCount)
            {
                return;
            }

            pointerFallbackCard = FindPointerCardFallback();
            if (pointerFallbackCard != null && pointerFallbackCard.BeginPointerInteraction())
            {
                pointerFallbackActive = true;
                lastSceneCommandPointerFrame = Time.frameCount;
            }
        }

        if (pointerFallbackActive && Input.GetMouseButton(0) && pointerFallbackCard != null)
        {
            if (pointerFallbackCard.DragPointerInteraction())
            {
                lastSceneCommandPointerFrame = Time.frameCount;
            }
        }

        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        if (CardView.LastDirectMouseUpFrame == Time.frameCount)
        {
            pointerFallbackActive = false;
            pointerFallbackCard = null;
            return;
        }

        if (!pointerFallbackActive || pointerFallbackCard == null)
        {
            return;
        }

        pointerFallbackCard.EndPointerInteraction();

        lastSceneCommandPointerFrame = Time.frameCount;
        pointerFallbackActive = false;
        pointerFallbackCard = null;
    }

    private CardView FindPointerCardFallback()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        CardView bestView = null;
        float bestDistance = float.MaxValue;
        foreach (CardView view in cardViews)
        {
            if (view == null || !view.TryPointerScreenDistance(mainCamera, out float distance))
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

        if (sceneCommandButtons.Count == 0)
        {
            sceneCommandButtons.AddRange(FindObjectsOfType<SceneCommandButton>());
        }

        foreach (SceneCommandButton button in sceneCommandButtons)
        {
            if (button == null)
            {
                continue;
            }

            bool available = IsSceneCommandAvailable(button.Command);
            if (IsSceneCommandVisible(button.Command, available) && TryPointerSceneCommandDistance(button, mainCamera, out _))
            {
                return null;
            }
        }

        return null;
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

        if (sceneCommandButtons.Count == 0)
        {
            sceneCommandButtons.AddRange(FindObjectsOfType<SceneCommandButton>());
        }

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

            if (!TryPointerSceneCommandDistance(button, mainCamera, out float distance))
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

    private void CreateHandViews(List<RuntimeCard> hand, PlayerSide side)
    {
        bool mulliganPresentation = MatchStartRules.ShouldUseMulliganPresentation(phase, activeSide) && side == PlayerSide.Player;
        for (int i = 0; i < hand.Count; i++)
        {
            bool hidden = side == PlayerSide.Enemy;
            RuntimeCard runtimeCard = hand[i];
            CardView view = GetOrCreateCardView(runtimeCard, hidden, true);
            Quaternion rotation = mulliganPresentation ? Quaternion.identity : HandRotation(side, i, hand.Count);
            bool centerInspect = centerInspectCard != null && centerInspectCard.Id == runtimeCard.Id;
            Vector3 position = centerInspect
                ? PlayableSceneRules.CenterInspectAnchor
                : (mulliganPresentation ? MulliganHandPosition(i, hand.Count) : HandPosition(side, i, hand.Count));
            position += FocusedHandHoverOffset(hand, side, i, mulliganPresentation);
            float scale = centerInspect
                ? PlayableSceneRules.CenterInspectScale
                : (mulliganPresentation ? PlayableSceneRules.MulliganHandScale : PlayableSceneRules.HandCardScale);
            bool compactHandPresentation = !centerInspect
                && (mulliganPresentation || !(side == PlayerSide.Player && playerHandRevealed));
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
            else if (side == PlayerSide.Player && playerHandRevealed && !centerInspect)
            {
                view.SetRevealedHandPresentation();
            }

            view.SetInteractionEnabled(!mulliganUsed || phase != GamePhase.Mulligan || side != PlayerSide.Player);
            view.SetDragEnabled(!mulliganPresentation);
            view.SetCenterInspectPresentation(centerInspect);
            if (side == PlayerSide.Player)
            {
                view.SetMulliganMarked(MulliganRules.IsMarked(mulliganMarkedIds, runtimeCard));
            }

            if (ConsumePendingDrawAnimation(runtimeCard, out PlayerSide drawSide))
            {
                view.PlayDrawFlight(DeckWorldPosition(drawSide), position);
            }
        }
    }

    private bool ConsumePendingDrawAnimation(RuntimeCard card, out PlayerSide side)
    {
        side = PlayerSide.Player;
        if (card == null)
        {
            return false;
        }

        for (int i = 0; i < pendingDrawAnimations.Count; i++)
        {
            if (pendingDrawAnimations[i].CardId == card.Id)
            {
                side = pendingDrawAnimations[i].Side;
                pendingDrawAnimations.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private Quaternion HandRotation(PlayerSide side, int index, int count)
    {
        float baseRotation = side == PlayerSide.Enemy ? 180f : 0f;
        float fanRotation = 0f;
        return Quaternion.Euler(0f, baseRotation + fanRotation, 0f);
    }

    private Vector3 FocusedHandHoverOffset(List<RuntimeCard> hand, PlayerSide side, int index, bool mulliganPresentation)
    {
        if (side != PlayerSide.Player || mulliganPresentation || string.IsNullOrEmpty(hoveredHandCardId))
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

        if (hoveredIndex < 0 || index == hoveredIndex)
        {
            return Vector3.zero;
        }

        float direction = index < hoveredIndex ? -1f : 1f;
        return Vector3.right * direction * 0.52f;
    }

    private void CreateCountermeasureViews(PlayerState state, float z)
    {
        for (int i = 0; i < state.Countermeasures.Count; i++)
        {
            bool hidden = state.Side == PlayerSide.Enemy;
            CardView view = GetOrCreateCardView(state.Countermeasures[i], hidden, false);
            Quaternion rotation = state.Side == PlayerSide.Enemy ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            view.SetLayout(
                CountermeasurePosition(state.Side, i, state.Countermeasures.Count, z),
                new Vector3(PlayableSceneRules.CountermeasureCardScale, 1f, PlayableSceneRules.CountermeasureCardScale),
                rotation,
                true);
            view.SetInteractionEnabled(true);
            view.SetDragEnabled(false);
        }
    }

    private Vector3 HandPosition(PlayerSide side, int index, int count)
    {
        if (sceneCardLayout != null)
        {
            return sceneCardLayout.HandPosition(side, index, count, playerHandRevealed);
        }

        float spacing = side == PlayerSide.Player && playerHandRevealed
            ? PlayableSceneRules.RevealedHandSpacing
            : PlayableSceneRules.HandSpacing;
        float z = side == PlayerSide.Player
            ? (playerHandRevealed ? PlayableSceneRules.PlayerHandRevealedAnchor.z : PlayableSceneRules.PlayerHandAnchor.z)
            : PlayableSceneRules.EnemyHandAnchor.z;
        if (side == PlayerSide.Player)
        {
            z += CardLayoutRules.HandFanDepthOffset(index, count);
        }

        float y = side == PlayerSide.Player
            ? 0.08f + CardLayoutRules.HandLayerHeightOffset(index)
            : 0.08f;
        return new Vector3(CardLayoutRules.OffsetIndex(index, count) * spacing, y, z);
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
            view.SetLayout(
                pair.Value.transform.position + Vector3.up * PlayableSceneRules.BoardCardHeight,
                new Vector3(PlayableSceneRules.BoardCardScale, 1f, PlayableSceneRules.BoardCardScale),
                Quaternion.identity,
                true);
            view.SetInteractionEnabled(true);
            view.SetDragEnabled(true);
            view.RefreshKeywordIcons(pendingDeployDropCardId == card.Id);
        }
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

    private CardView CreateTransientCardView(RuntimeCard card)
    {
        GameObject cardObject = new GameObject($"PlayedOrder_{card.CardName}");
        CardView view = cardObject.AddComponent<CardView>();
        view.Initialize(card, this, false);
        transientCardViews.Add(view);
        return view;
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

    private SlotInteract ResolvePointerSlot(Vector3 worldPosition, RuntimeCard attacker)
    {
        SlotInteract slot = board.GetSlot(worldPosition);
        if (attacker == null || attacker.Zone != CardZone.Frontline || board == null)
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

    private DamagePreview BuildAttackDamagePreview(RuntimeCard attacker, SlotInteract targetSlot)
    {
        if (attacker == null || targetSlot == null || !IsLegalAttackTarget(attacker, targetSlot))
        {
            return default;
        }

        if (targetSlot.IsOccupied)
        {
            CountermeasureResult countermeasurePrediction = PredictCountermeasureForAttack(attacker);
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

        DamagePreview preview = DamagePreviewRules.ForOrder(order, targetSlot.Occupant);
        if (order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent)
        {
            AddOrderAdjacentDamagePreview(order, targetSlot, out int adjacentTargets, out int adjacentDamage);
            preview.AdjacentTargets = adjacentTargets;
            preview.AdjacentDamage = adjacentDamage;
        }

        return preview;
    }

    private CountermeasureResult PredictCountermeasureForAttack(RuntimeCard attacker)
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

        return CountermeasureRules.Predict(defender.Countermeasures[0], attacker);
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
        adjacentDamage += ModifiedDamage(order.EffectAmount, slot.Occupant);
    }

    private Vector3 MulliganHandPosition(int index, int count)
    {
        float spacing = PlayableSceneRules.MulliganHandSpacing;
        Vector3 anchor = PlayableSceneRules.MulliganHandAnchor;
        return anchor
            + Vector3.right * CardLayoutRules.OffsetIndex(index, count) * spacing
            + Vector3.up * CardLayoutRules.HandLayerHeightOffset(index);
    }

    private Vector3 DeckWorldPosition(PlayerSide side)
    {
        float stackHeight = 0.12f + PlayableSceneRules.PileStackLayerCount * 0.006f;
        return side == PlayerSide.Player
            ? PlayableSceneRules.PlayerDeckPilePosition + Vector3.up * stackHeight
            : PlayableSceneRules.EnemyDeckPilePosition + Vector3.up * stackHeight;
    }

    private Vector3 DiscardWorldPosition(PlayerSide side)
    {
        float stackHeight = 0.14f + PlayableSceneRules.PileStackLayerCount * 0.006f;
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

    private void Shuffle(List<RuntimeCard> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int swapIndex = Random.Range(i, cards.Count);
            RuntimeCard temp = cards[i];
            cards[i] = cards[swapIndex];
            cards[swapIndex] = temp;
        }
    }
}
