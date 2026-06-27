using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CardView : MonoBehaviour
{
    private const float CardWidth = 0.96f;
    private const float CardHeight = 1.30f;

    private RuntimeCard card;
    private GameController controller;
    private TextMesh label;
    private TextMesh costLabel;
    private TextMesh attackLabel;
    private TextMesh defenseLabel;
    private TextMesh costBadgeLabel;
    private TextMesh attackBadgeLabel;
    private TextMesh defenseBadgeLabel;
    private TextMesh statusLabel;
    private MeshRenderer faceRenderer;
    private MeshRenderer rarityBandRenderer;
    private CardMotion motion;
    private bool isHidden;
    private bool isDragging;
    private bool hasLayout;
    private bool isHoldingPlayerHandOpen;
    private Vector3 dragStartPosition;
    private Color normalColor;

    public RuntimeCard Card => card;
    public bool IsHidden => isHidden;

    public void Initialize(RuntimeCard runtimeCard, GameController owner, bool hidden = false)
    {
        card = runtimeCard;
        controller = owner;
        isHidden = hidden;
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
        label.text = isHidden ? HiddenCardText() : BuildCardText();
        if (costLabel != null)
        {
            costLabel.text = isHidden ? string.Empty : $"{card.KreditCost}";
        }

        if (attackLabel != null)
        {
            attackLabel.text = !isHidden && CardTextRules.ShowBattlefieldStats(card) ? $"{card.Attack}" : string.Empty;
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = !isHidden && CardTextRules.ShowBattlefieldStats(card) ? $"{card.CurrentDefense}" : string.Empty;
        }

        if (statusLabel != null)
        {
            statusLabel.text = isHidden ? string.Empty : StatusText();
        }

        if (costBadgeLabel != null)
        {
            costBadgeLabel.text = string.Empty;
        }

        if (attackBadgeLabel != null)
        {
            attackBadgeLabel.text = string.Empty;
        }

        if (defenseBadgeLabel != null)
        {
            defenseBadgeLabel.text = string.Empty;
        }
    }


    private string HiddenCardText()
    {
        return card.Type == CardType.Countermeasure ? "SET\nCOUNTER" : "ENEMY\nCARD";
    }

    private string BuildCardText()
    {
        return $"{CardTextRules.ShortCardName(card)}\n{CardTextRules.CardFaceLine(card)}";
    }

    private string StatusText()
    {
        return CardTextRules.StatusLabel(card);
    }

    public void SetSelected(bool selected)
    {
        if (faceRenderer != null)
        {
            faceRenderer.material.color = selected ? new Color(1f, 0.88f, 0.3f) : normalColor;
        }

        motion?.SetSelected(selected);
    }

    public void SetHandPresentation()
    {
        if (!CardPresentationRules.ShouldUseHandPresentation(card))
        {
            return;
        }

        if (label != null)
        {
            label.text = CardTextRules.ShortCardName(card);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = PlayableSceneRules.HandCardTextCharacterSize;
            label.transform.localPosition = new Vector3(0f, 0.108f, -CardHeight * 0.29f);
            label.color = new Color(0.02f, 0.025f, 0.03f);
        }

        if (costLabel != null)
        {
            costLabel.characterSize = PlayableSceneRules.HandCardNumberCharacterSize;
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
            attackLabel.text = string.Empty;
        }

        if (defenseLabel != null)
        {
            defenseLabel.text = string.Empty;
        }

        if (statusLabel != null)
        {
            statusLabel.text = string.Empty;
        }
    }

    private void ApplyDefaultPresentation()
    {
        if (label != null)
        {
            label.anchor = TextAnchor.UpperLeft;
            label.alignment = TextAlignment.Left;
            label.characterSize = PlayableSceneRules.CardTextCharacterSize;
            label.transform.localPosition = new Vector3(-CardWidth * 0.32f, 0.088f, -CardHeight * 0.16f);
            label.color = isHidden ? Color.white : new Color(0.02f, 0.025f, 0.03f);
        }

        if (costLabel != null)
        {
            costLabel.characterSize = PlayableSceneRules.CardNumberCharacterSize;
        }
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

    public void PlayAttackLunge(Vector3 target)
    {
        motion?.PlayAttackLunge(target);
    }

    private void OnMouseDown()
    {
        if (!CanInteract())
        {
            return;
        }

        HoldPlayerHandOpen(true);
        isDragging = false;
        dragStartPosition = transform.position;
        motion?.SetDragging(false);
        controller?.HandleCardClicked(this);
    }

    private void OnMouseDrag()
    {
        if (!CanInteract())
        {
            return;
        }

        if (TryGetPointerWorldPosition(out Vector3 pointerPosition))
        {
            isDragging = true;
            motion?.SetDragging(true);
            if (card != null && card.Zone == CardZone.Hand)
            {
                transform.position = pointerPosition + Vector3.up * 0.2f;
            }
            else
            {
                controller?.HandleBoardCardDragPreview(this, pointerPosition);
            }
        }
    }

    private void OnMouseUp()
    {
        if (!CanInteract() || !isDragging)
        {
            return;
        }

        isDragging = false;
        motion?.SetDragging(false);
        Vector3 releasePosition = transform.position;
        if (card != null && card.Zone == CardZone.Hand)
        {
            releasePosition = transform.position;
            transform.position = dragStartPosition;
            motion?.ResetBasePosition(transform.position);
        }
        else if (TryGetPointerWorldPosition(out Vector3 pointerPosition))
        {
            releasePosition = pointerPosition;
        }

        controller?.ClearDragPreview();
        controller?.HandleCardReleased(this, releasePosition);
        HoldPlayerHandOpen(false);
    }

    private void OnMouseEnter()
    {
        if (CanInteract())
        {
            HoldPlayerHandOpen(true);
            motion?.SetHovered(true);
            controller?.HandleCardHovered(this);
        }
    }

    private void OnMouseExit()
    {
        motion?.SetHovered(false);
        if (CardInteractionRules.ShouldReleasePlayerHandHold(isDragging))
        {
            HoldPlayerHandOpen(false);
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
        return !isHidden && card != null && controller != null;
    }

    private bool TryGetPointerWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = transform.position;
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
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

        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
        face.name = "Face";
        face.transform.SetParent(transform, false);
        face.transform.localPosition = Vector3.zero;
        face.transform.localScale = new Vector3(CardWidth, 0.03f, CardHeight);
        Destroy(face.GetComponent<Collider>());

        faceRenderer = face.GetComponent<MeshRenderer>();
        normalColor = hidden ? new Color(0.15f, 0.18f, 0.25f) : FactionColor(card.Faction);
        faceRenderer.material = ResolveFaceMaterial(hidden);
        faceRenderer.material.color = normalColor;
        CreateInsetPanel("Art Panel", new Vector3(0f, 0.055f, 0.15f), new Vector3(CardWidth * 0.80f, 0.018f, CardHeight * 0.50f), hidden ? new Color(0.08f, 0.1f, 0.14f) : ArtColor(card));
        CreateInsetPanel("Cost Badge", new Vector3(-CardWidth * 0.37f, 0.078f, CardHeight * 0.42f), new Vector3(0.20f, 0.026f, 0.20f), new Color(0.09f, 0.085f, 0.045f));
        if (!hidden && card.Type == CardType.Unit)
        {
            CreateInsetPanel("Attack Badge", new Vector3(-CardWidth * 0.29f, 0.078f, -CardHeight * 0.42f), new Vector3(0.20f, 0.026f, 0.20f), new Color(0.56f, 0.08f, 0.06f));
            CreateInsetPanel("Defense Badge", new Vector3(CardWidth * 0.29f, 0.078f, -CardHeight * 0.42f), new Vector3(0.20f, 0.026f, 0.20f), new Color(0.07f, 0.22f, 0.55f));
        }
        CreateInsetPanel("Status Badge", new Vector3(0f, 0.074f, -CardHeight * 0.22f), new Vector3(CardWidth * 0.74f, 0.018f, 0.075f), new Color(0.13f, 0.095f, 0.04f));

        GameObject rarityBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rarityBand.name = "RarityBand";
        rarityBand.transform.SetParent(transform, false);
        rarityBand.transform.localPosition = new Vector3(0f, 0.052f, CardHeight * 0.45f);
        rarityBand.transform.localScale = new Vector3(CardWidth * 0.92f, 0.012f, 0.055f);
        Destroy(rarityBand.GetComponent<Collider>());
        rarityBandRenderer = rarityBand.GetComponent<MeshRenderer>();
        rarityBandRenderer.material = new Material(Shader.Find("Standard"));
        rarityBandRenderer.material.color = hidden ? new Color(0.35f, 0.38f, 0.46f) : RarityColor(card.Rarity);

        GameObject text = new GameObject("Label");
        text.transform.SetParent(transform, false);
        text.transform.localPosition = new Vector3(-CardWidth * 0.32f, 0.088f, -CardHeight * 0.16f);
        text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        label = text.AddComponent<TextMesh>();
        label.anchor = TextAnchor.UpperLeft;
        label.alignment = TextAlignment.Left;
        label.characterSize = PlayableSceneRules.CardTextCharacterSize;
        label.fontSize = 48;
        label.color = hidden ? Color.white : new Color(0.02f, 0.025f, 0.03f);
        costLabel = CreateCardText("Cost Number", new Vector3(-CardWidth * 0.37f, 0.104f, CardHeight * 0.42f), PlayableSceneRules.CardNumberCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.87f, 0.42f));
        attackLabel = CreateCardText("Attack Number", new Vector3(-CardWidth * 0.29f, 0.104f, -CardHeight * 0.42f), PlayableSceneRules.CardNumberCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.45f));
        defenseLabel = CreateCardText("Defense Number", new Vector3(CardWidth * 0.29f, 0.104f, -CardHeight * 0.42f), PlayableSceneRules.CardNumberCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.45f));
        costBadgeLabel = CreateCardText("Cost Badge Label", new Vector3(-CardWidth * 0.395f, 0.106f, CardHeight * 0.31f), PlayableSceneRules.CardBadgeLabelCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        costBadgeLabel.text = string.Empty;
        attackBadgeLabel = CreateCardText("Attack Badge Label", new Vector3(-CardWidth * 0.31f, 0.106f, -CardHeight * 0.30f), PlayableSceneRules.CardBadgeLabelCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        attackBadgeLabel.text = string.Empty;
        defenseBadgeLabel = CreateCardText("Defense Badge Label", new Vector3(CardWidth * 0.25f, 0.106f, -CardHeight * 0.30f), PlayableSceneRules.CardBadgeLabelCharacterSize, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
        defenseBadgeLabel.text = string.Empty;
        statusLabel = CreateCardText("Status Label", new Vector3(-CardWidth * 0.37f, 0.098f, -CardHeight * 0.22f), PlayableSceneRules.CardStatusCharacterSize, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.35f));

        motion = gameObject.AddComponent<CardMotion>();
    }

    private TextMesh CreateCardText(string textName, Vector3 localPosition, float characterSize, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(textName);
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.anchor = anchor;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 96;
        textMesh.color = color;
        return textMesh;
    }

    private void CreateInsetPanel(string panelName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = panelName;
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = localPosition;
        panel.transform.localScale = localScale;
        Destroy(panel.GetComponent<Collider>());

        MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
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

        return new Material(Shader.Find("Standard"));
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
