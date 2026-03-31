using System;

namespace Cards.Core
{
    /// <summary>
    /// 战斗接口：抽象了可参与战斗的对象（不论是卡牌实体、英雄还是怪物）
    /// </summary>
    public interface ICombatable
    {
        string CombatName { get; }
        int ArmorClass { get; }
        int AttackBonus { get; }
        int Attack { get; }
        int DiceCount { get; }
        int DiceSides { get; }

        void TakeDamage(int damage);
    }
}
