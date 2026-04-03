using System;
using System.Collections.Generic;
using Cards.Actions;
using Cards.Commands;
using Cards.Core;
using Cards.Data;
using Cards.Effects;
using Cards.Rules.Interactions;
using Cards.Services;
using Cards.Zones;

namespace Cards.FSM
{
    public sealed class GameSession
    {
        private readonly Func<GameAction> createDrawCardAction;
        private readonly IGameSessionBootstrap bootstrap;
        private readonly GameSessionOptions options;
        private float enemyTurnDelayRemaining;
        private bool hasStarted;

        public GameSession(
            GameContext context,
            Func<GameAction> createDrawCardAction,
            IGameSessionBootstrap bootstrap = null,
            GameSessionOptions options = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            this.createDrawCardAction = createDrawCardAction ?? throw new ArgumentNullException(nameof(createDrawCardAction));
            this.bootstrap = bootstrap;
            this.options = options ?? GameSessionOptions.Default;
            CurrentPhase = GamePhase.None;
        }

        public GameContext Context { get; }
        public GamePhase CurrentPhase { get; private set; }
        public bool HasStarted => hasStarted;
        public bool IsBusy => Context.Actions?.IsProcessing == true;
        public bool AcceptsPlayerInput => CurrentPhase == GamePhase.PlayerMainPhase && !IsBusy;

        public event Action<GamePhase> PhaseChanged;

        public void Start()
        {
            if (hasStarted)
            {
                return;
            }

            if (bootstrap != null && !bootstrap.Initialize(Context))
            {
                return;
            }

            hasStarted = true;
            TransitionTo(GamePhase.GameSetup);
        }

        public void Tick()
        {
            if (!hasStarted)
            {
                return;
            }

            switch (CurrentPhase)
            {
                case GamePhase.GameSetup:
                    if (!IsBusy)
                    {
                        TransitionTo(GamePhase.PlayerTurnStart);
                    }
                    break;
                case GamePhase.PlayerTurnStart:
                    if (!IsBusy)
                    {
                        TransitionTo(GamePhase.PlayerMainPhase);
                    }
                    break;
                case GamePhase.PlayerTurnEnd:
                    if (!IsBusy)
                    {
                        TransitionTo(GamePhase.EnemyTurn);
                    }
                    break;
                case GamePhase.EnemyTurn:
                    TickEnemyTurn();
                    break;
            }
        }

        public GameCommandResult TryDrawCard(DrawCardCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!AcceptsPlayerInput)
            {
                return GameCommandResult.Rejected("当前不是可操作的玩家主阶段，无法抽牌。");
            }

            EnqueueDrawActions(1);
            return GameCommandResult.Executed("已受理抽牌命令。");
        }

        public bool TryDrawCard()
        {
            return TryDrawCard(new DrawCardCommand()).Succeeded;
        }

        public GameCommandResult TryPlayCard(PlayCardCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!AcceptsPlayerInput)
            {
                return GameCommandResult.Rejected("当前不是可操作的玩家主阶段，无法打出卡牌。");
            }

            CardInstance sourceCard = command.Card;
            CardZone handZone = Context.Zones?.Get(ZoneId.PlayerHand);
            if (sourceCard == null)
            {
                return GameCommandResult.Rejected("缺少要打出的卡牌。");
            }

            if (handZone == null || !handZone.Contains(sourceCard))
            {
                return GameCommandResult.Rejected("只能从玩家手牌区打出卡牌。");
            }

            ZoneId targetBoardId = sourceCard.Owner.GetBoardZoneId();
            CardZone targetBoardZone = Context.Zones?.Get(targetBoardId);
            if (targetBoardZone == null)
            {
                return GameCommandResult.Rejected($"未找到可用战场区域: {targetBoardId}");
            }

            bool requiresTarget = RequiresPlayTarget(sourceCard, handZone, targetBoardZone);
            GameCommandResult targetResult = ResolvePlayTarget(command, requiresTarget, out CardInstance target);
            if (targetResult != null)
            {
                return targetResult;
            }

            var request = new InteractionRequest
            {
                Context = Context,
                Type = InteractionType.PlayCard,
                SourceCard = sourceCard,
                SourceZone = handZone,
                SourceZoneId = ZoneId.PlayerHand,
                TargetZone = targetBoardZone,
                TargetZoneId = targetBoardId,
                TargetEntity = target
            };

            Context.Rules?.ProcessInteraction(request);
            return BuildInteractionResult(request, $"已受理出牌命令: {sourceCard.Data?.CardName ?? "Unknown"}");
        }

        public GameCommandResult TryAttack(AttackCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!AcceptsPlayerInput)
            {
                return GameCommandResult.Rejected("当前不是可操作的玩家主阶段，无法发动攻击。");
            }

            CardInstance sourceCard = command.Card;
            if (sourceCard == null)
            {
                return GameCommandResult.Rejected("缺少攻击发起者。");
            }

            CardZone sourceZone = sourceCard.CurrentZone;
            if (sourceZone == null ||
                sourceCard.CurrentZoneId != ZoneId.PlayerBoard ||
                !sourceZone.Contains(sourceCard))
            {
                return GameCommandResult.Rejected("只有玩家战场上的实体可以发动攻击。");
            }

            if (!IsEntity(sourceCard))
            {
                return GameCommandResult.Rejected("只有实体牌可以发动攻击。");
            }

            GameCommandResult targetResult = ResolveAttackTarget(command, out CardInstance target);
            if (targetResult != null)
            {
                return targetResult;
            }

            var request = new InteractionRequest
            {
                Context = Context,
                Type = InteractionType.Attack,
                SourceCard = sourceCard,
                SourceZone = sourceZone,
                SourceZoneId = ZoneId.PlayerBoard,
                TargetZone = target?.CurrentZone,
                TargetZoneId = target?.CurrentZoneId ?? sourceCard.Owner.GetOpponent().GetBoardZoneId(),
                TargetEntity = target
            };

            Context.Rules?.ProcessInteraction(request);
            return BuildInteractionResult(request, $"已受理攻击命令: {sourceCard.Data?.CardName ?? "Unknown"}");
        }

        public GameCommandResult TryEndTurn(EndTurnCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!AcceptsPlayerInput)
            {
                return GameCommandResult.Rejected("当前不是可操作的玩家主阶段，无法结束回合。");
            }

            TransitionTo(GamePhase.PlayerTurnEnd);
            return GameCommandResult.Executed("已受理结束回合命令。");
        }

        public bool TryEndTurn()
        {
            return TryEndTurn(new EndTurnCommand()).Succeeded;
        }

        private void TickEnemyTurn()
        {
            if (IsBusy)
            {
                return;
            }

            enemyTurnDelayRemaining -= Context.Time?.DeltaTime ?? 0f;
            if (enemyTurnDelayRemaining <= 0f)
            {
                TransitionTo(GamePhase.PlayerTurnStart);
            }
        }

        private void TransitionTo(GamePhase nextPhase)
        {
            if (CurrentPhase == nextPhase)
            {
                return;
            }

            CurrentPhase = nextPhase;
            PhaseChanged?.Invoke(CurrentPhase);

            switch (CurrentPhase)
            {
                case GamePhase.GameSetup:
                    EnterGameSetup();
                    break;
                case GamePhase.PlayerTurnStart:
                    EnterPlayerTurnStart();
                    break;
                case GamePhase.PlayerMainPhase:
                    Log("[GameSession] 进入玩家主阶段。");
                    break;
                case GamePhase.PlayerTurnEnd:
                    Log("[GameSession] 进入玩家回合结束阶段。");
                    break;
                case GamePhase.EnemyTurn:
                    EnterEnemyTurn();
                    break;
            }
        }

        private void EnterGameSetup()
        {
            Log("[GameSession] 初始化游戏会话。");
            EnqueueDrawActions(options.InitialHandSize);
        }

        private void EnterPlayerTurnStart()
        {
            Log("[GameSession] 玩家回合开始。");
            EnqueueDrawActions(options.TurnStartDrawCount);
        }

        private void EnterEnemyTurn()
        {
            Log("[GameSession] 敌方回合开始。");
            enemyTurnDelayRemaining = options.EnemyTurnDelaySeconds;
            ExecuteEnemyTurn();
        }

        private void ExecuteEnemyTurn()
        {
            CardZone enemyBoard = Context.Zones?.Get(ZoneId.EnemyBoard);
            if (enemyBoard == null || enemyBoard.Count == 0)
            {
                Log("[GameSession] 场上没有敌方单位，敌方回合跳过。");
                return;
            }

            foreach (Cards.Core.CardInstance entity in enemyBoard.Cards)
            {
                if (entity == null)
                {
                    continue;
                }

                Log($"[GameSession] 敌方单位待执行行动: {entity.Data?.CardName ?? "Unknown"}");
            }
        }

        private void Log(string message)
        {
            Context.Logger?.Log(message);
        }

        private void EnqueueDrawActions(int drawCount)
        {
            for (int i = 0; i < drawCount; i++)
            {
                GameAction drawAction = createDrawCardAction();
                if (drawAction == null)
                {
                    Context.Logger?.LogError("[GameSession] 无法创建抽牌动作。");
                    return;
                }

                Context.Actions?.Enqueue(drawAction);
            }
        }

        private GameCommandResult ResolvePlayTarget(
            PlayCardCommand command,
            bool requiresTarget,
            out CardInstance target)
        {
            target = null;

            List<CardInstance> preferredTargets = GetTargetsFromZones(
                command.Card,
                command.Card.Owner.GetOpponent().GetBoardZoneId());
            List<CardInstance> fallbackTargets = GetTargetsFromZones(
                command.Card,
                ZoneId.PlayerBoard,
                ZoneId.EnemyBoard);
            List<CardInstance> availableTargets = GetTargetsFromZones(
                command.Card,
                command.Card.Owner.GetOpponent().GetBoardZoneId(),
                ZoneId.PlayerBoard,
                ZoneId.EnemyBoard);

            switch (command.TargetSelection.Mode)
            {
                case TargetSelectionMode.Explicit:
                    if (!IsValidTarget(command.TargetSelection.ExplicitTarget, availableTargets))
                    {
                        return GameCommandResult.Rejected("指定的出牌目标无效。");
                    }

                    target = command.TargetSelection.ExplicitTarget;
                    return null;

                case TargetSelectionMode.Deferred:
                    if (!requiresTarget)
                    {
                        return null;
                    }

                    if (availableTargets.Count == 0)
                    {
                        return GameCommandResult.Rejected("当前没有可用的出牌目标。");
                    }

                    return GameCommandResult.AwaitingTargetSelection(
                        "该命令需要先选择一个目标。",
                        availableTargets);

                default:
                    List<CardInstance> autoCandidates = preferredTargets.Count > 0
                        ? preferredTargets
                        : fallbackTargets;
                    if (autoCandidates.Count == 0)
                    {
                        return requiresTarget
                            ? GameCommandResult.Rejected("当前没有可用的出牌目标。")
                            : null;
                    }

                    target = ChooseRandomTarget(autoCandidates);
                    return null;
            }
        }

        private GameCommandResult ResolveAttackTarget(AttackCommand command, out CardInstance target)
        {
            target = null;

            List<CardInstance> availableTargets = GetTargetsFromZones(
                command.Card,
                command.Card.Owner.GetOpponent().GetBoardZoneId());

            switch (command.TargetSelection.Mode)
            {
                case TargetSelectionMode.Explicit:
                    if (!IsValidTarget(command.TargetSelection.ExplicitTarget, availableTargets))
                    {
                        return GameCommandResult.Rejected("指定的攻击目标无效。");
                    }

                    target = command.TargetSelection.ExplicitTarget;
                    return null;

                case TargetSelectionMode.Deferred:
                    if (availableTargets.Count == 0)
                    {
                        return GameCommandResult.Rejected("当前没有可攻击的目标。");
                    }

                    return GameCommandResult.AwaitingTargetSelection(
                        "该攻击命令需要先选择一个目标。",
                        availableTargets);

                default:
                    if (availableTargets.Count == 0)
                    {
                        return GameCommandResult.Rejected("当前没有可攻击的目标。");
                    }

                    target = ChooseRandomTarget(availableTargets);
                    return null;
            }
        }

        private bool RequiresPlayTarget(
            CardInstance sourceCard,
            CardZone sourceZone,
            CardZone targetZone)
        {
            IReadOnlyList<ICardEffect> effects = sourceCard?.Data?.PlayEffects;
            if (effects == null || effects.Count == 0)
            {
                return false;
            }

            var probeRequest = new InteractionRequest
            {
                Context = Context,
                Type = InteractionType.PlayCard,
                SourceCard = sourceCard,
                SourceZone = sourceZone,
                SourceZoneId = sourceZone?.ZoneId ?? ZoneId.PlayerHand,
                TargetZone = targetZone,
                TargetZoneId = targetZone?.ZoneId ?? sourceCard.Owner.GetBoardZoneId()
            };

            foreach (ICardEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (!effect.CanExecute(probeRequest, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private List<CardInstance> GetTargetsFromZones(CardInstance sourceCard, params ZoneId[] zoneIds)
        {
            var targets = new List<CardInstance>();
            if (sourceCard == null || zoneIds == null)
            {
                return targets;
            }

            foreach (ZoneId zoneId in zoneIds)
            {
                IReadOnlyList<CardZone> zones = Context.Zones?.GetAll(zoneId);
                if (zones == null)
                {
                    continue;
                }

                foreach (CardZone zone in zones)
                {
                    if (zone == null)
                    {
                        continue;
                    }

                    foreach (CardInstance card in zone.Cards)
                    {
                        if (card == null || card == sourceCard || targets.Contains(card))
                        {
                            continue;
                        }

                        targets.Add(card);
                    }
                }
            }

            return targets;
        }

        private CardInstance ChooseRandomTarget(IReadOnlyList<CardInstance> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            IRandom random = Context.Random;
            int index = random != null ? random.Range(0, candidates.Count) : 0;
            return candidates[index];
        }

        private static bool IsValidTarget(CardInstance target, IReadOnlyList<CardInstance> candidates)
        {
            if (target == null || candidates == null)
            {
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEntity(CardInstance card)
        {
            IReadOnlyList<CardTag> tags = card?.Data?.Tags;
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == CardTag.Entity)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameCommandResult BuildInteractionResult(
            InteractionRequest request,
            string successMessage)
        {
            if (request == null)
            {
                return GameCommandResult.Rejected("命令请求无效。");
            }

            if (request.IsCancelled)
            {
                return GameCommandResult.Rejected("命令被规则拒绝。");
            }

            if (!request.IsHandled)
            {
                return GameCommandResult.Rejected("命令未被任何规则处理。");
            }

            return GameCommandResult.Executed(successMessage, request);
        }
    }
}
