using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Cards.Data;
using Cards.Decks;

namespace Cards.Editor
{
    /// <summary>
    /// 自定义 Inspector，为 DeckConfig 提供一键导入/导出 JSON 的按钮
    /// </summary>
    [CustomEditor(typeof(DeckConfig))]
    public class DeckConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制原本的面板
            DrawDefaultInspector();

            DeckConfig deck = (DeckConfig)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Deck JSON Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Export to JSON"))
            {
                ExportToJson(deck);
            }

            if (GUILayout.Button("Import from JSON"))
            {
                ImportFromJson(deck);
            }
        }

        private void ExportToJson(DeckConfig deck)
        {
            string path = EditorUtility.SaveFilePanel("Export Deck as JSON", "Assets", $"{deck.deckName}.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            DeckSaveData saveData = new DeckSaveData
            {
                deckName = deck.deckName
            };

            foreach (var card in deck.cards)
            {
                if (card != null)
                {
                    string cardId = !string.IsNullOrWhiteSpace(card.CardId) ? card.CardId : card.name;
                    saveData.cardIds.Add(cardId);
                }
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            Debug.Log($"卡组导出成功：{path}");
        }

        private void ImportFromJson(DeckConfig deck)
        {
            string path = EditorUtility.OpenFilePanel("Import Deck from JSON", "Assets", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = File.ReadAllText(path);
            DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("JSON 解析失败！");
                return;
            }

            // 1. 找到项目中所有的 CardData 以便匹配
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            Dictionary<string, CardData> allAvailableCards = new Dictionary<string, CardData>();
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                CardData cardAsset = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                if (cardAsset == null) continue;

                string cardId = !string.IsNullOrWhiteSpace(cardAsset.CardId) ? cardAsset.CardId : cardAsset.name;
                if (!allAvailableCards.ContainsKey(cardId))
                {
                    allAvailableCards.Add(cardId, cardAsset);
                }
            }

            // 2. 更新当前 DeckConfig
            Undo.RecordObject(deck, "Import Deck JSON"); // 支持 Ctrl+Z 撤销
            
            deck.deckName = saveData.deckName;
            deck.cards.Clear();

            foreach (string cardId in saveData.cardIds)
            {
                if (allAvailableCards.TryGetValue(cardId, out CardData foundCard))
                {
                    deck.cards.Add(foundCard);
                }
                else
                {
                    Debug.LogWarning($"在项目中找不到 CardId 为 '{cardId}' 的卡牌，已跳过。");
                }
            }

            EditorUtility.SetDirty(deck); // 标记已修改并保存
            Debug.Log($"卡组导入成功！导入了 {deck.cards.Count} 张卡牌。");
        }
    }
}