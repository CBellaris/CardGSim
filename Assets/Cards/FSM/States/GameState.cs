using UnityEngine;

using Cards.Core;

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

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
            // Debug.Log($"[FSM] Exiting {this.GetType().Name}");
        }
    }
}