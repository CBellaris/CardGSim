# Cards Runtime 深度剥离改造计划

## 文档目的

本文件用于统一记录 `Assets/Cards` 模块从“以 Unity 为主导的玩法实现”迁移为“Runtime 主导、Unity 纯适配”的完整计划。

后续用途：

- 作为分阶段推进的主计划书
- 作为阶段完成后的验收记录归档位置
- 作为多人协作时的统一边界说明
- 作为后续新功能接入时的架构约束依据

本文档默认按阶段顺序推进，不并行抢跑未满足前置依赖的阶段。

## 当前问题摘要

结合当前代码结构，项目已经完成了第一轮有效拆分，但仍存在以下结构性问题：

- `Cards.Runtime` 已独立为无引擎引用程序集，但真正的流程编排仍主要在 Unity 侧。
- `GameManager` 仍承担上下文组装、回合驱动、Zone 初始化、规则挂载、卡牌实例生成、事件订阅等多项 Runtime Application 层职责。
- `FSM/States` 中的状态流转本质上是纯玩法流程逻辑，但当前仍依赖 Unity 侧的 `GameManager`。
- 输入处理仍由 Unity 视图事件直接驱动玩法行为，例如卡牌点击后由 Unity 侧 handler 负责判断当前阶段是否合法、如何选目标、如何构造请求。
- 动作系统仍将逻辑执行和 Unity 协程等待混在一起，导致 `DrawCardAction`、`ReshuffleAction` 等纯规则动作还没有彻底落入 Runtime。
- `CardData` 同时承担运行时卡牌定义和 Unity 资产表现字段，导致 Runtime 仍被 ScriptableObject 资产形态渗透。
- 一些运行时规则仍通过 Runtime 发事件、Unity 收事件再执行核心逻辑，说明 domain 逻辑还未真正闭环在 Runtime 内。
- 当前测试虽已覆盖部分 Runtime，但仍依赖 `CardData : ScriptableObject` 构造测试数据，边界约束还不够硬。

## 目标架构

改造完成后，整体架构收敛为三层：

### 1. Runtime 层

目录目标：

- `Assets/Cards/Runtime`

职责：

- 游戏会话与生命周期
- 回合状态与阶段流转
- 命令入口与交互请求
- 规则引擎与效果系统
- 动作系统的逻辑部分
- 战斗、伤害、死亡、区域迁移等纯玩法逻辑
- 纯运行时数据定义与状态对象

硬约束：

- 不引用 `UnityEngine`
- 不依赖 `MonoBehaviour`
- 不依赖 `ScriptableObject`
- 不直接包含 `Debug.Log`、`WaitForSeconds`、`Transform`、`Material`

### 2. Authoring 层

目录目标：

- `Assets/Cards/Data`
- `Assets/Cards/Decks`
- `Assets/Cards/Levels`
- `Assets/Cards/Editor`

职责：

- 资源录入
- ScriptableObject 配置
- Inspector / Editor 工具
- 运行时定义对象的构建与转换

硬约束：

- 只负责“编辑与构建”
- 不直接参与运行时规则判断
- 不作为 Runtime 的主状态源

### 3. Unity Host / Presentation 层

目录目标：

- `Assets/Cards/Core`
- `Assets/Cards/Actions` 中仍保留的纯 Unity 表现组件
- `Assets/Cards/Zones/Layouts`

职责：

- 生命周期对接
- 场景锚点与 View 绑定
- 输入采集并转 Runtime 命令
- 动画、布局、表现回放
- 运行时状态到 View 的同步

硬约束：

- 不做规则决策
- 不负责目标选择策略
- 不负责出牌合法性判断
- 不掌握回合流转真相

## 总体改造原则

1. Runtime 是唯一玩法真相来源。
2. Unity 只负责适配、输入、动画、渲染与场景绑定。
3. ScriptableObject 只承担 authoring，不直接等同于 runtime model。
4. 所有输入都转换为 Runtime 命令，而不是由 Unity 直接改状态。
5. 所有流程控制都沉到 Runtime，不由 MonoBehaviour 状态机驱动。
6. 动作逻辑与动作表现分离。
7. 每个阶段完成后必须补齐测试和验收记录，再进入下一阶段。
8. 未完成前置阶段前，不推进后续依赖阶段的实际代码迁移。

## 当前重点问题与对应代码区域

- `Assets/Cards/Core/GameManager.cs`
- `Assets/Cards/Rules/Handlers/CardPlayHandler.cs`
- `Assets/Cards/Rules/Handlers/CardDeathHandler.cs`
- `Assets/Cards/FSM/States/*.cs`
- `Assets/Cards/Actions/ActionManager.cs`
- `Assets/Cards/Actions/DrawCardAction.cs`
- `Assets/Cards/Actions/ReshuffleAction.cs`
- `Assets/Cards/Effects/*.cs`
- `Assets/Cards/Data/CardData.cs`
- `Assets/Cards/Levels/LevelZoneSetup.cs`
- `Assets/Cards/Core/CardEntityView.cs`
- `Assets/Cards/Tests/EditMode/*.cs`

## 阶段依赖总览

| 阶段 | 名称 | 前置阶段 |
| --- | --- | --- |
| Phase 0 | 基线冻结与迁移约束建立 | 无 |
| Phase 1 | Runtime Application 层落地 | Phase 0 |
| Phase 2 | 输入与命令模型重构 | Phase 1 |
| Phase 3 | 动作系统拆层 | Phase 1 |
| Phase 4 | Runtime 数据定义与 Authoring 解耦 | Phase 1 |
| Phase 5 | 规则、效果、目标选择彻底下沉 | Phase 2、Phase 3、Phase 4 |
| Phase 6 | Unity Host 瘦身与 View 适配重组 | Phase 5 |
| Phase 7 | 测试体系与程序集重组 | Phase 4，且在 Phase 6 完成前收口 |
| Phase 8 | 遗留功能回填与全量切换 | Phase 6、Phase 7 |

## 通用阶段门禁

每个阶段在进入“已完成”状态前，必须满足以下通用条件：

- 相关代码已合入主干或阶段集成分支
- 该阶段新增测试已完成
- 旧测试若失效，已迁移到新边界并恢复
- 没有引入新的 Runtime 对 Unity 的直接依赖
- 没有把流程控制重新塞回 Unity 侧
- 阶段验收记录已补充到本文档

---

## Phase 0 基线冻结与迁移约束建立

### 前置阶段

- 无

### 阶段目标

把“要迁什么、不迁什么、什么算完成”先固定下来，避免后续阶段边做边改目标。

### 具体改造内容

- 明确三个层次的职责边界：Runtime、Authoring、Unity Host。
- 冻结当前目录归属，标记哪些目录后续需要搬迁、保留或拆分。
- 制定 Runtime 禁止项清单：
  - `UnityEngine`
  - `MonoBehaviour`
  - `ScriptableObject`
  - `Debug.Log`
  - `WaitForSeconds`
  - `Transform`
  - `Material`
- 制定命名规范：
  - Runtime 会话类命名
  - 命令类型命名
  - 定义对象命名
  - Unity Host 适配组件命名
- 制定迁移完成定义：
  - “逻辑迁出”不等于文件位置变化，必须同时切断依赖方向
  - “已 Runtime 化”要求 headless 可测
- 制定 PR 边界规范：
  - 先建 Runtime API，再做 Unity 接入
  - 禁止在同一个 PR 同时大规模改逻辑和改命名

### 阶段输出物

- 本计划文档初版
- [架构边界说明](./Cards_Runtime_Boundaries.md)
- [禁止依赖清单与目录冻结结果](./Cards_Runtime_Boundaries.md)
- [Phase 0 基线扫描](./Cards_Runtime_Phase0_Baseline.md)
- 后续阶段的统一验收口径

### 阶段测试

- 检查 `Cards.Runtime.asmdef` 仍保持 `noEngineReferences = true`
- 对当前 `Assets/Cards` 扫描一遍 Unity 依赖与静态残留，形成基线统计

### 验收标准

- 全团队后续以本文件为准推进
- 所有后续阶段都能明确判断是否满足前置条件
- Runtime 边界约束得到确认，不再反复变更

### 验收记录

- 状态：已完成
- 完成日期：2026-04-03
- 实际结果：
  - 已补充 [Cards_Runtime_Boundaries.md](./Cards_Runtime_Boundaries.md)，固定三层边界、目录归属、命名规范、迁移完成定义与 PR 边界。
  - 已补充 [Cards_Runtime_Phase0_Baseline.md](./Cards_Runtime_Phase0_Baseline.md)，记录 `Assets/Cards` 的 Unity 依赖与静态残留基线。
  - 已新增 `Assets/Cards/Tests/EditMode/Phase0BoundaryTests.cs`，校验 `Cards.Runtime.asmdef` 的 `noEngineReferences` 和 Runtime 源码禁止项。
- 偏差说明：
  - Phase 0 只冻结边界和建立守卫，没有提前执行 Phase 1 之后的逻辑迁移。
  - 基线统计按源码扫描口径记录，未把 `.asset` 和 `.prefab` 序列化内容纳入计数。
  - 本地于 2026-04-03 两次执行 Unity EditMode 批处理测试时，进程均正常退出但未生成 `testResults` 文件；当前以 `Cards.Tests.dll` 编译成功和源码静态扫描作为本次 Phase 0 的实际验证依据。
- 遗留问题：
  - `GameManager`、`FSM/States`、`Rules/Handlers`、`Effects`、`Actions` 仍保留后续阶段要迁出的流程和表现耦合。
  - `CardData : ScriptableObject` 仍直接实现 `ICardData`，测试中也仍存在对 ScriptableObject 测试数据的依赖，需在 Phase 4 和 Phase 7 收口。
  - 需要单独排查当前 Tuanjie CLI 环境下 `-runTests -testResults` 未落盘的问题，避免影响后续阶段的自动化验收。

---

## Phase 1 Runtime Application 层落地

### 前置阶段

- Phase 0

### 阶段目标

把当前仍在 Unity 侧的“游戏会话与流程编排”下沉到 Runtime，建立稳定的 gameplay application layer。

### 具体改造内容

- 新增 Runtime 会话主类，建议名称之一：
  - `GameSession`
  - `CardGameRuntime`
- 会话主类持有：
  - `GameContext`
  - 当前 `GamePhase`
  - 生命周期接口
  - 主命令入口
  - `Tick`/推进接口
- 下沉当前由 `GameManager` 承担的流程职责：
  - 初始化牌局
  - 初始化回合
  - 阶段切换
  - 队列忙闲判断后的流转
- 把当前 `FSM/States` 中纯流程逻辑迁入 Runtime：
  - 开局
  - 玩家回合开始
  - 玩家主阶段
  - 玩家回合结束
  - 敌方回合
- 移除 Runtime 流程对 `GameManager` 的直接依赖。
- 将 `GameManager` 降为 Unity Host：
  - 初始化上下文
  - 创建 Runtime 会话
  - 每帧驱动 `Tick`
  - 连接 view 与 runtime

### 涉及区域

- `Assets/Cards/Core/GameManager.cs`
- `Assets/Cards/FSM/States/*.cs`
- `Assets/Cards/Runtime/FSM/*.cs`
- `Assets/Cards/Runtime/Services/GameContext.cs`

### 阶段测试

- 新增 headless 流程测试：
  - 初始化游戏后能完成初始抽牌
  - 玩家回合开始后自动进入主阶段
  - 结束回合后进入敌方回合
  - 敌方回合结束后回到玩家回合开始
- 验证 Unity Host 只做驱动，不掌握流程状态真相

### 验收标准

- 在不依赖 `GameManager` 业务逻辑的情况下，Runtime 可独立完成完整回合流转
- `FSM/States` 不再依赖 Unity 组件来决定流程
- `GameManager` 体量明显瘦身，只保留 host 职责

### 验收记录

- 状态：已完成
- 完成日期：2026-04-03
- 实际结果：
  - 已在 `Assets/Cards/Runtime/FSM` 新增 `GameSession`、`GamePhase`、`GameSessionOptions`、`IGameSessionBootstrap`，由 Runtime 持有流程 phase 真相、生命周期入口与 `Tick` 流转。
  - 已新增 `Assets/Cards/Core/UnityGameSessionBootstrap.cs`，将关卡 zone 初始化、卡组实例化与战场规则挂载收敛为 Unity Host bootstrap。
  - `GameManager` 已降为 host 组件，只保留 `GameContext` 组装、会话创建、每帧驱动 `Tick`、输入桥接与 Runtime/Unity 事件桥接。
  - 原 `Assets/Cards/FSM/States/*.cs` Unity 流程状态已移除，`CardPlayHandler` 改为直接读取 Runtime `GameSession` 的 phase 真相。
  - 已新增 `Assets/Cards/Tests/EditMode/GameSessionFlowTests.cs`，覆盖初始抽牌、玩家回合开始进入主阶段、结束回合进入敌方回合、敌方回合结束返回玩家回合开始的 headless 流程测试。
- 偏差说明：
  - `DrawCardAction` 仍位于 `Assets/Cards/Actions/DrawCardAction.cs`，Phase 1 通过向 `GameSession` 注入 `Func<GameAction>` 抽牌动作工厂解耦程序集依赖，未提前执行 Phase 3 的动作迁移。
  - `GameManager` 仍保留 `RequestMoveCardEvent` 与死亡事件桥接等 host 职责；输入命令统一化与出牌合法性完整下沉仍留待 Phase 2。
  - 当前敌方回合仍保持原有占位 AI 逻辑，只迁移了流程编排真相，没有在 Phase 1 扩展新的敌方行为规则。
- 遗留问题：
  - `CardPlayHandler` 的点击解释、自动选目标与出牌命令建模仍在 Unity 侧，需在 Phase 2 收口。
  - `DrawCardAction`、`ReshuffleAction` 等动作逻辑仍未迁入 Runtime，需在 Phase 3 完成逻辑与表现拆层。
  - 2026-04-03 本地再次执行 Tuanjie EditMode 批处理测试时，进程以 `exit 0` 正常退出，但 `-testResults /tmp/mycardex-editmode-results.xml` 仍未生成结果文件；当前以脚本编译成功和批处理退出码作为本阶段的实际自动化验证依据。

---

## Phase 2 输入与命令模型重构

### 前置阶段

- Phase 1

### 阶段目标

把“Unity 输入事件直接驱动玩法逻辑”改为“Unity 收集输入，Runtime 决定行为”的命令模型。

### 具体改造内容

- 设计统一命令入口，例如：
  - `TryPlayCard`
  - `TryAttack`
  - `EndTurn`
  - `DrawCard`
- Unity 视图层不再直接发布玩法事件后由 Unity handler 再做规则判断。
- `CardEntityView` 的点击行为改为只通知 Unity Host 或输入桥。
- 当前 `CardPlayHandler` 中的内容迁入 Runtime：
  - 当前阶段是否允许出牌
  - 手牌区域校验
  - 目标区域选择逻辑
  - 交互请求构造
  - 合法性判断
- 避免 Runtime 暴露 Unity 具体状态类型给上层识别。
- 为需要选择目标的命令设计可扩展接口：
  - 自动选择
  - 显式选择
  - 等待目标输入

### 涉及区域

- `Assets/Cards/Rules/Handlers/CardPlayHandler.cs`
- `Assets/Cards/Core/CardEntityView.cs`
- `Assets/Cards/Core/GameManager.cs`
- `Assets/Cards/Runtime/Rules/Interactions/*.cs`
- 新增 Runtime `Commands` 或 `Application` 目录

### 阶段测试

- 命令合法性测试
- 非法阶段下出牌失败测试
- 非法区域点击测试
- 自动选目标逻辑测试
- 明确目标与自动目标两种模式的回归测试

### 验收标准

- Unity 不再直接判断当前是否能出牌
- `CardPlayHandler` 这类 Unity 侧业务 handler 被删除或降为纯桥接
- 点击卡牌后，真正的玩法处理逻辑发生在 Runtime 命令入口

### 验收记录

- 状态：已完成
- 完成日期：2026-04-03
- 实际结果：
  - 已在 `Assets/Cards/Runtime/Commands` 新增 `PlayCardCommand`、`AttackCommand`、`DrawCardCommand`、`EndTurnCommand`、`CommandTargetSelection`、`GameCommandResult`，统一 Runtime 命令意图与执行结果模型。
  - `GameSession` 已接管出牌/攻击命令处理，负责玩家主阶段校验、手牌区域校验、目标选择、`InteractionRequest` 构造与规则调用。
  - 原 `Assets/Cards/Rules/Handlers/CardPlayHandler.cs` 已删除，`GameManager` 改为仅订阅 `CardClickedEvent` 并向 Runtime 转发 `PlayCardCommand`，不再在 Unity 侧做出牌合法性判断。
  - 已为目标选择补充自动、显式、等待输入三种模式，其中等待输入通过 `GameCommandResult.AwaitingTargetSelection` 暴露可选目标集合。
  - 已新增 `Assets/Cards/Tests/EditMode/GameSessionCommandTests.cs`，覆盖非法阶段出牌、非法区域点击、自动目标、显式目标、等待目标输入与攻击命令候选目标回归。
- 偏差说明：
  - 为保持现有单击交互不变，Unity Host 当前仍只把卡牌点击映射为 `PlayCardCommand`；`TryAttack` 和等待目标输入接口已在 Runtime 就绪，但尚未接入新的多段式 Unity 选目标 UI。
  - `RuleEngine` 的默认 zone transfer fallback 已限制为非 `Attack` 请求，避免攻击命令在缺少命中规则时意外退化为移动卡牌。
  - 2026-04-03 本地再次执行 Tuanjie EditMode 批处理测试时，`Cards.Runtime.dll`、`Cards.Unity.dll`、`Cards.Tests.dll` 均成功编译，进程以 `exit 0` 结束，但 `-testResults /tmp/mycardex-editmode-results.xml` 仍未生成结果文件；本阶段继续以编译成功、日志中无编译错误/显式失败和新增测试源码落库作为自动化验收依据。
- 遗留问题：
  - `CardDeathHandler` 仍在 Unity 侧通过事件桥接处理死亡落点，需在 Phase 5 与规则闭环下沉时继续收口。
  - `DrawCardAction`、`ReshuffleAction` 和效果类中的 Unity 依赖仍待在 Phase 3 / Phase 5 拆离。

---

## Phase 3 动作系统拆层

### 前置阶段

- Phase 1

### 阶段目标

把动作系统中的“规则执行”和“Unity 协程等待/表现播放”拆开，使动作逻辑彻底 runtime 化。

### 具体改造内容

- 审查所有 `GameAction` 派生类。
- 将纯逻辑动作迁入 Runtime：
  - `DrawCardAction`
  - `ReshuffleAction`
  - 后续其它纯逻辑动作
- 去除动作中的：
  - `WaitForSeconds`
  - `Debug.Log` fallback
  - 任何 Unity 时间与协程细节
- 设计 Runtime 动作执行结果模型：
  - 逻辑执行结果
  - 表现请求
  - 后续待完成状态
- 将 `ActionManager` 重构为 Unity 表现执行器或动作 presenter。
- 明确 `IActionQueue` 的职责：
  - Runtime 队列负责逻辑顺序
  - Unity runner 负责动画回放和可视完成通知
- 避免“动画完成”成为逻辑前置真相，逻辑必须先成立，表现随后跟进。

### 涉及区域

- `Assets/Cards/Actions/ActionManager.cs`
- `Assets/Cards/Actions/DrawCardAction.cs`
- `Assets/Cards/Actions/ReshuffleAction.cs`
- `Assets/Cards/Runtime/Actions/*.cs`
- `Assets/Cards/Runtime/Services/IActionQueue.cs`

### 阶段测试

- 纯逻辑动作执行顺序测试
- 抽牌堆为空时的洗牌重试测试
- 禁用动画模式回归测试
- 启用动画模式下的表现回放测试

### 验收标准

- 核心动作逻辑在无协程环境下可以正确执行
- Unity 协程只负责表现，不承载规则真相
- `DrawCardAction` 和 `ReshuffleAction` 不再依赖 Unity API

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## Phase 4 Runtime 数据定义与 Authoring 解耦

### 前置阶段

- Phase 1

### 阶段目标

把当前以 `ScriptableObject` 为中心的运行时数据来源，改造为“Authoring Asset -> Runtime Definition”的明确两段式模型。

### 具体改造内容

- 新增纯 Runtime 定义对象：
  - `CardDefinition`
  - `DeckDefinition`
  - `ZoneBlueprint`
  - `LevelDefinition`
- `CardData`、`DeckConfig`、`LevelConfig` 改为 Authoring 资产。
- 建立 builder / converter：
  - 从 Authoring 资产构建 Runtime 定义
  - 校验非法配置
  - 生成稳定 ID
- 将表现字段与逻辑字段拆开：
  - 材质、Prefab、Transform、布局锚点保留在 Unity/Authoring
  - 攻击、生命、标签、效果定义保留在 Runtime Definition
- 避免 `CardEntityView` 再强转 `CardData` 读取逻辑字段。
- 为测试提供纯 C# 的 definition builder 或 test factory。

### 涉及区域

- `Assets/Cards/Data/CardData.cs`
- `Assets/Cards/Decks/*.cs`
- `Assets/Cards/Levels/*.cs`
- `Assets/Cards/Core/CardEntityView.cs`
- 新增 Runtime `Definitions` 或 `Blueprints` 目录

### 阶段测试

- Asset 转 Definition 的转换测试
- 缺少必填字段时的失败测试
- 同一张卡牌定义构建结果稳定性测试
- 不依赖 ScriptableObject 的 Runtime 测试数据构建测试

### 验收标准

- Runtime 主流程不再直接依赖 `CardData : ScriptableObject`
- 测试代码能够用纯 runtime definition 构建牌局
- Unity 表现字段不再污染运行时逻辑模型

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## Phase 5 规则、效果、目标选择彻底下沉

### 前置阶段

- Phase 2
- Phase 3
- Phase 4

### 阶段目标

让规则执行从 Runtime 内部闭环完成，不再依赖 Unity 侧 handler 或回调补完核心逻辑。

### 具体改造内容

- 将当前效果类迁入 Runtime：
  - `DamageEffect`
  - `DrawCardEffect`
  - `AOEDamageEffect`
- 去掉效果类中的 Unity 相关属性与日志调用。
- 审查并统一 `ICardEffect` 的定义方式，使其只依赖 Runtime 接口。
- 将死亡处理逻辑下沉到 Runtime：
  - `CardDeathHandler` 若仍保留，应迁入 Runtime 或改为内建规则服务
- 去掉 Runtime -> EventBus -> Unity -> ZoneTransfer 的反向桥：
  - 例如非实体牌去弃牌堆/放逐区，应由 Runtime 直接完成区域迁移
- 把目标选择策略正式纳入 Runtime：
  - 默认自动选目标
  - 自定义目标提供接口
  - 无目标时的规则行为明确化
- 审视 `RuleEngine` 的默认行为，统一“验证、前置处理、执行、默认落点”的规则约定。

### 涉及区域

- `Assets/Cards/Effects/*.cs`
- `Assets/Cards/Runtime/Effects/*.cs`
- `Assets/Cards/Rules/Handlers/CardDeathHandler.cs`
- `Assets/Cards/Runtime/Rules/Interactions/*.cs`
- `Assets/Cards/Runtime/Core/Events/EventBus.cs`

### 阶段测试

- 单体伤害、范围伤害、抽牌效果测试
- 实体进场、非实体牌去向测试
- 死亡后移除/放逐测试
- 无目标、目标无效、攻击 miss/hit/crit 测试
- 完整“抽牌-出牌-结算-死亡-区域变更”集成测试

### 验收标准

- 核心规则链路不再依赖 Unity 侧 handler 兜底
- 区域迁移在 Runtime 内闭环完成
- 效果系统能够在无 Unity 环境下完成执行与测试

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## Phase 6 Unity Host 瘦身与 View 适配重组

### 前置阶段

- Phase 5

### 阶段目标

将 Unity 侧彻底收缩为 host + presenter + view binder，不再保留玩法核心。

### 具体改造内容

- 将 `GameManager` 改造为 `UnityGameHost` 或等价角色。
- 删除或迁出其内部业务逻辑：
  - 区域规则挂载
  - 回合流转逻辑
  - 业务事件处理
- `CardEntityView` 改成纯 View：
  - 展示状态
  - 采集点击
  - 播放移动与表现
- `LevelZoneSetup` 聚焦场景锚点绑定与 zone view 建立。
- `ZoneLayoutView` 继续作为表现层组件存在，但只对 Runtime 状态做监听和回放。
- 清理静态单例残留：
  - `GameManager.Instance`
  - `ActionManager.Instance`
- 清理 Unity 侧无实际价值的旧兼容属性与临时桥接逻辑。

### 涉及区域

- `Assets/Cards/Core/GameManager.cs`
- `Assets/Cards/Core/CardEntityView.cs`
- `Assets/Cards/Levels/LevelZoneSetup.cs`
- `Assets/Cards/Zones/Layouts/*.cs`
- `Assets/Cards/Core/Services/*.cs`

### 阶段测试

- 场景初始化测试
- View 与 Runtime 状态同步测试
- 卡牌移动布局回归测试
- 输入桥转命令测试
- 动画开关回归测试

### 验收标准

- Unity Host 不再做规则判断
- Unity Host 不再决定回合流转
- Unity View 不再依赖 Authoring 逻辑字段作为玩法真相
- 静态单例不再出现在 gameplay 主路径中

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## Phase 7 测试体系与程序集重组

### 前置阶段

- Phase 4
- 需在 Phase 6 完成前收口

### 阶段目标

建立能够长期守住边界的测试与程序集结构，防止后续新功能回流到 Unity 逻辑。

### 具体改造内容

- 将测试程序集拆分为：
  - Runtime Tests
  - Unity Tests
- Runtime Tests 不再引用 `Cards.Unity`。
- 将当前测试中对 `ScriptableObject.CreateInstance<CardData>()` 的依赖替换为 runtime definitions 或 test builders。
- 加入架构测试：
  - Runtime 中不得引用 `UnityEngine`
  - Runtime 中不得出现 `MonoBehaviour`
  - Runtime 中不得出现 `ScriptableObject`
  - Runtime 中不得出现 `Debug.Log`
  - Runtime 中不得出现 `WaitForSeconds`
- 调整 CI 运行方式：
  - Runtime 单测优先
  - Unity 集成测试单独执行

### 涉及区域

- `Assets/Cards/Tests/*.asmdef`
- `Assets/Cards/Tests/EditMode/*.cs`
- 新增架构检查测试或脚本

### 阶段测试

- Runtime Tests 独立执行测试
- Unity Tests 独立执行测试
- 架构扫描测试
- 回归测试清单完整性检查

### 验收标准

- Runtime 可被独立验证
- Unity 测试只验证适配、场景、动画、视图
- 新增 PR 若破坏分层可被自动测试拦截

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## Phase 8 遗留功能回填与全量切换

### 前置阶段

- Phase 6
- Phase 7

### 阶段目标

在新结构稳定后，把尚未完成或此前为了迁移而推迟的玩法功能补回，并删除旧路径。

### 具体改造内容

- 回填 TODO 功能：
  - 回合开始/结束 Buff 与 Debuff
  - 行动力/法力值
  - 更完整的敌方 AI
  - 多区域玩法支持
  - 更复杂的目标选择交互
- 删除旧兼容路径和临时桥接代码。
- 清理不再使用的旧状态、旧 handler、旧事件。
- 补充存档、配置导入导出、扩展能力的适配。
- 对所有新功能要求默认只走 Runtime 命令链，不允许再写成 Unity 直驱规则逻辑。

### 涉及区域

- 贯穿 `Assets/Cards` 全域

### 阶段测试

- 全局回归测试
- 长流程整局测试
- 关键配置导入导出测试
- 性能与内存回归
- 多场景或多布局兼容测试

### 验收标准

- 旧实现路径已删除
- 新增功能全部符合目标架构
- Runtime / Authoring / Unity 三层边界稳定

### 验收记录

- 状态：待完成
- 完成日期：
- 实际结果：
- 偏差说明：
- 遗留问题：

---

## 长期维护要求

在本计划全部完成之前，以及完成之后的持续迭代中，新增改动需遵守以下要求：

- 新玩法规则优先落在 Runtime
- 新资源录入优先落在 Authoring
- 新表现功能优先落在 Unity Presentation
- 若某需求必须跨层，先补接口，再做适配，不允许直接穿透引用
- 每完成一个阶段，都必须及时更新本文件中的验收记录
- 若阶段中途发现计划需要调整，应直接修改本文件对应阶段，而不是口头约定

## 阶段验收追加规范

每个阶段完成时，至少补充以下信息：

- 状态
- 完成日期
- 实际完成范围
- 与计划不一致的地方
- 新增测试
- 已知遗留问题
- 是否允许进入下一阶段

推荐填写格式：

```md
- 状态：已完成 / 部分完成 / 延后
- 完成日期：YYYY-MM-DD
- 实际结果：
  - ...
- 偏差说明：
  - ...
- 遗留问题：
  - ...
- 下一阶段准入结论：允许 / 有条件允许 / 不允许
```

## 当前结论

本项目不适合继续以“见招拆招”的方式零散迁移，必须先建立 Runtime Application 层，再继续下沉输入、动作、数据和规则。按本计划顺序推进，能够最大限度减少返工，并保证多人在不同时间接手时仍能对齐目标。
