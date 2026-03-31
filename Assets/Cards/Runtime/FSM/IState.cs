namespace Cards.FSM
{
    public interface IState
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void Enter();

        /// <summary>
        /// 每帧更新时调用
        /// </summary>
        void Update();

        /// <summary>
        /// 离开状态时调用
        /// </summary>
        void Exit();
    }
}
