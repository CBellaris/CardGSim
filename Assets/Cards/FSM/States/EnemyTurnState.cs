using UnityEngine;
using Cards.Actions;
using Cards.Core;

namespace Cards.FSM.States
{
    public class EnemyTurnState : GameState
    {
        private float delayTimer = 1.5f; // 敌人思考动画延迟

        public EnemyTurnState(GameManager gm) : base(gm) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[EnemyTurnState] 敌方回合开始...");
            delayTimer = 1.5f;
            
            // 简单的敌方 AI：如果场上有敌人，可以依次发起攻击
            // 目前先简化处理，稍后根据实体系统扩展
            ExecuteEnemyAI();
        }

        private void ExecuteEnemyAI()
        {
            var enemyBoard = gm.GetBoardZone(CardOwner.Enemy);
            // 如果场上没有实体，敌方就空过
            if (enemyBoard == null || enemyBoard.Count == 0)
            {
                Debug.Log("场上没有敌人，敌方回合跳过。");
                return;
            }

            // 遍历所有实体（假设现在实体都是敌人），让他们各自行动
            foreach (var entity in enemyBoard.Cards)
            {
                // 这里可以调用 entity.DoAction() 等逻辑
                // 或者简单塞入一个动画
                // Debug.Log($"敌人 {entity.CurrentCardData.CardName} 正在行动...");
                
                // 暂时用一个空的等待代替
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsActionQueueBusy) return;

            delayTimer -= Context?.Time?.DeltaTime ?? 0f;
            if (delayTimer <= 0)
            {
                // 敌人行动完毕，切回玩家回合开始
                gm.StateMachine.ChangeState(gm.PlayerTurnStart);
            }
        }
    }
}
