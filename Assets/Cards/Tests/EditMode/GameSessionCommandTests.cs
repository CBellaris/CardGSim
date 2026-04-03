using System.Collections.Generic;
using NUnit.Framework;
using Cards.Actions;
using Cards.Commands;
using Cards.Core;
using Cards.Core.Events;
using Cards.Data;
using Cards.Effects;
using Cards.FSM;
using Cards.Rules.Interactions;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class GameSessionCommandTests
    {
        [Test]
        public void TryPlayCard_WhenNotInPlayerMainPhase_ReturnsRejected()
        {
            GameContext context = CreateContext(0);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);

            CardInstance spell = CreateSpellCard("Firebolt", 2);
            zones.Hand.AddCard(spell);

            GameSession session = CreateSession(context);

            GameCommandResult result = session.TryPlayCard(new PlayCardCommand(spell));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.Rejected));
            Assert.That(zones.Hand.Contains(spell), Is.True);
        }

        [Test]
        public void TryPlayCard_WhenCardIsNotInPlayerHand_ReturnsRejected()
        {
            GameContext context = CreateContext(0);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);

            CardInstance spell = CreateSpellCard("Firebolt", 2);
            zones.PlayerBoard.AddCard(spell);

            GameSession session = CreateSession(context);
            AdvanceToPlayerMainPhase(session);

            GameCommandResult result = session.TryPlayCard(new PlayCardCommand(spell));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.Rejected));
            Assert.That(zones.PlayerBoard.Contains(spell), Is.True);
        }

        [Test]
        public void TryPlayCard_AutoTarget_PrefersOpponentBoardCandidates()
        {
            GameContext context = CreateContext(1);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);
            BridgeMoveRequests(context);

            CardInstance spell = CreateSpellCard("Firebolt", 2);
            CardInstance ally = CreateEntityCard("Ally", CardOwner.Player, 5);
            CardInstance enemyA = CreateEntityCard("Enemy-A", CardOwner.Enemy, 5);
            CardInstance enemyB = CreateEntityCard("Enemy-B", CardOwner.Enemy, 5);

            zones.Hand.AddCard(spell);
            zones.PlayerBoard.AddCard(ally);
            zones.EnemyBoard.AddCard(enemyA);
            zones.EnemyBoard.AddCard(enemyB);

            GameSession session = CreateSession(context);
            AdvanceToPlayerMainPhase(session);

            GameCommandResult result = session.TryPlayCard(new PlayCardCommand(spell));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.Executed));
            Assert.That(result.Interaction.TargetEntity, Is.EqualTo(enemyB));
            Assert.That(enemyA.Model.CurrentHealth, Is.EqualTo(5));
            Assert.That(enemyB.Model.CurrentHealth, Is.EqualTo(3));
            Assert.That(ally.Model.CurrentHealth, Is.EqualTo(5));
            Assert.That(zones.Hand.Contains(spell), Is.False);
            Assert.That(zones.Discard.Contains(spell), Is.True);
        }

        [Test]
        public void TryPlayCard_ExplicitTarget_UsesSelectedTarget()
        {
            GameContext context = CreateContext(1);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);
            BridgeMoveRequests(context);

            CardInstance spell = CreateSpellCard("Firebolt", 2);
            CardInstance enemyA = CreateEntityCard("Enemy-A", CardOwner.Enemy, 5);
            CardInstance enemyB = CreateEntityCard("Enemy-B", CardOwner.Enemy, 5);

            zones.Hand.AddCard(spell);
            zones.EnemyBoard.AddCard(enemyA);
            zones.EnemyBoard.AddCard(enemyB);

            GameSession session = CreateSession(context);
            AdvanceToPlayerMainPhase(session);

            GameCommandResult result = session.TryPlayCard(
                new PlayCardCommand(
                    spell,
                    CommandTargetSelection.Explicit(enemyA)));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.Executed));
            Assert.That(result.Interaction.TargetEntity, Is.EqualTo(enemyA));
            Assert.That(enemyA.Model.CurrentHealth, Is.EqualTo(3));
            Assert.That(enemyB.Model.CurrentHealth, Is.EqualTo(5));
            Assert.That(zones.Discard.Contains(spell), Is.True);
        }

        [Test]
        public void TryPlayCard_DeferredTargetSelection_ReturnsCandidatesWithoutApplyingState()
        {
            GameContext context = CreateContext(0);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);
            BridgeMoveRequests(context);

            CardInstance spell = CreateSpellCard("Firebolt", 2);
            CardInstance enemyA = CreateEntityCard("Enemy-A", CardOwner.Enemy, 5);
            CardInstance enemyB = CreateEntityCard("Enemy-B", CardOwner.Enemy, 5);

            zones.Hand.AddCard(spell);
            zones.EnemyBoard.AddCard(enemyA);
            zones.EnemyBoard.AddCard(enemyB);

            GameSession session = CreateSession(context);
            AdvanceToPlayerMainPhase(session);

            GameCommandResult result = session.TryPlayCard(
                new PlayCardCommand(
                    spell,
                    CommandTargetSelection.Deferred()));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.AwaitingTargetSelection));
            Assert.That(result.AvailableTargets.Count, Is.EqualTo(2));
            Assert.That(result.AvailableTargets, Contains.Item(enemyA));
            Assert.That(result.AvailableTargets, Contains.Item(enemyB));
            Assert.That(zones.Hand.Contains(spell), Is.True);
            Assert.That(zones.Discard.Contains(spell), Is.False);
            Assert.That(enemyA.Model.CurrentHealth, Is.EqualTo(5));
            Assert.That(enemyB.Model.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void TryAttack_DeferredTargetSelection_ReturnsOpponentCandidatesOnly()
        {
            GameContext context = CreateContext(0);
            ZoneSet zones = RegisterStandardZones(context);
            ConfigureBoardRules(zones.PlayerBoard);

            CardInstance attacker = CreateEntityCard("Knight", CardOwner.Player, 5, attack: 3, attackBonus: 5);
            CardInstance ally = CreateEntityCard("Ally", CardOwner.Player, 5);
            CardInstance enemyA = CreateEntityCard("Enemy-A", CardOwner.Enemy, 5);
            CardInstance enemyB = CreateEntityCard("Enemy-B", CardOwner.Enemy, 5);

            zones.PlayerBoard.AddCard(attacker);
            zones.PlayerBoard.AddCard(ally);
            zones.EnemyBoard.AddCard(enemyA);
            zones.EnemyBoard.AddCard(enemyB);

            GameSession session = CreateSession(context);
            AdvanceToPlayerMainPhase(session);

            GameCommandResult result = session.TryAttack(
                new AttackCommand(
                    attacker,
                    CommandTargetSelection.Deferred()));

            Assert.That(result.Status, Is.EqualTo(GameCommandStatus.AwaitingTargetSelection));
            Assert.That(result.AvailableTargets.Count, Is.EqualTo(2));
            Assert.That(result.AvailableTargets, Contains.Item(enemyA));
            Assert.That(result.AvailableTargets, Contains.Item(enemyB));
            Assert.That(result.AvailableTargets, Has.No.Member(ally));
        }

        private static GameContext CreateContext(params int[] randomValues)
        {
            return GameContext.CreateForTest(
                random: new Cards.Tests.MockRandom(randomValues),
                logger: new NullLogger());
        }

        private static GameSession CreateSession(GameContext context)
        {
            return new GameSession(
                context,
                () => new DrawCardAction(),
                options: new GameSessionOptions
                {
                    InitialHandSize = 0,
                    TurnStartDrawCount = 0,
                    EnemyTurnDelaySeconds = 0f
                });
        }

        private static void AdvanceToPlayerMainPhase(GameSession session)
        {
            session.Start();
            session.Tick();
            session.Tick();
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.PlayerMainPhase));
        }

        private static ZoneSet RegisterStandardZones(GameContext context)
        {
            return new ZoneSet
            {
                DrawPile = RegisterZone(context, ZoneId.PlayerDrawPile, "DrawPile"),
                Discard = RegisterZone(context, ZoneId.PlayerDiscardPile, "DiscardPile"),
                Exhaust = RegisterZone(context, ZoneId.PlayerExhaustPile, "ExhaustPile"),
                Hand = RegisterZone(context, ZoneId.PlayerHand, "HandZone"),
                PlayerBoard = RegisterZone(context, ZoneId.PlayerBoard, "PlayerBoard"),
                EnemyBoard = RegisterZone(context, ZoneId.EnemyBoard, "EnemyBoard")
            };
        }

        private static CardZone RegisterZone(GameContext context, ZoneId zoneId, string zoneName)
        {
            var zone = new CardZone(zoneId, zoneName);
            context.Zones.Register(zone);
            return zone;
        }

        private static void ConfigureBoardRules(CardZone boardZone)
        {
            boardZone.AddRule(new AttackRule());
            boardZone.AddRule(new PlayEffectRule());
            boardZone.AddRule(new PlayEntityRule());
            boardZone.AddRule(new CardDispositionRule());
        }

        private static void BridgeMoveRequests(GameContext context)
        {
            context.Events.Subscribe<RequestMoveCardEvent>(evt =>
            {
                CardZone sourceZone = evt.SourceZone ?? evt.Card?.CurrentZone;
                CardZone targetZone = context.Zones.Get(evt.TargetZoneId);
                context.ZoneTransfers.MoveCard(evt.Card, targetZone, sourceZone);
            });
        }

        private static CardInstance CreateSpellCard(string name, int damage)
        {
            return new CardInstance(
                new TestCardData(
                    name,
                    CardOwner.Player,
                    tags: new[] { CardTag.Action },
                    playEffects: new ICardEffect[] { new TargetedDamageEffect(damage) },
                    health: 1),
                CardOwner.Player,
                new NullLogger());
        }

        private static CardInstance CreateEntityCard(
            string name,
            CardOwner owner,
            int health,
            int attack = 0,
            int attackBonus = 0)
        {
            return new CardInstance(
                new TestCardData(
                    name,
                    owner,
                    tags: new[] { CardTag.Entity },
                    health: health,
                    attack: attack,
                    attackBonus: attackBonus),
                owner,
                new NullLogger());
        }

        private sealed class ZoneSet
        {
            public CardZone DrawPile { get; set; }
            public CardZone Discard { get; set; }
            public CardZone Exhaust { get; set; }
            public CardZone Hand { get; set; }
            public CardZone PlayerBoard { get; set; }
            public CardZone EnemyBoard { get; set; }
        }

        private sealed class TestCardData : ICardData
        {
            public TestCardData(
                string name,
                CardOwner owner,
                IEnumerable<CardTag> tags = null,
                IEnumerable<ICardEffect> playEffects = null,
                int health = 5,
                int armorClass = 10,
                int attack = 0,
                int attackBonus = 0,
                int diceCount = 0,
                int diceSides = 0)
            {
                CardId = name;
                CardName = name;
                Owner = owner;
                Health = health;
                ArmorClass = armorClass;
                Attack = attack;
                AttackBonus = attackBonus;
                DiceCount = diceCount;
                DiceSides = diceSides;
                Tags = tags != null ? new List<CardTag>(tags) : new List<CardTag>();
                PlayEffects = playEffects != null
                    ? new List<ICardEffect>(playEffects)
                    : new List<ICardEffect>();
            }

            public string CardId { get; }
            public string CardName { get; }
            public CardOwner Owner { get; }
            public int Cost => 0;
            public int Health { get; }
            public int ArmorClass { get; }
            public int AttackBonus { get; }
            public int Attack { get; }
            public int DiceCount { get; }
            public int DiceSides { get; }
            public IReadOnlyList<CardTag> Tags { get; }
            public IReadOnlyList<ICardEffect> PlayEffects { get; }
        }

        private sealed class TargetedDamageEffect : ICardEffect
        {
            private readonly int damage;

            public TargetedDamageEffect(int damage)
            {
                this.damage = damage;
            }

            public bool CanExecute(InteractionRequest request, out string failureReason)
            {
                if (request?.TargetEntity == null)
                {
                    failureReason = "Target required.";
                    return false;
                }

                failureReason = null;
                return true;
            }

            public List<GameAction> Execute(InteractionRequest request)
            {
                return new List<GameAction>
                {
                    new DamageAction(
                        request.TargetEntity,
                        damage,
                        request.SourceCard?.Data?.CardName ?? "Unknown")
                };
            }
        }
    }
}
