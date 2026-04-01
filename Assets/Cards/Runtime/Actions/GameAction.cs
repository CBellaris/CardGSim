using System.Collections;
using Cards.Services;

namespace Cards.Actions
{
    /// <summary>
    /// 所有游戏指令的抽象基类。
    /// 逻辑执行与动画等待被拆分为两个独立阶段，便于同步测试和禁用动画。
    /// </summary>
    public abstract class GameAction
    {
        public bool IsCompleted { get; protected set; } = false;

        /// <summary>
        /// 同步执行核心逻辑，负责修改游戏状态。
        /// </summary>
        public abstract void Execute(GameContext ctx);

        /// <summary>
        /// 仅负责表现层等待或动画播放。
        /// </summary>
        public virtual IEnumerator AnimateRoutine()
        {
            yield break;
        }

        /// <summary>
        /// 兼容旧调用方式：按当前动画策略串联逻辑和动画。
        /// </summary>
        public virtual IEnumerator ExecuteRoutine(GameContext ctx)
        {
            Execute(ctx);

            if (ctx?.AnimationPolicy?.IsEnabled != false)
            {
                yield return AnimateRoutine();
            }

            MarkCompleted();
        }

        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Called each frame after the action finishes its main routine but still awaits external completion.
        /// </summary>
        public virtual void Tick() { }
    }
}
