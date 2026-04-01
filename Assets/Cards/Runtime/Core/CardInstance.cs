using Cards.Data;
using Cards.Services;
using Cards.Zones;

namespace Cards.Core
{
    public class CardInstance : ICombatable
    {
        public CardModel Model { get; }
        public ICardData Data => Model?.Data;
        public CardOwner Owner { get; }

        public CardZone CurrentZone { get; internal set; }
        public ZoneId? CurrentZoneId { get; internal set; }
        public string CurrentZoneName => CurrentZone?.ZoneName;

        public CardInstance(ICardData data, CardOwner owner, ILogger logger = null)
        {
            Model = new CardModel(data, owner, logger);
            Owner = owner;
        }

        public string CombatName => Model?.CombatName ?? "Unknown";
        public int ArmorClass => Model?.ArmorClass ?? 0;
        public int AttackBonus => Model?.AttackBonus ?? 0;
        public int Attack => Model?.Attack ?? 0;
        public int DiceCount => Model?.DiceCount ?? 0;
        public int DiceSides => Model?.DiceSides ?? 0;

        public void TakeDamage(int damage)
        {
            Model?.TakeDamage(damage);
        }
    }
}
