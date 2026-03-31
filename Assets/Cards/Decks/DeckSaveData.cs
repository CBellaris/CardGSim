using System;
using System.Collections.Generic;
using Cards.Data;

namespace Cards.Decks
{
    /// <summary>
    /// 用于 JSON 序列化/反序列化的纯数据结构
    /// </summary>
    [Serializable]
    public class DeckSaveData
    {
        public string deckName;
        public List<string> cardIds = new List<string>();

        // 从存档数据重建 DeckConfig
        public void LoadIntoConfig(DeckConfig config, CardDatabase database)
        {
            config.cards.Clear();
            foreach (string id in cardIds)
            {
                CardData data = database.GetCardById(id);
                if (data != null)
                {
                    config.cards.Add(data);
                }
            }
        }
    }

    // 假设你有一个包含所有卡牌引用的数据库类
    public class CardDatabase
    {
        private Dictionary<string, CardData> cards = new Dictionary<string, CardData>();

        public CardData GetCardById(string id)
        {
            cards.TryGetValue(id, out CardData data);
            return data;
        }
    }
}