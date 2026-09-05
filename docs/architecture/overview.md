# 架构总览

## 目标

用可替换的积木拼出剪贴板管理器：业务与 UI 跨平台复用，OS 差异关在适配器里。

## 分层

```text
Inkboard.App                    Avalonia 视图 / 主题 / ViewModel / DI 组合根
        │
        ▼
Inkboard.Application            用例编排（捕获、搜索、粘贴、设置）
        │
        ├──────────────────────► Inkboard.Domain（模型与纯规则）
        │
        ▼
Inkboard.Infrastructure.Abstractions   端口接口
        ▲                ▲
        │                │
 Persistence        Platform.Windows / Platform.Linux
```

## 扩展方式

| 需求 | 落点 |
|---|---|
| 新业务能力 | Application 用例 + 可选 Domain 类型 |
| 新存储 | 实现 `IHistoryStore` |
| 新 OS 能力 | 对应 `Platform.*` 实现已有端口 |
| 新界面 | App 的 View / ViewModel，逻辑仍调 Application |

## 原则

- 依赖向内；Domain 不知道 Avalonia / Win32。
- 同一业务逻辑只写一次。
- 代码简洁，必要处写中文注释说明意图。
