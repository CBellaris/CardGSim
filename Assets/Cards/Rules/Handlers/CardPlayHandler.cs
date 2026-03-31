using UnityEngine;
using Cards.Core;
using Cards.Core.Events;
using Cards.Zones;
using Cards.Actions;
using Cards.Rules.Interactions;
using Cards.FSM.States;
using System.Collections.Generic;

namespace Cards.Rules.Handlers
{
    /// <summary>
    /// 处理玩家点击卡牌（尝试打出）的逻辑
    /// 解耦自 GameManager
    /// </summary>
    public static class CardPlayHandler
    {
        public static void Initialize()
        {
            EventBus.Subscribe<CardClickedEvent>(OnCardClicked);
        }

        private static void OnCardClicked(CardClickedEvent evt)
        {
            // 判断当前是否是玩家主阶段，如果不是，禁止出牌
            // 以后如果有专门的状态机管理类，可以从那里获取，目前通过GameManager实例获取状态
            if (GameManager.Instance != null && !(GameManager.Instance.StateMachine.CurrentState is PlayerMainPhaseState))
            {
                Debug.Log("当前不是出牌阶段，无法打出卡牌。");
                return;
            }

            // 简单加一个判断：如果队列正在执行关键动画，暂时不允许打牌，防止玩家在动画期间乱点
            if (ActionManager.Instance.IsExecuting) return;

            CardZone handZone = ZoneRegistry.Get(ZoneId.PlayerHand);

            if (handZone != null && handZone.Contains(evt.Card))
            {
                ZoneId targetBoardId = evt.Card.Owner.GetBoardZoneId();
                CardZone targetBoardZone = ZoneRegistry.Get(targetBoardId);

                if (targetBoardZone == null)
                {
                    Debug.LogWarning($"未找到可用战场区域: {targetBoardId}");
                    return;
                }

                // 1. 优先从对方战场选择目标；如果还未分阵营完成，则退化为从任意战场选择一个有效目标。
                CardEntity target = SelectTarget(evt.Card);

                // 2. 构造交互请求：卡牌从 手牌区 -> 战场区
                InteractionRequest request = new InteractionRequest
                {
                    SourceCard = evt.Card,
                    SourceZone = handZone,
                    SourceZoneId = ZoneId.PlayerHand,
                    TargetZone = targetBoardZone,
                    TargetZoneId = targetBoardId,
                    TargetEntity = target
                };

                // 3. 引擎处理请求
                RuleEngine.ProcessInteraction(request);
                
                Debug.Log($"尝试打出 {evt.Card.CurrentCardData.CardName}, 请求处理结果: Cancelled={request.IsCancelled}, Handled={request.IsHandled}");
            }
        }

        private static CardEntity SelectTarget(CardEntity sourceCard)
        {
            ZoneId preferredZoneId = sourceCard.Owner.GetOpponent().GetBoardZoneId();
            CardEntity preferredTarget = GetRandomTargetFromZones(ZoneRegistry.GetAll(preferredZoneId), sourceCard);
            if (preferredTarget != null)
            {
                return preferredTarget;
            }

            foreach (ZoneId boardZoneId in new[] { ZoneId.PlayerBoard, ZoneId.EnemyBoard })
            {
                CardEntity fallbackTarget = GetRandomTargetFromZones(ZoneRegistry.GetAll(boardZoneId), sourceCard);
                if (fallbackTarget != null)
                {
                    return fallbackTarget;
                }
            }

            return null;
        }

        private static CardEntity GetRandomTargetFromZones(IReadOnlyList<CardZone> zones, CardEntity sourceCard)
        {
            if (zones == null || zones.Count == 0) return null;

            List<CardEntity> candidates = new List<CardEntity>();
            foreach (CardZone zone in zones)
            {
                if (zone == null) continue;

                foreach (CardEntity card in zone.Cards)
                {
                    if (card != null && card != sourceCard)
                    {
                        candidates.Add(card);
                    }
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}