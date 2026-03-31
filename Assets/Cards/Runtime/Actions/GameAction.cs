using System.Collections;

namespace Cards.Actions
{
    /// <summary>
    /// 所有游戏指令的抽象基类
    /// 封装了逻辑修改和动画等待的过程
    /// </summary>
    public abstract class GameAction
    {
        public bool IsCompleted { get; protected set; } = false;

        /// <summary>
        /// 核心执行协程：把 逻辑修改 + 动画等待 全都包在这个协程里
        /// 继承的子类需要实现具体的表现和逻辑
        /// </summary>
        public abstract IEnumerator ExecuteRoutine();

        /// <summary>
        /// Called each frame after ExecuteRoutine completes but IsCompleted is still false.
        /// Override for actions that need to wait for external conditions (e.g. player targeting input).
        /// </summary>
        public virtual void Tick() { }
    }
}
