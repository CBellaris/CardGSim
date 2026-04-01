using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cards.Core;
using Cards.Data;
using Cards.Services;
using Cards.Zones;

namespace Cards.Tests.EditMode
{
    public class CardZoneTests
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
        public void AddRemoveDrawTakeAll_UpdateZoneStateAndEvents()
        {
            var zone = new CardZone(ZoneId.PlayerHand, "Hand");
            var first = CreateCard("First");
            var second = CreateCard("Second");
            int addedCount = 0;
            int removedCount = 0;

            zone.OnCardAdded += (_, _) => addedCount++;
            zone.OnCardRemoved += _ => removedCount++;

            zone.AddCard(first);
            zone.AddCard(second);
            Assert.That(zone.Count, Is.EqualTo(2));
            Assert.That(first.CurrentZone, Is.EqualTo(zone));
            Assert.That(second.CurrentZoneId, Is.EqualTo(ZoneId.PlayerHand));
            Assert.That(addedCount, Is.EqualTo(2));

            CardInstance drawn = zone.DrawTopCard();
            Assert.That(drawn, Is.EqualTo(second));
            Assert.That(drawn.CurrentZone, Is.Null);
            Assert.That(zone.Count, Is.EqualTo(1));
            Assert.That(removedCount, Is.EqualTo(1));

            List<CardInstance> remaining = zone.TakeAllCards();
            Assert.That(remaining.Count, Is.EqualTo(1));
            Assert.That(remaining[0], Is.EqualTo(first));
            Assert.That(zone.IsEmpty, Is.True);
            Assert.That(removedCount, Is.EqualTo(2));
        }

        [Test]
        public void Shuffle_ReordersCards_AndRaisesEvent()
        {
            var zone = new CardZone(ZoneId.PlayerDrawPile, "Draw");
            var first = CreateCard("First");
            var second = CreateCard("Second");
            var third = CreateCard("Third");
            int shuffledCount = 0;

            zone.AddCard(first);
            zone.AddCard(second);
            zone.AddCard(third);
            zone.OnShuffled += () => shuffledCount++;

            zone.Shuffle(new Cards.Tests.MockRandom(2, 2, 2));

            Assert.That(shuffledCount, Is.EqualTo(1));
            Assert.That(zone.Cards[0], Is.EqualTo(third));
            Assert.That(zone.Cards[1], Is.EqualTo(first));
            Assert.That(zone.Cards[2], Is.EqualTo(second));
        }

        private CardInstance CreateCard(string name)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.CardId = name;
            data.CardName = name;
            data.Health = 5;
            data.ArmorClass = 10;
            createdObjects.Add(data);
            return new CardInstance(data, CardOwner.Player, new NullLogger());
        }
    }
}
