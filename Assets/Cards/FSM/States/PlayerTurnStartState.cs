using UnityEngine;
using Cards.Actions;
using Cards.Core;

namespace Cards.FSM.States
{
    public class PlayerTurnStartState : GameState
    {
        public PlayerTurnStartState(GameManager gm) : base(gm) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[PlayerTurnStartState] 玩家回合开始...");

            // 回合开始时抽1张牌（可根据需求调整）
            ActionManager.Instance.AddAction(new DrawCardAction());

            // TODO: 在这里处理“回合开始时触发的Buff/Debuff（如中毒掉血）”
            // TODO: 在这里重置玩家的法力值/行动力
        }

        public override void Update()
        {
            base.Update();

            // 等待回合开始的结算动画和抽牌动画完成
            if (!ActionManager.Instance.IsExecuting)
            {
                gm.StateMachine.ChangeState(gm.PlayerMainPhase);
            }
        }
    }
}