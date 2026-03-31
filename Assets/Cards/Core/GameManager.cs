using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cards.Data;
using Cards.Decks;
using Cards.Actions; // 引入指令队列命名空间
using Cards.FSM; // 引入状态机命名空间
using Cards.FSM.States;
using Cards.Zones; // 引入区域命名空间
using Cards.Zones.Layouts; // 引入布局命名空间
using Cards.Levels; // 引入关卡命名空间
using Cards.Rules.Interactions;
using Cards.Core.Events;

namespace Cards.Core
{
    public class GameManager : MonoBehaviour
    {
    [Header("Deck Config")]
    // 现在引用的是外部的 DeckConfig 资源，而不是直接在这里维护卡牌列表
    [SerializeField] private DeckConfig initialDeckConfig; 
    
    [Header("Spawn Setup")]
    // 引用第三步做好的 3D Prefab
    [SerializeField] private GameObject cardPrefab; 
    
    [Header("Level Setup")]
    // 替换掉原来的 DesktopConfig
    [SerializeField] private LevelZoneSetup levelSetup;
        
    // 核心重构：将所有零散的列表替换为 CardZone 实例
    private CardZone handZone;
    private CardZone playerBoardZone;
    private CardZone enemyBoardZone;
    private CardZone drawPile;
    private CardZone discardPile;
    private CardZone exhaustPile;

    // 提供给外部和状态机的访问器
    public CardZone HandZone => handZone;
    public CardZone BoardZone => playerBoardZone;
    public CardZone PlayerBoardZone => playerBoardZone;
    public CardZone EnemyBoardZone => enemyBoardZone;
    public CardZone DrawPile => drawPile;
    public CardZone DiscardPile => discardPile;
    public CardZone ExhaustPile => exhaustPile;

    // 兼容原有的属性，方便 EnemyTurnState 读取
    public IReadOnlyList<CardEntity> EntitiesOnBoard => playerBoardZone != null ? playerBoardZone.Cards : new List<CardEntity>();

    // 状态机实例
    public GameStateMachine StateMachine { get; private set; }

    // Cached FSM state instances (reused to avoid GC allocation on every state transition)
    public GameSetupState GameSetup { get; private set; }
    public PlayerTurnStartState PlayerTurnStart { get; private set; }
    public PlayerMainPhaseState PlayerMainPhase { get; private set; }
    public PlayerTurnEndState PlayerTurnEnd { get; private set; }
    public EnemyTurnState EnemyTurn { get; private set; }

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 确保场景中有 ActionManager
        if (FindObjectOfType<ActionManager>() == null)
        {
            GameObject amObj = new GameObject("ActionManager");
            amObj.AddComponent<ActionManager>();
        }

        StateMachine = new GameStateMachine();

        GameSetup = new GameSetupState(this);
        PlayerTurnStart = new PlayerTurnStartState(this);
        PlayerMainPhase = new PlayerMainPhaseState(this);
        PlayerTurnEnd = new PlayerTurnEndState(this);
        EnemyTurn = new EnemyTurnState(this);

        // 订阅事件总线
        EventBus.Subscribe<RequestMoveCardEvent>(OnRequestMoveCard);
        
        // 注册独立的事件处理器
        Cards.Rules.Handlers.CardPlayHandler.Initialize();
        Cards.Rules.Handlers.CardDeathHandler.Initialize();
    }

    private void OnDestroy()
    {
        // 取消订阅或清空总线以防止内存泄漏
        EventBus.Clear();
        ZoneRegistry.Clear();
    }

    private void OnRequestMoveCard(RequestMoveCardEvent evt)
    {
        var targetZone = ZoneRegistry.Get(evt.TargetZoneId);
        if (targetZone == null)
        {
            Debug.LogWarning($"[GameManager] 未找到目标区域: {evt.TargetZoneId}");
            return;
        }

        var sourceZone = evt.SourceZone ?? evt.Card?.CurrentZone;
        if (!ZoneTransferService.MoveCard(evt.Card, targetZone, evt.UseAnimation, sourceZone))
        {
            Debug.LogWarning($"[GameManager] 卡牌移动失败: {evt.Card?.CurrentCardData?.CardName ?? "Unknown"} -> {evt.TargetZoneId}");
        }
    }

    void Start()
    {
        if (levelSetup == null)
        {
            Debug.LogError("LevelZoneSetup 未配置！");
            return;
        }

        StateMachine.Initialize(GameSetup);
    }

    public void InitializePiles()
    {
        // 1. 让 LevelZoneSetup 根据 SO 配置和场景锚点生成所有的 Zone
        levelSetup.InitializeZones();

        // 2. 从 LevelZoneSetup 中获取生成的 Zone 实例，赋给 GameManager 的快捷引用。
        // 对当前玩法仍默认使用每种 ZoneId 的第一个实例，但底层已经允许同类型区域存在多个实例。
        drawPile = levelSetup.GetZone(ZoneId.PlayerDrawPile);
        discardPile = levelSetup.GetZone(ZoneId.PlayerDiscardPile);
        exhaustPile = levelSetup.GetZone(ZoneId.PlayerExhaustPile);
        handZone = levelSetup.GetZone(ZoneId.PlayerHand);
        playerBoardZone = levelSetup.GetZone(ZoneId.PlayerBoard);
        enemyBoardZone = levelSetup.GetZone(ZoneId.EnemyBoard);

        // 如果配置里没配 ExhaustPile，给个默认的兜底防止报错
        if (exhaustPile == null) exhaustPile = new CardZone(ZoneId.PlayerExhaustPile, "FallbackPlayerExhaustPile");

        // 将所有区域注册到 ZoneRegistry，解耦底层组件对 GameManager 的依赖。
        foreach (CardZone zone in levelSetup.GetAllZones())
        {
            ZoneRegistry.Register(zone);
        }

        ZoneRegistry.Register(exhaustPile);

        // 3. 挂载全局规则（后续这些也可以配置在 LevelConfig 中）
        if (playerBoardZone != null)
        {
            playerBoardZone.AddRule(new Cards.Rules.Interactions.AttackRule());           // Priority 20: 实体攻击
            playerBoardZone.AddRule(new Cards.Rules.Interactions.PlayEffectRule());       // Priority 15: 执行卡牌效果
            playerBoardZone.AddRule(new Cards.Rules.Interactions.PlayEntityRule());       // Priority 10: 实体进场
            playerBoardZone.AddRule(new Cards.Rules.Interactions.CardDispositionRule());  // Priority  5: 非实体牌去向
        }

        if (enemyBoardZone != null)
        {
            enemyBoardZone.AddRule(new Cards.Rules.Interactions.AttackRule());
            enemyBoardZone.AddRule(new Cards.Rules.Interactions.PlayEffectRule());
            enemyBoardZone.AddRule(new Cards.Rules.Interactions.PlayEntityRule());
            enemyBoardZone.AddRule(new Cards.Rules.Interactions.CardDispositionRule());
        }
    }

    public void InitializeDeck()
    {
        if (initialDeckConfig == null || initialDeckConfig.cards == null)
        {
            Debug.LogError("未配置初始卡组 (DeckConfig)！");
            return;
        }

        // 1. 根据配置初始化卡牌实体并放在抽牌堆
        List<CardEntity> initialCards = new List<CardEntity>();
        
        // 我们不再需要硬编码找 drawPilePoint，因为位置逻辑已经封装在 Layout 里了
        // 这里只是给个初始出生点（甚至可以是 Vector3.zero），加入 drawPile 后会自动飞过去
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        foreach (CardData data in initialDeckConfig.cards)
        {
            if (data == null) continue;

            GameObject newCardObj = Instantiate(cardPrefab, spawnPos, spawnRot);
            CardEntity cardEntity = newCardObj.GetComponent<CardEntity>();
            cardEntity.SetupCard(data, CardOwner.Player);
            
            initialCards.Add(cardEntity);
        }

        drawPile.AddCards(initialCards, false);
        drawPile.Shuffle();
    }

    // 更新函数里处理状态机的轮询
    void Update()
    {
        StateMachine?.Update();
    }

    public CardZone GetBoardZone(CardOwner owner)
    {
        return owner == CardOwner.Enemy ? enemyBoardZone : playerBoardZone;
    }
}
}