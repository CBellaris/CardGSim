using System;
using UnityEngine;
using Cards.Data;

namespace Cards.Core
{
    /// <summary>
    /// 卡牌的纯数据模型 (Model)。
    /// 负责维护卡牌的当前生命值、护甲等状态，并处理受击逻辑。
    /// 不包含任何与 Unity MonoBehaviour 或 3D 渲染相关的代码，方便进行单元测试。
    /// </summary>
    public class CardModel : ICombatable
    {
        public CardData Data { get; private set; }
        public CardOwner Owner { get; private set; }

        public int CurrentHealth { get; private set; }
        public int CurrentArmorClass { get; private set; }

        // 事件：当生命值发生改变时触发，由 View 层监听更新 UI
        public event Action<int> OnHealthChanged;
        
        // 事件：当卡牌死亡时触发，由 Controller/Manager 监听处理离场逻辑
        public event Action OnDied;

        public CardModel(CardData data, CardOwner owner = CardOwner.Player)
        {
            Data = data;
            Owner = owner;
            if (data != null)
            {
                CurrentHealth = data.Health;
                CurrentArmorClass = data.ArmorClass;
            }
        }

        // --- ICombatable 接口实现 ---
        public string CombatName => Data != null ? Data.CardName : "Unknown";
        public int ArmorClass => CurrentArmorClass;
        public int AttackBonus => Data != null ? Data.AttackBonus : 0;
        public int Attack => Data != null ? Data.Attack : 0;
        public int DiceCount => Data != null ? Data.DiceCount : 0;
        public int DiceSides => Data != null ? Data.DiceSides : 0;

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            Debug.Log($"[Model] {CombatName} takes {damage} damage! Remaining Health: {CurrentHealth}");
            
            // 通知 View 更新血条显示
            OnHealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0)
            {
                Debug.Log($"[Model] {CombatName} has died.");
                OnDied?.Invoke();
            }
        }

        // 可以随时添加重置状态的方法（如从弃牌堆抽上来时重置血量）
        public void ResetStats()
        {
            if (Data != null)
            {
                CurrentHealth = Data.Health;
                CurrentArmorClass = Data.ArmorClass;
                OnHealthChanged?.Invoke(CurrentHealth);
            }
        }
    }
}