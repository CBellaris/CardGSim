using UnityEngine;
using Cards.Core;

namespace Cards.FSM.States
{
    public class PlayerTurnEndState : GameState
    {
        public PlayerTurnEndState(GameManager gm) : base(gm) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[PlayerTurnEndState] 玩家回合结束结算...");

            // TODO: 在这里处理“回合结束时”的Buff/Debuff（比如重置力量）
            // TODO: 弃掉手中未打出的牌（如果是杀戮尖塔类型）
        }

        public override void Update()
        {
            base.Update();

            // 等待结算动画完毕
            if (!IsActionQueueBusy)
            {
                gm.StateMachine.ChangeState(gm.EnemyTurn);
            }
        }
    }
}
