using System;
using Cards.Data;
using Cards.Services;

namespace Cards.Core
{
    /// <summary>
    /// 卡牌的纯数据模型 (Model)。
    /// 负责维护卡牌的当前生命值、护甲等状态，并处理受击逻辑。
    /// 不包含任何与 Unity MonoBehaviour 或 3D 渲染相关的代码，方便进行单元测试。
    /// </summary>
    public class CardModel : ICombatable
    {
        public ICardData Data { get; private set; }
        public CardOwner Owner { get; private set; }

        private readonly ILogger _logger;

        public int CurrentHealth { get; private set; }
        public int CurrentArmorClass { get; private set; }

        public event Action<int> OnHealthChanged;
        public event Action OnDied;

        public CardModel(ICardData data, CardOwner owner = CardOwner.Player, ILogger logger = null)
        {
            Data = data;
            Owner = owner;
            _logger = logger ?? new NullLogger();
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
            _logger.Log($"[Model] {CombatName} takes {damage} damage! Remaining Health: {CurrentHealth}");

            OnHealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0)
            {
                _logger.Log($"[Model] {CombatName} has died.");
                OnDied?.Invoke();
            }
        }

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
