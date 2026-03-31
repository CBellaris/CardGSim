using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cards.Core;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 规则引擎，负责接收交互请求，收集相关区域的规则，并按生命周期执行
    /// </summary>
    public class RuleEngine
    {
        public static void ProcessInteraction(InteractionRequest request)
        {
            if (request == null || request.SourceCard == null) return;

            // 1. 收集相关规则 (源区域的规则 + 目标区域的规则)
            List<IInteractionRule> applicableRules = new List<IInteractionRule>();
            
            if (request.SourceZone != null && request.SourceZone.Rules != null)
            {
                applicableRules.AddRange(request.SourceZone.Rules);
            }
            
            if (request.TargetZone != null && request.TargetZone.Rules != null)
            {
                applicableRules.AddRange(request.TargetZone.Rules);
            }

            // 按优先级降序排序
            applicableRules = applicableRules.OrderByDescending(r => r.Priority).ToList();

            // 2. 验证阶段 (Validate)
            foreach (var rule in applicableRules)
            {
                if (!rule.Validate(request))
                {
                    request.IsCancelled = true;
                    Debug.Log($"[RuleEngine] 请求被规则拦截取消: {rule.GetType().Name}");
                    break;
                }
            }

            if (request.IsCancelled) return;

            // 3. 执行前修改阶段 (BeforeExecute)
            foreach (var rule in applicableRules)
            {
                rule.BeforeExecute(request);
                if (request.IsCancelled) return; // 可能在修改阶段被取消
            }

            // 4. 执行阶段 (Execute)
            foreach (var rule in applicableRules)
            {
                rule.Execute(request);
                // 如果某个规则宣布它“处理”了这个请求（比如这是个攻击指令，攻击完了），就跳出
                if (request.IsHandled)
                {
                    break;
                }
            }

            // 5. 默认行为兜底：如果没有任何规则处理这个移动请求，执行默认的卡牌转移
            if (!request.IsHandled && !request.IsCancelled && request.TargetZone != null)
            {
                DefaultMoveCard(request);
            }
        }

        private static void DefaultMoveCard(InteractionRequest request)
        {
            if (ZoneTransferService.MoveCard(request.SourceCard, request.TargetZone, true, request.SourceZone))
            {
                request.IsHandled = true;
                Debug.Log($"[RuleEngine] 执行默认移动: {request.SourceCard.CurrentCardData.CardName} 到 {request.TargetZoneId}");
            }
        }
    }
}