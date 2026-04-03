# Cards Runtime Phase 0 边界冻结

## 文档目的

本文件用于把 Phase 0 已确认的边界约束固定到仓库中，作为后续 Phase 1 到 Phase 8 的实现依据。

生效范围：

- `Assets/Cards` 全模块
- `Cards.Runtime`
- `Cards.Unity`
- `Cards.Tests`

生效日期：

- 2026-04-03

## 三层职责边界

| 层次 | 目录归属 | 允许承担的职责 | 明确禁止承担的职责 |
| --- | --- | --- | --- |
| Runtime | `Assets/Cards/Runtime` | 游戏会话、阶段流转、命令入口、规则引擎、效果解析、战斗结算、区域迁移、运行时状态 | `UnityEngine`、`MonoBehaviour`、`ScriptableObject`、`Debug.Log`、`WaitForSeconds`、场景对象引用、材质与渲染细节 |
| Authoring | `Assets/Cards/Data`、`Assets/Cards/Decks`、`Assets/Cards/Levels`、`Assets/Cards/Editor` | ScriptableObject 配置、Inspector 工具、定义构建、编辑期校验 | 运行时规则判断、回合流转真相、直接作为 Runtime 状态源 |
| Unity Host / Presentation | `Assets/Cards/Core`、`Assets/Cards/Actions` 中保留的表现部分、`Assets/Cards/Zones/Layouts` | 生命周期对接、场景锚点、输入采集、动画回放、视图同步、Unity 服务适配 | 出牌合法性判断、目标选择真相、回合推进真相、核心规则决策 |

## 目录归属冻结

| 当前目录 | Phase 0 归属 | 后续动作 | 说明 |
| --- | --- | --- | --- |
| `Assets/Cards/Runtime` | Runtime | 保留并持续扩张 | Runtime 是唯一玩法真相来源 |
| `Assets/Cards/Data` | Authoring | 保留，Phase 4 补定义构建 | `CardData` 暂时仍兼做运行时接口桥接 |
| `Assets/Cards/Decks` | Authoring | 保留，Phase 4 解耦 | 继续只承载卡组资源配置 |
| `Assets/Cards/Levels` | Authoring + Host 边界带 | 保留，后续拆开构建与场景绑定 | `LevelZoneSetup` 仍带场景耦合 |
| `Assets/Cards/Editor` | Authoring | 保留 | 仅限编辑器工具 |
| `Assets/Cards/Core` | Unity Host | 保留，Phase 1/6 瘦身 | `GameManager` 属于阶段性过载点 |
| `Assets/Cards/Actions` | 过渡区 | Phase 3 拆成 Runtime 逻辑 + Unity 表现 | 当前逻辑执行与协程表现仍混合 |
| `Assets/Cards/Zones/Layouts` | Unity Host | 保留 | 纯布局和场景表现 |
| `Assets/Cards/FSM/States` | 过渡区 | Phase 1 下沉到 Runtime | 当前属于纯流程逻辑，但仍依赖 Unity 侧入口 |
| `Assets/Cards/Rules/Handlers` | 过渡区 | Phase 2/5 下沉到 Runtime | 当前仍由 Unity 侧处理输入解释与规则判断 |
| `Assets/Cards/Effects` | 过渡区 | Phase 5 下沉到 Runtime | 纯效果逻辑应回归 Runtime 闭环 |

## Runtime 禁止项

`Assets/Cards/Runtime` 内禁止出现以下依赖或等价做法：

- `UnityEngine`
- `MonoBehaviour`
- `ScriptableObject`
- `Debug.Log`
- `WaitForSeconds`
- `Transform`
- `Material`

补充约束：

- Runtime 不允许持有场景对象引用。
- Runtime 不允许通过 Unity 静态服务完成随机数、时间、日志、输入。
- Runtime 允许定义接口，但具体 Unity 适配实现必须留在 Runtime 外部。

## 命名规范

### Runtime 会话类

- 运行时主入口统一使用 `*Session` 命名。
- 单局玩法编排根对象推荐命名为 `GameSession`。
- 如存在更高层封装，`*Session` 仍应表示 Runtime 真相入口，而不是 Unity 包装器。

### 命令类型

- 所有由 Unity Host 发往 Runtime 的用户意图统一使用 `*Command` 命名。
- 命令名称描述“意图”，而不是 Unity 交互动作。
- 示例：`PlayCardCommand`、`EndTurnCommand`、`SelectTargetCommand`。

### 定义对象

- 纯运行时定义对象统一使用 `*Definition` 命名。
- Authoring 资产到 Runtime 定义的转换器统一使用 `*Builder` 或 `*Factory`。
- 示例：`CardDefinition`、`DeckDefinition`、`CardDefinitionBuilder`。

### Unity Host 适配组件

- 持有 MonoBehaviour 生命周期、负责桥接 Runtime 的组件统一使用 `*Host`。
- 负责视图同步或表现编排的组件优先使用 `*Presenter`、`*View`、`*ViewAdapter`。
- Unity 专属服务适配统一使用 `Unity*` 前缀。

## 迁移完成定义

以下条件必须同时满足，才算“已 Runtime 化”：

- 逻辑主体已迁出 Unity Host，而不是仅仅移动文件位置。
- 依赖方向已经切断，Runtime 不再依赖 Unity 类型。
- 对应功能可以在 headless 环境下完成单元测试或编辑器测试。
- Unity 侧只负责发命令、接结果、播表现，不再掌握规则真相。
- 新增边界不会把流程控制重新塞回 `MonoBehaviour`。

以下情况不算完成：

- 文件移到 `Assets/Cards/Runtime`，但仍依赖 `UnityEngine`。
- 规则仍靠 Runtime 发事件，Unity 回调后再执行核心判断。
- 测试仍必须依赖场景对象或协程等待才能验证结果。

## PR 边界规范

- 先建 Runtime API，再接 Unity Host。
- 一个 PR 内禁止同时进行大规模逻辑重写和系统性命名迁移。
- 涉及多个阶段的工作，优先按依赖方向拆成多个小 PR。
- 新建 Unity 适配代码时，不得反向要求 Runtime 感知 Unity 具体实现。
- 如需引入过渡层，必须在 PR 描述中写明所属阶段与拆除时机。

## Phase 0 结论

- Runtime、Authoring、Unity Host 三层边界已冻结。
- `Assets/Cards` 的目录归属与后续迁移方向已固定。
- 后续阶段的命名、验收和 PR 边界以本文件为准，除非重构计划书显式更新。
