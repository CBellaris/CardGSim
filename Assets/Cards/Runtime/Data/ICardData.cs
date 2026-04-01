using System.Collections.Generic;
using Cards.Effects;

namespace Cards.Data
{
    public interface ICardData
    {
        string CardId { get; }
        string CardName { get; }
        int Cost { get; }
        int Health { get; }
        int ArmorClass { get; }
        int AttackBonus { get; }
        int Attack { get; }
        int DiceCount { get; }
        int DiceSides { get; }
        IReadOnlyList<CardTag> Tags { get; }
        IReadOnlyList<ICardEffect> PlayEffects { get; }
    }
}
