using UnityEngine;
using Cards.Actions;
using Cards.Commands;
using Cards.Decks;
using Cards.FSM;
using Cards.Levels;
using Cards.Rules.Interactions;
using Cards.Core.Events;
using Cards.Core.Services;
using Cards.Services;
using Cards.Zones;

namespace Cards.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Deck Config")]
        [SerializeField] private DeckConfig initialDeckConfig;

        [Header("Spawn Setup")]
        [SerializeField] private GameObject cardPrefab;

        [Header("Level Setup")]
        [SerializeField] private LevelZoneSetup levelSetup;

        [Header("Runtime Options")]
        [SerializeField] private bool disableAnimations = false;

        public GameContext Context { get; private set; }
        public GameSession Session { get; private set; }

        private EventToken cardClickSubscription;
        private EventToken moveRequestSubscription;
        private Cards.Rules.Handlers.CardDeathHandler cardDeathHandler;

        private void Awake()
        {
            ActionManager actionManager = FindObjectOfType<ActionManager>();
            if (actionManager == null)
            {
                GameObject amObj = new GameObject("ActionManager");
                actionManager = amObj.AddComponent<ActionManager>();
            }

            IAnimationPolicy animationPolicy = disableAnimations
                ? new InstantAnimationPolicy()
                : new LiveAnimationPolicy();
            var zoneTransfers = new ZoneTransferService();
            var ruleEngine = new RuleEngine(zoneTransfers);
            Context = new GameContext(
                new UnityRandom(),
                new UnityLogger(),
                animationPolicy,
                actionManager,
                new EventBus(),
                new ZoneRegistry(),
                ruleEngine,
                zoneTransfers,
                new UnityInputProvider(),
                new UnityTimeProvider());
            actionManager.Initialize(Context);

            Session = new GameSession(
                Context,
                () => new DrawCardAction(),
                new UnityGameSessionBootstrap(initialDeckConfig, cardPrefab, levelSetup));

            cardClickSubscription = Context.Events.Subscribe<CardClickedEvent>(OnCardClicked);
            moveRequestSubscription = Context.Events.Subscribe<RequestMoveCardEvent>(OnRequestMoveCard);

            cardDeathHandler = new Cards.Rules.Handlers.CardDeathHandler(Context);
        }

        private void OnDestroy()
        {
            cardDeathHandler?.Dispose();
            Context?.Events?.Unsubscribe(cardClickSubscription);
            Context?.Events?.Unsubscribe(moveRequestSubscription);
            Context?.Events?.Clear();
            Context?.Zones?.Clear();
        }

        private void OnCardClicked(CardClickedEvent evt)
        {
            if (evt?.Card == null)
            {
                return;
            }

            HandleCommandResult(Session?.TryPlayCard(new PlayCardCommand(evt.Card)));
        }

        private void OnRequestMoveCard(RequestMoveCardEvent evt)
        {
            var targetZone = Context?.Zones?.Get(evt.TargetZoneId);
            if (targetZone == null)
            {
                Debug.LogWarning($"[GameManager] 未找到目标区域: {evt.TargetZoneId}");
                return;
            }

            var sourceZone = evt.SourceZone ?? evt.Card?.CurrentZone;
            if (!Context.ZoneTransfers.MoveCard(evt.Card, targetZone, sourceZone))
            {
                Debug.LogWarning($"[GameManager] 卡牌移动失败: {evt.Card?.Data?.CardName ?? "Unknown"} -> {evt.TargetZoneId}");
            }
        }

        private void Start()
        {
            Session?.Start();
        }

        private void Update()
        {
            Session?.Tick();

            if (Context?.Input?.WasPressed(GameInputAction.DrawCard) == true)
            {
                HandleCommandResult(Session?.TryDrawCard(new DrawCardCommand()));
            }

            if (Context?.Input?.WasPressed(GameInputAction.EndTurn) == true)
            {
                HandleCommandResult(Session?.TryEndTurn(new EndTurnCommand()));
            }
        }

        private void HandleCommandResult(GameCommandResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.Message))
            {
                return;
            }

            if (result.Status == GameCommandStatus.Rejected)
            {
                Context?.Logger?.LogWarning(result.Message);
                return;
            }

            Context?.Logger?.Log(result.Message);
        }
    }
}
