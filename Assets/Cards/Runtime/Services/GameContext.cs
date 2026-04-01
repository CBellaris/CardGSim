using Cards.Core;
using Cards.Core.Events;
using Cards.Rules.Interactions;
using Cards.Zones;

namespace Cards.Services
{
    /// <summary>
    /// 服务容器，统一承载 Runtime 逻辑所需的依赖。
    /// </summary>
    public class GameContext
    {
        public IRandom Random { get; }
        public ILogger Logger { get; }
        public DiceRoller Dice { get; }
        public CombatResolver Combat { get; }
        public IAnimationPolicy AnimationPolicy { get; }
        public IActionQueue Actions { get; }
        public IEventBus Events { get; }
        public IZoneRegistry Zones { get; }
        public IRuleEngine Rules { get; }
        public ZoneTransferService ZoneTransfers { get; }
        public IInputProvider Input { get; }
        public ITimeProvider Time { get; }

        public GameContext(
            IRandom random,
            ILogger logger,
            IAnimationPolicy animationPolicy = null,
            IActionQueue actions = null)
            : this(
                random,
                logger,
                animationPolicy,
                actions,
                null,
                null,
                null,
                null,
                null,
                null)
        {
        }

        public GameContext(
            IRandom random,
            ILogger logger,
            IAnimationPolicy animationPolicy,
            IActionQueue actions,
            IEventBus events,
            IZoneRegistry zones,
            IRuleEngine rules,
            ZoneTransferService zoneTransfers,
            IInputProvider input,
            ITimeProvider time)
        {
            Random = random ?? new SystemRandomAdapter();
            Logger = logger ?? new NullLogger();
            Dice = new DiceRoller(Random);
            Combat = new CombatResolver(Random, Dice);
            AnimationPolicy = animationPolicy ?? new DisabledAnimationPolicy();
            Actions = actions ?? new SynchronousActionQueue();
            Events = events ?? new EventBus();
            Zones = zones ?? new ZoneRegistry();
            ZoneTransfers = zoneTransfers ?? new ZoneTransferService();
            Rules = rules ?? new RuleEngine(ZoneTransfers);
            Input = input ?? new NullInputProvider();
            Time = time ?? new FixedTimeProvider();

            if (Actions is SynchronousActionQueue synchronousQueue)
            {
                synchronousQueue.Initialize(this);
            }
        }

        public static GameContext CreateForTest(
            IRandom random = null,
            ILogger logger = null,
            IAnimationPolicy animationPolicy = null,
            IActionQueue actions = null,
            IEventBus events = null,
            IZoneRegistry zones = null,
            IRuleEngine rules = null,
            ZoneTransferService zoneTransfers = null,
            IInputProvider input = null,
            ITimeProvider time = null)
        {
            return new GameContext(
                random,
                logger,
                animationPolicy,
                actions,
                events,
                zones,
                rules,
                zoneTransfers,
                input,
                time);
        }

        private sealed class SystemRandomAdapter : IRandom
        {
            private readonly System.Random random = new System.Random(0);

            public int Range(int min, int max)
            {
                return random.Next(min, max);
            }
        }

        private sealed class DisabledAnimationPolicy : IAnimationPolicy
        {
            public bool IsEnabled => false;
            public float TimeScale => 0f;
        }

        private sealed class NullInputProvider : IInputProvider
        {
            public bool WasPressed(GameInputAction action)
            {
                return false;
            }
        }

        private sealed class FixedTimeProvider : ITimeProvider
        {
            public float DeltaTime => 0f;
        }

        private sealed class SynchronousActionQueue : IActionQueue
        {
            private readonly System.Collections.Generic.Queue<Cards.Actions.GameAction> pendingActions =
                new System.Collections.Generic.Queue<Cards.Actions.GameAction>();
            private GameContext context;
            private bool isProcessing;

            public bool IsProcessing => isProcessing || pendingActions.Count > 0;

            public void Initialize(GameContext gameContext)
            {
                context = gameContext;
            }

            public void Enqueue(Cards.Actions.GameAction action)
            {
                if (action == null)
                {
                    return;
                }

                pendingActions.Enqueue(action);
                if (!isProcessing)
                {
                    ProcessQueue();
                }
            }

            private void ProcessQueue()
            {
                if (context == null)
                {
                    return;
                }

                isProcessing = true;
                while (pendingActions.Count > 0)
                {
                    Cards.Actions.GameAction action = pendingActions.Dequeue();
                    action.Execute(context);
                    action.MarkCompleted();
                }

                isProcessing = false;
            }
        }
    }
}
