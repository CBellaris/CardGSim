using System.Collections.Generic;
using System.Linq;
using Cards.Services;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
    public class RuleEngine : IRuleEngine
    {
        private readonly ZoneTransferService zoneTransfers;

        public RuleEngine(ZoneTransferService zoneTransfers)
        {
            this.zoneTransfers = zoneTransfers ?? new ZoneTransferService();
        }

        public void ProcessInteraction(InteractionRequest request)
        {
            if (request == null || request.SourceCard == null)
            {
                return;
            }

            List<IInteractionRule> applicableRules = new List<IInteractionRule>();

            if (request.SourceZone?.Rules != null)
            {
                applicableRules.AddRange(request.SourceZone.Rules);
            }

            if (request.TargetZone?.Rules != null)
            {
                applicableRules.AddRange(request.TargetZone.Rules);
            }

            applicableRules = applicableRules
                .Where(rule => rule != null)
                .OrderByDescending(rule => rule.Priority)
                .ToList();

            foreach (IInteractionRule rule in applicableRules)
            {
                if (!rule.Validate(request))
                {
                    request.IsCancelled = true;
                    request.Context?.Logger?.LogWarning($"[RuleEngine] 请求被规则拦截取消: {rule.GetType().Name}");
                    break;
                }
            }

            if (request.IsCancelled)
            {
                return;
            }

            foreach (IInteractionRule rule in applicableRules)
            {
                rule.BeforeExecute(request);
                if (request.IsCancelled)
                {
                    return;
                }
            }

            foreach (IInteractionRule rule in applicableRules)
            {
                rule.Execute(request);
                if (request.IsHandled)
                {
                    break;
                }
            }

            if (!request.IsHandled &&
                !request.IsCancelled &&
                request.TargetZone != null &&
                request.Type != InteractionType.Attack)
            {
                DefaultMoveCard(request);
            }
        }

        private void DefaultMoveCard(InteractionRequest request)
        {
            if (zoneTransfers.MoveCard(request.SourceCard, request.TargetZone, request.SourceZone))
            {
                request.IsHandled = true;
                request.Context?.Logger?.Log($"[RuleEngine] 执行默认移动: {request.SourceCard.Data?.CardName} 到 {request.TargetZoneId}");
            }
        }
    }
}
