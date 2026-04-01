using UnityEngine;
using System.Collections.Generic;

namespace Cards.Data
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/CardData")]
    public class CardData : ScriptableObject, ICardData
    {
        [Header("Basic Info")]
        public string CardId;
        public string CardName;
        [TextArea(2, 4)]
        public string Description;

        [Header("Visuals")]
        public Material CardArtMaterial; // 用于替换3D卡面的材质

        [Header("Stats")]
        public int Cost;
        public int Health;
        public int ArmorClass; // 防御等级 (AC)
        public int AttackBonus; // 攻击检定加值

        [Header("Damage/Effect")]
        public int Attack; // 基础伤害 (如果不使用骰子)
        public int DiceCount; // 掷骰数量 (例如 2d6 的 2)
        public int DiceSides; // 骰子面数 (例如 2d6 的 6)

        [Header("Tags & Types")]
        public List<CardTag> Tags = new List<CardTag>();

        [Header("Card Effects")]
        [SerializeReference]
        public List<Cards.Effects.ICardEffect> PlayEffects = new List<Cards.Effects.ICardEffect>();

        // --- ICardData 显式实现（字段→属性桥接） ---
        string ICardData.CardId => CardId;
        string ICardData.CardName => CardName;
        int ICardData.Cost => Cost;
        int ICardData.Health => Health;
        int ICardData.ArmorClass => ArmorClass;
        int ICardData.AttackBonus => AttackBonus;
        int ICardData.Attack => Attack;
        int ICardData.DiceCount => DiceCount;
        int ICardData.DiceSides => DiceSides;
        IReadOnlyList<CardTag> ICardData.Tags => Tags;
        IReadOnlyList<Cards.Effects.ICardEffect> ICardData.PlayEffects => PlayEffects;
    }
}
