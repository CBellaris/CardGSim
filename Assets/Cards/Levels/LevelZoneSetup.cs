using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cards.Zones;
using Cards.Zones.Layouts;
using Cards.Core;

namespace Cards.Levels
{
    [Serializable]
    public class ZoneTransformBinding
    {
        [Tooltip("推荐直接使用 ZoneId 绑定锚点。")]
        public ZoneId zoneId;
        [Tooltip("兼容旧配置：如果填写了 zoneName，会优先按名字匹配。")]
        public string zoneName;
        public Transform anchorPoint;
    }

    /// <summary>
    /// 挂载在场景中，负责将 LevelConfig (数据) 和 场景 Transform (表现) 结合，
    /// 并生成实际的 CardZone 实例供 GameManager 使用。
    /// 替代了原来的 DesktopConfig。
    /// </summary>
    public class LevelZoneSetup : MonoBehaviour
    {
        [Header("Level Data")]
        public LevelConfig levelConfig;

        [Header("Scene Anchors")]
        [Tooltip("将配置中的 zoneName 与场景中的实际 Transform 锚点绑定")]
        public List<ZoneTransformBinding> bindings = new List<ZoneTransformBinding>();

        // 运行时按“区域类型”和“区域实例名”双索引，允许同一个 ZoneId 存在多个实例。
        private Dictionary<ZoneId, List<CardZone>> activeZonesById = new Dictionary<ZoneId, List<CardZone>>();
        private Dictionary<string, CardZone> activeZonesByName = new Dictionary<string, CardZone>();

        public void InitializeZones()
        {
            if (levelConfig == null)
            {
                Debug.LogError("LevelZoneSetup 缺少 LevelConfig 配置！");
                return;
            }

            activeZonesById.Clear();
            activeZonesByName.Clear();

            foreach (var zoneData in levelConfig.zones)
            {
                if (!string.IsNullOrEmpty(zoneData.zoneName) && activeZonesByName.ContainsKey(zoneData.zoneName))
                {
                    Debug.LogError($"[LevelSetup] 存在重复的区域名称: {zoneData.zoneName}，请为同类型区域提供唯一实例名。");
                    continue;
                }

                // 查找绑定的 Transform 锚点
                Transform anchor = GetAnchorForZone(zoneData);
                
                IZoneLayout layout = null;
                
                // 根据配置创建 Layout
                if (zoneData.layoutType == LayoutType.Pile && anchor != null)
                {
                    layout = new PileLayout(anchor, zoneData.spacingOrOffset);
                }
                else if (zoneData.layoutType == LayoutType.Line && anchor != null)
                {
                    layout = new LineLayout(anchor, zoneData.spacingOrOffset, zoneData.layoutAxis);
                }

                CardZone newZone = new CardZone(zoneData.zoneId, zoneData.zoneName, layout);

                if (!activeZonesById.TryGetValue(zoneData.zoneId, out List<CardZone> zonesOfType))
                {
                    zonesOfType = new List<CardZone>();
                    activeZonesById.Add(zoneData.zoneId, zonesOfType);
                }

                zonesOfType.Add(newZone);

                if (!string.IsNullOrEmpty(zoneData.zoneName))
                {
                    activeZonesByName.Add(zoneData.zoneName, newZone);
                }

                Debug.Log($"[LevelSetup] 已初始化区域: {zoneData.zoneId} ({zoneData.zoneName}), Layout: {zoneData.layoutType}");
            }
        }

        private Transform GetAnchorForZone(ZoneConfigData zoneData)
        {
            var bindingByName = bindings.Find(b => !string.IsNullOrEmpty(b.zoneName) && b.zoneName == zoneData.zoneName);
            if (bindingByName != null)
            {
                return bindingByName.anchorPoint;
            }

            var bindingById = bindings.Find(b => string.IsNullOrEmpty(b.zoneName) && b.zoneId == zoneData.zoneId);
            return bindingById?.anchorPoint;
        }

        public CardZone GetZone(ZoneId zoneId, int index = 0)
        {
            if (activeZonesById.TryGetValue(zoneId, out List<CardZone> zones) && index >= 0 && index < zones.Count)
            {
                return zones[index];
            }
            Debug.LogWarning($"[LevelSetup] 未找到区域: {zoneId} (index={index})");
            return null;
        }

        public CardZone GetZone(string zoneName)
        {
            if (!string.IsNullOrEmpty(zoneName) && activeZonesByName.TryGetValue(zoneName, out CardZone zone))
            {
                return zone;
            }

            Debug.LogWarning($"[LevelSetup] 未找到区域实例: {zoneName}");
            return null;
        }

        public IReadOnlyList<CardZone> GetZones(ZoneId zoneId)
        {
            if (activeZonesById.TryGetValue(zoneId, out List<CardZone> zones))
            {
                return zones;
            }

            return Array.Empty<CardZone>();
        }

        public IReadOnlyList<CardZone> GetAllZones()
        {
            return activeZonesById.Values.SelectMany(zones => zones).ToList();
        }
    }
}