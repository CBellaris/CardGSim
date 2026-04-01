using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cards.Services;

namespace Cards.Actions
{
    /// <summary>
    /// 全局动作队列管理器
    /// 负责按顺序逐个执行进入队列的 GameAction
    /// </summary>
    public class ActionManager : MonoBehaviour, IActionQueue
    {
        private readonly Queue<GameAction> actionQueue = new Queue<GameAction>();
        private bool isExecuting = false;
        private GameContext context;

        public static ActionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(GameContext gameContext)
        {
            context = gameContext;
        }

        /// <summary>
        /// 将一个指令加入队列，并尝试启动队列处理
        /// </summary>
        public void AddAction(GameAction action)
        {
            Enqueue(action);
        }

        public void Enqueue(GameAction action)
        {
            if (action == null) return;

            actionQueue.Enqueue(action);
            if (!isExecuting)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            isExecuting = true;
            while (actionQueue.Count > 0)
            {
                GameAction currentAction = actionQueue.Dequeue();
                GameContext activeContext = context;
                if (activeContext == null)
                {
                    Debug.LogError("[ActionManager] 缺少 GameContext，无法执行动作队列。");
                    currentAction.MarkCompleted();
                    continue;
                }

                if (activeContext.AnimationPolicy?.IsEnabled == false)
                {
                    currentAction.Execute(activeContext);
                    currentAction.MarkCompleted();
                }
                else
                {
                    yield return StartCoroutine(currentAction.ExecuteRoutine(activeContext));
                }

                // Cross-frame polling: support actions that await external completion
                // (e.g. waiting for player targeting input) after their coroutine ends
                while (!currentAction.IsCompleted)
                {
                    currentAction.Tick();
                    yield return null;
                }
            }
            isExecuting = false;
        }

        /// <summary>
        /// 提供给外部查询当前是否还有指令在执行
        /// </summary>
        public bool IsExecuting => isExecuting || actionQueue.Count > 0;
        public bool IsProcessing => IsExecuting;
    }
}
