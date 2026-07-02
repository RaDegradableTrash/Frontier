using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
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

        if (card != null
            && card.Owner == PlayerSide.Player
            && card.Zone == CardZone.Hand
            && card.Type == CardType.Order
            && card.EffectType != CardEffectType.DeployWithBlitz
            && !OrderNeedsTarget(card))
        {
            TryPlayOrderOnSlot(null);
        }
    }

    private void ClearSelection()
    {
        if (selectedCard != null)
        {
            HighlightLegalTargets(selectedCard, false);
        }

        selectedCard = null;
        selectedView?.SetSelected(false);
        selectedView = null;
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
}
