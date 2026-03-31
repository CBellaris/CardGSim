using NUnit.Framework;

namespace Cards.Tests.EditMode
{
    public class RuntimeAssemblyTests
    {
        [Test]
        public void ZoneId_EnumValues_Exist()
        {
            Assert.That(System.Enum.GetValues(typeof(Zones.ZoneId)).Length, Is.GreaterThan(0));
        }

        [Test]
        public void CardTag_EnumValues_Exist()
        {
            Assert.That(System.Enum.GetValues(typeof(Data.CardTag)).Length, Is.GreaterThan(0));
        }

        [Test]
        public void GameStateMachine_CanInstantiate()
        {
            var sm = new FSM.GameStateMachine();
            Assert.That(sm, Is.Not.Null);
            Assert.That(sm.CurrentState, Is.Null);
        }
    }
}
