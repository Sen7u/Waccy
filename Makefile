# Inkboard 开发便捷入口
# 用法：make <目标>    查看全部：make help

export PATH := $(HOME)/.dotnet:$(PATH)
export DOTNET_ROOT := $(HOME)/.dotnet

SOLUTION := Inkboard.sln
APP_PROJECT := src/Inkboard.App/Inkboard.App.csproj
TEST_PROJECT := tests/Inkboard.Tests/Inkboard.Tests.csproj
CONFIGURATION ?= Debug
REFERENCE_DIR := reference/Maccy
REFERENCE_URL := https://github.com/p0deje/Maccy.git

.PHONY: help restore build test run clean watch coverage reference format check scaffold-verify publish-win

help: ## 显示可用命令
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}'

restore: ## 还原 NuGet 依赖
	dotnet restore $(SOLUTION)

build: ## 编译整个解决方案
	dotnet build $(SOLUTION) -c $(CONFIGURATION) --nologo

test: ## 运行单元测试
	dotnet test $(TEST_PROJECT) -c $(CONFIGURATION) --nologo --verbosity minimal

run: ## 启动 Avalonia 应用（Linux/Windows）
	dotnet run --project $(APP_PROJECT) -c $(CONFIGURATION) --no-launch-profile

watch: ## 监视测试并自动重跑
	dotnet watch --project $(TEST_PROJECT) test --nologo

coverage: ## 生成覆盖率
	dotnet test $(TEST_PROJECT) -c $(CONFIGURATION) --nologo \
		--collect:"XPlat Code Coverage" \
		--results-directory TestResults

clean: ## 清理 bin/obj
	dotnet clean $(SOLUTION) -c $(CONFIGURATION) --nologo
	find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2>/dev/null || true

format: ## 格式化代码
	dotnet format $(SOLUTION)

check: restore build test ## 还原 + 编译 + 测试（提交前建议跑）

scaffold-verify: check ## 验证架子可编译、单测可通过
	@echo "脚手架验证通过。"

reference: ## 克隆 Maccy 到 reference/（只读对照，不入库）
	@mkdir -p reference
	@if [ -d "$(REFERENCE_DIR)/.git" ]; then \
		echo "已存在 $(REFERENCE_DIR)，跳过克隆。"; \
	else \
		git clone --depth 1 $(REFERENCE_URL) $(REFERENCE_DIR); \
	fi

PUBLISH_DIR := dist/Inkboard-win-x64
PUBLISH_ZIP := dist/Inkboard-win-x64.zip

publish-win: ## 交叉编译 Windows x64 自包含 zip → dist/
	@rm -rf $(PUBLISH_DIR) $(PUBLISH_ZIP)
	dotnet publish $(APP_PROJECT) -c Release -r win-x64 --self-contained true \
		-p:PublishSingleFile=false \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-o $(PUBLISH_DIR) --nologo
	@cd dist && zip -qr Inkboard-win-x64.zip Inkboard-win-x64
	@echo "已生成 $(PUBLISH_ZIP) ($$(du -h $(PUBLISH_ZIP) | cut -f1))"
