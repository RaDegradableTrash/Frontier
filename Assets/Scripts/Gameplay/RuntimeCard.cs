using System;
using UnityEngine;

[Serializable]
public class RuntimeCard
{
    public string Id;
    public string CardName;
    public string Nation;
    public CardFaction Faction;
    public CardRarity Rarity;
    public CardType Type;
    public int KreditCost;
    public int OperationCost;
    public int DeploymentCostBonus;
    public int OperationCostBonus;
    public int Attack;
    public int Defense;
    public int CurrentDefense;
    public Texture2D Artwork;
    public string RulesText;
    public CardTrigger Trigger;
    public CardEffectType EffectType;
    public int EffectAmount;
    public string AddedCardName;
    public CardRule[] SpecialRules;
    public CardKeyword Keywords;
    public PlayerSide Owner;
    public CardZone Zone;
    public bool HasActed;
    public int AttacksThisTurn;

    public bool IsAlive => CurrentDefense > 0;
    public bool HasKeyword(CardKeyword keyword) => (Keywords & keyword) == keyword;
    public void AddKeyword(CardKeyword keyword) => Keywords |= keyword;
    public void RemoveKeyword(CardKeyword keyword) => Keywords &= ~keyword;
    public int EffectiveDeploymentCost => Math.Max(0, KreditCost + DeploymentCostBonus);
    public int EffectiveOperationCost => Math.Max(0, OperationCost + OperationCostBonus);
    public bool CanOperate(int availableKredits) => Type == CardType.Unit && !HasActed && !HasKeyword(CardKeyword.Pinned) && EffectiveOperationCost <= availableKredits;

    public RuntimeCard CloneFor(PlayerSide owner)
    {
        return new RuntimeCard
        {
            Id = Guid.NewGuid().ToString("N"),
            CardName = CardName,
            Nation = Nation,
            Faction = Faction,
            Rarity = Rarity,
            Type = Type,
            KreditCost = KreditCost,
            OperationCost = OperationCost,
            DeploymentCostBonus = DeploymentCostBonus,
            OperationCostBonus = OperationCostBonus,
            AddedCardName = AddedCardName,
            Attack = Attack,
            Defense = Defense,
            CurrentDefense = Defense,
            RulesText = RulesText,
            Trigger = Trigger,
            EffectType = EffectType,
            EffectAmount = EffectAmount,
            SpecialRules = SpecialRules,
            Keywords = Keywords,
            Artwork = Artwork,
            Owner = owner,
            Zone = CardZone.Deck,
            HasActed = false,
            AttacksThisTurn = 0
        };
    }
}
