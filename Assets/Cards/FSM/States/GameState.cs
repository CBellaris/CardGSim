using UnityEngine;

using Cards.Core;
using Cards.Services;

namespace Cards.FSM.States
{
    public abstract class GameState : IState
    {
        protected GameManager gm;

        public GameState(GameManager gm)
        {
            this.gm = gm;
        }

        public virtual void Enter()
        {
            // Debug.Log($"[FSM] Entering {this.GetType().Name}");
        }

        protected GameContext Context => gm != null ? gm.Context : null;
        protected bool IsActionQueueBusy => Context?.Actions?.IsProcessing == true;

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
            // Debug.Log($"[FSM] Exiting {this.GetType().Name}");
        }
    }
}
