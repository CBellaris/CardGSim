using System;
using System.Collections.Generic;
using Cards.Rules.Interactions;
using Cards.Core;
using Cards.Actions;

namespace Cards.Effects
{
    public interface ICardEffect
    {
        bool CanExecute(InteractionRequest request, out string failureReason);

        /// <summary>
        /// 根据交互上下文计算并返回需要执行的 Action 列表。
        /// Effect 只负责"算出要做什么"，由 Rule 层统一入队 ActionManager。
        /// </summary>
        List<GameAction> Execute(InteractionRequest request);
    }
}
