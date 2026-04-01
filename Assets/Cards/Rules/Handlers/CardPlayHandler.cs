using System;
using UnityEngine;
using Cards.Core;
using Cards.Core.Events;
using Cards.Zones;
using Cards.Rules.Interactions;
using Cards.FSM.States;
using System.Collections.Generic;
using Cards.Services;

namespace Cards.Rules.Handlers
{
    /// <summary>
    /// 处理玩家点击卡牌（尝试打出）的逻辑
    /// 解耦自 GameManager
    /// </summary>
    public class CardPlayHandler : IDisposable
    {
        private readonly GameContext context;
        private readonly Cards.FSM.GameStateMachine stateMachine;
        private readonly EventToken subscription;

        public CardPlayHandler(GameContext context, Cards.FSM.GameStateMachine stateMachine)
        {
            this.context = context;
            this.stateMachine = stateMachine;
            subscription = context?.Events?.Subscribe<CardClickedEvent>(OnCardClicked);
        }

        public void Dispose()
        {
            context?.Events?.Unsubscribe(subscription);
        }

        private void OnCardClicked(CardClickedEvent evt)
        {
            // 判断当前是否是玩家主阶段，如果不是，禁止出牌
            // 以后如果有专门的状态机管理类，可以从那里获取，目前通过GameManager实例获取状态
            if (stateMachine == null || !(stateMachine.CurrentState is PlayerMainPhaseState))
            {
                Debug.Log("当前不是出牌阶段，无法打出卡牌。");
                return;
            }

            // 简单加一个判断：如果队列正在执行关键动画，暂时不允许打牌，防止玩家在动画期间乱点
            if (context?.Actions?.IsProcessing == true) return;

            CardZone handZone = context?.Zones?.Get(ZoneId.PlayerHand);

            if (handZone != null && handZone.Contains(evt.Card))
            {
                ZoneId targetBoardId = evt.Card.Owner.GetBoardZoneId();
                CardZone targetBoardZone = context?.Zones?.Get(targetBoardId);

                if (targetBoardZone == null)
                {
                    Debug.LogWarning($"未找到可用战场区域: {targetBoardId}");
                    return;
                }

                // 1. 优先从对方战场选择目标；如果还未分阵营完成，则退化为从任意战场选择一个有效目标。
                CardInstance target = SelectTarget(evt.Card);

                // 2. 构造交互请求：卡牌从 手牌区 -> 战场区
                InteractionRequest request = new InteractionRequest
                {
                    Context = context,
                    SourceCard = evt.Card,
                    SourceZone = handZone,
                    SourceZoneId = ZoneId.PlayerHand,
                    TargetZone = targetBoardZone,
                    TargetZoneId = targetBoardId,
                    TargetEntity = target
                };

                // 3. 引擎处理请求
                context?.Rules?.ProcessInteraction(request);
                
                Debug.Log($"尝试打出 {evt.Card.Data?.CardName}, 请求处理结果: Cancelled={request.IsCancelled}, Handled={request.IsHandled}");
            }
        }

        private CardInstance SelectTarget(CardInstance sourceCard)
        {
            ZoneId preferredZoneId = sourceCard.Owner.GetOpponent().GetBoardZoneId();
            CardInstance preferredTarget = GetRandomTargetFromZones(context?.Zones?.GetAll(preferredZoneId), sourceCard);
            if (preferredTarget != null)
            {
                return preferredTarget;
            }

            foreach (ZoneId boardZoneId in new[] { ZoneId.PlayerBoard, ZoneId.EnemyBoard })
            {
                CardInstance fallbackTarget = GetRandomTargetFromZones(context?.Zones?.GetAll(boardZoneId), sourceCard);
                if (fallbackTarget != null)
                {
                    return fallbackTarget;
                }
            }

            return null;
        }

        private CardInstance GetRandomTargetFromZones(IReadOnlyList<CardZone> zones, CardInstance sourceCard)
        {
            if (zones == null || zones.Count == 0) return null;

            List<CardInstance> candidates = new List<CardInstance>();
            foreach (CardZone zone in zones)
            {
                if (zone == null) continue;

                foreach (CardInstance card in zone.Cards)
                {
                    if (card != null && card != sourceCard)
                    {
                        candidates.Add(card);
                    }
                }
            }

            if (candidates.Count == 0) return null;
            IRandom random = context?.Random;
            int index = random != null ? random.Range(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            return candidates[index];
        }
    }
}
