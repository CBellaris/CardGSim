namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 交互规则接口。可以挂载在区域上，拦截和处理进入/离开该区域的请求。
    /// </summary>
    public interface IInteractionRule
    {
        /// <summary>
        /// 规则的优先级，数值越高越先执行（用于处理反制规则优先于结算规则）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 第一阶段：验证请求是否合法。如果返回 false，请求将被取消。
        /// </summary>
        bool Validate(InteractionRequest request);

        /// <summary>
        /// 第二阶段：在实际发生移动/结算前，可以修改请求（比如修改目标、增加费用等）
        /// </summary>
        void BeforeExecute(InteractionRequest request);

        /// <summary>
        /// 第三阶段：实际执行逻辑（如产生伤害、把牌加入区域）。
        /// 如果这个规则完全接管了该行为，应该将 request.IsHandled 设为 true。
        /// </summary>
        void Execute(InteractionRequest request);
    }
}
