using System;
using System.Collections.Generic;
using Cards.Services;

namespace Cards.Zones
{
    public class ZoneRegistry : IZoneRegistry
    {
        private readonly Dictionary<ZoneId, List<CardZone>> zonesById = new Dictionary<ZoneId, List<CardZone>>();
        private readonly Dictionary<string, CardZone> zonesByName = new Dictionary<string, CardZone>();

        public void Register(CardZone zone)
        {
            if (zone == null)
            {
                return;
            }

            if (!zonesById.TryGetValue(zone.ZoneId, out List<CardZone> zones))
            {
                zones = new List<CardZone>();
                zonesById.Add(zone.ZoneId, zones);
            }

            if (!zones.Contains(zone))
            {
                zones.Add(zone);
            }

            if (!string.IsNullOrEmpty(zone.ZoneName))
            {
                zonesByName[zone.ZoneName] = zone;
            }
        }

        public CardZone Get(ZoneId id, int index = 0)
        {
            if (zonesById.TryGetValue(id, out List<CardZone> zones) && index >= 0 && index < zones.Count)
            {
                return zones[index];
            }

            return null;
        }

        public CardZone Get(string zoneName)
        {
            return !string.IsNullOrEmpty(zoneName) && zonesByName.TryGetValue(zoneName, out CardZone zone)
                ? zone
                : null;
        }

        public IReadOnlyList<CardZone> GetAll(ZoneId id)
        {
            if (zonesById.TryGetValue(id, out List<CardZone> zones))
            {
                return zones;
            }

            return Array.Empty<CardZone>();
        }

        public void Clear()
        {
            zonesById.Clear();
            zonesByName.Clear();
        }
    }
}
