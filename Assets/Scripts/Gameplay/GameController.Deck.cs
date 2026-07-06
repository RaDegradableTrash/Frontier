using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
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
            ClearCardInspectState();
            hasFrontlineController = false;
            selectedEnemyDeck = DeckArchetype.Endfield;
            BuildDecks();
            DrawOpeningHands();
            SetGamePhase(MatchStartRules.PhaseAfterAutoStart());
            SetStatus("Opening hand: click cards to mark for mulligan, then Mulligan or Keep Hand.");
            RefreshAllViews();
        }

        private void SelectDeckArchetype(DeckArchetype archetype)
        {
            if (selectedPlayerDeck == archetype)
            {
                return;
            }

            selectedPlayerDeck = archetype;
            SetStatus("Selected Endfield Deck.");
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
            int cardsToDraw = Mathf.Max(0, openingHandSize - player.Hand.Count);
            for (int i = 0; i < cardsToDraw; i++)
            {
                DrawCard(player);
            }

            mulliganMarkedIds.Clear();
            ClearCardInspectState();
            CancelAllCardPointerInteractions();
            phase = GamePhase.PlayerTurn;
            activeSide = PlayerSide.Player;
            playerHandRevealGraceUntil = Mathf.Min(playerHandRevealGraceUntil, Time.time);
            SetPlayerHandRevealed(false);
            SetStatus($"Mulligan replaced {markedCards.Count} card(s). Your turn.");
            RefreshAllViews();
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
                if (flightView == null)
                {
                    continue;
                }
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
            centerInspectView = null;
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
            CancelAllCardPointerInteractions();
            ClearDraggedHandCardIfNeeded(false);
            player.HeadquartersHealth = 20;
            enemy.HeadquartersHealth = 20;
            player.MaxKredits = 0;
            player.Kredits = 0;
            enemy.MaxKredits = 0;
            enemy.Kredits = 0;
            mulliganUsed = false;
            mulliganMarkedIds.Clear();
            ClearCardInspectState();
            hasFrontlineController = false;
            playerHandRevealGraceUntil = Mathf.Min(playerHandRevealGraceUntil, Time.time);
            SetPlayerHandRevealed(false);
            selectedEnemyDeck = DeckArchetype.Endfield;
            BuildDecks();
            SetGamePhase(GamePhase.DeckBuilder);
            SetStatus("Choose a starter deck, then start the match.");
            RefreshAllViews();
        }

        private void ClearRuntimeCards()
        {
            inspectedCard = null;
            centerInspectCard = null;
            pendingAirborneOrder = null;
            pendingAirborneUnit = null;
            pendingAirborneSlot = null;
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
            centerInspectCard = null;
            resolutionEvents.Clear();
            isResolvingEvents = false;
            actionLog.Clear();
            player.Deck.Clear();
            enemy.Deck.Clear();

            List<CardData> authoredCards = ActiveCardAssets();
            if (authoredCards.Count > 0)
            {
                AddAssetDeck(player.Deck, PlayerSide.Player, authoredCards);
                AddAssetDeck(enemy.Deck, PlayerSide.Enemy, authoredCards);
            }
            else
            {
                AddStarterDeck(player.Deck, PlayerSide.Player, selectedPlayerDeck);
                AddStarterDeck(enemy.Deck, PlayerSide.Enemy, selectedEnemyDeck);
            }

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

        private void AddAssetDeck(List<RuntimeCard> deck, PlayerSide owner, IList<CardData> assets)
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

        private List<CardData> ActiveCardAssets()
        {
            List<CardData> assets = new List<CardData>();
            if (playerDeckAssets != null)
            {
                for (int i = 0; i < playerDeckAssets.Count; i++)
                {
                    if (playerDeckAssets[i] != null)
                    {
                        assets.Add(playerDeckAssets[i]);
                    }
                }
            }

            if (assets.Count > 0)
            {
                assets.Sort((left, right) => left.id.CompareTo(right.id));
                return assets;
            }

    #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Cards" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    assets.Add(card);
                }
            }

            assets.Sort((left, right) => left.id.CompareTo(right.id));
    #endif
            return assets;
        }

    private RuntimeCard[] StarterTemplates(DeckArchetype archetype)
        {
            List<CardData> authoredCards = ActiveCardAssets();
            if (authoredCards.Count > 0)
            {
                RuntimeCard[] templates = new RuntimeCard[authoredCards.Count];
                for (int i = 0; i < authoredCards.Count; i++)
                {
                    templates[i] = authoredCards[i].ToRuntimeCard(PlayerSide.Player);
                }

                return templates;
            }

            return new[]
            {
                Unit(
                    PerlicaUnitName,
                    "Endfield",
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
                    "Endfield",
                    5,
                    4,
                    6,
                    1,
                    CardKeyword.Blitz,
                    "闪击。"),
                Unit(
                    M3UnitName,
                    "Endfield",
                    6,
                    6,
                    6,
                    2,
                    CardKeyword.Guard | CardKeyword.Smokescreen,
                    "守护。烟幕。攻击时使所有友方目标具有+1防御。"),
                Unit(
                    GilbertaUnitName,
                    "Endfield",
                    4,
                    3,
                    4,
                    2,
                    CardKeyword.Smokescreen,
                    "烟幕。攻击时对相邻敌方目标造成1点伤害。此单位在场时，场上其他友方单位具有-1行动费用。"),
                Order(
                    AirborneOrderName,
                    "Endfield",
                    5,
                    CardEffectType.DeployWithBlitz,
                    0,
                    "选择手牌中的一张单位牌，选择并将其部署于场上任意位置，使其具有闪击。"),
                Order(
                    SignalLostOrderName,
                    "Endfield",
                    2,
                    CardEffectType.IncreaseEnemyCosts,
                    1,
                    "使对方手牌中所有单位牌+1部署费用，对方场上所有单位牌+1行动费用。"),
                Order(
                    DiJiangOrderName,
                    "Endfield",
                    4,
                    CardEffectType.DamageTargetUnitAndAdjacent,
                    5,
                    "对一个敌方目标造成5点伤害，对周围目标造成3点伤害。"),
                Countermeasure(
                    TrapCountermeasureName,
                    "Endfield",
                    3,
                    CardEffectType.Trap,
                    2,
                    "友方单位即将受到攻击时，使其先获得+2+1与伏击。"),
                Countermeasure(
                    FieldIntelCountermeasureName,
                    "Endfield",
                    3,
                    CardEffectType.FieldIntel,
                    0,
                    "敌方回合结束时，抽若干张牌，其数量与本回合内对方打出的手牌数等同。")
            };
        }


        private CardFaction FactionFromNation(string nation)
        {
            return CardFaction.Endfield;
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
                DrawCardInternal(player, false);
                DrawCardInternal(enemy, false);
            }
        }

        private void DrawCard(PlayerState state)
        {
            DrawCardInternal(state, true);
        }

        private void DrawCardInternal(PlayerState state, bool animate)
        {
            if (state.Deck.Count == 0 || state.Hand.Count >= 9)
            {
                return;
            }

            RuntimeCard card = state.Deck[0];
            state.Deck.RemoveAt(0);
            AddCardToHandInternal(state, card, animate);
        }

        private void AddCardToHand(PlayerState state, RuntimeCard card)
        {
            AddCardToHandInternal(state, card, true);
        }

        private void AddCardToHandInternal(PlayerState state, RuntimeCard card, bool animate)
        {
            if (state == null || card == null || state.Hand.Count >= 9)
            {
                return;
            }

            card.Zone = CardZone.Hand;
            int handIndex = state.Hand.Count;
            state.Hand.Add(card);
            if (!animate)
            {
                return;
            }

            pendingDrawAnimations.Add(new PendingDrawAnimation
            {
                CardId = card.Id,
                Side = state.Side,
                HandIndex = handIndex
            });
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
