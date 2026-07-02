using System.Collections.Generic;
using UnityEngine;

public partial class GameController : MonoBehaviour
{
    private const string AirborneOrderName = "空降";
    private const string SignalLostOrderName = "连接丢失";
    private const string TrapCountermeasureName = "诱饵";
    private const string FieldIntelCountermeasureName = "FIELD INTEL";
    private const string DiJiangOrderName = "帝江号，清空区域";
    private const string PerlicaUnitName = "佩丽卡";
    private const string M3UnitName = "M3";
    private const string ChenQianyuUnitName = "陈千语";
    private const string GilbertaUnitName = "洁尔佩塔";

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

    private readonly PlayerState player = new PlayerState(PlayerSide.Player);
    private readonly PlayerState enemy = new PlayerState(PlayerSide.Enemy);
    private readonly List<CardView> cardViews = new List<CardView>();
    private readonly List<CardView> transientCardViews = new List<CardView>();
    private List<CardView> reusableCardViews;
    private readonly List<string> actionLog = new List<string>();
    private readonly Queue<ResolutionEvent> resolutionEvents = new Queue<ResolutionEvent>();
    private readonly Dictionary<RuntimeCard, SlotInteract> cardSlots = new Dictionary<RuntimeCard, SlotInteract>();
    private readonly List<ScenePileDisplay> pileDisplays = new List<ScenePileDisplay>();
    private readonly List<SceneKreditDisplay> kreditDisplays = new List<SceneKreditDisplay>();
    private readonly List<SceneCommandButton> sceneCommandButtons = new List<SceneCommandButton>();
    private readonly HashSet<string> mulliganMarkedIds = new HashSet<string>();
    private readonly HashSet<string> pendingDestroyedUnitIds = new HashSet<string>();
    private readonly RaycastHit[] pointerRaycastHits = new RaycastHit[64];

    private RuntimeCard selectedCard;
    private CardView selectedView;
    private GamePhase phase = GamePhase.DeckBuilder;
    private DeckArchetype selectedPlayerDeck = DeckArchetype.Endfield;
    private DeckArchetype selectedEnemyDeck = DeckArchetype.Endfield;
    private PlayerSide activeSide = PlayerSide.Player;
    private PlayerSide frontlineController = PlayerSide.Player;
    private bool hasFrontlineController;
    private bool mulliganUsed;
    private bool isResolvingEvents;
    private bool playerHandRevealed;
    private bool playerHandRevealRequested;
    private float playerHandRevealGraceUntil;
    private RuntimeCard inspectedCard;
    private RuntimeCard centerInspectCard;
    private CardView centerInspectView;
    private CardView hoveredCardView;
    private string hoveredHandCardId;
    private string pendingDeployDropCardId;
    private readonly List<PendingDrawAnimation> pendingDrawAnimations = new List<PendingDrawAnimation>();
    private RuntimeCard pendingAirborneOrder;
    private RuntimeCard pendingAirborneUnit;
    private SlotInteract pendingAirborneSlot;
    private FeedbackManager feedbackManager;
    private string status = "Choose a starter deck.";
    private DragTargetArrow dragTargetArrow;
    private int lastSceneCommandPointerFrame = -1;
    private CardView pointerFallbackCard;
    private bool pointerFallbackActive;
    private CardView pointerPressedCard;
    private Vector3 pointerPressedScreenPosition;
    private float pointerPressedTime;
    private RuntimeCard lastClickedCard;
    private int lastCardClickHandledFrame = -1;
    private RuntimeCard lastInspectClickedCard;
    private int lastInspectClickHandledFrame = -1;
    private bool ShouldMutateRuntime => Application.isPlaying;

    private struct PendingDrawAnimation
    {
        public string CardId;
        public PlayerSide Side;
        public int HandIndex;
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
        if (autoStartMatch)
        {
            StartNewMatch();
            if (MatchStartRules.ShouldAutoKeepOpeningHand())
            {
                KeepOpeningHand();
            }
            return;
        }

        SetStatus("Choose a starter deck, then start the match.");
        RefreshAllViews();
    }

    private void Update()
    {
        UpdateCardHover();
        UpdateHandReveal();
        HandleUnifiedPlayerHandPointerInput();
        HandleCardPointerFallbackInput();
        HandleHoveredCardClickShortcut();
        HandleSceneCommandPointerInput();

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetStatus(SceneGuidanceRules.HelpPrompt());
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ExecuteSceneCommand(SceneCommandType.SelectDeck);
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

private void EnsurePlayablePresentation()
    {
        PlayableScenePresenter presenter = FindObjectOfType<PlayableScenePresenter>();
        if (presenter == null)
        {
            presenter = gameObject.AddComponent<PlayableScenePresenter>();
        }

        if (Application.isPlaying)
        {
            presenter.EnableRuntimePresentation();
        }
    }

private void KeepOpeningHand()
    {
        mulliganMarkedIds.Clear();
        ClearCardInspectState();
        StartTurn(PlayerSide.Player);
    }

private void ResolveDeploymentEffect(PlayerState owner, RuntimeCard card, SlotInteract slot)
    {
        if (TryResolveAttachedDeploymentRule(owner, card, slot))
        {
            return;
        }

        DeploymentResult result = DeploymentRules.Resolve(card);
        if (!result.Triggered)
        {
            return;
        }

        if (result.GiveCardToHand)
        {
            RuntimeCard template = FindCardTemplateByName(result.CardNameToHand);
            if (template != null)
            {
                RuntimeCard reward = template.CloneFor(owner.Side);
                AddCardToHand(owner, reward);
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
                PlayerState affectedState = GetOpponentState(owner.Side);
                ApplySignalLostToAffectedCards(affectedState, amount);
                FlashSignalLostAffectedCards(affectedState);
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

    private bool TryResolveAttachedDeploymentRule(PlayerState owner, RuntimeCard card, SlotInteract slot)
    {
        if (card == null || card.SpecialRules == null || card.SpecialRules.Length == 0)
        {
            return false;
        }

        CardRuleExecutionContext context = CreateRuleContext(owner, card, slot);
        for (int i = 0; i < card.SpecialRules.Length; i++)
        {
            CardRule rule = card.SpecialRules[i];
            if (rule != null && rule.TryResolveDeployment(context))
            {
                return true;
            }
        }

        return false;
    }



































































































}
