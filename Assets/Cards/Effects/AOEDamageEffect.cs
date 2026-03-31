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
            string sourceName = request.SourceCard.CurrentCardData.CardName;
            Debug.Log($"[Effect] 触发全体伤害效果 {sourceName}");

            ZoneId preferredTargetZoneId = request.SourceCard.Owner.GetOpponent().GetBoardZoneId();
            var boardZone = ZoneRegistry.Get(preferredTargetZoneId) ?? request.TargetZone;
            if (boardZone != null && boardZone.Cards != null)
            {
                foreach (var entity in boardZone.Cards)
                {
                    if (entity != request.SourceCard)
                    {
                        int damage = CalculateDamage();
                        actions.Add(new DamageAction(entity.Model, damage, sourceName));
                    }
                }
            }

            return actions;
        }

        private int CalculateDamage()
        {
            if (diceCount <= 0 || diceSides <= 0)
            {
                return attackValue;
            }

            int totalDamage = 0;
            for (int i = 0; i < diceCount; i++)
            {
                totalDamage += UnityEngine.Random.Range(1, diceSides + 1);
            }

            return totalDamage;
        }
    }
}
