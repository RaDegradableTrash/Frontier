using System;
using UnityEngine;

public sealed class CardRuleExecutionContext
{
    public PlayerState Caster;
    public RuntimeCard Card;
    public SlotInteract TargetSlot;
    public BoardManager Board;
    public Func<PlayerSide, PlayerState> GetState;
    public Func<PlayerSide, PlayerState> GetOpponentState;
    public Func<string, RuntimeCard> FindCardTemplate;
    public Func<PlayerState, RuntimeCard> ChooseAirborneUnit;
    public Func<SlotInteract, PlayerSide, bool> IsEmptyAirborneSlot;
    public Action<PlayerState, RuntimeCard, SlotInteract, Color> DeployAirborneUnit;
    public Action<RuntimeCard, int, string> DamageUnit;
    public Action<PlayerState> DrawCard;
    public Action<PlayerState, RuntimeCard> AddCardToHand;
    public Action<PlayerState, int> GainFriendlyDefense;
    public Action<PlayerState, int> ApplyCostIncrease;
    public Action<PlayerState> FlashCostChange;
    public Action<string> SetStatus;
    public Action<string, Vector3, Color, FeedbackCueType> SpawnFloatingText;
    public Func<PlayerSide, Vector3> HeadquartersMarker;
    public Func<RuntimeCard, SlotInteract, int> AdjacentAreaDamage;
}
