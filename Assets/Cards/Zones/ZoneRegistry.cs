using System.Collections.Generic;

namespace Cards.Zones
{
    /// <summary>
    /// 全局区域注册中心
    /// 解除底层组件对 GameManager 的依赖
    /// </summary>
    public static class ZoneRegistry
    {
        private static readonly Dictionary<ZoneId, List<CardZone>> zonesById = new Dictionary<ZoneId, List<CardZone>>();
        private static readonly Dictionary<string, CardZone> zonesByName = new Dictionary<string, CardZone>();

        public static void Register(ZoneId id, CardZone zone)
        {
            Register(zone);
        }

        public static void Register(CardZone zone)
        {
            if (zone == null) return;

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

        public static CardZone Get(ZoneId id, int index = 0)
        {
            if (zonesById.TryGetValue(id, out List<CardZone> zones) && index >= 0 && index < zones.Count)
            {
                return zones[index];
            }

            return null;
        }

        public static CardZone Get(string zoneName)
        {
            return !string.IsNullOrEmpty(zoneName) && zonesByName.TryGetValue(zoneName, out CardZone zone) ? zone : null;
        }

        public static IReadOnlyList<CardZone> GetAll(ZoneId id)
        {
            if (zonesById.TryGetValue(id, out List<CardZone> zones))
            {
                return zones;
            }

            return System.Array.Empty<CardZone>();
        }
        
        public static void Clear()
        {
            zonesById.Clear();
            zonesByName.Clear();
        }
    }
}