# Inkboard — Agent / 协作者指南

跨平台剪贴板历史管理器（对齐 [Maccy](https://github.com/p0deje/Maccy) 行为，Avalonia + .NET 8 重写）。  
本文约定目录、分层与开发节奏，方便人与 Agent 用同一套积木方式改代码。

## 快速命令

```bash
make help          # 查看命令
make restore       # 还原依赖
make build         # 编译
make test          # 单测
make run           # 启动 UI
make check         # restore + build + test
make reference     # 拉取 reference/Maccy（只读对照）
```

需要本机安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

## 目录结构

```
Inkboard.sln
Makefile
AGENTS.md
README.md
docs/architecture/          # 架构说明
reference/                  # 本地对照（gitignore，make reference）
src/
  Inkboard.Domain/          # 领域模型与纯规则（无 UI、无 OS API）
  Inkboard.Application/     # 用例编排（依赖 Domain + Abstractions）
  Inkboard.Infrastructure.Abstractions/  # 端口接口
  Inkboard.Infrastructure.Persistence/  # SQLite 等实现
  Inkboard.Platform.Windows/            # Win32 适配器
  Inkboard.Platform.Linux/              # Linux 适配器
  Inkboard.App/             # Avalonia UI + DI 组合根
tests/
  Inkboard.Tests/           # 领域/应用层单测
```

## 分层规则（必须遵守）

1. **依赖只指向内**：`App → Application → Domain`；平台与持久化只依赖 `Abstractions`（及 Domain 类型）。
2. **业务零 OS 分支**：禁止在 Domain/Application 写 `if (Windows)`；差异放进 `Platform.*`。
3. **积木扩展**：新功能优先加 Application 用例 + 可选 Domain 类型；新平台能力只改对应 Platform 项目。
4. **ViewModel 保持薄**：转发布命令与状态，复杂逻辑进 Application。
5. **YAGNI**：不为「将来可能」加空抽象、堆防护性样板代码。
6. **中文注释**：关键类型、公共 API、非显然平台坑、用例流程说明「为什么」；不写复述代码的废话。

## 组合根

DI 注册集中在 `src/Inkboard.App/Composition/`。  
启动时按当前 OS **只注册一个平台适配器实现**，其余代码无感知。

## 测试约定

- 必测：搜索、去重、忽略规则、历史上限、置顶排序。
- 不测：Avalonia 视觉皮、纯 P/Invoke 包装。
- 使用内存假实现替代真实剪贴板；本云 Linux 跑 `make test` 即可。

## 开发节奏

1. **先架子后功能**：目录、接口、DI、主题资源到位，再填模块实现。
2. 改功能前先想清楚落在哪一层；不确定时问，或默认放 Application。
3. 提交前跑 `make check`。
4. UI 走墨青工具感设计（见 `docs/architecture/ui-design.md`），不要套默认模板皮。

## 参考

- 行为对照：`make reference` → `reference/Maccy`
- 架构图：`docs/architecture/overview.md`
