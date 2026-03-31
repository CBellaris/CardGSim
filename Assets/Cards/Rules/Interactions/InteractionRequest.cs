using Cards.Zones;
using Cards.Core;

namespace Cards.Rules.Interactions
{
    public enum InteractionType
    {
        ZoneTransfer, // 泛化的区域转移（如：抽牌、弃牌）
        PlayCard,     // 打出卡牌（通常伴随效果触发）
        Attack,       // 实体直接攻击
        // 其他可能的交互类型...
    }

    /// <summary>
    /// 代表一次卡牌从一个区域向另一个区域（或另一个实体）发起交互的意图
    /// </summary>
    public class InteractionRequest
    {
        public InteractionType Type { get; set; } = InteractionType.PlayCard;

        public CardEntity SourceCard { get; set; }
        public CardZone SourceZone { get; set; }
        public ZoneId SourceZoneId { get; set; }
        
        public CardZone TargetZone { get; set; }
        public ZoneId TargetZoneId { get; set; }
        
        // 可选：如果交互目标是区域内的某个具体实体（如攻击某个怪物）
        public CardEntity TargetEntity { get; set; }

        // 标记请求是否被拦截或取消
        public bool IsCancelled { get; set; }
        // 标记请求是否已经被某个规则“处理”了（比如处理成了攻击，就不需要再处理成进场了）
        public bool IsHandled { get; set; }
    }
}