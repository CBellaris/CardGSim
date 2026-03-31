using System.Collections.Generic;
using UnityEngine;
using Cards.Core;
using Cards.Zones;

namespace Cards.Levels
{
    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "CardGame/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Settings")]
        public string levelName;
        
        [Header("Zones Configuration")]
        public List<ZoneConfigData> zones = new List<ZoneConfigData>();
    }
}