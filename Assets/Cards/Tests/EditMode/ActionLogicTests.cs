using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cards.Actions;
using Cards.Core;
using Cards.Data;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class ActionLogicTests
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
        public void DamageAction_Execute_AppliesDamageSynchronously()
        {
            var target = new TestCombatable("Training Dummy", 12);
            var ctx = CreateContext();
            var action = new DamageAction(target, 4, "Unit Test");

            action.Execute(ctx);

            Assert.That(target.CurrentHealth, Is.EqualTo(8));
        }

        [Test]
        public void DrawCardAction_Execute_MovesTopCardToHand()
        {
            var ctx = CreateContext();
            CardZone drawPile = RegisterZone(ctx, ZoneId.PlayerDrawPile, "DrawPile");
            CardZone discardPile = RegisterZone(ctx, ZoneId.PlayerDiscardPile, "DiscardPile");
            CardZone handZone = RegisterZone(ctx, ZoneId.PlayerHand, "HandZone");
            CardInstance card = CreateCard("Strike");
            drawPile.AddCard(card);

            var action = new DrawCardAction();

            action.Execute(ctx);

            Assert.That(drawPile.Count, Is.EqualTo(0));
            Assert.That(discardPile.Count, Is.EqualTo(0));
            Assert.That(handZone.Count, Is.EqualTo(1));
            Assert.That(handZone.Contains(card), Is.True);
        }

        [Test]
        public void DrawCardAction_Execute_WhenDrawPileEmpty_EnqueuesReshuffleAndRetry()
        {
            var queue = new RecordingActionQueue();
            var ctx = CreateContext(queue: queue);
            RegisterZone(ctx, ZoneId.PlayerDrawPile, "DrawPile");
            CardZone discardPile = RegisterZone(ctx, ZoneId.PlayerDiscardPile, "DiscardPile");
            RegisterZone(ctx, ZoneId.PlayerHand, "HandZone");
            discardPile.AddCard(CreateCard("Recycle"));
            var action = new DrawCardAction();

            action.Execute(ctx);

            Assert.That(queue.EnqueuedActions.Count, Is.EqualTo(2));
            Assert.That(queue.EnqueuedActions[0], Is.TypeOf<ReshuffleAction>());
            Assert.That(queue.EnqueuedActions[1], Is.TypeOf<DrawCardAction>());
        }

        [Test]
        public void ReshuffleAction_Execute_RebuildsDrawPileAndResetsCardStats()
        {
            var ctx = CreateContext();
            CardZone drawPile = RegisterZone(ctx, ZoneId.PlayerDrawPile, "DrawPile");
            CardZone discardPile = RegisterZone(ctx, ZoneId.PlayerDiscardPile, "DiscardPile");
            CardInstance firstCard = CreateCard("Scout", health: 6);
            CardInstance secondCard = CreateCard("Knight", health: 8);
            firstCard.Model.TakeDamage(3);
            secondCard.Model.TakeDamage(5);
            discardPile.AddCard(firstCard);
            discardPile.AddCard(secondCard);

            var action = new ReshuffleAction(drawPile, discardPile);

            action.Execute(ctx);

            Assert.That(discardPile.Count, Is.EqualTo(0));
            Assert.That(drawPile.Count, Is.EqualTo(2));
            Assert.That(drawPile.Contains(firstCard), Is.True);
            Assert.That(drawPile.Contains(secondCard), Is.True);
            Assert.That(firstCard.Model.CurrentHealth, Is.EqualTo(6));
            Assert.That(secondCard.Model.CurrentHealth, Is.EqualTo(8));
        }

        private GameContext CreateContext(IActionQueue queue = null)
        {
            return GameContext.CreateForTest(
                new MockRandom(0, 1, 0, 1, 0, 1),
                new NullLogger(),
                new TestAnimationPolicy(false, 0f),
                queue ?? new RecordingActionQueue());
        }

        private CardZone RegisterZone(GameContext context, ZoneId zoneId, string zoneName)
        {
            var zone = new CardZone(zoneId, zoneName);
            context.Zones.Register(zone);
            return zone;
        }

        private CardInstance CreateCard(string cardName, int health = 5)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.CardId = cardName;
            data.CardName = cardName;
            data.Health = health;
            data.ArmorClass = 10;
            createdObjects.Add(data);

            return new CardInstance(data, CardOwner.Player, new NullLogger());
        }

        private sealed class TestAnimationPolicy : IAnimationPolicy
        {
            public bool IsEnabled { get; }
            public float TimeScale { get; }

            public TestAnimationPolicy(bool isEnabled, float timeScale)
            {
                IsEnabled = isEnabled;
                TimeScale = timeScale;
            }
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

        private sealed class TestCombatable : ICombatable
        {
            public string CombatName { get; }
            public int ArmorClass { get; }
            public int AttackBonus => 0;
            public int Attack => 0;
            public int DiceCount => 0;
            public int DiceSides => 0;
            public int CurrentHealth { get; private set; }

            public TestCombatable(string combatName, int health, int armorClass = 10)
            {
                CombatName = combatName;
                ArmorClass = armorClass;
                CurrentHealth = health;
            }

            public void TakeDamage(int damage)
            {
                CurrentHealth -= damage;
            }
        }
    }
}
