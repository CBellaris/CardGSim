using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cards.Actions;
using Cards.Core;
using Cards.Core.Events;
using Cards.Data;
using Cards.Rules.Interactions;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class RuleEngineTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void AttackRule_Hit_EnqueuesDamageActionAndAppliesDamage()
        {
            var queue = new RecordingActionQueue();
            var context = CreateContext(queue, 10);
            var attacker = CreateCard("Attacker", tags: new[] { CardTag.Entity }, attack: 3, attackBonus: 5);
            var target = CreateCard("Target", tags: new[] { CardTag.Entity }, health: 8, armorClass: 10);
            var sourceZone = RegisterZone(context, ZoneId.PlayerBoard, "PlayerBoard");
            var targetZone = RegisterZone(context, ZoneId.EnemyBoard, "EnemyBoard");
            sourceZone.AddRule(new AttackRule());
            sourceZone.AddCard(attacker);
            targetZone.AddCard(target);

            var request = new InteractionRequest
            {
                Context = context,
                Type = InteractionType.Attack,
                SourceCard = attacker,
                SourceZone = sourceZone,
                SourceZoneId = ZoneId.PlayerBoard,
                TargetZone = targetZone,
                TargetZoneId = ZoneId.EnemyBoard,
                TargetEntity = target
            };

            context.Rules.ProcessInteraction(request);

            Assert.That(request.IsHandled, Is.True);
            Assert.That(queue.EnqueuedActions.Count, Is.EqualTo(1));
            Assert.That(queue.EnqueuedActions[0], Is.TypeOf<DamageAction>());

            queue.EnqueuedActions[0].Execute(context);
            Assert.That(target.Model.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void PlayEntityRule_MovesEntityCardToBoard()
        {
            var context = CreateContext(new RecordingActionQueue(), 1);
            var hand = RegisterZone(context, ZoneId.PlayerHand, "Hand");
            var board = RegisterZone(context, ZoneId.PlayerBoard, "Board");
            board.AddRule(new PlayEntityRule());
            var entityCard = CreateCard("Soldier", tags: new[] { CardTag.Entity });
            hand.AddCard(entityCard);

            var request = new InteractionRequest
            {
                Context = context,
                Type = InteractionType.PlayCard,
                SourceCard = entityCard,
                SourceZone = hand,
                SourceZoneId = ZoneId.PlayerHand,
                TargetZone = board,
                TargetZoneId = ZoneId.PlayerBoard
            };

            context.Rules.ProcessInteraction(request);

            Assert.That(request.IsHandled, Is.True);
            Assert.That(hand.Contains(entityCard), Is.False);
            Assert.That(board.Contains(entityCard), Is.True);
            Assert.That(entityCard.CurrentZone, Is.EqualTo(board));
        }

        [Test]
        public void CardDispositionRule_PublishesMoveRequestForNonEntity()
        {
            var context = CreateContext(new RecordingActionQueue(), 1);
            var hand = RegisterZone(context, ZoneId.PlayerHand, "Hand");
            var board = RegisterZone(context, ZoneId.PlayerBoard, "Board");
            board.AddRule(new CardDispositionRule());
            var spellCard = CreateCard("Spell", tags: new[] { CardTag.Action });
            hand.AddCard(spellCard);

            RequestMoveCardEvent published = null;
            context.Events.Subscribe<RequestMoveCardEvent>(evt => published = evt);

            var request = new InteractionRequest
            {
                Context = context,
                Type = InteractionType.PlayCard,
                SourceCard = spellCard,
                SourceZone = hand,
                SourceZoneId = ZoneId.PlayerHand,
                TargetZone = board,
                TargetZoneId = ZoneId.PlayerBoard
            };

            context.Rules.ProcessInteraction(request);

            Assert.That(request.IsHandled, Is.True);
            Assert.That(hand.Contains(spellCard), Is.False);
            Assert.That(published, Is.Not.Null);
            Assert.That(published.Card, Is.EqualTo(spellCard));
            Assert.That(published.TargetZoneId, Is.EqualTo(ZoneId.PlayerDiscardPile));
        }

        private GameContext CreateContext(IActionQueue queue, params int[] randomValues)
        {
            return GameContext.CreateForTest(
                new Cards.Tests.MockRandom(randomValues),
                new NullLogger(),
                new TestAnimationPolicy(),
                queue);
        }

        private CardZone RegisterZone(GameContext context, ZoneId zoneId, string zoneName)
        {
            var zone = new CardZone(zoneId, zoneName);
            context.Zones.Register(zone);
            return zone;
        }

        private CardInstance CreateCard(
            string name,
            IEnumerable<CardTag> tags = null,
            int health = 5,
            int armorClass = 10,
            int attack = 0,
            int attackBonus = 0)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.CardId = name;
            data.CardName = name;
            data.Health = health;
            data.ArmorClass = armorClass;
            data.Attack = attack;
            data.AttackBonus = attackBonus;
            if (tags != null)
            {
                data.Tags.AddRange(tags);
            }

            createdObjects.Add(data);
            return new CardInstance(data, CardOwner.Player, new NullLogger());
        }

        private sealed class TestAnimationPolicy : IAnimationPolicy
        {
            public bool IsEnabled => false;
            public float TimeScale => 0f;
        }

        private sealed class RecordingActionQueue : IActionQueue
        {
            public List<GameAction> EnqueuedActions { get; } = new List<GameAction>();
            public bool IsProcessing => false;

            public void Enqueue(GameAction action)
            {
                EnqueuedActions.Add(action);
            }
        }
    }
}
