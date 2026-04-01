using System;
using System.Collections.Generic;
using UnityEngine;
using Cards.Rules.Interactions;
using Cards.Actions;
using Cards.Core;
using Cards.Zones;

namespace Cards.Effects
{
    [Serializable]
    public class AOEDamageEffect : ICardEffect
    {
        [Header("Damage Configuration")]
        public int attackValue;
        public int diceCount;
        public int diceSides;

        public bool CanExecute(InteractionRequest request, out string failureReason)
        {
            failureReason = null;
            return true;
        }

        public List<GameAction> Execute(InteractionRequest request)
        {
            var actions = new List<GameAction>();
            string sourceName = request.SourceCard.Data?.CardName ?? "Unknown";
            Debug.Log($"[Effect] 触发全体伤害效果 {sourceName}");

            ZoneId preferredTargetZoneId = request.SourceCard.Owner.GetOpponent().GetBoardZoneId();
            var boardZone = request.Context?.Zones?.Get(preferredTargetZoneId) ?? request.TargetZone;
            if (boardZone != null && boardZone.Cards != null)
            {
                foreach (var entity in boardZone.Cards)
                {
                    if (entity != request.SourceCard)
                    {
                        int damage = CalculateDamage(request);
                        actions.Add(new DamageAction(entity, damage, sourceName));
                    }
                }
            }

            return actions;
        }

        private int CalculateDamage(InteractionRequest request)
        {
            return request.Context?.Combat?.RollDamage(diceCount, diceSides, attackValue, false) ?? attackValue;
        }
    }
}
