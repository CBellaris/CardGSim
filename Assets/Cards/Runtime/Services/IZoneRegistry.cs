using System.Collections.Generic;
using Cards.Zones;

namespace Cards.Services
{
    public interface IZoneRegistry
    {
        void Register(CardZone zone);
        CardZone Get(ZoneId id, int index = 0);
        CardZone Get(string zoneName);
        IReadOnlyList<CardZone> GetAll(ZoneId id);
        void Clear();
    }
}
