using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cards.Actions;
using Cards.Core;
using Cards.Core.Events;
using Cards.Data;
using Cards.Effects;
using Cards.Rules.Handlers;
using Cards.Rules.Interactions;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class GameFlowTests
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
        public void FullTurn_DrawPlayDamageDeath_CompletesWithoutStaticServices()
        {
            GameContext context = GameContext.CreateForTest(
                random: new Cards.Tests.MockRandom(5, 5, 5, 5),
                logger: new NullLogger());

            CardZone drawPile = RegisterZone(context, ZoneId.PlayerDrawPile, "Draw");
            CardZone discardPile = RegisterZone(context, ZoneId.PlayerDiscardPile, "Discard");
            CardZone exhaustPile = RegisterZone(context, ZoneId.PlayerExhaustPile, "Exhaust");
            CardZone handZone = RegisterZone(context, ZoneId.PlayerHand, "Hand");
            CardZone playerBoard = RegisterZone(context, ZoneId.PlayerBoard, "PlayerBoard");
            CardZone enemyBoard = RegisterZone(context, ZoneId.EnemyBoard, "EnemyBoard");

            playerBoard.AddRule(new AttackRule());
            playerBoard.AddRule(new PlayEffectRule());
            playerBoard.AddRule(new PlayEntityRule());
            playerBoard.AddRule(new CardDispositionRule());

            EventToken moveRequestToken = context.Events.Subscribe<RequestMoveCardEvent>(evt =>
            {
                CardZone sourceZone = evt.SourceZone ?? evt.Card?.CurrentZone;
                CardZone targetZone = context.Zones.Get(evt.TargetZoneId);
                context.ZoneTransfers.MoveCard(evt.Card, targetZone, sourceZone);
            });

            var deathHandler = new CardDeathHandler(context);

            try
            {
                CardInstance spellCard = CreateSpellCard("Firebolt", 3);
                CardInstance enemy = CreateEntityCard("Goblin", CardOwner.Enemy, health: 3);
                drawPile.AddCard(spellCard);
                enemyBoard.AddCard(enemy);

                new DrawCardAction().Execute(context);

                Assert.That(handZone.Contains(spellCard), Is.True);

                var request = new InteractionRequest
                {
                    Context = context,
                    Type = InteractionType.PlayCard,
                    SourceCard = spellCard,
                    SourceZone = handZone,
                    SourceZoneId = ZoneId.PlayerHand,
                    TargetZone = playerBoard,
                    TargetZoneId = ZoneId.PlayerBoard,
                    TargetEntity = enemy
                };

                context.Rules.ProcessInteraction(request);

                Assert.That(request.IsHandled, Is.True);
                Assert.That(handZone.Contains(spellCard), Is.False);
                Assert.That(discardPile.Contains(spellCard), Is.True);
                Assert.That(enemyBoard.Contains(enemy), Is.False);
                Assert.That(exhaustPile.Contains(enemy), Is.False);
                Assert.That(enemy.Model.CurrentHealth, Is.LessThanOrEqualTo(0));
            }
            finally
            {
                deathHandler.Dispose();
                context.Events.Unsubscribe(moveRequestToken);
            }
        }

        private CardZone RegisterZone(GameContext context, ZoneId zoneId, string zoneName)
        {
            var zone = new CardZone(zoneId, zoneName);
            context.Zones.Register(zone);
            return zone;
        }

        private CardInstance CreateSpellCard(string name, int damage)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.CardId = name;
            data.CardName = name;
            data.Health = 1;
            data.ArmorClass = 10;
            data.Tags.Add(CardTag.Action);
            data.PlayEffects.Add(new DamageEffect
            {
                useAttackRoll = false,
                attackValue = damage
            });
            createdObjects.Add(data);
            return new CardInstance(data, CardOwner.Player, new NullLogger());
        }

        private CardInstance CreateEntityCard(string name, CardOwner owner, int health)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.CardId = name;
            data.CardName = name;
            data.Health = health;
            data.ArmorClass = 10;
            data.Tags.Add(CardTag.Entity);
            createdObjects.Add(data);
            return new CardInstance(data, owner, new NullLogger());
        }
    }
}
