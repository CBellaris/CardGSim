using System;
using UnityEngine;

namespace Cards.Zones
{
    public enum LayoutType
    {
        None,
        Pile,
        Line
    }

    [Serializable]
    public class ZoneConfigData
    {
        public ZoneId zoneId;
        [Tooltip("兼容旧场景绑定和调试日志用途，不再作为运行时主键。")]
        public string zoneName;
        public LayoutType layoutType;
        
        [Header("Layout Parameters")]
        public float spacingOrOffset = 0.02f; // 对 Pile 是 ZOffset，对 Line 是 Spacing
        public Vector3 layoutAxis = Vector3.right; // 对 Line 有效，展开方向
    }
}