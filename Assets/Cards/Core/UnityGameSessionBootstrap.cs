using System.Collections.Generic;
using UnityEngine;
using Cards.Data;
using Cards.Decks;
using Cards.FSM;
using Cards.Levels;
using Cards.Rules.Interactions;
using Cards.Services;
using Cards.Zones;

namespace Cards.Core
{
    public class UnityGameSessionBootstrap : IGameSessionBootstrap
    {
        private readonly DeckConfig initialDeckConfig;
        private readonly GameObject cardPrefab;
        private readonly LevelZoneSetup levelSetup;

        public UnityGameSessionBootstrap(
            DeckConfig initialDeckConfig,
            GameObject cardPrefab,
            LevelZoneSetup levelSetup)
        {
            this.initialDeckConfig = initialDeckConfig;
            this.cardPrefab = cardPrefab;
            this.levelSetup = levelSetup;
        }

        public bool Initialize(GameContext context)
        {
            if (context == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] 缺少 GameContext。");
                return false;
            }

            if (!InitializeZones(context, out CardZone drawPile))
            {
                return false;
            }

            return InitializeDeck(context, drawPile);
        }

        private bool InitializeZones(GameContext context, out CardZone drawPile)
        {
            drawPile = null;

            if (levelSetup == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] LevelZoneSetup 未配置。");
                return false;
            }

            context.Zones.Clear();
            levelSetup.InitializeZones(context);

            drawPile = levelSetup.GetZone(ZoneId.PlayerDrawPile);
            CardZone discardPile = levelSetup.GetZone(ZoneId.PlayerDiscardPile);
            CardZone handZone = levelSetup.GetZone(ZoneId.PlayerHand);
            CardZone playerBoardZone = levelSetup.GetZone(ZoneId.PlayerBoard);
            CardZone enemyBoardZone = levelSetup.GetZone(ZoneId.EnemyBoard);
            CardZone exhaustPile = levelSetup.GetZone(ZoneId.PlayerExhaustPile) ??
                                   new CardZone(ZoneId.PlayerExhaustPile, "FallbackPlayerExhaustPile");

            if (drawPile == null || discardPile == null || handZone == null || playerBoardZone == null || enemyBoardZone == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] 关卡缺少必要区域，无法启动会话。");
                return false;
            }

            foreach (CardZone zone in levelSetup.GetAllZones())
            {
                context.Zones.Register(zone);
            }

            context.Zones.Register(exhaustPile);

            ConfigureBoardRules(playerBoardZone);
            ConfigureBoardRules(enemyBoardZone);
            return true;
        }

        private bool InitializeDeck(GameContext context, CardZone drawPile)
        {
            if (initialDeckConfig == null || initialDeckConfig.cards == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] 未配置初始卡组。");
                return false;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] Card Prefab 未配置。");
                return false;
            }

            if (cardPrefab.GetComponent<CardEntityView>() == null)
            {
                Debug.LogError("[UnityGameSessionBootstrap] Card Prefab 缺少 CardEntityView。");
                return false;
            }

            var initialCards = new List<CardInstance>();

            foreach (CardData data in initialDeckConfig.cards)
            {
                if (data == null)
                {
                    continue;
                }

                GameObject newCardObject = Object.Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                CardEntityView cardView = newCardObject.GetComponent<CardEntityView>();
                var cardInstance = new CardInstance(data, CardOwner.Player, context.Logger);
                cardView.SetupCard(cardInstance, context);
                levelSetup.RegisterCardView(cardInstance, cardView);
                initialCards.Add(cardInstance);
            }

            drawPile.AddCards(initialCards);
            drawPile.Shuffle(context.Random);
            return true;
        }

        private static void ConfigureBoardRules(CardZone boardZone)
        {
            if (boardZone == null)
            {
                return;
            }

            boardZone.AddRule(new AttackRule());
            boardZone.AddRule(new PlayEffectRule());
            boardZone.AddRule(new PlayEntityRule());
            boardZone.AddRule(new CardDispositionRule());
        }
    }
}
