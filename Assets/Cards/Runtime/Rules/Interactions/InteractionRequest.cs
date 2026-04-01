using Cards.Core;
using Cards.Services;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
    public enum InteractionType
    {
        ZoneTransfer,
        PlayCard,
        Attack
    }

    public class InteractionRequest
    {
        public InteractionType Type { get; set; } = InteractionType.PlayCard;
        public GameContext Context { get; set; }

        public CardInstance SourceCard { get; set; }
        public CardZone SourceZone { get; set; }
        public ZoneId SourceZoneId { get; set; }

        public CardZone TargetZone { get; set; }
        public ZoneId TargetZoneId { get; set; }
        public CardInstance TargetEntity { get; set; }

        public bool IsCancelled { get; set; }
        public bool IsHandled { get; set; }
    }
}
