using System.Collections.Generic;
using UnityEngine;
using Cards.Data;

namespace Cards.Decks
{
    /// <summary>
    /// 卡组配置文件
    /// 支持在编辑器中直接拖拽配置卡牌，也可以通过 Editor 脚本与 JSON 进行互相导入/导出
    /// </summary>
    [CreateAssetMenu(fileName = "NewDeckConfig", menuName = "Cards/Deck Config")]
    public class DeckConfig : ScriptableObject
    {
        [Header("Deck Identity")]
        public string deckName = "My Custom Deck";
        
        [Header("Card List")]
        // 这里存储着卡组里所有的卡牌数据引用
        public List<CardData> cards = new List<CardData>();
    }
}