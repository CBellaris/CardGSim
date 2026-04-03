using System.Collections.Generic;
using NUnit.Framework;
using Cards.Core;
using Cards.Data;
using Cards.FSM;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class GameSessionFlowTests
    {
        [Test]
        public void Start_DealsInitialHandWithoutGameManager()
        {
            GameContext context = CreateContext();
            CardZone drawPile = RegisterZone(context, ZoneId.PlayerDrawPile, "DrawPile");
            CardZone handZone = RegisterZone(context, ZoneId.PlayerHand, "HandZone");
            RegisterZone(context, ZoneId.PlayerDiscardPile, "DiscardPile");
            RegisterZone(context, ZoneId.PlayerExhaustPile, "ExhaustPile");
            RegisterZone(context, ZoneId.PlayerBoard, "PlayerBoard");
            RegisterZone(context, ZoneId.EnemyBoard, "EnemyBoard");

            drawPile.AddCards(new[]
            {
                CreateCard("Card-1"),
                CreateCard("Card-2"),
                CreateCard("Card-3"),
                CreateCard("Card-4")
            });

            GameSession session = CreateSession(context);

            session.Start();

            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.GameSetup));
            Assert.That(handZone.Count, Is.EqualTo(3));
            Assert.That(drawPile.Count, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterPlayerTurnStart_AutoTransitionsToMainPhase()
        {
            GameContext context = CreateContext();
            CardZone handZone = RegisterStandardZonesWithDeck(context, 5);
            GameSession session = CreateSession(context);

            session.Start();
            session.Tick();
            session.Tick();

            Assert.That(handZone.Count, Is.EqualTo(4));
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.PlayerMainPhase));
        }

        [Test]
        public void TryEndTurn_FromMainPhase_TransitionsToEnemyTurn()
        {
            GameContext context = CreateContext();
            RegisterStandardZonesWithDeck(context, 5);
            GameSession session = CreateSession(context);

            AdvanceToPlayerMainPhase(session);

            bool accepted = session.TryEndTurn();
            session.Tick();

            Assert.That(accepted, Is.True);
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.EnemyTurn));
        }

        [Test]
        public void EnemyTurn_CompletesAndReturnsToPlayerTurnStart()
        {
            GameContext context = CreateContext();
            RegisterStandardZonesWithDeck(context, 5);
            GameSession session = CreateSession(context);

            AdvanceToPlayerMainPhase(session);
            session.TryEndTurn();
            session.Tick();
            session.Tick();

            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.PlayerTurnStart));
        }

        private static GameContext CreateContext()
        {
            return GameContext.CreateForTest(
                random: new Cards.Tests.MockRandom(0, 1, 2, 0, 1, 2),
                logger: new NullLogger(),
                input: new TestInputProvider(),
                time: new TestTimeProvider(1f));
        }

        private static GameSession CreateSession(GameContext context)
        {
            return new GameSession(
                context,
                () => new Cards.Actions.DrawCardAction(),
                options: new GameSessionOptions
                {
                    InitialHandSize = 3,
                    TurnStartDrawCount = 1,
                    EnemyTurnDelaySeconds = 0f
                });
        }

        private static CardZone RegisterStandardZonesWithDeck(GameContext context, int cardCount)
        {
            CardZone drawPile = RegisterZone(context, ZoneId.PlayerDrawPile, "DrawPile");
            RegisterZone(context, ZoneId.PlayerDiscardPile, "DiscardPile");
            CardZone handZone = RegisterZone(context, ZoneId.PlayerHand, "HandZone");
            RegisterZone(context, ZoneId.PlayerExhaustPile, "ExhaustPile");
            RegisterZone(context, ZoneId.PlayerBoard, "PlayerBoard");
            RegisterZone(context, ZoneId.EnemyBoard, "EnemyBoard");

            var cards = new List<CardInstance>();
            for (int i = 0; i < cardCount; i++)
            {
                cards.Add(CreateCard($"Card-{i + 1}"));
            }

            drawPile.AddCards(cards);
            return handZone;
        }

        private static void AdvanceToPlayerMainPhase(GameSession session)
        {
            session.Start();
            session.Tick();
            session.Tick();
            Assert.That(session.CurrentPhase, Is.EqualTo(GamePhase.PlayerMainPhase));
        }

        private static CardZone RegisterZone(GameContext context, ZoneId zoneId, string zoneName)
        {
            var zone = new CardZone(zoneId, zoneName);
            context.Zones.Register(zone);
            return zone;
        }

        private static CardInstance CreateCard(string name)
        {
            return new CardInstance(new TestCardData(name), CardOwner.Player, new NullLogger());
        }

        private sealed class TestInputProvider : IInputProvider
        {
            public bool WasPressed(GameInputAction action)
            {
                return false;
            }
        }

        private sealed class TestTimeProvider : ITimeProvider
        {
            public TestTimeProvider(float deltaTime)
            {
                DeltaTime = deltaTime;
            }

            public float DeltaTime { get; }
        }

        private sealed class TestCardData : ICardData
        {
            public TestCardData(string cardName)
            {
                CardId = cardName;
                CardName = cardName;
                Tags = new List<CardTag>();
                PlayEffects = new List<Cards.Effects.ICardEffect>();
            }

            public string CardId { get; }
            public string CardName { get; }
            public int Cost => 0;
            public int Health => 1;
            public int ArmorClass => 10;
            public int AttackBonus => 0;
            public int Attack => 0;
            public int DiceCount => 0;
            public int DiceSides => 0;
            public IReadOnlyList<CardTag> Tags { get; }
            public IReadOnlyList<Cards.Effects.ICardEffect> PlayEffects { get; }
        }
    }
}
