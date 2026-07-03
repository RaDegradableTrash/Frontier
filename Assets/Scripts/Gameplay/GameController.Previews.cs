using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
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
            PlayerSide headquartersSide = HeadquartersSideForSlot(slot);
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

        int damage = ModifiedDamage(AdjacentAreaDamage(order), slot.Occupant);
        CardView adjacentView = FindView(slot.Occupant);
        if (adjacentView != null)
        {
            adjacentView.ShowDamagePreview(damage, damage >= slot.Occupant.CurrentDefense);
        }
    }
}
