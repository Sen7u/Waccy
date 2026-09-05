# Inkboard — Agent / 协作者指南

跨平台剪贴板历史管理器（对齐 [Maccy](https://github.com/p0deje/Maccy) 行为，Avalonia + .NET 8 重写）。  
本文约定目录、分层、代码风格与开发节奏，方便人与 Agent 用同一套积木方式改代码。

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
  Inkboard.Infrastructure.Persistence/  # 存储实现
  Inkboard.Platform.Windows/            # Win32 适配器
  Inkboard.Platform.Linux/              # Linux 适配器
  Inkboard.App/             # Avalonia UI + DI 组合根
    Themes/                 # 设计令牌
    Styles/                 # ControlTheme
    Services/               # 仅 UI 宿主（如 PopupHost）
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

## 代码风格（优雅 / 简洁）

这些是硬性偏好，改代码时按此取舍：

1. **可读优先于炫技**：命名表达意图；短函数、早返回；避免深层嵌套。
2. **重复只写一次**：业务规则放 Domain/Application；Platform 禁止复制去重/搜索逻辑。
3. **够用即可**：能一个类说清的事不要拆三个「未来接口」；没有调用方的抽象删掉。
4. **边界才处理失败**：平台 API / IO 处消化异常；领域层用清晰前置条件，不裹无意义 try/catch。
5. **注释写「为什么」**：公共 API、动效时长选择、Win32/X11 坑必须有中文说明；禁止 `// 设置 x` 这类复述。
6. **UI 动效克制**：动效服务层级（进场/选中/删除），禁止装饰性闪烁；时长走 `Motion.*` 令牌。
7. **样式走令牌**：颜色、圆角、间距、时长只引用 `Themes/`，禁止在控件上散落魔法色值。

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
4. UI 走墨青工具感（见 `docs/architecture/ui-design.md`），不要套默认模板皮；动效与令牌同步交付。

## 参考

- 行为对照：`make reference` → `reference/Maccy`
- 架构图：`docs/architecture/overview.md`
- UI 令牌与动效：`docs/architecture/ui-design.md`
