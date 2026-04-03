# Cards Runtime Phase 0 基线扫描

## 扫描目的

本文件记录 Phase 0 完成时 `Assets/Cards` 的 Unity 依赖与静态残留基线，用于后续阶段对照债务收敛情况。

扫描日期：

- 2026-04-03

扫描范围：

- `Assets/Cards` 下的 `.cs` 源文件

扫描模式：

- 关键字扫描 `UnityEngine`
- 关键字扫描 `MonoBehaviour`
- 关键字扫描 `ScriptableObject`
- 关键字扫描 `Debug.Log`
- 关键字扫描 `WaitForSeconds`
- 关键字扫描 `Transform`
- 关键字扫描 `Material`

说明：

- 本基线只统计源码，不把 `.asset`、`.prefab`、`.meta` 纳入统计。
- `Assets/Cards/Runtime` 中存在 1 处原始文本命中，来源于注释中的 `MonoBehaviour` 字样，不属于代码依赖。

## 关键结论

- `Assets/Cards/Runtime/Cards.Runtime.asmdef` 已保持 `noEngineReferences = true`。
- `Assets/Cards/Runtime` 当前共有 37 个 `.cs` 文件，代码级 Unity 禁止项命中为 0。
- Unity 依赖目前主要集中在 Authoring、Unity Host，以及尚未迁出的过渡目录。
- 结构性迁移债务最重的区域是 `FSM/States`、`Rules/Handlers`、`Effects`、`Actions`、`Core/GameManager.cs`。

## 分目录基线

| 目录 | `.cs` 文件数 | 命中禁止项的文件数 | 基线解读 | 后续阶段 |
| --- | --- | --- | --- | --- |
| `Assets/Cards/Runtime` | 37 | 0（代码级） | Runtime 边界当前可守住 | 持续保持 |
| `Assets/Cards/Data` | 1 | 1 | 预期 Authoring 依赖；`CardData` 仍兼做运行时接口桥接 | Phase 4 |
| `Assets/Cards/Decks` | 2 | 1 | 预期 Authoring 依赖 | Phase 4 |
| `Assets/Cards/Levels` | 3 | 3 | Authoring 与场景绑定混合 | Phase 4 / Phase 6 |
| `Assets/Cards/Editor` | 2 | 2 | 预期编辑器依赖 | 保持 |
| `Assets/Cards/Core` | 7 | 5 | Unity Host 依赖合理，但 `GameManager` 过载 | Phase 1 / Phase 6 |
| `Assets/Cards/Actions` | 3 | 3 | 逻辑执行与 Unity 协程表现耦合 | Phase 3 |
| `Assets/Cards/Zones/Layouts` | 4 | 2 | 纯展示层依赖，合理保留 | 保持 |
| `Assets/Cards/FSM/States` | 6 | 6 | 纯流程逻辑仍在 Unity 侧 | Phase 1 |
| `Assets/Cards/Rules/Handlers` | 2 | 2 | 输入解释与规则入口仍在 Unity 侧 | Phase 2 / Phase 5 |
| `Assets/Cards/Effects` | 3 | 3 | 效果解析仍依赖 Unity 静态调用 | Phase 5 |

## 原始关键字命中汇总

以下统计基于上述源码范围的原始文本扫描结果：

| 关键字 | 命中次数 |
| --- | --- |
| `using UnityEngine` | 27 |
| `Debug.Log` | 23 |
| `Transform` | 10 |
| `Debug.LogWarning` | 9 |
| `Debug.LogError` | 8 |
| `MonoBehaviour` | 5 |
| `WaitForSeconds` | 4 |
| `ScriptableObject` | 3 |
| `Material` | 1 |

## 当前迁移热点

- `Assets/Cards/Core/GameManager.cs`
- `Assets/Cards/FSM/States/*.cs`
- `Assets/Cards/Rules/Handlers/*.cs`
- `Assets/Cards/Effects/*.cs`
- `Assets/Cards/Actions/*.cs`
- `Assets/Cards/Data/CardData.cs`

## 对后续阶段的约束

- Phase 1 起新增 Runtime 逻辑不得回流到 `Assets/Cards/Core` 或 `Assets/Cards/FSM/States`。
- Phase 2 起新增输入入口不得继续加在 `Rules/Handlers` 中。
- Phase 3 起动作逻辑与协程表现必须分别落位。
- Phase 4 起新增 Runtime 数据结构不得继续直接依赖 `ScriptableObject` 资产。
