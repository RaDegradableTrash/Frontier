using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CardView : MonoBehaviour
{
    private const float CardWidth = 0.96f;
    private const float CardHeight = 1.30f;

    private RuntimeCard card;
    private GameController controller;
    private TMP_Text label;
    private TMP_Text costLabel;
    private TMP_Text attackLabel;
    private TMP_Text defenseLabel;
    private TMP_Text costBadgeLabel;
    private TMP_Text attackBadgeLabel;
    private TMP_Text defenseBadgeLabel;
    private TMP_Text operationLabel;
    private TMP_Text statusLabel;
    private TMP_Text selectionLabel;
    private MeshRenderer faceRenderer;
    private MeshRenderer rarityBandRenderer;
    private MeshRenderer selectionRenderer;
    private MeshRenderer[] selectionFrameRenderers;
    private MeshRenderer dragShadowRenderer;
    private MeshRenderer costBadgeRenderer;
    private MeshRenderer operationBadgeRenderer;
    private CardMotion motion;
    private bool isHidden;
    private bool isDragging;
    private bool hasLayout;
    private bool isHoldingPlayerHandOpen;
    private bool orderHoverAboveHand;
    private bool isSelected;
    private bool isHovered;
    private bool interactionEnabled = true;
    private bool dragEnabled = true;
    private Vector3 dragStartPosition;
    private Color normalColor;
    private GameObject keywordIconColumn;
    private GameObject discardOverlay;
    private MeshRenderer discardOverlayRenderer;
    private CardPrefabTemplate prefabTemplate;
    private bool useHandPrefab;
    private TMP_Text damagePreviewLabel;
    private GameObject damagePreviewSkullObject;
    private MeshRenderer damagePreviewSkullRenderer;
    private readonly System.Collections.Generic.List<GameObject> keywordIconObjects = new System.Collections.Generic.List<GameObject>();
    private Coroutine keywordBlinkRoutine;

    public RuntimeCard Card => card;
    public bool IsHidden => isHidden;
    public bool UsesHandPrefab => useHandPrefab;
    public static int LastDirectMouseDownFrame { get; private set; } = -1;
    public static int LastDirectMouseUpFrame { get; private set; } = -1;

    public void Initialize(RuntimeCard runtimeCard, GameController owner, bool hidden = false)
    {
        Initialize(runtimeCard, owner, hidden, runtimeCard != null && runtimeCard.Zone == CardZone.Hand);
    }

    public void Initialize(RuntimeCard runtimeCard, GameController owner, bool hidden, bool handPrefab)
    {
        card = runtimeCard;
        controller = owner;
        isHidden = hidden;
        useHandPrefab = handPrefab;
        BuildVisuals(hidden);
        Refresh();
    }

    public void Refresh()
    {
        if (card == null || label == null)
        {
            return;
        }

        ApplyDefaultPresentation();
        bool shouldHideFaceText = isHidden || card.Owner == PlayerSide.Enemy && card.Zone == CardZone.Hand;
        label.text = isHidden ? HiddenCardText() : BuildCardText();
        SetTextVisible(label, !shouldHideFaceText);
        if (costLabel != null)
        {
            costLabel.text = shouldHideFaceText ? string.Empty : $"{card.KreditCost}";
            SetTextVisible(costLabel, !shouldHideFaceText);
        }

        if (attackLabel != null)
        {
            attackLabel.text = !shouldHideFaceText && CardTextRules.ShowBattlefieldStats(card) ? $"{card.Attack}" : string.Empty;
            SetTextVisible(attackLabel, !shouldHideFaceText && !string.IsNullOrEmpty(attackLabel.text));
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = !shouldHideFaceText && CardTextRules.ShowBattlefieldStats(card) ? $"{card.CurrentDefense}" : string.Empty;
            SetTextVisible(defenseLabel, !shouldHideFaceText && !string.IsNullOrEmpty(defenseLabel.text));
        }

        if (statusLabel != null)
        {
            statusLabel.text = shouldHideFaceText ? string.Empty : StatusText();
            SetTextVisible(statusLabel, !shouldHideFaceText && !string.IsNullOrEmpty(statusLabel.text));
        }

        if (costBadgeLabel != null)
        {
            costBadgeLabel.text = string.Empty;
            SetTextVisible(costBadgeLabel, false);
        }

        if (attackBadgeLabel != null)
        {
            attackBadgeLabel.text = string.Empty;
            SetTextVisible(attackBadgeLabel, false);
        }

        if (defenseBadgeLabel != null)
        {
            defenseBadgeLabel.text = string.Empty;
            SetTextVisible(defenseBadgeLabel, false);
        }

        if (!shouldHideFaceText)
        {
            RefreshOperationBadge();
        }
    }

    private void SetTextVisible(TMP_Text textMesh, bool visible)
    {
        if (textMesh == null)
        {
            return;
        }

        if (textMesh.gameObject.activeSelf != visible)
        {
            textMesh.gameObject.SetActive(visible);
        }

        Renderer renderer = textMesh.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void SetCardFaceTextVisible(bool visible)
    {
        SetTextVisible(label, visible);
        SetTextVisible(costLabel, visible);
        SetTextVisible(operationLabel, visible);
        SetTextVisible(attackLabel, visible);
        SetTextVisible(defenseLabel, visible);
        SetTextVisible(statusLabel, visible);
        SetTextVisible(costBadgeLabel, visible);
        SetTextVisible(attackBadgeLabel, visible);
        SetTextVisible(defenseBadgeLabel, visible);
    }

    private System.Collections.IEnumerator RevealDrawnCardAfterFlip()
    {
        yield return new WaitForSeconds(CardMotionRules.DrawFlightSeconds * 0.52f);
        Refresh();
    }


    private string HiddenCardText()
    {
        return card.Type == CardType.Countermeasure ? "SET\nCOUNTER" : "ENEMY\nCARD";
    }

    private string BuildCardText()
    {
        if (card.Zone == CardZone.Hand)
        {
            return $"{TypeLabel(card.Type)} {CardTextRules.ShortCardName(card)}";
        }

        if (card.Type == CardType.Unit
            && (card.Zone == CardZone.PlayerSupport
                || card.Zone == CardZone.Frontline
                || card.Zone == CardZone.EnemySupport))
        {
            return CardTextRules.ShortCardName(card);
        }

        return BuildKardsDetailText(14, 2);
    }

    private string BuildKardsDetailText(int maxCharsPerLine, int maxRulesLines)
    {
        string typeLine = card.Type == CardType.Unit
            ? KeywordLine(card)
            : EffectLabel(card.EffectType);
        string keywordLine = KeywordLine(card);
        string rules = WrapText(card.RulesText, maxCharsPerLine, maxRulesLines);
        string text = CardTextRules.DisplayCardName(card);
        if (!string.IsNullOrEmpty(typeLine))
        {
            text = $"{text}\n{typeLine}";
        }

        if (card.Type != CardType.Unit && !string.IsNullOrEmpty(keywordLine))
        {
            text = $"{text}\n{keywordLine}";
        }

        return string.IsNullOrEmpty(rules) ? text : $"{text}\n{rules}";
    }

    private string BuildHandRulesText()
    {
        if (card == null)
        {
            return string.Empty;
        }

        string keywordLine = KeywordLine(card);
        string effectLine = EffectLabel(card.EffectType);
        if (card.EffectAmount > 0 && !string.IsNullOrEmpty(effectLine))
        {
            effectLine = $"{effectLine} {card.EffectAmount}";
        }

        string rulesLine = WrapText(card.RulesText, 12, 2);
        if (card.Type == CardType.Unit)
        {
            if (!string.IsNullOrEmpty(keywordLine) && !string.IsNullOrEmpty(effectLine))
            {
                return $"{keywordLine}\n{effectLine}";
            }

            if (!string.IsNullOrEmpty(keywordLine))
            {
                return keywordLine;
            }

            if (!string.IsNullOrEmpty(effectLine))
            {
                return effectLine;
            }
        }

        if (!string.IsNullOrEmpty(effectLine))
        {
            return string.IsNullOrEmpty(rulesLine) ? effectLine : $"{effectLine}\n{rulesLine}";
        }

        return rulesLine;
    }

    private string StatusText()
    {
        return CardTextRules.StatusLabel(card);
    }

    private static string TypeLabel(CardType type)
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

    private static string EffectLabel(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return "范围伤害";
            case CardEffectType.DeployWithBlitz:
                return "空降部署";
            case CardEffectType.IncreaseEnemyCosts:
                return "费用干扰";
            case CardEffectType.Trap:
                return "战斗反制";
            case CardEffectType.FieldIntel:
                return "情报抽牌";
            case CardEffectType.AddUnitToHand:
                return "加入手牌";
            default:
                return string.Empty;
        }
    }

    private static string KeywordLine(RuntimeCard runtimeCard)
    {
        if (runtimeCard == null || runtimeCard.Keywords == CardKeyword.None)
        {
            return string.Empty;
        }

        string line = string.Empty;
        AppendKeyword(runtimeCard, CardKeyword.Blitz, "闪击", ref line);
        AppendKeyword(runtimeCard, CardKeyword.Guard, "守护", ref line);
        AppendKeyword(runtimeCard, CardKeyword.Smokescreen, "烟幕", ref line);
        AppendKeyword(runtimeCard, CardKeyword.Ambush, "伏击", ref line);
        AppendKeyword(runtimeCard, CardKeyword.Fury, "狂怒", ref line);
        return line;
    }

    private static void AppendKeyword(RuntimeCard runtimeCard, CardKeyword keyword, string labelText, ref string line)
    {
        if (!runtimeCard.HasKeyword(keyword))
        {
            return;
        }

        line = string.IsNullOrEmpty(line) ? labelText : $"{line} / {labelText}";
    }

    private static string WrapText(string text, int maxCharsPerLine, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string cleaned = text.Replace("，", "， ").Replace("。", "。 ");
        string[] words = cleaned.Split(' ');
        string result = string.Empty;
        string line = string.Empty;
        int lines = 0;
        foreach (string rawWord in words)
        {
            string word = rawWord.Trim();
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            if (line.Length + word.Length > maxCharsPerLine && line.Length > 0)
            {
                result += string.IsNullOrEmpty(result) ? line : $"\n{line}";
                lines++;
                if (lines >= maxLines)
                {
                    return $"{result}…";
                }

                line = word;
            }
            else
            {
                line = string.IsNullOrEmpty(line) ? word : $"{line}{word}";
            }
        }

        if (!string.IsNullOrEmpty(line) && lines < maxLines)
        {
            result += string.IsNullOrEmpty(result) ? line : $"\n{line}";
        }

        return result;
    }

    public void SetSelected(bool selected)
    {
        if (faceRenderer != null && prefabTemplate == null)
        {
            faceRenderer.material.color = normalColor;
        }

        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = false;
        }

        SetHoverFrameVisible(isHovered);

        if (selectionLabel != null)
        {
            selectionLabel.text = string.Empty;
            Renderer renderer = selectionLabel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        isSelected = selected;
        motion?.SetSelected(false);
    }

    public void SetHandPresentation(bool prominent)
    {
        if (card == null)
        {
            return;
        }

        if (label != null)
        {
            label.text = $"{TypeLabel(card.Type)} {CardTextRules.ShortCardName(card)}";
        }

        if (costLabel != null)
        {
            costLabel.text = $"{card.KreditCost}";
            SetTextVisible(costLabel, true);
        }

        if (costBadgeLabel != null)
        {
            if (!PlayableSceneRules.HandCardBadgeLabelsEnabled)
            {
                costBadgeLabel.text = string.Empty;
            }
        }

        if (attackBadgeLabel != null)
        {
            attackBadgeLabel.text = string.Empty;
        }

        if (defenseBadgeLabel != null)
        {
            defenseBadgeLabel.text = string.Empty;
        }

        if (attackLabel != null)
        {
            attackLabel.text = card.Type == CardType.Unit ? $"{card.Attack}" : string.Empty;
            SetTextVisible(attackLabel, card.Type == CardType.Unit);
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = card.Type == CardType.Unit ? $"{card.CurrentDefense}" : string.Empty;
            SetTextVisible(defenseLabel, card.Type == CardType.Unit);
        }

        if (statusLabel != null)
        {
            statusLabel.text = BuildHandRulesText();
            SetTextVisible(statusLabel, !string.IsNullOrEmpty(statusLabel.text));
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? $"{card.OperationCost}" : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = false;
        }
    }

    public void SetRevealedHandPresentation()
    {
        if (card == null)
        {
            return;
        }

        if (label != null)
        {
            label.text = $"{TypeLabel(card.Type)} {CardTextRules.ShortCardName(card)}";
        }

        if (costLabel != null)
        {
            costLabel.text = $"{card.KreditCost}";
            SetTextVisible(costLabel, true);
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? $"{card.OperationCost}" : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (attackLabel != null)
        {
            attackLabel.text = card.Type == CardType.Unit ? $"{card.Attack}" : string.Empty;
            SetTextVisible(attackLabel, card.Type == CardType.Unit);
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = card.Type == CardType.Unit ? $"{card.CurrentDefense}" : string.Empty;
            SetTextVisible(defenseLabel, card.Type == CardType.Unit);
        }

        if (statusLabel != null)
        {
            statusLabel.text = BuildHandRulesText();
            SetTextVisible(statusLabel, !string.IsNullOrEmpty(statusLabel.text));
        }
    }

    public void SetDetailPresentation()
    {
        if (card == null || isHidden)
        {
            return;
        }

        if (label != null)
        {
            label.text = BuildKardsDetailText(18, 2);
            SetTextVisible(label, true);
        }

        if (costLabel != null)
        {
            costLabel.text = $"{card.KreditCost}";
            SetTextVisible(costLabel, true);
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? $"{card.OperationCost}" : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (attackLabel != null)
        {
            attackLabel.text = card.Type == CardType.Unit ? $"{card.Attack}" : string.Empty;
            SetTextVisible(attackLabel, card.Type == CardType.Unit);
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = card.Type == CardType.Unit ? $"{card.CurrentDefense}" : string.Empty;
            SetTextVisible(defenseLabel, card.Type == CardType.Unit);
        }
    }

    private void ApplyDefaultPresentation()
    {
    }

    public void SetLayout(Vector3 position, Vector3 scale, Quaternion rotation, bool animate)
    {
        transform.rotation = rotation;
        motion?.SetBaseScale(scale);
        if (CardMotionRules.ShouldAnimateLayout(hasLayout, animate))
        {
            motion?.ResetBasePosition(position);
        }
        else
        {
            transform.localScale = scale;
            transform.position = position;
            motion?.ResetBasePosition(position);
        }

        hasLayout = true;
    }

    public void PlayDeployDrop(Vector3 fromPosition, Vector3 toPosition)
    {
        motion?.PlayDeployDrop(fromPosition, toPosition);
    }

    public void PlayDrawFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        SetCardFaceTextVisible(false);
        motion?.PlayDrawFlight(fromPosition, toPosition);
        StartCoroutine(RevealDrawnCardAfterFlip());
    }

    public void PlayMulliganDiscardFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        motion?.PlayMulliganDiscardFlight(fromPosition, toPosition);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

    public void SetDragEnabled(bool enabled)
    {
        dragEnabled = enabled;
    }

    public void CancelPointerInteraction()
    {
        bool wasDragging = isDragging;
        isDragging = false;
        isHovered = false;
        orderHoverAboveHand = false;
        motion?.SetDragging(false);
        motion?.SetHovered(false);
        SetDragShadowVisible(false);
        SetHoverFrameVisible(false);
        HoldPlayerHandOpen(false);
        if (wasDragging && hasLayout && card != null && card.Zone == CardZone.Hand)
        {
            transform.position = dragStartPosition;
            motion?.ResetBasePosition(dragStartPosition);
        }
        else if (hasLayout)
        {
            motion?.ResetBasePosition(transform.position);
        }
    }

    public void SetMulliganMarked(bool marked)
    {
        EnsureDiscardOverlay();
        if (discardOverlay != null)
        {
            discardOverlay.SetActive(marked);
        }
    }

    public void ShowDamagePreview(int damage, bool lethal)
    {
        EnsureDamagePreviewOverlay();
        if (damagePreviewLabel == null || damagePreviewSkullObject == null)
        {
            return;
        }

        if (lethal)
        {
            damagePreviewLabel.text = string.Empty;
            damagePreviewSkullObject.SetActive(true);
            if (damagePreviewSkullRenderer != null)
            {
                damagePreviewSkullRenderer.enabled = true;
            }

            return;
        }

        if (damage <= 0)
        {
            HideDamagePreview();
            return;
        }

        damagePreviewSkullObject.SetActive(false);
        damagePreviewLabel.text = damage.ToString();
        SetTextVisible(damagePreviewLabel, true);
    }

    public void HideDamagePreview()
    {
        if (damagePreviewLabel != null)
        {
            damagePreviewLabel.text = string.Empty;
            SetTextVisible(damagePreviewLabel, false);
        }

        if (damagePreviewSkullObject != null)
        {
            damagePreviewSkullObject.SetActive(false);
        }
    }

    public void SetCenterInspectPresentation(bool active)
    {
        if (motion == null)
        {
            return;
        }

        motion.SetBaseScale(active
            ? new Vector3(PlayableSceneRules.CenterInspectScale, 1f, PlayableSceneRules.CenterInspectScale)
            : new Vector3(PlayableSceneRules.HandCardScale, 1f, PlayableSceneRules.HandCardScale));
    }

    public void RefreshKeywordIcons(bool blinkOnReveal)
    {
        ClearKeywordIcons();
        if (!CardKeywordIconRules.ShouldShowOnBoard(card))
        {
            return;
        }

        EnsureKeywordIconColumn();
        System.Collections.Generic.List<CardKeyword> keywords = CardKeywordIconRules.VisibleKeywords(card);
        for (int i = 0; i < keywords.Count; i++)
        {
            CreateKeywordIcon(keywords[i], i);
        }

        if (blinkOnReveal && keywordIconColumn != null && gameObject.activeInHierarchy)
        {
            if (keywordBlinkRoutine != null)
            {
                StopCoroutine(keywordBlinkRoutine);
            }

            keywordBlinkRoutine = StartCoroutine(BlinkKeywordIcons());
        }
    }

    private System.Collections.IEnumerator BlinkKeywordIcons()
    {
        for (int pulse = 0; pulse < 2; pulse++)
        {
            SetKeywordIconsVisible(false);
            yield return new WaitForSeconds(0.12f);
            SetKeywordIconsVisible(true);
            yield return new WaitForSeconds(0.12f);
        }

        SetKeywordIconsVisible(true);
        keywordBlinkRoutine = null;
    }

    private void SetKeywordIconsVisible(bool visible)
    {
        foreach (GameObject iconObject in keywordIconObjects)
        {
            if (iconObject != null)
            {
                iconObject.SetActive(visible);
            }
        }
    }

    public void PlayAttackLunge(Vector3 target)
    {
        motion?.PlayAttackLunge(target);
    }

    private void Update()
    {
        UpdateRaycastHover();
    }

    private void OnMouseDown()
    {
        LastDirectMouseDownFrame = Time.frameCount;
        BeginPointerInteraction();
    }

    private void OnMouseDrag()
    {
        DragPointerInteraction();
    }

    private void OnMouseUp()
    {
        LastDirectMouseUpFrame = Time.frameCount;
        EndPointerInteraction();
    }

    public bool BeginPointerInteraction()
    {
        if (!CanInteract())
        {
            return false;
        }

        HoldPlayerHandOpen(true);
        isDragging = false;
        dragStartPosition = transform.position;
        motion?.SetDragging(false);
        return true;
    }

    public bool DragPointerInteraction()
    {
        if (!CanInteract() || !CanDrag())
        {
            return false;
        }

        if (TryGetPointerWorldPosition(out Vector3 pointerPosition))
        {
            isDragging = true;
            motion?.SetDragging(true);
            SetDragShadowVisible(card != null && card.Zone == CardZone.Hand);
            if (card != null && card.Zone == CardZone.Hand)
            {
                transform.rotation = Quaternion.identity;
                if (card.Type == CardType.Order && OrderDragRules.ShouldHoverAboveHand(card))
                {
                    orderHoverAboveHand = true;
                    Vector3 hoverPosition = dragStartPosition
                        + Vector3.up * PlayableSceneRules.DraggedHandCardLift
                        + Vector3.forward * 0.55f;
                    transform.position = hoverPosition;
                    controller?.HandleHandOrderDragPreview(this, pointerPosition);
                }
                else if (card.Type == CardType.Order && OrderDragRules.ShouldFollowPointer(card))
                {
                    orderHoverAboveHand = false;
                    transform.position = pointerPosition
                        + Vector3.up * PlayableSceneRules.DraggedHandCardLift
                        + Vector3.forward * PlayableSceneRules.DraggedHandCardForwardOffset;
                    controller?.HandleHandOrderDragPreview(this, pointerPosition);
                }
                else
                {
                    orderHoverAboveHand = false;
                    transform.position = pointerPosition
                        + Vector3.up * PlayableSceneRules.DraggedHandCardLift
                        + Vector3.forward * PlayableSceneRules.DraggedHandCardForwardOffset;
                }
            }
            else
            {
                controller?.HandleBoardCardDragPreview(this, pointerPosition);
            }

            return true;
        }

        return false;
    }

    public bool EndPointerInteraction()
    {
        if (!CanInteract())
        {
            return false;
        }

        if (!isDragging)
        {
            controller?.HandleCardClicked(this);
            HoldPlayerHandOpen(false);
            return true;
        }

        isDragging = false;
        orderHoverAboveHand = false;
        motion?.SetDragging(false);
        SetDragShadowVisible(false);
        Vector3 releasePosition = transform.position;
        if (card != null && card.Zone == CardZone.Hand)
        {
            if (TryGetPointerWorldPosition(out Vector3 handReleasePosition))
            {
                releasePosition = handReleasePosition;
            }

            transform.position = dragStartPosition;
            motion?.ResetBasePosition(transform.position);
            if (card.Type == CardType.Order)
            {
                controller?.ClearDragPreview();
                controller?.HandleHandOrderReleased(this, releasePosition);
                HoldPlayerHandOpen(false);
                return true;
            }
        }
        else if (TryGetPointerWorldPosition(out Vector3 boardReleasePosition))
        {
            releasePosition = boardReleasePosition;
        }

        controller?.ClearDragPreview();
        controller?.HandleCardReleased(this, releasePosition);
        HoldPlayerHandOpen(false);
        return true;
    }

    private void OnMouseEnter()
    {
    }

    private void OnMouseExit()
    {
    }

    private void UpdateRaycastHover()
    {
        Camera mainCamera = Camera.main;
        bool hoveredNow = CanInteract()
            && !isDragging
            && TryPointerRaycastDistance(mainCamera, Input.mousePosition, out _);

        if (hoveredNow == isHovered)
        {
            return;
        }

        isHovered = hoveredNow;
        SetHoverFrameVisible(hoveredNow);
        motion?.SetHovered(hoveredNow);

        if (hoveredNow)
        {
            HoldPlayerHandOpen(true);
            controller?.HandleCardHovered(this);
        }
        else if (CardInteractionRules.ShouldReleasePlayerHandHold(isDragging) && !isSelected)
        {
            HoldPlayerHandOpen(false);
            controller?.HandleCardHoverEnded(this);
        }
    }

    private void SetHoverFrameVisible(bool visible)
    {
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = false;
        }

        if (selectionFrameRenderers == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in selectionFrameRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void HoldPlayerHandOpen(bool heldOpen)
    {
        if (heldOpen && CardInteractionRules.ShouldHoldPlayerHandOpen(card, isHidden))
        {
            isHoldingPlayerHandOpen = true;
            controller?.SetPlayerHandRevealRequested(true);
        }
        else if (!heldOpen && CardInteractionRules.ShouldReleaseHeldPlayerHand(isHoldingPlayerHandOpen))
        {
            isHoldingPlayerHandOpen = false;
            controller?.SetPlayerHandRevealRequested(false);
        }
    }

    private bool CanInteract()
    {
        return interactionEnabled && !isHidden && card != null && controller != null;
    }

    private bool CanDrag()
    {
        return dragEnabled;
    }

    public bool TryPointerScreenDistance(Camera mainCamera, out float distance)
    {
        return TryPointerRaycastDistance(mainCamera, Input.mousePosition, out distance);
    }

    public bool TryPointerRaycastDistance(Camera mainCamera, Vector3 screenPointer, out float distance)
    {
        distance = float.MaxValue;
        if (!CanInteract() || mainCamera == null)
        {
            return false;
        }

        if (screenPointer.x < 0f || screenPointer.x > Screen.width || screenPointer.y < 0f || screenPointer.y > Screen.height)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPointer);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        float nearestCardDistance = float.MaxValue;
        CardView nearestCard = null;
        foreach (RaycastHit hit in hits)
        {
            CardView hitView = hit.collider != null ? hit.collider.GetComponentInParent<CardView>() : null;
            if (hitView == null || !hitView.CanInteract())
            {
                continue;
            }

            if (hit.distance < nearestCardDistance)
            {
                nearestCardDistance = hit.distance;
                nearestCard = hitView;
            }
        }

        if (nearestCard != this)
        {
            return false;
        }

        distance = nearestCardDistance;
        return true;
    }

    private bool TryGetPointerWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = transform.position;
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Vector3 pointer = Input.mousePosition;
        if (pointer.x < 0f || pointer.x > Screen.width || pointer.y < 0f || pointer.y > Screen.height)
        {
            return false;
        }

        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(pointer);
        if (!boardPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        worldPosition = ray.GetPoint(enter);
        return true;
    }

    private void BuildVisuals(bool hidden)
    {
        transform.localScale = Vector3.one;
        GetComponent<BoxCollider>().size = new Vector3(CardWidth, 0.04f, CardHeight);

        if (TryBuildVisualsFromPrefab(hidden))
        {
            motion = gameObject.AddComponent<CardMotion>();
            return;
        }

        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
        face.name = "Face";
        face.transform.SetParent(transform, false);
        face.transform.localPosition = Vector3.zero;
        face.transform.localScale = new Vector3(CardWidth, 0.03f, CardHeight);
        RuntimeSafeDestroy.Destroy(face.GetComponent<Collider>());

        faceRenderer = face.GetComponent<MeshRenderer>();
        normalColor = hidden ? new Color(0.15f, 0.18f, 0.25f) : FactionColor(card.Faction);
        faceRenderer.material = ResolveFaceMaterial(hidden);
        faceRenderer.material.color = normalColor;

        MeshRenderer cardFrame = CreateInsetPanel("Card Frame", new Vector3(0f, -0.004f, 0f), new Vector3(CardWidth * 1.02f, 0.028f, CardHeight * 1.02f), new Color(0.04f, 0.045f, 0.05f));
        cardFrame.transform.SetSiblingIndex(0);

        dragShadowRenderer = CreateInsetPanel("Drag Shadow", new Vector3(0.045f, -0.018f, -0.045f), new Vector3(CardWidth * 0.98f, 0.010f, CardHeight * 0.98f), new Color(0f, 0f, 0f, 0.55f));
        dragShadowRenderer.enabled = false;
        selectionRenderer = CreateInsetPanel("Selection Glow", new Vector3(0f, 0.049f, 0f), new Vector3(CardWidth * 1.04f, 0.008f, CardHeight * 1.04f), new Color(1f, 1f, 1f, 0.55f));
        selectionRenderer.enabled = false;
        selectionFrameRenderers = CreateSelectionFrame();
        CreateInsetPanel("Art Panel", new Vector3(0f, 0.055f, 0.08f), new Vector3(CardWidth * 0.86f, 0.016f, CardHeight * 0.58f), hidden ? new Color(0.08f, 0.1f, 0.14f) : ArtColor(card));
        CreateArtTexture(hidden);
        CreateInsetPanel("Rules Plate", new Vector3(0f, 0.058f, -CardHeight * 0.18f), new Vector3(CardWidth * 0.86f, 0.012f, CardHeight * 0.42f), hidden ? new Color(0.09f, 0.1f, 0.13f) : new Color(0.86f, 0.80f, 0.66f));
        costBadgeRenderer = CreateInsetPanel("Cost Badge", new Vector3(-CardWidth * 0.39f, 0.066f, CardHeight * 0.43f), new Vector3(0.23f, 0.025f, 0.23f), new Color(0.08f, 0.075f, 0.045f));
        operationBadgeRenderer = CreateInsetPanel("Operation Badge", new Vector3(CardWidth * 0.39f, 0.067f, CardHeight * 0.43f), new Vector3(0.21f, 0.025f, 0.21f), new Color(0.12f, 0.11f, 0.08f));
        operationBadgeRenderer.enabled = false;
        if (!hidden && card.Type == CardType.Unit)
        {
            CreateInsetPanel("Attack Badge", new Vector3(-CardWidth * 0.30f, 0.066f, -CardHeight * 0.43f), new Vector3(0.24f, 0.025f, 0.23f), new Color(0.58f, 0.08f, 0.06f));
            CreateInsetPanel("Defense Badge", new Vector3(CardWidth * 0.30f, 0.066f, -CardHeight * 0.43f), new Vector3(0.24f, 0.025f, 0.23f), new Color(0.06f, 0.22f, 0.58f));
        }

        GameObject rarityBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rarityBand.name = "RarityBand";
        rarityBand.transform.SetParent(transform, false);
        rarityBand.transform.localPosition = new Vector3(0f, 0.052f, CardHeight * 0.45f);
        rarityBand.transform.localScale = new Vector3(CardWidth * 0.92f, 0.012f, 0.055f);
        RuntimeSafeDestroy.Destroy(rarityBand.GetComponent<Collider>());
        rarityBandRenderer = rarityBand.GetComponent<MeshRenderer>();
        rarityBandRenderer.material = CreateRuntimeColorMaterial(hidden ? new Color(0.35f, 0.38f, 0.46f) : RarityColor(card.Rarity));

        GameObject text = new GameObject("Label");
        text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(-CardWidth * 0.40f, 0.084f, CardHeight * 0.28f);
        text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        label = text.AddComponent<TextMeshPro>();
        ConfigureTmpText(label, 0.030f, TextAnchor.UpperLeft);
        label.color = hidden ? Color.white : new Color(0.04f, 0.035f, 0.025f);
        costLabel = CreateCardText("Cost Number", new Vector3(-CardWidth * 0.39f, 0.094f, CardHeight * 0.43f), 0.105f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        operationLabel = CreateCardText("Operation Number", new Vector3(CardWidth * 0.39f, 0.095f, CardHeight * 0.43f), 0.080f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.38f));
        attackLabel = CreateCardText("Attack Number", new Vector3(-CardWidth * 0.30f, 0.094f, -CardHeight * 0.43f), 0.105f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        defenseLabel = CreateCardText("Defense Number", new Vector3(CardWidth * 0.30f, 0.094f, -CardHeight * 0.43f), 0.105f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
        costBadgeLabel = CreateCardText("Cost Badge Label", new Vector3(-CardWidth * 0.39f, 0.097f, CardHeight * 0.28f), 0.024f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        costBadgeLabel.text = string.Empty;
        attackBadgeLabel = CreateCardText("Attack Badge Label", new Vector3(-CardWidth * 0.30f, 0.097f, -CardHeight * 0.27f), 0.024f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        attackBadgeLabel.text = string.Empty;
        defenseBadgeLabel = CreateCardText("Defense Badge Label", new Vector3(CardWidth * 0.30f, 0.097f, -CardHeight * 0.27f), 0.024f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        defenseBadgeLabel.text = string.Empty;
        statusLabel = CreateCardText("Status Label", new Vector3(-CardWidth * 0.38f, 0.096f, -CardHeight * 0.33f), 0.025f, TextAnchor.MiddleLeft, new Color(0.12f, 0.08f, 0.02f));
        selectionLabel = CreateCardText("Selection Label", new Vector3(0f, 0.112f, CardHeight * 0.02f), 0.012f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.015f));
        selectionLabel.text = string.Empty;
        Renderer selectionLabelRenderer = selectionLabel.GetComponent<Renderer>();
        if (selectionLabelRenderer != null)
        {
            selectionLabelRenderer.enabled = false;
        }

        motion = gameObject.AddComponent<CardMotion>();
    }

    private bool TryBuildVisualsFromPrefab(bool hidden)
    {
        string resourcePath = CardPrefabResourcePath();
        CardPrefabTemplate prefab = Resources.Load<CardPrefabTemplate>(resourcePath);
        if (prefab == null)
        {
            return false;
        }

        prefabTemplate = Instantiate(prefab, transform);
        prefabTemplate.name = "Card Visual";
        prefabTemplate.transform.localPosition = Vector3.zero;
        prefabTemplate.transform.localRotation = Quaternion.identity;
        prefabTemplate.transform.localScale = Vector3.one;
        SanitizePrefabMaterials(prefabTemplate);
        ApplyPrefabTextFont(prefabTemplate);

        faceRenderer = prefabTemplate.faceRenderer;
        rarityBandRenderer = prefabTemplate.rarityBandRenderer;
        selectionRenderer = prefabTemplate.selectionRenderer;
        dragShadowRenderer = prefabTemplate.dragShadowRenderer;
        costBadgeRenderer = prefabTemplate.costBadgeRenderer;
        operationBadgeRenderer = prefabTemplate.operationBadgeRenderer;
        selectionFrameRenderers = prefabTemplate.selectionFrameRenderers;
        label = prefabTemplate.titleLabel;
        costLabel = prefabTemplate.costLabel;
        operationLabel = prefabTemplate.operationLabel;
        attackLabel = prefabTemplate.attackLabel;
        defenseLabel = prefabTemplate.defenseLabel;
        costBadgeLabel = prefabTemplate.costBadgeLabel;
        attackBadgeLabel = prefabTemplate.attackBadgeLabel;
        defenseBadgeLabel = prefabTemplate.defenseBadgeLabel;
        statusLabel = prefabTemplate.statusLabel;
        selectionLabel = prefabTemplate.selectionLabel;

        normalColor = hidden ? new Color(0.15f, 0.18f, 0.25f) : FactionColor(card.Faction);
        if (faceRenderer != null && hidden)
        {
            faceRenderer.material = ResolveFaceMaterial(hidden);
            faceRenderer.material.color = normalColor;
        }

        if (rarityBandRenderer != null && hidden)
        {
            rarityBandRenderer.material.color = hidden ? new Color(0.35f, 0.38f, 0.46f) : RarityColor(card.Rarity);
        }

        if (dragShadowRenderer != null)
        {
            dragShadowRenderer.enabled = false;
        }

        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = false;
        }

        if (selectionFrameRenderers != null)
        {
            foreach (MeshRenderer renderer in selectionFrameRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        if (selectionLabel != null)
        {
            selectionLabel.text = string.Empty;
            SetTextVisible(selectionLabel, false);
        }

        return true;
    }

    private void SanitizePrefabMaterials(CardPrefabTemplate template)
    {
        if (template == null)
        {
            return;
        }

        MeshRenderer[] renderers = template.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (renderer.GetComponent<TMP_Text>() != null)
            {
                continue;
            }

            Color color = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color")
                ? renderer.sharedMaterial.color
                : Color.white;
            renderer.material = CreateRuntimeColorMaterial(color);
        }
    }

    private void ApplyPrefabTextFont(CardPrefabTemplate template)
    {
        if (template == null)
        {
            return;
        }

        TMP_Text[] texts = template.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            CardTmpFont.Apply(text);
        }
    }

    private Material CreateRuntimeColorMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private Material CreateRuntimeTextureMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private string CardPrefabResourcePath()
    {
        if (card != null && card.Type == CardType.Unit)
        {
            if (!useHandPrefab
                && (card.Zone == CardZone.PlayerSupport
                    || card.Zone == CardZone.Frontline
                    || card.Zone == CardZone.EnemySupport))
            {
                return "CardPrefabs/UnitCard_Board";
            }

            return "CardPrefabs/UnitCard_Hand";
        }

        if (card != null && card.Type == CardType.Countermeasure)
        {
            return "CardPrefabs/CounterCard_Hand";
        }

        return "CardPrefabs/OrderCard_Hand";
    }

    private TMP_Text CreateCardText(string textName, Vector3 localPosition, float characterSize, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(textName);
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TMP_Text textMesh = textObject.AddComponent<TextMeshPro>();
        ConfigureTmpText(textMesh, characterSize, anchor);
        textMesh.color = color;
        return textMesh;
    }

    private static void ConfigureTmpText(TMP_Text text, float characterSize, TextAnchor anchor)
    {
        text.fontSize = characterSize * 150f;
        text.alignment = TmpAlignment(anchor);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        CardTmpFont.Apply(text);
    }

    private static TextAlignmentOptions TmpAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.Center;
        }
    }

    private MeshRenderer CreateInsetPanel(string panelName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = panelName;
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = localPosition;
        panel.transform.localScale = localScale;
        RuntimeSafeDestroy.Destroy(panel.GetComponent<Collider>());

        MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
        renderer.material = CreateRuntimeColorMaterial(color);
        return renderer;
    }

    private MeshRenderer[] CreateSelectionFrame()
    {
        Color frameColor = new Color(1f, 1f, 1f, 0.94f);
        MeshRenderer top = CreateInsetPanel("Selection Top Edge", new Vector3(0f, 0.086f, CardHeight * 0.505f), new Vector3(CardWidth * 1.03f, 0.012f, 0.018f), frameColor);
        MeshRenderer bottom = CreateInsetPanel("Selection Bottom Edge", new Vector3(0f, 0.086f, -CardHeight * 0.505f), new Vector3(CardWidth * 1.03f, 0.012f, 0.018f), frameColor);
        MeshRenderer left = CreateInsetPanel("Selection Left Edge", new Vector3(-CardWidth * 0.505f, 0.086f, 0f), new Vector3(0.018f, 0.012f, CardHeight * 1.03f), frameColor);
        MeshRenderer right = CreateInsetPanel("Selection Right Edge", new Vector3(CardWidth * 0.505f, 0.086f, 0f), new Vector3(0.018f, 0.012f, CardHeight * 1.03f), frameColor);
        MeshRenderer[] renderers = { top, bottom, left, right };
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        return renderers;
    }

    private void CreateArtTexture(bool hidden)
    {
        Color baseTone = hidden ? new Color(0.16f, 0.19f, 0.26f) : ArtColor(card);
        Color lightTone = Color.Lerp(baseTone, Color.white, hidden ? 0.08f : 0.18f);
        Color darkTone = Color.Lerp(baseTone, Color.black, hidden ? 0.28f : 0.34f);

        CreateInsetPanel("Art Horizon", new Vector3(0f, 0.071f, CardHeight * 0.16f), new Vector3(CardWidth * 0.72f, 0.010f, 0.022f), lightTone);
        CreateInsetPanel("Art Shadow", new Vector3(0f, 0.072f, CardHeight * 0.02f), new Vector3(CardWidth * 0.74f, 0.010f, 0.020f), darkTone);
    }

    private void EnsureDiscardOverlay()
    {
        if (discardOverlay != null)
        {
            return;
        }

        discardOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        discardOverlay.name = "Mulligan Discard Overlay";
        discardOverlay.transform.SetParent(transform, false);
        discardOverlay.transform.localPosition = new Vector3(0f, 0.11f, 0f);
        discardOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        discardOverlay.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        Collider collider = discardOverlay.GetComponent<Collider>();
        if (collider != null)
        {
            RuntimeSafeDestroy.Destroy(collider);
        }

        discardOverlayRenderer = discardOverlay.GetComponent<MeshRenderer>();
        discardOverlayRenderer.material = CreateRuntimeTextureMaterial(Color.white);
        Texture2D discardTexture = SceneIconRegistry.Active != null
            ? SceneIconRegistry.Active.DiscardThisCardIcon
            : Resources.Load<Texture2D>("Icons/DiscardThisCard");
        if (discardTexture != null)
        {
            discardOverlayRenderer.material.mainTexture = discardTexture;
        }

        discardOverlay.SetActive(false);
    }

    private void EnsureDamagePreviewOverlay()
    {
        if (damagePreviewLabel != null && damagePreviewSkullObject != null)
        {
            return;
        }

        damagePreviewLabel = CreateCardText("Damage Preview Label", new Vector3(0f, 0.132f, 0f), 0.13f, TextAnchor.MiddleCenter, new Color(1f, 0.24f, 0.14f));
        SetTextVisible(damagePreviewLabel, false);

        damagePreviewSkullObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        damagePreviewSkullObject.name = "Damage Preview Skull";
        damagePreviewSkullObject.transform.SetParent(transform, false);
        damagePreviewSkullObject.transform.localPosition = new Vector3(0f, 0.134f, 0f);
        damagePreviewSkullObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        damagePreviewSkullObject.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
        Collider collider = damagePreviewSkullObject.GetComponent<Collider>();
        if (collider != null)
        {
            RuntimeSafeDestroy.Destroy(collider);
        }

        damagePreviewSkullRenderer = damagePreviewSkullObject.GetComponent<MeshRenderer>();
        damagePreviewSkullRenderer.material = CreateRuntimeTextureMaterial(Color.white);
        Texture2D skullTexture = SceneIconRegistry.Active != null
            ? SceneIconRegistry.Active.EstimatedDeathSkullIcon
            : Resources.Load<Texture2D>("Icons/EstimatedDeathSkull");
        if (skullTexture != null)
        {
            damagePreviewSkullRenderer.material.mainTexture = skullTexture;
        }

        damagePreviewSkullObject.SetActive(false);
    }

    private void EnsureKeywordIconColumn()
    {
        if (keywordIconColumn != null)
        {
            return;
        }

        keywordIconColumn = new GameObject("Keyword Icon Column");
        keywordIconColumn.transform.SetParent(transform, false);
        keywordIconColumn.transform.localPosition = new Vector3(CardWidth * 0.56f, 0.09f, 0f);
    }

    private void CreateKeywordIcon(CardKeyword keyword, int index)
    {
        GameObject iconObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        iconObject.name = $"Keyword Icon {keyword}";
        iconObject.transform.SetParent(keywordIconColumn.transform, false);
        iconObject.transform.localPosition = new Vector3(0f, 0f, CardHeight * 0.34f - index * 0.18f);
        iconObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        iconObject.transform.localScale = new Vector3(0.14f, 0.14f, 1f);
        Collider collider = iconObject.GetComponent<Collider>();
        if (collider != null)
        {
            RuntimeSafeDestroy.Destroy(collider);
        }

        MeshRenderer renderer = iconObject.GetComponent<MeshRenderer>();
        renderer.material = CreateRuntimeTextureMaterial(Color.white);
        Texture2D iconTexture = KeywordIconTexture(keyword);
        if (iconTexture != null)
        {
            renderer.material.mainTexture = iconTexture;
            renderer.material.color = Color.white;
        }
        else
        {
            renderer.material.color = KeywordIconColor(keyword);
        }
        GameObject labelObject = new GameObject("Keyword Label");
        labelObject.transform.SetParent(iconObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TMP_Text textMesh = labelObject.AddComponent<TextMeshPro>();
        textMesh.text = CardKeywordIconRules.Label(keyword);
        ConfigureTmpText(textMesh, 0.018f, TextAnchor.MiddleCenter);
        textMesh.color = Color.white;
        keywordIconObjects.Add(iconObject);
    }

    private void ClearKeywordIcons()
    {
        foreach (GameObject iconObject in keywordIconObjects)
        {
            if (iconObject != null)
            {
                RuntimeSafeDestroy.Destroy(iconObject);
            }
        }

        keywordIconObjects.Clear();
    }

    private Color KeywordIconColor(CardKeyword keyword)
    {
        switch (keyword)
        {
            case CardKeyword.Blitz:
                return new Color(0.92f, 0.58f, 0.12f);
            case CardKeyword.Fury:
                return new Color(0.82f, 0.18f, 0.12f);
            case CardKeyword.Guard:
                return new Color(0.18f, 0.48f, 0.82f);
            case CardKeyword.Smokescreen:
                return new Color(0.42f, 0.42f, 0.46f);
            case CardKeyword.Ambush:
                return new Color(0.52f, 0.18f, 0.62f);
            case CardKeyword.Mobilize:
                return new Color(0.18f, 0.62f, 0.34f);
            case CardKeyword.HeavyArmor:
                return new Color(0.34f, 0.36f, 0.42f);
            case CardKeyword.Pinned:
                return new Color(0.72f, 0.52f, 0.08f);
            default:
                return new Color(0.3f, 0.3f, 0.3f);
        }
    }

    private Texture2D KeywordIconTexture(CardKeyword keyword)
    {
        switch (keyword)
        {
            case CardKeyword.Guard:
                return Resources.Load<Texture2D>("Icons/Shield") ?? Resources.Load<Texture2D>("Icons/Protected");
            case CardKeyword.Blitz:
            case CardKeyword.Fury:
            case CardKeyword.Ambush:
                return Resources.Load<Texture2D>("Icons/FistFury");
            case CardKeyword.Pinned:
                return Resources.Load<Texture2D>("Icons/EstimatedDeathSkull");
            default:
                return null;
        }
    }

    private void RefreshOperationBadge()
    {
        if (operationLabel != null)
        {
            operationLabel.text = string.Empty;
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = false;
        }

        if (controller == null || isHidden || costLabel == null || costBadgeRenderer == null)
        {
            return;
        }

        int availableKredits = controller.AvailableKreditsFor(card.Owner);
        CardOperationBadgeState state = CardOperationBadgeRules.State(card, availableKredits);
        if (state == CardOperationBadgeState.Hidden)
        {
            return;
        }

        costLabel.text = CardOperationBadgeRules.Text(card, availableKredits);
        if (statusLabel != null && string.IsNullOrEmpty(statusLabel.text))
        {
            statusLabel.text = OperationStateLabel(state);
        }
    }

    private Color OperationBadgeColor(CardOperationBadgeState state)
    {
        switch (state)
        {
            case CardOperationBadgeState.Ready:
                return new Color(0.95f, 0.76f, 0.18f);
            case CardOperationBadgeState.NeedKredits:
                return new Color(0.22f, 0.20f, 0.16f);
            case CardOperationBadgeState.Spent:
                return new Color(0.32f, 0.32f, 0.30f);
            case CardOperationBadgeState.Pinned:
                return new Color(0.62f, 0.44f, 0.06f);
            default:
                return new Color(0.12f, 0.11f, 0.08f);
        }
    }

    private string OperationStateLabel(CardOperationBadgeState state)
    {
        switch (state)
        {
            case CardOperationBadgeState.Ready:
                return "READY";
            case CardOperationBadgeState.NeedKredits:
                return "NEED K";
            case CardOperationBadgeState.Spent:
                return "SPENT";
            case CardOperationBadgeState.Pinned:
                return "PINNED";
            default:
                return string.Empty;
        }
    }

    private void SetDragShadowVisible(bool visible)
    {
        if (dragShadowRenderer != null)
        {
            dragShadowRenderer.enabled = visible;
        }
    }

    private Color ArtColor(RuntimeCard runtimeCard)
    {
        switch (runtimeCard.Type)
        {
            case CardType.Unit:
                return Color.Lerp(FactionColor(runtimeCard.Faction), new Color(0.1f, 0.12f, 0.16f), 0.45f);
            case CardType.Order:
                return new Color(0.18f, 0.16f, 0.34f);
            case CardType.Countermeasure:
                return new Color(0.32f, 0.16f, 0.36f);
            default:
                return new Color(0.18f, 0.18f, 0.2f);
        }
    }

    private Color FactionColor(CardFaction faction)
    {
        switch (faction)
        {
            case CardFaction.USA:
                return new Color(0.72f, 0.84f, 0.95f);
            case CardFaction.Germany:
                return new Color(0.72f, 0.72f, 0.68f);
            case CardFaction.Soviet:
                return new Color(0.9f, 0.66f, 0.62f);
            case CardFaction.Japan:
                return new Color(0.93f, 0.82f, 0.66f);
            default:
                return new Color(0.82f, 0.9f, 0.76f);
        }
    }

    private Material ResolveFaceMaterial(bool hidden)
    {
        if (!hidden && SceneVisualStyle.Active != null)
        {
            Material styledMaterial = SceneVisualStyle.Active.CardMaterialFor(card);
            if (styledMaterial != null)
            {
                return new Material(styledMaterial);
            }
        }

        Color fallbackColor = hidden || card == null ? new Color(0.17f, 0.19f, 0.23f) : ArtColor(card);
        return CreateRuntimeColorMaterial(fallbackColor);
    }

    private Color RarityColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Limited:
                return new Color(0.45f, 0.75f, 1f);
            case CardRarity.Special:
                return new Color(0.78f, 0.45f, 1f);
            case CardRarity.Elite:
                return new Color(1f, 0.68f, 0.2f);
            default:
                return new Color(0.82f, 0.82f, 0.82f);
        }
    }
}
