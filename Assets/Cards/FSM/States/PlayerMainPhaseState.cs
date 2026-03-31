using UnityEngine;
using Cards.Actions;
using Cards.Core;

namespace Cards.FSM.States
{
    public class PlayerMainPhaseState : GameState
    {
        public PlayerMainPhaseState(GameManager gm) : base(gm) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[PlayerMainPhaseState] 等待玩家出牌...");
            // 可以触发 UI：显示“你的回合”提示
        }

        public override void Update()
        {
            base.Update();

            // 在 ActionManager 执行动画（比如伤害结算、抽牌）期间，锁定玩家输入
            if (ActionManager.Instance.IsExecuting) return;

            // 监听空格抽牌（测试用）
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ActionManager.Instance.AddAction(new DrawCardAction());
            }

            // 监听回车键结束回合（测试用，后续可改为 UI 按钮事件）
            if (Input.GetKeyDown(KeyCode.Return))
            {
                gm.StateMachine.ChangeState(gm.PlayerTurnEnd);
            }

            // 出牌逻辑已在 GameManager.PlayCard 中处理，
            // 稍后我们会在 PlayCard 里增加 FSM 状态判断，只有在这个 State 才能出牌
        }
    }
}