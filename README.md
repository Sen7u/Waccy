# Inkboard

跨平台剪贴板历史管理器（Avalonia + .NET 8）。行为对齐 [Maccy](https://github.com/p0deje/Maccy)，面向 Windows / Linux。

当前可用：**文本 + 图片历史 MVP** — 监听剪贴板、模糊/子串搜索、置顶、删除、墨青弹出层；历史 **SQLite** 持久化（`%LocalAppData%/Inkboard/history.db`）；托盘可暂停捕获 / 切换「选中后默认粘贴」；设置窗可改历史上限、热键、忽略应用（JSON：`settings.json`）。Windows 支持全局热键、来源进程名与图片捕获；Linux 用窗口内快捷键 / 托盘唤起（粘贴注入仍为占位）。

## 快速开始

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。Linux 上剪贴板监听还需安装 `xsel`（`sudo apt install xsel`）。

```bash
make restore      # 还原依赖
make build        # 编译
make test         # 单测
make run          # 启动 UI
make check        # restore + build + test
make publish-win  # 交叉编译 Windows x64 自包含 zip → dist/
make reference    # 拉取 reference/Maccy 作只读对照
```

Windows 包：解压后运行 `Inkboard.App.exe`（自包含，无需另装 .NET）。默认热键 `Ctrl+Shift+V`。

## 解决方案结构

| 项目 | 职责 |
|---|---|
| `Inkboard.Domain` | 实体与纯规则 |
| `Inkboard.Application` | 用例编排 |
| `Inkboard.Infrastructure.Abstractions` | 端口接口 |
| `Inkboard.Infrastructure.Persistence` | 存储（历史 SQLite；设置 JSON） |
| `Inkboard.Platform.Windows` / `.Linux` | OS 适配器 |
| `Inkboard.App` | Avalonia UI + DI 组合根 |
| `Inkboard.Tests` | 领域/应用层单测 |

依赖只指向内；业务代码不写 `if (Windows)`。详见 [AGENTS.md](AGENTS.md) 与 [docs/architecture/overview.md](docs/architecture/overview.md)。

## 开发约定

- 代码优雅简洁，中文注释写「为什么」；样式走 `Themes/` 令牌
- 先扩接口/用例，再填平台实现
- 提交前 `make check`
