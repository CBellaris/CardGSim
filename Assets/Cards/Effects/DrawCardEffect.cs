using System;
using System.Collections.Generic;
using UnityEngine;
using Cards.Rules.Interactions;
using Cards.Actions;
using Cards.Core;

namespace Cards.Effects
{
    [Serializable]
    public class DrawCardEffect : ICardEffect
    {
        public int amount = 1;

        public bool CanExecute(InteractionRequest request, out string failureReason)
        {
            failureReason = null;
            return true;
        }

        public List<GameAction> Execute(InteractionRequest request)
        {
            var actions = new List<GameAction>();
            Debug.Log($"[Effect] 触发抽牌效果，抽取 {amount} 张牌。");
            
            for (int i = 0; i < amount; i++)
            {
                actions.Add(new DrawCardAction());
            }

            return actions;
        }
    }
}
