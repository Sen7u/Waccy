# Inkboard

跨平台剪贴板历史管理器（Avalonia + .NET 8）。行为对齐 [Maccy](https://github.com/p0deje/Maccy)，面向 Windows / Linux。

当前可用：**文本历史 MVP** — 监听剪贴板、搜索、置顶、删除、墨青弹出层动效；Linux 用窗口内 `Ctrl+Shift+V` / 托盘唤起，Windows 注册全局热键。

## 快速开始

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。Linux 上剪贴板监听还需安装 `xsel`（`sudo apt install xsel`）。

```bash
make restore   # 还原依赖
make build     # 编译
make test      # 单测
make run       # 启动 UI
make check     # restore + build + test
make reference # 拉取 reference/Maccy 作只读对照
```

## 解决方案结构

| 项目 | 职责 |
|---|---|
| `Inkboard.Domain` | 实体与纯规则 |
| `Inkboard.Application` | 用例编排 |
| `Inkboard.Infrastructure.Abstractions` | 端口接口 |
| `Inkboard.Infrastructure.Persistence` | 存储（当前内存） |
| `Inkboard.Platform.Windows` / `.Linux` | OS 适配器 |
| `Inkboard.App` | Avalonia UI + DI 组合根 |
| `Inkboard.Tests` | 领域/应用层单测 |

依赖只指向内；业务代码不写 `if (Windows)`。详见 [AGENTS.md](AGENTS.md) 与 [docs/architecture/overview.md](docs/architecture/overview.md)。

## 开发约定

- 代码优雅简洁，中文注释写「为什么」；样式走 `Themes/` 令牌
- 先扩接口/用例，再填平台实现
- 提交前 `make check`
