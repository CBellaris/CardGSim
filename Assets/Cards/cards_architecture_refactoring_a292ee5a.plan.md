---
name: Cards Architecture Refactoring
overview: 将卡牌游戏模拟器的核心逻辑从 Unity 运行时中物理隔离出来，分 5 个可独立开发/验证的阶段完成：建立程序集边界、抽象平台原语、拆分动画/逻辑、分离实体数据与视图、去静态化服务。
todos:
  - id: phase-0
    content: "Phase 0: 创建 4 个 asmdef，提取 ZoneId/CardTag 枚举，移动 6 个纯 C# 文件到 Runtime/（IInteractionRule 因依赖 InteractionRequest→CardEntity 推迟到 Phase 3）"
    status: completed
  - id: phase-1
    content: "Phase 1: 创建 IRandom/ILogger/ICardData 接口 + GameContext，重构 DiceRoller/CombatResolver/CardModel，编写首批单元测试"
    status: pending
  - id: phase-2
    content: "Phase 2: GameAction 拆分 Execute()+AnimateRoutine()，创建 IAnimationPolicy/IActionQueue，重构 3 个 Action 子类，添加一键禁用动画"
    status: pending
  - id: phase-3
    content: "Phase 3: 创建 CardInstance，重构 CardZone 为纯逻辑，CardEntity→CardEntityView，更新所有 Rule/Effect 引用，创建 ZoneLayoutView"
    status: pending
  - id: phase-4
    content: "Phase 4: EventBus/ZoneRegistry/RuleEngine 去 static，完善 GameContext 最终版，编写完整游戏回合集成测试"
    status: pending
isProject: false
---

# Cards 架构重构详细计划

## 目标目录结构总览

```
Assets/Cards/
├── Cards.Unity.asmdef              ← Unity 层程序集（自动涵盖子目录中没有独立 asmdef 的文件）
├── Runtime/
│   ├── Cards.Runtime.asmdef        ← 纯 C# 逻辑程序集（零 UnityEngine 引用）
│   └── ...纯逻辑文件
├── Tests/
│   ├── Cards.Tests.asmdef          ← 测试程序集
│   └── EditMode/
├── Editor/
│   ├── Cards.Editor.asmdef         ← 编辑器程序集
│   └── ...
├── Core/, Actions/, FSM/, ...      ← Unity 依赖的文件留在原位
```

核心原则：`Runtime/` 文件夹中**不允许出现任何 `using UnityEngine`**，由 asmdef 的引用列表在编译层面强制保证。

---

## Phase 0：程序集边界 + 纯类型提取

**前置条件**: 无
**目标**: 建立物理隔离的编译边界，将已经是纯 C# 的类型移入 `Runtime/`
**预计改动**: 新建 4 个 asmdef + 8~10 个文件移动/提取

### 0.1 创建 Assembly Definition 文件

**新建文件**:

- `Assets/Cards/Runtime/Cards.Runtime.asmdef`
  - `autoReferenced: false`
  - `references: []`（不引用任何 Unity 模块）
  - `includePlatforms: []`（全平台）
  - `defineConstraints: []`
- `Assets/Cards/Cards.Unity.asmdef`
  - `references: ["Cards.Runtime"]`
  - `autoReferenced: true`
  - 放在 `Assets/Cards/` 根目录，自动覆盖 Core/, Actions/, Zones/, FSM/, Effects/, Rules/, Levels/, Decks/, Data/ 等子目录
- `Assets/Cards/Editor/Cards.Editor.asmdef`
  - `references: ["Cards.Unity", "Cards.Runtime"]`
  - `includePlatforms: ["Editor"]`
- `Assets/Cards/Tests/Cards.Tests.asmdef`
  - `references: ["Cards.Runtime", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`
  - `includePlatforms: ["Editor"]`
  - `defineConstraints: ["UNITY_INCLUDE_TESTS"]`
  - `optionalUnityReferences: ["TestAssemblies"]`

### 0.2 从现有文件中提取纯类型

以下类型当前被混合在包含 Unity 依赖的文件中，需要提取到独立文件并移入 `Runtime/`：

**从 [ZoneConfigData.cs](Assets/Cards/Levels/ZoneConfigData.cs) 中提取**:

- `ZoneId` 枚举 → `Assets/Cards/Runtime/Zones/ZoneId.cs`
- `LayoutType` 枚举保留在原文件（仅 Unity 层使用）

**从 [CardData.cs](Assets/Cards/Data/CardData.cs) 中提取**:

- `CardTag` 枚举 → `Assets/Cards/Runtime/Data/CardTag.cs`

### 0.3 移动已经是纯 C# 的文件到 Runtime/

逐个检查 `using`，移除无用的 `using UnityEngine`，然后移动：


| 原路径                                      | 新路径                                              | 需要的修改                                |
| ---------------------------------------- | ------------------------------------------------ | ------------------------------------ |
| `Core/ICombatable.cs`                    | `Runtime/Core/ICombatable.cs`                    | 无（已是纯 C#）                            |
| `Core/CardOwner.cs`                      | `Runtime/Core/CardOwner.cs`                      | `using Cards.Zones` 改为引用新的 ZoneId 位置 |
| `Zones/ZoneIdExtensions.cs`              | `Runtime/Zones/ZoneIdExtensions.cs`              | 命名空间不变                               |
| `FSM/IState.cs`                          | `Runtime/FSM/IState.cs`                          | 删除 `using UnityEngine`（未实际使用）        |
| `FSM/GameStateMachine.cs`                | `Runtime/FSM/GameStateMachine.cs`                | 删除 `using UnityEngine`（未实际使用）        |
| `Rules/Interactions/IInteractionRule.cs` | `Runtime/Rules/Interactions/IInteractionRule.cs` | 无                                    |
| `Actions/GameAction.cs`                  | `Runtime/Actions/GameAction.cs`                  | 删除 `using UnityEngine`（未实际使用）        |


### 0.4 修复编译

移动后，Unity 层的文件会通过 `Cards.Unity.asmdef` 引用 `Cards.Runtime` 来访问这些类型。**命名空间保持不变**（如 `Cards.Core`、`Cards.Zones`），确保现有代码只需要重新编译，无需改任何 `using` 语句。

### 0.5 验证清单（Phase 0 完成标准）

- Unity 编辑器无编译错误
- 运行游戏，行为与重构前完全一致
- `Cards.Runtime.asmdef` 的 `references` 列表为空（无 UnityEngine）
- 在 `Runtime/` 中的任何文件加一行 `using UnityEngine;` 并尝试使用会导致**编译失败**（证明隔离生效）
- `Assets/Cards/Tests/EditMode/` 下创建一个空的占位测试文件，确认 Test Runner 能识别

---

## Phase 1：平台抽象接口 + GameContext

**前置条件**: Phase 0 完成
**目标**: 用接口替换 `UnityEngine.Random`、`Debug.Log` 等平台调用；创建 GameContext 容器
**预计改动**: 新建 ~8 个文件，修改 ~6 个文件

### 1.1 在 Runtime/ 中创建服务接口

**新建文件**:

- `Runtime/Services/IRandom.cs`

```csharp
namespace Cards.Services
{
    public interface IRandom
    {
        int Range(int minInclusive, int maxExclusive);
    }
}
```

- `Runtime/Services/ILogger.cs`

```csharp
namespace Cards.Services
{
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
```

- `Runtime/Services/GameContext.cs`（Phase 1 版本，后续阶段扩展）

```csharp
namespace Cards.Services
{
    public class GameContext
    {
        public IRandom Random { get; }
        public ILogger Logger { get; }

        public GameContext(IRandom random, ILogger logger)
        {
            Random = random;
            Logger = logger;
        }
    }
}
```

### 1.2 在 Unity 层创建接口实现

**新建文件**:

- `Core/Services/UnityRandom.cs`（在 Cards.Unity 程序集中）

```csharp
using Cards.Services;
namespace Cards.Core.Services
{
    public class UnityRandom : IRandom
    {
        public int Range(int min, int max) => UnityEngine.Random.Range(min, max);
    }
}
```

- `Core/Services/UnityLogger.cs`

```csharp
using Cards.Services;
namespace Cards.Core.Services
{
    public class UnityLogger : ILogger
    {
        public void Log(string msg) => UnityEngine.Debug.Log(msg);
        public void LogWarning(string msg) => UnityEngine.Debug.LogWarning(msg);
        public void LogError(string msg) => UnityEngine.Debug.LogError(msg);
    }
}
```

### 1.3 重构逻辑层类，注入 IRandom / ILogger

**修改 [DiceRoller.cs](Assets/Cards/Core/DiceRoller.cs)** → 移入 `Runtime/Core/DiceRoller.cs`：

- 改为实例类，构造函数接收 `IRandom`
- `Roll()` 改为实例方法，内部调用 `_random.Range()` 代替 `UnityEngine.Random.Range()`

**修改 [CombatResolver.cs](Assets/Cards/Core/CombatResolver.cs)** → 移入 `Runtime/Core/CombatResolver.cs`：

- 改为实例类，构造函数接收 `IRandom` 和 `DiceRoller`
- `RollAttack()` / `RollDamage()` 改为实例方法
- 删除 `using UnityEngine`

**修改 [CardModel.cs](Assets/Cards/Core/CardModel.cs)** → 移入 `Runtime/Core/CardModel.cs`：

- 构造函数增加 `ILogger` 参数（可选，默认为 `NullLogger`）
- 所有 `Debug.Log` 替换为 `_logger.Log()`
- `CardData` 的引用改为纯数据接口 `ICardData`（见 1.4）

### 1.4 CardData 纯数据接口

**新建 `Runtime/Data/ICardData.cs`**:

```csharp
namespace Cards.Data
{
    public interface ICardData
    {
        string CardId { get; }
        string CardName { get; }
        int Cost { get; }
        int Health { get; }
        int ArmorClass { get; }
        int AttackBonus { get; }
        int Attack { get; }
        int DiceCount { get; }
        int DiceSides { get; }
        System.Collections.Generic.IReadOnlyList<CardTag> Tags { get; }
    }
}
```

**修改 [CardData.cs](Assets/Cards/Data/CardData.cs)**（保持在 Unity 层）:

- 实现 `ICardData` 接口
- 现有字段不变，添加接口属性的显式实现

**修改 CardModel**:

- 将 `public CardData Data` 改为 `public ICardData Data`

### 1.5 GameManager 初始化 GameContext

**修改 [GameManager.cs](Assets/Cards/Core/GameManager.cs)**:

- 在 `Awake()` 中创建 `GameContext`：

```csharp
var ctx = new GameContext(new UnityRandom(), new UnityLogger());
```

- 将 ctx 作为公共属性 `public GameContext Context { get; private set; }` 供 FSM States 等访问
- 向下传递给需要的组件（CombatResolver 的创建等）

### 1.6 编写第一批单元测试

**新建 `Tests/EditMode/DiceRollerTests.cs`**:

- 用一个固定序列的 `MockRandom` 测试 `DiceRoller.Roll()`
- 验证 2d6 的结果等于两次 Range 调用之和

**新建 `Tests/EditMode/CombatResolverTests.cs`**:

- 测试自然骰 20 → CriticalHit
- 测试自然骰 1 → CriticalMiss
- 测试 totalAttack >= AC → Hit
- 测试 totalAttack < AC → Miss
- 全部使用 `MockRandom`，结果 100% 可预测

**新建 `Tests/EditMode/Mocks/MockRandom.cs`**:

```csharp
public class MockRandom : IRandom
{
    private Queue<int> sequence;
    public MockRandom(params int[] values) { sequence = new Queue<int>(values); }
    public int Range(int min, int max) => sequence.Dequeue();
}
```

### 1.7 验证清单（Phase 1 完成标准）

- `Runtime/` 文件夹中仍然没有任何 `using UnityEngine`
- Unity Test Runner 中所有 EditMode 测试通过
- CombatResolver 的测试覆盖了 4 种攻击结果 
- 运行游戏，行为与重构前完全一致
- `DiceRoller` 和 `CombatResolver` 在 Runtime/ 中，不再是 static class

---

## Phase 2：Action 系统逻辑/动画分离

**前置条件**: Phase 1 完成
**目标**: 每个 GameAction 的逻辑可以脱离 Unity 协程独立执行；动画可一键禁用
**预计改动**: 新建 ~4 个文件，修改 ~6 个文件

### 2.1 定义动画策略接口

**新建 `Runtime/Services/IAnimationPolicy.cs`**:

```csharp
namespace Cards.Services
{
    public interface IAnimationPolicy
    {
        bool IsEnabled { get; }
        float TimeScale { get; }  // 1.0=正常, 0=跳过
    }
}
```

**新建 `Runtime/Services/IActionQueue.cs`**:

```csharp
namespace Cards.Services
{
    public interface IActionQueue
    {
        void Enqueue(GameAction action);
        bool IsProcessing { get; }
    }
}
```

### 2.2 重构 GameAction 基类

**修改 `Runtime/Actions/GameAction.cs`**（已在 Runtime/ 中）:

```csharp
public abstract class GameAction
{
    public bool IsCompleted { get; protected set; }

    // 纯逻辑：同步执行，修改游戏状态
    public abstract void Execute(GameContext ctx);

    // 动画表现：返回协程供 Unity 层播放（仅在动画启用时调用）
    public virtual IEnumerator AnimateRoutine() { yield break; }

    // 保留向后兼容：默认的 ExecuteRoutine 同时调用逻辑和动画
    public virtual IEnumerator ExecuteRoutine(GameContext ctx)
    {
        Execute(ctx);
        yield return AnimateRoutine();
        IsCompleted = true;
    }

    public virtual void Tick() { }
}
```

### 2.3 逐个重构现有 Action

**[DamageAction.cs](Assets/Cards/Actions/DamageAction.cs)** → 移入 `Runtime/Actions/`:

```csharp
public override void Execute(GameContext ctx)
{
    target.TakeDamage(damage);
    ctx.Logger.Log($"[Action] {sourceName} dealt {damage} to {target.CombatName}");
}

public override IEnumerator AnimateRoutine()
{
    yield return new WaitForSeconds(0.2f);  // 前摇
    yield return new WaitForSeconds(0.3f);  // 受击硬直
}
```

**[DrawCardAction.cs](Assets/Cards/Actions/DrawCardAction.cs)** → 移入 `Runtime/Actions/`:

- `Execute()`: 纯数据操作（从 drawPile 取牌、调用 RuleEngine、加入 handZone）
- `AnimateRoutine()`: `yield return new WaitForSeconds(0.35f)`
- 注意：当前直接访问 `ZoneRegistry`（static），暂时保留，Phase 4 去静态化时再改

**[ReshuffleAction.cs](Assets/Cards/Actions/ReshuffleAction.cs)** → 移入 `Runtime/Actions/`:

- `Execute()`: 取出弃牌堆所有牌 → ResetStats → 加入抽牌堆 → Shuffle
- `AnimateRoutine()`: 飞行动画循环 + 等待

### 2.4 修改 ActionManager 支持动画策略

**修改 [ActionManager.cs](Assets/Cards/Actions/ActionManager.cs)**（保留在 Unity 层）:

- 实现 `IActionQueue` 接口
- `ProcessQueue` 协程改为：

```csharp
private IEnumerator ProcessQueue()
{
    isExecuting = true;
    while (actionQueue.Count > 0)
    {
        GameAction action = actionQueue.Dequeue();
        
        if (animationPolicy.IsEnabled)
        {
            yield return StartCoroutine(action.ExecuteRoutine(ctx));
        }
        else
        {
            action.Execute(ctx);
            action.IsCompleted = true;  // 跳过动画直接完成
        }

        while (!action.IsCompleted) { action.Tick(); yield return null; }
    }
    isExecuting = false;
}
```

### 2.5 扩展 GameContext

**修改 `Runtime/Services/GameContext.cs`**:

- 增加 `IAnimationPolicy AnimationPolicy { get; }` 属性
- 增加 `IActionQueue Actions { get; }` 属性（Phase 2 先作为可选字段）

### 2.6 提供一键禁用入口

**新建 `Core/Services/AnimationPolicies.cs`**（Unity 层）:

```csharp
public class LiveAnimationPolicy : IAnimationPolicy
{
    public bool IsEnabled => true;
    public float TimeScale => 1.0f;
}

public class InstantAnimationPolicy : IAnimationPolicy
{
    public bool IsEnabled => false;
    public float TimeScale => 0f;
}
```

**修改 GameManager**:

- 添加 `[SerializeField] private bool disableAnimations = false;`
- 在 Inspector 中可勾选禁用动画
- 根据这个值选择 `LiveAnimationPolicy` 或 `InstantAnimationPolicy`

### 2.7 编写测试

**新建 `Tests/EditMode/ActionLogicTests.cs`**:

- 测试 `DamageAction.Execute()` 正确扣血
- 测试 `DrawCardAction.Execute()` 从牌堆顶部抽牌到手牌
- 测试 `ReshuffleAction.Execute()` 弃牌堆清空、牌堆重建
- 所有测试使用 `InstantAnimationPolicy`，无协程，纯同步

### 2.8 验证清单（Phase 2 完成标准）

- 在 GameManager Inspector 中勾选 `disableAnimations`，游戏逻辑正常运行但无动画等待
- 取消勾选后，动画行为恢复正常
- Action 的逻辑测试全部通过
- `DamageAction`、`DrawCardAction`、`ReshuffleAction` 已在 Runtime/ 中（纯 C#）
- `AnimateRoutine()` 中的 `WaitForSeconds` 不影响逻辑测试（因为测试时不调用它）

---

## Phase 3：CardEntity 数据/视图分离 + CardZone 纯逻辑化

**前置条件**: Phase 2 完成
**目标**: 卡牌的"逻辑身份"不再是 MonoBehaviour；CardZone 的数据操作可以脱离 Unity 测试
**预计改动**: 新建 ~3 个文件，修改 ~15 个文件（波及面最广的阶段）

### 3.1 创建 CardInstance（纯逻辑卡牌身份）

**新建 `Runtime/Core/CardInstance.cs`**:

```csharp
namespace Cards.Core
{
    public class CardInstance
    {
        public CardModel Model { get; }
        public ICardData Data => Model?.Data;
        public CardOwner Owner { get; }
        
        // 区域追踪（纯数据）
        public ZoneId? CurrentZoneId { get; set; }
        
        public CardInstance(ICardData data, CardOwner owner, ILogger logger = null)
        {
            Model = new CardModel(data, owner, logger);
            Owner = owner;
        }
    }
}
```

### 3.2 重构 CardZone 为纯逻辑

**修改 `Runtime/Zones/CardZone.cs`**（移入 Runtime/）:

- 泛型参数从 `CardEntity` 改为 `CardInstance`
- **移除** `Layout` 属性和所有 `Layout.Arrange()` 调用
- **增加**事件通知：

```csharp
public event Action<CardInstance, int> OnCardAdded;
public event Action<CardInstance> OnCardRemoved;
public event Action OnShuffled;

public void AddCard(CardInstance card)
{
    cards.Add(card);
    card.CurrentZoneId = this.ZoneId;
    OnCardAdded?.Invoke(card, cards.Count - 1);
}
```

- `Shuffle()` 使用 `IRandom` 代替 `UnityEngine.Random`（从 GameContext 获取）

### 3.3 创建 ZoneLayoutView（Unity 层视觉同步器）

**新建 `Zones/Layouts/ZoneLayoutView.cs`**（Unity 层）:

- 持有一个 `CardZone` 引用和一个 `IZoneLayout` 引用
- 监听 `CardZone.OnCardAdded` / `OnCardRemoved` / `OnShuffled` 事件
- 负责找到对应的 `CardEntityView` 并调用 `MoveTo()` 或重新排列
- 维护一个 `Dictionary<CardInstance, CardEntityView>` 映射

### 3.4 重构 CardEntity → CardEntityView

**修改 [CardEntity.cs](Assets/Cards/Core/CardEntity.cs)** → 重命名为 `CardEntityView`（保留在 Unity 层）:

- 持有 `CardInstance` 引用而非直接持有 `CardModel`
- `SetupCard()` 改为接收 `CardInstance`
- 视觉更新、`OnMouseDown`、`MoveTo` 协程保留
- 不再维护 `CurrentZone` / `CurrentZoneId`（由 `CardInstance` 管理）

### 3.5 重构 InteractionRequest

**修改 `Runtime/Rules/Interactions/InteractionRequest.cs`**（移入 Runtime/）:

- `SourceCard` 和 `TargetEntity` 类型从 `CardEntity` 改为 `CardInstance`
- `SourceZone` / `TargetZone` 已经是 `CardZone`（Phase 3.2 后为纯逻辑类型）

### 3.6 更新所有 Rule 和 Effect

以下文件中的 `CardEntity` 引用改为 `CardInstance`：

- [AttackRule.cs](Assets/Cards/Rules/Interactions/AttackRule.cs) → 移入 Runtime/
- [PlayEffectRule.cs](Assets/Cards/Rules/Interactions/PlayEffectRule.cs) → 移入 Runtime/
- [PlayEntityRule.cs](Assets/Cards/Rules/Interactions/PlayEntityRule.cs) → 移入 Runtime/
- [CardDispositionRule.cs](Assets/Cards/Rules/Interactions/CardDispositionRule.cs) → 移入 Runtime/
- [RuleEngine.cs](Assets/Cards/Rules/Interactions/RuleEngine.cs) → 移入 Runtime/
- [DamageEffect.cs](Assets/Cards/Effects/DamageEffect.cs) → 移入 Runtime/
- [AOEDamageEffect.cs](Assets/Cards/Effects/AOEDamageEffect.cs) → 移入 Runtime/
- [DrawCardEffect.cs](Assets/Cards/Effects/DrawCardEffect.cs) → 移入 Runtime/
- [ICardEffect.cs](Assets/Cards/Effects/ICardEffect.cs) → 移入 Runtime/

注意：DamageEffect 和 AOEDamageEffect 当前使用 `[Serializable]` + `[Header]` 等 Unity 属性。移入 Runtime 后需要移除这些 attribute，改为纯字段。序列化兼容性通过在 Unity 层保留一个 Wrapper 或使用 `[SerializeReference]` 的自定义 drawer 处理。

### 3.7 更新事件定义

**修改 `Runtime/Core/Events/EventBus.cs`** 中的事件类型（移入 Runtime/）:

- `CardClickedEvent`、`CardDiedEvent`、`RequestMoveCardEvent` 等中的 `CardEntity` 引用改为 `CardInstance`
- `RequestMoveCardEvent.UseAnimation` 移除（动画决策由 AnimationPolicy 统一控制）

### 3.8 更新 Handlers

[CardPlayHandler.cs](Assets/Cards/Rules/Handlers/CardPlayHandler.cs) 和 [CardDeathHandler.cs](Assets/Cards/Rules/Handlers/CardDeathHandler.cs) 保留在 Unity 层，但内部引用改为 `CardInstance`。

### 3.9 编写测试

**新建 `Tests/EditMode/CardZoneTests.cs`**:

- 测试 AddCard / RemoveCard / DrawTopCard / Shuffle / TakeAllCards
- 全部使用 `CardInstance`（纯逻辑），无需 GameObject
- 验证事件触发次数和顺序

**新建 `Tests/EditMode/RuleEngineTests.cs`**:

- 构造 InteractionRequest，手动挂载规则，验证 ProcessInteraction 的行为
- 测试 AttackRule 命中/未命中
- 测试 PlayEntityRule 将实体牌正确放入战场
- 测试 CardDispositionRule 将非实体牌送入弃牌堆

### 3.10 验证清单（Phase 3 完成标准）

- `CardZone` 在 Runtime/ 中，不依赖 `UnityEngine`
- `InteractionRequest`、所有 Rule、所有 Effect 在 Runtime/ 中
- CardZone 和 RuleEngine 的单元测试全部通过
- 游戏运行正常：`ZoneLayoutView` 正确响应 CardZone 事件并驱动视觉排列
- `CardEntityView` 仍能正确显示卡面、响应点击、播放移动动画

---

## Phase 4：去静态化——EventBus / ZoneRegistry / RuleEngine 实例化

**前置条件**: Phase 3 完成
**目标**: 所有静态服务变为实例，通过 GameContext 注入；可在纯 C# 环境中模拟完整游戏回合
**预计改动**: 新建 ~4 个接口文件，修改 ~12 个文件

### 4.1 创建服务接口

**新建 `Runtime/Services/IEventBus.cs`**:

```csharp
namespace Cards.Services
{
    public interface IEventBus
    {
        EventToken Subscribe<T>(Action<T> handler);
        void Unsubscribe(EventToken token);
        void Publish<T>(T gameEvent);
        void Clear();
    }
}
```

**新建 `Runtime/Services/IZoneRegistry.cs`**:

```csharp
namespace Cards.Services
{
    public interface IZoneRegistry
    {
        void Register(CardZone zone);
        CardZone Get(ZoneId id, int index = 0);
        IReadOnlyList<CardZone> GetAll(ZoneId id);
        void Clear();
    }
}
```

**新建 `Runtime/Services/IRuleEngine.cs`**:

```csharp
namespace Cards.Services
{
    public interface IRuleEngine
    {
        void ProcessInteraction(InteractionRequest request);
    }
}
```

### 4.2 将静态类改为实例类并实现接口

**修改 `Runtime/Core/Events/EventBus.cs`**:

- `public class EventBus : IEventBus`（去掉 `static`）
- `_subscribers` 变为实例字段

**修改 `Runtime/Zones/ZoneRegistry.cs`**:

- `public class ZoneRegistry : IZoneRegistry`（去掉 `static`）
- `zonesById` / `zonesByName` 变为实例字段

**修改 `Runtime/Rules/Interactions/RuleEngine.cs`**:

- `public class RuleEngine : IRuleEngine`（去掉 `static`）
- `ProcessInteraction` 变为实例方法
- 构造函数接收 `IZoneRegistry`（用于 DefaultMoveCard 中的 ZoneTransferService）

**修改 `Runtime/Zones/ZoneTransferService.cs`**（移入 Runtime/）:

- 去掉 `static`，变为实例类

### 4.3 完善 GameContext（最终版本）

```csharp
public class GameContext
{
    public IRandom Random { get; }
    public ILogger Logger { get; }
    public IAnimationPolicy AnimationPolicy { get; }
    public IActionQueue Actions { get; }
    public IEventBus Events { get; }
    public IZoneRegistry Zones { get; }
    public IRuleEngine Rules { get; }
    public CombatResolver Combat { get; }
    
    // 工厂方法
    public static GameContext CreateForTest(IRandom random = null, ...)
    {
        // 使用 Mock/默认实现创建完整的测试上下文
    }
}
```

### 4.4 更新所有消费者

需要修改的文件（将 `XxxClass.StaticMethod()` 改为 `ctx.Xxx.Method()`）：

- `Runtime/Actions/DrawCardAction.cs` — `ZoneRegistry.Get()` → `ctx.Zones.Get()`，`ActionManager.Instance.AddAction()` → `ctx.Actions.Enqueue()`，`RuleEngine.ProcessInteraction()` → `ctx.Rules.ProcessInteraction()`
- `Runtime/Actions/ReshuffleAction.cs` — 类似
- `Runtime/Rules/Interactions/AttackRule.cs` — `CombatResolver.Xxx()` → `ctx.Combat.Xxx()`，`ActionManager.Instance.AddAction()` → `ctx.Actions.Enqueue()`
- `Runtime/Rules/Interactions/PlayEffectRule.cs` — `ActionManager.Instance.AddAction()` → `ctx.Actions.Enqueue()`
- `Runtime/Rules/Interactions/CardDispositionRule.cs` — `EventBus.Publish()` → `ctx.Events.Publish()`
- `Runtime/Effects/DamageEffect.cs` — `CombatResolver` 调用
- `Runtime/Effects/AOEDamageEffect.cs` — `ZoneRegistry.Get()` → `ctx.Zones.Get()`
- `Runtime/Effects/DrawCardEffect.cs` — 无直接静态调用，但生成的 DrawCardAction 需要 ctx
- Unity 层：`CardPlayHandler.cs`、`CardDeathHandler.cs`、`GameManager.cs`、所有 FSM States

### 4.5 FSM States 去除 Unity 输入依赖

**修改 FSM States**（仍在 Unity 层）:

- 将 `Input.GetKeyDown()` 调用提取到 `IInputProvider` 接口
- 将 `Time.deltaTime` 提取到 `ITimeProvider` 接口
- 或更简单地：让 FSM States 保持在 Unity 层，但让它们通过 `GameContext` 获取服务

### 4.6 编写集成测试

**新建 `Tests/EditMode/GameFlowTests.cs`**:

```csharp
[Test]
public void FullTurn_DrawAndPlayCard_DamageApplied()
{
    // 1. 用 GameContext.CreateForTest() 创建纯测试环境
    // 2. 创建 2 个 CardZone（手牌、战场），注册到 ZoneRegistry
    // 3. 创建 CardInstance，放入手牌
    // 4. 构造 InteractionRequest，调用 RuleEngine
    // 5. 断言目标扣血、牌进入弃牌堆
    // 全程无 Unity 依赖，纯 C# 同步执行
}
```

### 4.7 清理静态残留

- EventBus 原来的 static 方法可暂时保留为 `[Obsolete]` 的转发方法，方便渐进迁移
- 在 Runtime/ 程序集中搜索所有剩余的 `static`，确认只剩工具方法（如 `CombatResolver.IsHit()`）

### 4.8 验证清单（Phase 4 完成标准）

- `Runtime/` 程序集中没有任何 `static class`（枚举扩展方法除外）
- `Runtime/` 程序集中没有任何 `Singleton` 或 `.Instance` 调用
- GameFlowTests 可在纯 EditMode 下通过，模拟完整的抽牌-出牌-伤害-死亡流程
- 游戏运行行为与重构前完全一致
- `GameContext.CreateForTest()` 可以在 3 行代码内创建一个完整的可测试环境

---

## 各阶段依赖关系图

```mermaid
flowchart TD
    P0["Phase 0: 程序集边界 + 纯类型提取"]
    P1["Phase 1: IRandom/ILogger + GameContext"]
    P2["Phase 2: Action 逻辑/动画分离"]
    P3["Phase 3: CardEntity 拆分 + CardZone 纯逻辑化"]
    P4["Phase 4: 去静态化 EventBus/ZoneRegistry/RuleEngine"]

    P0 --> P1
    P0 --> P2
    P1 --> P2
    P2 --> P3
    P3 --> P4
```



注意：**Phase 1 和 Phase 2 的"代码改动"不重叠**（Phase 1 改 DiceRoller/CombatResolver/CardModel，Phase 2 改 GameAction/ActionManager/各Action子类），可以由两个人**基于 Phase 0 的成果同时开始**，但 Phase 2 的 ActionManager 改造依赖 Phase 1 中的 GameContext 定义，因此需要先协商 GameContext 接口，或让 Phase 1 先完成接口定义部分。

## 单阶段交付模板

每个阶段交付时应包含：

1. **变更清单**：列出所有新建、移动、修改的文件
2. **编译验证**：Unity 编辑器零编译错误截图
3. **功能验证**：游戏运行录屏（10秒，展示抽牌-出牌-洗牌流程）
4. **测试报告**：Test Runner 截图，所有测试绿色
5. **隔离验证**（Phase 0 起）：在 Runtime/ 中添加 `using UnityEngine` 会编译失败的截图

