using UnityEngine;
using System.Collections.Generic;
using Cards.Actions;
using Cards.Core;

namespace Cards.FSM.States
{
    public class GameSetupState : GameState
    {
        public GameSetupState(GameManager gm) : base(gm) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[GameSetupState] 初始化游戏...");

            // 1. 初始化牌堆
            gm.InitializePiles();
            
            // 2. 初始化卡组（生成实体并放入抽牌堆）
            gm.InitializeDeck();

            // 3. 游戏开始，抽取初始手牌 (比如3张)
            for (int i = 0; i < 3; i++)
            {
                Context?.Actions?.Enqueue(new DrawCardAction());
            }

            // 初始化完成后，自动流转到玩家回合开始阶段
            // 注意：因为这里刚刚把抽牌动作塞入队列，我们需要在 Update 里等待队列执行完毕再流转
        }

        public override void Update()
        {
            base.Update();

            // 等待所有的发牌动画/逻辑执行完毕
            if (!IsActionQueueBusy)
            {
                gm.StateMachine.ChangeState(gm.PlayerTurnStart);
            }
        }
    }
}
