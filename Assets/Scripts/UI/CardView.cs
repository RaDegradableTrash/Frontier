using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CardView : MonoBehaviour
{
    private const float CardHeight = PlayableSceneRules.HandCardHeight;
    private const float CardWidth = PlayableSceneRules.HandCardWidth;
    private const float BoardCardSize = PlayableSceneRules.BoardCardSize;
    private const float DragStartThresholdPixels = 18f;
    private const float DragStartHoldSeconds = 0.02f;
    private const float DraggedHandCardMaxPointerOffsetX = 0.56f;
    private const float DraggedHandCardMaxPointerOffsetZ = 0.34f;

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
    private MeshRenderer primaryArtRenderer;
    private MeshRenderer blurredFrameRenderer;
    private MeshRenderer artFrameRenderer;
    private CardMotion motion;
    private bool isHidden;
    private bool isDragging;
    private bool hasLayout;
    private bool isHoldingPlayerHandOpen;
    private bool isSelected;
    private bool isHovered;
    private bool handBillboardEnabled;
    private bool interactionEnabled = true;
    private bool dragEnabled = true;
    private Vector3 dragStartPosition;
    private Vector3 dragPointerOffsetWorld;
    private Vector3 dragPointerTargetOffsetWorld;
    private Vector3 dragVisualVelocity;
    private Quaternion layoutRotation = Quaternion.identity;
    private Vector3 pointerDownScreenPosition;
    private Vector3 directClickDownScreenPosition;
    private float pointerDownTime;
    private bool directClickHadDrag;
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
    private Coroutine costFlashRoutine;
    private readonly System.Collections.Generic.List<MeshRenderer> boardFuelDotRenderers = new System.Collections.Generic.List<MeshRenderer>();
    private static readonly RaycastHit[] PointerRaycastHits = new RaycastHit[64];
    private static int pointerRaycastFrame = -1;
    private static CardView pointerRaycastNearestCard;
    private static float pointerRaycastNearestDistance;

    public RuntimeCard Card => card;
    public bool IsHidden => isHidden;
    public bool UsesHandPrefab => useHandPrefab;

    private static Material EditableMaterial(Renderer renderer)
    {
        return renderer == null
            ? null
            : Application.isPlaying ? renderer.material : renderer.sharedMaterial;
    }

    private static void SetEditableMaterial(Renderer renderer, Material material)
    {
        if (renderer == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            renderer.material = material;
        }
        else
        {
            renderer.sharedMaterial = material;
        }
    }

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
        if (card == null)
        {
            return;
        }

        ApplyDefaultPresentation();
        bool shouldHideFaceText = isHidden || card.Owner == PlayerSide.Enemy && card.Zone == CardZone.Hand;
        SetHandDetailPanelsVisible(!IsHandCard());
        if (label != null)
        {
            label.text = isHidden ? HiddenCardText() : BuildCardText();
            SetTextVisible(label, !shouldHideFaceText && !IsHandCard());
        }
        if (costLabel != null)
        {
            costLabel.text = shouldHideFaceText ? string.Empty : DisplayKreditCostText();
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
            statusLabel.text = string.Empty;
            SetTextVisible(statusLabel, false);
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

    private string DisplayKreditCostText()
    {
        return controller != null && card != null ? controller.DisplayKreditCostFor(card).ToString() : (card != null ? card.KreditCost.ToString() : string.Empty);
    }

    private void SetTextPanelVisible(TMP_Text textMesh, bool visible)
    {
        if (textMesh == null || textMesh.transform.parent == null)
        {
            return;
        }

        GameObject panel = textMesh.transform.parent.gameObject;
        if (panel != gameObject && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }
    }

    private void SetHandDetailPanelsVisible(bool visible)
    {
        if (prefabTemplate != null)
        {
            return;
        }

        SetTextPanelVisible(label, visible);
        SetTextPanelVisible(statusLabel, visible);
    }

    private string DisplayOperationCostText()
    {
        return controller != null && card != null ? controller.DisplayOperationCostFor(card).ToString() : (card != null ? card.OperationCost.ToString() : string.Empty);
    }

    public void FlashCostChange()
    {
        if (costFlashRoutine != null)
        {
            StopCoroutine(costFlashRoutine);
        }

        costFlashRoutine = StartCoroutine(FlashCostChangeRoutine());
    }

    private System.Collections.IEnumerator FlashCostChangeRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            SetTextVisible(costLabel, false);
            SetTextVisible(operationLabel, false);
            yield return new WaitForSeconds(0.08f);
            Refresh();
            if (costLabel != null && !string.IsNullOrEmpty(costLabel.text))
            {
                SetTextVisible(costLabel, true);
            }
            if (operationLabel != null && !string.IsNullOrEmpty(operationLabel.text))
            {
                SetTextVisible(operationLabel, true);
            }
            yield return new WaitForSeconds(0.08f);
        }

        Refresh();
        costFlashRoutine = null;
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
            Material material = EditableMaterial(faceRenderer);
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = normalColor;
            }
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

    public void PlayMulliganSelectionPulse()
    {
        motion?.Pulse();
    }

    public void SetHandPresentation(bool prominent)
    {
        if (card == null)
        {
            return;
        }

        handBillboardEnabled = true;
        SetHandDetailPanelsVisible(false);

        if (label != null)
        {
            label.text = $"{TypeLabel(card.Type)} {CardTextRules.ShortCardName(card)}";
            SetTextVisible(label, false);
        }

        if (costLabel != null)
        {
            costLabel.text = DisplayKreditCostText();
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
            attackBadgeLabel.text = card.Type == CardType.Unit ? card.Attack.ToString() : string.Empty;
            SetTextVisible(attackBadgeLabel, card.Type == CardType.Unit && PlayableSceneRules.HandCardBadgeLabelsEnabled);
        }

        if (defenseBadgeLabel != null)
        {
            defenseBadgeLabel.text = card.Type == CardType.Unit ? card.CurrentDefense.ToString() : string.Empty;
            SetTextVisible(defenseBadgeLabel, card.Type == CardType.Unit && PlayableSceneRules.HandCardBadgeLabelsEnabled);
        }

        ApplyHandStatLabels();

        if (statusLabel != null)
        {
            statusLabel.text = string.Empty;
            SetTextVisible(statusLabel, false);
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? DisplayOperationCostText() : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = card.Type == CardType.Unit;
        }

    }

    public void SetRevealedHandPresentation()
    {
        if (card == null)
        {
            return;
        }

        handBillboardEnabled = true;
        SetHandDetailPanelsVisible(false);

        if (label != null)
        {
            label.text = $"{TypeLabel(card.Type)} {CardTextRules.ShortCardName(card)}";
            SetTextVisible(label, false);
        }

        if (costLabel != null)
        {
            costLabel.text = DisplayKreditCostText();
            SetTextVisible(costLabel, true);
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? DisplayOperationCostText() : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = card.Type == CardType.Unit;
        }

        ApplyHandStatLabels();

        if (statusLabel != null)
        {
            statusLabel.text = string.Empty;
            SetTextVisible(statusLabel, false);
        }
    }

    public void SetDetailPresentation()
    {
        handBillboardEnabled = false;
        if (card == null || isHidden)
        {
            return;
        }

        SetHandDetailPanelsVisible(false);

        if (label != null)
        {
            label.text = $"{TypeLabel(card.Type)} {CardTextRules.DisplayCardName(card)}";
            SetTextVisible(label, false);
        }

        if (costLabel != null)
        {
            costLabel.text = DisplayKreditCostText();
            SetTextVisible(costLabel, true);
        }

        if (operationLabel != null)
        {
            operationLabel.text = card.Type == CardType.Unit ? DisplayOperationCostText() : string.Empty;
            SetTextVisible(operationLabel, card.Type == CardType.Unit);
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = card.Type == CardType.Unit;
        }

        if (statusLabel != null)
        {
            statusLabel.text = string.Empty;
            SetTextVisible(statusLabel, false);
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

    public void SetBoardUnitPresentation()
    {
        handBillboardEnabled = false;
        if (card == null || card.Type != CardType.Unit)
        {
            return;
        }

        if (label != null)
        {
            label.text = string.Empty;
            SetTextVisible(label, false);
        }

        if (costLabel != null)
        {
            costLabel.text = string.Empty;
            SetTextVisible(costLabel, false);
        }

        if (statusLabel != null)
        {
            statusLabel.text = string.Empty;
            SetTextVisible(statusLabel, false);
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

        if (attackLabel != null)
        {
            attackLabel.text = card.Attack.ToString();
            SetTextVisible(attackLabel, true);
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = card.CurrentDefense.ToString();
            SetTextVisible(defenseLabel, true);
        }

        if (operationLabel != null)
        {
            operationLabel.text = string.Empty;
            SetTextVisible(operationLabel, false);
        }

        RefreshBoardFuelDots();
    }

    private void ApplyHandStatLabels()
    {
        bool showStats = card != null && card.Type == CardType.Unit;
        string attackText = showStats ? card.Attack.ToString() : string.Empty;
        string defenseText = showStats ? card.CurrentDefense.ToString() : string.Empty;

        if (attackLabel != null)
        {
            attackLabel.text = attackText;
            SetTextVisible(attackLabel, showStats);
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = defenseText;
            SetTextVisible(defenseLabel, showStats);
        }

        if (attackBadgeLabel != null)
        {
            attackBadgeLabel.text = attackText;
            SetTextVisible(attackBadgeLabel, showStats && PlayableSceneRules.HandCardBadgeLabelsEnabled);
        }

        if (defenseBadgeLabel != null)
        {
            defenseBadgeLabel.text = defenseText;
            SetTextVisible(defenseBadgeLabel, showStats && PlayableSceneRules.HandCardBadgeLabelsEnabled);
        }
    }

    private void RefreshBoardFuelDots()
    {
        if (card == null || card.Type != CardType.Unit)
        {
            return;
        }

        System.Collections.Generic.List<MeshRenderer> dots = new System.Collections.Generic.List<MeshRenderer>();
        if (prefabTemplate != null)
        {
            MeshRenderer[] allRenderers = prefabTemplate.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                MeshRenderer renderer = allRenderers[i];
                if (renderer != null && renderer.name.StartsWith("FuelDot_", System.StringComparison.Ordinal))
                {
                    dots.Add(renderer);
                }
            }
        }
        else
        {
            dots.AddRange(boardFuelDotRenderers);
        }

        dots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        int fuelCost = Mathf.Clamp(controller != null ? controller.DisplayOperationCostFor(card) : card.OperationCost, 0, dots.Count);
        float squeeze = Mathf.InverseLerp(2f, Mathf.Max(2f, dots.Count), fuelCost);
        float spacing = Mathf.Lerp(0.088f, 0.050f, squeeze);
        for (int i = 0; i < dots.Count; i++)
        {
            bool visible = i < fuelCost;
            dots[i].enabled = visible;
            dots[i].gameObject.SetActive(visible);
            if (visible)
            {
                Vector3 localPosition = dots[i].transform.localPosition;
                localPosition.x = (i - (fuelCost - 1) * 0.5f) * spacing;
                dots[i].transform.localPosition = localPosition;
            }
        }
    }

    private void ApplyDefaultPresentation()
    {
        ApplyOwnerMetalTint();
    }

    public void SetLayout(Vector3 position, Vector3 scale, Quaternion rotation, bool animate)
    {
        motion?.SetBaseScale(scale);
        layoutRotation = rotation;
        if (isDragging)
        {
            hasLayout = true;
            return;
        }

        transform.rotation = rotation;
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
    private void LateUpdate()
    {
        UpdateHandBillboard();
    }

    private void UpdateHandBillboard()
    {
        if (!handBillboardEnabled
            || isDragging
            || motion != null && motion.IsSpecialMoveActive
            || card == null
            || card.Zone != CardZone.Hand)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraDirection = mainCamera.transform.position - transform.position;
        if (cameraDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 baseNormal = layoutRotation * Vector3.up;
        Quaternion fullBillboard = Quaternion.FromToRotation(baseNormal, cameraDirection.normalized) * layoutRotation;
        Quaternion partialBillboard = Quaternion.Slerp(layoutRotation, fullBillboard, PlayableSceneRules.HandBillboardStrength);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            partialBillboard,
            1f - Mathf.Exp(-PlayableSceneRules.HandBillboardLerpSpeed * Time.deltaTime));
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

    public void PlayDrawFlight(Vector3 fromPosition, Vector3 stagedPosition, Vector3 toPosition)
    {
        SetCardFaceTextVisible(false);
        motion?.PlayDrawFlight(fromPosition, stagedPosition, toPosition);
        StartCoroutine(RevealDrawnCardAfterFlip());
    }

    public void PlayMulliganDiscardFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        motion?.PlayMulliganDiscardFlight(fromPosition, toPosition);
    }

    public void PlayFailedReturn(Vector3 fromPosition, Vector3 toPosition)
    {
        if (motion == null)
        {
            return;
        }

        motion.PlayFailedReturn(fromPosition, toPosition);
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

        if (active)
        {
            isHovered = false;
            isDragging = false;
            motion.SetHovered(false);
            motion.SetDragging(false);
            SetDragShadowVisible(false);
            SetHoverFrameVisible(false);
            motion.ResetBasePosition(transform.position);
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

    private void OnMouseDown()
    {
        directClickDownScreenPosition = Input.mousePosition;
        directClickHadDrag = false;
    }

    private void OnMouseDrag()
    {
    }

    private void OnMouseUp()
    {
    }

    private void OnMouseUpAsButton()
    {
        return;
#pragma warning disable CS0162
        if (!CanInteract())
        {
            return;
        }

        if (directClickHadDrag || Vector3.Distance(Input.mousePosition, directClickDownScreenPosition) >= DragStartThresholdPixels)
        {
            return;
        }

        if (card == null || card.Owner != PlayerSide.Player || card.Zone != CardZone.Hand)
        {
            return;
        }

        if (controller != null && controller.ShouldRouteCardClickToMulligan(this))
        {
            controller?.HandleCardClicked(this);
            return;
        }

        controller?.HandleCardInspectRequested(this);
#pragma warning restore CS0162
    }

    public bool BeginPointerInteraction()
    {
        return BeginPointerInteraction(Input.mousePosition, Time.time);
    }

    public bool BeginPointerInteraction(Vector3 initialScreenPosition, float initialTime)
    {
        if (!CanInteract())
        {
            return false;
        }

        isDragging = false;
        directClickHadDrag = false;
        dragStartPosition = transform.position;
        pointerDownScreenPosition = initialScreenPosition;
        pointerDownTime = initialTime;
        dragPointerOffsetWorld = Vector3.zero;
        dragPointerTargetOffsetWorld = Vector3.zero;
        dragVisualVelocity = Vector3.zero;
        if (TryGetPointerWorldPosition(initialScreenPosition, out Vector3 pointerWorldPosition))
        {
            dragPointerOffsetWorld = dragStartPosition - pointerWorldPosition;
            if (card != null && card.Zone == CardZone.Hand)
            {
                dragPointerOffsetWorld.y = 0f;
                dragPointerTargetOffsetWorld = ClampedHandDragPointerOffset(dragPointerOffsetWorld);
            }
            else
            {
                dragPointerTargetOffsetWorld = dragPointerOffsetWorld;
            }
        }

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
            if (!isDragging
                && (Vector3.Distance(Input.mousePosition, pointerDownScreenPosition) < DragStartThresholdPixels
                    || Time.time - pointerDownTime < DragStartHoldSeconds))
            {
                return true;
            }

            bool startingDrag = !isDragging;
            isDragging = true;
            if (startingDrag)
            {
                directClickHadDrag = true;
                dragVisualVelocity = Vector3.zero;
                if (card != null && card.Zone == CardZone.Hand)
                {
                    dragStartPosition = transform.position;
                    dragPointerOffsetWorld = dragStartPosition - pointerPosition;
                    dragPointerOffsetWorld.y = 0f;
                    dragPointerTargetOffsetWorld = ClampedHandDragPointerOffset(dragPointerOffsetWorld);
                }

                controller?.HandleCardDragStarted(this);
            }
            else
            {
                if (card == null || card.Type != CardType.Order || !OrderDragRules.ShouldHoverAboveHand(card))
                {
                    motion?.SetDragging(true);
                }
            }
            SetDragShadowVisible(card != null && card.Zone == CardZone.Hand);
            if (card != null && card.Zone == CardZone.Hand)
            {
                Vector3 handDragPosition = pointerPosition
                    + dragPointerOffsetWorld
                    + Vector3.up * PlayableSceneRules.DraggedHandCardLift;
                Vector3 previousPosition = transform.position;
                Vector3 visualPosition = startingDrag
                    ? handDragPosition
                    : Vector3.SmoothDamp(transform.position, handDragPosition, ref dragVisualVelocity, 0.035f, 60f);
                if (card.Type == CardType.Order && OrderDragRules.ShouldHoverAboveHand(card))
                {
                    Vector3 anchoredPosition = dragStartPosition + Vector3.forward * 0.28f + Vector3.up * 0.08f;
                    transform.position = startingDrag
                        ? anchoredPosition
                        : Vector3.SmoothDamp(transform.position, anchoredPosition, ref dragVisualVelocity, 0.030f, 60f);
                    transform.rotation = Quaternion.identity;
                    controller?.HandleHandOrderDragPreview(this, pointerPosition);
                }
                else if (card.Type == CardType.Order && OrderDragRules.ShouldFollowPointer(card))
                {
                    transform.position = visualPosition;
                    controller?.HandleHandOrderDragPreview(this, pointerPosition);
                }
                else
                {
                    transform.position = visualPosition;
                }

                if (card.Type == CardType.Order && OrderDragRules.ShouldHoverAboveHand(card))
                {
                    transform.rotation = Quaternion.identity;
                }
                else
                {
                    ApplyDragWobble(transform.position - previousPosition);
                }

                if (startingDrag)
                {
                    if (card.Type == CardType.Order && OrderDragRules.ShouldHoverAboveHand(card))
                    {
                        motion?.SetDragging(false);
                    }
                    else
                    {
                        motion?.BeginManualDrag(transform.position);
                    }
                }
            }
            else
            {
                controller?.HandleBoardCardDragPreview(this, pointerPosition);
                if (startingDrag)
                {
                    motion?.BeginManualDrag(transform.position);
                }
            }

            return true;
        }

        return false;
    }

    private static Vector3 ClampedHandDragPointerOffset(Vector3 offset)
    {
        offset.x = Mathf.Clamp(offset.x, -DraggedHandCardMaxPointerOffsetX, DraggedHandCardMaxPointerOffsetX);
        offset.y = 0f;
        offset.z = Mathf.Clamp(offset.z, -DraggedHandCardMaxPointerOffsetZ, DraggedHandCardMaxPointerOffsetZ);
        return offset;
    }

    private void ApplyDragWobble(Vector3 movementDelta)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = movementDelta.magnitude / deltaTime;
        if (speed <= 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, deltaTime * 8f);
            return;
        }

        Vector3 localDirection = transform.InverseTransformDirection(movementDelta.normalized);
        float pitch = Mathf.Clamp(localDirection.z * speed * 10.4976f, -58.32f, 58.32f);
        float roll = Mathf.Clamp(-localDirection.x * speed * 10.4976f, -69.984f, 69.984f);
        float yaw = Mathf.Clamp(-localDirection.x * speed * 4.6656f, -34.992f, 34.992f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, yaw, roll), deltaTime * 12f);
    }

    public bool EndPointerInteraction()
    {
        if (!CanInteract())
        {
            return false;
        }

        bool releasedAsClick = Vector3.Distance(Input.mousePosition, pointerDownScreenPosition) < DragStartThresholdPixels;
        if (!isDragging || releasedAsClick)
        {
            isDragging = false;
            motion?.SetDragging(false);
            SetDragShadowVisible(false);
            transform.rotation = Quaternion.identity;
            HoldPlayerHandOpen(false);
            if (card != null && card.Zone == CardZone.Hand)
            {
                transform.position = dragStartPosition;
                motion?.ResetBasePosition(transform.position);
            }

            return true;
        }

        isDragging = false;
        dragVisualVelocity = Vector3.zero;
        motion?.SetDragging(false);
        SetDragShadowVisible(false);
        transform.rotation = Quaternion.identity;
        Vector3 releasePosition = transform.position;
            if (card != null && card.Zone == CardZone.Hand)
            {
                if (TryGetPointerWorldPosition(out Vector3 handReleasePosition))
                {
                    releasePosition = handReleasePosition;
                }

                if (card.Type == CardType.Order)
                {
                    controller?.ClearDragPreview();
                    controller?.HandleHandOrderReleased(this, releasePosition);
                    HoldPlayerHandOpen(false);
                    return true;
                }

                controller?.ClearDragPreview();
                controller?.HandleCardReleased(this, releasePosition);
                HoldPlayerHandOpen(false);
                return true;
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

    public void SetPointerHovered(bool hoveredNow)
    {
        if (hoveredNow == isHovered)
        {
            return;
        }

        isHovered = hoveredNow;
        SetHoverFrameVisible(hoveredNow);
        motion?.SetHovered(hoveredNow && !IsHandCard());
        if (!hoveredNow && CardInteractionRules.ShouldReleasePlayerHandHold(isDragging) && !isSelected)
        {
            HoldPlayerHandOpen(false);
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

    public bool TryPointerProjectedDistance(Camera mainCamera, Vector3 screenPointer, out float distance)
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

        float cardWidth = CardWidth * Mathf.Abs(transform.lossyScale.x);
        float cardHeight = (IsBoardUnitCard() ? BoardCardSize : CardHeight) * Mathf.Abs(transform.lossyScale.z);
        Vector3 right = transform.right * (cardWidth * 0.5f);
        Vector3 forward = transform.forward * (cardHeight * 0.5f);
        Vector3[] corners =
        {
            mainCamera.WorldToScreenPoint(transform.position - right - forward),
            mainCamera.WorldToScreenPoint(transform.position - right + forward),
            mainCamera.WorldToScreenPoint(transform.position + right - forward),
            mainCamera.WorldToScreenPoint(transform.position + right + forward),
        };

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < corners.Length; i++)
        {
            if (corners[i].z < 0f)
            {
                return false;
            }

            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        const float pointerPaddingPixels = 12f;
        if (screenPointer.x < minX - pointerPaddingPixels || screenPointer.x > maxX + pointerPaddingPixels
            || screenPointer.y < minY - pointerPaddingPixels || screenPointer.y > maxY + pointerPaddingPixels)
        {
            return false;
        }

        Vector3 centerScreen = mainCamera.WorldToScreenPoint(transform.position);
        distance = Vector2.Distance(new Vector2(screenPointer.x, screenPointer.y), new Vector2(centerScreen.x, centerScreen.y));
        return true;
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

        UpdatePointerRaycastCache(mainCamera, screenPointer);
        if (pointerRaycastNearestCard != this)
        {
            return false;
        }

        distance = pointerRaycastNearestDistance;
        return true;
    }

    public static CardView RaycastPointerCard(Camera mainCamera, Vector3 screenPointer)
    {
        if (mainCamera == null)
        {
            return null;
        }

        if (screenPointer.x < 0f || screenPointer.x > Screen.width || screenPointer.y < 0f || screenPointer.y > Screen.height)
        {
            return null;
        }

        UpdatePointerRaycastCache(mainCamera, screenPointer);
        return pointerRaycastNearestCard;
    }

    private static void UpdatePointerRaycastCache(Camera mainCamera, Vector3 screenPointer)
    {
        if (pointerRaycastFrame == Time.frameCount)
        {
            return;
        }

        pointerRaycastFrame = Time.frameCount;
        pointerRaycastNearestCard = null;
        pointerRaycastNearestDistance = float.MaxValue;

        Ray ray = mainCamera.ScreenPointToRay(screenPointer);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            CardView hitView = hit.collider != null ? hit.collider.GetComponentInParent<CardView>() : null;
            if (hitView == null || !hitView.CanInteract())
            {
                continue;
            }

            pointerRaycastNearestDistance = hit.distance;
            pointerRaycastNearestCard = hitView;
            return;
        }
    }

    private bool TryGetPointerWorldPosition(out Vector3 worldPosition)
    {
        return TryGetPointerWorldPosition(Input.mousePosition, out worldPosition);
    }

    private bool TryGetPointerWorldPosition(Vector3 screenPointer, out Vector3 worldPosition)
    {
        worldPosition = transform.position;
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        if (screenPointer.x < 0f || screenPointer.x > Screen.width || screenPointer.y < 0f || screenPointer.y > Screen.height)
        {
            return false;
        }

        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(screenPointer);
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
        GetComponent<BoxCollider>().size = IsBoardUnitCard()
            ? new Vector3(BoardCardSize, 0.04f, BoardCardSize)
            : new Vector3(CardWidth, 0.04f, CardHeight);

        if (TryBuildVisualsFromPrefab(hidden))
        {
            motion = gameObject.GetComponent<CardMotion>() ?? gameObject.AddComponent<CardMotion>();
            return;
        }

        Debug.LogError($"Card prefab build failed for '{card?.CardName ?? "<null>"}'. Ensure prefabs are present in Resources/{CardPrefabResourcePath()}.");
    }

    private void BuildRuntimeHandCard(bool hidden)
    {
        Color frameColor = RuntimeFrameColor();
        Color paperColor = hidden ? new Color(0.20f, 0.22f, 0.26f) : RuntimePaperColor();
        normalColor = paperColor;

        dragShadowRenderer = CreateInsetPanel("Drag Shadow", new Vector3(0.035f, -0.018f, -0.035f), new Vector3(CardWidth * 1.02f, 0.010f, CardHeight * 1.02f), new Color(0f, 0f, 0f, 0.42f));
        dragShadowRenderer.enabled = false;
        CreateInsetPanel("Outer Frame", new Vector3(0f, -0.006f, 0f), new Vector3(CardWidth * 1.02f, 0.018f, CardHeight * 1.02f), frameColor);
        faceRenderer = CreateInsetPanel("Paper Inset", new Vector3(0f, 0.012f, -0.010f), new Vector3(CardWidth * 0.86f, 0.014f, CardHeight * 0.84f), paperColor);
        CreateInsetPanel("Inner Border", new Vector3(0f, 0.024f, 0f), new Vector3(CardWidth * 0.91f, 0.010f, CardHeight * 0.92f), frameColor);

        costBadgeRenderer = CreateInsetPanel("Cost Badge", new Vector3(-CardWidth * 0.365f, 0.060f, CardHeight * 0.405f), new Vector3(0.155f, 0.020f, 0.155f), new Color(0.08f, 0.075f, 0.045f));
        CreateInsetPanel("Title Plate", new Vector3(CardWidth * 0.055f, 0.054f, CardHeight * 0.405f), new Vector3(CardWidth * 0.610f, 0.012f, 0.135f), new Color(0.78f, 0.69f, 0.46f));
        CreateInsetPanel("Flag Plate", new Vector3(CardWidth * 0.380f, 0.055f, CardHeight * 0.405f), new Vector3(0.135f, 0.012f, 0.135f), frameColor);

        float artHeight = CardHeight * PlayableSceneRules.CardArtPanelHeightRatio;
        float artWidth = artHeight * PlayableSceneRules.HandCardAspectRatio;
        float artZ = CardHeight * 0.105f;
        artFrameRenderer = CreateInsetPanel("Art Frame", new Vector3(0f, 0.038f, artZ), new Vector3(artWidth + 0.055f, 0.010f, artHeight + 0.055f), new Color(0.83f, 0.70f, 0.36f));
        primaryArtRenderer = CreateImagePanel("Art Panel", new Vector3(0f, 0.052f, artZ), artWidth, artHeight, hidden ? new Color(0.10f, 0.12f, 0.16f) : ArtColor(card));
        ApplyRuntimeCardArtwork(primaryArtRenderer, hidden);

        CreateInsetPanel("Rules Plate", new Vector3(0f, 0.042f, -CardHeight * 0.285f), new Vector3(CardWidth * 0.805f, 0.012f, CardHeight * 0.205f), new Color(0.86f, 0.80f, 0.66f));
        rarityBandRenderer = CreateInsetPanel("Rarity Band", new Vector3(0f, 0.052f, -CardHeight * 0.475f), new Vector3(CardWidth * 0.135f, 0.010f, 0.030f), new Color(0.94f, 0.66f, 0.20f));

        if (!hidden && card.Type == CardType.Unit)
        {
            CreateInsetPanel("Attack Badge", new Vector3(-CardWidth * 0.285f, 0.052f, -CardHeight * 0.195f), new Vector3(0.165f, 0.022f, 0.165f), new Color(0.55f, 0.08f, 0.06f));
            operationBadgeRenderer = CreateInsetPanel("Operation Badge", new Vector3(0f, 0.054f, -CardHeight * 0.195f), new Vector3(0.170f, 0.022f, 0.165f), new Color(0.10f, 0.10f, 0.085f));
            CreateInsetPanel("Defense Badge", new Vector3(CardWidth * 0.285f, 0.052f, -CardHeight * 0.195f), new Vector3(0.165f, 0.022f, 0.165f), new Color(0.06f, 0.18f, 0.50f));
        }
        else
        {
            operationBadgeRenderer = null;
        }

        selectionRenderer = CreateInsetPanel("Selection Glow", new Vector3(0f, 0.070f, 0f), new Vector3(CardWidth * 1.04f, 0.008f, CardHeight * 1.04f), new Color(1f, 1f, 1f, 0.45f));
        selectionRenderer.enabled = false;
        selectionFrameRenderers = CreateSelectionFrame();

        label = CreateCardText("Title Label", new Vector3(CardWidth * 0.055f, 0.085f, CardHeight * 0.405f), 0.024f, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.035f));
        costLabel = CreateCardText("Cost Number", new Vector3(-CardWidth * 0.365f, 0.090f, CardHeight * 0.405f), 0.064f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        operationLabel = CreateCardText("Operation Number", new Vector3(0f, 0.088f, -CardHeight * 0.195f), 0.052f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        attackLabel = CreateCardText("Attack Number", new Vector3(-CardWidth * 0.285f, 0.088f, -CardHeight * 0.195f), 0.058f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        defenseLabel = CreateCardText("Defense Number", new Vector3(CardWidth * 0.285f, 0.088f, -CardHeight * 0.195f), 0.058f, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        statusLabel = CreateCardText("Rules Label", new Vector3(0f, 0.080f, -CardHeight * 0.285f), 0.022f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.035f));
        selectionLabel = CreateCardText("Selection Label", new Vector3(0f, 0.112f, 0f), 0.012f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.015f));
    }

    private void BuildRuntimeBoardCard(bool hidden)
    {
        float boardSize = BoardCardSize;
        Color frame = new Color(0.84f, 0.84f, 0.82f);
        Color interior = hidden ? new Color(0.20f, 0.22f, 0.26f) : new Color(0.98f, 0.98f, 0.97f);
        normalColor = interior;

        dragShadowRenderer = CreateInsetPanel("Drag Shadow", new Vector3(0.035f, -0.018f, -0.035f), new Vector3(boardSize * 1.02f, 0.010f, boardSize * 1.02f), new Color(0f, 0f, 0f, 0.42f));
        dragShadowRenderer.enabled = false;
        CreateInsetPanel("Outer Frame", new Vector3(0f, 0.020f, 0f), new Vector3(boardSize * 0.985f, 0.010f, boardSize * 0.985f), frame);
        faceRenderer = CreateInsetPanel("Interior", new Vector3(0f, 0.030f, 0.040f), new Vector3(boardSize * 0.900f, 0.010f, boardSize * 0.860f), interior);
        primaryArtRenderer = CreateImagePanel("Board Art", new Vector3(0f, 0.046f, boardSize * 0.145f), boardSize * 0.720f, boardSize * 0.720f, hidden ? new Color(0.12f, 0.14f, 0.16f) : new Color(0.98f, 0.98f, 0.97f));
        ApplyRuntimeCardArtwork(primaryArtRenderer, hidden);
        CreateArcPanel("Lower Left Arc", new Vector3(-boardSize * 0.455f, 0.058f, -boardSize * 0.455f), boardSize * 0.285f, boardSize * 0.285f, 0f, 90f, frame);
        CreateArcPanel("Lower Right Arc", new Vector3(boardSize * 0.455f, 0.058f, -boardSize * 0.455f), boardSize * 0.285f, boardSize * 0.285f, 90f, 180f, frame);
        rarityBandRenderer = CreateInsetPanel("Rarity Band", new Vector3(0f, 0.062f, -boardSize * 0.475f), new Vector3(boardSize * 0.100f, 0.006f, 0.022f), frame);

        boardFuelDotRenderers.Clear();
        for (int i = 0; i < 8; i++)
        {
            MeshRenderer dot = CreateEllipsePanel($"FuelDot_{i}", new Vector3(FuelDotX(i, 2), 0.070f, -boardSize * 0.355f), boardSize * 0.050f, boardSize * 0.050f, new Color(0.70f, 0.70f, 0.68f));
            dot.enabled = false;
            boardFuelDotRenderers.Add(dot);
        }

        selectionRenderer = CreateInsetPanel("Selection Glow", new Vector3(0f, 0.078f, 0f), new Vector3(boardSize * 1.04f, 0.008f, boardSize * 1.04f), new Color(1f, 1f, 1f, 0.45f));
        selectionRenderer.enabled = false;
        selectionFrameRenderers = CreateSelectionFrame();

        label = CreateCardText("Title Label", new Vector3(0f, 0.092f, boardSize * 0.405f), 0.018f, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.035f));
        costLabel = CreateCardText("Cost Number", new Vector3(0f, 0.092f, boardSize * 0.405f), 0.001f, TextAnchor.MiddleCenter, Color.clear);
        attackLabel = CreateCardText("Attack Number", new Vector3(-boardSize * 0.350f, 0.096f, -boardSize * 0.380f), 0.075f, TextAnchor.MiddleCenter, new Color(0.02f, 0.02f, 0.018f));
        operationLabel = CreateCardText("Operation Number", new Vector3(0f, 0.096f, -boardSize * 0.380f), 0.001f, TextAnchor.MiddleCenter, Color.clear);
        defenseLabel = CreateCardText("Defense Number", new Vector3(boardSize * 0.350f, 0.096f, -boardSize * 0.380f), 0.075f, TextAnchor.MiddleCenter, new Color(0.02f, 0.02f, 0.018f));
        statusLabel = CreateCardText("Rules Label", new Vector3(0f, 0.092f, -boardSize * 0.050f), 0.001f, TextAnchor.MiddleCenter, Color.clear);
        selectionLabel = CreateCardText("Selection Label", new Vector3(0f, 0.112f, 0f), 0.012f, TextAnchor.MiddleCenter, new Color(0.08f, 0.055f, 0.015f));
    }

    private bool IsBoardUnitCard()
    {
        return card != null
            && card.Type == CardType.Unit
            && !useHandPrefab
            && (card.Zone == CardZone.PlayerSupport
                || card.Zone == CardZone.Frontline
                || card.Zone == CardZone.EnemySupport);
    }

    private bool IsHandCard()
    {
        return card != null && card.Zone == CardZone.Hand;
    }

    private string BuildDetailRulesText()
    {
        if (card == null)
        {
            return string.Empty;
        }

        string keywordLine = KeywordLine(card);
        string rulesText = WrapText(card.RulesText, 18, 4);
        if (string.IsNullOrEmpty(keywordLine))
        {
            return rulesText;
        }

        return string.IsNullOrEmpty(rulesText) ? keywordLine : $"{keywordLine}\n{rulesText}";
    }


    private bool TryBuildVisualsFromPrefab(bool hidden)
    {
        string resourcePath = CardPrefabResourcePath();
        CardPrefabTemplate prefab = Resources.Load<CardPrefabTemplate>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Missing card prefab template '{resourcePath}' for card {card?.CardName ?? "<null>"}.");
            return false;
        }

        prefabTemplate = Instantiate(prefab, transform);
        prefabTemplate.name = "Card Visual";
        prefabTemplate.transform.localPosition = Vector3.zero;
        prefabTemplate.transform.localRotation = Quaternion.identity;
        prefabTemplate.transform.localScale = Vector3.one;
        RemoveChildColliders(prefabTemplate.transform);
        ApplyOwnerMetalTint();

        faceRenderer = prefabTemplate.faceRenderer;
        rarityBandRenderer = prefabTemplate.rarityBandRenderer;
        selectionRenderer = prefabTemplate.selectionRenderer;
        dragShadowRenderer = prefabTemplate.dragShadowRenderer;
        costBadgeRenderer = prefabTemplate.costBadgeRenderer;
        operationBadgeRenderer = prefabTemplate.operationBadgeRenderer;
        primaryArtRenderer = prefabTemplate.primaryArtRenderer;
        blurredFrameRenderer = prefabTemplate.blurredFrameRenderer;
        artFrameRenderer = prefabTemplate.artFrameRenderer;
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
        BindMissingPrefabTextLabels();

        ClearPrefabDefaultArtwork();

        normalColor = hidden ? new Color(0.15f, 0.18f, 0.25f) : FactionColor(card.Faction);
        if (faceRenderer != null && hidden)
        {
            Material material = ResolveFaceMaterial(hidden);
            if (Application.isPlaying)
            {
                faceRenderer.material = material;
            }
            else
            {
                faceRenderer.sharedMaterial = material;
            }

            material = EditableMaterial(faceRenderer);
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = normalColor;
            }
        }

        if (rarityBandRenderer != null && hidden)
        {
            Material material = EditableMaterial(rarityBandRenderer);
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = hidden ? new Color(0.35f, 0.38f, 0.46f) : RarityColor(card.Rarity);
            }
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

        ApplyPrefabRuntimeVisibility();
        ApplyCardNameArtwork();
        return true;
    }

    private void ClearPrefabDefaultArtwork()
    {
        if (primaryArtRenderer != null)
        {
            SetEditableMaterial(primaryArtRenderer, CreateRuntimeTextureMaterial(new Color(0.08f, 0.095f, 0.11f, 1f)));
        }

        if (blurredFrameRenderer != null)
        {
            SetEditableMaterial(blurredFrameRenderer, CreateRuntimeTextureMaterial(Color.clear));
        }

        if (artFrameRenderer != null)
        {
            SetEditableMaterial(artFrameRenderer, CreateRuntimeTextureMaterial(Color.clear));
        }
    }

    private void ApplyPrefabRuntimeVisibility()
    {
        if (prefabTemplate == null)
        {
            return;
        }

        Transform stats = FindChildRecursive(prefabTemplate.transform, "Stats");
        if (stats != null)
        {
            stats.gameObject.SetActive(card != null && card.Type == CardType.Unit);
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void BindMissingPrefabTextLabels()
    {
        if (prefabTemplate == null)
        {
            return;
        }

        if (attackLabel == null)
        {
            attackLabel = FindPrefabText("AttackNumber", "AttackValue", "AttackBadge");
        }

        if (defenseLabel == null)
        {
            defenseLabel = FindPrefabText("DefenseNumber", "DefenseValue", "DefenseBadge", "DefenceBadge");
        }

        if (costLabel == null)
        {
            costLabel = FindPrefabText("CostNumber");
        }

        if (operationLabel == null)
        {
            operationLabel = FindPrefabText("OperationNumber");
        }
    }

    private TMP_Text FindPrefabText(params string[] candidateNames)
    {
        for (int i = 0; i < candidateNames.Length; i++)
        {
            Transform candidate = FindChildRecursive(prefabTemplate.transform, candidateNames[i]);
            if (candidate == null)
            {
                continue;
            }

            TMP_Text directText = candidate.GetComponent<TMP_Text>();
            if (directText != null)
            {
                return directText;
            }

            TMP_Text childText = candidate.GetComponentInChildren<TMP_Text>(true);
            if (childText != null)
            {
                return childText;
            }
        }

        return null;
    }

    private void RemoveChildColliders(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] childColliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < childColliders.Length; i++)
        {
            Collider collider = childColliders[i];
            if (collider != null && collider.transform != transform)
            {
                RuntimeSafeDestroy.Destroy(collider);
            }
        }
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
            Texture texture = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_MainTex")
                ? renderer.sharedMaterial.mainTexture
                : null;
            SetEditableMaterial(renderer, texture != null
                ? CreateRuntimeTextureMaterial(color, texture)
                : CreateRuntimeColorMaterial(color));
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
        return CreateRuntimeTextureMaterial(color, null);
    }

    private Material CreateRuntimeTextureMaterial(Color color, Texture texture)
    {
        Shader shader = Shader.Find("Unlit/Texture");
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
        if (texture != null)
        {
            material.mainTexture = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
        }
        return material;
    }

    private void ApplyCardNameArtwork()
    {
        if (card == null || prefabTemplate == null || isHidden)
        {
            return;
        }

        string artKey = ResolveArtworkKeyForCard(card, IsBoardUnitCard());
        Texture2D artTexture = Resources.Load<Texture2D>($"CardArt/{artKey}");

        if (artTexture != null)
        {
            ApplyTexture(primaryArtRenderer, artTexture);
        }
    }

    private void ApplyTexture(MeshRenderer renderer, Texture2D texture)
    {
        if (renderer == null || texture == null)
        {
            return;
        }

        Material currentMaterial = EditableMaterial(renderer);
        Color color = currentMaterial != null && currentMaterial.HasProperty("_Color")
            ? currentMaterial.color
            : Color.white;
        SetEditableMaterial(renderer, CreateRuntimeTextureMaterial(color, texture));
    }

    private static string ResolveArtworkKeyForCard(RuntimeCard runtimeCard, bool forBoardCard)
    {
        if (runtimeCard == null)
        {
            return string.Empty;
        }

        string keyByCardName = CardArtworkKey(runtimeCard.CardName, forBoardCard);
        string resolvedByCardName = EnsureExistingArtworkKey(keyByCardName);
        if (!string.IsNullOrEmpty(resolvedByCardName))
        {
            return resolvedByCardName;
        }

        if (runtimeCard.Artwork != null)
        {
            string boundKey = ResolveArtworkKeyForTextureName(runtimeCard.Artwork.name, forBoardCard);
            if (!string.IsNullOrEmpty(boundKey))
            {
                string existingBoundKey = EnsureExistingArtworkKey(boundKey);
                if (!string.IsNullOrEmpty(existingBoundKey))
                {
                    return existingBoundKey;
                }
            }
        }

        return string.Empty;
    }

    public static string ResolveArtworkKey(RuntimeCard runtimeCard, bool forBoardCard)
    {
        return ResolveArtworkKeyForCard(runtimeCard, forBoardCard);
    }

    public static Texture2D ResolveArtworkTexture(RuntimeCard runtimeCard, bool forBoardCard)
    {
        string key = ResolveArtworkKey(runtimeCard, forBoardCard);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Resources.Load<Texture2D>($"CardArt/{key}");
    }

    private static string EnsureExistingArtworkKey(string artworkKey)
    {
        if (string.IsNullOrWhiteSpace(artworkKey))
        {
            return string.Empty;
        }

        if (Resources.Load<Texture2D>($"CardArt/{artworkKey}") != null)
        {
            return artworkKey;
        }

        return string.Empty;
    }

    private static string ResolveArtworkKeyForTextureName(string textureName, bool forBoardCard)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return string.Empty;
        }

        string handKey = textureName;
        string boardKey = $"{textureName}_Avator";

        if (forBoardCard)
        {
            string explicitBoard = textureName.EndsWith("_Avator", System.StringComparison.OrdinalIgnoreCase)
                ? textureName
                : $"{textureName}_Avator";
            if (Resources.Load<Texture2D>($"CardArt/{explicitBoard}") != null)
            {
                return explicitBoard;
            }
        }
        else if (textureName.EndsWith("_Avator", System.StringComparison.OrdinalIgnoreCase))
        {
            handKey = textureName.Substring(0, textureName.Length - "_Avator".Length);
            if (Resources.Load<Texture2D>($"CardArt/{handKey}") != null)
            {
                return handKey;
            }
        }

        return Resources.Load<Texture2D>($"CardArt/{textureName}") != null
            ? textureName
            : (forBoardCard ? boardKey : handKey);
    }

    private static string CardArtworkKey(string cardName, bool forBoardCard)
    {
        string normalized = NormalizeArtworkKey(cardName);
        if (forBoardCard && !normalized.EndsWith("_avator"))
        {
            string boardPreferred = $"{normalized}_Avator";
            if (Resources.Load<Texture2D>($"CardArt/{boardPreferred}") != null)
            {
                return boardPreferred;
            }
        }

        if (normalized.Contains("perlica") || normalized.Contains("佩丽卡"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "Perlica_Avator", "Perlica" } : new[] { "Perlica" }, forBoardCard ? "Perlica_Avator" : "Perlica");
        }

        if (normalized.Contains("chenqianyu") || normalized.Contains("陈千语"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "Chenqianyu_Avator", "Chenqianyu" } : new[] { "Chenqianyu", "ChenQianyu" }, forBoardCard ? "Chenqianyu_Avator" : "Chenqianyu");
        }

        if (normalized.Contains("gilberta") || normalized.Contains("洁尔佩塔"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "Gilberta_Avator", "Gilberta" } : new[] { "Gilberta" }, forBoardCard ? "Gilberta_Avator" : "Gilberta");
        }

        if (normalized.Contains("signallost") || normalized.Contains("连接丢失"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "FieldIntel_Avator", "FieldIntel" } : new[] { "FieldIntel" }, forBoardCard ? "FieldIntel_Avator" : "FieldIntel");
        }

        if (normalized.Contains("omvdijiang") || normalized.Contains("dijiang") || normalized.Contains("帝江号"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "DijiangClearTheArea_Avator", "DijiangClearTheArea" } : new[] { "DijiangClearTheArea" }, forBoardCard ? "DijiangClearTheArea_Avator" : "DijiangClearTheArea");
        }

        if (normalized.Contains("fieldintel"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "FieldIntel_Avator", "FieldIntel" } : new[] { "FieldIntel" }, forBoardCard ? "FieldIntel_Avator" : "FieldIntel");
        }

        if (normalized.Contains("airborne") || normalized.Contains("空降"))
        {
            return FirstExistingArtworkKey(forBoardCard ? new[] { "Airborne_Avator", "M3_Avator", "M3" } : new[] { "Airborne", "M3" }, forBoardCard ? "M3_Avator" : "M3");
        }

        if (normalized.Contains("trap") || normalized.Contains("诱饵"))
        {
            return "Trap";
        }

        if (normalized == "m3" || normalized.Contains("m3"))
        {
            return forBoardCard && Resources.Load<Texture2D>("CardArt/M3_Avator") != null ? "M3_Avator" : "M3";
        }

        Texture2D exact = Resources.Load<Texture2D>($"CardArt/{cardName}");
        return exact != null ? cardName : string.Empty;
    }

    private static string FirstExistingArtworkKey(string[] keys, string fallback)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Resources.Load<Texture2D>($"CardArt/{keys[i]}") != null)
            {
                return keys[i];
            }
        }

        return EnsureExistingArtworkKey(fallback);
    }

    private static string NormalizeArtworkKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c) || c > 127)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    private string CardPrefabResourcePath()
    {
        return ResolvePrefabPath(card, useHandPrefab);
    }

    public static string ResolvePrefabPath(RuntimeCard runtimeCard, bool useHandPrefab)
    {
        if (runtimeCard != null && runtimeCard.Type == CardType.Unit)
        {
            if (!useHandPrefab
                && (runtimeCard.Zone == CardZone.PlayerSupport
                    || runtimeCard.Zone == CardZone.Frontline
                    || runtimeCard.Zone == CardZone.EnemySupport))
            {
                return "CardPrefabs/UnitCard_Board";
            }

            return "CardPrefabs/UnitCard_Hand";
        }

        if (runtimeCard != null && runtimeCard.Type == CardType.Countermeasure)
        {
            return "CardPrefabs/CounterCard_Hand";
        }

        return "CardPrefabs/OrderCard_Hand";
    }

    private void ApplyOwnerMetalTint()
    {
        if (card == null || isHidden || IsBoardUnitCard())
        {
            return;
        }

        if (prefabTemplate != null)
        {
            return;
        }

        Color faceColor = OwnerMetalColor(card.Owner);
        Color artColor = Color.Lerp(faceColor, card.Owner == PlayerSide.Enemy ? new Color(0.45f, 0.13f, 0.10f) : new Color(0.68f, 0.70f, 0.66f), 0.45f);
        if (faceRenderer != null)
        {
            Material material = EditableMaterial(faceRenderer);
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = faceColor;
            }
        }

        if (prefabTemplate == null)
        {
            normalColor = faceColor;
            return;
        }

        MeshRenderer[] renderers = prefabTemplate.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            Material material = EditableMaterial(renderer);
            if (renderer == null || renderer.GetComponent<TMP_Text>() != null || material == null || !material.HasProperty("_Color"))
            {
                continue;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("art") || objectName.Contains("panel"))
            {
                material.color = artColor;
            }
            else if (objectName.Contains("face") || objectName.Contains("paper") || objectName.Contains("body"))
            {
                material.color = faceColor;
            }
        }

        normalColor = faceColor;
    }

    private Color OwnerMetalColor(PlayerSide owner)
    {
        return owner == PlayerSide.Enemy
            ? new Color(0.50f, 0.24f, 0.22f, 1f)
            : new Color(0.58f, 0.60f, 0.56f, 1f);
    }

    private Color RuntimeFrameColor()
    {
        if (card == null)
        {
            return new Color(0.18f, 0.19f, 0.18f);
        }

        if (card.Owner == PlayerSide.Enemy)
        {
            return new Color(0.42f, 0.15f, 0.13f);
        }

        switch (card.Type)
        {
            case CardType.Order:
                return new Color(0.22f, 0.20f, 0.32f);
            case CardType.Countermeasure:
                return new Color(0.30f, 0.18f, 0.31f);
            default:
                return new Color(0.18f, 0.19f, 0.18f);
        }
    }

    private Color RuntimePaperColor()
    {
        if (card == null)
        {
            return new Color(0.78f, 0.72f, 0.56f);
        }

        Color baseColor;
        switch (card.Type)
        {
            case CardType.Order:
                baseColor = new Color(0.58f, 0.54f, 0.70f);
                break;
            case CardType.Countermeasure:
                baseColor = new Color(0.56f, 0.39f, 0.58f);
                break;
            default:
                baseColor = new Color(0.78f, 0.72f, 0.56f);
                break;
        }

        return card.Owner == PlayerSide.Enemy
            ? Color.Lerp(baseColor, new Color(0.50f, 0.18f, 0.15f), 0.45f)
            : baseColor;
    }

    private void ApplyRuntimeCardArtwork(MeshRenderer renderer, bool hidden)
    {
        if (renderer == null || hidden || card == null)
        {
            return;
        }

        string artKey = ResolveArtworkKeyForCard(card, IsBoardUnitCard());
        Texture2D artTexture = Resources.Load<Texture2D>($"CardArt/{artKey}");
        if (artTexture != null)
        {
            ApplyTexture(renderer, artTexture);
        }
    }

    private static float FuelDotX(int index, int count)
    {
        int visibleCount = Mathf.Max(1, count);
        float squeeze = Mathf.InverseLerp(2f, 8f, visibleCount);
        float spacing = Mathf.Lerp(0.088f, 0.050f, squeeze);
        return (index - (visibleCount - 1) * 0.5f) * spacing;
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
        text.fontSize = characterSize * 12f;
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
        SetEditableMaterial(renderer, CreateRuntimeColorMaterial(color));
        return renderer;
    }

    private MeshRenderer CreateImagePanel(string panelName, Vector3 localPosition, float width, float height, Color color)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = localPosition;

        MeshFilter filter = panel.AddComponent<MeshFilter>();
        filter.mesh = RuntimeQuadMesh(width, height);

        MeshRenderer renderer = panel.AddComponent<MeshRenderer>();
        SetEditableMaterial(renderer, CreateRuntimeTextureMaterial(color));
        return renderer;
    }

    private MeshRenderer CreateEllipsePanel(string panelName, Vector3 localPosition, float radiusX, float radiusZ, Color color)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = localPosition;

        MeshFilter filter = panel.AddComponent<MeshFilter>();
        filter.mesh = RuntimeEllipseMesh(radiusX, radiusZ);

        MeshRenderer renderer = panel.AddComponent<MeshRenderer>();
        SetEditableMaterial(renderer, CreateRuntimeColorMaterial(color));
        return renderer;
    }

    private MeshRenderer CreateArcPanel(string panelName, Vector3 localPosition, float radiusX, float radiusZ, float startDegrees, float endDegrees, Color color)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = localPosition;

        MeshFilter filter = panel.AddComponent<MeshFilter>();
        filter.mesh = RuntimeArcMesh(radiusX, radiusZ, startDegrees, endDegrees);

        MeshRenderer renderer = panel.AddComponent<MeshRenderer>();
        SetEditableMaterial(renderer, CreateRuntimeColorMaterial(color));
        return renderer;
    }

    private static Mesh RuntimeQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh { name = "RuntimeCardImageQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-width * 0.5f, 0f, -height * 0.5f),
            new Vector3(width * 0.5f, 0f, -height * 0.5f),
            new Vector3(width * 0.5f, 0f, height * 0.5f),
            new Vector3(-width * 0.5f, 0f, height * 0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.normals = UpNormals(4);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh RuntimeEllipseMesh(float radiusX, float radiusZ)
    {
        const int segments = 24;
        Mesh mesh = new Mesh { name = "RuntimeCardEllipse" };
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = i == segments - 1 ? 1 : i + 2;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = next;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = UpNormals(vertices.Length);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh RuntimeArcMesh(float radiusX, float radiusZ, float startDegrees, float endDegrees)
    {
        const int segments = 16;
        Mesh mesh = new Mesh { name = "RuntimeCardArc" };
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Deg2Rad * Mathf.Lerp(startDegrees, endDegrees, t);
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = UpNormals(vertices.Length);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3[] UpNormals(int count)
    {
        Vector3[] normals = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            normals[i] = Vector3.up;
        }

        return normals;
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

        Transform overlayTransform = FindFirstChildTransform(transform, "Mulligan Discard Overlay");
        if (overlayTransform == null)
        {
            discardOverlayRenderer = CreateImagePanel(
                "Mulligan Discard Overlay",
                new Vector3(0f, 0.128f, 0f),
                CardWidth * 0.72f,
                CardHeight * 0.72f,
                new Color(1f, 0.18f, 0.08f, 0.72f));
            discardOverlay = discardOverlayRenderer.gameObject;
        }
        else
        {
            discardOverlay = overlayTransform.gameObject;
            discardOverlayRenderer = discardOverlay.GetComponent<MeshRenderer>();
        }

        if (discardOverlayRenderer != null)
        {
            Material material = EditableMaterial(discardOverlayRenderer);
            if (material == null)
            {
                material = CreateRuntimeTextureMaterial(Color.white);
                SetEditableMaterial(discardOverlayRenderer, material);
            }

            Texture2D discardTexture = SceneIconRegistry.Active != null
                ? SceneIconRegistry.Active.DiscardThisCardIcon
                : Resources.Load<Texture2D>("Icons/DiscardThisCard");
            if (discardTexture != null)
            {
                material.mainTexture = discardTexture;
            }
        }

        discardOverlay.SetActive(false);
    }

    private void EnsureDamagePreviewOverlay()
    {
        if (damagePreviewLabel != null && damagePreviewSkullObject != null)
        {
            return;
        }

        if (damagePreviewLabel == null)
        {
            Transform labelTransform = FindFirstChildTransform(transform, "Damage Preview Label");
            if (labelTransform != null)
            {
                damagePreviewLabel = labelTransform.GetComponent<TMP_Text>();
            }
        }

        if (damagePreviewSkullObject == null)
        {
            Transform skullTransform = FindFirstChildTransform(transform, "Damage Preview Skull");
            if (skullTransform != null)
            {
                damagePreviewSkullObject = skullTransform.gameObject;
            }
        }

        if (damagePreviewLabel == null && damagePreviewSkullObject == null)
        {
            return;
        }

        if (damagePreviewLabel != null)
        {
            damagePreviewLabel.text = string.Empty;
            SetTextVisible(damagePreviewLabel, false);
        }

        if (damagePreviewSkullObject == null)
        {
            return;
        }

        damagePreviewSkullRenderer = damagePreviewSkullObject.GetComponent<MeshRenderer>();
        if (damagePreviewSkullRenderer != null)
        {
            Material material = EditableMaterial(damagePreviewSkullRenderer);
            if (material == null)
            {
                material = CreateRuntimeTextureMaterial(Color.white);
                SetEditableMaterial(damagePreviewSkullRenderer, material);
            }

            Texture2D skullTexture = SceneIconRegistry.Active != null
                ? SceneIconRegistry.Active.EstimatedDeathSkullIcon
                : Resources.Load<Texture2D>("Icons/EstimatedDeathSkull");
            if (skullTexture != null)
            {
                material.mainTexture = skullTexture;
            }
        }

        damagePreviewSkullObject.SetActive(false);
    }

    private void EnsureKeywordIconColumn()
    {
        if (keywordIconColumn != null)
        {
            return;
        }

        keywordIconColumn = FindFirstChildTransform(transform, "Keyword Icon Column")?.gameObject;
    }

    private void CreateKeywordIcon(CardKeyword keyword, int index)
    {
        if (keywordIconColumn == null)
        {
            return;
        }

        string iconName = $"Keyword Icon {keyword}";
        Transform iconTransform = FindFirstChildTransform(keywordIconColumn.transform, iconName);
        if (iconTransform == null)
        {
            return;
        }

        GameObject iconObject = iconTransform.gameObject;
        MeshRenderer renderer = iconObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = EditableMaterial(renderer);
            if (material == null)
            {
                material = CreateRuntimeTextureMaterial(Color.white);
                SetEditableMaterial(renderer, material);
            }

            Texture2D iconTexture = KeywordIconTexture(keyword);
            if (iconTexture != null)
            {
                material.mainTexture = iconTexture;
                material.color = Color.white;
            }
            else
            {
                material.color = KeywordIconColor(keyword);
            }
        }

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

    private static Transform FindFirstChildTransform(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform direct = root.Find(childName);
        if (direct != null)
        {
            return direct;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform nested = FindFirstChildTransform(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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
            operationLabel.text = card != null && card.Type == CardType.Unit ? DisplayOperationCostText() : string.Empty;
            SetTextVisible(operationLabel, card != null && card.Type == CardType.Unit);
        }

        if (operationBadgeRenderer != null)
        {
            operationBadgeRenderer.enabled = card != null && card.Type == CardType.Unit;
        }

        if (controller == null || isHidden || costLabel == null || costBadgeRenderer == null)
        {
            return;
        }

        int availableKredits = controller.AvailableKreditsFor(card.Owner);
        int operationCost = controller.DisplayOperationCostFor(card);
        CardOperationBadgeState state = CardOperationBadgeRules.State(card, availableKredits, operationCost);
        if (state == CardOperationBadgeState.Hidden)
        {
            return;
        }

        costLabel.text = CardOperationBadgeRules.Text(card, availableKredits, operationCost);
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
        return new Color(0.82f, 0.9f, 0.76f);
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
